using System;
using InnerNet;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class Extras
{
    public static BoolOpt PerksOn, BountyOn, EventsOn;
    public static IntOpt EventEvery;

    static readonly System.Random rng = new();
    static readonly Dictionary<byte, int> perk = new();
    static readonly HashSet<byte> perkUsed = new();
    static readonly Dictionary<byte, byte> target = new();
    static float nextEvent;
    static bool running;

    static readonly string[] PerkRu = { "Везунчик", "Зоркий", "Шустрый" };
    static readonly string[] PerkEn = { "Lucky", "Sharp", "Quick" };
    static readonly string[] PerkTextRu =
    {
        "первое нападение на тебя отменится",
        "на первом собрании ты узнаешь одного игрока, который точно не предатель",
        "один раз за игру команда /perk сбросит все твои перезарядки"
    };
    static readonly string[] PerkTextEn =
    {
        "the first attack on you is cancelled",
        "at the first meeting you learn one player who is surely not an impostor",
        "once per game /perk resets all your cooldowns"
    };

    public static void Define(ConfigFile c)
    {
        PerksOn = new BoolOpt("PerksOn", OptTab.System, true, c);
        PerksOn.NameRu = "Перки: каждому игроку случайный довесок"; PerksOn.NameEn = "Perks: a random bonus for every player";
        BountyOn = new BoolOpt("BountyOn", OptTab.System, true, c);
        BountyOn.NameRu = "Охота за головами: у каждого тайная цель"; BountyOn.NameEn = "Bounty hunt: everyone has a secret target";
        EventsOn = new BoolOpt("EventsOn", OptTab.System, true, c);
        EventsOn.NameRu = "События раунда"; EventsOn.NameEn = "Round events";
        EventEvery = new IntOpt("EventEvery", OptTab.System, 3, 1, 10, 1, c);
        EventEvery.NameRu = "События раунда: раз в сколько минут"; EventEvery.NameEn = "Round events: every how many minutes";
        foreach (var o in new Opt[] { PerksOn, BountyOn, EventsOn, EventEvery }) { o.GroupRu = "Режимы"; o.GroupEn = "Modes"; }
    }

    public static void Reset()
    {
        perk.Clear(); perkUsed.Clear(); target.Clear(); running = false; nextEvent = 0f;
    }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => !RoleLogic.Demoted.Contains(p.PlayerId) && (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId));
    static string Nm(byte id) { var p = Utils.GetPlayer(id); return p != null && p.Data != null ? p.Data.PlayerName : "?"; }

    static List<PlayerControl> Living()
    {
        var l = new List<PlayerControl>();
        foreach (var p in PlayerControl.AllPlayerControls) if (Alive(p)) l.Add(p);
        return l;
    }

    static void ClearCooldowns(byte id)
    {
        Roles75.ResetCooldowns(id); Roles30.ResetCooldowns(id); NR.ResetCooldowns(id); Plumber.ResetCooldowns(id); Pinkman.ResetCooldowns(id);
    }

    public static void OnAssigned()
    {
        Reset();
        if (!Utils.AmHost) return;
        running = true;
        nextEvent = Time.time + EventEvery.Value * 60f;
        var all = Living();
        foreach (var p in all)
        {
            bool ru = Translator.IsRu(p);
            if (PerksOn.Value)
            {
                int k = rng.Next(PerkRu.Length);
                perk[p.PlayerId] = k;
                if (k == 0) RoleLogic.ShieldArmed.Add(p.PlayerId);
                Utils.SendSecret(p, ru ? $"Твой перк: {PerkRu[k]}, {PerkTextRu[k]}. Подробнее: /perk" : $"Your perk: {PerkEn[k]}, {PerkTextEn[k]}. More: /perk");
            }
            if (BountyOn.Value && all.Count > 1)
            {
                var others = all.Where(x => x.PlayerId != p.PlayerId).ToList();
                var t = others[rng.Next(others.Count)];
                target[p.PlayerId] = t.PlayerId;
                Utils.SendSecret(p, ru ? $"Твоя тайная цель: {t.Data.PlayerName}. Если к собранию цель погибнет, получишь щит и новую цель. Напомнить: /bounty" : $"Your secret target: {t.Data.PlayerName}. If the target is dead by a meeting you get a shield and a new target. Reminder: /bounty");
            }
        }
    }

    public static void OnMeeting()
    {
        if (!Utils.AmHost || !running) return;
        try
        {
            if (PerksOn.Value)
            {
                foreach (var kv in perk.ToList())
                {
                    if (kv.Value != 1 || !perkUsed.Add(kv.Key)) continue;
                    var p = Utils.GetPlayer(kv.Key);
                    if (!Alive(p)) continue;
                    var pool = Living().Where(x => x.PlayerId != p.PlayerId && !IsImp(x)).ToList();
                    if (pool.Count == 0) continue;
                    var pick = pool[rng.Next(pool.Count)];
                    bool ru = Translator.IsRu(p);
                    Utils.SendSecret(p, ru ? $"Зоркий: {pick.Data.PlayerName} точно не предатель." : $"Sharp: {pick.Data.PlayerName} is surely not an impostor.");
                }
            }
            if (BountyOn.Value)
            {
                foreach (var kv in target.ToList())
                {
                    var hunter = Utils.GetPlayer(kv.Key);
                    if (!Alive(hunter)) { target.Remove(kv.Key); continue; }
                    var victim = Utils.GetPlayer(kv.Value);
                    if (Alive(victim)) continue;
                    bool ru = Translator.IsRu(hunter);
                    RoleLogic.ShieldArmed.Add(hunter.PlayerId);
                    var others = Living().Where(x => x.PlayerId != hunter.PlayerId).ToList();
                    string head = ru ? $"Цель {Nm(kv.Value)} выбыла, тебе выдан щит." : $"Target {Nm(kv.Value)} is out, you got a shield.";
                    if (others.Count == 0) { target.Remove(kv.Key); Utils.SendSecret(hunter, head); continue; }
                    var next = others[rng.Next(others.Count)];
                    target[kv.Key] = next.PlayerId;
                    Utils.SendSecret(hunter, head + (ru ? $" Новая цель: {next.Data.PlayerName}." : $" New target: {next.Data.PlayerName}."));
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Extras.OnMeeting: " + e.Message); }
    }

    static bool InRound() =>
        AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started
        && MeetingHud.Instance == null && ShipStatus.Instance != null;

    static void Everyone(string ru, string en)
    {
        foreach (var p in Living()) RoleLogic.Hint(p, Translator.IsRu(p) ? ru : en);
    }

    public static void Tick()
    {
        if (!Utils.AmHost || !running || EventsOn == null || !EventsOn.Value || !InRound() || Time.time < nextEvent) return;
        nextEvent = Time.time + EventEvery.Value * 60f;
        try
        {
            var all = Living();
            if (all.Count < 2) return;
            switch (rng.Next(3))
            {
                case 0:
                    {
                        var p = all[rng.Next(all.Count)];
                        RoleLogic.ShieldArmed.Add(p.PlayerId);
                        RoleLogic.Hint(p, Translator.IsRu(p) ? "Событие: судьба подарила тебе щит!" : "Event: fate gave you a shield!");
                        foreach (var o in all) if (o.PlayerId != p.PlayerId) RoleLogic.Hint(o, Translator.IsRu(o) ? "Событие: кому-то из живых выпал щит." : "Event: someone alive got a shield.");
                        break;
                    }
                case 1:
                    foreach (var p in all) ClearCooldowns(p.PlayerId);
                    Everyone("Событие: передышка, у всех сброшены перезарядки.", "Event: a breather, everyone's cooldowns are reset.");
                    break;
                default:
                    {
                        int imps = all.Count(IsImp);
                        Everyone($"Событие: тревога, живых предателей в комнате: {imps}.", $"Event: alert, impostors alive: {imps}.");
                        break;
                    }
            }
            Plugin.Logger.LogInfo("Extras: round event fired");
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Extras.Tick: " + e.Message); }
    }

    public static bool Handle(PlayerControl me, string cmd, string[] a)
    {
        bool ru = Translator.IsRu(me);
        void Say(string r, string e) => Utils.SendPrivate(me, ru ? r : e);
        switch (cmd)
        {
            case "perk":
                {
                    if (PerksOn == null || !PerksOn.Value) { Say("Перки выключены.", "Perks are off."); return true; }
                    if (!perk.TryGetValue(me.PlayerId, out var k)) { Say("Перки выдаются в начале игры.", "Perks are given when a game starts."); return true; }
                    if (k == 2 && Alive(me) && !Utils.InLobby && perkUsed.Add(me.PlayerId))
                    {
                        ClearCooldowns(me.PlayerId);
                        Say("Шустрый: перезарядки сброшены. Больше так не выйдет.", "Quick: cooldowns reset. Only once per game.");
                        return true;
                    }
                    string used = k == 2 && perkUsed.Contains(me.PlayerId) ? (ru ? " (уже использован)" : " (already used)") : "";
                    Say($"Твой перк: {PerkRu[k]}, {PerkTextRu[k]}{used}.", $"Your perk: {PerkEn[k]}, {PerkTextEn[k]}{used}.");
                    return true;
                }
            case "bounty":
                {
                    if (BountyOn == null || !BountyOn.Value) { Say("Охота за головами выключена.", "Bounty hunt is off."); return true; }
                    if (!target.TryGetValue(me.PlayerId, out var t)) { Say("Цель выдаётся в начале игры.", "A target is given when a game starts."); return true; }
                    Say($"Твоя цель: {Nm(t)}. Погибнет к собранию, получишь щит.", $"Your target: {Nm(t)}. If dead by a meeting you get a shield.");
                    return true;
                }
        }
        return false;
    }
}
