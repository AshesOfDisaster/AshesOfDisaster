using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// 40 новых ролей: по 10 мирных, предателей, нейтралов и мистиков.
public static partial class Extra33
{
    class Spot { public string Kind; public byte Owner, Target; public Vector2 Pos; public float Until, Next; }
    static readonly List<Spot> spots = new();
    static readonly Dictionary<byte, float> scapegoats = new();
    static readonly Dictionary<byte, int> reapKills = new();
    static readonly Dictionary<byte, float> reapLast = new();
    static float lastDeathAt = -999f;
    static int deathCount;

    static void Clear40() { spots.Clear(); scapegoats.Clear(); reapKills.Clear(); reapLast.Clear(); lastDeathAt = -999f; deathCount = 0; }

    static void Death40(PlayerControl killer, PlayerControl target)
    {
        lastDeathAt = Time.time; deathCount++;
        if (target == null) return;
        foreach (var kv in scapegoats.ToList())
        {
            if (Time.time > kv.Value) { scapegoats.Remove(kv.Key); continue; }
            var sg = Utils.GetPlayer(kv.Key);
            if (!Alive(sg) || sg.PlayerId == target.PlayerId) continue;
            scapegoats.Remove(kv.Key);
            foreach (var o in Others(sg)) Utils.SendSecret(o, T(Translator.IsRu(o), $"Рядом с телом заметили: {Nm(sg)}.", $"{Nm(sg)} was seen near the body."));
        }
    }

    static void Tick40(float now)
    {
        foreach (var sp in spots.ToList())
        {
            var owner = Utils.GetPlayer(sp.Owner);
            if (!Alive(owner) || now > sp.Until) { spots.Remove(sp); continue; }
            bool ru = Translator.IsRu(owner);
            if (sp.Kind == "alarm")
            {
                var o = Others(owner).FirstOrDefault(x => Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 2f);
                if (o == null) continue;
                spots.Remove(sp);
                H(owner, T(ru, $"Сигналка сработала: рядом с меткой {Nm(o)}.", $"The tripwire went off: {Nm(o)} is by the mark."));
            }
            else if (sp.Kind == "stash")
            {
                var o = Others(owner).FirstOrDefault(x => Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 1.5f);
                if (o == null) continue;
                spots.Remove(sp);
                RoleLogic.ShieldArmed.Add(o.PlayerId); RoleLogic.ShieldUsed.Remove(o.PlayerId);
                Utils.SendSecret(o, T(Translator.IsRu(o), "Ты нашёл тайник: у тебя щит!", "You found a stash: you have a shield!"));
                H(owner, T(ru, $"Твой тайник нашёл {Nm(o)}.", $"{Nm(o)} found your stash."));
            }
            else if (sp.Kind == "tag")
            {
                var tg = Utils.GetPlayer(sp.Target);
                if (!Alive(tg)) { spots.Remove(sp); continue; }
                if (now < sp.Next) continue;
                sp.Next = now + 5f;
                var me = owner.GetTruePosition(); var tp = tg.GetTruePosition();
                H(owner, T(ru, $"{Nm(tg)}: {Where(true, me, tp)}, {Mathf.RoundToInt(Vector2.Distance(me, tp))} м.", $"{Nm(tg)}: {Where(false, me, tp)}, {Mathf.RoundToInt(Vector2.Distance(me, tp))} m."));
            }
        }
    }

