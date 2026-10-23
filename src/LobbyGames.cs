using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class LobbyGames
{
    public static BoolOpt On;
    static readonly System.Random rng = new();
    static readonly Dictionary<byte, int> points = new();
    static readonly Dictionary<byte, int> streak = new();

    static int secret;
    static byte bestId = 255; static int bestRoll; static float rollEnds;
    static int quizAnswer; static string quizText; static float quizEnds;

    public static void Define(ConfigFile c)
    {
        On = new BoolOpt("LobbyGames", OptTab.System, true, c);
        On.NameRu = "Мини-игры в лобби"; On.NameEn = "Lobby mini-games";
        On.GroupRu = "Лобби"; On.GroupEn = "Lobby";
    }

    public static void Reset() { points.Clear(); streak.Clear(); secret = 0; bestId = 255; bestRoll = 0; quizAnswer = 0; quizText = null; }

    static bool Valid(PlayerControl p) => p != null && p.Data != null && !p.Data.Disconnected;
    static string Nm(byte id) { var p = Utils.GetPlayer(id); return p != null && p.Data != null ? p.Data.PlayerName : "?"; }
    static void Add(byte id, int n) => points[id] = (points.TryGetValue(id, out var v) ? v : 0) + n;

    static void Tell(PlayerControl to, string ru, string en) => Utils.SendPrivate(to, Translator.IsRu(to) ? ru : en);
    static void All(string ru, string en)
    {
        foreach (var p in PlayerControl.AllPlayerControls.ToArray()) if (Valid(p)) Tell(p, ru, en);
    }

    public static bool Handle(PlayerControl me, string cmd, string[] a)
    {
        if (On == null || !On.Value) return false;
        switch (cmd)
        {
            case "games":
            case "game":
                if (!Utils.InLobby) { Tell(me, "Мини-игры работают только в лобби.", "Mini-games work in the lobby only."); return true; }
                Tell(me, "Мини-игры: /guess число - угадай число 1-100; /roll - кубик 1-100; /quiz и /a ответ - викторина; /rps к|н|б - камень-ножницы-бумага; /coin о|р - монетка; /slots - слоты; /top - очки.",
                         "Mini-games: /guess N - guess 1-100; /roll - dice 1-100; /quiz and /a answer - quiz; /rps r|p|s - rock-paper-scissors; /coin h|t - coin; /slots - slot machine; /top - points.");
                return true;
            case "top":
                {
                    if (points.Count == 0) { Tell(me, "Очков пока нет. Играй: /games", "No points yet. Play: /games"); return true; }
                    var rows = points.OrderByDescending(x => x.Value).Take(5).Select((x, i) => $"{i + 1}. {Nm(x.Key)} - {x.Value}");
                    Tell(me, "Топ лобби:\n" + string.Join("\n", rows), "Lobby top:\n" + string.Join("\n", rows));
                    return true;
                }
            case "guess": Guess(me, a); return true;
            case "roll": Roll(me); return true;
            case "quiz": Quiz(me); return true;
            case "a": Answer(me, a); return true;
            case "rps": Rps(me, a); return true;
            case "coin": Coin(me, a); return true;
            case "slots": Slots(me); return true;
        }
        return false;
    }

    static bool LobbyOnly(PlayerControl me)
    {
        if (Utils.InLobby) return false;
        Tell(me, "Мини-игры работают только в лобби.", "Mini-games work in the lobby only.");
        return true;
    }

    static void Guess(PlayerControl me, string[] a)
    {
        if (LobbyOnly(me)) return;
        if (secret == 0) secret = rng.Next(1, 101);
        if (a.Length == 0 || !int.TryParse(a[0], out var n)) { Tell(me, "Я загадал число от 1 до 100. Пиши: /guess число", "I picked a number from 1 to 100. Type: /guess N"); return; }
        if (n == secret)
        {
            Add(me.PlayerId, 3);
            All($"{me.Data.PlayerName} угадал число {secret}! +3 очка. Загадано новое.", $"{me.Data.PlayerName} guessed {secret}! +3 points. A new number is set.");
            secret = rng.Next(1, 101);
        }
        else Tell(me, n < secret ? "Больше." : "Меньше.", n < secret ? "Higher." : "Lower.");
    }

    static void Roll(PlayerControl me)
    {
        if (LobbyOnly(me)) return;
        float now = Time.time;
        if (now > rollEnds) { bestId = 255; bestRoll = 0; }
        int r = rng.Next(1, 101);
        if (r > bestRoll)
        {
            bestRoll = r; bestId = me.PlayerId; rollEnds = now + 30f;
            All($"{me.Data.PlayerName} бросил {r} - лучший бросок! Осталось 30 с, чтобы побить.", $"{me.Data.PlayerName} rolled {r} - the best roll! 30 s left to beat it.");
        }
        else Tell(me, $"Ты бросил {r}. Лучший: {bestRoll} ({Nm(bestId)}).", $"You rolled {r}. Best: {bestRoll} ({Nm(bestId)}).");
    }

    static void Quiz(PlayerControl me)
    {
        if (LobbyOnly(me)) return;
        float now = Time.time;
        if (quizText != null && now < quizEnds) { Tell(me, "Вопрос: " + quizText + " Ответ: /a число", "Question: " + quizText + " Answer: /a N"); return; }
        int x = rng.Next(5, 20), y = rng.Next(3, 12), z = rng.Next(1, 30);
        switch (rng.Next(3))
        {
            case 0: quizText = $"{x} * {y} + {z} = ?"; quizAnswer = x * y + z; break;
            case 1: quizText = $"{x * y} - {z} = ?"; quizAnswer = x * y - z; break;
            default: quizText = $"({x} + {y}) * {z % 5 + 2} = ?"; quizAnswer = (x + y) * (z % 5 + 2); break;
        }
        quizEnds = now + 60f;
        All("Викторина! " + quizText + " Отвечай: /a число (60 с)", "Quiz! " + quizText + " Answer: /a N (60 s)");
    }

    static void Answer(PlayerControl me, string[] a)
    {
        if (LobbyOnly(me)) return;
        if (quizText == null || Time.time > quizEnds) { Tell(me, "Сейчас нет вопроса. Запусти: /quiz", "No question now. Start one: /quiz"); return; }
        if (a.Length == 0 || !int.TryParse(a[0], out var n)) { Tell(me, "Пиши: /a число", "Type: /a N"); return; }
        if (n != quizAnswer) { Tell(me, "Неверно.", "Wrong."); return; }
        Add(me.PlayerId, 2); quizText = null;
        All($"{me.Data.PlayerName} ответил верно ({quizAnswer})! +2 очка.", $"{me.Data.PlayerName} was right ({quizAnswer})! +2 points.");
    }

    static void Rps(PlayerControl me, string[] a)
    {
        if (LobbyOnly(me)) return;
        int pick = -1;
        if (a.Length > 0)
        {
            string s = a[0].ToLowerInvariant();
            if (s is "к" or "r" or "rock" or "камень") pick = 0;
            else if (s is "н" or "s" or "scissors" or "ножницы") pick = 1;
            else if (s is "б" or "p" or "paper" or "бумага") pick = 2;
        }
        if (pick < 0) { Tell(me, "Пиши: /rps к (камень), н (ножницы) или б (бумага)", "Type: /rps r (rock), s (scissors) or p (paper)"); return; }
        int bot = rng.Next(3);
        string[] ruN = { "камень", "ножницы", "бумага" }, enN = { "rock", "scissors", "paper" };
        if (pick == bot) { Tell(me, $"Я тоже {ruN[bot]}. Ничья.", $"I also chose {enN[bot]}. Draw."); return; }
        bool win = (pick + 1) % 3 == bot;
        if (win)
        {
            int st = (streak.TryGetValue(me.PlayerId, out var v) ? v : 0) + 1; streak[me.PlayerId] = st;
            int gain = st % 3 == 0 ? 3 : 1; Add(me.PlayerId, gain);
            Tell(me, $"У меня {ruN[bot]}. Ты победил! +{gain} (серия {st}).", $"I chose {enN[bot]}. You win! +{gain} (streak {st}).");
        }
        else { streak[me.PlayerId] = 0; Tell(me, $"У меня {ruN[bot]}. Ты проиграл.", $"I chose {enN[bot]}. You lose."); }
    }

    static void Coin(PlayerControl me, string[] a)
    {
        if (LobbyOnly(me)) return;
        int pick = -1;
        if (a.Length > 0)
        {
            string s = a[0].ToLowerInvariant();
            if (s is "о" or "орёл" or "орел" or "h" or "heads") pick = 0;
            else if (s is "р" or "решка" or "t" or "tails") pick = 1;
        }
        if (pick < 0) { Tell(me, "Пиши: /coin о (орёл) или /coin р (решка)", "Type: /coin h (heads) or /coin t (tails)"); return; }
        int flip = rng.Next(2);
        string[] ruN = { "орёл", "решка" }, enN = { "heads", "tails" };
        if (pick == flip) { Add(me.PlayerId, 1); Tell(me, $"Выпал {ruN[flip]}. Угадал, +1 очко.", $"It is {enN[flip]}. Right, +1 point."); }
        else Tell(me, $"Выпал {ruN[flip]}. Не угадал.", $"It is {enN[flip]}. Wrong.");
    }

    static void Slots(PlayerControl me)
    {
        if (LobbyOnly(me)) return;
        int x = rng.Next(1, 7), y = rng.Next(1, 7), z = rng.Next(1, 7);
        string line = $"[{x}] [{y}] [{z}]";
        if (x == y && y == z)
        {
            Add(me.PlayerId, 5);
            All($"{me.Data.PlayerName} выбил {line} в слотах! +5 очков.", $"{me.Data.PlayerName} hit {line} in slots! +5 points.");
        }
        else if (x == y || y == z || x == z)
        {
            Add(me.PlayerId, 1);
            Tell(me, line + " Пара, +1 очко.", line + " A pair, +1 point.");
        }
        else Tell(me, line + " Мимо.", line + " No luck.");
    }

    static float nextTick;
    public static void Tick()
    {
        if (!Utils.AmHost || On == null || !On.Value || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        if (!Utils.InLobby) return;
        try
        {
            float now = Time.time;
            if (bestId != 255 && now > rollEnds)
            {
                Add(bestId, 2);
                All($"Кубик: победил {Nm(bestId)} с броском {bestRoll}! +2 очка.", $"Dice: {Nm(bestId)} wins with {bestRoll}! +2 points.");
                bestId = 255; bestRoll = 0;
            }
            if (quizText != null && now > quizEnds)
            {
                All($"Время вышло. Ответ был: {quizAnswer}.", $"Time is up. The answer was {quizAnswer}.");
                quizText = null;
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("LobbyGames.Tick: " + e.Message); }
    }
}
