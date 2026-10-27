using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static class Roles30
{
    static readonly System.Random rng = new();
    static readonly Dictionary<string, IntOpt> X = new();
    static int G(string k) => X.TryGetValue(k, out var o) ? o.Value : 0;

    public static void Define(ConfigFile c)
    {
        void R(string id, string ru, string en, string color, OptTab tab, string dru, string den) => Opts.AddRole(c, id, ru, en, color, tab, dru, den);
        IntOpt E(string role, string key, OptTab tab, int def, int min, int max, int step, string ru, string en)
        { var o = Opts.Extra(role, new IntOpt(key, tab, def, min, max, step, c), ru, en); X[key] = o; return o; }
        const OptTab C = OptTab.Crewmate, I = OptTab.Impostor, N = OptTab.Neutral;

        R("Sentry", "Часовой", "Sentry", "#FFD54F", C,
            "Мирный. Питомец ставит пост-растяжку. Кто первым пройдёт рядом, тот будет назван тебе, и пост исчезнет. На собрании посты пропадают. [Питомец]",
            "Crewmate. The pet places a tripwire post. The first player to pass near it is named to you and the post vanishes. Posts vanish at a meeting. [Pet]");
        R("Statistician", "Статистик", "Statistician", "#90CAF9", C,
            "Мирный. На собрании напиши /ab и узнаешь, сколько предателей (для сервера) ещё живо. Проверок: 2. [Собрание /ab]",
            "Crewmate. In a meeting type /ab to learn how many impostors (as the server sees them) are still alive. Checks: 2. [Meeting /ab]");
        R("Intercessor", "Заступник", "Intercessor", "#FFAB91", C,
            "Мирный. Питомец рядом с игроком берёт его под защиту. Если на подопечного нападут, нападение отменится, а погибнешь ты. Подопечный один, новый заменяет прежнего. На собрании защита снимается. [Питомец]",
            "Crewmate. The pet next to a player takes him under your protection. If he is attacked the attack is cancelled and you die instead. One ward at a time, a new one replaces the old one. The protection is dropped at a meeting. [Pet]");
        R("Peacemaker", "Миротворец", "Peacemaker", "#F8BBD0", C,
            "Мирный. Питомец создаёт вокруг тебя зону мира на несколько секунд. Пока она действует, предатели рядом не могут никого убить. Использований: 2. [Питомец]",
            "Crewmate. The pet creates a peace zone around you for a few seconds. While it lasts, impostors nearby cannot kill anyone. Uses: 2. [Pet]");

        R("Eyewitness", "Очевидец", "Eyewitness", "#FFE082", C,
            "Мирный. Если рядом с тобой (по умолчанию в радиусе 7 м) кого-то убили, ты узнаёшь имя убийцы. Но с некоторым шансом имя будет ложным. [Сама]",
            "Crewmate. If someone is killed near you (within 7 m by default) you learn the killer's name. But with some chance the name is false. [Passive]");
        R("Forensic", "Криминалист", "Forensic", "#B39DDB", C,
            "Мирный. Питомец рядом с трупом (до 2 м) покажет, сколько секунд назад он погиб. Съеденное тело вернуть нельзя. Осмотров: 3. [Питомец]",
            "Crewmate. The pet next to a body (within 2 m) shows how many seconds ago it died. An eaten body cannot be restored. Exams: 3. [Pet]");

        R("Minelayer", "Минёр", "Minelayer", "#E65100", I,
            "Предатель. Питомец ставит мину. Первый член экипажа, наступивший на неё, погибает (щит спасает). На собрании мины пропадают. Мин одновременно: 3. [Питомец]",
            "Impostor. The pet places a mine. The first crewmate to step on it dies (a shield saves him). Mines vanish at a meeting. Mines at once: 3. [Pet]");
        R("Eclipser", "Затмитель", "Eclipser", "#37474F", I,
            "Предатель. Питомец гасит свет (саботаж электричества), если сейчас нет другого саботажа. Перезарядка 90 с. [Питомец]",
            "Impostor. The pet turns the lights off (electrical sabotage) if no other sabotage is active. Cooldown: 90 s. [Pet]");
        R("Returner", "Возвращенец", "Returner", "#26A69A", I,
            "Предатель. Питомец ставит метку. После каждого твоего убийства тебя возвращает к метке (она остаётся). На собрании метка сбрасывается. [Питомец]",
            "Impostor. The pet sets a mark. After each of your kills you are returned to the mark (it stays). The mark is cleared at a meeting. [Pet]");
        R("Lurer", "Приманщик", "Lurer", "#FF8A80", I,
            "Предатель. Питомец притягивает к тебе ближайшего члена экипажа (в радиусе до 8 м). Использований: 4, перезарядка 45 с. [Питомец]",
            "Impostor. The pet pulls the nearest crewmate (within 8 m) to you. Uses: 4, cooldown 45 s. [Pet]");

        R("Trapper", "Капканщик", "Trapper", "#A1887F", I,
            "Предатель. Питомец ставит капкан. Первый член экипажа, наступивший на него, замирает на несколько секунд, а ты узнаёшь его имя. На собрании капканы пропадают. [Питомец]",
            "Impostor. The pet places a trap. The first crewmate to step on it freezes for a few seconds and you learn his name. Traps vanish at a meeting. [Pet]");

        R("Avenger", "Мститель", "Avenger", "#D32F2F", N,
            "Нейтрал. Если тебя убьют, убийца погибнет через несколько секунд (время в настройках). [Сама]",
            "Neutral. If you are killed, your killer dies a few seconds later (time in the settings). [Passive]");
        R("Hypnotist", "Гипнотизёр", "Hypnotist", "#AB47BC", C,
            "Мирный. Питомец рядом с игроком (до 3 м) обездвиживает его на несколько секунд. Перезарядка 45 с. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) freezes him in place for a few seconds. Cooldown: 45 s. [Pet]");

        R("Clockmaker", "Часовщик", "Clockmaker", "#4E342E", C,
            "Мирный. Питомец отматывает время для тебя: ты возвращаешься туда, где был около 10 секунд назад. Перезарядка 60 с. [Питомец]",
            "Crewmate. The pet rewinds time for you: you return to where you were about 10 seconds ago. Cooldown: 60 s. [Pet]");

        E("Sentry", "SentryCd", C, 15, 5, 90, 5, "Перезарядка поста (сек)", "Post cooldown (sec)");
        E("Sentry", "SentryMax", C, 3, 1, 8, 1, "Постов одновременно", "Posts at a time");
        E("Statistician", "StatUses", C, 2, 1, 10, 1, "Число проверок", "Checks per game");
        E("Intercessor", "IntercessorCd", C, 30, 5, 120, 5, "Перезарядка выбора подопечного (сек)", "Ward choice cooldown (sec)");
        E("Peacemaker", "PeaceCd", C, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Peacemaker", "PeaceUses", C, 2, 1, 10, 1, "Использований за игру", "Uses per game");
        E("Peacemaker", "PeaceSeconds", C, 6, 2, 20, 1, "Длительность зоны (сек)", "Zone duration (sec)");
        E("Peacemaker", "PeaceRadius", C, 5, 2, 12, 1, "Радиус зоны (м)", "Zone radius (m)");

        E("Minelayer", "MineCd", I, 25, 5, 120, 5, "Перезарядка мины (сек)", "Mine cooldown (sec)");
        E("Minelayer", "MineMax", I, 3, 1, 8, 1, "Мин одновременно", "Mines at a time");
        E("Eclipser", "EclipserCd", I, 90, 20, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Returner", "ReturnerCd", I, 10, 0, 120, 5, "Перезарядка смены метки (сек)", "Mark change cooldown (sec)");
        E("Lurer", "LurerCd", I, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Lurer", "LurerUses", I, 4, 1, 15, 1, "Использований за игру", "Uses per game");

        E("Eyewitness", "WitnessRadius", C, 7, 3, 20, 1, "Радиус, в котором он видит убийство (м)", "Radius in which he sees a kill (m)");
        E("Eyewitness", "WitnessTrue", C, 60, 0, 100, 5, "Шанс, что имя убийцы верное (%)", "Chance the killer's name is true (%)");
        E("Forensic", "ForensicUses", C, 3, 1, 10, 1, "Осмотров за игру", "Exams per game");
        E("Trapper", "TrapCd", I, 25, 5, 120, 5, "Перезарядка капкана (сек)", "Trap cooldown (sec)");
        E("Trapper", "TrapMax", I, 2, 1, 6, 1, "Капканов одновременно", "Traps at a time");
        E("Trapper", "TrapSecs", I, 5, 1, 15, 1, "Сколько секунд жертва застыла", "Seconds the victim is frozen");
        E("Clockmaker", "ClockCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Avenger", "AvengerDelay", N, 20, 3, 90, 1, "Через сколько секунд гибнет убийца", "Seconds until the killer dies");
        E("Hypnotist", "HypnoCd", N, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Hypnotist", "HypnoSecs", N, 4, 1, 15, 1, "Сколько секунд игрок неподвижен", "Seconds the player is frozen");
    }

    class Freeze { public byte Id; public Vector2 Pos; public float Until, NextTp; }
    static readonly Dictionary<string, float> lastUse = new();
    static readonly Dictionary<string, int> lastCd = new();
    static readonly Dictionary<string, int> useMax = new();
    static readonly Dictionary<string, int> uses = new();
    static readonly List<(byte victim, Vector2 pos)> bodies = new();
    static readonly List<(byte owner, Vector2 pos)> posts = new();
    static readonly List<(byte owner, Vector2 pos)> mines = new();
    static readonly Dictionary<byte, byte> ward = new();
    static readonly Dictionary<byte, float> peace = new();
    static readonly Dictionary<byte, Vector2> home = new();
    static readonly List<(byte killer, float at)> avenge = new();
    static readonly List<(byte id, Vector2 pos, float at)> tpQueue = new();
    static readonly List<Freeze> freezes = new();
    static readonly List<(byte owner, Vector2 pos)> traps = new();
    static readonly Dictionary<byte, List<(float t, Vector2 pos)>> history = new();
    static readonly Dictionary<byte, float> deathTime = new();
    static float nextTick;

    public static int Defuse(Vector2 pos, float radius)
    {
        int n = mines.RemoveAll(m => Vector2.Distance(m.pos, pos) <= radius);
        n += traps.RemoveAll(t => Vector2.Distance(t.pos, pos) <= radius);
        n += posts.RemoveAll(t => Vector2.Distance(t.pos, pos) <= radius);
        return n;
    }

    public static void FreezePlayer(PlayerControl t, float seconds)
    {
        if (t == null || t.Data == null || freezes.Any(f => f.Id == t.PlayerId)) return;
        freezes.Add(new Freeze { Id = t.PlayerId, Pos = t.GetTruePosition(), Until = Time.time + seconds, NextTp = 0f });
    }

    public static void ResetCooldowns(byte id)
    {
        string suffix = ":" + id;
        foreach (var k in lastUse.Keys.Where(x => x.EndsWith(suffix)).ToList()) lastUse.Remove(k);
    }

    public static List<(string key, byte owner, Vector2 pos, string ru, string en, string color)> Items()
    {
        var l = new List<(string key, byte owner, Vector2 pos, string ru, string en, string color)>();
        foreach (var x in posts) l.Add(("sp" + x.owner + "_" + x.pos.x.ToString("0.0") + "_" + x.pos.y.ToString("0.0"), x.owner, x.pos, "Пост", "Post", "#4FC3F7"));
        foreach (var x in mines) l.Add(("mi" + x.owner + "_" + x.pos.x.ToString("0.0") + "_" + x.pos.y.ToString("0.0"), x.owner, x.pos, "Мина", "Mine", "#FF5252"));
        foreach (var x in traps) l.Add(("tr" + x.owner + "_" + x.pos.x.ToString("0.0") + "_" + x.pos.y.ToString("0.0"), x.owner, x.pos, "Капкан", "Trap", "#FFB74D"));
        foreach (var kv in home) l.Add(("hm" + kv.Key + "_" + kv.Value.x.ToString("0.0") + "_" + kv.Value.y.ToString("0.0"), kv.Key, kv.Value, "Метка", "Mark", "#81C784"));
        return l;
    }

    public static void Reset()
    {
        lastUse.Clear(); lastCd.Clear(); useMax.Clear(); uses.Clear(); bodies.Clear(); posts.Clear(); mines.Clear(); traps.Clear(); history.Clear(); ward.Clear(); peace.Clear(); home.Clear();
        avenge.Clear(); tpQueue.Clear(); freezes.Clear(); deathTime.Clear();
    }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => !RoleLogic.Demoted.Contains(p.PlayerId) && (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId));
    static string Room(Vector2 pos, bool ru) => Utils.RoomName(pos, ru);
    static void Say(PlayerControl p, string s) => RoleLogic.Hint(p, s);
    static void Secret(PlayerControl p, string s) => Utils.SendSecret(p, s);
    static List<PlayerControl> Players() => PlayerControl.AllPlayerControls.ToArray().Where(Alive).ToList();
    static int Used(string k, byte id) => uses.TryGetValue(k + id, out var u) ? u : 0;
    static void Use(string k, byte id) => uses[k + id] = Used(k, id) + 1;
    static string T(bool ru, string a, string b) => ru ? a : b;

    static bool Wait(string key, PlayerControl p, int cd, bool ru)
    {
        string k = key + ":" + p.PlayerId;
        lastCd[k] = cd;
        if (lastUse.TryGetValue(k, out var l) && Time.time - l < cd)
        { Say(p, T(ru, $"Перезарядка: ещё {Mathf.CeilToInt(cd - (Time.time - l))} с.", $"Cooldown: {Mathf.CeilToInt(cd - (Time.time - l))} s left.")); return true; }
        return false;
    }
    static void Stamp(string key, PlayerControl p) => lastUse[key + ":" + p.PlayerId] = Time.time;

    public static int CdLeft(string role, byte id)
    {
        string k = role + ":" + id;
        if (!lastUse.TryGetValue(k, out var t0) || !lastCd.TryGetValue(k, out var c0)) return -1;
        return Mathf.Max(0, Mathf.CeilToInt(c0 - (Time.time - t0)));
    }

    public static int UsesLeft(string role, byte id)
    {
        string k = useMax.ContainsKey(role + id) ? role : role.ToLowerInvariant();
        if (!useMax.TryGetValue(k + id, out var m0)) return -1;
        return Math.Max(0, m0 - Used(k, id));
    }
    static bool NoUses(string key, PlayerControl p, int max, bool ru)
    {
        useMax[key + p.PlayerId] = max;
        if (Used(key, p.PlayerId) < max) return false;
        Say(p, T(ru, "Использования закончились.", "No uses left.")); return true;
    }

    static PlayerControl Nearest(PlayerControl me, float maxDist, Func<PlayerControl, bool> ok)
    {
        PlayerControl best = null; float bd = maxDist; var pos = me.GetTruePosition();
        foreach (var o in Players())
        {
            if (o.PlayerId == me.PlayerId || !ok(o)) continue;
            float d = Vector2.Distance(pos, o.GetTruePosition());
            if (d < bd) { bd = d; best = o; }
        }
        return best;
    }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p); string n = r.Colored(ru); byte id = p.PlayerId;
        int L(string k, int max) => Math.Max(0, max - Used(k, id));
        switch (r.Id)
        {
            case "Sentry": return T(ru, $"{n}: питомец ставит пост; кто пройдёт рядом - будет назван тебе. Постов {posts.Count(x => x.owner == id)}/{G("SentryMax")}.", $"{n}: pet places a post; whoever passes near is named to you. Posts {posts.Count(x => x.owner == id)}/{G("SentryMax")}.");
            case "Statistician": return T(ru, $"{n}: на собрании напиши /ab: узнаешь число живых предателей. Проверок: {L("stat", G("StatUses"))}.", $"{n}: in a meeting type /ab to learn the number of living impostors. Checks left: {L("stat", G("StatUses"))}.");
            case "Intercessor": return T(ru, $"{n}: питомец рядом с игроком берёт его под защиту; за подопечного погибнешь ты. Подопечный: {WardName(id, true)}.", $"{n}: pet next to a player protects him; you die in his place. Ward: {WardName(id, false)}.");
            case "Peacemaker": return T(ru, $"{n}: питомец создаёт зону мира ({G("PeaceSeconds")} с). Осталось: {L("peace", G("PeaceUses"))}.", $"{n}: pet creates a peace zone ({G("PeaceSeconds")} s). Left: {L("peace", G("PeaceUses"))}.");
            case "Minelayer": return T(ru, $"{n}: питомец ставит мину; наступивший член экипажа погибает. Мин {mines.Count(x => x.owner == id)}/{G("MineMax")}.", $"{n}: pet places a mine; a crewmate who steps on it dies. Mines {mines.Count(x => x.owner == id)}/{G("MineMax")}.");
            case "Eclipser": return T(ru, $"{n}: питомец гасит свет.", $"{n}: pet turns the lights off.");
            case "Returner": return T(ru, $"{n}: питомец ставит метку; после убийства ты возвращаешься к ней. Метка: {(home.ContainsKey(id) ? Room(home[id], true) : "нет")}.", $"{n}: pet sets a mark; after a kill you return to it. Mark: {(home.ContainsKey(id) ? Room(home[id], false) : "none")}.");
            case "Lurer": return T(ru, $"{n}: питомец притягивает ближайшего члена экипажа (до 8 м). Осталось: {L("lurer", G("LurerUses"))}.", $"{n}: pet pulls the nearest crewmate (up to 8 m). Left: {L("lurer", G("LurerUses"))}.");
            case "Eyewitness": return T(ru, $"{n}: если рядом (до {G("WitnessRadius")} м) кого-то убьют, ты узнаешь имя убийцы (верное с шансом {G("WitnessTrue")}%).", $"{n}: if someone is killed near you (within {G("WitnessRadius")} m) you learn the killer's name (true with {G("WitnessTrue")}% chance).");
            case "Forensic": return T(ru, $"{n}: питомец у трупа: узнаёшь, когда он погиб. Осмотров: {L("forensic", G("ForensicUses"))}.", $"{n}: the pet next to a body shows when it died. Exams left: {L("forensic", G("ForensicUses"))}.");
            case "Trapper": return T(ru, $"{n}: питомец ставит капкан; наступивший застывает на {G("TrapSecs")} с. Капканов {traps.Count(x => x.owner == id)}/{G("TrapMax")}.", $"{n}: pet places a trap; whoever steps on it freezes for {G("TrapSecs")} s. Traps {traps.Count(x => x.owner == id)}/{G("TrapMax")}.");
            case "Clockmaker": return T(ru, $"{n}: питомец отматывает тебя на ~10 секунд назад.", $"{n}: pet rewinds you ~10 seconds back.");
            case "Avenger": return T(ru, $"{n}: если тебя убьют, убийца погибнет через {G("AvengerDelay")} с.", $"{n}: if you are killed, your killer dies after {G("AvengerDelay")} s.");
            case "Hypnotist": return T(ru, $"{n}: питомец рядом с игроком обездвиживает его на {G("HypnoSecs")} с.", $"{n}: pet next to a player freezes him for {G("HypnoSecs")} s.");
        }
        return null;
    }

    static string WardName(byte id, bool ru)
    {
        if (ward.TryGetValue(id, out var w) && Utils.GetPlayer(w) is PlayerControl wp && wp.Data != null) return wp.Data.PlayerName;
        return ru ? "нет" : "none";
    }

    public static bool Pet(PlayerControl p, RoleDef role)
    {
        bool ru = Translator.IsRu(p); byte id = p.PlayerId; var ss = ShipStatus.Instance;
        switch (role.Id)
        {
            case "Sentry":
                {
                    if (Wait("Sentry", p, G("SentryCd"), ru)) return true;
                    if (posts.Count(x => x.owner == id) >= G("SentryMax")) { Say(p, T(ru, "Достигнут лимит постов.", "Post limit reached.")); return true; }
                    Stamp("Sentry", p); posts.Add((id, p.GetTruePosition()));
                    Say(p, T(ru, $"Пост поставлен: {Room(p.GetTruePosition(), true)}.", "Post placed."));
                    return true;
                }
            case "Intercessor":
                {
                    if (Wait("Intercessor", p, G("IntercessorCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Intercessor", p); ward[id] = t.PlayerId;
                    Say(p, T(ru, $"Ты взял под защиту: {t.Data.PlayerName}. Если на него нападут, погибнешь ты.", $"You protect {t.Data.PlayerName}. If he is attacked you die instead."));
                    return true;
                }
            case "Peacemaker":
                {
                    if (NoUses("peace", p, G("PeaceUses"), ru) || Wait("Peacemaker", p, G("PeaceCd"), ru)) return true;
                    Stamp("Peacemaker", p); Use("peace", id); peace[id] = Time.time + G("PeaceSeconds");
                    Say(p, T(ru, $"Зона мира на {G("PeaceSeconds")} с: рядом (до {G("PeaceRadius")} м) убийства невозможны.", "Peace zone is on."));
                    return true;
                }
            case "Minelayer":
                {
                    if (Wait("Minelayer", p, G("MineCd"), ru)) return true;
                    if (mines.Count(x => x.owner == id) >= G("MineMax")) { Say(p, T(ru, "Достигнут лимит мин.", "Mine limit reached.")); return true; }
                    Stamp("Minelayer", p); mines.Add((id, p.GetTruePosition()));
                    Say(p, T(ru, $"Мина заложена ({mines.Count(x => x.owner == id)}/{G("MineMax")}): {Room(p.GetTruePosition(), true)}.", "Mine placed."));
                    return true;
                }
            case "Trapper":
                {
                    if (Wait("Trapper", p, G("TrapCd"), ru)) return true;
                    if (traps.Count(x => x.owner == id) >= G("TrapMax")) { Say(p, T(ru, "Достигнут лимит капканов.", "Trap limit reached.")); return true; }
                    Stamp("Trapper", p); traps.Add((id, p.GetTruePosition()));
                    Say(p, T(ru, $"Капкан поставлен ({traps.Count(x => x.owner == id)}/{G("TrapMax")}): {Room(p.GetTruePosition(), true)}.", "Trap placed."));
                    return true;
                }
            case "Clockmaker":
                {
                    if (Wait("Clockmaker", p, G("ClockCd"), ru)) return true;
                    if (!history.TryGetValue(id, out var h) || h.Count == 0) { Say(p, T(ru, "Прошлое ещё не записано.", "No past recorded yet.")); return true; }
                    float want = Time.time - 10f; var target = h[0];
                    foreach (var e in h) if (e.t <= want) target = e;
                    Stamp("Clockmaker", p); Commands.Tp(p, target.pos); h.Clear();
                    Say(p, T(ru, $"Время отмотано: {Room(target.pos, true)}.", "Time rewound."));
                    return true;
                }
            case "Forensic":
                {
                    if (NoUses("forensic", p, G("ForensicUses"), ru)) return true;
                    var me = p.GetTruePosition(); (byte victim, Vector2 pos) best = default; float bd = 2f; bool found = false;
                    foreach (var b in bodies) { float d = Vector2.Distance(me, b.pos); if (d < bd) { bd = d; best = b; found = true; } }
                    if (!found) { Say(p, T(ru, "Рядом нет трупа.", "No body nearby.")); return true; }
                    Use("forensic", id);
                    int ago = deathTime.TryGetValue(best.victim, out var dt) ? Mathf.RoundToInt(Time.time - dt) : 0;
                    string extra;
                    if (RoleLogic.Eaten.Contains(best.victim)) extra = T(ru, "От тела почти ничего не осталось.", "Almost nothing is left of the body.");
                    else extra = "";
                    Say(p, T(ru, $"Смерть наступила {ago} с назад. {extra}", $"Death occurred {ago} s ago. {extra}"));
                    return true;
                }
            case "Eclipser": return Sabotage(p, "Eclipser", G("EclipserCd"), SystemTypes.Electrical, T(ru, "Свет погас.", "Lights are off."), ru);
            case "Returner":
                {
                    if (Wait("Returner", p, G("ReturnerCd"), ru)) return true;
                    Stamp("Returner", p); home[id] = p.GetTruePosition();
                    Say(p, T(ru, $"Метка поставлена: {Room(p.GetTruePosition(), true)}. После убийства вернёшься сюда.", "Mark set."));
                    return true;
                }
            case "Lurer":
                {
                    if (NoUses("lurer", p, G("LurerUses"), ru) || Wait("Lurer", p, G("LurerCd"), ru)) return true;
                    var t = Nearest(p, 8f, o => !IsImp(o));
                    if (t == null) { Say(p, T(ru, "Рядом нет членов экипажа.", "No crewmates nearby.")); return true; }
                    Stamp("Lurer", p); Use("lurer", id);
                    Commands.Tp(t, p.GetTruePosition() + new Vector2(0.6f, 0f));
                    Say(p, T(ru, $"{t.Data.PlayerName} притянут к тебе.", $"{t.Data.PlayerName} is pulled to you."));
                    return true;
                }
            case "Hypnotist":
                {
                    if (Wait("Hypnotist", p, G("HypnoCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => freezes.All(f => f.Id != o.PlayerId));
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Hypnotist", p);
                    freezes.Add(new Freeze { Id = t.PlayerId, Pos = t.GetTruePosition(), Until = Time.time + G("HypnoSecs"), NextTp = 0f });
                    Say(p, T(ru, $"{t.Data.PlayerName} застыл на {G("HypnoSecs")} с.", $"{t.Data.PlayerName} is frozen for {G("HypnoSecs")} s."));
                    return true;
                }
        }
        return false;
    }

    static bool Sabotage(PlayerControl p, string key, int cd, SystemTypes sys, string done, bool ru)
    {
        if (Wait(key, p, cd, ru)) return true;
        var ss = ShipStatus.Instance; if (ss == null) return true;
        try
        {
            foreach (var st in new[] { SystemTypes.Electrical, SystemTypes.Comms, SystemTypes.Reactor, SystemTypes.Laboratory, SystemTypes.LifeSupp, SystemTypes.HeliSabotage })
                if (ss.Systems != null && ss.Systems.ContainsKey(st))
                {
                    var act = ss.Systems[st].TryCast<IActivatable>();
                    if (act != null && act.IsActive) { Say(p, T(ru, "Сейчас уже идёт саботаж.", "A sabotage is already active.")); return true; }
                }
            Stamp(key, p);
            ss.RpcUpdateSystem(SystemTypes.Sabotage, (byte)sys);
            Say(p, done);
        }
        catch (Exception e) { Plugin.Logger.LogWarning(key + ": " + e.Message); }
        return true;
    }

    public static bool UseMeeting(PlayerControl p, string[] args)
    {
        if (!RoleState.Assigned.TryGetValue(p.PlayerId, out var role)) return false;
        bool ru = Translator.IsRu(p); byte id = p.PlayerId;
        if (role.Id == "Statistician")
        {
            if (p.Data.IsDead) { Secret(p, T(ru, "Мёртвые не считают.", "The dead can't count.")); return true; }
            if (MeetingHud.Instance == null) { Secret(p, T(ru, "Работает только на собрании.", "Meeting only.")); return true; }
            if (Used("stat", id) >= G("StatUses")) { Secret(p, T(ru, "Проверки закончились.", "No checks left.")); return true; }
            Use("stat", id);
            int n = Players().Count(IsImp);
            Secret(p, T(ru, $"Живых предателей (для сервера): {n}. Проверок осталось: {Math.Max(0, G("StatUses") - Used("stat", id))}.", $"Living impostors (as the server sees them): {n}. Checks left: {Math.Max(0, G("StatUses") - Used("stat", id))}."));
            return true;
        }
        return false;
    }

    public static bool BlockKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            if (killer == null || target == null || killer.Data == null || target.Data == null) return false;
            var kp = killer.GetTruePosition();
            foreach (var kv in peace.ToList())
            {
                if (Time.time > kv.Value) { peace.Remove(kv.Key); continue; }
                var pm = Utils.GetPlayer(kv.Key);
                if (!Alive(pm)) continue;
                if (Vector2.Distance(kp, pm.GetTruePosition()) <= G("PeaceRadius"))
                {
                    RoleLogic.Kill(killer, target, false);
                    Say(killer, T(Translator.IsRu(killer), "Здесь действует зона мира - убить не получилось.", "A peace zone is active here - the kill failed."));
                    return true;
                }
            }
            foreach (var kv in ward.ToList())
            {
                if (kv.Value != target.PlayerId) continue;
                var ic = Utils.GetPlayer(kv.Key);
                ward.Remove(kv.Key);
                if (!Alive(ic) || ic.PlayerId == killer.PlayerId) continue;
                Say(ic, T(Translator.IsRu(ic), $"Ты заслонил {target.Data.PlayerName} и погиб вместо него.", $"You shielded {target.Data.PlayerName} and died in his place."));
                Say(target, T(Translator.IsRu(target), "Кто-то заслонил тебя от смерти.", "Someone took the blow for you."));
                RoleLogic.Kill(killer, ic, true);
                Plugin.Logger.LogInfo($"Intercessor {ic.Data.PlayerName} died for {target.Data.PlayerName}");
                return true;
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.BlockKill: " + e.Message); }
        return false;
    }

    public static void OnKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            if (target == null || target.Data == null) return;
            bool self = killer == null || killer.PlayerId == target.PlayerId;
            var tpos = target.GetTruePosition();
            bodies.Add((target.PlayerId, tpos));
            deathTime[target.PlayerId] = Time.time;
            if (self) return;
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Eyewitness" || kv.Key == target.PlayerId || kv.Key == killer.PlayerId) continue;
                var w = Utils.GetPlayer(kv.Key);
                if (!Alive(w) || Vector2.Distance(w.GetTruePosition(), tpos) > G("WitnessRadius")) continue;
                var named = killer;
                if (rng.Next(100) >= G("WitnessTrue"))
                {
                    var pool = Players().Where(o => o.PlayerId != w.PlayerId && o.PlayerId != killer.PlayerId && o.PlayerId != target.PlayerId).ToList();
                    if (pool.Count > 0) named = pool[rng.Next(pool.Count)];
                }
                Say(w, T(Translator.IsRu(w), $"Ты видел убийство! Убийца - {named.Data.PlayerName}.", $"You saw a kill! The killer is {named.Data.PlayerName}."));
            }
            if (RoleLogic.Has(target, "Avenger")) { avenge.Add((killer.PlayerId, Time.time + G("AvengerDelay"))); Say(killer, T(Translator.IsRu(killer), "Ты чувствуешь, что за эту смерть придётся расплатиться…", "You feel you will pay for this death...")); }
            if (RoleLogic.Has(killer, "Returner") && home.TryGetValue(killer.PlayerId, out var h))
                tpQueue.Add((killer.PlayerId, h, Time.time + 0.8f));
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.OnKill: " + e.Message); }
    }

    public static void OnMeeting()
    {
        posts.Clear(); mines.Clear(); ward.Clear(); peace.Clear(); home.Clear(); tpQueue.Clear(); freezes.Clear(); bodies.Clear();
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null || RoleState.Assigned.Count == 0) return;
        try
        {
            float now = Time.time;
            var players = Players();

            for (int i = tpQueue.Count - 1; i >= 0; i--)
                if (now >= tpQueue[i].at) { var t = Utils.GetPlayer(tpQueue[i].id); if (Alive(t)) Commands.Tp(t, tpQueue[i].pos); tpQueue.RemoveAt(i); }

            for (int i = avenge.Count - 1; i >= 0; i--)
                if (now >= avenge[i].at)
                {
                    var k = Utils.GetPlayer(avenge[i].killer); avenge.RemoveAt(i);
                    if (Alive(k)) { Say(k, T(Translator.IsRu(k), "Месть настигла тебя.", "Revenge has caught up with you.")); RoleLogic.Kill(k, k, true); }
                }

            for (int i = mines.Count - 1; i >= 0; i--)
            {
                var m = mines[i]; var owner = Utils.GetPlayer(m.owner);
                if (!Alive(owner)) { mines.RemoveAt(i); continue; }
                var victim = players.FirstOrDefault(o => o.PlayerId != m.owner && !IsImp(o) && Vector2.Distance(o.GetTruePosition(), m.pos) < 0.9f);
                if (victim == null) continue;
                mines.RemoveAt(i);
                if (RoleLogic.ShieldArmed.Remove(victim.PlayerId))
                {
                    RoleLogic.ShieldUsed.Add(victim.PlayerId);
                    Secret(victim, T(Translator.IsRu(victim), "Защита сработала: ты наступил на мину!", "Shield triggered: you stepped on a mine!"));
                    Say(owner, T(Translator.IsRu(owner), $"{victim.Data.PlayerName} наступил на мину, но щит его спас.", $"{victim.Data.PlayerName} stepped on a mine but a shield saved him."));
                }
                else
                {
                    Say(owner, T(Translator.IsRu(owner), $"Мина сработала: {victim.Data.PlayerName}.", $"Mine triggered: {victim.Data.PlayerName}."));
                    RoleLogic.Kill(owner, victim, true);
                }
            }

            for (int i = traps.Count - 1; i >= 0; i--)
            {
                var tr = traps[i]; var owner = Utils.GetPlayer(tr.owner);
                if (!Alive(owner)) { traps.RemoveAt(i); continue; }
                var v = players.FirstOrDefault(o => o.PlayerId != tr.owner && !IsImp(o) && freezes.All(f => f.Id != o.PlayerId) && Vector2.Distance(o.GetTruePosition(), tr.pos) < 0.9f);
                if (v == null) continue;
                traps.RemoveAt(i);
                freezes.Add(new Freeze { Id = v.PlayerId, Pos = v.GetTruePosition(), Until = now + G("TrapSecs"), NextTp = 0f });
                Say(owner, T(Translator.IsRu(owner), $"Капкан сработал: {v.Data.PlayerName} ({Room(tr.pos, true)}).", $"Trap triggered: {v.Data.PlayerName}."));
                Say(v, T(Translator.IsRu(v), "Ты попал в капкан!", "You are caught in a trap!"));
            }

            for (int i = posts.Count - 1; i >= 0; i--)
            {
                var po = posts[i]; var owner = Utils.GetPlayer(po.owner);
                if (!Alive(owner)) { posts.RemoveAt(i); continue; }
                var who = players.FirstOrDefault(o => o.PlayerId != po.owner && !Roles75.Stealth(o.PlayerId) && Vector2.Distance(o.GetTruePosition(), po.pos) < 1.2f);
                if (who == null) continue;
                posts.RemoveAt(i);
                Say(owner, T(Translator.IsRu(owner), $"Пост сработал: {who.Data.PlayerName} прошёл у поста ({Room(po.pos, true)}).", $"Post triggered: {who.Data.PlayerName} passed ({Room(po.pos, false)})."));
            }

            for (int i = freezes.Count - 1; i >= 0; i--)
            {
                var f = freezes[i]; var t = Utils.GetPlayer(f.Id);
                if (now >= f.Until || !Alive(t)) { freezes.RemoveAt(i); continue; }
                if (now >= f.NextTp && Vector2.Distance(t.GetTruePosition(), f.Pos) > 0.15f) { Commands.Tp(t, f.Pos); f.NextTp = now + 0.4f; }
            }

            foreach (var kv in RoleState.Assigned.ToList())
            {
                var p = Utils.GetPlayer(kv.Key);
                if (!Alive(p)) continue;
                byte id = p.PlayerId; var pos = p.GetTruePosition(); bool ru = Translator.IsRu(p);
                switch (kv.Value.Id)
                {
                    case "Clockmaker":
                        {
                            if (!history.TryGetValue(id, out var hl)) history[id] = hl = new List<(float t, Vector2 pos)>();
                            hl.Add((now, pos));
                            while (hl.Count > 0 && now - hl[0].t > 15f) hl.RemoveAt(0);
                            break;
                        }
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.Tick: " + e.Message); }
    }
}
