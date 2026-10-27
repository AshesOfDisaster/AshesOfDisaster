using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

// 28 новых ролей (10 нейтралов, 5 мистиков, 10 предателей, 3 мирных) и 5 ролей призраков.
public static class Extra33
{
    class Spec
    {
        public string Id, Ru, En, Color, HintRu, HintEn, DescRu, DescEn;
        public OptTab Tab;
        public int Cd, Uses;
        public Func<PlayerControl, bool, bool> Fn;
    }

    class Timed { public string Kind; public byte Owner, Target; public Vector2 Pos; public float At; }
    class Flower { public byte Owner; public Vector2 Pos; public bool Wilted; }
    class Hide { public byte Id; public float At; public bool Added; }

    static readonly System.Random rng = new();
    static readonly List<Spec> Specs = new();
    static readonly Dictionary<string, Spec> ById = new();
    static readonly Dictionary<string, IntOpt> X = new();
    static readonly Dictionary<string, float> last = new();
    static readonly Dictionary<string, int> used = new();
    static readonly List<Timed> timers = new();
    static readonly List<(byte id, Vector2 pos)> bodies = new();
    static readonly HashSet<byte> eaten = new();
    static readonly HashSet<byte> charge = new();
    static readonly Dictionary<byte, float> berserk = new();
    static readonly Dictionary<byte, float> berserkNext = new();
    static readonly Dictionary<byte, (byte owner, float at)> infect = new();
    static readonly Dictionary<byte, float> ambush = new();
    static readonly List<Flower> flowers = new();
    static readonly List<Hide> hides = new();
    class Rune { public Vector2 Pos; public HashSet<byte> Seen = new(); }
    static readonly Dictionary<byte, Rune> runes = new();
    static readonly Dictionary<byte, List<byte>> alibi = new();
    static readonly Dictionary<byte, float> aloneSince = new(), closeSince = new(), nextWarn = new();
    static readonly HashSet<byte> hermitDone = new();
    static readonly Dictionary<byte, Queue<(float t, Vector2 pos)>> hist = new();
    static readonly Dictionary<byte, int> moves = new();
    static readonly Dictionary<byte, string> lastRoom = new();
    static readonly Dictionary<byte, HashSet<string>> visited = new();
    static readonly HashSet<byte> rounderDone = new();
    static readonly Dictionary<byte, string> hearth = new();
    static readonly Dictionary<byte, Vector2> hearthPos = new();
    static readonly Dictionary<byte, float> hearthIn = new();
    static readonly Dictionary<byte, (byte id, float until)> passenger = new();
    static readonly Dictionary<int, List<(byte pid, float t)>> ventLog = new();
    static readonly Dictionary<int, byte> sealedVents = new(), mineVents = new();
    static readonly List<(byte owner, Vector2 pos)> snares = new();
    static readonly List<(string kind, byte owner, byte pid, int vent, float at)> vacts = new();
    static readonly List<(byte pid, int vent, float at, int tries)> bq = new();
    static readonly Dictionary<byte, List<string>> deathNear = new();
    static readonly List<(byte owner, Vector2 pos)> pits = new();
    static readonly Dictionary<byte, byte> contract = new();
    static readonly Dictionary<byte, float> botNext = new();

    // Выкинуть игрока из люка так, чтобы не сломать ему состояние: ждём, пока закончится анимация входа (~1 с), потом проверяем, что вышел.
    public static void QueueBoot(byte pid, int vent) => bq.Add((pid, vent, Time.time + 1.0f, 0));

    static void ProcessBoots(float now)
    {
        foreach (var b in bq.Where(x => now >= x.at).ToList())
        {
            bq.Remove(b);
            var bp = Utils.GetPlayer(b.pid);
            if (!Alive(bp) || bp.MyPhysics == null) continue;
            try
            {
                if (b.tries == 0 && !bp.inVent && now < b.at + 1.5f) { bq.Add((b.pid, b.vent, now + 0.25f, 0)); continue; }
                if (b.tries > 0 && !bp.inVent) continue;
                bp.MyPhysics.RpcBootFromVent(b.vent);
                if (b.tries < 2) bq.Add((b.pid, b.vent, now + 1.2f, b.tries + 1));
            }
            catch (Exception e) { Plugin.Logger.LogWarning("Extra33.Boot: " + e.Message); }
        }
    }

    static float roundStart, nextSample;

    static bool NearVent(PlayerControl p, float max, out int id)
    {
        id = -1;
        var vents = ShipStatus.Instance != null ? ShipStatus.Instance.AllVents : null;
        if (vents == null) return false;
        var me = p.GetTruePosition(); float bd = max;
        foreach (var v in vents)
        {
            if (v == null) continue;
            float d = Vector2.Distance(me, (Vector2)v.transform.position);
            if (d < bd) { bd = d; id = v.Id; }
        }
        return id >= 0;
    }

    static string VentRoom(int vid, bool ru)
    {
        var vents = ShipStatus.Instance != null ? ShipStatus.Instance.AllVents : null;
        if (vents != null)
            foreach (var v in vents)
                if (v != null && v.Id == vid) return Utils.RoomName((Vector2)v.transform.position, ru);
        return "?";
    }

    static Vector2 VentPos(int vid)
    {
        var vents = ShipStatus.Instance != null ? ShipStatus.Instance.AllVents : null;
        if (vents != null)
            foreach (var v in vents)
                if (v != null && v.Id == vid) return (Vector2)v.transform.position;
        return default;
    }

    static string Hk(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o).ToString();

    // Всё, что поставили роли: для значков на карте (Markers). Видит только владелец.
    public static List<(string key, byte owner, Vector2 pos, string ru, string en, string color)> Items()
    {
        var l = new List<(string, byte, Vector2, string, string, string)>();
        foreach (var sn in snares) l.Add(("sn" + Hk(sn), sn.owner, sn.pos, "Силок", "Snare", "#A1887F"));
        foreach (var kv in mineVents) l.Add(("mn" + kv.Key, kv.Value, VentPos(kv.Key), "Мина", "Mine", "#FF5252"));
        foreach (var kv in sealedVents) l.Add(("sl" + kv.Key, kv.Value, VentPos(kv.Key), "Печать", "Seal", "#90A4AE"));
        foreach (var kv in hearth) if (hearthPos.TryGetValue(kv.Key, out var hp)) l.Add(("hr" + kv.Key, kv.Key, hp, "Очаг", "Hearth", "#FF9800"));
        foreach (var kv in runes) l.Add(("rn" + kv.Key, kv.Key, kv.Value.Pos, "Руна", "Rune", "#B39DDB"));
        foreach (var t in timers) if (t.Kind == "bomb") l.Add(("bm" + Hk(t), t.Owner, t.Pos, "Бомба", "Bomb", "#FF1744"));
        foreach (var f in flowers) if (!f.Wilted) l.Add(("fl" + Hk(f), f.Owner, f.Pos, "Цветок", "Flower", "#F48FB1"));
        foreach (var w in RoleLogic.Webs) l.Add(("wb" + Hk(w), w.Owner, w.Pos, "Паутина", "Web", "#E0E0E0"));
        foreach (var c in RoleLogic.Clouds) l.Add(("cd" + Hk(c), c.Owner, c.Pos, c.Storm ? "Гроза" : "Туча", c.Storm ? "Storm" : "Cloud", "#78909C"));
        foreach (var pt in Witch.Pots()) l.Add(("wp" + pt.id, pt.id, pt.pos, "Котёл", "Cauldron", "#9575CD"));
        foreach (var pit in pits) l.Add(("pt" + pit.owner + "_" + pit.pos.x.ToString("0.0") + "_" + pit.pos.y.ToString("0.0"), pit.owner, pit.pos, "Яма", "Pit", "#8D6E63"));
        return l;
    }