    static string Dir8(Vector2 from, Vector2 to)
    {
        var d = to - from;
        if (d.magnitude < 0.5f) return "прямо здесь";
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        string[] n = { "восток", "северо-восток", "север", "северо-запад", "запад", "юго-запад", "юг", "юго-восток" };
        return n[((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8];
    }

    static string Dir8En(Vector2 from, Vector2 to)
    {
        var d = to - from;
        if (d.magnitude < 0.5f) return "right here";
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        string[] n = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        return n[((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8];
    }

    static string Where(bool ru, Vector2 from, Vector2 to) => ru ? Dir8(from, to) : Dir8En(from, to);

    static bool IsCrewSide(PlayerControl o)
        => o != null && !IsImp(o) && RoleState.Assigned.TryGetValue(o.PlayerId, out var r) && r.Tab != OptTab.Neutral && r.Tab != OptTab.Impostor;

    static bool NeutralKill(PlayerControl p, bool ru, float range, Func<PlayerControl, bool> ok, string noneRu, string noneEn)
    {
        var t = Nearest(p, range, ok);
        if (t == null) { H(p, T(ru, noneRu, noneEn)); return false; }
        return Strike(p, t);
    }

    static int Around(PlayerControl p, float r) => Others(p).Count(o => Vector2.Distance(o.GetTruePosition(), p.GetTruePosition()) <= r);

    static void Define40()
    {
        const OptTab N = OptTab.Neutral, M = OptTab.Mystic, C = OptTab.Crewmate, I = OptTab.Impostor;

        // ---------------- Мирные ----------------
        S("Lantern", "Фонарщик", "Lantern", "#FFD54F", C, 20, 0, "питомец: сколько людей рядом", "pet: how many people are near",
            "Мирный. Показывает, сколько живых игроков сейчас в радиусе 7 метров от тебя. Перезарядка 20 с. [Питомец]",
            "Crewmate. Shows how many living players are within 7 meters of you right now. Cooldown 20 s. [Pet]",
            (p, ru) => { int n = Around(p, 7f); H(p, T(ru, $"Рядом с тобой игроков: {n}.", $"Players near you: {n}.")); return true; });
        S("Accountant", "Счетовод", "Accountant", "#A1887F", C, 30, 0, "питомец: сколько живых и тел", "pet: how many alive and bodies",
            "Мирный. Показывает, сколько игроков живо и сколько тел лежит на карте непоказанными. Перезарядка 30 с. [Питомец]",
            "Crewmate. Shows how many players are alive and how many bodies lie on the map. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                int alive = PlayerControl.AllPlayerControls.ToArray().Count(Alive);
                int b = bodies.Count(x => !eaten.Contains(x.id));
                H(p, T(ru, $"Живых: {alive}. Тел на карте: {b}.", $"Alive: {alive}. Bodies on the map: {b}."));
                return true;
            });
        S("Compass", "Компас", "Compass", "#26A69A", C, 40, 4, "питомец: где ближайшее тело", "pet: where the nearest body is",
            "Мирный. Показывает направление на ближайшее тело и расстояние до него. Использований: 4, перезарядка 40 с. [Питомец]",
            "Crewmate. Shows the direction and distance to the nearest body. Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = bodies.Where(b => !eaten.Contains(b.id)).ToList();
                if (list.Count == 0) { H(p, T(ru, "Тел на карте нет.", "No bodies on the map.")); return false; }
                var nb = list.OrderBy(b => Vector2.Distance(b.pos, me)).First();
                H(p, T(ru, $"Ближайшее тело: {Where(true, me, nb.pos)}, примерно {Mathf.RoundToInt(Vector2.Distance(me, nb.pos))} м.", $"Nearest body: {Where(false, me, nb.pos)}, about {Mathf.RoundToInt(Vector2.Distance(me, nb.pos))} m."));
                return true;
            });
        S("Herald", "Вестник", "Herald", "#FFB74D", C, 90, 2, "питомец: позвать всех к себе", "pet: call everyone to you",
            "Мирный. Всем живым приходит сообщение, что вестник зовёт собраться, с направлением на него. Использований: 2, перезарядка 90 с. [Питомец]",
            "Crewmate. Every living player gets a message that the herald calls them, with the direction to him. Uses: 2, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                foreach (var o in Others(p))
                    Utils.SendSecret(o, T(Translator.IsRu(o), $"Вестник зовёт всех: он на {Where(true, o.GetTruePosition(), me)}.", $"The Herald calls everyone: he is to the {Where(false, o.GetTruePosition(), me)}."));
                H(p, T(ru, "Все получили твой зов.", "Everyone got your call."));
                return true;
            });
        S("Pacifier", "Миротворец", "Pacifier", "#81C784", C, 50, 3, "питомец: заморозить ближайшего на 4 с", "pet: freeze the nearest for 4 s",
            "Мирный. Замораживает ближайшего игрока (до 2,5 м) на 4 секунды. Использований: 3, перезарядка 50 с. [Питомец]",
            "Crewmate. Freezes the nearest player (within 2.5 m) for 4 seconds. Uses: 3, cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                Roles30.FreezePlayer(t, 4f);
                H(p, T(ru, $"{Nm(t)} заморожен на 4 секунды.", $"{Nm(t)} is frozen for 4 seconds."));
                return true;
            });
        S("Investigator", "Следователь", "Investigator", "#7986CB", C, 35, 3, "питомец: был ли рядом с телом", "pet: was he near a body",
            "Мирный. Проверяет ближайшего игрока (до 2,5 м): стоит ли он рядом с телом (до 5 м). Это только улика, не приговор. Использований: 3, перезарядка 35 с. [Питомец]",
            "Crewmate. Checks the nearest player (within 2.5 m): is he next to a body (within 5 m). Only a clue, not a verdict. Uses: 3, cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool near = bodies.Any(b => !eaten.Contains(b.id) && Vector2.Distance(b.pos, t.GetTruePosition()) <= 5f);
                H(p, T(ru, near ? $"{Nm(t)} стоит рядом с телом." : $"{Nm(t)} не рядом с телом.", near ? $"{Nm(t)} is next to a body." : $"{Nm(t)} is not near a body."));
                return true;
            });
        S("Seer", "Вещун", "Seer", "#9575CD", C, 60, 3, "питомец: назвать точно мирного", "pet: name a sure crewmate",
            "Мирный. Называет случайного живого игрока, который точно на стороне мирных. Использований: 3, перезарядка 60 с. [Питомец]",
            "Crewmate. Names a random living player who is surely on the crew side. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var l = Others(p).Where(IsCrewSide).ToList();
                if (l.Count == 0) { H(p, T(ru, "Некого назвать.", "Nobody to name.")); return false; }
                var t = l[rng.Next(l.Count)];
                H(p, T(ru, $"{Nm(t)} точно мирный.", $"{Nm(t)} is surely crew."));
                return true;
            });
        S("Tripwire", "Сигналка", "Tripwire", "#EF9A9A", C, 40, 3, "питомец: поставить сигналку", "pet: set a tripwire",
            "Мирный. Питомец ставит невидимую сигналку на твоём месте (до 90 секунд). Первый, кто подойдёт к ней ближе 2 м, назовётся тебе по имени. Использований: 3, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet sets an invisible tripwire where you stand (up to 90 seconds). The first one to come within 2 m of it is named to you. Uses: 3, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                spots.Add(new Spot { Kind = "alarm", Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + 90f });
                H(p, T(ru, "Сигналка поставлена на 90 секунд.", "Tripwire set for 90 seconds."));
                return true;
            });
        S("Stash", "Тайник", "Stash", "#FFCC80", C, 60, 2, "питомец: спрятать щит", "pet: hide a shield",
            "Мирный. Питомец прячет на твоём месте тайник со щитом (до 120 секунд). Первый, кто подойдёт ближе 1,5 м, получает щит, а ты узнаёшь, кто. Использований: 2, перезарядка 60 с. [Питомец]",
            "Crewmate. The pet hides a shield stash where you stand (up to 120 seconds). The first one to come within 1.5 m gets the shield and you learn who. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                spots.Add(new Spot { Kind = "stash", Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + 120f });
                H(p, T(ru, "Тайник спрятан на 120 секунд.", "Stash hidden for 120 seconds."));
                return true;
            });
        S("Chrono", "Хронометр", "Chronometer", "#B0BEC5", C, 20, 0, "питомец: сколько прошло с последней смерти", "pet: time since the last death",
            "Мирный. Показывает, сколько секунд прошло с последней смерти игрока. Перезарядка 20 с. [Питомец]",
            "Crewmate. Shows how many seconds have passed since a player last died. Cooldown 20 s. [Pet]",
            (p, ru) =>
            {
                if (lastDeathAt < 0) { H(p, T(ru, "Ещё никто не погиб.", "Nobody has died yet.")); return true; }
                int sec = Mathf.RoundToInt(Time.time - lastDeathAt);
                H(p, T(ru, $"С последней смерти прошло {sec} с.", $"{sec} s since the last death."));
                return true;
            });

        // ---------------- Предатели ----------------
        S("Discharger", "Разрядник", "Discharger", "#FFEE58", I, 60, 0, "питомец: сорвать щиты вокруг", "pet: break the shields around",
            "Предатель. Питомец снимает щиты со всех не-предателей в радиусе 6 м. Перезарядка 60 с. [Питомец]",
            "Impostor. The pet strips the shields off every non-impostor within 6 m. Cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && RoleLogic.ShieldArmed.Contains(o.PlayerId) && Vector2.Distance(o.GetTruePosition(), me) <= 6f).ToList();
                if (l.Count == 0) { H(p, T(ru, "Щитов рядом нет.", "No shields nearby.")); return false; }
                foreach (var o in l) RoleLogic.ShieldArmed.Remove(o.PlayerId);
                H(p, T(ru, $"Щитов снято: {l.Count}.", $"Shields removed: {l.Count}."));
                return true;
            });
        S("Handout", "Подачка", "Handout", "#A5D6A7", I, 45, 0, "питомец: ложный щит", "pet: a fake shield",
            "Предатель. Питомец говорит ближайшему не-предателю (до 2,5 м), что у него щит. Щита на самом деле нет. Перезарядка 45 с. [Питомец]",
            "Impostor. The pet tells the nearest non-impostor (within 2.5 m) that he has a shield. There is no real shield. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                Utils.SendSecret(t, T(Translator.IsRu(t), "Ты получил щит!", "You got a shield!"));
                H(p, T(ru, $"{Nm(t)} думает, что у него щит.", $"{Nm(t)} thinks he has a shield."));
                return true;
            });
        S("Shadower", "Следящий", "Shadower", "#546E7A", I, 60, 0, "питомец: метка слежки на 40 с", "pet: a tracking tag for 40 s",
            "Предатель. Питомец ставит метку слежки на ближайшего не-предателя (до 3 м). 40 секунд каждые 5 секунд ты видишь, где он (направление и расстояние). Перезарядка 60 с. [Питомец]",
            "Impostor. The pet tags the nearest non-impostor (within 3 m). For 40 seconds every 5 seconds you see where he is (direction and distance). Cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                spots.RemoveAll(x => x.Kind == "tag" && x.Owner == p.PlayerId);
                spots.Add(new Spot { Kind = "tag", Owner = p.PlayerId, Target = t.PlayerId, Until = Time.time + 40f, Next = 0f });
                H(p, T(ru, $"Метка слежки на {Nm(t)}.", $"Tracking tag on {Nm(t)}."));
                return true;
            });
        S("Armor", "Броня", "Armor", "#90A4AE", I, 90, 3, "питомец: щит себе", "pet: a shield for yourself",
            "Предатель. Питомец надевает на тебя щит от одного нападения, если щита ещё нет. Использований: 3, перезарядка 90 с. [Питомец]",
            "Impostor. The pet puts a one-attack shield on you if you have none. Uses: 3, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                if (RoleLogic.ShieldArmed.Contains(p.PlayerId)) { H(p, T(ru, "Щит у тебя уже есть.", "You already have a shield.")); return false; }
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                H(p, T(ru, "Щит надет.", "Shield on."));
                return true;
            });
        S("Siren", "Сирена", "Siren", "#F06292", I, 70, 0, "питомец: ложный крик", "pet: a false scream",
            "Предатель. Все не-предатели в радиусе 12 м слышат, что где-то на случайной стороне кричат. Так можно увести их от тебя. Перезарядка 70 с. [Питомец]",
            "Impostor. Every non-impostor within 12 m hears a scream somewhere in a random direction. A way to lead them away from you. Cooldown 70 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), me) <= 12f).ToList();
                if (l.Count == 0) { H(p, T(ru, "В радиусе 12 м никого нет.", "Nobody within 12 m.")); return false; }
                if (!RandomDoor(out var fake)) fake = me + new Vector2(5f, 0f);
                foreach (var o in l)
                    Utils.SendSecret(o, T(Translator.IsRu(o), $"Где-то на {Where(true, o.GetTruePosition(), fake)} слышен крик!", $"A scream somewhere to the {Where(false, o.GetTruePosition(), fake)}!"));
                H(p, T(ru, $"Крик услышали: {l.Count}.", $"Players who heard the scream: {l.Count}."));
                return true;
            });
        S("Scarecrow", "Пугало", "Scarecrow", "#8D6E63", I, 45, 0, "питомец: напугать всех рядом", "pet: scare everyone near",
            "Предатель. Все не-предатели в радиусе 7 м получают сообщение, что у них за спиной кто-то стоит. Перезарядка 45 с. [Питомец]",
            "Impostor. Every non-impostor within 7 m gets a message that somebody stands behind them. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), me) <= 7f).ToList();
                if (l.Count == 0) { H(p, T(ru, "В радиусе 7 м никого нет.", "Nobody within 7 m.")); return false; }
                foreach (var o in l) Utils.SendSecret(o, T(Translator.IsRu(o), "Тебе кажется, что за спиной кто-то стоит...", "You feel like somebody stands behind you..."));
                H(p, T(ru, $"Напугано игроков: {l.Count}.", $"Players scared: {l.Count}."));
                return true;
            });
        S("Headhunter", "Головорез", "Headhunter", "#D32F2F", I, 25, 0, "питомец: сколько целей вокруг", "pet: how many targets around",
            "Предатель. Показывает, сколько не-предателей в радиусе 8 м, и расстояние до ближайшего. Перезарядка 25 с. [Питомец]",
            "Impostor. Shows how many non-impostors are within 8 m and the distance to the nearest. Cooldown 25 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), me) <= 8f).ToList();
                if (l.Count == 0) { H(p, T(ru, "В радиусе 8 м никого нет.", "Nobody within 8 m.")); return true; }
                float d = l.Min(o => Vector2.Distance(o.GetTruePosition(), me));
                H(p, T(ru, $"Целей рядом: {l.Count}, ближайшая в {d:0.#} м.", $"Targets near: {l.Count}, the nearest is {d:0.#} m away."));
                return true;
            });
        S("Infiltrator", "Лазутчик", "Infiltrator", "#78909C", I, 30, 0, "питомец: где ближайший мирный", "pet: where the nearest crewmate is",
            "Предатель. Показывает направление и расстояние до ближайшего не-предателя. Перезарядка 30 с. [Питомец]",
            "Impostor. Shows the direction and distance to the nearest non-impostor. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 999f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Целей нет.", "No targets.")); return false; }
                var me = p.GetTruePosition(); var tp = t.GetTruePosition();
                H(p, T(ru, $"Ближайшая цель: {Where(true, me, tp)}, примерно {Mathf.RoundToInt(Vector2.Distance(me, tp))} м.", $"Nearest target: {Where(false, me, tp)}, about {Mathf.RoundToInt(Vector2.Distance(me, tp))} m."));
                return true;
            });
        S("Scapegoat", "Козёл отпущения", "Scapegoat", "#BCAAA4", I, 80, 0, "питомец: назначить виноватого", "pet: pick a scapegoat",
            "Предатель. Питомец метит ближайшего не-предателя (до 3 м). Если в ближайшие 30 секунд кого-то убьют, всем живым придёт сообщение, что рядом с телом заметили именно его. Перезарядка 80 с. [Питомец]",
            "Impostor. The pet marks the nearest non-impostor (within 3 m). If anyone is killed in the next 30 seconds, every living player is told that he was seen near the body. Cooldown 80 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !IsImp(o) && !scapegoats.ContainsKey(o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                scapegoats[t.PlayerId] = Time.time + 30f;
                H(p, T(ru, $"{Nm(t)} назначен виноватым на 30 секунд.", $"{Nm(t)} is the scapegoat for 30 seconds."));
                return true;
            });
        S("Tally", "Счётчик убийств", "Tally", "#E57373", I, 30, 0, "питомец: счёт смертей", "pet: the death count",
            "Предатель. Показывает, сколько игроков погибло за игру и сколько секунд прошло с последней смерти. Перезарядка 30 с. [Питомец]",
            "Impostor. Shows how many players died during the game and how many seconds passed since the last death. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                string ago = lastDeathAt < 0 ? "-" : Mathf.RoundToInt(Time.time - lastDeathAt).ToString();
                H(p, T(ru, $"Смертей: {deathCount}. С последней: {ago} с.", $"Deaths: {deathCount}. Since the last one: {ago} s."));
                return true;
            });

        // ---------------- Нейтралы (настоящие убийцы) ----------------
        S("Hangman", "Висельник", "Hangman", "#6D4C41", N, 40, 0, "питомец: убить, если вы вдвоём", "pet: kill if you are alone together",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если рядом (до 7 м) нет других свидетелей. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if nobody else is within 7 m. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                var me = p.GetTruePosition();
                if (Others(p).Any(o => o.PlayerId != t.PlayerId && Vector2.Distance(o.GetTruePosition(), me) <= 7f)) { H(p, T(ru, "Рядом есть свидетели.", "There are witnesses nearby.")); return false; }
                return Strike(p, t);
            });
        S("Crowdbane", "Толпобой", "Crowdbane", "#EF5350", N, 45, 0, "питомец: убить в толпе", "pet: kill in a crowd",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если вокруг (до 6 м) есть ещё хотя бы двое. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if at least two more are within 6 m. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                if (Around(p, 6f) < 3) { H(p, T(ru, "Нужна толпа: хотя бы трое рядом.", "You need a crowd: at least three nearby.")); return false; }
                return Strike(p, t);
            });
        S("Bloodsucker", "Кровосос", "Bloodsucker", "#B71C1C", N, 50, 0, "питомец: убить и получить щит", "pet: kill and gain a shield",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), и ты получаешь щит. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m) and you gain a shield. [Pet]",
            (p, ru) =>
            {
                bool ok = NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
                if (ok) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
                return ok;
            });
        S("Runaway", "Беглец", "Runaway", "#00897B", N, 55, 0, "питомец: убить и сбежать", "pet: kill and run",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м) и сразу переносит тебя к случайной двери. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m) and moves you to a random door. [Pet]",
            (p, ru) =>
            {
                bool ok = NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
                if (ok && RandomDoor(out var pos)) Commands.Tp(p, pos);
                return ok;
            });
        S("Sniper", "Снайпер", "Sniper", "#37474F", N, 90, 2, "питомец: убить в дальнем радиусе", "pet: a long range kill",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока в радиусе 5 м без промаха. Всего два выстрела за игру, перезарядка 90 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player within 5 m without a miss. Only two shots per game, cooldown 90 s. [Pet]",
            (p, ru) => NeutralKill(p, ru, 5f, o => true, "Никого нет ближе 5 м.", "Nobody within 5 m."));
        S("Judge", "Судья", "Judge", "#5D4037", N, 40, 0, "питомец: казнить подозрительного", "pet: execute a suspicious one",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если он стоит рядом с телом (до 5 м). [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if he stands next to a body (within 5 m). [Pet]",
            (p, ru) => NeutralKill(p, ru, 2f, o => bodies.Any(b => !eaten.Contains(b.id) && Vector2.Distance(b.pos, o.GetTruePosition()) <= 5f), "Рядом нет подозрительного (у тела, до 2 м).", "No suspicious player nearby (next to a body, within 2 m)."));
        S("Vulture", "Стервятник", "Vulture", "#4E342E", N, 25, 0, "питомец: убить сразу после смерти", "pet: kill right after a death",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только в течение 30 секунд после чьей-то смерти. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only within 30 seconds after somebody's death. [Pet]",
            (p, ru) =>
            {
                if (lastDeathAt < 0 || Time.time - lastDeathAt > 30f) { H(p, T(ru, "Нужна свежая смерть (не старше 30 секунд).", "You need a fresh death (not older than 30 seconds).")); return false; }
                return NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
            });
        S("Finalist", "Финалист", "Finalist", "#FFC107", N, 20, 0, "питомец: убить, когда игроков мало", "pet: kill when few are left",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только когда в живых осталось не больше шести. Зато перезарядка короткая. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only when no more than six are alive. The cooldown is short. [Pet]",
            (p, ru) =>
            {
                if (PlayerControl.AllPlayerControls.ToArray().Count(Alive) > 6) { H(p, T(ru, "Рано: в живых больше шести.", "Too early: more than six are alive.")); return false; }
                return NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
            });
        S("Trigger", "Курок", "Trigger", "#FF7043", N, 20, 3, "питомец: три быстрых убийства", "pet: three quick kills",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м). Всего три убийства за игру, зато перезарядка 20 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m). Only three kills per game, but the cooldown is 20 s. [Pet]",
            (p, ru) => NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m."));
        S("Breaker", "Пробойник", "Breaker", "#455A64", N, 55, 0, "питомец: убить сквозь щит", "pet: kill through a shield",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), даже если у него есть щит: щит просто сгорает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m) even if he has a shield: the shield just burns out. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                RoleLogic.ShieldArmed.Remove(t.PlayerId);
                return Strike(p, t);
            });
        foreach (var id in new[] { "Hangman", "Crowdbane", "Bloodsucker", "Runaway", "Sniper", "Judge", "Vulture", "Finalist", "Trigger", "Breaker" })
            RoleLogic.RealImpNeutrals.Add(id);

        // ---------------- Мистики ----------------
        S("Pulse", "Пульс", "Pulse", "#AB47BC", M, 25, 0, "питомец: расстояние до ближайшего", "pet: distance to the nearest",
            "Мистик. Показывает расстояние до ближайшего живого игрока. Перезарядка 25 с. [Питомец]",
            "Mystic. Shows the distance to the nearest living player. Cooldown 25 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 999f, o => true);
                if (t == null) { H(p, T(ru, "Больше никого нет.", "Nobody else is here.")); return false; }
                float d = Vector2.Distance(p.GetTruePosition(), t.GetTruePosition());
                H(p, T(ru, $"Ближайший игрок в {d:0.#} м.", $"The nearest player is {d:0.#} m away."));
                return true;
            });
        S("Medium", "Медиум", "Medium", "#B39DDB", M, 45, 0, "питомец: последние погибшие", "pet: the last dead",
            "Мистик. Называет двух последних погибших игроков. Перезарядка 45 с. [Питомец]",
            "Mystic. Names the last two players who died. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var l = bodies.Skip(Math.Max(0, bodies.Count - 2)).Select(b => Utils.GetPlayer(b.id)).Where(x => x != null && x.Data != null).Select(Nm).ToList();
                if (l.Count == 0) { H(p, T(ru, "Пока никто не погиб.", "Nobody has died yet.")); return false; }
                H(p, T(ru, "Последние погибшие: " + string.Join(", ", l) + ".", "The last dead: " + string.Join(", ", l) + "."));
                return true;
            });
        S("Omen", "Предвестник", "Omen", "#5E35B1", M, 40, 4, "питомец: светлый или тёмный", "pet: light or dark",
            "Мистик. Смотрит на ближайшего игрока (до 2,5 м): светлый (не предатель) или тёмный. Ответ верен в 70% случаев. Использований: 4, перезарядка 40 с. [Питомец]",
            "Mystic. Looks at the nearest player (within 2.5 m): light (not an impostor) or dark. The answer is right 70% of the time. Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool dark = IsImp(t);
                if (rng.Next(100) >= 70) dark = !dark;
                H(p, T(ru, dark ? $"{Nm(t)}: тёмный знак." : $"{Nm(t)}: светлый знак.", dark ? $"{Nm(t)}: a dark sign." : $"{Nm(t)}: a light sign."));
                return true;
            });
        S("Palmist", "Хиромант", "Palmist", "#CE93D8", M, 40, 3, "питомец: есть ли у роли способность", "pet: does the role have an ability",
            "Мистик. Смотрит на ближайшего игрока (до 2,5 м) и говорит, есть ли у его роли активная способность. Использований: 3, перезарядка 40 с. [Питомец]",
            "Mystic. Looks at the nearest player (within 2.5 m) and tells whether his role has an active ability. Uses: 3, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool act = RoleState.Assigned.TryGetValue(t.PlayerId, out var r) && (!ById.TryGetValue(r.Id, out var sp) || sp.Fn != null);
                H(p, T(ru, act ? $"У {Nm(t)} есть способность." : $"У {Nm(t)} нет способности.", act ? $"{Nm(t)} has an ability." : $"{Nm(t)} has no ability."));
                return true;
            });
        S("Clockmaker", "Часовщик", "Clockmaker", "#9FA8DA", M, 120, 1, "питомец: сбросить перезарядки", "pet: reset cooldowns",
            "Мистик. Сбрасывает перезарядки всех твоих способностей. Один раз за игру. [Питомец]",
            "Mystic. Resets the cooldowns of all your abilities. Once per game. [Pet]",
            (p, ru) =>
            {
                var k = ":" + p.PlayerId;
                foreach (var key in last.Keys.Where(x => x.EndsWith(k) && !x.StartsWith("Clockmaker")).ToList()) last.Remove(key);
                H(p, T(ru, "Перезарядки сброшены.", "Cooldowns are reset."));
                return true;
            });
        S("Fortuna", "Фортуна", "Fortuna", "#FFD740", M, 90, 2, "питомец: шанс на щит", "pet: a chance for a shield",
            "Мистик. С шансом 50% даёт тебе щит. При неудаче перезарядка всё равно уходит. Использований: 2, перезарядка 90 с. [Питомец]",
            "Mystic. A 50% chance to give you a shield. On a miss the cooldown is still spent. Uses: 2, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                if (rng.Next(100) < 50)
                {
                    RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                    H(p, T(ru, "Удача: у тебя щит.", "Luck: you have a shield."));
                }
                else H(p, T(ru, "Удача отвернулась.", "Luck looked away."));
                return true;
            });
        S("Census", "Перепись", "Census", "#90CAF9", M, 60, 3, "питомец: сколько нейтралов в живых", "pet: how many neutrals are alive",
            "Мистик. Говорит, сколько нейтралов сейчас живо. Использований: 3, перезарядка 60 с. [Питомец]",
            "Mystic. Tells how many neutrals are alive right now. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                int n = PlayerControl.AllPlayerControls.ToArray().Count(o => Alive(o) && RoleState.Assigned.TryGetValue(o.PlayerId, out var r) && r.Tab == OptTab.Neutral);
                H(p, T(ru, $"Нейтралов в живых: {n}.", $"Neutrals alive: {n}."));
                return true;
            });
        S("Hunch", "Чутьё", "Hunch", "#7E57C2", M, 30, 0, "питомец: есть ли тёмный рядом", "pet: is a dark one near",
            "Мистик. Говорит, есть ли в радиусе 6 м предатель или нейтрал-убийца. Имя не называет. Перезарядка 30 с. [Питомец]",
            "Mystic. Tells whether an impostor or a neutral killer is within 6 m. Does not name him. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                bool dark = Others(p).Any(o => Vector2.Distance(o.GetTruePosition(), me) <= 6f && !IsCrewSide(o));
                H(p, T(ru, dark ? "Чутьё: рядом кто-то тёмный." : "Чутьё: рядом спокойно.", dark ? "Hunch: somebody dark is near." : "Hunch: all is calm nearby."));
                return true;
            });
        S("Bodyecho", "Эхо тела", "Body Echo", "#B39DDB", M, 45, 0, "питомец: где лежит последнее тело", "pet: where the last body lies",
            "Мистик. Называет комнату, где лежит последнее тело. Перезарядка 45 с. [Питомец]",
            "Mystic. Names the room where the last body lies. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var list = bodies.Where(b => !eaten.Contains(b.id)).ToList();
                if (list.Count == 0) { H(p, T(ru, "Тел на карте нет.", "No bodies on the map.")); return false; }
                string room = Utils.RoomLabel(Utils.RoomName(list[list.Count - 1].pos, false), ru);
                H(p, T(ru, $"Последнее тело лежит: {room}.", $"The last body lies: {room}."));
                return true;
            });
        S("Lot", "Жребий", "Lot", "#CE93D8", M, 75, 2, "питомец: три имени, среди них тёмный", "pet: three names, a dark one among them",
            "Мистик. Называет троих игроков, и среди них есть хотя бы один не-мирный (предатель или нейтрал), если такие остались. Использований: 2, перезарядка 75 с. [Питомец]",
            "Mystic. Names three players, and at least one of them is not crew (an impostor or a neutral) if any are left. Uses: 2, cooldown 75 s. [Pet]",
            (p, ru) =>
            {
                var all = Others(p);
                if (all.Count < 3) { H(p, T(ru, "Слишком мало игроков.", "Too few players.")); return false; }
                var dark = all.Where(o => !IsCrewSide(o)).OrderBy(o => rng.Next()).Take(1).ToList();
                var rest = all.Where(o => !dark.Contains(o)).OrderBy(o => rng.Next()).Take(3 - dark.Count);
                var pick = dark.Concat(rest).OrderBy(o => rng.Next()).Select(Nm);
                H(p, T(ru, "Жребий: " + string.Join(", ", pick) + ".", "Lot: " + string.Join(", ", pick) + "."));
                return true;
            });
    }
}
