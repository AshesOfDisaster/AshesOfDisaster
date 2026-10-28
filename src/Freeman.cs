using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AshesOfDisaster;

// Фримен: нейтрал-убийца с ломом. Режимы лома переключаются люком.
public static class Freeman
{
    class St
    {
        public int Mode;        // 0 щит, 1 двери, 2 убийство
        public int Stage;       // 0 квест закрыт, 1 кристалл в нижнем моторе, 2 несёт его, 3 кристалл в верхнем моторе, 4 несёт второй, 5 убийство открыто
        public Vector2 Prev; public bool HasPrev; public float PrevT;
        public float LastVent;
    }

    static readonly Dictionary<byte, St> st = new();
    static readonly Dictionary<string, float> last = new();
    static int meetings;
    static readonly List<(byte pid, int vent, float at)> boots = new();
    static float nextTick;
    static bool fastTick;

    public static IntOpt ShieldCd, DoorCd, KillCd, QuestMeetings;

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "Freeman", "Фримен", "Freeman", "#FF9100", OptTab.Neutral,
            "Нейтрал. Для сервера ты настоящий предатель. У тебя лом, у него три режима, и переключаются они люком. Прыгни в люк, и ты сразу выпрыгнешь: режим сменится, а над головой будет написана способность. Используется питомцем. Щит: даёт тебе щит. Двери: открывает все двери в комнате, где ты стоишь. Убийство: убивает ближайшего игрока (до 2 м), но режим закрыт, пока не пройден квест. Квест открывается после 2 собрания: возьми кристалл в нижнем моторе и отнеси его в навигацию, потом возьми второй в верхнем моторе и тоже отнеси в навигацию. С кристаллом ты очень медленный и не можешь использовать способности. Квест работает на карте Скелд, на других картах убийство открывается сразу после 2 собрания. [Питомец, люк]",
            "Neutral. The server sees you as a real impostor. You have a crowbar with three modes, switched with a vent. Jump into a vent and you pop out at once: the mode changes and the ability is written above your head. It is used with the pet. Shield: gives you a shield. Doors: opens all doors in the room you stand in. Kill: kills the nearest player (within 2 m), but the mode is locked until the quest is done. The quest opens after the 2nd meeting: take a crystal in lower engine and carry it to navigation, then take a second one in upper engine and carry it to navigation too. With a crystal you are very slow and cannot use abilities. The quest works on Skeld; on other maps the kill mode opens right after the 2nd meeting. [Pet, vent]");
        ShieldCd = Opts.Extra("Freeman", new IntOpt("FreemanShieldCd", OptTab.Neutral, 60, 10, 240, 5, c), "Перезарядка щита (сек)", "Shield cooldown (sec)");
        DoorCd = Opts.Extra("Freeman", new IntOpt("FreemanDoorCd", OptTab.Neutral, 15, 5, 120, 5, c), "Перезарядка дверей (сек)", "Doors cooldown (sec)");
        KillCd = Opts.Extra("Freeman", new IntOpt("FreemanKillCd", OptTab.Neutral, 35, 10, 180, 5, c), "Перезарядка убийства (сек)", "Kill cooldown (sec)");
        QuestMeetings = Opts.Extra("Freeman", new IntOpt("FreemanQuestMeetings", OptTab.Neutral, 2, 1, 10, 1, c), "После какого собрания открывается квест", "Quest opens after meeting number");
        RoleLogic.RealImpNeutrals.Add("Freeman");
    }

    public static List<(string key, byte owner, Vector2 pos, string ru, string en, string color)> Items()
    {
        var l = new List<(string key, byte owner, Vector2 pos, string ru, string en, string color)>();
        foreach (var kv in st)
        {
            var s = kv.Value; Vector2 c;
            if (s.Stage == 1 && RoomCenter(SystemTypes.LowerEngine, out c)) l.Add(("fq1_" + kv.Key, kv.Key, c, "Кристалл", "Crystal", "#4DD0E1"));
            else if (s.Stage == 3 && RoomCenter(SystemTypes.UpperEngine, out c)) l.Add(("fq3_" + kv.Key, kv.Key, c, "Кристалл", "Crystal", "#4DD0E1"));
            else if ((s.Stage == 2 || s.Stage == 4) && RoomCenter(SystemTypes.Nav, out c)) l.Add(("fqn_" + kv.Key, kv.Key, c, "Сюда кристалл", "Deliver here", "#FFD54F"));
        }
        return l;
    }

    public static void Reset() { st.Clear(); last.Clear(); boots.Clear(); meetings = 0; }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static St S(byte id) { if (!st.TryGetValue(id, out var s)) { s = new St(); st[id] = s; } return s; }
    static string T(bool ru, string r, string e) => ru ? r : e;
    static void H(PlayerControl p, string s) => RoleLogic.Hint(p, s);

    static bool RoomCenter(SystemTypes id, out Vector2 c)
    {
        c = default;
        try
        {
            var rooms = ShipStatus.Instance != null ? ShipStatus.Instance.AllRooms : null;
            if (rooms == null) return false;
            foreach (var r in rooms)
                if (r != null && r.roomArea != null && r.RoomId == id) { c = (Vector2)r.roomArea.bounds.center; return true; }
        }
        catch { }
        return false;
    }

    static bool QuestMap() => RoomCenter(SystemTypes.LowerEngine, out _) && RoomCenter(SystemTypes.UpperEngine, out _) && RoomCenter(SystemTypes.Nav, out _);

    static string ModeName(int m, bool ru) => m == 0 ? T(ru, "Щит", "Shield") : m == 1 ? T(ru, "Двери", "Doors") : T(ru, "Убийство", "Kill");

    static string QuestText(St s, bool ru)
    {
        switch (s.Stage)
        {
            case 0: return T(ru, $"квест откроется после {QuestMeetings.Value} собрания (было {meetings})", $"the quest opens after meeting {QuestMeetings.Value} (so far {meetings})");
            case 1: return T(ru, "квест: возьми кристалл в нижнем моторе", "quest: take the crystal in lower engine");
            case 2: return T(ru, "несёшь кристалл: отнеси в навигацию", "carrying the crystal: take it to navigation");
            case 3: return T(ru, "квест: возьми второй кристалл в верхнем моторе", "quest: take the second crystal in upper engine");
            case 4: return T(ru, "несёшь второй кристалл: отнеси в навигацию", "carrying the second crystal: take it to navigation");
            default: return T(ru, "убийство открыто", "kill mode unlocked");
        }
    }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p); var s = S(p.PlayerId);
        return ru ? $"{r.Colored(true)}: лом, режим «{ModeName(s.Mode, true)}» (люк переключает: щит, двери, убийство). Питомец использует режим. {QuestText(s, true)}."
                  : $"{r.Colored(false)}: crowbar, mode \"{ModeName(s.Mode, false)}\" (a vent switches: shield, doors, kill). The pet uses the mode. {QuestText(s, false)}.";
    }

    static float CdFor(int m) => m == 0 ? ShieldCd.Value : m == 1 ? DoorCd.Value : KillCd.Value;

    public static string Short(PlayerControl p, bool ru)
    {
        var s = S(p.PlayerId);
        string cd = "";
        if (last.TryGetValue("m" + s.Mode + ":" + p.PlayerId, out var t0) && Time.time - t0 < CdFor(s.Mode))
            cd = T(ru, $", перезарядка {Mathf.CeilToInt(CdFor(s.Mode) - (Time.time - t0))} с", $", cooldown {Mathf.CeilToInt(CdFor(s.Mode) - (Time.time - t0))} s");
        return T(ru, $"лом: {ModeName(s.Mode, true)}{cd}\n{QuestText(s, true)}", $"crowbar: {ModeName(s.Mode, false)}{cd}\n{QuestText(s, false)}");
    }

    public static void OnVent(PlayerControl p, int ventId)
    {
        try
        {
            if (!Utils.AmHost || p == null || !RoleLogic.Has(p, "Freeman")) return;
            var s = S(p.PlayerId);
            if (Time.time - s.LastVent < 1.5f) return;
            s.LastVent = Time.time;
            Extra33.QueueBoot(p.PlayerId, ventId);
            int n = (s.Mode + 1) % 3;
            if (n == 2 && s.Stage < 5) n = 0;
            s.Mode = n;
            bool ruV = Translator.IsRu(p);
            H(p, T(ruV, $"Лом: режим «{ModeName(n, true)}». Применяй питомцем.", $"Crowbar: mode \"{ModeName(n, false)}\". Use it with the pet."));
            NameInfo.Notify(p, T(ruV, $"Лом: {ModeName(n, true)}", $"Crowbar: {ModeName(n, false)}"), 6f);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Freeman.OnVent: " + e.Message); }
    }

    public static void OnMeeting() { meetings++; foreach (var s in st.Values) s.HasPrev = false; }

    static bool Carrying(St s) => s.Stage == 2 || s.Stage == 4;

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        var s = S(p.PlayerId);
        if (Carrying(s)) { H(p, T(ru, "С кристаллом способности отключены.", "Abilities are off while you carry the crystal.")); return true; }
        string key = "m" + s.Mode + ":" + p.PlayerId;
        if (last.TryGetValue(key, out var t0) && Time.time - t0 < CdFor(s.Mode))
        { H(p, T(ru, $"Рано, ещё {Mathf.CeilToInt(CdFor(s.Mode) - (Time.time - t0))} с.", $"Too early, {Mathf.CeilToInt(CdFor(s.Mode) - (Time.time - t0))} s left.")); return true; }
        bool ok = false;
        try
        {
            if (s.Mode == 0) ok = Shield(p, ru);
            else if (s.Mode == 1) ok = Doors(p, ru);
            else ok = Kill(p, ru, s);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Freeman.Use: " + e.Message); }
        if (ok) last[key] = Time.time;
        return true;
    }

    static bool Shield(PlayerControl p, bool ru)
    {
        if (RoleLogic.ShieldArmed.Contains(p.PlayerId)) { H(p, T(ru, "Щит у тебя уже есть.", "You already have a shield.")); return false; }
        RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
        H(p, T(ru, "Лом: щит от первого нападения.", "Crowbar: a shield against the first attack."));
        return true;
    }

    static bool Doors(PlayerControl p, bool ru)
    {
        var ss = ShipStatus.Instance;
        var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) { H(p, T(ru, "На этой карте нет дверей.", "There are no doors on this map.")); return false; }
        var pos = p.GetTruePosition();
        SystemTypes? room = null;
        foreach (var r in ss.AllRooms)
            if (r != null && r.roomArea != null && r.roomArea.OverlapPoint(pos)) { room = r.RoomId; break; }
        ISystemType sys = ss.Systems.ContainsKey(SystemTypes.Doors) ? ss.Systems[SystemTypes.Doors] : null;
        var auto = sys != null ? sys.TryCast<AutoDoorsSystemType>() : null;
        int opened = 0;
        for (int i = 0; i < doors.Length; i++)
        {
            var d = doors[i];
            if (d == null || d.IsOpen) continue;
            bool mine = room.HasValue ? d.Room == room.Value : Vector2.Distance(pos, (Vector2)d.transform.position) <= 3f;
            if (!mine) continue;
            Welder.Unlock(d.Room);
            if (auto != null) { d.SetDoorway(true); auto.dirtyBits |= (uint)(1 << i); }
            else ss.RpcUpdateSystem(SystemTypes.Doors, (byte)(64 | i));
            opened++;
        }
        if (opened == 0) { H(p, T(ru, "Здесь нет закрытых дверей.", "No closed doors here.")); return false; }
        H(p, T(ru, $"Лом вскрыл двери: {opened}.", $"The crowbar opened doors: {opened}."));
        return true;
    }

    static bool Kill(PlayerControl p, bool ru, St s)
    {
        if (s.Stage < 5) { H(p, T(ru, "Режим убийства закрыт: пройди квест с кристаллами.", "Kill mode is locked: finish the crystal quest.")); return false; }
        PlayerControl t = null; float bd = 2f; var me = p.GetTruePosition();
        foreach (var o in PlayerControl.AllPlayerControls.ToArray())
        {
            if (!Alive(o) || o.PlayerId == p.PlayerId) continue;
            float d = Vector2.Distance(me, o.GetTruePosition());
            if (d < bd) { bd = d; t = o; }
        }
        if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
        if (!RoleLogic.CanKill(p, t)) return false;
        if (RoleLogic.ShieldArmed.Remove(t.PlayerId))
        {
            RoleLogic.ShieldUsed.Add(t.PlayerId);
            Utils.SendSecret(t, Translator.IsRu(t) ? "Защита сработала!" : "Shield triggered!");
            H(p, T(ru, $"У {t.Data.PlayerName} сработала защита.", $"{t.Data.PlayerName} had a shield."));
            return true;
        }
        if (!RoleLogic.Kill(p, t, true)) return false;
        H(p, T(ru, $"Убит: {t.Data.PlayerName}.", $"Killed: {t.Data.PlayerName}."));
        return true;
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + (fastTick ? 0.1f : 0.25f);
        fastTick = false;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null) { boots.Clear(); return; }
        try
        {
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Freeman") continue;
                var p = Utils.GetPlayer(kv.Key);
                if (!Alive(p)) continue;
                bool ru = Translator.IsRu(p);
                var s = S(kv.Key);
                var pos = p.GetTruePosition();

                if (s.Stage == 0 && meetings >= QuestMeetings.Value)
                {
                    if (QuestMap()) { s.Stage = 1; H(p, T(ru, "Квест лома открыт: в нижнем моторе лежит кристалл, возьми его (подойди) и отнеси в навигацию.", "The crowbar quest is open: a crystal lies in lower engine, take it (walk up) and carry it to navigation.")); }
                    else { s.Stage = 5; H(p, T(ru, "На этой карте нет нужных комнат: режим убийства открыт сразу.", "This map has no needed rooms: kill mode is open right away.")); }
                }
                else if (s.Stage == 1 && RoomCenter(SystemTypes.LowerEngine, out var c1) && Vector2.Distance(pos, c1) <= 1.6f)
                { s.Stage = 2; s.HasPrev = false; H(p, T(ru, "Ты взял кристалл. Ты очень медленный и без способностей. Отнеси его в навигацию.", "You took the crystal. You are very slow and have no abilities. Carry it to navigation.")); }
                else if (s.Stage == 2 && RoomCenter(SystemTypes.Nav, out var n1) && Vector2.Distance(pos, n1) <= 2f)
                { s.Stage = 3; H(p, T(ru, "Кристалл доставлен. Второй лежит в верхнем моторе.", "The crystal is delivered. The second one lies in upper engine.")); }
                else if (s.Stage == 3 && RoomCenter(SystemTypes.UpperEngine, out var c2) && Vector2.Distance(pos, c2) <= 1.6f)
                { s.Stage = 4; s.HasPrev = false; H(p, T(ru, "Ты взял второй кристалл. Отнеси его в навигацию.", "You took the second crystal. Carry it to navigation.")); }
                else if (s.Stage == 4 && RoomCenter(SystemTypes.Nav, out var n2) && Vector2.Distance(pos, n2) <= 2f)
                { s.Stage = 5; H(p, T(ru, "Квест пройден: режим убийства открыт (переключай люком).", "Quest done: kill mode is unlocked (switch with a vent).")); }

                if (Carrying(s))
                {
                    fastTick = true;
                    float dtc = s.HasPrev ? Mathf.Clamp(Time.time - s.PrevT, 0.05f, 0.5f) : 0.1f;
                    s.PrevT = Time.time;
                    if (s.HasPrev)
                    {
                        var delta = pos - s.Prev;
                        float max = 2.5f * 0.35f * dtc;
                        if (delta.magnitude > max)
                        {
                            if (Commands.WallBetween(s.Prev, pos)) { Commands.Tp(p, s.Prev); continue; }
                            var target = s.Prev + delta.normalized * max;
                            Commands.Tp(p, target);
                            s.Prev = target;
                            continue;
                        }
                    }
                    s.Prev = pos; s.HasPrev = true;
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Freeman.Tick: " + e.Message); }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), "CoEnterVent")]
public static class FreemanVentPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(PlayerPhysics), "CoEnterVent");

    public static bool Prefix(PlayerPhysics __instance, int id)
    {
        try { Freeman.OnVent(__instance.myPlayer, id); } catch { }
        try { Buttons.Vent(__instance.myPlayer, id); } catch { }
        try { Extra33.OnVent(__instance.myPlayer, id); } catch { }
        try { God.OnVent(__instance.myPlayer, id); } catch { }
        return true;
    }
}
