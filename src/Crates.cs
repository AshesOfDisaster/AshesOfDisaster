using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class Crates
{
    public static BoolOpt On;
    public static IntOpt MinGap, MaxGap, Lifetime;
    static readonly System.Random rng = new();
    static float nextCrate, nextTick, until;
    static Vector2 pos;
    static bool active;

    public static void Define(ConfigFile c)
    {
        On = new BoolOpt("CratesOn", OptTab.Game, false, c);
        MinGap = new IntOpt("CratesMinGap", OptTab.Game, 80, 20, 300, 10, c);
        MaxGap = new IntOpt("CratesMaxGap", OptTab.Game, 160, 40, 600, 10, c);
        Lifetime = new IntOpt("CratesLifetime", OptTab.Game, 40, 15, 120, 5, c);
        On.NameRu = "Ящики снабжения"; On.NameEn = "Supply crates";
        MinGap.NameRu = "Минимум секунд между ящиками"; MinGap.NameEn = "Minimum seconds between crates";
        MaxGap.NameRu = "Максимум секунд между ящиками"; MaxGap.NameEn = "Maximum seconds between crates";
        Lifetime.NameRu = "Сколько секунд лежит ящик"; Lifetime.NameEn = "Seconds a crate stays";
        foreach (Opt o in new Opt[] { On, MinGap, MaxGap, Lifetime }) { o.GroupRu = "Ящики снабжения"; o.GroupEn = "Supply crates"; }
    }

    public static void Reset() { nextCrate = 0f; active = false; }
    public static void OnMeeting() { nextCrate = 0f; active = false; }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId);
    static void Announce(string ru, string en)
    {
        foreach (var p in PlayerControl.AllPlayerControls.ToArray())
            if (Alive(p)) RoleLogic.Hint(p, Translator.IsRu(p) ? ru : en);
    }
    static float Gap() { int lo = Math.Min(MinGap.Value, MaxGap.Value), hi = Math.Max(MinGap.Value, MaxGap.Value); return rng.Next(lo, hi + 1); }

    static bool RandomDoor(out Vector2 p)
    {
        p = default;
        var ss = ShipStatus.Instance; var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) return false;
        var d = doors[rng.Next(doors.Length)];
        if (d == null) return false;
        p = d.transform.position; return true;
    }

    public static void Tick()
    {
        if (!Utils.AmHost || On == null || !On.Value || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null || RoleState.Assigned.Count == 0) return;
        try
        {
            float now = Time.time;
            if (nextCrate <= 0f) { nextCrate = now + Gap(); return; }
            if (active)
            {
                if (now >= until) { active = false; Announce("Ящик снабжения унесло ветром.", "The supply crate is gone."); }
                else
                    foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                        if (Alive(p) && Vector2.Distance(p.GetTruePosition(), pos) < 1.5f) { active = false; Open(p); break; }
            }
            if (!active && now >= nextCrate)
            {
                nextCrate = now + Gap();
                if (RandomDoor(out var sp))
                {
                    pos = sp; active = true; until = now + Lifetime.Value;
                    Announce($"Ящик снабжения: «{Utils.RoomName(sp, true)}». Кто первым дойдёт - откроет!", $"Supply crate at \"{Utils.RoomName(sp, false)}\". The first to reach it opens it!");
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Crates.Tick: " + e.Message); }
    }

    static void Open(PlayerControl p)
    {
        bool ru = Translator.IsRu(p); string name = p.Data.PlayerName;
        Announce($"{name} открыл ящик снабжения.", $"{name} opened the supply crate.");
        switch (rng.Next(6))
        {
            case 0:
                if (IsImp(p)) { RoleLogic.Hint(p, ru ? "Ящик: щит не подошёл предателю, внутри пусто." : "Crate: the shield doesn't fit an impostor, it is empty."); break; }
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                RoleLogic.Hint(p, ru ? "Ящик: щит от первого нападения!" : "Crate: a shield against the first attack!");
                break;
            case 1:
                {
                    float best = 999f; var me = p.GetTruePosition();
                    foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                        if (Alive(o) && o.PlayerId != p.PlayerId && IsImp(o)) best = Math.Min(best, Vector2.Distance(me, o.GetTruePosition()));
                    string w = best < 5f ? (ru ? "ГОРЯЧО" : "HOT") : best < 12f ? (ru ? "тепло" : "warm") : (ru ? "холодно" : "cold");
                    RoleLogic.Hint(p, ru ? $"Ящик: радар показывает «{w}»." : $"Crate: the radar says \"{w}\".");
                    break;
                }
            case 2:
                if (RandomDoor(out var dest)) { Commands.Tp(p, dest); RoleLogic.Hint(p, ru ? "Ящик: телепорт в случайную комнату!" : "Crate: teleported to a random room!"); }
                break;
            case 3:
                {
                    var others = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && o.PlayerId != p.PlayerId).ToList();
                    if (others.Count == 0) break;
                    var t = others[rng.Next(others.Count)];
                    var a = p.GetTruePosition(); var b = t.GetTruePosition();
                    Commands.Tp(p, b); Commands.Tp(t, a);
                    RoleLogic.Hint(p, ru ? $"Ящик: ты поменялся местами с {t.Data.PlayerName}!" : $"Crate: you swapped places with {t.Data.PlayerName}!");
                    RoleLogic.Hint(t, ru ? $"Ящик {name}: тебя поменяли местами!" : $"{name}'s crate: you were swapped!");
                    break;
                }
            case 4:
                RoleLogic.FlashPlayer(p, 3f);
                RoleLogic.Hint(p, ru ? "Ящик: в нём сигнализация, экран мигает!" : "Crate: it had an alarm, your screen flashes!");
                break;
            default:
                RoleLogic.Hint(p, ru ? "Ящик: пусто. Не повезло." : "Crate: empty. Bad luck.");
                break;
        }
    }
}
