using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// Версия 4.0.0: десять новых ролей, все работают от питомца.
// Мирные: Магнит, Радар, Страж, Снеговик. Предатели: Крюк, Толкач, Оглушитель.
// Мистики: Меняла, Тасовщик, Искривитель.
public static partial class Extra33
{
    static bool Free(PlayerControl o) => o != null && !o.inVent;

    // Сдвигает игрока на dist метров вдоль dir, но не сквозь стену: если на полной длине
    // стена мешает, пробует по полметра короче.
    static bool Slide(PlayerControl t, Vector2 dir, float dist)
    {
        var from = t.GetTruePosition();
        for (float d = dist; d >= 0.5f; d -= 0.5f)
        {
            var dest = from + dir * d;
            if (Commands.WallBetween(from, dest, true)) continue;
            Commands.Tp(t, dest);
            return true;
        }
        return false;
    }

    static Vector2 Dir(Vector2 from, Vector2 to)
    {
        var diff = to - from;
        return diff.magnitude < 0.05f ? new Vector2(1f, 0f) : diff.normalized;
    }

    static void Define42()
    {
        const OptTab C = OptTab.Crewmate, I = OptTab.Impostor, M = OptTab.Mystic;

        // ---------------- Мирные ----------------
        S("Magnet", "Магнит", "Magnet", "#EF9A9A", C, 30, 0, "питомец: притянуть игроков к себе", "pet: pull players toward you",
            "Мирный. Питомец включает магнит: все игроки в радиусе 4 м подтягиваются к тебе на 3 м (стена останавливает раньше, вплотную никого не прижимает). Перезарядка 30 с. Радиус и силу можно менять в настройках. [Питомец]",
            "Crewmate. The pet turns the magnet on: every player within 4 m is pulled 3 m toward you (a wall stops them earlier, nobody ends up on top of you). Cooldown 30 s. Radius and force can be changed in the options. [Pet]",
            (p, ru) =>
            {
                float mgR = G("MagnetRadius"), mgPull = G("MagnetPull");
                var mgPos = p.GetTruePosition(); int mgCount = 0;
                foreach (var mgT in Others(p).Where(Free))
                {
                    var mgFrom = mgT.GetTruePosition();
                    float mgDist = Vector2.Distance(mgFrom, mgPos);
                    if (mgDist > mgR) continue;
                    float mgStep = Mathf.Min(mgPull, mgDist - 1f);
                    if (mgStep < 0.5f) continue;
                    if (Slide(mgT, Dir(mgFrom, mgPos), mgStep)) { mgCount++; H(mgT, T(Translator.IsRu(mgT), "Тебя притянул магнит!", "A magnet pulled you in!")); }
                }
                if (mgCount == 0) { H(p, T(ru, $"Некого притянуть (до {mgR:0.#} м).", $"Nobody to pull within {mgR:0.#} m.")); return false; }
                H(p, T(ru, $"Притянуто игроков: {mgCount}.", $"Players pulled in: {mgCount}."));
                return true;
            });
        E("Magnet", "MagnetRadius", 4, 2, 8, 1, "Радиус магнита (м)", "Magnet radius (m)");
        E("Magnet", "MagnetPull", 3, 1, 6, 1, "Сила: на сколько метров тянет", "Force: how many metres it pulls");

        S("Radar", "Радар", "Radar", "#80CBC4", C, 20, 6, "питомец: узнать, сколько игроков рядом", "pet: learn how many players are near",
            "Мирный. Питомец включает радар: ты узнаёшь, сколько живых игроков в радиусе 6 м, и в какой комнате находится ближайший к тебе игрок на всей карте. Использований 6, перезарядка 20 с. [Питомец]",
            "Crewmate. The pet turns the radar on: you learn how many living players are within 6 m and which room the player closest to you is in anywhere on the map. Uses 6, cooldown 20 s. [Pet]",
            (p, ru) =>
            {
                var rdNear = Nearest(p, 999f, o => true);
                if (rdNear == null) { H(p, T(ru, "Радар никого не видит.", "The radar sees nobody.")); return false; }
                int rdCount = Around(p, 6f);
                string rdRoom = Utils.RoomName(rdNear.GetTruePosition(), ru);
                H(p, T(ru, $"Радар: в радиусе 6 м игроков {rdCount}. Ближайший к тебе сейчас: {rdRoom}.", $"Radar: {rdCount} players within 6 m. The closest to you is in: {rdRoom}."));
                return true;
            });

        S("Guardian", "Страж", "Guardian", "#FFE082", C, 60, 2, "питомец: дать щит ближайшему игроку", "pet: give the nearest player a shield",
            "Мирный. Питомец накрывает щитом ближайшего игрока (до 2 м, не себя): щит спасает от одного убийства. Если у него уже есть щит, питомец ничего не тратит. Использований 2, перезарядка 60 с. [Питомец]",
            "Crewmate. The pet covers the nearest player (within 2 m, not yourself) with a shield: the shield saves from one kill. If he already has a shield nothing is spent. Uses 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var gdT = Nearest(p, 2f, o => Free(o));
                if (gdT == null) { H(p, T(ru, "Рядом никого (до 2 м).", "Nobody within 2 m.")); return false; }
                if (RoleLogic.ShieldArmed.Contains(gdT.PlayerId)) { H(p, T(ru, $"У {Nm(gdT)} уже есть щит.", $"{Nm(gdT)} already has a shield.")); return false; }
                RoleLogic.ShieldArmed.Add(gdT.PlayerId);
                RoleLogic.ShieldUsed.Remove(gdT.PlayerId);
                Utils.SendSecret(gdT, T(Translator.IsRu(gdT), "Страж накрыл тебя щитом.", "A Guardian covered you with a shield."));
                H(p, T(ru, $"Щит получил {Nm(gdT)}.", $"{Nm(gdT)} got a shield."));
                return true;
            });

