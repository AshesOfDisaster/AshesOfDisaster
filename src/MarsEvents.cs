using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

// «Марс»: случайные события станции на любой карте. Всё делает хост существующими средствами
// (саботаж света, закрытие двери, мигание экрана, щит Крестьянина) — клиентам ничего не отправляется сверх обычного.
public static class Mars
{
    public static BoolOpt On, Storm, Airlock, Quake, Sample;
    public static IntOpt MinGap, MaxGap, SampleSecs;
    static readonly System.Random rng = new();
    static float nextEvent, nextTick, sampleUntil;
    static Vector2 samplePos;
    static bool sampleActive;

    public static void Define(ConfigFile c)
    {
        BoolOpt B(string id, bool def, string ru, string en) { var o = new BoolOpt(id, OptTab.Game, def, c); o.NameRu = ru; o.NameEn = en; o.GroupRu = "Марс"; o.GroupEn = "Mars"; return o; }
        IntOpt I(string id, int def, int min, int max, int step, string ru, string en) { var o = new IntOpt(id, OptTab.Game, def, min, max, step, c); o.NameRu = ru; o.NameEn = en; o.GroupRu = "Марс"; o.GroupEn = "Mars"; return o; }
        On = B("MarsOn", false, "Марсианские события включены", "Mars events enabled");
        MinGap = I("MarsMinGap", 90, 30, 300, 10, "Минимум секунд между событиями", "Minimum seconds between events");
        MaxGap = I("MarsMaxGap", 180, 60, 600, 10, "Максимум секунд между событиями", "Maximum seconds between events");
        Storm = B("MarsStorm", true, "Песчаная буря (гаснет свет)", "Dust storm (lights out)");
        Airlock = B("MarsAirlock", true, "Герметизация (закрываются двери комнаты)", "Pressure seal (a room's doors close)");
        Quake = B("MarsQuake", true, "Марсотрясение (мигают экраны)", "Marsquake (screens flash)");
        Sample = B("MarsSample", true, "Образец грунта (награда — щит)", "Soil sample (reward: a shield)");
        SampleSecs = I("MarsSampleSecs", 40, 15, 120, 5, "Сколько секунд лежит образец", "Seconds the sample stays");
    }

    public static void Reset() { nextEvent = 0f; sampleActive = false; }
    public static void OnMeeting() { sampleActive = false; nextEvent = 0f; }

    static void Announce(string ru, string en)
    {
        foreach (var p in PlayerControl.AllPlayerControls.ToArray())
            if (p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected)
                RoleLogic.Hint(p, Translator.IsRu(p) ? ru : en);
    }

    static bool AnySabotage(ShipStatus ss)
    {
        if (ss.Systems == null) return false;
        foreach (var st in new[] { SystemTypes.Electrical, SystemTypes.Comms, SystemTypes.Reactor, SystemTypes.Laboratory, SystemTypes.LifeSupp, SystemTypes.HeliSabotage })
            if (ss.Systems.ContainsKey(st))
            {
                var act = ss.Systems[st].TryCast<IActivatable>();
                if (act != null && act.IsActive) return true;
            }
        return false;
    }

    static bool RandomDoor(out Vector2 pos, out SystemTypes room)
    {
        pos = default; room = default;
        var ss = ShipStatus.Instance; var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) return false;
        var d = doors[rng.Next(doors.Length)];
        if (d == null) return false;
        pos = d.transform.position; room = d.Room; return true;
    }

    public static void Tick()
    {
        if (!Utils.AmHost || On == null || !On.Value || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null || RoleState.Assigned.Count == 0) return;
        try
        {
            float now = Time.time;
            if (nextEvent <= 0f) { nextEvent = now + RandomGap(); return; }

            // образец грунта: первый живой не-предатель рядом забирает награду
            if (sampleActive)
            {
                if (now >= sampleUntil) { sampleActive = false; Announce("Образец грунта унесло ветром.", "The soil sample was blown away."); }
                else
                    foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (p == null || p.Data == null || p.Data.IsDead || p.Data.Disconnected) continue;
                        if (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId)) continue;
                        if (Vector2.Distance(p.GetTruePosition(), samplePos) > 1.5f) continue;
                        sampleActive = false;
                        bool ru = Translator.IsRu(p);
                        if (!RoleLogic.ShieldArmed.Contains(p.PlayerId)) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
                        RoleLogic.Hint(p, ru ? "Ты нашёл образец грунта: у тебя щит от первого нападения!" : "You found the soil sample: you have a shield against the first attack!");
                        Announce($"{p.Data.PlayerName} нашёл образец грунта.", $"{p.Data.PlayerName} found the soil sample.");
                        break;
                    }
            }

            if (now < nextEvent) return;
            nextEvent = now + RandomGap();
            var pool = new List<int>();
            if (Storm.Value) pool.Add(0);
            if (Airlock.Value) pool.Add(1);
            if (Quake.Value) pool.Add(2);
            if (Sample.Value && !sampleActive) pool.Add(3);
            if (pool.Count == 0) return;
            var ss = ShipStatus.Instance;
            switch (pool[rng.Next(pool.Count)])
            {
                case 0:
                    if (AnySabotage(ss)) return;
                    Announce("Песчаная буря! Свет на станции погас.", "Dust storm! The station lights went out.");
                    ss.RpcUpdateSystem(SystemTypes.Sabotage, (byte)SystemTypes.Electrical);
                    break;
                case 1:
                    if (RandomDoor(out var dp, out var room))
                    {
                        Announce($"Герметизация: закрылись двери комнаты «{Utils.RoomName(dp, true)}».", $"Pressure seal: the doors of \"{Utils.RoomName(dp, false)}\" closed.");
                        ss.RpcCloseDoorsOfType(room);
                    }
                    break;
                case 2:
                    Announce("Марсотрясение! Держитесь.", "Marsquake! Hold on.");
                    foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                        if (p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected) RoleLogic.FlashPlayer(p, 2f);
                    break;
                case 3:
                    if (RandomDoor(out var sp, out _))
                    {
                        samplePos = sp; sampleActive = true; sampleUntil = now + SampleSecs.Value;
                        Announce($"На станции найден образец грунта: «{Utils.RoomName(sp, true)}». Первый мирный, кто дойдёт, получит щит!", $"A soil sample was spotted at \"{Utils.RoomName(sp, false)}\". The first crewmate to reach it gets a shield!");
                    }
                    break;
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Mars.Tick: " + e.Message); }
    }

    static float RandomGap()
    {
        int lo = Math.Min(MinGap.Value, MaxGap.Value), hi = Math.Max(MinGap.Value, MaxGap.Value);
        return rng.Next(lo, hi + 1);
    }
}
