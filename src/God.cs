using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AshesOfDisaster;

public static class God
{
    class Mode { public string Ru, En; public int Cost, Cd; public bool Target; public string DescRu, DescEn; }

    static readonly Mode[] Modes =
    {
        new Mode { Ru = "Кара", En = "Smite", Cost = 4, Cd = 25, Target = true, DescRu = "превращение в игрока убивает его", DescEn = "morph into a player to kill him" },
        new Mode { Ru = "Приговор", En = "Doom", Cost = 5, Cd = 30, Target = true, DescRu = "превращение в игрока: он умрёт через 20 с", DescEn = "morph into a player: he dies in 20 s" },
        new Mode { Ru = "Перенос", En = "Swap", Cost = 1, Cd = 10, Target = true, DescRu = "превращение в игрока меняет вас местами", DescEn = "morph into a player to swap places" },
        new Mode { Ru = "Откровение", En = "Reveal", Cost = 2, Cd = 15, Target = true, DescRu = "превращение в игрока открывает его роль", DescEn = "morph into a player to see his role" },
        new Mode { Ru = "Благословение", En = "Bless", Cost = 3, Cd = 20, Target = true, DescRu = "превращение в игрока даёт ему щит", DescEn = "morph into a player to give him a shield" },
        new Mode { Ru = "Остановка времени", En = "Time stop", Cost = 6, Cd = 60, Target = false, DescRu = "питомец: все замирают на 6 с", DescEn = "pet: everyone freezes for 6 s" },
        new Mode { Ru = "Гнев", En = "Wrath", Cost = 8, Cd = 60, Target = false, DescRu = "питомец: гибнут все ближе 3 м", DescEn = "pet: everyone within 3 m dies" },
        new Mode { Ru = "Молитва", En = "Prayer", Cost = 0, Cd = 45, Target = false, DescRu = "питомец: стой неподвижно 8 с и получи 4 веры", DescEn = "pet: stand still for 8 s and gain 4 faith" },
        new Mode { Ru = "Знамение", En = "Omen", Cost = 2, Cd = 30, Target = false, DescRu = "питомец: сколько убийц ещё живо", DescEn = "pet: how many killers are still alive" },
    };