        S("Snowman", "Снеговик", "Snowman", "#E1F5FE", C, 40, 0, "питомец: заморозить ближайшего игрока", "pet: freeze the nearest player",
            "Мирный. Питомец замораживает ближайшего игрока (до 3 м): он стоит на месте несколько секунд (по умолчанию 3). Подходит, чтобы остановить убегающего предателя. Перезарядка 40 с. [Питомец]",
            "Crewmate. The pet freezes the nearest player (within 3 m): he stands still for a few seconds (3 by default). Good for stopping an impostor who runs away. Cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var snT = Nearest(p, 3f, o => Free(o));
                if (snT == null) { H(p, T(ru, "Рядом никого (до 3 м).", "Nobody within 3 m.")); return false; }
                Roles30.FreezePlayer(snT, G("SnowmanSec"));
                Utils.SendSecret(snT, T(Translator.IsRu(snT), "Тебя заморозил Снеговик!", "A Snowman froze you!"), 1, false);
                H(p, T(ru, $"{Nm(snT)} заморожен на {G("SnowmanSec")} с.", $"{Nm(snT)} is frozen for {G("SnowmanSec")} s."));
                return true;
            });
        E("Snowman", "SnowmanSec", 3, 1, 8, 1, "Длительность заморозки (сек)", "Freeze duration (sec)");

        // ---------------- Предатели ----------------
        S("Hook", "Крюк", "Hook", "#B0BEC5", I, 35, 0, "питомец: притянуть ближайшего к себе", "pet: pull the nearest player to you",
            "Предатель. Питомец бросает крюк в ближайшего не-предателя (до 5 м) и подтягивает его к тебе почти вплотную. Стена останавливает рывок раньше. Перезарядка 35 с. [Питомец]",
            "Impostor. The pet throws a hook at the nearest non-impostor (within 5 m) and drags him almost right next to you. A wall stops the pull earlier. Cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var hkT = Nearest(p, 5f, o => Free(o) && !IsImp(o));
                if (hkT == null) { H(p, T(ru, "Крюку не за что зацепиться (до 5 м).", "Nothing for the hook to catch (within 5 m).")); return false; }
                var hkFrom = hkT.GetTruePosition(); var hkTo = p.GetTruePosition();
                float hkStep = Vector2.Distance(hkFrom, hkTo) - 1f;
                if (hkStep < 0.5f || !Slide(hkT, Dir(hkFrom, hkTo), hkStep)) { H(p, T(ru, "Игрок уже слишком близко или за стеной.", "The player is too close already or behind a wall.")); return false; }
                H(hkT, T(Translator.IsRu(hkT), "Тебя дёрнул крюк!", "A hook yanked you!"));
                H(p, T(ru, $"{Nm(hkT)} подтянут к тебе.", $"{Nm(hkT)} was dragged to you."));
                return true;
            });

        S("Shover", "Толкач", "Shover", "#A1887F", I, 30, 0, "питомец: оттолкнуть ближайшего", "pet: shove the nearest player away",
            "Предатель. Питомец отталкивает ближайшего не-предателя (до 2 м) от тебя на 5 м, стена останавливает раньше. Хорошо отводит свидетеля от тела. Перезарядка 30 с. [Питомец]",
            "Impostor. The pet shoves the nearest non-impostor (within 2 m) 5 m away from you, a wall stops him earlier. Good for sending a witness away from a body. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var shT = Nearest(p, 2f, o => Free(o) && !IsImp(o));
                if (shT == null) { H(p, T(ru, "Рядом нет подходящего игрока (до 2 м).", "No suitable player within 2 m.")); return false; }
                if (!Slide(shT, Dir(p.GetTruePosition(), shT.GetTruePosition()), 5f)) { H(p, T(ru, "Мешает стена, оттолкнуть не вышло.", "A wall is in the way, the shove failed.")); return false; }
                H(shT, T(Translator.IsRu(shT), "Тебя кто-то толкнул!", "Somebody shoved you!"));
                H(p, T(ru, $"{Nm(shT)} отброшен.", $"{Nm(shT)} was shoved away."));
                return true;
            });

        S("Stunner", "Оглушитель", "Stunner", "#FF8A80", I, 50, 3, "питомец: оглушить игроков рядом", "pet: stun nearby players",
            "Предатель. Питомец оглушает всех не-предателей в радиусе 3 м: они замирают на 2 секунды. Времени хватит, чтобы уйти. Использований 3, перезарядка 50 с. [Питомец]",
            "Impostor. The pet stuns every non-impostor within 3 m: they freeze for 2 seconds. Use the time to walk away. Uses 3, cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                var stPos = p.GetTruePosition(); int stCount = 0;
                foreach (var stT in Others(p).Where(o => Free(o) && !IsImp(o) && Vector2.Distance(o.GetTruePosition(), stPos) <= 3f))
                {
                    Roles30.FreezePlayer(stT, 2f);
                    Utils.SendSecret(stT, T(Translator.IsRu(stT), "Тебя оглушили!", "You were stunned!"), 1, false);
                    stCount++;
                }
                if (stCount == 0) { H(p, T(ru, "Рядом никого (до 3 м).", "Nobody within 3 m.")); return false; }
                H(p, T(ru, $"Оглушено игроков: {stCount}.", $"Players stunned: {stCount}."));
                return true;
            });

        // ---------------- Мистики ----------------
        S("Exchanger", "Меняла", "Exchanger", "#CE93D8", M, 45, 4, "питомец: поменяться местами с ближайшим", "pet: swap places with the nearest player",
            "Мистик. Питомец меняет тебя местами с ближайшим игроком (до 3 м): ты оказываешься там, где стоял он, а он там, где стоял ты. Можно спастись от убийцы или внезапно оказаться у цели. Использований 4, перезарядка 45 с. [Питомец]",
            "Mystic. The pet swaps you with the nearest player (within 3 m): you land where he stood and he lands where you stood. Can save you from a killer or put you suddenly next to a target. Uses 4, cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var exT = Nearest(p, 3f, o => Free(o));
                if (exT == null) { H(p, T(ru, "Рядом никого (до 3 м).", "Nobody within 3 m.")); return false; }
                var exMine = p.GetTruePosition(); var exHis = exT.GetTruePosition();
                Commands.Tp(p, exHis); Commands.Tp(exT, exMine);
                H(exT, T(Translator.IsRu(exT), "Тебя поменяли местами с кем-то!", "You were swapped with somebody!"));
                H(p, T(ru, $"Ты поменялся местами с {Nm(exT)}.", $"You swapped places with {Nm(exT)}."));
                return true;
            });

        S("Shuffler", "Тасовщик", "Shuffler", "#9FA8DA", M, 60, 3, "питомец: поменять местами двух игроков рядом", "pet: swap two nearby players",
            "Мистик. Питомец меняет местами двух ближайших к тебе игроков (оба не дальше 6 м). Оба игрока получат сообщение о смене места. Использований 3, перезарядка 60 с. [Питомец]",
            "Mystic. The pet swaps the two players closest to you (both within 6 m). They are told about it. Uses 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var shPos = p.GetTruePosition();
                var shTwo = Others(p).Where(o => Free(o) && Vector2.Distance(o.GetTruePosition(), shPos) <= 6f)
                                     .OrderBy(o => Vector2.Distance(o.GetTruePosition(), shPos)).Take(2).ToList();
                if (shTwo.Count < 2) { H(p, T(ru, "Нужно двое игроков не дальше 6 м.", "You need two players within 6 m.")); return false; }
                var shA = shTwo[0].GetTruePosition(); var shB = shTwo[1].GetTruePosition();
                Commands.Tp(shTwo[0], shB); Commands.Tp(shTwo[1], shA);
                foreach (var shT in shTwo) H(shT, T(Translator.IsRu(shT), "Тебя поменяли местами с кем-то!", "You were swapped with somebody!"));
                H(p, T(ru, $"Поменялись местами: {Nm(shTwo[0])} и {Nm(shTwo[1])}.", $"Swapped places: {Nm(shTwo[0])} and {Nm(shTwo[1])}."));
                return true;
            });

        S("Warper", "Искривитель", "Warper", "#7E57C2", M, 70, 3, "питомец: отправить себя и ближайшего к дверям", "pet: send yourself and the nearest player to doors",
            "Мистик. Питомец искривляет пространство: ты и ближайший игрок (до 3 м) оказываетесь у двух разных случайных дверей на карте. Если рядом никого нет, телепортируешься только ты. Использований 3, перезарядка 70 с. [Питомец]",
            "Mystic. The pet bends space: you and the nearest player (within 3 m) end up at two different random doors on the map. If nobody is near, only you are teleported. Uses 3, cooldown 70 s. [Pet]",
            (p, ru) =>
            {
                if (!RandomDoor(out var wpMine)) { H(p, T(ru, "На этой карте нет дверей.", "This map has no doors.")); return false; }
                var wpT = Nearest(p, 3f, o => Free(o));
                Vector2 wpHis = default;
                bool wpBoth = wpT != null && RandomDoor(out wpHis);
                Commands.Tp(p, wpMine);
                if (wpBoth)
                {
                    Commands.Tp(wpT, wpHis);
                    H(wpT, T(Translator.IsRu(wpT), "Пространство исказилось, тебя унесло!", "Space bent and carried you away!"));
                    H(p, T(ru, $"Ты и {Nm(wpT)} разбросаны по дверям.", $"You and {Nm(wpT)} were scattered to doors."));
                }
                else H(p, T(ru, "Тебя перенесло к случайной двери.", "You were moved to a random door."));
                return true;
            });
    }
}