    // вызывается патчем CoEnterVent
    public static void OnVent(PlayerControl p, int vid)
    {
        try
        {
            if (!Utils.AmHost || p == null || p.Data == null) return;
            if (!ventLog.TryGetValue(vid, out var lg)) ventLog[vid] = lg = new List<(byte, float)>();
            lg.Add((p.PlayerId, Time.time));
            if (sealedVents.TryGetValue(vid, out var so) && so != p.PlayerId)
            {
                QueueBoot(p.PlayerId, vid);
                var sp = Utils.GetPlayer(so);
                if (Alive(sp)) H(sp, T(Translator.IsRu(sp), $"В запечатанный люк лез {Nm(p)}: вышвырнут.", $"{Nm(p)} tried the sealed vent: thrown out."));
            }
            else if (mineVents.TryGetValue(vid, out var mo) && mo != p.PlayerId)
            {
                var mp = Utils.GetPlayer(mo);
                if (Alive(mp) && !IsImp(p))
                {
                    mineVents.Remove(vid);
                    QueueBoot(p.PlayerId, vid);
                    vacts.Add(("mine", mo, p.PlayerId, vid, Time.time + 1.4f));
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Extra33.OnVent: " + e.Message); }
    }

    static float nextTick;

    static int G(string k) => X.TryGetValue(k, out var o) ? o.Value : 0;
    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => p != null && p.Data != null && !RoleLogic.Demoted.Contains(p.PlayerId) && (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId));
    static string T(bool ru, string r, string e) => ru ? r : e;
    static void H(PlayerControl p, string s) => RoleLogic.Hint(p, s);
    static string Nm(PlayerControl p) => p.Data.PlayerName;
    static string Key(string id, byte pid) => id + ":" + pid;

    static List<PlayerControl> Others(PlayerControl me)
        => PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && o.PlayerId != me.PlayerId).ToList();

    static PlayerControl Nearest(PlayerControl me, float max, Func<PlayerControl, bool> ok)
    {
        PlayerControl best = null; float bd = max;
        var pos = me.GetTruePosition();
        foreach (var o in Others(me))
        {
            if (!ok(o)) continue;
            float d = Vector2.Distance(pos, o.GetTruePosition());
            if (d < bd) { bd = d; best = o; }
        }
        return best;
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

    // true - действие состоялось (убийство или щит поймал удар), false - убить было нельзя
    static bool Strike(PlayerControl by, PlayerControl t)
    {
        if (!Alive(by) || !Alive(t) || by.PlayerId == t.PlayerId) return false;
        if (!RoleLogic.CanKill(by, t)) return false;
        bool ru = Translator.IsRu(by);
        if (RoleLogic.ShieldArmed.Remove(t.PlayerId))
        {
            RoleLogic.ShieldUsed.Add(t.PlayerId);
            Utils.SendSecret(t, Translator.IsRu(t) ? "Защита сработала!" : "Shield triggered!");
            H(by, T(ru, $"У {Nm(t)} сработала защита.", $"{Nm(t)} had a shield."));
            return true;
        }
        if (!RoleLogic.Kill(by, t, true)) return false;
        H(by, T(ru, $"Убит: {Nm(t)}.", $"Killed: {Nm(t)}."));
        return true;
    }

    public static bool Handles(string id) => ById.ContainsKey(id);

    static IntOpt E(string role, string key, int def, int min, int max, int step, string ru, string en)
    {
        var o = Opts.Extra(role, new IntOpt(key, ById[role].Tab, def, min, max, step, ConfigRef), ru, en);
        X[key] = o; return o;
    }

    static ConfigFile ConfigRef;

    static void S(string id, string ru, string en, string color, OptTab tab, int cd, int uses,
        string hintRu, string hintEn, string dRu, string dEn, Func<PlayerControl, bool, bool> fn)
    {
        var s = new Spec { Id = id, Ru = ru, En = en, Color = color, Tab = tab, Cd = cd, Uses = uses, HintRu = hintRu, HintEn = hintEn, DescRu = dRu, DescEn = dEn, Fn = fn };
        Specs.Add(s); ById[id] = s;
        Opts.AddRole(ConfigRef, id, ru, en, color, tab, dRu, dEn);
        if (cd > 0) E(id, id + "Cd", cd, 5, 240, 5, "Перезарядка (сек)", "Cooldown (sec)");
        if (uses > 0) E(id, id + "Uses", uses, 1, 15, 1, "Использований за игру", "Uses per game");
    }

    public static void Define(ConfigFile c)
    {
        ConfigRef = c;
        const OptTab N = OptTab.Neutral, M = OptTab.Mystic, C = OptTab.Crewmate, I = OptTab.Impostor;

        // ---------------- Нейтралы (настоящие убийцы, убивают питомцем) ----------------
        S("Reaper", "Жнец", "Reaper", "#8E24AA", N, 40, 0, "питомец: убить рядом с трупом", "pet: kill next to a body",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если рядом лежит труп (до 4 м). [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only when a body lies nearby (within 4 m). [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                if (!bodies.Any(b => !eaten.Contains(b.id) && Vector2.Distance(b.pos, me) <= 4f)) { H(p, T(ru, "Рядом нет трупа (нужно ближе 4 м).", "No body nearby (within 4 m).")); return false; }
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                return Strike(p, t);
            });
        S("Poisoner", "Отравитель", "Poisoner", "#43A047", N, 30, 0, "питомец: отравить игрока", "pet: poison a player",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец отравляет ближайшего игрока (до 2 м), и через несколько секунд он умирает. Собрание отменяет отравление. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet poisons the nearest player (within 2 m) and he dies after a few seconds. A meeting cancels the poison. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => !timers.Any(x => x.Kind == "poison" && x.Target == o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет, кого отравить (до 2 м).", "Nobody to poison within 2 m.")); return false; }
                timers.Add(new Timed { Kind = "poison", Owner = p.PlayerId, Target = t.PlayerId, At = Time.time + G("PoisonDelay") });
                H(p, T(ru, $"{Nm(t)} отравлен, умрёт через {G("PoisonDelay")} с.", $"{Nm(t)} is poisoned, dies in {G("PoisonDelay")} s."));
                return true;
            });
        E("Poisoner", "PoisonDelay", 25, 5, 90, 5, "Через сколько секунд яд убивает", "Seconds until the poison kills");
        S("Hunter", "Охотник", "Hunter", "#6D4C41", N, 35, 0, "питомец: выстрел по ближайшему", "pet: shoot the nearest",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец стреляет в ближайшего игрока (до 6 м). Шанс попасть задаётся в настройках, иначе промах. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet shoots the nearest player (within 6 m). The hit chance is in the settings, otherwise it is a miss. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, G("HunterRadius") / 10f, o => true);
                if (t == null) { H(p, T(ru, "В радиусе выстрела никого нет.", "Nobody in shooting range.")); return false; }
                if (rng.Next(100) >= G("HunterChance")) { H(p, T(ru, "Промах!", "A miss!")); return true; }
                return Strike(p, t);
            });
        E("Hunter", "HunterChance", 60, 10, 100, 10, "Шанс попасть (%)", "Hit chance (%)");
        E("Hunter", "HunterRadius", 60, 20, 100, 5, "Дальность выстрела (десятые доли метра)", "Shot range (tenths of a meter)");
        S("Executioner", "Палач", "Executioner", "#B71C1C", N, 30, 0, "питомец: казнь без свидетелей", "pet: execute with no witnesses",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец казнит ближайшего игрока (до 1,8 м), но только когда в радиусе 6 м нет других живых игроков. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet executes the nearest player (within 1.8 m), but only when no other living players are within 6 m. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 1.8f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 1,8 м.", "Nobody within 1.8 m.")); return false; }
                var me = p.GetTruePosition();
                if (Others(p).Any(o => o.PlayerId != t.PlayerId && Vector2.Distance(me, o.GetTruePosition()) <= 6f))
                { H(p, T(ru, "Рядом свидетели (до 6 м).", "Witnesses nearby (within 6 m).")); return false; }
                return Strike(p, t);
            });
        S("Berserker", "Берсерк", "Berserker", "#E64A19", N, 60, 0, "питомец: ярость на несколько секунд", "pet: frenzy for a few seconds",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец вводит тебя в ярость: пока она длится, любой игрок ближе 1,3 м к тебе погибает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet sends you into a frenzy: while it lasts any player within 1.3 m of you dies. [Pet]",
            (p, ru) =>
            {
                berserk[p.PlayerId] = Time.time + G("BerserkTime");
                H(p, T(ru, $"Ярость! {G("BerserkTime")} с.", $"Frenzy! {G("BerserkTime")} s."));
                return true;
            });
        E("Berserker", "BerserkTime", 12, 3, 40, 1, "Длительность ярости (сек)", "Frenzy duration (sec)");
        S("Ghoul", "Упырь", "Ghoul", "#558B2F", N, 15, 0, "питомец: съесть труп, потом убить", "pet: eat a body, then kill",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец рядом с трупом (до 2,5 м) заряжает тебя. Следующий питомец рядом с игроком (до 2 м) убивает его. Заряд один. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet next to a body (within 2.5 m) charges you. The next pet next to a player (within 2 m) kills him. One charge at a time. [Pet]",
            (p, ru) =>
            {
                if (charge.Contains(p.PlayerId))
                {
                    var t = Nearest(p, 2f, o => true);
                    if (t == null) { H(p, T(ru, "Заряд есть, но рядом никого (до 2 м).", "You are charged, but nobody is within 2 m.")); return false; }
                    if (!Strike(p, t)) return false;
                    charge.Remove(p.PlayerId);
                    return true;
                }
                var me = p.GetTruePosition();
                foreach (var b in bodies)
                    if (!eaten.Contains(b.id) && Vector2.Distance(b.pos, me) <= 2.5f)
                    {
                        eaten.Add(b.id); charge.Add(p.PlayerId);
                        H(p, T(ru, "Ты съел труп и заряжен: питомец рядом с игроком убьёт его.", "You ate the body and are charged: the pet next to a player kills him."));
                        return true;
                    }
                H(p, T(ru, "Рядом нет трупа (до 2,5 м).", "No body within 2.5 m."));
                return false;
            });
        S("Detonator", "Подрывник", "Detonator", "#FF6F00", N, 45, 0, "питомец: заложить бомбу", "pet: plant a bomb",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец закладывает бомбу там, где ты стоишь. Через несколько секунд все игроки в радиусе 2,5 м от неё погибают (тебя это не касается). Бомбы пропадают на собрании. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet plants a bomb where you stand. After a few seconds every player within 2.5 m of it dies (not you). Bombs vanish at a meeting. [Pet]",
            (p, ru) =>
            {
                var pos = p.GetTruePosition();
                timers.Add(new Timed { Kind = "bomb", Owner = p.PlayerId, Pos = pos, At = Time.time + G("DetonatorDelay") });
                H(p, T(ru, $"Бомба заложена: {Utils.RoomName(pos, true)}. Взрыв через {G("DetonatorDelay")} с.", $"Bomb planted in {Utils.RoomName(pos, false)}. Boom in {G("DetonatorDelay")} s."));
                return true;
            });
        E("Detonator", "DetonatorDelay", 10, 3, 40, 1, "Через сколько секунд взрыв", "Seconds until the blast");
        S("Sleeper", "Сонник", "Sleeper", "#5E35B1", N, 40, 0, "питомец: усыпить и убить", "pet: put to sleep and kill",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец усыпляет ближайшего игрока (до 2,5 м): он замирает на 3 секунды и затем погибает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet puts the nearest player (within 2.5 m) to sleep: he freezes for 3 seconds and then dies. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !timers.Any(x => x.Kind == "sleep" && x.Target == o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом никого (до 2,5 м).", "Nobody within 2.5 m.")); return false; }
                Roles30.FreezePlayer(t, 3f);
                timers.Add(new Timed { Kind = "sleep", Owner = p.PlayerId, Target = t.PlayerId, At = Time.time + 3.2f });
                H(p, T(ru, $"{Nm(t)} засыпает.", $"{Nm(t)} falls asleep."));
                return true;
            });
        S("Stormcaller", "Грозовик", "Stormcaller", "#0288D1", N, 50, 0, "питомец: молния в твоей комнате", "pet: lightning in your room",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец бьёт молнией случайного игрока, который находится с тобой в одной комнате. В коридоре не работает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet strikes a random player who is in the same room as you with lightning. Does not work in a hallway. [Pet]",
            (p, ru) =>
            {
                string room = Utils.RoomName(p.GetTruePosition(), false);
                if (room == "hallway") { H(p, T(ru, "В коридоре молния не бьёт, зайди в комнату.", "Lightning does not strike in a hallway, enter a room.")); return false; }
                var list = Others(p).Where(o => Utils.RoomName(o.GetTruePosition(), false) == room).ToList();
                if (list.Count == 0) { H(p, T(ru, "В комнате больше никого нет.", "Nobody else in the room.")); return false; }
                return Strike(p, list[rng.Next(list.Count)]);
            });
        S("Plague", "Чумной", "Plague", "#9E9D24", N, 45, 0, "питомец: заразить игрока", "pet: infect a player",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец заражает ближайшего игрока (до 2 м). Заражённый заражает всех, кто подойдёт ближе 2 м, и когда срок выйдет, все заражённые погибают. Собрание лечит всех. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet infects the nearest player (within 2 m). An infected player infects everyone who comes within 2 m, and when the time runs out every infected player dies. A meeting cures everyone. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => !infect.ContainsKey(o.PlayerId));
                if (t == null) { H(p, T(ru, "Некого заразить (до 2 м).", "Nobody to infect within 2 m.")); return false; }
                infect[t.PlayerId] = (p.PlayerId, Time.time + G("PlagueLife"));
                H(p, T(ru, $"{Nm(t)} заражён, срок {G("PlagueLife")} с.", $"{Nm(t)} is infected, {G("PlagueLife")} s left."));
                return true;
            });
        E("Plague", "PlagueLife", 20, 8, 90, 2, "Срок болезни (сек)", "Disease time (sec)");
        foreach (var id in new[] { "Reaper", "Poisoner", "Hunter", "Executioner", "Berserker", "Ghoul", "Detonator", "Sleeper", "Stormcaller", "Plague", "Hearthkeeper", "Charon", "Pouncer" })
            RoleLogic.RealImpNeutrals.Add(id);

        // ---------------- Мистики ----------------
        S("Mirrorer", "Зеркальщик", "Mirrorer", "#B0BEC5", M, 40, 4, "питомец: поменять двух игроков", "pet: swap two players",
            "Мистик. Питомец меняет местами двух ближайших к тебе игроков (в радиусе 8 м). Использований: 4, перезарядка 40 с. [Питомец]",
            "Mystic. The pet swaps the places of the two players nearest to you (within 8 m). Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var two = Others(p).Where(o => Vector2.Distance(me, o.GetTruePosition()) <= 8f).OrderBy(o => Vector2.Distance(me, o.GetTruePosition())).Take(2).ToList();
                if (two.Count < 2) { H(p, T(ru, "Рядом нужно хотя бы двое (до 8 м).", "Two players are needed nearby (within 8 m).")); return false; }
                var a = two[0].GetTruePosition(); var b = two[1].GetTruePosition();
                Commands.Tp(two[0], b); Commands.Tp(two[1], a);
                H(p, T(ru, $"Зеркало поменяло {Nm(two[0])} и {Nm(two[1])}.", $"The mirror swapped {Nm(two[0])} and {Nm(two[1])}."));
                return true;
            });
        S("Dreamer", "Сновидец", "Dreamer", "#7986CB", M, 35, 5, "питомец: сон о людных комнатах", "pet: dream about crowded rooms",
            "Мистик. Питомец показывает сон: две комнаты, где сейчас больше всего живых игроков. Использований: 5, перезарядка 35 с. [Питомец]",
            "Mystic. The pet shows a dream: the two rooms with the most living players right now. Uses: 5, cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var all = PlayerControl.AllPlayerControls.ToArray().Where(Alive).ToList();
                var rooms = all.Select(o => Utils.RoomName(o.GetTruePosition(), ru)).Where(n => n != T(ru, "коридор", "hallway"))
                    .GroupBy(n => n).OrderByDescending(g => g.Count()).Take(2).ToList();
                if (rooms.Count == 0) { H(p, T(ru, "Сон пустой: все в коридорах.", "An empty dream: everyone is in hallways.")); return true; }
                H(p, T(ru, "Сон: людно в комнатах: ", "Dream: crowded rooms: ") + string.Join(", ", rooms.Select(g => $"{g.Key} ({g.Count()})")) + ".");
                return true;
            });
        S("Astrologer", "Астролог", "Astrologer", "#FFD54F", M, 45, 3, "питомец: звёзды о заданиях", "pet: the stars about tasks",
            "Мистик. Питомец читает звёзды: сколько процентов заданий выполнено мирными и кто сделал больше всех. Использований: 3, перезарядка 45 с. [Питомец]",
            "Mystic. The pet reads the stars: what percent of tasks the crewmates have done and who did the most. Uses: 3, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                int total = 0, done = 0, best = -1; string star = null;
                foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (o == null || o.Data == null || o.Data.Disconnected || IsImp(o) || o.Data.Tasks == null) continue;
                    int d = 0;
                    foreach (var t in o.Data.Tasks) { total++; if (t.Complete) { done++; d++; } }
                    if (Alive(o) && d > best) { best = d; star = Nm(o); }
                }
                int pct = total == 0 ? 0 : done * 100 / total;
                H(p, T(ru, $"Звёзды: выполнено {pct}% заданий. " + (star != null ? $"Больше всех сделал: {star}." : ""),
                           $"The stars: {pct}% of tasks are done. " + (star != null ? $"Did the most: {star}." : "")));
                return true;
            });
        S("Banshee", "Банши", "Banshee", "#CE93D8", M, 50, 4, "питомец: крик, экраны мигают", "pet: scream, screens flash",
            "Мистик. Питомец издаёт крик: у всех игроков в радиусе 6 м от тебя на 2,5 секунды мигает экран. Использований: 4, перезарядка 50 с. [Питомец]",
            "Mystic. The pet lets out a scream: the screens of all players within 6 m of you flash for 2.5 seconds. Uses: 4, cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = Others(p).Where(o => Vector2.Distance(me, o.GetTruePosition()) <= 6f).ToList();
                if (list.Count == 0) { H(p, T(ru, "Кричать некому: рядом никого.", "Nobody to scream at.")); return false; }
                foreach (var o in list) RoleLogic.FlashPlayer(o, 2.5f);
                H(p, T(ru, $"Крик услышали: {list.Count}.", $"Heard the scream: {list.Count}."));
                return true;
            });
        S("Wanderer", "Странник", "Wanderer", "#4DB6AC", M, 45, 4, "питомец: перенестись к случайному игроку", "pet: jump to a random player",
            "Мистик. Питомец переносит тебя к случайному живому игроку. Использований: 4, перезарядка 45 с. [Питомец]",
            "Mystic. The pet moves you to a random living player. Uses: 4, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var list = Others(p);
                if (list.Count == 0) { H(p, T(ru, "Больше никого нет.", "Nobody else is alive.")); return false; }
                var t = list[rng.Next(list.Count)];
                Commands.Tp(p, t.GetTruePosition());
                H(p, T(ru, $"Ты вышел из тумана рядом с {Nm(t)}.", $"You stepped out of the mist next to {Nm(t)}."));
                return true;
            });

        // ---------------- Мирные ----------------
        S("Teacher", "Учитель", "Teacher", "#FFCC80", C, 45, 3, "питомец: помочь с заданием", "pet: help with a task",
            "Мирный. Питомец рядом с игроком (до 3 м), у которого остались задания, выполняет за него одно задание. Использований: 3, перезарядка 45 с. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) who still has tasks completes one task for him. Uses: 3, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !IsImp(o) && o.Data.Tasks != null && o.Data.Tasks.ToArray().Any(x => !x.Complete));
                if (t == null) { H(p, T(ru, "Рядом нет, кому помочь с заданием (до 3 м).", "Nobody nearby needs help with a task (within 3 m).")); return false; }
                var task = t.Data.Tasks.ToArray().First(x => !x.Complete);
                try { t.RpcCompleteTask(task.Id); }
                catch (Exception e) { Plugin.Logger.LogWarning("Teacher: " + e.Message); H(p, T(ru, "Не получилось.", "It failed.")); return false; }
                H(p, T(ru, $"Ты помог {Nm(t)}: задание выполнено.", $"You helped {Nm(t)}: a task is done."));
                Utils.SendSecret(t, Translator.IsRu(t) ? "Учитель выполнил за тебя одно задание!" : "A teacher completed one of your tasks!");
                return true;
            });
        S("Hider", "Затаившийся", "Hider", "#90A4AE", C, 60, 2, "питомец: затаиться под щитом", "pet: hide under a shield",
            "Мирный. Питомец прячет тебя на 8 секунд: ты замираешь, но на это время у тебя щит от нападения. Использований: 2, перезарядка 60 с. [Питомец]",
            "Crewmate. The pet hides you for 8 seconds: you freeze, but you have a shield against an attack for that time. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                bool had = RoleLogic.ShieldArmed.Contains(p.PlayerId);
                if (!had) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
                Roles30.FreezePlayer(p, 8f);
                hides.Add(new Hide { Id = p.PlayerId, At = Time.time + 8f, Added = !had });
                H(p, T(ru, "Ты затаился на 8 секунд под щитом.", "You hide for 8 seconds under a shield."));
                return true;
            });
        S("Gardener", "Садовник", "Gardener", "#66BB6A", C, 20, 0, "питомец: посадить цветок", "pet: plant a flower",
            "Мирный. Питомец сажает цветок. Если рядом с цветком (до 4 м) кого-то убьют, он завянет, и на собрании ты узнаешь комнаты увядших цветков. Цветки обновляются каждое собрание. [Питомец]",
            "Crewmate. The pet plants a flower. If someone is killed near a flower (within 4 m) it wilts, and at the meeting you learn the rooms of the wilted flowers. Flowers reset every meeting. [Pet]",
            (p, ru) =>
            {
                int mine = flowers.Count(f => f.Owner == p.PlayerId);
                if (mine >= G("GardenerMax")) { H(p, T(ru, "Цветков уже максимум.", "Flower limit reached.")); return false; }
                var pos = p.GetTruePosition();
                if (flowers.Any(f => f.Owner == p.PlayerId && Vector2.Distance(f.Pos, pos) < 1.5f)) { H(p, T(ru, "Здесь уже растёт цветок.", "A flower is already here.")); return false; }
                flowers.Add(new Flower { Owner = p.PlayerId, Pos = pos });
                H(p, T(ru, $"Цветок посажен: {Utils.RoomName(pos, true)} ({mine + 1}/{G("GardenerMax")}).", $"Flower planted in {Utils.RoomName(pos, false)} ({mine + 1}/{G("GardenerMax")})."));
                return true;
            });
        E("Gardener", "GardenerMax", 3, 1, 8, 1, "Цветков одновременно", "Flowers at a time");

        // ---------------- Предатели ----------------
        S("Shocker", "Шокер", "Shocker", "#FFEB3B", I, 60, 0, "питомец: оглушить всех рядом", "pet: stun everyone nearby",
            "Предатель. Питомец оглушает всех не-предателей в радиусе 3,5 м: они замирают на 2,5 секунды. [Питомец]",
            "Impostor. The pet stuns all non-impostors within 3.5 m: they freeze for 2.5 seconds. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = Others(p).Where(o => !IsImp(o) && Vector2.Distance(me, o.GetTruePosition()) <= 3.5f).ToList();
                if (list.Count == 0) { H(p, T(ru, "Некого оглушать.", "Nobody to stun.")); return false; }
                foreach (var o in list) Roles30.FreezePlayer(o, 2.5f);
                H(p, T(ru, $"Оглушено: {list.Count}.", $"Stunned: {list.Count}."));
                return true;
            });
        S("Blinder", "Ослепитель", "Blinder", "#FFF59D", I, 50, 0, "питомец: ослепить всех рядом", "pet: blind everyone nearby",
            "Предатель. Питомец ослепляет всех не-предателей в радиусе 6 м: на 3 секунды у них мигает экран. [Питомец]",
            "Impostor. The pet blinds all non-impostors within 6 m: their screens flash for 3 seconds. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = Others(p).Where(o => !IsImp(o) && Vector2.Distance(me, o.GetTruePosition()) <= 6f).ToList();
                if (list.Count == 0) { H(p, T(ru, "Некого слепить.", "Nobody to blind.")); return false; }
                foreach (var o in list) RoleLogic.FlashPlayer(o, 3f);
                H(p, T(ru, $"Ослеплено: {list.Count}.", $"Blinded: {list.Count}."));
                return true;
            });
        S("Thief", "Вор", "Thief", "#78909C", I, 30, 3, "питомец: украсть щит", "pet: steal a shield",
            "Предатель. Питомец рядом с игроком (до 2 м), у которого есть щит, крадёт щит себе. Использований: 3, перезарядка 30 с. [Питомец]",
            "Impostor. The pet next to a player (within 2 m) who has a shield steals the shield for you. Uses: 3, cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => RoleLogic.ShieldArmed.Contains(o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет игрока со щитом (до 2 м).", "Nobody with a shield within 2 m.")); return false; }
                RoleLogic.ShieldArmed.Remove(t.PlayerId);
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                H(p, T(ru, $"Ты украл щит у {Nm(t)}.", $"You stole the shield of {Nm(t)}."));
                return true;
            });
        S("Pointer", "Наводчик", "Pointer", "#EF5350", I, 40, 6, "питомец: найти самого одинокого", "pet: find the loneliest",
            "Предатель. Питомец показывает самого одинокого мирного: у кого дальше всего от других живых игроков. Называется имя и комната. Использований: 6, перезарядка 40 с. [Питомец]",
            "Impostor. The pet shows the loneliest crewmate: the one farthest from other living players. His name and room are shown. Uses: 6, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                PlayerControl best = null; float bd = -1f;
                foreach (var o in Others(p))
                {
                    if (IsImp(o)) continue;
                    float d = 999f; var op = o.GetTruePosition();
                    foreach (var q in Others(o)) { if (q.PlayerId == p.PlayerId) continue; d = Mathf.Min(d, Vector2.Distance(op, q.GetTruePosition())); }
                    if (d > bd) { bd = d; best = o; }
                }
                if (best == null) { H(p, T(ru, "Мирных не осталось.", "No crewmates left.")); return false; }
                H(p, T(ru, $"Самый одинокий: {Nm(best)}, {Utils.RoomName(best.GetTruePosition(), true)}.", $"The loneliest: {Nm(best)}, {Utils.RoomName(best.GetTruePosition(), false)}."));
                return true;
            });
        S("Leech", "Пиявка", "Leech", "#AD1457", I, 0, 0, "после убийства получаешь щит", "after a kill you get a shield",
            "Предатель. После каждого твоего убийства ты получаешь щит от первого нападения. Работает сама. [Сама]",
            "Impostor. After each of your kills you get a shield against the first attack. Works by itself. [Passive]",
            null);
        S("Alarmist", "Паникёр", "Alarmist", "#FF7043", I, 90, 2, "питомец: тревога у всех на экране", "pet: alarm flashes on every screen",
            "Предатель. Питомец поднимает тревогу: у всех не-предателей на 1,5 секунды мигает экран. Использований: 2, перезарядка 90 с. [Питомец]",
            "Impostor. The pet raises an alarm: the screens of all non-impostors flash for 1.5 seconds. Uses: 2, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                var list = Others(p).Where(o => !IsImp(o)).ToList();
                if (list.Count == 0) { H(p, T(ru, "Некому слать тревогу.", "Nobody to alarm.")); return false; }
                foreach (var o in list) RoleLogic.FlashPlayer(o, 1.5f);
                H(p, T(ru, "Тревога поднята.", "The alarm is raised."));
                return true;
            });
        S("Jumper", "Прыгун", "Jumper", "#26C6DA", I, 35, 0, "питомец: прыжок к случайной двери", "pet: jump to a random door",
            "Предатель. Питомец переносит тебя к случайной двери на карте. Перезарядка 35 с. [Питомец]",
            "Impostor. The pet moves you to a random door on the map. Cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                if (!RandomDoor(out var pos)) { H(p, T(ru, "Прыгнуть не получилось.", "The jump failed.")); return false; }
                Commands.Tp(p, pos);
                H(p, T(ru, $"Прыжок: {Utils.RoomName(pos, true)}.", $"Jump: {Utils.RoomName(pos, false)}."));
                return true;
            });
        S("Rumormonger", "Сплетник", "Rumormonger", "#BA68C8", I, 30, 4, "питомец: пустить ложный слух", "pet: spread a false rumor",
            "Предатель. Питомец шепчет случайному мирному слух, что другой мирный и есть предатель. Слух ложный. Использований: 4, перезарядка 30 с. [Питомец]",
            "Impostor. The pet whispers to a random crewmate a rumor that another crewmate is the impostor. The rumor is false. Uses: 4, cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var crew = Others(p).Where(o => !IsImp(o)).ToList();
                if (crew.Count < 2) { H(p, T(ru, "Нужно хотя бы двое мирных.", "At least two crewmates are needed.")); return false; }
                var listener = crew[rng.Next(crew.Count)];
                var rest = crew.Where(o => o.PlayerId != listener.PlayerId).ToList();
                var named = rest[rng.Next(rest.Count)];
                Utils.SendSecret(listener, Translator.IsRu(listener) ? $"Ходит слух: {Nm(named)} - предатель." : $"A rumor goes around: {Nm(named)} is the impostor.");
                H(p, T(ru, $"Слух о {Nm(named)} дошёл до {Nm(listener)}.", $"The rumor about {Nm(named)} reached {Nm(listener)}."));
                return true;
            });
        S("Carousel", "Карусель", "Carousel", "#F06292", I, 55, 3, "питомец: покрутить игроков рядом", "pet: spin the players nearby",
            "Предатель. Питомец двигает по кругу до четырёх ближайших не-предателей (до 7 м): каждый оказывается на месте соседа. Использований: 3, перезарядка 55 с. [Питомец]",
            "Impostor. The pet rotates up to four nearest non-impostors (within 7 m) in a circle: each ends up where his neighbor stood. Uses: 3, cooldown 55 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = Others(p).Where(o => !IsImp(o) && Vector2.Distance(me, o.GetTruePosition()) <= 7f).OrderBy(o => Vector2.Distance(me, o.GetTruePosition())).Take(4).ToList();
                if (list.Count < 2) { H(p, T(ru, "Нужно хотя бы двое рядом (до 7 м).", "At least two players are needed nearby (within 7 m).")); return false; }
                var pos = list.Select(o => o.GetTruePosition()).ToList();
                for (int i = 0; i < list.Count; i++) Commands.Tp(list[i], pos[(i + 1) % list.Count]);
                H(p, T(ru, $"Карусель закружила {list.Count} игроков.", $"The carousel spun {list.Count} players."));
                return true;
            });
        S("Ambusher", "Засадник", "Ambusher", "#455A64", I, 50, 0, "питомец: засада на 5 секунд", "pet: ambush for 5 seconds",
            "Предатель. Питомец включает засаду на 5 секунд: первый не-предатель, подошедший ближе 2,2 м, замирает на 4 секунды. [Питомец]",
            "Impostor. The pet sets an ambush for 5 seconds: the first non-impostor who comes within 2.2 m freezes for 4 seconds. [Pet]",
            (p, ru) =>
            {
                ambush[p.PlayerId] = Time.time + 5f;
                H(p, T(ru, "Засада на 5 секунд.", "Ambush for 5 seconds."));
                return true;
            });



        // ---------------- Мистики: расследование и чутьё ----------------
        S("Necromancer", "Некромант", "Necromancer", "#7E57C2", M, 60, 2, "питомец у трупа: кто был рядом", "pet at a body: who was nearby",
            "Мистик. Питомец рядом с трупом (до 2,5 м) вызывает дух погибшего. Он называет одного игрока, который стоял рядом в момент смерти и точно не убийца, либо говорит, что рядом не было никого, кроме убийцы. Трупы сбрасываются на собрании. Использований: 2, перезарядка 60 с. [Питомец]",
            "Mystic. The pet next to a body (within 2.5 m) calls the spirit of the dead. It names one player who stood nearby when the victim died and is surely not the killer, or says that nobody but the killer was near. Bodies reset at a meeting. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                (byte id, Vector2 pos) best = default; float bd = 2.5f; bool found = false;
                foreach (var b in bodies)
                {
                    float d = Vector2.Distance(b.pos, me);
                    if (d <= bd) { bd = d; best = b; found = true; }
                }
                if (!found) { H(p, T(ru, "Рядом нет трупа (до 2,5 м).", "No body within 2.5 m.")); return false; }
                var victim = Utils.GetPlayer(best.id);
                string vn = victim != null && victim.Data != null ? Nm(victim) : "?";
                if (!alibi.TryGetValue(best.id, out var list) || list.Count == 0)
                { H(p, T(ru, $"Дух {vn}: рядом не было никого, кроме убийцы.", $"The spirit of {vn}: nobody but the killer was near.")); return true; }
                var w = Utils.GetPlayer(list[rng.Next(list.Count)]);
                H(p, T(ru, $"Дух {vn}: рядом стоял {(w != null ? Nm(w) : "?")}, и это не убийца.", $"The spirit of {vn}: {(w != null ? Nm(w) : "?")} stood nearby and is not the killer."));
                return true;
            });
        S("Hermit", "Отшельник", "Hermit", "#A1887F", M, 0, 0, "в одиночестве получаешь щит", "solitude gives you a shield",
            "Мистик. Если рядом (в радиусе 8 м) нет ни одного живого игрока 25 секунд подряд, ты получаешь щит от первого нападения. Один раз между собраниями. Работает сама. [Сама]",
            "Mystic. If there is no living player within 8 m for 25 seconds in a row you get a shield against the first attack. Once between meetings. Works by itself. [Passive]",
            null);
        E("Hermit", "HermitTime", 25, 10, 90, 5, "Сколько секунд одиночества нужно", "Seconds of solitude needed");
        S("Intuitive", "Интуит", "Intuitive", "#80CBC4", M, 0, 0, "чуешь опасность рядом", "you sense danger nearby",
            "Мистик. Если предатель (или нейтрал-убийца) стоит рядом (до 4 м) 6 секунд и дольше, ты чувствуешь тревогу. Кто это, не говорится. Предупреждение не чаще раза в минуту. Работает сама. [Сама]",
            "Mystic. If an impostor (or a neutral killer) stays near you (within 4 m) for 6 seconds or longer you feel unease. It does not say who. A warning comes at most once a minute. Works by itself. [Passive]",
            null);
        E("Intuitive", "IntuitiveWarn", 60, 20, 180, 10, "Пауза между предупреждениями (сек)", "Pause between warnings (sec)");
        S("Runologist", "Рунолог", "Runologist", "#9575CD", M, 20, 0, "питомец: руна, повторно - отчёт", "pet: a rune, again for the report",
            "Мистик. Питомец рисует руну на полу. Сколько разных игроков подошло к ней ближе 2 м, считается без имён. Питомец ещё раз показывает счёт и стирает руну, а на собрании счёт приходит сам. Руна одна. Перезарядка 20 с. [Питомец]",
            "Mystic. The pet draws a rune on the floor. How many different players came within 2 m of it is counted, without names. The pet again shows the count and erases the rune, and at a meeting the count arrives by itself. One rune. Cooldown 20 s. [Pet]",
            (p, ru) =>
            {
                if (runes.TryGetValue(p.PlayerId, out var rn))
                {
                    runes.Remove(p.PlayerId);
                    H(p, T(ru, $"Руна стёрта: рядом с ней побывало игроков: {rn.Seen.Count}.", $"The rune is erased: players who came near it: {rn.Seen.Count}."));
                    return true;
                }
                var pos = p.GetTruePosition();
                runes[p.PlayerId] = new Rune { Pos = pos };
                H(p, T(ru, $"Руна нарисована: {Utils.RoomName(pos, true)}.", $"Rune drawn in {Utils.RoomName(pos, false)}."));
                return true;
            });



        // ---------------- Новая волна: уникальные роли ----------------
        S("Pedometer", "Шагомер", "Pedometer", "#AED581", C, 0, 0, "на собрании: кто больше всех бегал", "at a meeting: who moved the most",
            "Мирный. Считает, сколько раз каждый игрок менял комнату за раунд. На собрании ты узнаёшь, кто был самым непоседливым и кто почти не двигался, с числами. Работает сам. [Сама]",
            "Crewmate. Counts how many times each player changed rooms during the round. At a meeting you learn who moved the most and who barely moved, with numbers. Works by itself. [Passive]",
            null);
        S("Interrogator", "Дознаватель", "Interrogator", "#FFAB91", C, 40, 3, "питомец: где игрок был 20 секунд назад", "pet: where a player was 20 seconds ago",
            "Мирный. Питомец рядом с игроком (до 3 м) спрашивает его алиби: ты узнаёшь, в какой комнате он находился 20 секунд назад. Использований: 3, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) asks for his alibi: you learn which room he was in 20 seconds ago. Uses: 3, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого (до 3 м).", "Nobody nearby (within 3 m).")); return false; }
                if (!hist.TryGetValue(t.PlayerId, out var q) || q.Count == 0) { H(p, T(ru, "Пока нет данных, подожди немного.", "No data yet, wait a bit.")); return false; }
                float want = Time.time - 20f;
                var best = q.OrderBy(x => Mathf.Abs(x.t - want)).First();
                if (Mathf.Abs(best.t - want) > 6f) { H(p, T(ru, "Игрок слишком недавно в игре: данных за 20 секунд нет.", "No data for 20 seconds back yet.")); return false; }
                H(p, T(ru, $"{Nm(t)} 20 секунд назад был: {Utils.RoomName(best.pos, true)}.", $"{Nm(t)} was in {Utils.RoomName(best.pos, false)} 20 seconds ago."));
                return true;
            });
        S("Atlas", "Атлас", "Atlas", "#80DEEA", C, 30, 5, "питомец: пустые комнаты", "pet: empty rooms",
            "Мирный. Питомец показывает комнаты, где сейчас нет ни одного живого игрока, до пяти штук. Хорошо для тихих заданий и для поиска, где точно не прячутся. Использований: 5, перезарядка 30 с. [Питомец]",
            "Crewmate. The pet shows rooms where there is not a single living player right now, up to five. Good for quiet tasks and for knowing where nobody hides. Uses: 5, cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var ss = ShipStatus.Instance;
                if (ss == null || ss.AllRooms == null) return false;
                var alive = PlayerControl.AllPlayerControls.ToArray().Where(Alive).Select(o => o.GetTruePosition()).ToList();
                var empty = new List<string>();
                foreach (var r in ss.AllRooms)
                {
                    if (r == null || r.roomArea == null) continue;
                    string id = r.RoomId.ToString();
                    if (id == "Hallway" || id == "Outside") continue;
                    if (alive.Any(pos => r.roomArea.OverlapPoint(pos))) continue;
                    string nm = Utils.RoomLabel(id, ru);
                    if (!empty.Contains(nm)) empty.Add(nm);
                }
                if (empty.Count == 0) { H(p, T(ru, "Пустых комнат нет: везде кто-то есть.", "No empty rooms: somebody is everywhere.")); return true; }
                var pick = empty.OrderBy(x => rng.Next()).Take(5);
                H(p, T(ru, "Сейчас пусто: ", "Empty now: ") + string.Join(", ", pick) + ".");
                return true;
            });
        S("Pawnbroker", "Ломбардщик", "Pawnbroker", "#BCAAA4", C, 60, 2, "питомец: обменять щит на задание", "pet: trade a shield for a task",
            "Мирный. Если у тебя есть щит, питомец рядом с игроком без щита (до 2,5 м) отдаёт ему твой щит, а за услугу ты получаешь одно своё выполненное задание. Использований: 2, перезарядка 60 с. [Питомец]",
            "Crewmate. If you have a shield, the pet next to a player without one (within 2.5 m) gives him your shield, and as payment one of your own tasks is completed. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                if (!RoleLogic.ShieldArmed.Contains(p.PlayerId)) { H(p, T(ru, "У тебя нет щита, нечего закладывать.", "You have no shield to trade.")); return false; }
                var t = Nearest(p, 2.5f, o => !RoleLogic.ShieldArmed.Contains(o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет игрока без щита (до 2,5 м).", "Nobody without a shield nearby (within 2.5 m).")); return false; }
                RoleLogic.ShieldArmed.Remove(p.PlayerId);
                RoleLogic.ShieldArmed.Add(t.PlayerId); RoleLogic.ShieldUsed.Remove(t.PlayerId);
                Utils.SendSecret(t, Translator.IsRu(t) ? "Ломбардщик отдал тебе свой щит!" : "A pawnbroker gave you his shield!");
                var task = p.Data.Tasks != null ? p.Data.Tasks.ToArray().FirstOrDefault(x => !x.Complete) : null;
                string pay = T(ru, "заданий у тебя больше нет", "you have no tasks left");
                if (task != null)
                {
                    try { p.RpcCompleteTask(task.Id); pay = T(ru, "твоё задание выполнено", "one of your tasks is done"); }
                    catch (Exception e) { Plugin.Logger.LogWarning("Pawnbroker: " + e.Message); }
                }
                H(p, T(ru, $"Сделка: щит ушёл к {Nm(t)}, {pay}.", $"Deal: the shield went to {Nm(t)}, {pay}."));
                return true;
            });
        S("Rounder", "Обходчик", "Rounder", "#A5D6A7", C, 0, 0, "обойди много разных комнат - получишь щит", "visit many different rooms to earn a shield",
            "Мирный. Если за раунд ты побываешь в шести разных комнатах (число в настройках), ты получаешь щит от первого нападения. Один раз между собраниями. Работает сам. [Сама]",
            "Crewmate. If you visit six different rooms in a round (the number is in the settings) you get a shield against the first attack. Once between meetings. Works by itself. [Passive]",
            null);
        E("Rounder", "RounderRooms", 6, 3, 12, 1, "Сколько разных комнат нужно обойти", "Different rooms to visit");

        S("Bugler", "Горнист", "Bugler", "#FFCA28", I, 0, 1, "питомец: экстренный сбор (раз за игру)", "pet: emergency gathering (once a game)",
            "Предатель. Питомец трубит в горн: сразу созывается собрание, как будто нажали кнопку. Только один раз за игру и не раньше чем через минуту после начала раунда. Хороший способ сорвать погоню или выдать себя за паникёра. [Питомец]",
            "Impostor. The pet blows the bugle: a meeting is called at once as if a button was pressed. Only once per game and not earlier than a minute into the round. A good way to break a chase or to pass as a panicker. [Pet]",
            (p, ru) =>
            {
                if (Time.time - roundStart < 60f) { H(p, T(ru, $"Рано: подожди ещё {Mathf.CeilToInt(60f - (Time.time - roundStart))} с.", $"Too early: wait {Mathf.CeilToInt(60f - (Time.time - roundStart))} s more.")); return false; }
                try { p.ReportDeadBody(null); }
                catch (Exception e) { Plugin.Logger.LogWarning("Bugler: " + e.Message); H(p, T(ru, "Горн не сработал.", "The bugle failed.")); return false; }
                return true;
            });
        S("Packleader", "Вожак стаи", "Packleader", "#C62828", I, 0, 0, "после убийства щит союзникам", "after a kill your allies get a shield",
            "Предатель. После каждого твоего убийства все остальные живые предатели получают щит от первого нападения (если у них его нет). Работает сам. [Сама]",
            "Impostor. After each of your kills all other living impostors get a shield against the first attack (if they have none). Works by itself. [Passive]",
            null);
        S("Soulsipper", "Душелов", "Soulsipper", "#6A1B9A", I, 0, 0, "после убийства узнаёшь роль жертвы", "after a kill you learn the victim's role",
            "Предатель. После каждого твоего убийства ты узнаёшь, какая роль была у жертвы. Так видно, кого стоит опасаться дальше. Работает сам. [Сама]",
            "Impostor. After each of your kills you learn what role the victim had. That shows whom to fear next. Works by itself. [Passive]",
            null);
        S("Burrower", "Кротоход", "Burrower", "#795548", I, 40, 0, "питомец: нырнуть к ближайшему люку", "pet: dive to the nearest vent",
            "Предатель. Питомец переносит тебя к ближайшему люку, который дальше 3 м от тебя. Быстрый путь отхода. Перезарядка 40 с. [Питомец]",
            "Impostor. The pet moves you to the nearest vent that is more than 3 m away. A quick way out. Cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var ss = ShipStatus.Instance;
                var vents = ss != null ? ss.AllVents : null;
                if (vents == null || vents.Length == 0) { H(p, T(ru, "На карте нет люков.", "There are no vents on this map.")); return false; }
                var me = p.GetTruePosition();
                Vector2 best = default; float bd = 9999f; bool found = false;
                foreach (var v in vents)
                {
                    if (v == null) continue;
                    var vp = (Vector2)v.transform.position;
                    float d = Vector2.Distance(me, vp);
                    if (d > 3f && d < bd) { bd = d; best = vp; found = true; }
                }
                if (!found) { H(p, T(ru, "Подходящего люка нет.", "No suitable vent.")); return false; }
                Commands.Tp(p, best);
                H(p, T(ru, $"Нырок: {Utils.RoomName(best, true)}.", $"Dive: {Utils.RoomName(best, false)}."));
                return true;
            });

        S("Hearthkeeper", "Хранитель очага", "Hearthkeeper", "#E65100", N, 25, 0, "питомец: объявить комнату своей", "pet: claim this room",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец объявляет комнату, где ты стоишь, своим очагом (одна комната до собрания). Любой, кто пробудет в очаге 6 секунд подряд, пока и ты в нём, погибает. Выйдешь из комнаты, и отсчёт сбрасывается. В коридоре не работает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet claims the room you stand in as your hearth (one room until a meeting). Anyone who stays in the hearth for 6 seconds in a row while you are in it too dies. Leave the room and the count resets. Does not work in a hallway. [Pet]",
            (p, ru) =>
            {
                string room = Utils.RoomName(p.GetTruePosition(), false);
                if (room == "hallway") { H(p, T(ru, "В коридоре очаг не развести.", "You cannot claim a hallway.")); return false; }
                hearth[p.PlayerId] = room; hearthPos[p.PlayerId] = p.GetTruePosition();
                H(p, T(ru, $"Очаг: {Utils.RoomName(p.GetTruePosition(), true)}. Кто пробудет здесь {G("HearthSecs")} с вместе с тобой, погибнет.", $"Hearth: {room}. Whoever stays here {G("HearthSecs")} s with you dies."));
                return true;
            });
        E("Hearthkeeper", "HearthSecs", 6, 3, 20, 1, "Сколько секунд в очаге до гибели", "Seconds in the hearth before death");
        S("Charon", "Перевозчик", "Charon", "#37474F", N, 50, 0, "питомец: взять пассажира, повторно - перевезти", "pet: take a passenger, again to ferry",
            "Нейтрал. Для сервера ты настоящий предатель. Первый питомец берёт в пассажиры ближайшего игрока (до 3 м). Второй питомец в течение 15 секунд переносит вас обоих к случайной двери, пассажир замирает на 3 секунды и погибает. Щит спасает пассажира. [Питомец]",
            "Neutral. The server sees you as a real impostor. The first pet takes the nearest player (within 3 m) as a passenger. The second pet within 15 seconds moves you both to a random door, the passenger freezes for 3 seconds and dies. A shield saves the passenger. [Pet]",
            (p, ru) =>
            {
                if (passenger.TryGetValue(p.PlayerId, out var ps) && Time.time < ps.until)
                {
                    var t = Utils.GetPlayer(ps.id);
                    passenger.Remove(p.PlayerId);
                    if (!Alive(t)) { H(p, T(ru, "Пассажир сбежал.", "The passenger is gone.")); return false; }
                    if (!RandomDoor(out var dest)) { H(p, T(ru, "Переправа не удалась.", "The crossing failed.")); return false; }
                    Commands.Tp(p, dest); Commands.Tp(t, dest);
                    Roles30.FreezePlayer(t, 3f);
                    timers.Add(new Timed { Kind = "ferry", Owner = p.PlayerId, Target = t.PlayerId, At = Time.time + 3.2f });
                    H(p, T(ru, $"Переправа: {Utils.RoomName(dest, true)}.", $"Crossing: {Utils.RoomName(dest, false)}."));
                    return true;
                }
                var pass = Nearest(p, 3f, o => true);
                if (pass == null) { H(p, T(ru, "Рядом нет пассажира (до 3 м).", "No passenger nearby (within 3 m).")); return false; }
                passenger[p.PlayerId] = (pass.PlayerId, Time.time + G("CharonWindow"));
                H(p, T(ru, $"Пассажир: {Nm(pass)}. Питомец ещё раз за {G("CharonWindow")} с, и ты его перевезёшь.", $"Passenger: {Nm(pass)}. Pet again within {G("CharonWindow")} s to ferry him."));
                return false;
            });
        E("Charon", "CharonWindow", 15, 5, 40, 1, "Сколько секунд можно перевезти пассажира", "Seconds to ferry the passenger");

        S("Ductlistener", "Слухач", "Ductlistener", "#B0BEC5", M, 25, 6, "питомец: кто сейчас в вентиляции", "pet: who is in the vents now",
            "Мистик. Питомец прикладывает ухо к вентиляции: ты узнаёшь, сколько живых игроков сейчас сидит в люках. Имён нет. Это улика против предателей. Использований: 6, перезарядка 25 с. [Питомец]",
            "Mystic. The pet puts an ear to the ducts: you learn how many living players are in the vents right now. No names. It is evidence against impostors. Uses: 6, cooldown 25 s. [Pet]",
            (p, ru) =>
            {
                int n = Others(p).Count(o => o.inVent);
                H(p, n == 0 ? T(ru, "В вентиляции тихо: никого.", "The vents are quiet: nobody.") : T(ru, $"В вентиляции кто-то есть: {n}.", $"Somebody is in the vents: {n}."));
                return true;
            });


        // ---------------- Волна 3.11: люки и ловушки ----------------
        S("Ventsleuth", "Следопыт люков", "Ventsleuth", "#4DB6AC", C, 45, 4, "питомец у люка: кто им пользовался", "pet near a vent: who used it",
            "Мирный. Питомец рядом с люком (до 3 м) показывает, кто последним в него залезал, если это было недавно, или сколько раз им пользовались. Использований: 4. [Питомец]",
            "Crewmate. The pet next to a vent (within 3 m) shows who entered it last if it was recent, or how many times it was used. Uses: 4. [Pet]",
            (p, ru) =>
            {
                if (!NearVent(p, 3f, out int vid)) { H(p, T(ru, "Рядом нет люка (до 3 м).", "No vent nearby (within 3 m).")); return false; }
                if (!ventLog.TryGetValue(vid, out var lg) || lg.Count == 0) { H(p, T(ru, "Этим люком ещё не пользовались.", "Nobody used this vent yet.")); return true; }
                var lastUse = lg[lg.Count - 1];
                var who = Utils.GetPlayer(lastUse.pid);
                if (Time.time - lastUse.t <= 90f && who != null) H(p, T(ru, $"В этот люк недавно залезал {Nm(who)}. Всего раз: {lg.Count}.", $"{Nm(who)} entered this vent recently. Total uses: {lg.Count}."));
                else H(p, T(ru, $"Им пользовались {lg.Count} раз, но давно.", $"Used {lg.Count} time(s), but a while ago."));
                return true;
            });
        S("Sealer", "Запечатыватель", "Sealer", "#90A4AE", C, 30, 2, "питомец у люка: запечатать до собрания", "pet near a vent: seal it until a meeting",
            "Мирный. Питомец рядом с люком (до 3 м) запечатывает его до следующего собрания: любого, кто влезет, сразу вышвырнет, а ты узнаешь, кто это был. Использований: 2. [Питомец]",
            "Crewmate. The pet next to a vent (within 3 m) seals it until the next meeting: anyone who enters is thrown out at once and you learn who it was. Uses: 2. [Pet]",
            (p, ru) =>
            {
                if (!NearVent(p, 3f, out int vid)) { H(p, T(ru, "Рядом нет люка (до 3 м).", "No vent nearby (within 3 m).")); return false; }
                if (sealedVents.ContainsKey(vid) || mineVents.ContainsKey(vid)) { H(p, T(ru, "Этот люк уже занят.", "This vent is already taken.")); return false; }
                sealedVents[vid] = p.PlayerId;
                H(p, T(ru, "Люк запечатан до собрания.", "Vent sealed until a meeting."));
                return true;
            });
        S("Snarer", "Силковщик", "Snarer", "#A1887F", C, 35, 3, "питомец: поставить силок на этом месте", "pet: lay a snare right here",
            "Мирный. Питомец ставит силок на месте, где ты стоишь. Кто наступит (ближе 1,2 м, кроме тебя), замирает на 5 секунд, а ты узнаёшь, кто и в какой комнате. Силки исчезают на собрании. Использований: 3. [Питомец]",
            "Crewmate. The pet lays a snare where you stand. Anyone who steps close (within 1.2 m, except you) freezes for 5 seconds and you learn who and in which room. Snares vanish at a meeting. Uses: 3. [Pet]",
            (p, ru) =>
            {
                snares.Add((p.PlayerId, p.GetTruePosition()));
                H(p, T(ru, $"Силок поставлен: {Utils.RoomName(p.GetTruePosition(), true)}.", $"Snare laid: {Utils.RoomName(p.GetTruePosition(), false)}."));
                return true;
            });
        S("Enumerator", "Переписчик", "Enumerator", "#FFCC80", C, 30, 4, "питомец: где сейчас больше всего людей", "pet: where the crowds are right now",
            "Мирный. Питомец сообщает три самые людные комнаты и сколько в них живых игроков. Использований: 4. [Питомец]",
            "Crewmate. The pet reports the three most crowded rooms and how many living players are in each. Uses: 4. [Pet]",
            (p, ru) =>
            {
                var ss = ShipStatus.Instance;
                if (ss == null || ss.AllRooms == null) return false;
                var alive = PlayerControl.AllPlayerControls.ToArray().Where(Alive).Select(o => o.GetTruePosition()).ToList();
                var res = new List<(string name, int n)>();
                foreach (var r in ss.AllRooms)
                {
                    if (r == null || r.roomArea == null) continue;
                    string id = r.RoomId.ToString();
                    if (id == "Hallway" || id == "Outside") continue;
                    int n = alive.Count(pos => r.roomArea.OverlapPoint(pos));
                    if (n > 0) res.Add((Utils.RoomLabel(id, ru), n));
                }
                if (res.Count == 0) { H(p, T(ru, "Все в коридорах.", "Everybody is in the hallways.")); return true; }
                H(p, T(ru, "Людно: ", "Crowded: ") + string.Join(", ", res.OrderByDescending(x => x.n).Take(3).Select(x => $"{x.name} ({x.n})")) + ".");
                return true;
            });
        S("Cartographer", "Картограф", "Cartographer", "#C5E1A5", C, 0, 0, "на собрании: куда никто не заходил", "at a meeting: rooms nobody entered",
            "Мирный. На каждом собрании ты узнаёшь, в какие комнаты за раунд не заходил никто. Значит, там убийцу можно не искать. Работает сам. [Сама]",
            "Crewmate. At every meeting you learn which rooms nobody entered during the round, so you need not look for the killer there. Works by itself. [Passive]",
            null);

        S("Ventbomber", "Люкобомбист", "Ventbomber", "#D84315", I, 40, 2, "питомец у люка: заминировать", "pet near a vent: mine it",
            "Предатель. Питомец рядом с люком (до 3 м) минирует его до собрания. Первый не-предатель, который в него залезет, погибнет (щит спасает). Использований: 2. [Питомец]",
            "Impostor. The pet next to a vent (within 3 m) mines it until a meeting. The first non-impostor who enters it dies (a shield saves him). Uses: 2. [Pet]",
            (p, ru) =>
            {
                if (!NearVent(p, 3f, out int vid)) { H(p, T(ru, "Рядом нет люка (до 3 м).", "No vent nearby (within 3 m).")); return false; }
                if (sealedVents.ContainsKey(vid) || mineVents.ContainsKey(vid)) { H(p, T(ru, "Этот люк уже занят.", "This vent is already taken.")); return false; }
                mineVents[vid] = p.PlayerId;
                H(p, T(ru, "Люк заминирован.", "Vent mined."));
                return true;
            });
        S("Flashbulb", "Вспышник", "Flashbulb", "#FFF176", I, 50, 3, "питомец: ослепить всех рядом", "pet: blind everyone nearby",
            "Предатель. Питомец даёт яркую вспышку: все не-предатели в радиусе 6 м на 4 секунды теряют обзор. Использований: 3. [Питомец]",
            "Impostor. The pet makes a bright flash: all non-impostors within 6 m lose sight for 4 seconds. Uses: 3. [Pet]",
            (p, ru) =>
            {
                var pos = p.GetTruePosition();
                var tg = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), pos) <= 6f).ToList();
                if (tg.Count == 0) { H(p, T(ru, "Рядом некого ослеплять.", "Nobody nearby to blind.")); return false; }
                foreach (var o in tg) RoleLogic.FlashPlayer(o, 4f);
                H(p, T(ru, $"Ослеплено: {tg.Count}.", $"Blinded: {tg.Count}."));
                return true;
            });
        S("Beguiler", "Заманщик", "Beguiler", "#EC407A", I, 45, 3, "питомец: увести свидетеля к двери", "pet: lead a witness to a door",
            "Предатель. Питомец уводит ближайшего не-предателя (до 5 м) к случайной двери и замораживает его на 2 секунды. Так можно убрать свидетеля. Использований: 3. [Питомец]",
            "Impostor. The pet leads the nearest non-impostor (within 5 m) to a random door and freezes him for 2 seconds. A way to get rid of a witness. Uses: 3. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 5f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели (до 5 м).", "No target nearby (within 5 m).")); return false; }
                if (!RandomDoor(out var dest)) return false;
                Commands.Tp(t, dest); Roles30.FreezePlayer(t, 2f);
                H(p, T(ru, $"{Nm(t)} уведён: {Utils.RoomName(dest, true)}.", $"{Nm(t)} led away: {Utils.RoomName(dest, false)}."));
                return true;
            });
        S("Alarmer", "Тревожник", "Alarmer", "#FF7043", I, 70, 2, "питомец: ложная тревога всем", "pet: a false alarm for everyone",
            "Предатель. Питомец поднимает ложную тревогу: все живые не-предатели видят над ником надпись про крик в случайной пустой комнате. Пока они бегут туда, ты работаешь в другом месте. Использований: 2. [Питомец]",
            "Impostor. The pet raises a false alarm: all living non-impostors see a line above their nick about a scream in a random empty room. While they run there you work elsewhere. Uses: 2. [Pet]",
            (p, ru) =>
            {
                var ss = ShipStatus.Instance;
                if (ss == null || ss.AllRooms == null) return false;
                var alive = PlayerControl.AllPlayerControls.ToArray().Where(Alive).Select(o => o.GetTruePosition()).ToList();
                var empty = new List<string>();
                foreach (var r in ss.AllRooms)
                {
                    if (r == null || r.roomArea == null) continue;
                    string id = r.RoomId.ToString();
                    if (id == "Hallway" || id == "Outside") continue;
                    if (!alive.Any(pos => r.roomArea.OverlapPoint(pos))) empty.Add(id);
                }
                if (empty.Count == 0) { H(p, T(ru, "Пустых комнат нет, тревога не нужна.", "No empty rooms, no alarm needed.")); return false; }
                string room = empty[rng.Next(empty.Count)];
                foreach (var o in Others(p).Where(x => !IsImp(x)))
                    NameInfo.Notify(o, T(Translator.IsRu(o), $"Тревога! Крик: {Utils.RoomLabel(room, true)}", $"Alarm! A scream: {Utils.RoomLabel(room, false)}"), 8f);
                H(p, T(ru, $"Ложная тревога: {Utils.RoomLabel(room, true)}.", $"False alarm: {Utils.RoomLabel(room, false)}."));
                return true;
            });

        S("Pouncer", "Прыгун", "Pouncer", "#BF360C", N, 45, 0, "питомец: прыжок и убийство ближайшего", "pet: leap and kill the nearest",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец бросает тебя к ближайшему игроку (до 8 м) и через 0,7 секунды ты его убиваешь. Щит спасает жертву. Перезарядка 45 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet throws you to the nearest player (within 8 m) and 0.7 seconds later you kill him. A shield saves the victim. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 8f, o => true);
                if (t == null) { H(p, T(ru, "Рядом нет цели (до 8 м).", "No target nearby (within 8 m).")); return false; }
                Commands.Tp(p, t.GetTruePosition());
                timers.Add(new Timed { Kind = "pounce", Owner = p.PlayerId, Target = t.PlayerId, At = Time.time + 0.7f });
                return true;
            });

        S("Ventecho", "Эхо люков", "Ventecho", "#7986CB", M, 30, 5, "питомец: сколько раз лазили в люки", "pet: how many vent entries so far",
            "Мистик. Питомец прислушивается к эху: ты узнаёшь, сколько раз за раунд кто-то залезал в люки и сколько разных игроков это делало. Имён нет. Использований: 5. [Питомец]",
            "Mystic. The pet listens to the echo: you learn how many times someone entered vents this round and how many different players did it. No names. Uses: 5. [Pet]",
            (p, ru) =>
            {
                int total = ventLog.Values.Sum(l => l.Count);
                int who = ventLog.Values.SelectMany(l => l.Select(x => x.pid)).Distinct().Count();
                H(p, total == 0 ? T(ru, "Эхо молчит: в люки никто не лазил.", "The echo is silent: nobody used the vents.") : T(ru, $"Эхо: люки использовали {total} раз(а), игроков: {who}.", $"Echo: vents used {total} time(s) by {who} player(s)."));
                return true;
            });

        DefineFifteen();
        DefineGhosts(c);
    }

    static readonly Dictionary<byte, int> newtonMode = new();
    static readonly Dictionary<string, int> newtonUsed = new();
    static readonly HashSet<byte> newtonShield = new();
    static readonly Dictionary<byte, float> newtonRe = new();

    static bool InElectrical(Vector2 pos)
    {
        var ss = ShipStatus.Instance;
        if (ss == null || ss.AllRooms == null) return false;
        foreach (var r in ss.AllRooms)
            if (r != null && r.roomArea != null && r.RoomId == SystemTypes.Electrical && r.roomArea.OverlapPoint(pos)) return true;
        return false;
    }

    static string FixLights()
    {
        try
        {
            var ss = ShipStatus.Instance;
            if (ss == null || ss.Systems == null || !ss.Systems.ContainsKey(SystemTypes.Electrical)) return null;
            var act = ss.Systems[SystemTypes.Electrical].TryCast<IActivatable>();
            if (act == null || !act.IsActive) return null;
            var sw = ss.Systems[SystemTypes.Electrical].TryCast<SwitchSystem>();
            if (sw == null) return null;
            sw.ActualSwitches = 0; sw.ExpectedSwitches = 0;
            return "lights";
        }
        catch { return null; }
    }

    static void NewtonTick(float now)
    {
        foreach (var nk in RoleState.Assigned.ToList())
        {
            if (nk.Value.Id != "Newton") continue;
            var np = Utils.GetPlayer(nk.Key);
            if (!Alive(np)) { newtonShield.Remove(nk.Key); continue; }
            bool inside = InElectrical(np.GetTruePosition());
            bool ru = Translator.IsRu(np);
            if (inside)
            {
                if (!newtonShield.Contains(nk.Key))
                {
                    if (!RoleLogic.ShieldArmed.Contains(nk.Key)) { RoleLogic.ShieldArmed.Add(nk.Key); RoleLogic.ShieldUsed.Remove(nk.Key); newtonShield.Add(nk.Key); H(np, T(ru, "Электрический щит включён.", "Electric shield on.")); }
                }
                else if (!RoleLogic.ShieldArmed.Contains(nk.Key))
                {
                    if (!newtonRe.TryGetValue(nk.Key, out var rt)) newtonRe[nk.Key] = now + 60f;
                    else if (now >= rt) { RoleLogic.ShieldArmed.Add(nk.Key); RoleLogic.ShieldUsed.Remove(nk.Key); newtonRe.Remove(nk.Key); H(np, T(ru, "Щит снова заряжен.", "The shield is recharged.")); }
                }
            }
            else if (newtonShield.Contains(nk.Key))
            {
                RoleLogic.ShieldArmed.Remove(nk.Key); newtonShield.Remove(nk.Key); newtonRe.Remove(nk.Key);
            }
        }
    }

    static readonly string[] horseRows = { "...DB.", "...BBB", "DBBBBB", "BBBBBB", "B.B..B" };

    // картинка лошади из цветных блоков (тег mark) под ником, видна всем
    public static string HorseArt(PlayerControl p)
    {
        if (Opts.HorseCostume == null || !Opts.HorseCostume.Value || !Alive(p)) return null;
        if (!RoleState.Assigned.TryGetValue(p.PlayerId, out var role) || role.Id != "Hobbyhorse") return null;
        var sb = new System.Text.StringBuilder();
        string size = Opts.HorseSize.Value + "%";
        string shift = (Opts.HorseShift.Value / 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < horseRows.Length; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append("<voffset=-").Append(shift).Append("em><size=").Append(size).Append('>');
            foreach (char ch in horseRows[i])
            {
                if (ch == 'B') sb.Append("<mark=#8D6E63FF>  </mark>");
                else if (ch == 'D') sb.Append("<mark=#5D4037FF>  </mark>");
                else sb.Append("  ");
            }
            sb.Append("</size></voffset>");
        }
        return sb.ToString();
    }

    static readonly HashSet<byte> horseOn = new();
    static readonly Dictionary<byte, float> horseStamina = new(), horseTired = new(), horseT = new();
    static readonly Dictionary<byte, Vector2> horsePrev = new();
    static readonly Dictionary<string, float> horseHit = new();
    static float nextHorse;

    static void HorseTick(float now)
    {
        if (now < nextHorse) return;
        nextHorse = now + 0.1f;
        foreach (var hk in RoleState.Assigned.ToList())
        {
            if (hk.Value.Id != "Hobbyhorse") continue;
            byte hid = hk.Key;
            var hp = Utils.GetPlayer(hid);
            if (!Alive(hp)) { horseOn.Remove(hid); continue; }
            bool ru = Translator.IsRu(hp);
            float dt = horseT.TryGetValue(hid, out var lastT) ? Mathf.Min(now - lastT, 0.5f) : 0.1f;
            horseT[hid] = now;
            float st = horseStamina.TryGetValue(hid, out var sv) ? sv : 100f;
            Vector2 pos = hp.GetTruePosition();
            if (!horseOn.Contains(hid))
            {
                horseStamina[hid] = Mathf.Min(100f, st + 12f * dt);
                horsePrev[hid] = pos;
                continue;
            }
            st -= 25f * dt;
            if (st <= 0f)
            {
                horseOn.Remove(hid); horseStamina[hid] = 0f; horseTired[hid] = now + 4f;
                H(hp, T(ru, "Конь выдохся!", "The horse is winded!"));
                continue;
            }
            horseStamina[hid] = st;
            if (horsePrev.TryGetValue(hid, out var prev))
            {
                Vector2 disp = pos - prev;
                if (disp.magnitude > 0.02f && disp.magnitude < 3f)
                {
                    Vector2 target = pos + disp * (G("HorseBoost") / 100f);
                    Vector2 dir = target - pos;
                    bool blocked = false;
                    try { blocked = PhysicsHelpers.AnyNonTriggersBetween(pos, dir.normalized, dir.magnitude + 0.3f, Constants.ShipAndObjectsMask); } catch { }
                    if (!blocked) { Commands.Tp(hp, target); pos = target; }
                }
            }
            horsePrev[hid] = pos;
            foreach (var hv in Others(hp))
            {
                if (Vector2.Distance(hv.GetTruePosition(), pos) > 0.9f) continue;
                string hkey = hid + ":" + hv.PlayerId;
                if (horseHit.TryGetValue(hkey, out var ht) && now - ht < 5f) continue;
                horseHit[hkey] = now;
                Roles30.FreezePlayer(hv, 1.5f);
                H(hp, T(ru, $"Ты сбил {Nm(hv)}.", $"You knocked down {Nm(hv)}."));
                Utils.SendSecret(hv, Translator.IsRu(hv) ? "Тебя сбил конь!" : "A horse knocked you down!");
            }
        }
    }

    static bool NearBody(Vector2 me, float max, out byte id)
    {
        id = 0; bool found = false; float bd = max;
        foreach (var b in bodies)
        {
            if (eaten.Contains(b.id)) continue;
            float d = Vector2.Distance(b.pos, me);
            if (d <= bd) { bd = d; id = b.id; found = true; }
        }
        return found;
    }

    static bool TasksDone(PlayerControl pl)
    {
        if (pl == null || pl.Data == null) return false;
        var tk = pl.Data.Tasks;
        if (tk == null) return true;
        return !tk.ToArray().Any(x => !x.Complete);
    }

    // 15 ролей: по пять мирных, предателей и нейтралов-убийц
    static void DefineFifteen()
    {
        const OptTab N = OptTab.Neutral, C = OptTab.Crewmate, I = OptTab.Impostor;

        // ---------------- Мирные ----------------
        S("Switcher", "Подменщик", "Switcher", "#26A69A", C, 45, 3, "питомец: поменяться местами", "pet: swap places",
            "Мирный. Питомец меняет тебя местами с ближайшим игроком (до 3 м). Можно выдернуть друга из-под удара или самому уйти от опасности. Использований: 3, перезарядка 45 с. [Питомец]",
            "Crewmate. The pet swaps you with the nearest player (within 3 m). Pull a friend out of danger or get away yourself. Uses: 3, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет (до 3 м).", "Nobody within 3 m.")); return false; }
                var posA = p.GetTruePosition(); var posB = t.GetTruePosition();
                Commands.Tp(p, posB); Commands.Tp(t, posA);
                H(p, T(ru, $"Ты поменялся местами с {Nm(t)}.", $"You swapped places with {Nm(t)}."));
                return true;
            });
        S("Taskmaster", "Табельщик", "Taskmaster", "#9CCC65", C, 40, 4, "питомец: сколько заданий сделал игрок", "pet: how many tasks a player did",
            "Мирный. Питомец рядом с игроком (до 3 м) показывает, сколько заданий он выполнил из скольких. Если списка заданий нет, ты узнаешь и это. Иногда почерк не разобрать (шанс в настройках). Использований: 4, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) shows how many of his tasks are done. If he has no task list you learn that too. Sometimes the handwriting is unreadable (chance in the settings). Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет (до 3 м).", "Nobody within 3 m.")); return false; }
                if (rng.Next(100) < G("TaskmasterFail")) { H(p, T(ru, "Записи не разобрать, попытка потрачена.", "The notes are unreadable, the try is spent.")); return true; }
                var arr = t.Data.Tasks != null ? t.Data.Tasks.ToArray() : null;
                if (arr == null || arr.Length == 0) { H(p, T(ru, $"{Nm(t)}: списка заданий нет.", $"{Nm(t)}: no task list.")); return true; }
                int done = arr.Count(x => x.Complete);
                H(p, T(ru, $"{Nm(t)}: заданий выполнено {done} из {arr.Length}.", $"{Nm(t)}: {done} of {arr.Length} tasks done."));
                return true;
            });
        E("Taskmaster", "TaskmasterFail", 20, 0, 90, 10, "Шанс, что записи не разобрать (%)", "Chance the notes are unreadable (%)");
        S("Mourner", "Плакальщик", "Mourner", "#B0BEC5", C, 0, 0, "слышишь крик, если рядом убили", "you hear a scream if a kill happens near",
            "Мирный. Если кого-то убили рядом с тобой (по умолчанию в радиусе 12 м), ты слышишь крик и узнаёшь комнату, где это случилось. Имя убийцы не открывается. [Сама]",
            "Crewmate. If someone is killed near you (within 12 m by default) you hear a scream and learn the room where it happened. The killer's name is not revealed. [Passive]",
            null);
        E("Mourner", "MournerRadius", 12, 4, 30, 1, "Радиус, откуда слышен крик (м)", "Radius you can hear a scream from (m)");
        S("Coroner", "Коронер", "Coroner", "#A1887F", C, 35, 4, "питомец у тела: кто был рядом при смерти", "pet next to a body: who was near at death",
            "Мирный. Питомец рядом с телом (до 2,5 м) называет всех, кто был в радиусе 8 м от жертвы в момент смерти, включая убийцу. Список перемешан. Использований: 4, перезарядка 35 с. [Питомец]",
            "Crewmate. The pet next to a body (within 2.5 m) names everyone who was within 8 m of the victim at the moment of death, the killer included. The list is shuffled. Uses: 4, cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                if (!NearBody(p.GetTruePosition(), 2.5f, out var bid)) { H(p, T(ru, "Рядом нет тела (до 2,5 м).", "No body within 2.5 m.")); return false; }
                if (!deathNear.TryGetValue(bid, out var names) || names.Count == 0) { H(p, T(ru, "Рядом с телом в момент смерти никого не было.", "Nobody was near the body at the moment of death.")); return true; }
                H(p, T(ru, "Рядом при смерти были: ", "Near at the moment of death: ") + string.Join(", ", names) + ".");
                return true;
            });
        S("Dispatcher", "Диспетчер", "Dispatcher", "#4FC3F7", C, 40, 4, "питомец: где последним лезли в люк", "pet: where a vent was entered last",
            "Мирный. Питомец сообщает, в какой комнате последний раз кто-то залез в люк и сколько секунд назад. Имени нет. Использований: 4, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet tells in which room someone entered a vent last and how many seconds ago. No name. Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                int bestVent = -1; float bestAt = -1f;
                foreach (var kv in ventLog)
                    foreach (var en in kv.Value)
                        if (en.t > bestAt) { bestAt = en.t; bestVent = kv.Key; }
                if (bestVent < 0) { H(p, T(ru, "Диспетчер: в люки пока никто не лазил.", "Dispatcher: nobody has used the vents yet.")); return true; }
                int ago = Mathf.RoundToInt(Time.time - bestAt);
                H(p, T(ru, $"Диспетчер: последний вход в люк - {VentRoom(bestVent, true)}, {ago} с назад.", $"Dispatcher: the last vent entry was in {VentRoom(bestVent, false)}, {ago} s ago."));
                return true;
            });

        S("Bot", "Бот", "Bot", "#4DD0E1", C, 0, 0, "бот сам выполняет твои задания", "a bot does your tasks for you",
            "Мирный. У тебя есть бот: он сам ходит по карте и выполняет твои задания, примерно одно за 50 секунд (можно менять в настройках). Тебе остаётся следить за игрой и искать предателей. Во время собрания бот стоит. Когда ты погибаешь, бот останавливается. [Сама]",
            "Crewmate. You have a bot: it walks the map by itself and completes your tasks, about one every 50 seconds (adjustable in the settings). You only have to watch the game and look for impostors. The bot stands still during a meeting. When you die the bot stops. [Passive]",
            null);
        E("Bot", "BotEvery", 50, 10, 240, 5, "Как часто бот делает задание (сек)", "Seconds between bot tasks");

        S("Newton", "Ньютон", "Newton", "#FFD600", C, 30, 0, "питомец: режим (снаружи) или удар током (в электрощитовой)", "pet: switch mode (outside) or discharge (in Electrical)",
            "Мирный. Твоя стихия - электрощитовая. Внутри неё у тебя электрический щит: он отменяет одно нападение и возвращается через минуту, а когда выходишь, пропадает. Питомец вне электрощитовой переключает режим: Задания или Контроль. Питомец внутри неё применяет режим. Задания: выполняет до двух твоих заданий и по одному заданию каждому мирному, кто в электрощитовой (использований 4). Контроль: все остальные в электрощитовой замирают на 4 секунды, а если идёт саботаж света, он чинится (использований 3). Перезарядка 30 с. [Питомец]",
            "Crewmate. Electrical is your element. Inside it you have an electric shield: it cancels one attack and comes back after a minute, and it disappears when you leave. The pet outside Electrical switches the mode: Tasks or Control. The pet inside it uses the mode. Tasks: completes up to two of your tasks and one task for every crewmate in Electrical (4 uses). Control: everyone else in Electrical freezes for 4 seconds, and an active lights sabotage is fixed (3 uses). Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                byte nid = p.PlayerId;
                int nm = newtonMode.TryGetValue(nid, out var nmv) ? nmv : 0;
                if (!InElectrical(p.GetTruePosition()))
                {
                    nm = 1 - nm; newtonMode[nid] = nm;
                    H(p, T(ru, nm == 0 ? "Режим: Задания. Применяй питомцем в электрощитовой." : "Режим: Контроль. Применяй питомцем в электрощитовой.", nm == 0 ? "Mode: Tasks. Use the pet inside Electrical." : "Mode: Control. Use the pet inside Electrical."));
                    return false;
                }
                string ukey = "n" + nm + ":" + nid;
                int nlim = nm == 0 ? G("NewtonTaskUses") : G("NewtonControlUses");
                if (newtonUsed.TryGetValue(ukey, out var nu) && nu >= nlim) { H(p, T(ru, "Использования этого режима закончились.", "No uses left for this mode.")); return false; }
                if (nm == 0)
                {
                    int done = 0;
                    foreach (var nt in p.Data.Tasks.ToArray().Where(x => !x.Complete).Take(2).ToList())
                    {
                        try { p.RpcCompleteTask(nt.Id); done++; } catch (Exception e) { Plugin.Logger.LogWarning("Newton: " + e.Message); }
                    }
                    int helped = 0;
                    foreach (var o in Others(p).Where(x => !IsImp(x) && InElectrical(x.GetTruePosition()) && x.Data.Tasks != null).ToList())
                    {
                        var ot = o.Data.Tasks.ToArray().FirstOrDefault(x => !x.Complete);
                        if (ot == null) continue;
                        try { o.RpcCompleteTask(ot.Id); helped++; Utils.SendSecret(o, Translator.IsRu(o) ? "Ньютон выполнил за тебя задание!" : "Newton completed a task for you!"); } catch (Exception e) { Plugin.Logger.LogWarning("Newton help: " + e.Message); }
                    }
                    if (done == 0 && helped == 0) { H(p, T(ru, "Заданий для выполнения нет.", "No tasks to complete.")); return false; }
                    H(p, T(ru, $"Разряд: твоих заданий {done}, чужих {helped}.", $"Discharge: your tasks {done}, others {helped}."));
                }
                else
                {
                    var frozen = Others(p).Where(x => InElectrical(x.GetTruePosition())).ToList();
                    foreach (var o in frozen) Roles30.FreezePlayer(o, 4f);
                    string fix = FixLights();
                    if (frozen.Count == 0 && fix == null) { H(p, T(ru, "В электрощитовой никого нет и свет в порядке.", "Nobody in Electrical and the lights are fine.")); return false; }
                    H(p, T(ru, $"Контроль: замерло {frozen.Count}." + (fix != null ? " Свет починен." : ""), $"Control: frozen {frozen.Count}." + (fix != null ? " Lights fixed." : "")));
                }
                newtonUsed[ukey] = (newtonUsed.TryGetValue(ukey, out var nu2) ? nu2 : 0) + 1;
                return true;
            });
        E("Newton", "NewtonTaskUses", 4, 1, 15, 1, "Использований режима Задания", "Uses of the Tasks mode");
        E("Newton", "NewtonControlUses", 3, 1, 15, 1, "Использований режима Контроль", "Uses of the Control mode");

        S("Hobbyhorse", "Хобихорсер", "Hobbyhorse", "#FFB74D", C, 5, 0, "питомец: галоп вкл или выкл", "pet: gallop on or off",
            "Мирный. Ты катаешься на своём хобби-коне. Питомец включает и выключает галоп. Пока идёт галоп, ты бежишь намного быстрее (по умолчанию вдвое), но конь тратит силы, их шкала 100 и падает на 25 в секунду. Пока стоишь или идёшь шагом, силы восстанавливаются. Когда силы кончатся, конь выдохнется и не пойдёт в галоп 4 секунды. В галопе ты сбиваешь всех, кто ближе 0,9 м: они замирают на 1,5 секунды (одного и того же игрока не чаще раза в 5 секунд). Стены не пробиваешь. [Питомец]",
            "Crewmate. You ride your hobby horse. The pet starts and stops the gallop. During a gallop you run much faster (twice as fast by default), but the horse spends stamina: the scale is 100 and drops by 25 per second. While you stand or walk the stamina recovers. When it runs out the horse is winded and will not gallop for 4 seconds. In a gallop you knock down everyone closer than 0.9 m: they freeze for 1.5 seconds (the same player not more than once every 5 seconds). You cannot go through walls. [Pet]",
            (p, ru) =>
            {
                byte hid = p.PlayerId;
                if (horseOn.Contains(hid)) { horseOn.Remove(hid); H(p, T(ru, "Конь перешёл на шаг.", "The horse slowed to a walk.")); return true; }
                if (horseTired.TryGetValue(hid, out var tiredUntil) && Time.time < tiredUntil) { H(p, T(ru, "Конь выдохся, подожди.", "The horse is winded, wait.")); return false; }
                float st = horseStamina.TryGetValue(hid, out var sv) ? sv : 100f;
                if (st < 20f) { H(p, T(ru, $"Силы коня: {Mathf.RoundToInt(st)}, нужно хотя бы 20.", $"Horse stamina: {Mathf.RoundToInt(st)}, at least 20 needed.")); return false; }
                horseOn.Add(hid); horsePrev.Remove(hid);
                H(p, T(ru, $"Галоп! Силы: {Mathf.RoundToInt(st)}.", $"Gallop! Stamina: {Mathf.RoundToInt(st)}."));
                return true;
            });
        E("Hobbyhorse", "HorseBoost", 100, 20, 300, 10, "Прибавка скорости в галопе (%)", "Speed bonus in a gallop (%)");

        // ---------------- Предатели ----------------
        S("Tripper", "Подножник", "Tripper", "#E53935", I, 50, 3, "питомец: подножка вплотную", "pet: trip someone up close",
            "Предатель. Питомец рядом с не-предателем (до 1,5 м) ставит ему подножку: он замирает на 6 секунд. Использований: 3, перезарядка 50 с. [Питомец]",
            "Impostor. The pet next to a non-impostor (within 1.5 m) trips him: he freezes for 6 seconds. Uses: 3, cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 1.5f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом никого нет (до 1,5 м).", "Nobody within 1.5 m.")); return false; }
                Roles30.FreezePlayer(t, 6f);
                H(p, T(ru, $"{Nm(t)} споткнулся.", $"{Nm(t)} tripped."));
                return true;
            });
        S("Eraser", "Стиратель", "Eraser", "#78909C", I, 60, 2, "питомец: стереть следы", "pet: erase the traces",
            "Предатель. Питомец стирает следы: журнал люков, историю перемещений и списки тех, кто был рядом с телами. Это ломает проверки Дознавателя, Коронера, Диспетчера и Следопыта люков. Использований: 2, перезарядка 60 с. [Питомец]",
            "Impostor. The pet erases the traces: the vent log, the movement history and the lists of who was near bodies. This breaks the checks of the Interrogator, Coroner, Dispatcher and Ventsleuth. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                ventLog.Clear(); alibi.Clear(); hist.Clear(); deathNear.Clear();
                H(p, T(ru, "Следы стёрты.", "The traces are erased."));
                return true;
            });
        S("Lockpicker", "Взломщик", "Lockpicker", "#FFB300", I, 40, 4, "питомец: найти щиты и сорвать ближайший", "pet: find shields and strip the nearest",
            "Предатель. Питомец называет всех мирных, у кого сейчас есть щит, а щит ближайшего из них (до 3 м) срывает. Использований: 4, перезарядка 40 с. [Питомец]",
            "Impostor. The pet names every crewmate who has a shield right now and strips the shield of the nearest one (within 3 m). Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var armed = Others(p).Where(o => !IsImp(o) && RoleLogic.ShieldArmed.Contains(o.PlayerId)).ToList();
                if (armed.Count == 0) { H(p, T(ru, "Ни у кого из мирных нет щита.", "No crewmate has a shield.")); return true; }
                var me = p.GetTruePosition();
                var closest = armed.OrderBy(o => Vector2.Distance(o.GetTruePosition(), me)).First();
                string list = string.Join(", ", armed.Select(o => Nm(o)));
                string tail = "";
                if (Vector2.Distance(closest.GetTruePosition(), me) <= 3f)
                {
                    RoleLogic.ShieldArmed.Remove(closest.PlayerId);
                    tail = T(ru, $" Щит {Nm(closest)} сорван.", $" The shield of {Nm(closest)} is stripped.");
                }
                H(p, T(ru, "Со щитом: ", "With a shield: ") + list + "." + tail);
                return true;
            });
        S("Dazer", "Дурман", "Dazer", "#7E57C2", I, 40, 0, "питомец: отложенное оглушение", "pet: delayed stun",
            "Предатель. Питомец одурманивает ближайшего не-предателя (до 2,5 м): через несколько секунд он замирает на несколько секунд. Собрание отменяет дурман. [Питомец]",
            "Impostor. The pet dazes the nearest non-impostor (within 2.5 m): after a few seconds he freezes for a few seconds. A meeting cancels the daze. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !IsImp(o) && !timers.Any(x => x.Kind == "daze" && x.Target == o.PlayerId));
                if (t == null) { H(p, T(ru, "Некого одурманить (до 2,5 м).", "Nobody to daze within 2.5 m.")); return false; }
                timers.Add(new Timed { Kind = "daze", Owner = p.PlayerId, Target = t.PlayerId, At = Time.time + G("DazeDelay") });
                H(p, T(ru, $"{Nm(t)} одурманен, подействует через {G("DazeDelay")} с.", $"{Nm(t)} is dazed, it kicks in in {G("DazeDelay")} s."));
                return true;
            });
        E("Dazer", "DazeDelay", 5, 1, 30, 1, "Через сколько секунд подействует", "Seconds until it kicks in");
        E("Dazer", "DazeSecs", 5, 1, 15, 1, "Сколько секунд жертва стоит", "Seconds the victim stands still");
        S("Changeling", "Подменыш", "Changeling", "#AB47BC", I, 60, 3, "питомец: поменяться с мирным", "pet: swap with a crewmate",
            "Предатель. Питомец меняет тебя местами со случайным мирным игроком на любом расстоянии. Так можно уйти от тела и оставить рядом с ним другого. Использований: 3, перезарядка 60 с. [Питомец]",
            "Impostor. The pet swaps you with a random crewmate at any distance. Walk away from a body and leave someone else standing next to it. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var list = Others(p).Where(o => !IsImp(o)).ToList();
                if (list.Count == 0) { H(p, T(ru, "Не с кем меняться.", "Nobody to swap with.")); return false; }
                var t = list[rng.Next(list.Count)];
                var posA = p.GetTruePosition(); var posB = t.GetTruePosition();
                Commands.Tp(p, posB); Commands.Tp(t, posA);
                H(p, T(ru, $"Ты поменялся местами с {Nm(t)}.", $"You swapped places with {Nm(t)}."));
                return true;
            });

        // ---------------- Нейтралы (настоящие убийцы, убивают питомцем) ----------------
        S("Digger", "Копатель", "Digger", "#8D6E63", N, 25, 0, "питомец: выкопать яму", "pet: dig a pit",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец роет яму там, где ты стоишь. Первый чужой игрок, подошедший к яме ближе 1,2 м, проваливается и погибает. Ямы пропадают на собрании. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet digs a pit where you stand. The first other player who comes within 1.2 m of it falls in and dies. Pits vanish at a meeting. [Pet]",
            (p, ru) =>
            {
                if (pits.Count(x => x.owner == p.PlayerId) >= G("DiggerMax")) { H(p, T(ru, "Все ямы уже выкопаны.", "All pits are already dug.")); return false; }
                pits.Add((p.PlayerId, p.GetTruePosition()));
                H(p, T(ru, $"Яма выкопана. Ям: {pits.Count(x => x.owner == p.PlayerId)}/{G("DiggerMax")}.", $"The pit is dug. Pits: {pits.Count(x => x.owner == p.PlayerId)}/{G("DiggerMax")}."));
                return true;
            });
        E("Digger", "DiggerMax", 2, 1, 6, 1, "Сколько ям может быть одновременно", "Pits at a time");
        S("Liquidator", "Ликвидатор", "Liquidator", "#546E7A", N, 25, 0, "питомец: убить того, кто закончил задания", "pet: kill someone who finished the tasks",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если у него не осталось невыполненных заданий. Тех, у кого заданий нет вообще, тоже можно. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if he has no unfinished tasks. Players with no tasks at all count too. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => TasksDone(o));
                if (t == null) { H(p, T(ru, "Подходящей цели нет: нужен игрок без невыполненных заданий (до 2 м).", "No fitting target: the player must have no unfinished tasks (within 2 m).")); return false; }
                return Strike(p, t);
            });
        S("Duelist", "Дуэлянт", "Duelist", "#C62828", N, 40, 0, "питомец: вызвать на дуэль", "pet: challenge to a duel",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец вызывает ближайшего игрока (до 3 м) на дуэль: вы оба замираете, а когда время выйдет, он погибает. Щит его спасёт. Собрание отменяет дуэль. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet challenges the nearest player (within 3 m) to a duel: you both freeze and when the time is up he dies. A shield saves him. A meeting cancels the duel. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !timers.Any(x => x.Kind == "duel" && x.Target == o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом никого нет (до 3 м).", "Nobody within 3 m.")); return false; }
                Roles30.FreezePlayer(p, G("DuelWait")); Roles30.FreezePlayer(t, G("DuelWait"));
                timers.Add(new Timed { Kind = "duel", Owner = p.PlayerId, Target = t.PlayerId, At = Time.time + G("DuelWait") + 0.2f });
                H(p, T(ru, $"Дуэль с {Nm(t)}: {G("DuelWait")} с.", $"Duel with {Nm(t)}: {G("DuelWait")} s."));
                return true;
            });
        E("Duelist", "DuelWait", 3, 1, 10, 1, "Сколько секунд длится дуэль", "Duel length (sec)");
        S("Arsonist", "Поджигатель", "Arsonist", "#FF7043", N, 60, 0, "питомец: поджечь комнату", "pet: set the room on fire",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец поджигает комнату, где ты стоишь (в коридоре не работает). У всех в ней мигает экран, а через несколько секунд погибают все, кто остался внутри, кроме тебя. Собрание тушит огонь. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet sets the room you stand in on fire (does not work in a hallway). The screens of everyone inside flash, and after a few seconds everyone still inside dies, except you. A meeting puts the fire out. [Pet]",
            (p, ru) =>
            {
                string room = Utils.RoomName(p.GetTruePosition(), false);
                if (room == "hallway") { H(p, T(ru, "В коридоре поджигать нечего, зайди в комнату.", "Nothing to burn in a hallway, enter a room.")); return false; }
                timers.Add(new Timed { Kind = "fire:" + room, Owner = p.PlayerId, At = Time.time + G("ArsonDelay") });
                foreach (var burning in Others(p).Where(x => Utils.RoomName(x.GetTruePosition(), false) == room).ToList()) RoleLogic.FlashPlayer(burning, 2f);
                H(p, T(ru, $"Комната горит! Через {G("ArsonDelay")} с погибнут все, кто останется внутри.", $"The room is on fire! In {G("ArsonDelay")} s everyone still inside dies."));
                return true;
            });
        E("Arsonist", "ArsonDelay", 6, 2, 20, 1, "Через сколько секунд огонь убивает", "Seconds until the fire kills");
        S("Mercenary", "Наёмник", "Mercenary", "#6D4C41", N, 30, 0, "питомец: контракт, где цель или убить", "pet: contract, where the target is or kill",
            "Нейтрал. Для сервера ты настоящий предатель. Первый питомец назначает случайную цель (контракт). Дальше питомец показывает, в какой комнате цель, а если она ближе 2,5 м, убивает её. После убийства питомец назначит новую цель. [Питомец]",
            "Neutral. The server sees you as a real impostor. The first pet use assigns a random target (the contract). After that the pet shows which room the target is in, and kills it if it is within 2.5 m. After a kill the next pet use assigns a new target. [Pet]",
            (p, ru) =>
            {
                byte cid;
                if (!contract.TryGetValue(p.PlayerId, out cid) || !Alive(Utils.GetPlayer(cid)))
                {
                    var pool = Others(p).ToList();
                    if (pool.Count == 0) { H(p, T(ru, "Некого брать в контракт.", "Nobody to put a contract on.")); return false; }
                    var pick = pool[rng.Next(pool.Count)];
                    contract[p.PlayerId] = pick.PlayerId;
                    H(p, T(ru, $"Новый контракт: {Nm(pick)}.", $"New contract: {Nm(pick)}."));
                    return false;
                }
                var target = Utils.GetPlayer(cid);
                if (Vector2.Distance(p.GetTruePosition(), target.GetTruePosition()) <= 2.5f)
                {
                    if (!Strike(p, target)) return false;
                    contract.Remove(p.PlayerId);
                    return true;
                }
                H(p, T(ru, $"Контракт: {Nm(target)}, сейчас в комнате: {Utils.RoomName(target.GetTruePosition(), true)}.", $"Contract: {Nm(target)}, now in: {Utils.RoomName(target.GetTruePosition(), false)}."));
                return true;
            });
        foreach (var nid in new[] { "Digger", "Liquidator", "Duelist", "Arsonist", "Mercenary" })
            RoleLogic.RealImpNeutrals.Add(nid);
    }

    // ---------------- состояние и вспомогательное ----------------
    public static void Reset()
    {
        Markers.Clear();
        last.Clear(); used.Clear(); timers.Clear(); bodies.Clear(); eaten.Clear(); charge.Clear(); berserk.Clear(); berserkNext.Clear();
        infect.Clear(); ambush.Clear(); flowers.Clear(); hides.Clear(); hist.Clear(); moves.Clear(); lastRoom.Clear(); visited.Clear(); rounderDone.Clear(); hearth.Clear(); hearthPos.Clear(); hearthIn.Clear(); passenger.Clear(); ventLog.Clear(); sealedVents.Clear(); mineVents.Clear(); snares.Clear(); vacts.Clear(); bq.Clear(); roundStart = Time.time; runes.Clear(); alibi.Clear(); aloneSince.Clear(); closeSince.Clear(); nextWarn.Clear(); hermitDone.Clear(); ghostPick.Clear(); gUsed.Clear(); gLast.Clear(); deathNear.Clear(); pits.Clear(); contract.Clear(); botNext.Clear(); horseOn.Clear(); horseStamina.Clear(); horseTired.Clear(); horseT.Clear(); horsePrev.Clear(); horseHit.Clear(); newtonMode.Clear(); newtonUsed.Clear(); newtonShield.Clear(); newtonRe.Clear();
    }

    static int Left(Spec s, byte id) => s.Uses <= 0 ? -1 : Math.Max(0, G(s.Id + "Uses") - (used.TryGetValue(Key(s.Id, id), out var u) ? u : 0));
    static int CdOf(Spec s) => s.Cd > 0 ? G(s.Id + "Cd") : 0;

    public static string Status(PlayerControl p, RoleDef r)
    {
        if (!ById.TryGetValue(r.Id, out var s)) return null;
        bool ru = Translator.IsRu(p);
        string d = ru ? s.DescRu : s.DescEn;
        int left = Left(s, p.PlayerId);
        string extra = r.Id == "Hobbyhorse" ? T(ru, $" Силы коня: {Mathf.RoundToInt(horseStamina.TryGetValue(p.PlayerId, out var hsv) ? hsv : 100f)}{(horseOn.Contains(p.PlayerId) ? ", галоп" : "")}.", $" Horse stamina: {Mathf.RoundToInt(horseStamina.TryGetValue(p.PlayerId, out var hsw) ? hsw : 100f)}{(horseOn.Contains(p.PlayerId) ? ", galloping" : "")}.") : r.Id == "Newton" ? T(ru, $" Режим: {(newtonMode.TryGetValue(p.PlayerId, out var nmd) && nmd == 1 ? "Контроль" : "Задания")}.", $" Mode: {(newtonMode.TryGetValue(p.PlayerId, out var nme) && nme == 1 ? "Control" : "Tasks")}.") : r.Id == "Gardener" ? T(ru, $" Цветков: {flowers.Count(f => f.Owner == p.PlayerId)}/{G("GardenerMax")}.", $" Flowers: {flowers.Count(f => f.Owner == p.PlayerId)}/{G("GardenerMax")}.") : "";
        return r.Colored(ru) + ": " + d + (left >= 0 ? T(ru, $" Осталось: {left}.", $" Left: {left}.") : "") + extra;
    }

    public static string Short(PlayerControl p, string id, bool ru)
    {
        var s = ById[id];
        string hint = ru ? s.HintRu : s.HintEn;
        if (s.Fn == null) return hint;
        int left = Left(s, p.PlayerId);
        if (left == 0) return T(ru, "<color=#FF5555>использования закончились</color>", "<color=#FF5555>no uses left</color>");
        string tail = left > 0 ? T(ru, $", осталось {left}", $", {left} left") : "";
        if (last.TryGetValue(Key(id, p.PlayerId), out var t0) && Time.time - t0 < CdOf(s))
        {
            int w = Mathf.CeilToInt(CdOf(s) - (Time.time - t0));
            return T(ru, $"<color=#FFB74D>перезарядка {w} с</color>{tail}", $"<color=#FFB74D>cooldown {w} s</color>{tail}");
        }
        return hint + tail;
    }

    public static bool Use(PlayerControl p, string id)
    {
        bool ru = Translator.IsRu(p);
        var s = ById[id];
        if (s.Fn == null) { H(p, T(ru, "У этой роли нет активной способности, она работает сама.", "This role has no active ability, it works by itself.")); return true; }
        if (Left(s, p.PlayerId) == 0) { H(p, T(ru, "Использований больше не осталось.", "No uses left.")); return true; }
        if (last.TryGetValue(Key(id, p.PlayerId), out var t0) && Time.time - t0 < CdOf(s))
        {
            H(p, T(ru, $"Рано, ещё {Mathf.CeilToInt(CdOf(s) - (Time.time - t0))} с.", $"Too early, {Mathf.CeilToInt(CdOf(s) - (Time.time - t0))} s left."));
            return true;
        }
        bool ok = false;
        try { ok = s.Fn(p, ru); }
        catch (Exception e) { Plugin.Logger.LogWarning($"Extra33.{id}: {e.Message}"); }
        if (ok)
        {
            last[Key(id, p.PlayerId)] = Time.time;
            used[Key(id, p.PlayerId)] = (used.TryGetValue(Key(id, p.PlayerId), out var u) ? u : 0) + 1;
        }
        try { NameInfo.Refresh(); } catch { }
        return true;
    }

    public static void OnKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            if (target == null || target.Data == null) return;
            var pos = target.GetTruePosition();
            bodies.Add((target.PlayerId, pos));
            var near = new List<byte>();
            foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                if (Alive(o) && o.PlayerId != target.PlayerId && (killer == null || o.PlayerId != killer.PlayerId) && Vector2.Distance(o.GetTruePosition(), pos) <= 8f) near.Add(o.PlayerId);
            alibi[target.PlayerId] = near;
            deathNear[target.PlayerId] = PlayerControl.AllPlayerControls.ToArray()
                .Where(w => Alive(w) && w.PlayerId != target.PlayerId && Vector2.Distance(w.GetTruePosition(), pos) <= 8f)
                .Select(w => w.Data.PlayerName).OrderBy(w => rng.Next()).ToList();
            foreach (var mk in RoleState.Assigned.ToList())
            {
                if (mk.Value.Id != "Mourner") continue;
                var mourner = Utils.GetPlayer(mk.Key);
                if (!Alive(mourner) || mourner.PlayerId == target.PlayerId) continue;
                if (Vector2.Distance(mourner.GetTruePosition(), pos) > G("MournerRadius")) continue;
                bool mru = Translator.IsRu(mourner);
                H(mourner, T(mru, $"Ты услышал крик: кого-то убили, комната - {Utils.RoomName(pos, true)}.", $"You heard a scream: someone was killed, room - {Utils.RoomName(pos, false)}."));
            }
            if (killer != null && killer.PlayerId != target.PlayerId)
            {
                if (RoleLogic.Has(killer, "Packleader"))
                    foreach (var ally in PlayerControl.AllPlayerControls.ToArray())
                        if (Alive(ally) && ally.PlayerId != killer.PlayerId && IsImp(ally) && !RoleLogic.ShieldArmed.Contains(ally.PlayerId))
                        {
                            RoleLogic.ShieldArmed.Add(ally.PlayerId); RoleLogic.ShieldUsed.Remove(ally.PlayerId);
                            H(ally, T(Translator.IsRu(ally), "Вожак стаи дал тебе щит.", "The packleader gave you a shield."));
                        }
                if (RoleLogic.Has(killer, "Soulsipper"))
                {
                    bool kru = Translator.IsRu(killer);
                    H(killer, T(kru, $"Душа {Nm(target)}: роль - {RoleLogic.RoleLabel(target.PlayerId, target.Data.RoleType, true)}.", $"Soul of {Nm(target)}: role - {RoleLogic.RoleLabel(target.PlayerId, target.Data.RoleType, false)}."));
                }
            }
            foreach (var f in flowers) if (Vector2.Distance(f.Pos, pos) <= 4f) f.Wilted = true;
            if (killer != null && killer.PlayerId != target.PlayerId && RoleLogic.Has(killer, "Leech"))
            {
                RoleLogic.ShieldArmed.Add(killer.PlayerId); RoleLogic.ShieldUsed.Remove(killer.PlayerId);
                H(killer, T(Translator.IsRu(killer), "Пиявка: после убийства у тебя щит.", "Leech: you have a shield after the kill."));
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Extra33.OnKill: " + e.Message); }
    }

    public static void OnMeeting()
    {
        try
        {
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Pedometer") continue;
                var pm = Utils.GetPlayer(kv.Key);
                if (!Alive(pm)) continue;
                bool pru = Translator.IsRu(pm);
                var rank = PlayerControl.AllPlayerControls.ToArray().Where(Alive).Where(o => o.PlayerId != pm.PlayerId)
                    .Select(o => (o, n: moves.TryGetValue(o.PlayerId, out var mv) ? mv : 0)).OrderByDescending(x => x.n).ToList();
                if (rank.Count == 0) continue;
                var top = rank.First(); var low = rank.Last();
                Utils.SendSecret(pm, T(pru,
                    $"Шагомер: больше всех ходил {Nm(top.o)} ({top.n} смен комнат), меньше всех - {Nm(low.o)} ({low.n}).",
                    $"Pedometer: {Nm(top.o)} moved the most ({top.n} room changes), {Nm(low.o)} the least ({low.n})."));
            }
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Cartographer") continue;
                var cg = Utils.GetPlayer(kv.Key);
                var ss = ShipStatus.Instance;
                if (!Alive(cg) || ss == null || ss.AllRooms == null) continue;
                bool cru = Translator.IsRu(cg);
                var seen = new HashSet<string>(visited.Values.SelectMany(v => v));
                var untouched = new List<string>();
                foreach (var r in ss.AllRooms)
                {
                    if (r == null) continue;
                    string id = r.RoomId.ToString();
                    if (id == "Hallway" || id == "Outside") continue;
                    string nm = Utils.RoomLabel(id, false);
                    if (!seen.Contains(nm) && !untouched.Contains(Utils.RoomLabel(id, cru))) untouched.Add(Utils.RoomLabel(id, cru));
                }
                Utils.SendSecret(cg, untouched.Count == 0 ? T(cru, "Картограф: за раунд побывали во всех комнатах.", "Cartographer: every room was visited this round.")
                    : T(cru, "Картограф: никто не заходил в ", "Cartographer: nobody entered ") + string.Join(", ", untouched) + ".");
            }
            sealedVents.Clear(); mineVents.Clear(); snares.Clear(); vacts.Clear(); ventLog.Clear();
            moves.Clear(); lastRoom.Clear(); visited.Clear(); rounderDone.Clear(); hearth.Clear(); hearthPos.Clear(); hearthIn.Clear(); passenger.Clear(); roundStart = Time.time;
            timers.Clear(); bodies.Clear(); eaten.Clear(); berserk.Clear(); infect.Clear(); ambush.Clear(); alibi.Clear(); deathNear.Clear(); pits.Clear();
            aloneSince.Clear(); closeSince.Clear(); hermitDone.Clear();
            foreach (var rk in runes.ToList())
            {
                var ro = Utils.GetPlayer(rk.Key);
                if (Alive(ro)) Utils.SendSecret(ro, T(Translator.IsRu(ro), $"Руна: рядом с ней побывало игроков: {rk.Value.Seen.Count}.", $"Rune: players who came near it: {rk.Value.Seen.Count}."));
            }
            runes.Clear();
            foreach (var g in flowers.Where(f => f.Wilted).GroupBy(f => f.Owner))
            {
                var o = Utils.GetPlayer(g.Key);
                if (!Alive(o)) continue;
                bool ru = Translator.IsRu(o);
                var rooms = g.Select(f => Utils.RoomName(f.Pos, ru)).Distinct();
                Utils.SendSecret(o, T(ru, "Увяли цветы: ", "Wilted flowers: ") + string.Join(", ", rooms) + T(ru, ". Там кого-то убили.", ". Someone was killed there."));
            }
            flowers.Clear();
            gLast.Clear();
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Extra33.OnMeeting: " + e.Message); }
    }

    public static void FastTick()
    {
        try { if (Utils.AmHost && MeetingHud.Instance == null) HorseTick(Time.time); }
        catch (Exception e) { Plugin.Logger.LogWarning("Extra33.FastTick: " + e.Message); }
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        if (MeetingHud.Instance != null) return;
        try
        {
            float now = Time.time;
            foreach (var t in timers.Where(x => now >= x.At).ToList())
            {
                timers.Remove(t);
                var owner = Utils.GetPlayer(t.Owner);
                if (!Alive(owner)) continue;
                if (t.Kind == "bomb")
                {
                    foreach (var o in Others(owner).Where(x2 => Vector2.Distance(x2.GetTruePosition(), t.Pos) <= 2.5f).ToList()) Strike(owner, o);
                    H(owner, T(Translator.IsRu(owner), "Бомба взорвалась.", "The bomb went off."));
                }
                else if (t.Kind == "daze")
                {
                    var dazed = Utils.GetPlayer(t.Target);
                    if (Alive(dazed)) Roles30.FreezePlayer(dazed, G("DazeSecs"));
                }
                else if (t.Kind.StartsWith("fire:"))
                {
                    string fireRoom = t.Kind.Substring(5);
                    foreach (var burnt in Others(owner).Where(fx => Utils.RoomName(fx.GetTruePosition(), false) == fireRoom).ToList()) Strike(owner, burnt);
                    H(owner, T(Translator.IsRu(owner), "Огонь догорел.", "The fire burned out."));
                }
                else
                {
                    var tg = Utils.GetPlayer(t.Target);
                    if (Alive(tg)) Strike(owner, tg);
                }
            }

            NewtonTick(now);
            foreach (var botKv in RoleState.Assigned.ToList())
            {
                if (botKv.Value.Id != "Bot") continue;
                var botP = Utils.GetPlayer(botKv.Key);
                if (!Alive(botP) || botP.Data.Tasks == null) continue;
                if (!botNext.TryGetValue(botKv.Key, out var botAt)) { botNext[botKv.Key] = now + G("BotEvery"); continue; }
                if (now < botAt) continue;
                botNext[botKv.Key] = now + G("BotEvery");
                var botTask = botP.Data.Tasks.ToArray().FirstOrDefault(bt => !bt.Complete);
                if (botTask == null) continue;
                try { botP.RpcCompleteTask(botTask.Id); }
                catch (Exception be) { Plugin.Logger.LogWarning("Bot: " + be.Message); continue; }
                int botLeft = botP.Data.Tasks.ToArray().Count(bt => !bt.Complete);
                H(botP, T(Translator.IsRu(botP), $"Бот выполнил задание. Осталось: {botLeft}.", $"The bot completed a task. Left: {botLeft}."));
            }

            foreach (var pitItem in pits.ToList())
            {
                var pitOwner = Utils.GetPlayer(pitItem.owner);
                if (!Alive(pitOwner)) { pits.Remove(pitItem); continue; }
                var faller = Others(pitOwner).FirstOrDefault(pv => Vector2.Distance(pv.GetTruePosition(), pitItem.pos) <= 1.2f);
                if (faller == null) continue;
                pits.Remove(pitItem);
                Strike(pitOwner, faller);
            }

            foreach (var kv in berserk.ToList())
            {
                var b = Utils.GetPlayer(kv.Key);
                if (!Alive(b) || now >= kv.Value) { berserk.Remove(kv.Key); if (Alive(b)) H(b, T(Translator.IsRu(b), "Ярость прошла.", "The frenzy is over.")); continue; }
                if (berserkNext.TryGetValue(kv.Key, out var nx) && now < nx) continue;
                var bp = b.GetTruePosition();
                var v = Others(b).FirstOrDefault(o => Vector2.Distance(bp, o.GetTruePosition()) <= 1.3f);
                if (v != null) { Strike(b, v); berserkNext[kv.Key] = now + 0.8f; }
            }

            if (infect.Count > 0)
            {
                foreach (var kv in infect.ToList())
                {
                    var sick = Utils.GetPlayer(kv.Key); var owner = Utils.GetPlayer(kv.Value.owner);
                    if (!Alive(sick) || !Alive(owner)) { infect.Remove(kv.Key); continue; }
                    if (now >= kv.Value.at) { infect.Remove(kv.Key); Strike(owner, sick); continue; }
                    var sp = sick.GetTruePosition();
                    foreach (var o in Others(sick))
                        if (o.PlayerId != owner.PlayerId && !infect.ContainsKey(o.PlayerId) && Vector2.Distance(sp, o.GetTruePosition()) <= 2f)
                            infect[o.PlayerId] = kv.Value;
                }
            }

            foreach (var kv in ambush.ToList())
            {
                var a = Utils.GetPlayer(kv.Key);
                if (!Alive(a) || now >= kv.Value) { ambush.Remove(kv.Key); continue; }
                var ap = a.GetTruePosition();
                var v = Others(a).FirstOrDefault(o => !IsImp(o) && Vector2.Distance(ap, o.GetTruePosition()) <= 2.2f);
                if (v == null) continue;
                Roles30.FreezePlayer(v, 4f);
                ambush.Remove(kv.Key);
                H(a, T(Translator.IsRu(a), $"Засада сработала: {Nm(v)} замер.", $"The ambush worked: {Nm(v)} froze."));
            }

            foreach (var rn2 in runes.Values)
                foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                    if (Alive(o) && Vector2.Distance(o.GetTruePosition(), rn2.Pos) <= 2f) rn2.Seen.Add(o.PlayerId);

            foreach (var kv in RoleState.Assigned.ToList())
            {
                string rid = kv.Value.Id;
                if (rid != "Hermit" && rid != "Intuitive") continue;
                var m = Utils.GetPlayer(kv.Key);
                if (!Alive(m)) continue;
                var mp = m.GetTruePosition();
                bool ru2 = Translator.IsRu(m);
                if (rid == "Hermit")
                {
                    if (hermitDone.Contains(kv.Key)) continue;
                    if (Others(m).Any(o => Vector2.Distance(o.GetTruePosition(), mp) <= 8f)) { aloneSince.Remove(kv.Key); continue; }
                    if (!aloneSince.TryGetValue(kv.Key, out var since)) { aloneSince[kv.Key] = now; continue; }
                    if (now - since < G("HermitTime")) continue;
                    hermitDone.Add(kv.Key); aloneSince.Remove(kv.Key);
                    if (RoleLogic.ShieldArmed.Contains(kv.Key)) continue;
                    RoleLogic.ShieldArmed.Add(kv.Key); RoleLogic.ShieldUsed.Remove(kv.Key);
                    H(m, T(ru2, "Одиночество дало тебе щит.", "Solitude gave you a shield."));
                }
                else
                {
                    if (nextWarn.TryGetValue(kv.Key, out var nw) && now < nw) continue;
                    if (!Others(m).Any(o => IsImp(o) && Vector2.Distance(o.GetTruePosition(), mp) <= 4f)) { closeSince.Remove(kv.Key); continue; }
                    if (!closeSince.TryGetValue(kv.Key, out var cs)) { closeSince[kv.Key] = now; continue; }
                    if (now - cs < 6f) continue;
                    closeSince.Remove(kv.Key); nextWarn[kv.Key] = now + G("IntuitiveWarn");
                    H(m, T(ru2, "Тебе не по себе: рядом кто-то опасный.", "You feel uneasy: someone dangerous is near."));
                }
            }

            if (now >= nextSample)
            {
                nextSample = now + 1f;
                bool anyRounder = RoleState.Assigned.Values.Any(r => r.Id == "Rounder");
                foreach (var pl in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (!Alive(pl)) continue;
                    var pos = pl.GetTruePosition();
                    if (!hist.TryGetValue(pl.PlayerId, out var q)) hist[pl.PlayerId] = q = new Queue<(float, Vector2)>();
                    q.Enqueue((now, pos));
                    while (q.Count > 0 && now - q.Peek().Item1 > 40f) q.Dequeue();
                    string rm = Utils.RoomName(pos, false);
                    if (rm == "hallway") continue;
                    if (lastRoom.TryGetValue(pl.PlayerId, out var lr) && lr != rm) moves[pl.PlayerId] = (moves.TryGetValue(pl.PlayerId, out var mv) ? mv : 0) + 1;
                    lastRoom[pl.PlayerId] = rm;
                    if (!visited.TryGetValue(pl.PlayerId, out var vs)) visited[pl.PlayerId] = vs = new HashSet<string>();
                    vs.Add(rm);
                    if (anyRounder && RoleLogic.Has(pl, "Rounder") && !rounderDone.Contains(pl.PlayerId) && vs.Count >= G("RounderRooms"))
                    {
                        rounderDone.Add(pl.PlayerId);
                        if (!RoleLogic.ShieldArmed.Contains(pl.PlayerId))
                        {
                            RoleLogic.ShieldArmed.Add(pl.PlayerId); RoleLogic.ShieldUsed.Remove(pl.PlayerId);
                            H(pl, T(Translator.IsRu(pl), "Ты обошёл всё вокруг: получен щит.", "You made your rounds: shield granted."));
                        }
                    }
                }
            }

            foreach (var hk in hearth.ToList())
            {
                var owner = Utils.GetPlayer(hk.Key);
                if (!Alive(owner)) { hearth.Remove(hk.Key); continue; }
                bool ownerIn = Utils.RoomName(owner.GetTruePosition(), false) == hk.Value;
                foreach (var o in Others(owner))
                {
                    bool inside = ownerIn && Utils.RoomName(o.GetTruePosition(), false) == hk.Value;
                    if (!inside) { hearthIn.Remove(o.PlayerId); continue; }
                    if (!hearthIn.TryGetValue(o.PlayerId, out var st)) { hearthIn[o.PlayerId] = now; H(o, T(Translator.IsRu(o), "Здесь жарко: очаг хозяина. Уходи из комнаты!", "It is hot here: a hearth. Leave the room!")); continue; }
                    if (now - st >= G("HearthSecs")) { hearthIn.Remove(o.PlayerId); Strike(owner, o); }
                }
            }

            ProcessBoots(now);

            foreach (var va in vacts.Where(x => now >= x.at).ToList())
            {
                vacts.Remove(va);
                var vp = Utils.GetPlayer(va.pid);
                if (!Alive(vp)) continue;
                if (va.kind == "mine")
                {
                    var mo = Utils.GetPlayer(va.owner);
                    if (Alive(mo)) timers.Add(new Timed { Kind = "pounce", Owner = va.owner, Target = va.pid, At = now + 0.5f });
                }
            }

            foreach (var sn in snares.ToList())
            {
                var so = Utils.GetPlayer(sn.owner);
                if (!Alive(so)) { snares.Remove(sn); continue; }
                var victim = Others(so).FirstOrDefault(o => Vector2.Distance(o.GetTruePosition(), sn.pos) <= 1.2f);
                if (victim == null) continue;
                snares.Remove(sn);
                Roles30.FreezePlayer(victim, 5f);
                H(so, T(Translator.IsRu(so), $"Силок сработал: {Nm(victim)}, {Utils.RoomName(sn.pos, true)}.", $"Snare triggered: {Nm(victim)}, {Utils.RoomName(sn.pos, false)}."));
            }

            foreach (var pk in passenger.ToList())
                if (now > pk.Value.until) passenger.Remove(pk.Key);

            foreach (var h in hides.Where(x => now >= x.At).ToList())
            {
                hides.Remove(h);
                if (h.Added) RoleLogic.ShieldArmed.Remove(h.Id);
                var hp = Utils.GetPlayer(h.Id);
                if (Alive(hp)) H(hp, T(Translator.IsRu(hp), "Ты вышел из укрытия.", "You left your hiding place."));
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Extra33.Tick: " + e.Message); }
    }

    // ---------------- роли призраков ----------------
    class GSpec
    {
        public string Id, Ru, En, Color, Team, Cmd, DescRu, DescEn;
        public BoolOpt On; public IntOpt Uses, Cd;
    }
    static readonly List<GSpec> GSpecs = new();
    static readonly Dictionary<byte, string> ghostPick = new();
    static readonly Dictionary<byte, int> gUsed = new();
    static readonly Dictionary<byte, float> gLast = new();

    static void GS(ConfigFile c, string id, string ru, string en, string color, string team, string cmd, int uses, int cd, string dRu, string dEn)
    {
        var g = new GSpec { Id = id, Ru = ru, En = en, Color = color, Team = team, Cmd = cmd, DescRu = dRu, DescEn = dEn };
        g.On = new BoolOpt("Ghost" + id, OptTab.Ghost, true, c);
        g.On.NameRu = ru + " (роль призраков)"; g.On.NameEn = en + " (ghost role)";
        g.On.GroupRu = "Призраки"; g.On.GroupEn = "Ghosts";
        g.On.ShortRu = "Включён"; g.On.ShortEn = "Enabled";
        g.Uses = new IntOpt("Ghost" + id + "Uses", OptTab.Ghost, uses, 1, 10, 1, c);
        g.Uses.NameRu = ru + ": использований за игру"; g.Uses.NameEn = en + ": uses per game";
        g.Uses.GroupRu = "Призраки"; g.Uses.GroupEn = "Ghosts";
        g.Uses.ShortRu = "Использований за игру"; g.Uses.ShortEn = "Uses per game";
        if (cd > 0)
        {
            g.Cd = new IntOpt("Ghost" + id + "Cd", OptTab.Ghost, cd, 5, 120, 5, c);
            g.Cd.NameRu = ru + ": перезарядка (с)"; g.Cd.NameEn = en + ": cooldown (s)";
            g.Cd.GroupRu = "Призраки"; g.Cd.GroupEn = "Ghosts";
            g.Cd.ShortRu = "Перезарядка (с)"; g.Cd.ShortEn = "Cooldown (s)";
        }
        GSpecs.Add(g);
    }

    static void DefineGhosts(ConfigFile c)
    {
        GS(c, "Guardian", "Дух-хранитель", "Guardian Spirit", "#A5D6A7", "crew", "ward", 2, 0,
            "Призрак-мирный. /ward ID - накинуть щит на живого мирного, он отменит первое нападение. Без ID покажет список.",
            "Crewmate ghost. /ward ID - put a shield on a living crewmate, it cancels the first attack. Without an ID it shows the list.");
        GS(c, "Medium", "Медиум", "Medium", "#80DEEA", "crew", "sense", 3, 0,
            "Призрак-мирный. /sense ID - спросить дух погибшего игрока, кем он был: мирным, предателем или нейтралом. Без ID покажет список погибших.",
            "Crewmate ghost. /sense ID - ask the spirit of a dead player who he was: a crewmate, an impostor or a neutral. Without an ID it shows the list of the dead.");
        GS(c, "Phantom", "Фантом", "Phantom", "#EF9A9A", "imp", "scare", 4, 25,
            "Призрак-предатель. /scare ID - напугать живого не-предателя: у него на 3 секунды мигает экран. Без ID покажет список.",
            "Impostor ghost. /scare ID - scare a living non-impostor: his screen flashes for 3 seconds. Without an ID it shows the list.");
        GS(c, "Trickster", "Шутник-призрак", "Ghost Trickster", "#FFCC80", "imp", "swap", 2, 0,
            "Призрак-предатель. /swap ID1 ID2 - поменять местами двух живых не-предателей. Без ID покажет список.",
            "Impostor ghost. /swap ID1 ID2 - swap the places of two living non-impostors. Without IDs it shows the list.");
        GS(c, "Wisp", "Огонёк", "Wisp", "#FFF176", "other", "wisp", 3, 20,
            "Нейтральный призрак. /wisp ID - увести живого игрока огоньком к случайной двери. Без ID покажет список.",
            "Neutral ghost. /wisp ID - lead a living player with a light to a random door. Without an ID it shows the list.");
    }

    public static (string ru, string en, string color, BoolOpt on, Opt[] extras)[] GhostRoles()
        => GSpecs.Select(g => (g.Ru, g.En, g.Color, g.On, g.Cd != null ? new Opt[] { g.Uses, g.Cd } : new Opt[] { g.Uses })).ToArray();

    public static string GhostOf(PlayerControl p) => p != null && ghostPick.TryGetValue(p.PlayerId, out var k) && k != "default" ? k : null;

    static GSpec GetG(string id) => GSpecs.Find(g => g.Id == id);

    // true - игроку выдана новая роль призрака (и сообщение отправлено)
    public static bool TryGhost(PlayerControl p, string team)
    {
        if (!ghostPick.TryGetValue(p.PlayerId, out var pick))
        {
            var pool = GSpecs.Where(g => g.Team == team && g.On.Value).Select(g => g.Id).ToList();
            pool.Add("default");
            pick = pool[rng.Next(pool.Count)];
            ghostPick[p.PlayerId] = pick;
            if (pick != "default")
            {
                var g = GetG(pick);
                bool ru = Translator.IsRu(p);
                Plugin.Logger.LogInfo($"Ghost role: {Nm(p)} -> {g.Id}");
                Utils.SendSecret(p, T(ru, $"Ты {g.Ru} (роль призраков). ", $"You are the {g.En} (ghost role). ") + (ru ? g.DescRu : g.DescEn) + T(ru, $" Использований: {g.Uses.Value}.", $" Uses: {g.Uses.Value}."));
            }
            return pick != "default";
        }
        return pick != "default";
    }

    public static string GhostDesc(string id, bool ru)
    {
        var g = GetG(id);
        return g == null ? null : (ru ? g.Ru : g.En) + " (" + T(ru, "роль призраков", "ghost role") + ")\n" + (ru ? g.DescRu : g.DescEn);
    }

    public static string GhostFind(string q, bool ru)
    {
        q = q.ToLowerInvariant();
        foreach (var g in GSpecs)
            if (g.Ru.ToLowerInvariant().StartsWith(q) || g.En.ToLowerInvariant().StartsWith(q) || g.Id.ToLowerInvariant().StartsWith(q)) return GhostDesc(g.Id, ru);
        return null;
    }

    static PlayerControl Pick(string[] a, int i, Func<PlayerControl, bool> ok)
    {
        if (a == null || a.Length <= i) return null;
        return PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(o => o != null && o.Data != null && ok(o)
            && (o.PlayerId.ToString() == a[i] || string.Equals(o.Data.PlayerName, a[i], StringComparison.OrdinalIgnoreCase)));
    }

    public static bool HandleGhost(PlayerControl me, string cmd, string[] a)
    {
        var g = GSpecs.Find(x => x.Cmd == cmd);
        if (g == null) return false;
        bool ru = Translator.IsRu(me);
        void Tell(string r, string e) => Utils.SendPrivate(me, ru ? r : e);
        if (me == null || me.Data == null || !me.Data.IsDead || !ghostPick.TryGetValue(me.PlayerId, out var k) || k != g.Id)
        { Tell($"Эта команда только для роли призрака «{g.Ru}».", $"This command is only for the ghost role \"{g.En}\"."); return true; }
        if (!g.On.Value) { Tell("Эта роль призраков выключена.", "This ghost role is off."); return true; }
        gUsed.TryGetValue(me.PlayerId, out int n);
        if (n >= g.Uses.Value) { Tell("Использования закончились.", "No uses left."); return true; }
        if (g.Id == "Medium")
        {
            var dead = PlayerControl.AllPlayerControls.ToArray().Where(o => o != null && o.Data != null && o.Data.IsDead && !o.Data.Disconnected && o.PlayerId != me.PlayerId).ToList();
            var tg = Pick(a, 0, o => o.Data.IsDead && o.PlayerId != me.PlayerId);
            if (tg == null)
            {
                if (dead.Count == 0) { Tell("Других погибших нет.", "There are no other dead players."); return true; }
                Tell("Погибшие: " + string.Join(", ", dead.Select(o => $"{o.PlayerId}={Nm(o)}")) + ". Спросить: /sense ID", "The dead: " + string.Join(", ", dead.Select(o => $"{o.PlayerId}={Nm(o)}")) + ". Ask: /sense ID");
                return true;
            }
            string who = IsImp(tg) ? (RoleState.Assigned.TryGetValue(tg.PlayerId, out var nr) && nr.Tab == OptTab.Neutral ? T(ru, "нейтралом", "a neutral") : T(ru, "предателем", "an impostor"))
                       : (RoleState.Assigned.TryGetValue(tg.PlayerId, out var rr) && rr.Tab == OptTab.Neutral ? T(ru, "нейтралом", "a neutral") : T(ru, "мирным", "a crewmate"));
            gUsed[me.PlayerId] = n + 1;
            Tell($"Дух {Nm(tg)} шепчет: «я был {who}». Осталось: {g.Uses.Value - n - 1}.", $"The spirit of {Nm(tg)} whispers: \"I was {who}\". Left: {g.Uses.Value - n - 1}.");
            return true;
        }
        if (MeetingHud.Instance != null || AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started)
        { Tell("Это работает только в раунде, не на собрании.", "This works only during a round, not in a meeting."); return true; }
        if (g.Cd != null && gLast.TryGetValue(me.PlayerId, out var lt) && Time.time - lt < g.Cd.Value)
        { Tell($"Перезарядка: ещё {Mathf.CeilToInt(g.Cd.Value - (Time.time - lt))} с.", $"Cooldown: {Mathf.CeilToInt(g.Cd.Value - (Time.time - lt))} s."); return true; }

        Func<PlayerControl, bool> okT = g.Id == "Guardian" ? o => Alive(o) && !IsImp(o) && !RoleLogic.ShieldArmed.Contains(o.PlayerId)
                                      : g.Id == "Wisp" ? o => Alive(o)
                                      : o => Alive(o) && !IsImp(o);
        var list = PlayerControl.AllPlayerControls.ToArray().Where(okT).ToList();
        var t1 = Pick(a, 0, okT);
        if (t1 == null)
        {
            if (list.Count == 0) { Tell("Подходящих живых игроков нет.", "No suitable living players."); return true; }
            Tell("Кого выбрать: " + string.Join(", ", list.Select(o => $"{o.PlayerId}={Nm(o)}")) + $". Команда: /{g.Cmd} ID" + (g.Id == "Trickster" ? " ID" : ""),
                 "Who to pick: " + string.Join(", ", list.Select(o => $"{o.PlayerId}={Nm(o)}")) + $". Command: /{g.Cmd} ID" + (g.Id == "Trickster" ? " ID" : ""));
            return true;
        }
        string done;
        switch (g.Id)
        {
            case "Guardian":
                RoleLogic.ShieldArmed.Add(t1.PlayerId); RoleLogic.ShieldUsed.Remove(t1.PlayerId);
                Utils.SendSecret(t1, Translator.IsRu(t1) ? "Дух-хранитель накинул на тебя щит от первого нападения!" : "A guardian spirit put a shield on you against the first attack!");
                done = T(ru, $"Щит накинут на {Nm(t1)}.", $"Shield put on {Nm(t1)}.");
                break;
            case "Phantom":
                RoleLogic.FlashPlayer(t1, 3f);
                done = T(ru, $"{Nm(t1)} напуган.", $"{Nm(t1)} is scared.");
                break;
            case "Trickster":
                {
                    var t2 = Pick(a, 1, o => okT(o) && o.PlayerId != t1.PlayerId);
                    if (t2 == null) { Tell("Нужны два разных игрока: /swap ID1 ID2", "Two different players are needed: /swap ID1 ID2"); return true; }
                    var pa = t1.GetTruePosition(); var pb = t2.GetTruePosition();
                    Commands.Tp(t1, pb); Commands.Tp(t2, pa);
                    done = T(ru, $"{Nm(t1)} и {Nm(t2)} поменялись местами.", $"{Nm(t1)} and {Nm(t2)} swapped places.");
                    break;
                }
            default:
                {
                    if (!RandomDoor(out var pos)) { Tell("Огонёк погас.", "The light went out."); return true; }
                    Commands.Tp(t1, pos);
                    done = T(ru, $"{Nm(t1)} увёл огонёк.", $"A light led {Nm(t1)} away.");
                    break;
                }
        }
        gUsed[me.PlayerId] = n + 1; gLast[me.PlayerId] = Time.time;
        Tell(done + $" Осталось: {g.Uses.Value - n - 1}.", done + $" Left: {g.Uses.Value - n - 1}.");
        return true;
    }
}
