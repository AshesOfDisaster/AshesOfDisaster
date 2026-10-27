using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class Joker
{
    static readonly System.Random rng = new();
    static readonly Dictionary<byte, float> last = new();
    static readonly Dictionary<byte, int> boost = new();

    public static IntOpt Radius, Cooldown, BoostMax;

    static readonly string[] RankRu = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "валет", "дама", "король", "туз" };
    static readonly string[] RankEn = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king", "ace" };

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "Joker", "Джокер", "Joker", "#B388FF", OptTab.Neutral,
            "Нейтрал. Питомец рядом с игроком: вы играете в карты на жизнь. Каждый тянет по карте, у кого старше, тот победил. Проиграл игрок - он умирает, а ты получаешь буст (в следующей игре две карты, берётся лучшая). Проиграл ты - умираешь ты. Для сервера ты настоящий предатель. Перезарядка 30 с, радиус 2,5 м. [Питомец]",
            "Neutral. The pet next to a player: you play cards for life. Each draws one card and the higher card wins. If the player loses he dies and you get a boost (in your next game you draw two cards and the better one counts). If you lose, you die. The server sees you as a real impostor. Cooldown 30 s, radius 2.5 m. [Pet]");
        Radius = Opts.Extra("Joker", new IntOpt("JokerRadius", OptTab.Neutral, 25, 10, 60, 5, c), "Радиус вызова на игру (десятые доли метра)", "Challenge radius (tenths of a meter)");
        Cooldown = Opts.Extra("Joker", new IntOpt("JokerCooldown", OptTab.Neutral, 30, 5, 120, 5, c), "Перезарядка между играми (сек)", "Cooldown between games (sec)");
        BoostMax = Opts.Extra("Joker", new IntOpt("JokerBoostMax", OptTab.Neutral, 1, 1, 5, 1, c), "Сколько бустов можно копить", "Max boosts you can store");
    }

    public static void Reset() { last.Clear(); boost.Clear(); }

    static int Boosts(byte id) => boost.TryGetValue(id, out var v) ? v : 0;
    static int Draw() => rng.Next(2, 15);
    static string Card(int v, bool ru) => (ru ? RankRu : RankEn)[v - 2];

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        int b = Boosts(p.PlayerId);
        return ru ? $"{r.Colored(true)}: питомец рядом с игроком - сыграть в карты на жизнь. Проиграет он - умрёт, и ты получишь буст (две карты на выбор). Проиграешь ты - умрёшь. Бустов: {b}/{BoostMax.Value}."
                  : $"{r.Colored(false)}: pet next to a player - play cards for life. If he loses he dies and you get a boost (two cards, the better counts). If you lose, you die. Boosts: {b}/{BoostMax.Value}.";
    }

    public static string Short(PlayerControl p, bool ru)
    {
        int b = Boosts(p.PlayerId);
        if (last.TryGetValue(p.PlayerId, out var t0) && Time.time - t0 < Cooldown.Value)
        {
            int left = Mathf.CeilToInt(Cooldown.Value - (Time.time - t0));
            return ru ? $"колода тасуется {left} с, бустов {b}/{BoostMax.Value}" : $"shuffling the deck {left} s, boosts {b}/{BoostMax.Value}";
        }
        return ru ? $"питомец: сыграть в карты, бустов {b}/{BoostMax.Value}" : $"pet: play cards, boosts {b}/{BoostMax.Value}";
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        if (last.TryGetValue(p.PlayerId, out var t0) && Time.time - t0 < Cooldown.Value)
        {
            RoleLogic.Hint(p, ru ? $"Колода ещё тасуется: {Mathf.CeilToInt(Cooldown.Value - (Time.time - t0))} с." : "The deck is still being shuffled.");
            return true;
        }
        PlayerControl foe = null;
        float best = Radius.Value / 10f;
        var me = p.GetTruePosition();
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (o == null || o.Data == null || o.Data.IsDead || o.Data.Disconnected || o.PlayerId == p.PlayerId) continue;
            float d = Vector2.Distance(me, o.GetTruePosition());
            if (d <= best) { best = d; foe = o; }
        }
        if (foe == null) { RoleLogic.Hint(p, ru ? "Рядом никого, с кем можно сыграть." : "Nobody close enough to play with."); return true; }
        if (!RoleLogic.CanKill(p, foe)) return true;
        last[p.PlayerId] = Time.time;
        Duel(p, foe);
        return true;
    }

    static void Duel(PlayerControl jk, PlayerControl foe)
    {
        bool ru = Translator.IsRu(jk), ruFoe = Translator.IsRu(foe);
        int charges = Boosts(jk.PlayerId);
        bool boosted = charges > 0;
        int mine = Draw();
        if (boosted)
        {
            mine = Math.Max(mine, Draw());
            boost[jk.PlayerId] = charges - 1;
        }
        int theirs = Draw();
        for (int i = 0; mine == theirs && i < 20; i++) { mine = Draw(); theirs = Draw(); }
        bool win = mine > theirs;
        string foeName = foe.Data.PlayerName, jkName = jk.Data.PlayerName;
        Plugin.Logger.LogInfo($"Joker duel: {jkName} ({mine}) vs {foeName} ({theirs}) boosted={boosted} win={win}");

        string score = ru ? $"Карты: у тебя {Card(mine, true)}, у {foeName} {Card(theirs, true)}." : $"Cards: you {Card(mine, false)}, {foeName} {Card(theirs, false)}.";
        if (win)
        {
            if (RoleLogic.ShieldArmed.Remove(foe.PlayerId))
            {
                RoleLogic.ShieldUsed.Add(foe.PlayerId);
                Utils.SendSecret(foe, ruFoe ? "Джокер сыграл с тобой и выиграл, но сработала защита - ты жив!" : "The Joker beat you at cards, but your shield triggered - you live!");
                RoleLogic.Hint(jk, score + (ru ? " Ты выиграл, но у него сработала защита - он жив, буста нет." : " You won, but his shield triggered - he lives, no boost."));
                return;
            }
            RoleLogic.Hint(jk, score + (ru ? " Ты выиграл!" : " You won!"));
            if (!RoleLogic.Kill(jk, foe, true))
            {
                last.Remove(jk.PlayerId);
                if (boosted) boost[jk.PlayerId] = charges;
                return;
            }
            int now = Math.Min(BoostMax.Value, Boosts(jk.PlayerId) + 1);
            boost[jk.PlayerId] = now;
            RoleLogic.Hint(jk, ru ? $"{foeName} проиграл и погиб. Буст: {now}/{BoostMax.Value} (следующая игра - две карты)." : $"{foeName} lost and died. Boost: {now}/{BoostMax.Value} (next game: two cards).");
        }
        else
        {
            RoleLogic.Hint(jk, score + (ru ? " Ты проиграл - ты погибаешь." : " You lost - you die."));
            Utils.SendSecret(foe, ruFoe ? $"{jkName} (Джокер) сыграл с тобой в карты и проиграл - он погиб." : $"{jkName} (Joker) played cards with you and lost - he died.");
            RoleLogic.Kill(jk, jk, true);
        }
    }
}