    static readonly Dictionary<byte, int> mode = new(), faith = new();
    static readonly Dictionary<byte, float> lastFaith = new(), lastShield = new();
    static readonly List<(byte owner, byte target, float at)> dooms = new();
    static readonly Dictionary<byte, int[]> order = new();
    static readonly Dictionary<byte, int> orderPos = new(), kills = new(), streak = new(), lastPower = new();
    static readonly Dictionary<byte, bool> known = new();
    static readonly Dictionary<byte, HashSet<byte>> seen = new();
    static readonly Dictionary<byte, float> shieldBan = new();
    static readonly Dictionary<byte, (Vector2 pos, float until)> praying = new();
    static readonly System.Random rng = new();
    static int pendingCost;
    static float nextTick;
    public static IntOpt FaithStart, FaithEvery, ShieldEvery;

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "God", "Бог", "God", "#FFD54F", OptTab.Neutral,
            "Нейтрал. Для сервера ты настоящий предатель, у тебя кнопка превращения. Ты копишь веру: она растёт медленно, на собрании +1, а молитва даёт +4, но только если стоять неподвижно. Режим силы переключается люком (смена стоит 1 веру), после каждого собрания порядок режимов перемешивается, а текущий режим тебе неизвестен, пока не переключишь. Девять режимов: Кара, Приговор, Перенос, Откровение, Благословение, Остановка времени, Гнев, Молитва, Знамение. Повтор одной силы подряд дорожает на 1 веру за каждый раз. Кара и Приговор на игроке, чью роль ты не открывал Откровением, стоят на 2 веры больше. Три убийства сжигают всю веру и отнимают щит на 2 минуты. Силы с целью работают так: нажми кнопку превращения и выбери игрока в меню, сам ты не превратишься. Силы без цели включает питомец. Каждая сила стоит веры и перезаряжается. У тебя свой щит, он сам возвращается. Ты не видишь роли, пока не используешь Откровение. [Люк, превращение и питомец]",
            "Neutral. The server sees you as a real impostor, you have a shapeshift button. You gather faith: it grows slowly, +1 at a meeting, and Prayer gives +4 but only if you stand still. The power mode is switched with a vent (a switch costs 1 faith), the order of modes is reshuffled after every meeting and the current mode is unknown until you switch. Nine modes: Smite, Doom, Swap, Reveal, Bless, Time stop, Wrath, Prayer, Omen. Repeating a power costs 1 more faith each time. Smite and Doom on a player whose role you have not revealed cost 2 more. Three kills burn all your faith and remove your shield for 2 minutes. Targeted powers work like this: press the shapeshift button and pick a player in the menu, you do not actually morph. Powers without a target are triggered by the pet. Every power costs faith and has a cooldown. You have your own shield that returns by itself. You cannot see roles until you use Reveal. [Vent, morph and pet]");
        FaithStart = Opts.Extra("God", new IntOpt("GodFaithStart", OptTab.Neutral, 3, 0, 12, 1, c), "Вера в начале игры", "Faith at the start");
        FaithEvery = Opts.Extra("God", new IntOpt("GodFaithEvery", OptTab.Neutral, 40, 5, 120, 5, c), "Раз в сколько секунд растёт вера на 1", "Seconds per +1 faith");
        ShieldEvery = Opts.Extra("God", new IntOpt("GodShieldEvery", OptTab.Neutral, 90, 15, 300, 5, c), "Через сколько секунд возвращается щит", "Seconds until the shield returns");
        RoleLogic.RealImpNeutrals.Add("God");
    }

    public static void Reset() { order.Clear(); orderPos.Clear(); kills.Clear(); streak.Clear(); lastPower.Clear(); known.Clear(); seen.Clear(); shieldBan.Clear(); praying.Clear(); mode.Clear(); faith.Clear(); lastUseKey.Clear(); lastFaith.Clear(); lastShield.Clear(); dooms.Clear(); }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static string T(bool ru, string r, string e) => ru ? r : e;
    static void H(PlayerControl p, string s) => RoleLogic.Hint(p, s);
    static string Nm(PlayerControl p) => p.Data.PlayerName;
    static int[] Ord(byte id)
    {
        if (!order.TryGetValue(id, out var o)) { o = Enumerable.Range(0, Modes.Length).OrderBy(x => rng.Next()).ToArray(); order[id] = o; }
        return o;
    }
    static int M(byte id) => Ord(id)[orderPos.TryGetValue(id, out var q) ? q : 0];
    static bool Known(byte id) => known.TryGetValue(id, out var k) && k;
    static int F(byte id) { if (!faith.TryGetValue(id, out var f)) { f = FaithStart.Value; faith[id] = f; lastFaith[id] = Time.time; } return f; }

    static bool IsGod(PlayerControl p) => p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && r.Id == "God";

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        if (!Known(p.PlayerId)) return r.Colored(ru) + ": " + T(ru, $"режим неизвестен, переключи его люком. Вера: {F(p.PlayerId)}.", $"mode unknown, switch it with a vent. Faith: {F(p.PlayerId)}.");
        var m = Modes[M(p.PlayerId)];
        return r.Colored(ru) + ": " + T(ru, $"режим {m.Ru} ({m.DescRu}). Вера: {F(p.PlayerId)}, цена {m.Cost}.", $"mode {m.En} ({m.DescEn}). Faith: {F(p.PlayerId)}, cost {m.Cost}.");
    }

    public static string Short(PlayerControl p, bool ru)
    {
        if (!Known(p.PlayerId)) return T(ru, $"режим неизвестен, вера {F(p.PlayerId)}", $"mode unknown, faith {F(p.PlayerId)}");
        var m = Modes[M(p.PlayerId)];
        return T(ru, $"{m.Ru}, вера {F(p.PlayerId)}/{m.Cost}", $"{m.En}, faith {F(p.PlayerId)}/{m.Cost}");
    }

    public static void OnVent(PlayerControl p, int ventId)
    {
        try
        {
            if (!Utils.AmHost || !IsGod(p) || !Alive(p)) return;
            orderPos[p.PlayerId] = ((orderPos.TryGetValue(p.PlayerId, out var q) ? q : 0) + 1) % Modes.Length;
            known[p.PlayerId] = true;
            if (F(p.PlayerId) > 0) faith[p.PlayerId] = F(p.PlayerId) - 1;
            int next = M(p.PlayerId);
            Extra33.QueueBoot(p.PlayerId, ventId);
            var m = Modes[next];
            bool ru = Translator.IsRu(p);
            H(p, T(ru, $"Режим (смена стоит 1 веру): {m.Ru}. {m.DescRu}. Цена: {m.Cost}, вера: {F(p.PlayerId)}.", $"Mode: {m.En}. {m.DescEn}. Cost: {m.Cost}, faith: {F(p.PlayerId)}."));
        }
        catch (Exception e) { Plugin.Logger.LogWarning("God.OnVent: " + e.Message); }
    }

    static bool Ready(PlayerControl p, bool ru, bool wantTarget, PlayerControl target = null)
    {
        if (!Known(p.PlayerId)) { H(p, T(ru, "Ты не знаешь, какой сейчас режим. Переключи его люком.", "You do not know the current mode. Switch it with a vent.")); return false; }
        int idx = M(p.PlayerId); var m = Modes[idx];
        if (m.Target != wantTarget)
        {
            H(p, T(ru, wantTarget ? $"Режим {m.Ru} включается питомцем, а не превращением." : $"Режим {m.Ru} работает через превращение: выбери игрока в меню.", wantTarget ? $"Mode {m.En} is used with the pet, not the morph." : $"Mode {m.En} works through the morph: pick a player in the menu."));
            return false;
        }
        string key = idx + ":" + p.PlayerId;
        if (lastUseKey.TryGetValue(key, out var t0) && Time.time - t0 < m.Cd)
        { H(p, T(ru, $"Перезарядка: ещё {Mathf.CeilToInt(m.Cd - (Time.time - t0))} с.", $"Cooldown: {Mathf.CeilToInt(m.Cd - (Time.time - t0))} s left.")); return false; }
        int cost = m.Cost;
        if (lastPower.TryGetValue(p.PlayerId, out var lp) && lp == idx) cost += streak.TryGetValue(p.PlayerId, out var sk) ? sk : 0;
        if ((idx == 0 || idx == 1) && target != null && !(seen.TryGetValue(p.PlayerId, out var sn) && sn.Contains(target.PlayerId))) cost += 2;
        if (F(p.PlayerId) < cost)
        { H(p, T(ru, $"Не хватает веры: {F(p.PlayerId)} из {cost}.", $"Not enough faith: {F(p.PlayerId)} of {cost}.")); return false; }
        pendingCost = cost;
        return true;
    }

    static readonly Dictionary<string, float> lastUseKey = new();

    static void Pay(PlayerControl p)
    {
        int idx = M(p.PlayerId);
        faith[p.PlayerId] = Math.Max(0, F(p.PlayerId) - pendingCost);
        if (lastPower.TryGetValue(p.PlayerId, out var lp) && lp == idx) streak[p.PlayerId] = (streak.TryGetValue(p.PlayerId, out var sk) ? sk : 0) + 1; else streak[p.PlayerId] = 1;
        lastPower[p.PlayerId] = idx;
        if (kills.TryGetValue(p.PlayerId, out var kc) && kc >= 3)
        {
            kills[p.PlayerId] = 0; faith[p.PlayerId] = 0; shieldBan[p.PlayerId] = Time.time + 120f; RoleLogic.ShieldArmed.Remove(p.PlayerId);
            bool pru = Translator.IsRu(p);
            H(p, T(pru, "Гордыня! Три убийства: вера сгорела, щит потерян на 2 минуты.", "Hubris! Three kills: your faith burned away and the shield is gone for 2 minutes."));
        }
        lastUseKey[idx + ":" + p.PlayerId] = Time.time;
    }

    static bool Strike(PlayerControl by, PlayerControl t)
    {
        if (!Alive(by) || !Alive(t) || by.PlayerId == t.PlayerId) return false;
        if (!RoleLogic.CanKill(by, t)) return false;
        bool ru = Translator.IsRu(by);
        if (RoleLogic.ShieldArmed.Remove(t.PlayerId))
        {
            RoleLogic.ShieldUsed.Add(t.PlayerId);
            Utils.SendSecret(t, Translator.IsRu(t) ? "Защита сработала!" : "Shield triggered!");
            H(by, T(ru, $"У {Nm(t)} сработала защита.", $"{Nm(t)} had a shield."));
            return true;
        }
        if (!RoleLogic.Kill(by, t, true)) return false;
        kills[by.PlayerId] = (kills.TryGetValue(by.PlayerId, out var kk) ? kk : 0) + 1;
        H(by, T(ru, $"Убит: {Nm(t)}.", $"Killed: {Nm(t)}."));
        return true;
    }

    // кнопка превращения: возвращает true, если превращение надо отменить
    public static bool OnMorph(PlayerControl god, PlayerControl target)
    {
        try
        {
            if (!Utils.AmHost || !IsGod(god) || !Alive(god)) return false;
            bool ru = Translator.IsRu(god);
            if (target == null || !Alive(target) || target.PlayerId == god.PlayerId) return true;
            if (!Ready(god, ru, true, target)) return true;
            int idx = M(god.PlayerId);
            switch (idx)
            {
                case 0:
                    if (!Strike(god, target)) { H(god, T(ru, "Кара не удалась.", "The smite failed.")); return true; }
                    break;
                case 1:
                    if (dooms.Any(d => d.target == target.PlayerId)) { H(god, T(ru, "Он уже приговорён.", "He is already doomed.")); return true; }
                    dooms.Add((god.PlayerId, target.PlayerId, Time.time + 20f));
                    H(god, T(ru, $"{Nm(target)} приговорён, у него 20 с.", $"{Nm(target)} is doomed, 20 s left."));
                    break;
                case 2:
                    {
                        var a = god.GetTruePosition(); var b = target.GetTruePosition();
                        Commands.Tp(god, b); Commands.Tp(target, a);
                        H(god, T(ru, $"Вы с {Nm(target)} поменялись местами.", $"You and {Nm(target)} swapped places."));
                        break;
                    }
                case 3:
                    if (!seen.TryGetValue(god.PlayerId, out var sset)) seen[god.PlayerId] = sset = new HashSet<byte>();
                    sset.Add(target.PlayerId);
                    H(god, T(ru, $"Откровение: {Nm(target)} - {RoleLogic.RoleLabel(target.PlayerId, target.Data.RoleType, true)}.", $"Revelation: {Nm(target)} - {RoleLogic.RoleLabel(target.PlayerId, target.Data.RoleType, false)}."));
                    break;
                case 4:
                    RoleLogic.ShieldArmed.Add(target.PlayerId); RoleLogic.ShieldUsed.Remove(target.PlayerId);
                    H(god, T(ru, $"{Nm(target)} под твоей защитой.", $"{Nm(target)} is under your protection."));
                    Utils.SendSecret(target, Translator.IsRu(target) ? "Кто-то свыше дал тебе щит." : "Someone above gave you a shield.");
                    break;
                default:
                    return true;
            }
            Pay(god);
            try { NameInfo.Refresh(); } catch { }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("God.OnMorph: " + e.Message); }
        return true;
    }

    // питомец: силы без цели
    public static bool Use(PlayerControl p)
    {
        try
        {
            bool ru = Translator.IsRu(p);
            if (!Ready(p, ru, false)) return true;
            int idx = M(p.PlayerId);
            var others = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && o.PlayerId != p.PlayerId).ToList();
            if (idx == 5)
            {
                foreach (var o in others) Roles30.FreezePlayer(o, 6f);
                H(p, T(ru, "Время остановилось на 6 секунд.", "Time has stopped for 6 seconds."));
            }
            else if (idx == 6)
            {
                var me = p.GetTruePosition();
                var near = others.Where(o => Vector2.Distance(me, o.GetTruePosition()) <= 3f).ToList();
                if (near.Count == 0) { H(p, T(ru, "Рядом никого нет (до 3 м).", "Nobody within 3 m.")); return true; }
                foreach (var o in near) Strike(p, o);
            }
            else if (idx == 7)
            {
                praying[p.PlayerId] = (p.GetTruePosition(), Time.time + 8f);
                H(p, T(ru, "Ты молишься. Не двигайся 8 секунд.", "You pray. Do not move for 8 seconds."));
            }
            else if (idx == 8)
            {
                int n = others.Count(o => RoleLogic.IsImpRole(o.Data.RoleType) && !RoleLogic.Demoted.Contains(o.PlayerId));
                H(p, T(ru, $"Знамение: убийц ещё живо {n}.", $"Omen: killers still alive: {n}."));
            }
            else return true;
            Pay(p);
            try { NameInfo.Refresh(); } catch { }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("God.Use: " + e.Message); }
        return true;
    }

    public static void OnMeeting()
    {
        dooms.Clear(); praying.Clear();
        foreach (var kv in RoleState.Assigned.ToList())
            if (kv.Value.Id == "God")
            {
                faith[kv.Key] = Math.Min(20, F(kv.Key) + 1);
                order[kv.Key] = Enumerable.Range(0, Modes.Length).OrderBy(x => rng.Next()).ToArray();
                orderPos[kv.Key] = 0; known[kv.Key] = false; streak[kv.Key] = 0; lastPower.Remove(kv.Key);
            }
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.5f;
        if (MeetingHud.Instance != null) return;
        try
        {
            float now = Time.time;
            foreach (var d in dooms.Where(x => now >= x.at).ToList())
            {
                dooms.Remove(d);
                var owner = Utils.GetPlayer(d.owner); var tg = Utils.GetPlayer(d.target);
                if (Alive(owner) && Alive(tg)) Strike(owner, tg);
            }
            foreach (var pr in praying.ToList())
            {
                var pg = Utils.GetPlayer(pr.Key);
                if (!Alive(pg)) { praying.Remove(pr.Key); continue; }
                bool pru = Translator.IsRu(pg);
                if (Vector2.Distance(pg.GetTruePosition(), pr.Value.pos) > 0.4f) { praying.Remove(pr.Key); H(pg, T(pru, "Ты сдвинулся: молитва сорвана.", "You moved: the prayer is broken.")); continue; }
                if (now >= pr.Value.until) { praying.Remove(pr.Key); faith[pr.Key] = Math.Min(20, F(pr.Key) + 4); H(pg, T(pru, $"Молитва услышана. Вера: {F(pr.Key)}.", $"The prayer is heard. Faith: {F(pr.Key)}.")); }
            }
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "God") continue;
                var g = Utils.GetPlayer(kv.Key);
                if (!Alive(g)) continue;
                if (shieldBan.TryGetValue(kv.Key, out var ban) && now < ban) { RoleLogic.ShieldArmed.Remove(kv.Key); lastShield[kv.Key] = now; }
                F(kv.Key);
                if (!lastFaith.TryGetValue(kv.Key, out var lf)) lastFaith[kv.Key] = lf = now;
                if (now - lf >= FaithEvery.Value) { faith[kv.Key] = Math.Min(20, F(kv.Key) + 1); lastFaith[kv.Key] = now; }
                if (!lastShield.TryGetValue(kv.Key, out var ls)) lastShield[kv.Key] = ls = now;
                if (RoleLogic.ShieldArmed.Contains(kv.Key)) lastShield[kv.Key] = now;
                else if (now - ls >= ShieldEvery.Value)
                {
                    RoleLogic.ShieldArmed.Add(kv.Key); RoleLogic.ShieldUsed.Remove(kv.Key); lastShield[kv.Key] = now;
                    H(g, T(Translator.IsRu(g), "Щит вернулся.", "The shield is back."));
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("God.Tick: " + e.Message); }
    }
}

[HarmonyPatch(typeof(PlayerControl), "CheckShapeshift")]
public static class GodShapeshiftPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(PlayerControl), "CheckShapeshift");

    public static bool Prefix(PlayerControl __instance, PlayerControl target)
    {
        try
        {
            if (!God.OnMorph(__instance, target)) return true;
            try
            {
                var rej = AccessTools.Method(typeof(PlayerControl), "RpcRejectShapeshift");
                if (rej != null) rej.Invoke(__instance, null);
            }
            catch { }
            return false;
        }
        catch { return true; }
    }
}
