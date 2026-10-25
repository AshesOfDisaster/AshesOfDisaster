using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class Witch
{
    class W
    {
        public bool Placed, Broken;
        public Vector2 Pot, Prev, Dir = Vector2.right;
        public float NextBrew, PotionEnd, NextBoost, NextSee, NextDash;
        public int Potion, Dashes;
    }

    static readonly System.Random rng = new();
    static readonly Dictionary<byte, W> st = new();
    static float nextTick;

    public static IntOpt Cooldown, Duration, BrewRadius, BreakRadius;

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "Witch", "Ведьма", "Witch", "#9575CD", OptTab.Mystic,
            "Мистик. Питомец ставит котёл там, где ты стоишь. Рядом с котлом питомец варит зелье на несколько секунд, и одновременно действует только одно: ускорение (ты бежишь быстрее), зоркость (видишь игроков вокруг и их комнаты) или фаза (питомец перебрасывает тебя вперёд сквозь стену, два раза). Если мирный наступит на котёл, он разобьётся, и варить ты больше не сможешь. [Pet]",
            "Mystic. The pet puts a cauldron where you stand. Next to the cauldron the pet brews a potion for a few seconds, and only one works at a time: haste (you run faster), sight (you see players around you and their rooms) or phase (the pet throws you forward through a wall, twice). If a crewmate steps on the cauldron it breaks and you can't brew any more. [Pet]");
        BrewRadius = Opts.Extra("Witch", new IntOpt("WitchBrewRadius", OptTab.Mystic, 25, 10, 60, 5, c), "Как близко к котлу варить (десятые доли метра)", "How close to the cauldron to brew (tenths of a meter)");
        BreakRadius = Opts.Extra("Witch", new IntOpt("WitchBreakRadius", OptTab.Mystic, 12, 5, 30, 1, c), "Радиус, где котёл разбивается (десятые доли метра)", "Radius where the cauldron breaks (tenths of a meter)");
        Cooldown = Opts.Extra("Witch", new IntOpt("WitchCooldown", OptTab.Mystic, 30, 5, 120, 5, c), "Перезарядка варки (сек)", "Brewing cooldown (sec)");
        Duration = Opts.Extra("Witch", new IntOpt("WitchDuration", OptTab.Mystic, 12, 4, 40, 2, c), "Сколько секунд действует зелье", "Potion duration (sec)");
    }

    public static void Reset() { st.Clear(); }

    public static List<(byte id, Vector2 pos)> Pots() => st.Where(kv => kv.Value.Placed && !kv.Value.Broken).Select(kv => (kv.Key, kv.Value.Pot)).ToList();

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId);

    static W Get(byte id)
    {
        if (!st.TryGetValue(id, out var w)) st[id] = w = new W();
        return w;
    }

    static string PotionName(int k, bool ru) => k == 1 ? (ru ? "ускорение" : "haste") : k == 2 ? (ru ? "зоркость" : "sight") : k == 3 ? (ru ? "фаза" : "phase") : (ru ? "нет" : "none");

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        return ru ? $"{r.Colored(true)}: питомец ставит котёл, рядом с ним варит зелье (ускорение, зоркость или фаза). Наступит мирный на котёл - он разобьётся."
                  : $"{r.Colored(false)}: the pet puts a cauldron and brews a potion next to it (haste, sight or phase). If a crewmate steps on the cauldron it breaks.";
    }

    public static string Short(PlayerControl p, bool ru)
    {
        var w = Get(p.PlayerId);
        if (w.Broken) return ru ? "котёл разбит, варить нельзя" : "cauldron broken, no brewing";
        if (!w.Placed) return ru ? "питомец: поставить котёл" : "pet: put the cauldron";
        float now = Time.time;
        if (w.Potion != 0 && now < w.PotionEnd)
            return ru ? $"зелье: {PotionName(w.Potion, true)}, ещё {Mathf.CeilToInt(w.PotionEnd - now)} с" : $"potion: {PotionName(w.Potion, false)}, {Mathf.CeilToInt(w.PotionEnd - now)} s left";
        if (now < w.NextBrew) return ru ? $"котёл остывает {Mathf.CeilToInt(w.NextBrew - now)} с" : $"cauldron cooling {Mathf.CeilToInt(w.NextBrew - now)} s";
        return ru ? "питомец у котла: сварить зелье" : "pet at the cauldron: brew a potion";
    }

    static bool Walkable(Vector2 pos)
    {
        try
        {
            var rooms = ShipStatus.Instance != null ? ShipStatus.Instance.AllRooms : null;
            if (rooms == null) return false;
            foreach (var room in rooms)
            {
                if (room == null || room.roomArea == null) continue;
                if (room.RoomId.ToString() == "Outside") continue;
                if (room.roomArea.OverlapPoint(pos)) return true;
            }
        }
        catch { }
        return false;
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        var w = Get(p.PlayerId);
        float now = Time.time;
        var me = p.GetTruePosition();
        if (w.Broken) { RoleLogic.Hint(p, ru ? "Котёл разбит, варить больше нельзя." : "The cauldron is broken, no more brewing."); return true; }
        if (!w.Placed)
        {
            w.Placed = true; w.Pot = me; w.Prev = me;
            RoleLogic.Hint(p, ru ? $"Котёл стоит: {Utils.RoomName(me, true)}. Следи, чтобы на него не наступили!" : $"The cauldron is set: {Utils.RoomName(me, false)}. Don't let anyone step on it!");
            return true;
        }
        if (w.Potion == 3 && now < w.PotionEnd && w.Dashes > 0) { Dash(p, w, ru); return true; }
        if (Vector2.Distance(me, w.Pot) > BrewRadius.Value / 10f)
        {
            RoleLogic.Hint(p, ru ? $"Подойди к котлу ({Utils.RoomName(w.Pot, true)}), чтобы сварить зелье." : $"Go to the cauldron ({Utils.RoomName(w.Pot, false)}) to brew a potion.");
            return true;
        }
        if (w.Potion != 0 && now < w.PotionEnd)
        {
            RoleLogic.Hint(p, ru ? $"Одно зелье за раз, ещё {Mathf.CeilToInt(w.PotionEnd - now)} с." : $"One potion at a time, {Mathf.CeilToInt(w.PotionEnd - now)} s left.");
            return true;
        }
        if (now < w.NextBrew)
        {
            RoleLogic.Hint(p, ru ? $"Котёл остывает: ещё {Mathf.CeilToInt(w.NextBrew - now)} с." : $"The cauldron is cooling: {Mathf.CeilToInt(w.NextBrew - now)} s left.");
            return true;
        }
        w.Potion = 1 + rng.Next(3);
        w.PotionEnd = now + Duration.Value;
        w.NextBrew = now + Duration.Value + Cooldown.Value;
        w.Dashes = 2; w.Prev = me;
        string what = w.Potion == 1 ? (ru ? "Зелье ускорения: ты бежишь быстрее!" : "Haste potion: you run faster!")
                    : w.Potion == 2 ? (ru ? "Зелье зоркости: ты видишь игроков вокруг, сквозь стены!" : "Sight potion: you see the players around you, through walls!")
                    : (ru ? "Зелье фазы: погладь питомца, и тебя перебросит вперёд сквозь стену (два раза)!" : "Phase potion: pet your pet and you jump forward through a wall (twice)!");
        RoleLogic.Hint(p, what);
        Plugin.Logger.LogInfo($"Witch: {p.Data.PlayerName} brewed potion {w.Potion}");
        return true;
    }

    static void Dash(PlayerControl p, W w, bool ru)
    {
        float now = Time.time;
        if (now < w.NextDash) return;
        w.NextDash = now + 1f;
        var me = p.GetTruePosition();
        foreach (float dist in new[] { 3.4f, 2.8f, 2.2f, 1.6f })
        {
            var dest = me + w.Dir * dist;
            if (!Walkable(dest)) continue;
            Commands.Tp(p, dest);
            w.Dashes--;
            w.Prev = dest;
            RoleLogic.Hint(p, ru ? $"Фаза: ты прошла сквозь стену. Прыжков осталось: {w.Dashes}." : $"Phase: you passed through a wall. Jumps left: {w.Dashes}.");
            if (w.Dashes <= 0) w.PotionEnd = now;
            return;
        }
        RoleLogic.Hint(p, ru ? "Впереди нет места для прыжка. Повернись к стене и попробуй ещё раз." : "No room for a jump ahead. Face a wall and try again.");
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        if (st.Count == 0 && RoleState.Assigned.Count == 0) return;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null) return;
        try
        {
            float now = Time.time;
            foreach (var kv in RoleState.Assigned)
            {
                if (kv.Value.Id != "Witch") continue;
                var p = Utils.GetPlayer(kv.Key);
                if (!Alive(p)) continue;
                var w = Get(kv.Key);
                bool ru = Translator.IsRu(p);
                var pos = p.GetTruePosition();
                var delta = pos - w.Prev;
                if (delta.magnitude > 0.05f && delta.magnitude < 3f) w.Dir = delta.normalized;

                if (w.Placed && !w.Broken)
                {
                    foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (!Alive(o) || o.PlayerId == p.PlayerId || IsImp(o)) continue;
                        if (Vector2.Distance(o.GetTruePosition(), w.Pot) > BreakRadius.Value / 10f) continue;
                        w.Broken = true; w.Potion = 0;
                        RoleLogic.Hint(p, ru ? $"{o.Data.PlayerName} наступил на твой котёл, и он разбился. Больше варить нельзя." : $"{o.Data.PlayerName} stepped on your cauldron and it broke. No more brewing.");
                        Plugin.Logger.LogInfo($"Witch: cauldron of {p.Data.PlayerName} broken by {o.Data.PlayerName}");
                        break;
                    }
                }

                if (w.Potion != 0 && now >= w.PotionEnd)
                {
                    w.Potion = 0;
                    RoleLogic.Hint(p, ru ? "Зелье выдохлось." : "The potion wore off.");
                }
                else if (w.Potion == 1 && now >= w.NextBoost && delta.magnitude > 0.08f && delta.magnitude < 1.5f)
                {
                    w.NextBoost = now + 0.25f;
                    var dest = pos + w.Dir * 0.5f;
                    if (Walkable(dest)) { Commands.Tp(p, dest); pos = dest; }
                }
                else if (w.Potion == 2 && now >= w.NextSee)
                {
                    w.NextSee = now + 1f;
                    var near = PlayerControl.AllPlayerControls.ToArray()
                        .Where(o => Alive(o) && o.PlayerId != p.PlayerId && Vector2.Distance(o.GetTruePosition(), pos) <= 14f)
                        .OrderBy(o => Vector2.Distance(o.GetTruePosition(), pos)).Take(4)
                        .Select(o => o.Data.PlayerName + " (" + Utils.RoomName(o.GetTruePosition(), ru) + ")").ToList();
                    NameInfo.Notify(p, (ru ? "Вижу: " : "I see: ") + (near.Count == 0 ? (ru ? "никого рядом" : "nobody near") : string.Join(", ", near)), 1.4f);
                }
                w.Prev = pos;
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Witch.Tick: " + e.Message); }
    }
}
