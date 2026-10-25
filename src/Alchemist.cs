using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class Alchemist
{
    static readonly System.Random rng = new();
    static readonly Dictionary<byte, float> last = new();
    static readonly Dictionary<byte, int> used = new();

    public static IntOpt Cooldown, Uses;

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "Alchemist", "Алхимик", "Alchemist", "#CE93D8", OptTab.Mystic,
            "Мистик. Питомец варит зелье со случайным эффектом: щит от первого нападения, радар на предателя, телепорт в случайную комнату, обмен местами со случайным игроком, а иногда зелье выходит пустым. Использований: 4, перезарядка 45 с. [Pet]",
            "Mystic. The pet brews a potion with a random effect: a shield against the first attack, an impostor radar, a teleport to a random room, a swap of places with a random player, and sometimes the potion turns out empty. Uses: 4, cooldown 45 s. [Pet]");
        Cooldown = Opts.Extra("Alchemist", new IntOpt("AlchemistCooldown", OptTab.Mystic, 45, 5, 180, 5, c), "Перезарядка зелья (сек)", "Potion cooldown (sec)");
        Uses = Opts.Extra("Alchemist", new IntOpt("AlchemistUses", OptTab.Mystic, 4, 1, 12, 1, c), "Зелий за игру", "Potions per game");
    }

    public static void Reset() { last.Clear(); used.Clear(); }

    static int Left(byte id) => Math.Max(0, Uses.Value - (used.TryGetValue(id, out var v) ? v : 0));
    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId);

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        return ru ? $"{r.Colored(true)}: питомец варит зелье со случайным эффектом (щит, радар, телепорт, обмен местами). Зелий осталось: {Left(p.PlayerId)}."
                  : $"{r.Colored(false)}: the pet brews a potion with a random effect (shield, radar, teleport, swap). Potions left: {Left(p.PlayerId)}.";
    }

    public static string Short(PlayerControl p, bool ru)
    {
        if (last.TryGetValue(p.PlayerId, out var t0) && Time.time - t0 < Cooldown.Value)
        {
            int left = Mathf.CeilToInt(Cooldown.Value - (Time.time - t0));
            return ru ? $"зелье варится {left} с, зелий {Left(p.PlayerId)}" : $"brewing {left} s, potions {Left(p.PlayerId)}";
        }
        return ru ? $"питомец: сварить зелье, зелий {Left(p.PlayerId)}" : $"pet: brew a potion, potions {Left(p.PlayerId)}";
    }

    static bool RandomDoor(out Vector2 pos)
    {
        pos = default;
        var ss = ShipStatus.Instance; var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) return false;
        var d = doors[rng.Next(doors.Length)];
        if (d == null) return false;
        pos = d.transform.position;
        return true;
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        if (Left(p.PlayerId) <= 0) { RoleLogic.Hint(p, ru ? "Зелий больше не осталось." : "No potions left."); return true; }
        if (last.TryGetValue(p.PlayerId, out var t0) && Time.time - t0 < Cooldown.Value)
        {
            RoleLogic.Hint(p, ru ? $"Зелье ещё варится: {Mathf.CeilToInt(Cooldown.Value - (Time.time - t0))} с." : $"The potion is still brewing: {Mathf.CeilToInt(Cooldown.Value - (Time.time - t0))} s.");
            return true;
        }
        last[p.PlayerId] = Time.time;
        used[p.PlayerId] = (used.TryGetValue(p.PlayerId, out var u) ? u : 0) + 1;
        switch (rng.Next(6))
        {
            case 0:
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                RoleLogic.Hint(p, ru ? "Зелье защиты: щит от первого нападения!" : "Potion of protection: a shield against the first attack!");
                break;
            case 1:
                {
                    float best = 999f; var me = p.GetTruePosition();
                    foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                        if (Alive(o) && o.PlayerId != p.PlayerId && IsImp(o)) best = Math.Min(best, Vector2.Distance(me, o.GetTruePosition()));
                    string w = best < 5f ? (ru ? "ГОРЯЧО" : "HOT") : best < 12f ? (ru ? "тепло" : "warm") : (ru ? "холодно" : "cold");
                    RoleLogic.Hint(p, ru ? $"Зелье чутья: предатель рядом - «{w}»." : $"Potion of sense: the impostor is \"{w}\".");
                    break;
                }
            case 2:
                if (RandomDoor(out var dest)) { Commands.Tp(p, dest); RoleLogic.Hint(p, ru ? "Зелье переноса: ты в случайной комнате!" : "Potion of travel: you are in a random room!"); }
                else RoleLogic.Hint(p, ru ? "Зелье выдохлось." : "The potion went flat.");
                break;
            case 3:
                {
                    var others = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && o.PlayerId != p.PlayerId).ToList();
                    if (others.Count == 0) { RoleLogic.Hint(p, ru ? "Зелье выдохлось." : "The potion went flat."); break; }
                    var t = others[rng.Next(others.Count)];
                    var a = p.GetTruePosition(); var b = t.GetTruePosition();
                    Commands.Tp(p, b); Commands.Tp(t, a);
                    RoleLogic.Hint(p, ru ? $"Зелье обмена: ты поменялся местами с {t.Data.PlayerName}!" : $"Potion of exchange: you swapped places with {t.Data.PlayerName}!");
                    RoleLogic.Hint(t, ru ? "Тебя поменяли местами с другим игроком!" : "You were swapped with another player!");
                    break;
                }
            default:
                RoleLogic.Hint(p, ru ? "Зелье вышло пустым, ничего не произошло." : "The potion came out empty, nothing happened.");
                break;
        }
        Plugin.Logger.LogInfo($"Alchemist: {p.Data.PlayerName} brewed a potion");
        return true;
    }
}
