using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class MysticRoles
{
    static readonly System.Random rng = new();
    static readonly Dictionary<string, float> last = new();
    static readonly Dictionary<string, int> used = new();

    public static IntOpt HerbCd, HerbUses, HerbRadius, FortuneCd, FortuneUses;

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "Herbalist", "Знахарь", "Herbalist", "#81C784", OptTab.Mystic,
            "Мистик. Питомец рядом с игроком (до 3 м) даёт ему случайную траву: щит от первого нападения, мигание экрана, телепорт в случайную комнату, щит тебе самому или пустую траву. Использований: 4, перезарядка 40 с. [Pet]",
            "Mystic. The pet next to a player (within 3 m) gives him a random herb: a shield against the first attack, a screen flash, a teleport to a random room, a shield for yourself or an empty herb. Uses: 4, cooldown 40 s. [Pet]");
        HerbRadius = Opts.Extra("Herbalist", new IntOpt("HerbRadius", OptTab.Mystic, 30, 10, 60, 5, c), "Радиус (десятые доли метра)", "Radius (tenths of a meter)");
        HerbCd = Opts.Extra("Herbalist", new IntOpt("HerbCooldown", OptTab.Mystic, 40, 5, 180, 5, c), "Перезарядка (сек)", "Cooldown (sec)");
        HerbUses = Opts.Extra("Herbalist", new IntOpt("HerbUses", OptTab.Mystic, 4, 1, 12, 1, c), "Трав за игру", "Herbs per game");

        Opts.AddRole(c, "FortuneTeller", "Гадалка", "Fortune Teller", "#F48FB1", OptTab.Mystic,
            "Мистик. Питомец тянет карту судьбы: Звезда называет одного точно не предателя, Луна называет двух игроков, и один из них предатель, Колесо говорит, сколько игроков живо, Башня переносит тебя в случайную комнату, а иногда карта пустая. Использований: 4, перезарядка 40 с. [Pet]",
            "Mystic. The pet draws a fate card: Star names one player who is surely not an impostor, Moon names two players and one of them is an impostor, Wheel tells how many players are alive, Tower teleports you to a random room, and sometimes the card is blank. Uses: 4, cooldown 40 s. [Pet]");
        FortuneCd = Opts.Extra("FortuneTeller", new IntOpt("FortuneCooldown", OptTab.Mystic, 40, 5, 180, 5, c), "Перезарядка (сек)", "Cooldown (sec)");
        FortuneUses = Opts.Extra("FortuneTeller", new IntOpt("FortuneUses", OptTab.Mystic, 4, 1, 12, 1, c), "Карт за игру", "Cards per game");
    }

    public static void Reset() { last.Clear(); used.Clear(); }

    static string Key(string role, byte id) => role + ":" + id;
    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId);
    static int Max(string role) => role == "Herbalist" ? HerbUses.Value : FortuneUses.Value;
    static int Cd(string role) => role == "Herbalist" ? HerbCd.Value : FortuneCd.Value;
    static int Left(string role, byte id) => Math.Max(0, Max(role) - (used.TryGetValue(Key(role, id), out var v) ? v : 0));

    public static bool Handles(string id) => id == "Herbalist" || id == "FortuneTeller";

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        int left = Left(r.Id, p.PlayerId);
        if (r.Id == "Herbalist")
            return ru ? $"{r.Colored(true)}: питомец рядом с игроком даёт ему случайную траву (щит, мигание, телепорт, пусто). Трав осталось: {left}."
                      : $"{r.Colored(false)}: the pet next to a player gives him a random herb (shield, flash, teleport, empty). Herbs left: {left}.";
        return ru ? $"{r.Colored(true)}: питомец тянет карту судьбы (Звезда, Луна, Колесо, Башня). Карт осталось: {left}."
                  : $"{r.Colored(false)}: the pet draws a fate card (Star, Moon, Wheel, Tower). Cards left: {left}.";
    }

    public static string Short(PlayerControl p, string role, bool ru)
    {
        int left = Left(role, p.PlayerId);
        if (last.TryGetValue(Key(role, p.PlayerId), out var t0) && Time.time - t0 < Cd(role))
        {
            int w = Mathf.CeilToInt(Cd(role) - (Time.time - t0));
            return ru ? $"перезарядка {w} с, осталось {left}" : $"cooldown {w} s, left {left}";
        }
        return role == "Herbalist" ? (ru ? $"питомец: дать траву, трав {left}" : $"pet: give a herb, herbs {left}")
                                   : (ru ? $"питомец: вытянуть карту, карт {left}" : $"pet: draw a card, cards {left}");
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

    static bool Ready(PlayerControl p, string role, bool ru)
    {
        if (Left(role, p.PlayerId) <= 0) { RoleLogic.Hint(p, ru ? "Использований больше не осталось." : "No uses left."); return false; }
        if (last.TryGetValue(Key(role, p.PlayerId), out var t0) && Time.time - t0 < Cd(role))
        {
            RoleLogic.Hint(p, ru ? $"Рано, ещё {Mathf.CeilToInt(Cd(role) - (Time.time - t0))} с." : $"Too early, {Mathf.CeilToInt(Cd(role) - (Time.time - t0))} s left.");
            return false;
        }
        return true;
    }

    static void Spend(PlayerControl p, string role)
    {
        last[Key(role, p.PlayerId)] = Time.time;
        used[Key(role, p.PlayerId)] = (used.TryGetValue(Key(role, p.PlayerId), out var u) ? u : 0) + 1;
    }

    public static bool Use(PlayerControl p, string role)
    {
        bool ru = Translator.IsRu(p);
        if (!Ready(p, role, ru)) return true;
        if (role == "Herbalist") Herb(p, ru); else Card(p, ru);
        return true;
    }

    static void Herb(PlayerControl p, bool ru)
    {
        PlayerControl t = null;
        float best = HerbRadius.Value / 10f;
        var me = p.GetTruePosition();
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (!Alive(o) || o.PlayerId == p.PlayerId) continue;
            float d = Vector2.Distance(me, o.GetTruePosition());
            if (d <= best) { best = d; t = o; }
        }
        if (t == null) { RoleLogic.Hint(p, ru ? "Рядом никого, кому можно дать траву." : "Nobody close enough for a herb."); return; }
        Spend(p, "Herbalist");
        bool ruT = Translator.IsRu(t);
        bool fat = RoleLogic.Has(t, "Fat");
        switch (rng.Next(5))
        {
            case 0:
                if (fat) { RoleLogic.Hint(p, ru ? "Трава не подошла, ничего не случилось." : "The herb did not fit, nothing happened."); break; }
                RoleLogic.ShieldArmed.Add(t.PlayerId); RoleLogic.ShieldUsed.Remove(t.PlayerId);
                RoleLogic.Hint(p, ru ? $"Трава удачи: {t.Data.PlayerName} получил щит." : $"Herb of luck: {t.Data.PlayerName} got a shield.");
                Utils.SendSecret(t, ruT ? "Знахарь дал тебе траву: у тебя щит от первого нападения!" : "A herbalist gave you a herb: you have a shield against the first attack!");
                break;
            case 1:
                RoleLogic.FlashPlayer(t, 3f);
                RoleLogic.Hint(p, ru ? $"Трава-зевака: у {t.Data.PlayerName} замигал экран." : $"Dizzy herb: {t.Data.PlayerName}'s screen flashes.");
                break;
            case 2:
                if (RandomDoor(out var dest)) { Commands.Tp(t, dest); RoleLogic.Hint(p, ru ? $"Трава-ветер: {t.Data.PlayerName} унесло в другую комнату." : $"Wind herb: {t.Data.PlayerName} was blown to another room."); }
                else RoleLogic.Hint(p, ru ? "Трава засохла." : "The herb withered.");
                break;
            case 3:
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                RoleLogic.Hint(p, ru ? "Трава знахаря: щит достался тебе самому!" : "Herbalist's herb: the shield went to you!");
                break;
            default:
                RoleLogic.Hint(p, ru ? "Трава оказалась пустой." : "The herb was empty.");
                break;
        }
    }

    static void Card(PlayerControl p, bool ru)
    {
        Spend(p, "FortuneTeller");
        var others = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && o.PlayerId != p.PlayerId).ToList();
        var imps = others.Where(IsImp).ToList();
        var safe = others.Where(o => !IsImp(o)).ToList();
        switch (rng.Next(5))
        {
            case 0:
                if (safe.Count == 0) { RoleLogic.Hint(p, ru ? "Звезда погасла, имя не открылось." : "The Star went dark, no name came."); break; }
                RoleLogic.Hint(p, ru ? $"Карта Звезда: {safe[rng.Next(safe.Count)].Data.PlayerName} точно не предатель." : $"Star card: {safe[rng.Next(safe.Count)].Data.PlayerName} is surely not an impostor.");
                break;
            case 1:
                if (imps.Count == 0 || safe.Count == 0) { RoleLogic.Hint(p, ru ? "Луна скрылась за тучей." : "The Moon hid behind a cloud."); break; }
                {
                    var a = imps[rng.Next(imps.Count)]; var b = safe[rng.Next(safe.Count)];
                    string n1 = a.Data.PlayerName, n2 = b.Data.PlayerName;
                    if (rng.Next(2) == 0) { var tmp = n1; n1 = n2; n2 = tmp; }
                    RoleLogic.Hint(p, ru ? $"Карта Луна: один из двоих предатель - {n1} или {n2}." : $"Moon card: one of these two is an impostor - {n1} or {n2}.");
                }
                break;
            case 2:
                RoleLogic.Hint(p, ru ? $"Карта Колесо: сейчас живы {others.Count + 1} игроков, из них предателей {imps.Count}." : $"Wheel card: {others.Count + 1} players are alive, {imps.Count} of them impostors.");
                break;
            case 3:
                if (RandomDoor(out var dest)) { Commands.Tp(p, dest); RoleLogic.Hint(p, ru ? "Карта Башня: ты перенёсся в другую комнату!" : "Tower card: you were moved to another room!"); }
                else RoleLogic.Hint(p, ru ? "Башня рухнула, ничего не вышло." : "The Tower fell, nothing came of it.");
                break;
            default:
                RoleLogic.Hint(p, ru ? "Карта пустая." : "The card is blank.");
                break;
        }
    }
}
