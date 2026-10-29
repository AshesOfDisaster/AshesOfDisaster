using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// 40 новых ролей: по 10 мирных, предателей, нейтралов и мистиков.
public static partial class Extra33
{
    static readonly Dictionary<byte, (Vector2 pos, float at)> anchors = new();

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
        S("Shieldbearer", "Щитоносец", "Shieldbearer", "#4FC3F7", C, 60, 2, "питомец: щит ближайшему", "pet: a shield for the nearest",
            "Мирный. Даёт щит ближайшему игроку (до 2,5 м), если у него его ещё нет. Щит гасит одно нападение. Использований: 2, перезарядка 60 с. [Питомец]",
            "Crewmate. Gives a shield to the nearest player (within 2.5 m) who has none. A shield absorbs one attack. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !RoleLogic.ShieldArmed.Contains(o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет игрока без щита.", "Nobody without a shield nearby.")); return false; }
                RoleLogic.ShieldArmed.Add(t.PlayerId); RoleLogic.ShieldUsed.Remove(t.PlayerId);
                Utils.SendSecret(t, T(Translator.IsRu(t), "Щитоносец дал тебе щит.", "A Shieldbearer gave you a shield."));
                H(p, T(ru, $"Щит получил {Nm(t)}.", $"{Nm(t)} got a shield."));
                return true;
            });
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
                {
                    bool r2 = Translator.IsRu(o);
                    Utils.SendSecret(o, T(r2, $"Вестник зовёт всех: он на {Where(true, o.GetTruePosition(), me)}.", $"The Herald calls everyone: he is to the {Where(false, o.GetTruePosition(), me)}."));
                }
                H(p, T(ru, "Все получили твой зов.", "Everyone got your call."));
                return true;
            });
        S("Exchanger", "Обменщик", "Exchanger", "#BA68C8", C, 45, 3, "питомец: поменяться местами", "pet: swap places",
            "Мирный. Меняет тебя местами с ближайшим игроком (до 3 м). Хорошо, чтобы вырваться из угла. Использований: 3, перезарядка 45 с. [Питомец]",
            "Crewmate. Swaps you with the nearest player (within 3 m). Good to get out of a corner. Uses: 3, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                var a = p.GetTruePosition(); var b = t.GetTruePosition();
                Commands.Tp(p, b); Commands.Tp(t, a);
                H(p, T(ru, $"Ты поменялся местами с {Nm(t)}.", $"You swapped places with {Nm(t)}."));
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
            "Мирный. Проверяет ближайшего игрока (до 2,5 м): был ли он рядом с телом (до 5 м). Это только улика, не приговор. Использований: 3, перезарядка 35 с. [Питомец]",
            "Crewmate. Checks the nearest player (within 2.5 m): is he near a body (within 5 m). Only a clue, not a verdict. Uses: 3, cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool near = bodies.Any(b => !eaten.Contains(b.id) && Vector2.Distance(b.pos, t.GetTruePosition()) <= 5f);
                H(p, T(ru, near ? $"{Nm(t)} стоит рядом с телом." : $"{Nm(t)} не рядом с телом.", near ? $"{Nm(t)} is next to a body." : $"{Nm(t)} is not near a body."));
                return true;
            });
        S("Courier", "Курьер", "Courier", "#FF8A65", C, 45, 3, "питомец: рывок к случайной двери", "pet: dash to a random door",
            "Мирный. Переносит тебя к случайной двери на карте. Побег или быстрый путь. Использований: 3, перезарядка 45 с. [Питомец]",
            "Crewmate. Moves you to a random door on the map. An escape or a shortcut. Uses: 3, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                if (!RandomDoor(out var pos)) return false;
                Commands.Tp(p, pos);
                H(p, T(ru, "Ты прибежал к двери.", "You dashed to a door."));
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

        // ---------------- Предатели ----------------
        S("Freezer", "Заморозчик", "Freezer", "#4DD0E1", I, 45, 0, "питомец: заморозить на 5 с", "pet: freeze for 5 s",
            "Предатель. Замораживает ближайшего не-предателя (до 2,5 м) на 5 секунд. Перезарядка 45 с. [Питомец]",
            "Impostor. Freezes the nearest non-impostor (within 2.5 m) for 5 seconds. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                Roles30.FreezePlayer(t, 5f);
                H(p, T(ru, $"{Nm(t)} заморожен.", $"{Nm(t)} is frozen."));
                return true;
            });
        S("Teleporter", "Телепортёр", "Teleporter", "#7E57C2", I, 50, 0, "питомец: прыжок к случайному игроку", "pet: jump to a random player",
            "Предатель. Переносит тебя к случайному живому игроку. Перезарядка 50 с. [Питомец]",
            "Impostor. Moves you to a random living player. Cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                var l = Others(p);
                if (l.Count == 0) return false;
                var t = l[rng.Next(l.Count)];
                Commands.Tp(p, t.GetTruePosition());
                H(p, T(ru, $"Ты оказался рядом с {Nm(t)}.", $"You appeared next to {Nm(t)}."));
                return true;
            });
        S("Dragger", "Тащила", "Dragger", "#8D6E63", I, 40, 0, "питомец: притянуть ближайшего", "pet: pull the nearest to you",
            "Предатель. Притягивает к тебе ближайшего не-предателя (в радиусе 6 м). Перезарядка 40 с. [Питомец]",
            "Impostor. Pulls the nearest non-impostor (within 6 m) to you. Cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 6f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "В радиусе 6 м никого нет.", "Nobody within 6 m.")); return false; }
                Commands.Tp(t, p.GetTruePosition());
                H(p, T(ru, $"{Nm(t)} притянут к тебе.", $"{Nm(t)} was pulled to you."));
                return true;
            });
        S("Pusher", "Толкатель", "Pusher", "#FF7043", I, 45, 0, "питомец: отправить ближайшего к двери", "pet: send the nearest to a door",
            "Предатель. Выбрасывает ближайшего не-предателя (до 2,5 м) к случайной двери. Перезарядка 45 с. [Питомец]",
            "Impostor. Throws the nearest non-impostor (within 2.5 m) to a random door. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !IsImp(o));
                if (t == null || !RandomDoor(out var pos)) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                Commands.Tp(t, pos);
                H(p, T(ru, $"{Nm(t)} отброшен к двери.", $"{Nm(t)} was thrown to a door."));
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
        S("Eclipse", "Затмитель", "Eclipse", "#455A64", I, 60, 0, "питомец: ослепить всех рядом", "pet: blind everyone near",
            "Предатель. Гасит свет в глазах у всех не-предателей в радиусе 8 м на 5 секунд. Перезарядка 60 с. [Питомец]",
            "Impostor. Blinds every non-impostor within 8 m for 5 seconds. Cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), me) <= 8f).ToList();
                if (l.Count == 0) { H(p, T(ru, "В радиусе 8 м никого нет.", "Nobody within 8 m.")); return false; }
                foreach (var o in l) RoleLogic.FlashPlayer(o, 5f);
                H(p, T(ru, $"Ослеплено игроков: {l.Count}.", $"Players blinded: {l.Count}."));
                return true;
            });
        S("Anchor", "Якорь", "Anchor", "#5C6BC0", I, 20, 0, "питомец: метка и возврат", "pet: set a mark and return",
            "Предатель. Первое нажатие ставит метку, второе возвращает тебя к ней (метка живёт 90 секунд). Перезарядка 20 с. [Питомец]",
            "Impostor. The first press sets a mark, the second returns you to it (the mark lasts 90 seconds). Cooldown 20 s. [Pet]",
            (p, ru) =>
            {
                if (anchors.TryGetValue(p.PlayerId, out var a) && Time.time - a.at < 90f)
                {
                    anchors.Remove(p.PlayerId);
                    Commands.Tp(p, a.pos);
                    H(p, T(ru, "Ты вернулся к метке.", "You returned to the mark."));
                    return true;
                }
                anchors[p.PlayerId] = (p.GetTruePosition(), Time.time);
                H(p, T(ru, "Метка поставлена. Нажми ещё раз, чтобы вернуться.", "Mark set. Press again to return."));
                return true;
            });
        S("Disruptor", "Ловкач", "Disruptor", "#EC407A", I, 70, 0, "питомец: поменять двух игроков местами", "pet: swap two players",
            "Предатель. Меняет местами двух случайных не-предателей. Перезарядка 70 с. [Питомец]",
            "Impostor. Swaps two random non-impostors. Cooldown 70 s. [Pet]",
            (p, ru) =>
            {
                var l = Others(p).Where(o => !IsImp(o)).OrderBy(o => rng.Next()).Take(2).ToList();
                if (l.Count < 2) { H(p, T(ru, "Не хватает игроков.", "Not enough players.")); return false; }
                var a = l[0].GetTruePosition(); var b = l[1].GetTruePosition();
                Commands.Tp(l[0], b); Commands.Tp(l[1], a);
                H(p, T(ru, $"{Nm(l[0])} и {Nm(l[1])} поменялись местами.", $"{Nm(l[0])} and {Nm(l[1])} swapped places."));
                return true;
            });
        S("Swapper2", "Подмен", "Swapper", "#26C6DA", I, 45, 0, "питомец: поменяться местами", "pet: swap places",
            "Предатель. Меняет тебя местами с ближайшим не-предателем (до 3 м). Перезарядка 45 с. [Питомец]",
            "Impostor. Swaps you with the nearest non-impostor (within 3 m). Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                var a = p.GetTruePosition(); var b = t.GetTruePosition();
                Commands.Tp(p, b); Commands.Tp(t, a);
                H(p, T(ru, $"Ты поменялся местами с {Nm(t)}.", $"You swapped places with {Nm(t)}."));
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
        S("Lancer", "Копейщик", "Lancer", "#FFA726", N, 55, 0, "питомец: убить издалека", "pet: kill from afar",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока в радиусе 3,5 м. Перезарядка 55 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player within 3.5 m. Cooldown 55 s. [Pet]",
            (p, ru) => NeutralKill(p, ru, 3.5f, o => true, "Никого нет ближе 3,5 м.", "Nobody within 3.5 m."));
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
        S("Butcher", "Мясник", "Butcher", "#880E4F", N, 70, 0, "питомец: убить двоих рядом", "pet: kill two nearby",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает сразу двоих ближайших игроков (до 2 м), если рядом их двое. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the two nearest players (within 2 m) at once, if there are two. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => Vector2.Distance(o.GetTruePosition(), me) <= 2f).OrderBy(o => Vector2.Distance(o.GetTruePosition(), me)).Take(2).ToList();
                if (l.Count < 2) { H(p, T(ru, "Нужны двое ближе 2 м.", "You need two players within 2 m.")); return false; }
                bool any = false;
                foreach (var t in l) any |= Strike(p, t);
                return any;
            });
        S("Sniper", "Снайпер", "Sniper", "#37474F", N, 90, 2, "питомец: убить в дальнем радиусе", "pet: a long range kill",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока в радиусе 5 м. Всего два выстрела за игру, перезарядка 90 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player within 5 m. Only two shots per game, cooldown 90 s. [Pet]",
            (p, ru) => NeutralKill(p, ru, 5f, o => true, "Никого нет ближе 5 м.", "Nobody within 5 m."));
        S("Traitorhunter", "Охотник на предателей", "Traitor Hunter", "#1565C0", N, 35, 0, "питомец: убить только предателя", "pet: kill only an impostor",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего предателя (до 3 м). Мирных он не трогает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest impostor (within 3 m). Crewmates are left alone. [Pet]",
            (p, ru) => NeutralKill(p, ru, 3f, IsImp, "Рядом нет предателя (до 3 м).", "No impostor nearby (within 3 m)."));
        S("Crewbane", "Мироед", "Crewbane", "#2E7D32", N, 30, 0, "питомец: убить мирного", "pet: kill a crewmate",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего мирного (до 2 м). Предателей он не трогает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest crewmate (within 2 m). Impostors are left alone. [Pet]",
            (p, ru) => NeutralKill(p, ru, 2f, o => !IsImp(o), "Рядом нет мирного (до 2 м).", "No crewmate nearby (within 2 m)."));
        S("Gambler", "Азартный", "Gambler", "#F9A825", N, 25, 0, "питомец: убить с шансом", "pet: kill with a chance",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м) с шансом 60%. При неудаче перезарядка всё равно уходит. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m) with a 60% chance. On a miss the cooldown is still spent. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                if (rng.Next(100) < 60) return Strike(p, t);
                H(p, T(ru, "Не повезло: удар мимо.", "Bad luck: you missed."));
                return true;
            });

        // ---------------- Мистики ----------------
        S("Oracle", "Оракул", "Oracle", "#7E57C2", M, 60, 3, "питомец: один предатель или больше", "pet: one impostor or more",
            "Мистик. Говорит, остался ли в живых один предатель или больше одного. Использований: 3, перезарядка 60 с. [Питомец]",
            "Mystic. Tells whether one impostor or more than one is still alive. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                int n = PlayerControl.AllPlayerControls.ToArray().Count(o => Alive(o) && IsImp(o));
                H(p, T(ru, n <= 1 ? "Знамение: предатель остался один." : "Знамение: предателей больше одного.", n <= 1 ? "Omen: one impostor is left." : "Omen: there is more than one impostor."));
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
        S("Windseer", "Ветрогон", "Windseer", "#80CBC4", M, 35, 0, "питомец: где ближайший игрок", "pet: where the nearest player is",
            "Мистик. Показывает направление на ближайшего живого игрока. Перезарядка 35 с. [Питомец]",
            "Mystic. Shows the direction to the nearest living player. Cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 999f, o => true);
                if (t == null) { H(p, T(ru, "Больше никого нет.", "Nobody else is here.")); return false; }
                H(p, T(ru, $"Ближайший игрок: {Where(true, p.GetTruePosition(), t.GetTruePosition())}.", $"The nearest player: {Where(false, p.GetTruePosition(), t.GetTruePosition())}."));
                return true;
            });
        S("Soulcounter", "Счётчик душ", "Soul Counter", "#B0BEC5", M, 30, 0, "питомец: сколько душ улетело", "pet: how many souls left",
            "Мистик. Показывает, сколько игроков уже погибло. Перезарядка 30 с. [Питомец]",
            "Mystic. Shows how many players have already died. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                int dead = PlayerControl.AllPlayerControls.ToArray().Count(o => o != null && o.Data != null && o.Data.IsDead);
                H(p, T(ru, $"Погибших игроков: {dead}.", $"Players who died: {dead}."));
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
        S("Sleepwalker", "Лунатик", "Sleepwalker", "#7986CB", M, 50, 3, "питомец: прогулка к случайной двери", "pet: a walk to a random door",
            "Мистик. Переносит тебя к случайной двери на карте. Использований: 3, перезарядка 50 с. [Питомец]",
            "Mystic. Moves you to a random door on the map. Uses: 3, cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                if (!RandomDoor(out var pos)) return false;
                Commands.Tp(p, pos);
                H(p, T(ru, "Ты очнулся у двери.", "You woke up by a door."));
                return true;
            });
    }
}
