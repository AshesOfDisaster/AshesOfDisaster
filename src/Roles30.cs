using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

// 30 дополнительных ролей: по 10 мирных / предателей / нейтралов.
// Мирные: Архивариус, Часовой, Статистик, Меченый, Диспетчер, Заступник, Миротворец, Следователь, Бродяга, Везунчик.
// Предатели: Минёр, Тасовщик, Затмитель, Возвращенец, Упырь, Ревизор, Приманщик, Взломщик, Сплетник, Глушитель.
// Нейтралы: Мародёр, Авантюрист, Затворник, Турист, Мститель, Торговец, Лунатик, Крот, Гипнотизёр, Мошенник.
public static class Roles30
{
    static readonly System.Random rng = new();
    static readonly Dictionary<string, IntOpt> X = new();
    static int G(string k) => X.TryGetValue(k, out var o) ? o.Value : 0;

    // ---------------------------------------------------------------- Описание и настройки
    public static void Define(ConfigFile c)
    {
        void R(string id, string ru, string en, string color, OptTab tab, string dru, string den) => Opts.AddRole(c, id, ru, en, color, tab, dru, den);
        IntOpt E(string role, string key, OptTab tab, int def, int min, int max, int step, string ru, string en)
        { var o = Opts.Extra(role, new IntOpt(key, tab, def, min, max, step, c), ru, en); X[key] = o; return o; }
        const OptTab C = OptTab.Crewmate, I = OptTab.Impostor, N = OptTab.Neutral;

        // ---------- Мирные
        R("Archivist", "Архивариус", "Archivist", "#A1887F", C,
            "Член экипажа. В начале каждого собрания получает архив: в какой комнате умер каждый, кто погиб с прошлого собрания.",
            "A crewmate. At the start of each meeting receives an archive: in which room each player who died since the last meeting was killed.");
        R("Sentry", "Часовой", "Sentry", "#FFD54F", C,
            "Член экипажа. Питомец ставит пост (растяжку): первый, кто пройдёт рядом, будет назван Часовому, пост исчезает. Посты пропадают на собрании. Нужен питомец!",
            "A crewmate. Pet places a tripwire post: the first player to pass near it is named to the Sentry and the post vanishes. Posts vanish at a meeting. A pet is required!");
        R("Statistician", "Статистик", "Statistician", "#90CAF9", C,
            "Член экипажа. На собрании командой /ab узнаёт, сколько предателей (для сервера) ещё живо. Число проверок ограничено.",
            "A crewmate. In a meeting /ab shows how many impostors (as the server sees them) are still alive. The number of checks is limited.");
        R("Marked", "Меченый", "Marked", "#EF9A9A", C,
            "Член экипажа. Если его убили, на следующем собрании всем мирным приходит имя убийцы — но с настраиваемым шансом имя ложное.",
            "A crewmate. If he is killed, at the next meeting all crewmates get the name of the killer - but with a configurable chance the name is false.");
        R("Dispatcher", "Диспетчер", "Dispatcher", "#80CBC4", C,
            "Член экипажа. Питомец показывает комнаты, где сейчас больше всего живых игроков. Перезарядка настраивается. Нужен питомец!",
            "A crewmate. Pet shows the rooms with the most living players right now. Cooldown is configurable. A pet is required!");
        R("Intercessor", "Заступник", "Intercessor", "#FFAB91", C,
            "Член экипажа. Питомец рядом с игроком берёт его под защиту: если подопечного попытаются убить, нападение отменяется, а погибает сам Заступник. Подопечный один, новый заменяет прежнего. Метка пропадает на собрании. Нужен питомец!",
            "A crewmate. Pet next to a player takes him under protection: if the ward is attacked the attack is cancelled and the Intercessor dies instead. One ward at a time. The ward is dropped at a meeting. A pet is required!");
        R("Peacemaker", "Миротворец", "Peacemaker", "#F8BBD0", C,
            "Член экипажа. Питомец создаёт вокруг него зону мира на несколько секунд: пока она действует, предатели рядом не могут никого убить. Число использований ограничено. Нужен питомец!",
            "A crewmate. Pet creates a peace zone around him for a few seconds: while it lasts impostors nearby can't kill anyone. Number of uses is limited. A pet is required!");
        R("Investigator", "Следователь", "Investigator", "#9575CD", C,
            "Член экипажа. На собрании командой /ab ID узнаёт, в какой комнате находился игрок в момент последнего убийства. Число проверок ограничено.",
            "A crewmate. In a meeting /ab ID shows which room a player was in at the moment of the last kill. The number of checks is limited.");
        R("Wanderer", "Бродяга", "Wanderer", "#AED581", C,
            "Член экипажа. Питомец переносит его к случайному живому игроку (это может оказаться и опасно). Перезарядка настраивается. Нужен питомец!",
            "A crewmate. Pet teleports him to a random living player (which may be dangerous). Cooldown is configurable. A pet is required!");
        R("Lucky", "Везунчик", "Lucky", "#FFF176", C,
            "Член экипажа. Нападение на него с настраиваемым шансом проваливается. Число спасений за игру ограничено.",
            "A crewmate. An attack on him fails with a configurable chance. The number of saves per game is limited.");

        R("Eyewitness", "Очевидец", "Eyewitness", "#FFE082", C,
            "Член экипажа. Если рядом с ним (в радиусе, по умолчанию 7 м) кого-то убили, он узнаёт имя убийцы — но с настраиваемым шансом имя ложное.",
            "A crewmate. If someone is killed near him (within the radius, 7 m by default) he learns the killer's name - but with a configurable chance the name is false.");
        R("Forensic", "Криминалист", "Forensic", "#B39DDB", C,
            "Член экипажа. Питомец рядом с трупом (до 2 м): узнаёт, сколько секунд назад тот погиб, и смывает краску Маляра, чтобы труп можно было доложить. Съеденное тело не вернуть. Число осмотров ограничено. Нужен питомец!",
            "A crewmate. Pet next to a body (within 2 m): learns how many seconds ago it died and washes off the Painter's paint so the body can be reported. An eaten body can't be restored. Number of exams is limited. A pet is required!");
        R("Aide", "Помощник", "Aide", "#A5D6A7", C,
            "Член экипажа. Питомец рядом с членом экипажа (до 3 м) засчитывает ему одно невыполненное задание. Число помощей и перезарядка настраиваются. Нужен питомец!",
            "A crewmate. Pet next to a crewmate (within 3 m) completes one of his unfinished tasks. Number of helps and cooldown are configurable. A pet is required!");

        // ---------- Предатели
        R("Minelayer", "Минёр", "Minelayer", "#E65100", I,
            "Предатель. Питомец ставит мину: первый член экипажа, наступивший на неё, погибает (щит спасает). Мины пропадают на собрании. Число мин настраивается. Нужен питомец!",
            "Impostor. Pet places a mine: the first crewmate to step on it dies (a shield saves him). Mines vanish at a meeting. Number of mines is configurable. A pet is required!");
        R("Shuffler", "Тасовщик", "Shuffler", "#BA68C8", I,
            "Предатель. Питомец меняет местами двух случайных членов экипажа. Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet swaps the positions of two random crewmates. Cooldown is configurable. A pet is required!");
        R("Eclipser", "Затмитель", "Eclipser", "#37474F", I,
            "Предатель. Питомец гасит свет (саботаж электричества), если сейчас нет другого саботажа. Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet turns the lights off (electrical sabotage) if no other sabotage is active. Cooldown is configurable. A pet is required!");
        R("Returner", "Возвращенец", "Returner", "#26A69A", I,
            "Предатель. Питомец ставит метку; после каждого твоего убийства тебя возвращает к метке (метка остаётся). Метка сбрасывается на собрании. Нужен питомец!",
            "Impostor. Pet sets a mark; after each of your kills you are returned to the mark (it stays). The mark is cleared at a meeting. A pet is required!");
        R("Ghoul", "Упырь", "Ghoul", "#6D4C41", I,
            "Предатель. С настраиваемым шансом жертва съедается целиком: тело нельзя доложить.",
            "Impostor. With a configurable chance the victim is eaten whole: the body can't be reported.");
        R("Auditor", "Ревизор", "Auditor", "#FFCA28", I,
            "Предатель. Питомец показывает, сколько заданий осталось у экипажа и у кого их больше всего. Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet shows how many tasks the crew has left and who has the most. Cooldown is configurable. A pet is required!");
        R("Lurer", "Приманщик", "Lurer", "#FF8A80", I,
            "Предатель. Питомец притягивает к тебе ближайшего члена экипажа (в радиусе до 8 м). Число использований и перезарядка настраиваются. Нужен питомец!",
            "Impostor. Pet pulls the nearest crewmate (within 8 m) to you. Number of uses and cooldown are configurable. A pet is required!");
        R("Breaker", "Взломщик", "Breaker", "#B0BEC5", I,
            "Предатель. Питомец рядом с игроком (до 3 м) ломает его защиту, если она есть. Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet next to a player (within 3 m) breaks his shield if he has one. Cooldown is configurable. A pet is required!");
        R("Rumormonger", "Сплетник", "Rumormonger", "#CE93D8", I,
            "Предатель. На собрании с настраиваемым шансом всем мирным приходит слух про случайного невиновного игрока.",
            "Impostor. At a meeting, with a configurable chance, all crewmates receive a rumor about a random innocent player.");
        R("Jammer", "Глушитель", "Jammer", "#546E7A", I,
            "Предатель. Питомец включает саботаж связи, если сейчас нет другого саботажа. Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet starts the comms sabotage if no other sabotage is active. Cooldown is configurable. A pet is required!");

        R("Mystifier", "Мистификатор", "Mystifier", "#9575CD", I,
            "Предатель. Питомец рассылает всем мирным ложную тревогу: «в такой-то комнате кто-то кричит» (случайная комната). Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet sends all crewmates a false alert: \"someone is screaming in that room\" (a random room). Cooldown is configurable. A pet is required!");
        R("Stalker", "Сталкер", "Stalker", "#455A64", I,
            "Предатель. Питомец показывает, сколько членов экипажа сейчас в одиночестве (никого в радиусе 6 м), и где ближайший из них. Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet shows how many crewmates are alone right now (nobody within 6 m) and where the nearest of them is. Cooldown is configurable. A pet is required!");
        R("Slammer", "Захлопыватель", "Slammer", "#8D6E63", I,
            "Предатель. Питомец захлопывает сразу все двери на карте (как массовый саботаж дверей, они откроются сами). Перезарядка настраивается. Нужен питомец!",
            "Impostor. Pet slams all doors on the map at once (like a mass door sabotage, they open by themselves). Cooldown is configurable. A pet is required!");
        R("Trapper", "Капканщик", "Trapper", "#A1887F", I,
            "Предатель. Питомец ставит капкан: первый член экипажа, наступивший на него, застывает на несколько секунд, а ты узнаёшь его имя. Капканы пропадают на собрании. Нужен питомец!",
            "Impostor. Pet places a trap: the first crewmate to step on it freezes for a few seconds and you learn his name. Traps vanish at a meeting. A pet is required!");
        R("Reaper", "Жнец", "Reaper", "#880E4F", I,
            "Предатель. После убийства с настраиваемым шансом получает щит, который отменит первое нападение на него.",
            "Impostor. After a kill, with a configurable chance, he gets a shield that cancels the first attack on him.");

        // ---------- Нейтралы
        R("Marauder", "Мародёр", "Marauder", "#8D6E63", N,
            "Нейтрал. Питомец рядом с трупом (до 2 м) — обыскать его: ты получаешь щит. Один труп — один раз. Число обысков ограничено. Нужен питомец!",
            "Neutral. Pet next to a body (within 2 m) - loot it: you get a shield. One body - once. Number of loots is limited. A pet is required!");
        R("Adventurer", "Авантюрист", "Adventurer", "#FF7043", N,
            "Нейтрал. Питомец — бросить кости: щит, телепорт к случайному игроку, подсказка о чьём-то местоположении или мигание экрана. Число бросков ограничено. Нужен питомец!",
            "Neutral. Pet - roll the dice: a shield, a teleport to a random player, a hint about someone's location or a screen flash. Number of rolls is limited. A pet is required!");
        R("Hermit", "Затворник", "Hermit", "#78909C", N,
            "Нейтрал. Если рядом (в радиусе 6 м) никого нет в течение настраиваемого времени, получает щит. Работает сама.",
            "Neutral. If nobody is nearby (within 6 m) for a configurable time he gets a shield. Works by itself.");
        R("Tourist", "Турист", "Tourist", "#4DD0E1", N,
            "Нейтрал. Побывав в настраиваемом числе разных комнат, получает щит и начинает считать заново. Работает сама.",
            "Neutral. After visiting a configurable number of different rooms he gets a shield and starts counting again. Works by itself.");
        R("Avenger", "Мститель", "Avenger", "#D32F2F", N,
            "Нейтрал. Если его убили, через настраиваемое время погибает и убийца.",
            "Neutral. If he is killed, his killer dies too after a configurable delay.");
        R("Trader", "Торговец", "Trader", "#FFA726", N,
            "Нейтрал. Питомец рядом с игроком (до 3 м) отнимает у него щит, если он есть, и отдаёт тебе. Перезарядка настраивается. Нужен питомец!",
            "Neutral. Pet next to a player (within 3 m) takes his shield, if he has one, and gives it to you. Cooldown is configurable. A pet is required!");
        R("Sleepwalker", "Лунатик", "Sleepwalker", "#7E57C2", N,
            "Нейтрал. Время от времени сам переносится к случайному игроку. Работает сама.",
            "Neutral. From time to time he teleports to a random player by himself. Works by itself.");
        R("Mole", "Крот", "Mole", "#795548", N,
            "Нейтрал. Питомец переносит его к случайной двери на карте. Число использований и перезарядка настраиваются. Нужен питомец!",
            "Neutral. Pet teleports him to a random door on the map. Number of uses and cooldown are configurable. A pet is required!");
        R("Hypnotist", "Гипнотизёр", "Hypnotist", "#AB47BC", N,
            "Нейтрал. Питомец рядом с игроком (до 3 м) обездвиживает его на несколько секунд. Перезарядка настраивается. Нужен питомец!",
            "Neutral. Pet next to a player (within 3 m) freezes him in place for a few seconds. Cooldown is configurable. A pet is required!");
        R("Swindler", "Мошенник", "Swindler", "#CDDC39", N,
            "Нейтрал. Питомец рядом с игроком (до 3 м): тому приходит ложное сообщение, что он получил защиту, а настоящий щит получает Мошенник. Число обманов ограничено. Нужен питомец!",
            "Neutral. Pet next to a player (within 3 m): he gets a fake message that he received a shield, while the Swindler gets a real one. Number of tricks is limited. A pet is required!");

        R("Scarecrow", "Пугало", "Scarecrow", "#FBC02D", N,
            "Нейтрал. Питомец пугает всех игроков вокруг (в радиусе 6 м): у них на несколько секунд мигает экран. Перезарядка настраивается. Нужен питомец!",
            "Neutral. Pet scares all players around (within 6 m): their screens flash for a few seconds. Cooldown is configurable. A pet is required!");
        R("Clockmaker", "Часовщик", "Clockmaker", "#4E342E", N,
            "Нейтрал. Питомец отматывает время для тебя: ты возвращаешься туда, где был около 10 секунд назад. Перезарядка настраивается. Нужен питомец!",
            "Neutral. Pet rewinds time for you: you return to where you were about 10 seconds ago. Cooldown is configurable. A pet is required!");
        R("Lottery", "Лотерейщик", "Lottery", "#00BFA5", N,
            "Нейтрал. На каждом собрании с настраиваемым шансом выигрывает щит.",
            "Neutral. At every meeting he wins a shield with a configurable chance.");

        R("Magnet", "Магнит", "Magnet", "#E53935", N,
            "Нейтрал. Питомец притягивает к тебе всех игроков вокруг (в радиусе настраивается). Перезарядка настраивается. Нужен питомец!",
            "Neutral. Pet pulls all players around you towards you (radius configurable). Cooldown is configurable. A pet is required!");
        R("Fence", "Скупщик", "Fence", "#C0CA33", N,
            "Нейтрал. За каждые N заданий, выполненных экипажем (настраивается), получает щит. Работает сама.",
            "Neutral. For every N tasks completed by the crew (configurable) he gets a shield. Works by itself.");

        // ---------- Настройки
        E("Sentry", "SentryCd", C, 15, 5, 90, 5, "Перезарядка поста (сек)", "Post cooldown (sec)");
        E("Sentry", "SentryMax", C, 3, 1, 8, 1, "Постов одновременно", "Posts at a time");
        E("Statistician", "StatUses", C, 2, 1, 10, 1, "Число проверок", "Checks per game");
        E("Marked", "MarkedTrue", C, 60, 0, 100, 5, "Шанс, что имя убийцы верное (%)", "Chance the killer's name is true (%)");
        E("Dispatcher", "DispatcherCd", C, 30, 5, 120, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Intercessor", "IntercessorCd", C, 30, 5, 120, 5, "Перезарядка выбора подопечного (сек)", "Ward choice cooldown (sec)");
        E("Peacemaker", "PeaceCd", C, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Peacemaker", "PeaceUses", C, 2, 1, 10, 1, "Использований за игру", "Uses per game");
        E("Peacemaker", "PeaceSeconds", C, 6, 2, 20, 1, "Длительность зоны (сек)", "Zone duration (sec)");
        E("Peacemaker", "PeaceRadius", C, 5, 2, 12, 1, "Радиус зоны (м)", "Zone radius (m)");
        E("Investigator", "InvUses", C, 2, 1, 10, 1, "Число проверок", "Checks per game");
        E("Wanderer", "WandererCd", C, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Lucky", "LuckyFail", C, 30, 5, 100, 5, "Шанс провала нападения (%)", "Chance an attack fails (%)");
        E("Lucky", "LuckyMax", C, 2, 1, 6, 1, "Спасений за игру", "Saves per game");

        E("Minelayer", "MineCd", I, 25, 5, 120, 5, "Перезарядка мины (сек)", "Mine cooldown (sec)");
        E("Minelayer", "MineMax", I, 3, 1, 8, 1, "Мин одновременно", "Mines at a time");
        E("Shuffler", "ShufflerCd", I, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Eclipser", "EclipserCd", I, 90, 20, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Returner", "ReturnerCd", I, 10, 0, 120, 5, "Перезарядка смены метки (сек)", "Mark change cooldown (sec)");
        E("Ghoul", "GhoulEat", I, 50, 5, 100, 5, "Шанс съесть жертву (%)", "Chance to eat the victim (%)");
        E("Auditor", "AuditorCd", I, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Lurer", "LurerCd", I, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Lurer", "LurerUses", I, 4, 1, 15, 1, "Использований за игру", "Uses per game");
        E("Breaker", "BreakerCd", I, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Rumormonger", "RumorChance", I, 60, 5, 100, 5, "Шанс слуха на собрании (%)", "Chance of a rumor per meeting (%)");
        E("Jammer", "JammerCd", I, 90, 20, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");

        E("Eyewitness", "WitnessRadius", C, 7, 3, 20, 1, "Радиус, в котором он видит убийство (м)", "Radius in which he sees a kill (m)");
        E("Eyewitness", "WitnessTrue", C, 60, 0, 100, 5, "Шанс, что имя убийцы верное (%)", "Chance the killer's name is true (%)");
        E("Forensic", "ForensicUses", C, 3, 1, 10, 1, "Осмотров за игру", "Exams per game");
        E("Aide", "AideUses", C, 3, 1, 10, 1, "Помощей за игру", "Helps per game");
        E("Aide", "AideCd", C, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Mystifier", "MystifierCd", I, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Stalker", "StalkerCd", I, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Magnet", "MagnetCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Magnet", "MagnetRadius", N, 6, 2, 15, 1, "Радиус притяжения (м)", "Pull radius (m)");
        E("Fence", "FenceTasks", N, 5, 1, 30, 1, "Заданий экипажа до щита", "Crew tasks until a shield");
        E("Slammer", "SlammerCd", I, 90, 20, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Trapper", "TrapCd", I, 25, 5, 120, 5, "Перезарядка капкана (сек)", "Trap cooldown (sec)");
        E("Trapper", "TrapMax", I, 2, 1, 6, 1, "Капканов одновременно", "Traps at a time");
        E("Trapper", "TrapSecs", I, 5, 1, 15, 1, "Сколько секунд жертва застыла", "Seconds the victim is frozen");
        E("Reaper", "ReaperShield", I, 50, 5, 100, 5, "Шанс получить щит после убийства (%)", "Chance of a shield after a kill (%)");
        E("Scarecrow", "ScareCd", N, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Scarecrow", "ScareSecs", N, 3, 1, 8, 1, "Сколько секунд мигает экран", "Flash duration (sec)");
        E("Clockmaker", "ClockCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Lottery", "LotteryWin", N, 30, 5, 100, 5, "Шанс выигрыша на собрании (%)", "Win chance per meeting (%)");
        E("Marauder", "MarauderUses", N, 2, 1, 8, 1, "Обысков за игру", "Loots per game");
        E("Adventurer", "AdvCd", N, 30, 5, 120, 5, "Перезарядка броска (сек)", "Roll cooldown (sec)");
        E("Adventurer", "AdvUses", N, 5, 1, 15, 1, "Бросков за игру", "Rolls per game");
        E("Hermit", "HermitSecs", N, 30, 10, 120, 5, "Секунд одиночества до щита", "Seconds alone until a shield");
        E("Tourist", "TouristRooms", N, 6, 2, 15, 1, "Разных комнат до щита", "Different rooms until a shield");
        E("Avenger", "AvengerDelay", N, 20, 3, 90, 1, "Через сколько секунд гибнет убийца", "Seconds until the killer dies");
        E("Trader", "TraderCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Sleepwalker", "SleepMin", N, 40, 10, 180, 5, "Минимум секунд между телепортами", "Min seconds between teleports");
        E("Sleepwalker", "SleepMax", N, 80, 20, 300, 5, "Максимум секунд между телепортами", "Max seconds between teleports");
        E("Mole", "MoleCd", N, 40, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Mole", "MoleUses", N, 4, 1, 15, 1, "Использований за игру", "Uses per game");
        E("Hypnotist", "HypnoCd", N, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Hypnotist", "HypnoSecs", N, 4, 1, 15, 1, "Сколько секунд игрок неподвижен", "Seconds the player is frozen");
        E("Swindler", "SwindlerCd", N, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Swindler", "SwindlerUses", N, 2, 1, 8, 1, "Обманов за игру", "Tricks per game");
    }

    // ---------------------------------------------------------------- Состояние
    class Freeze { public byte Id; public Vector2 Pos; public float Until, NextTp; }
    static readonly Dictionary<string, float> lastUse = new();
    static readonly Dictionary<string, int> uses = new();
    static readonly List<(byte victim, byte killer, Vector2 pos)> deaths = new();
    static readonly List<(byte victim, Vector2 pos)> bodies = new();
    static readonly HashSet<byte> looted = new();
    static Dictionary<byte, Vector2> snapshot = new();
    static readonly List<(byte owner, Vector2 pos)> posts = new();
    static readonly List<(byte owner, Vector2 pos)> mines = new();
    static readonly Dictionary<byte, byte> ward = new();
    static readonly Dictionary<byte, float> peace = new();
    static readonly Dictionary<byte, Vector2> home = new();
    static readonly Dictionary<byte, byte> markedBy = new();
    static readonly List<(byte killer, float at)> avenge = new();
    static readonly List<(byte id, Vector2 pos, float at)> tpQueue = new();
    static readonly List<Freeze> freezes = new();
    static readonly Dictionary<byte, float> hermit = new();
    static readonly Dictionary<byte, HashSet<string>> visited = new();
    static readonly Dictionary<byte, float> sleepNext = new();
    static readonly Dictionary<byte, string> lastRoomOf = new();
    static readonly Dictionary<byte, int> luckyUsed = new();
    static readonly List<(byte owner, Vector2 pos)> traps = new();
    static readonly Dictionary<byte, List<(float t, Vector2 pos)>> history = new();
    static readonly Dictionary<byte, float> deathTime = new();
    static int fenceLast = -1, fenceAcc;
    static float nextTick;

    public static void Reset()
    {
        lastUse.Clear(); uses.Clear(); deaths.Clear(); bodies.Clear(); looted.Clear(); snapshot = new(); posts.Clear(); mines.Clear(); traps.Clear(); history.Clear(); ward.Clear(); peace.Clear(); home.Clear();
        markedBy.Clear(); avenge.Clear(); tpQueue.Clear(); freezes.Clear(); hermit.Clear(); visited.Clear(); sleepNext.Clear(); lastRoomOf.Clear(); luckyUsed.Clear(); traps.Clear(); history.Clear(); deathTime.Clear(); fenceLast = -1; fenceAcc = 0;
    }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected && !WallVisual.IsPuppet(p);
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
        if (lastUse.TryGetValue(k, out var l) && Time.time - l < cd)
        { Say(p, T(ru, $"Перезарядка: ещё {Mathf.CeilToInt(cd - (Time.time - l))} с.", "Cooling down.")); return true; }
        return false;
    }
    static void Stamp(string key, PlayerControl p) => lastUse[key + ":" + p.PlayerId] = Time.time;
    static bool NoUses(string key, PlayerControl p, int max, bool ru)
    {
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

    static void GiveShield(PlayerControl p) => RoleLogic.ShieldArmed.Add(p.PlayerId);

    // ---------------------------------------------------------------- Статус (сводка на собрании)
    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p); string n = r.Colored(ru); byte id = p.PlayerId;
        int L(string k, int max) => Math.Max(0, max - Used(k, id));
        switch (r.Id)
        {
            case "Archivist": return T(ru, $"{n}: на каждом собрании приходит архив — где умер каждый погибший.", $"{n}: every meeting you get an archive - where each dead player died.");
            case "Sentry": return T(ru, $"{n}: питомец ставит пост; кто пройдёт рядом — будет назван тебе. Постов {posts.Count(x => x.owner == id)}/{G("SentryMax")}.", $"{n}: pet places a post; whoever passes near is named to you. Posts {posts.Count(x => x.owner == id)}/{G("SentryMax")}.");
            case "Statistician": return T(ru, $"{n}: на собрании /ab — число живых предателей. Проверок: {L("stat", G("StatUses"))}.", $"{n}: in a meeting /ab shows living impostors. Checks left: {L("stat", G("StatUses"))}.");
            case "Marked": return T(ru, $"{n}: если тебя убьют, мирные узнают имя убийцы (верное с шансом {G("MarkedTrue")}%).", $"{n}: if you are killed, crewmates learn the killer's name (true with {G("MarkedTrue")}% chance).");
            case "Dispatcher": return T(ru, $"{n}: питомец показывает, где сейчас больше всего игроков.", $"{n}: pet shows where most players are right now.");
            case "Intercessor": return T(ru, $"{n}: питомец рядом с игроком берёт его под защиту; за подопечного погибнешь ты. Подопечный: {WardName(id, true)}.", $"{n}: pet next to a player protects him; you die in his place. Ward: {WardName(id, false)}.");
            case "Peacemaker": return T(ru, $"{n}: питомец создаёт зону мира ({G("PeaceSeconds")} с). Осталось: {L("peace", G("PeaceUses"))}.", $"{n}: pet creates a peace zone ({G("PeaceSeconds")} s). Left: {L("peace", G("PeaceUses"))}.");
            case "Investigator": return T(ru, $"{n}: на собрании /ab ID — где был игрок в момент последнего убийства. Проверок: {L("inv", G("InvUses"))}.", $"{n}: in a meeting /ab ID shows where a player was at the last kill. Checks left: {L("inv", G("InvUses"))}.");
            case "Wanderer": return T(ru, $"{n}: питомец переносит тебя к случайному игроку.", $"{n}: pet teleports you to a random player.");
            case "Lucky": return T(ru, $"{n}: нападение на тебя проваливается с шансом {G("LuckyFail")}%. Спасений: {L("lucky", G("LuckyMax"))}.", $"{n}: an attack on you fails with {G("LuckyFail")}% chance. Saves left: {L("lucky", G("LuckyMax"))}.");
            case "Minelayer": return T(ru, $"{n}: питомец ставит мину; наступивший член экипажа погибает. Мин {mines.Count(x => x.owner == id)}/{G("MineMax")}.", $"{n}: pet places a mine; a crewmate who steps on it dies. Mines {mines.Count(x => x.owner == id)}/{G("MineMax")}.");
            case "Shuffler": return T(ru, $"{n}: питомец меняет местами двух случайных членов экипажа.", $"{n}: pet swaps two random crewmates.");
            case "Eclipser": return T(ru, $"{n}: питомец гасит свет.", $"{n}: pet turns the lights off.");
            case "Returner": return T(ru, $"{n}: питомец ставит метку; после убийства ты возвращаешься к ней. Метка: {(home.ContainsKey(id) ? Room(home[id], true) : "нет")}.", $"{n}: pet sets a mark; after a kill you return to it. Mark: {(home.ContainsKey(id) ? Room(home[id], false) : "none")}.");
            case "Ghoul": return T(ru, $"{n}: с шансом {G("GhoulEat")}% жертва съедается целиком — тело не доложить.", $"{n}: with {G("GhoulEat")}% chance the victim is eaten whole - the body can't be reported.");
            case "Auditor": return T(ru, $"{n}: питомец показывает, сколько заданий осталось у экипажа и у кого больше всего.", $"{n}: pet shows how many tasks the crew has left and who has the most.");
            case "Lurer": return T(ru, $"{n}: питомец притягивает ближайшего члена экипажа (до 8 м). Осталось: {L("lurer", G("LurerUses"))}.", $"{n}: pet pulls the nearest crewmate (up to 8 m). Left: {L("lurer", G("LurerUses"))}.");
            case "Breaker": return T(ru, $"{n}: питомец рядом с игроком ломает его щит.", $"{n}: pet next to a player breaks his shield.");
            case "Rumormonger": return T(ru, $"{n}: на собрании с шансом {G("RumorChance")}% мирным приходит ложный слух о случайном невиновном.", $"{n}: at a meeting, with {G("RumorChance")}% chance, crewmates get a rumor about a random innocent.");
            case "Jammer": return T(ru, $"{n}: питомец включает саботаж связи.", $"{n}: pet starts the comms sabotage.");
            case "Eyewitness": return T(ru, $"{n}: если рядом (до {G("WitnessRadius")} м) кого-то убьют, ты узнаешь имя убийцы (верное с шансом {G("WitnessTrue")}%).", $"{n}: if someone is killed near you (within {G("WitnessRadius")} m) you learn the killer's name (true with {G("WitnessTrue")}% chance).");
            case "Forensic": return T(ru, $"{n}: питомец у трупа — время смерти и смыть краску. Осмотров: {L("forensic", G("ForensicUses"))}.", $"{n}: pet next to a body - time of death and wash off paint. Exams left: {L("forensic", G("ForensicUses"))}.");
            case "Aide": return T(ru, $"{n}: питомец рядом с членом экипажа засчитывает ему задание. Осталось: {L("aide", G("AideUses"))}.", $"{n}: pet next to a crewmate completes a task for him. Left: {L("aide", G("AideUses"))}.");
            case "Mystifier": return T(ru, $"{n}: питомец рассылает мирным ложную тревогу о крике в случайной комнате.", $"{n}: pet sends crewmates a false alert about screaming in a random room.");
            case "Stalker": return T(ru, $"{n}: питомец показывает, кто из мирных сейчас в одиночестве.", $"{n}: pet shows which crewmates are alone right now.");
            case "Magnet": return T(ru, $"{n}: питомец притягивает к тебе всех в радиусе {G("MagnetRadius")} м.", $"{n}: pet pulls everyone within {G("MagnetRadius")} m to you.");
            case "Fence": return T(ru, $"{n}: за каждые {G("FenceTasks")} заданий экипажа — щит.", $"{n}: a shield for every {G("FenceTasks")} crew tasks.");
            case "Slammer": return T(ru, $"{n}: питомец захлопывает все двери на карте.", $"{n}: pet slams all doors on the map.");
            case "Trapper": return T(ru, $"{n}: питомец ставит капкан; наступивший застывает на {G("TrapSecs")} с. Капканов {traps.Count(x => x.owner == id)}/{G("TrapMax")}.", $"{n}: pet places a trap; whoever steps on it freezes for {G("TrapSecs")} s. Traps {traps.Count(x => x.owner == id)}/{G("TrapMax")}.");
            case "Reaper": return T(ru, $"{n}: после убийства с шансом {G("ReaperShield")}% получаешь щит.", $"{n}: after a kill you get a shield with {G("ReaperShield")}% chance.");
            case "Scarecrow": return T(ru, $"{n}: питомец пугает всех в радиусе 6 м — у них мигает экран.", $"{n}: pet scares everyone within 6 m - their screens flash.");
            case "Clockmaker": return T(ru, $"{n}: питомец отматывает тебя на ~10 секунд назад.", $"{n}: pet rewinds you ~10 seconds back.");
            case "Lottery": return T(ru, $"{n}: на каждом собрании шанс {G("LotteryWin")}% выиграть щит.", $"{n}: every meeting a {G("LotteryWin")}% chance to win a shield.");
            case "Marauder": return T(ru, $"{n}: питомец рядом с трупом — обыскать (даёт щит). Осталось: {L("maraud", G("MarauderUses"))}.", $"{n}: pet next to a body - loot it (gives a shield). Left: {L("maraud", G("MarauderUses"))}.");
            case "Adventurer": return T(ru, $"{n}: питомец — бросить кости (щит / телепорт / подсказка / мигание). Осталось: {L("adv", G("AdvUses"))}.", $"{n}: pet - roll the dice (shield / teleport / hint / flash). Left: {L("adv", G("AdvUses"))}.");
            case "Hermit": return T(ru, $"{n}: {G("HermitSecs")} с в одиночестве (никого в 6 м) — получаешь щит.", $"{n}: {G("HermitSecs")} s alone (nobody within 6 m) gives you a shield.");
            case "Tourist": return T(ru, $"{n}: побывай в {G("TouristRooms")} разных комнатах — получишь щит.", $"{n}: visit {G("TouristRooms")} different rooms to get a shield.");
            case "Avenger": return T(ru, $"{n}: если тебя убьют, убийца погибнет через {G("AvengerDelay")} с.", $"{n}: if you are killed, your killer dies after {G("AvengerDelay")} s.");
            case "Trader": return T(ru, $"{n}: питомец рядом с игроком забирает его щит себе.", $"{n}: pet next to a player takes his shield.");
            case "Sleepwalker": return T(ru, $"{n}: время от времени тебя уносит к случайному игроку.", $"{n}: from time to time you wake up next to a random player.");
            case "Mole": return T(ru, $"{n}: питомец переносит тебя к случайной двери. Осталось: {L("mole", G("MoleUses"))}.", $"{n}: pet teleports you to a random door. Left: {L("mole", G("MoleUses"))}.");
            case "Hypnotist": return T(ru, $"{n}: питомец рядом с игроком обездвиживает его на {G("HypnoSecs")} с.", $"{n}: pet next to a player freezes him for {G("HypnoSecs")} s.");
            case "Swindler": return T(ru, $"{n}: питомец рядом с игроком — ложный щит ему, настоящий тебе. Осталось: {L("swindle", G("SwindlerUses"))}.", $"{n}: pet next to a player - fake shield for him, real one for you. Left: {L("swindle", G("SwindlerUses"))}.");
        }
        return null;
    }

    static string WardName(byte id, bool ru)
    {
        if (ward.TryGetValue(id, out var w) && Utils.GetPlayer(w) is PlayerControl wp && wp.Data != null) return wp.Data.PlayerName;
        return ru ? "нет" : "none";
    }

    // ---------------------------------------------------------------- Способности питомца
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
            case "Dispatcher":
                {
                    if (Wait("Dispatcher", p, G("DispatcherCd"), ru)) return true;
                    Stamp("Dispatcher", p);
                    var counts = new Dictionary<string, int>();
                    foreach (var o in Players()) { var n = Room(o.GetTruePosition(), ru); counts[n] = counts.TryGetValue(n, out var c) ? c + 1 : 1; }
                    Say(p, T(ru, "Больше всего людей: ", "Most crowded: ") + string.Join(", ", counts.OrderByDescending(x => x.Value).Take(3).Select(x => $"{x.Key} ({x.Value})")));
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
            case "Wanderer":
                {
                    if (Wait("Wanderer", p, G("WandererCd"), ru)) return true;
                    var list = Players().Where(o => o.PlayerId != id).ToList();
                    if (list.Count == 0) { Say(p, T(ru, "Некуда идти.", "Nowhere to go.")); return true; }
                    Stamp("Wanderer", p); var t = list[rng.Next(list.Count)];
                    Commands.Tp(p, t.GetTruePosition() + new Vector2(0.5f, 0f));
                    Say(p, T(ru, $"Ты забрёл в комнату: {Room(t.GetTruePosition(), true)}.", "You wandered off."));
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
            case "Shuffler":
                {
                    if (Wait("Shuffler", p, G("ShufflerCd"), ru)) return true;
                    var list = Players().Where(o => o.PlayerId != id && !IsImp(o)).ToList();
                    if (list.Count < 2) { Say(p, T(ru, "Нужны хотя бы двое мирных.", "Need at least two crewmates.")); return true; }
                    Stamp("Shuffler", p);
                    int i = rng.Next(list.Count), j; do { j = rng.Next(list.Count); } while (j == i);
                    Vector2 a = list[i].GetTruePosition(), b = list[j].GetTruePosition();
                    Commands.Tp(list[i], b); Commands.Tp(list[j], a);
                    Say(p, T(ru, $"Поменял местами: {list[i].Data.PlayerName} и {list[j].Data.PlayerName}.", "Swapped two crewmates."));
                    return true;
                }
            case "Slammer":
                {
                    if (Wait("Slammer", p, G("SlammerCd"), ru)) return true;
                    var doors = ss != null ? ss.AllDoors : null;
                    if (doors == null || doors.Length == 0) { Say(p, T(ru, "На этой карте нет дверей.", "No doors on this map.")); return true; }
                    Stamp("Slammer", p);
                    var seen = new HashSet<SystemTypes>(); int n = 0;
                    foreach (var d in doors) { if (d == null || !seen.Add(d.Room)) continue; try { ss.RpcCloseDoorsOfType(d.Room); n++; } catch { } }
                    Say(p, T(ru, $"Двери захлопнуты ({n}).", $"Doors slammed ({n})."));
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
            case "Scarecrow":
                {
                    if (Wait("Scarecrow", p, G("ScareCd"), ru)) return true;
                    Stamp("Scarecrow", p); int n = 0; var me = p.GetTruePosition();
                    foreach (var o in Players())
                        if (o.PlayerId != id && Vector2.Distance(me, o.GetTruePosition()) <= 6f) { RoleLogic.FlashPlayer(o, G("ScareSecs")); n++; }
                    Say(p, T(ru, $"Ты напугал игроков: {n}.", $"You scared {n} players."));
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
                    else if (RoleLogic.Painted.Remove(best.victim)) extra = T(ru, "Краска смыта — труп можно доложить.", "Paint washed off - the body can be reported.");
                    else extra = T(ru, "Следов краски нет.", "No traces of paint.");
                    Say(p, T(ru, $"Смерть наступила {ago} с назад. {extra}", $"Death occurred {ago} s ago. {extra}"));
                    return true;
                }
            case "Aide":
                {
                    if (NoUses("aide", p, G("AideUses"), ru) || Wait("Aide", p, G("AideCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => !IsImp(o) && o.Data.Tasks != null && o.Data.Tasks.ToArray().Any(x => !x.Complete));
                    if (t == null) { Say(p, T(ru, "Рядом нет члена экипажа с невыполненными заданиями.", "No crewmate with unfinished tasks nearby.")); return true; }
                    Stamp("Aide", p); Use("aide", id);
                    var task = t.Data.Tasks.ToArray().First(x => !x.Complete);
                    t.RpcCompleteTask(task.Id);
                    Say(p, T(ru, $"Ты помог {t.Data.PlayerName} с заданием.", $"You helped {t.Data.PlayerName} with a task."));
                    Say(t, T(Translator.IsRu(t), "Кто-то помог тебе с заданием.", "Someone helped you with a task."));
                    return true;
                }
            case "Mystifier":
                {
                    if (Wait("Mystifier", p, G("MystifierCd"), ru)) return true;
                    var doors = ss != null ? ss.AllDoors : null;
                    if (doors == null || doors.Length == 0) { Say(p, T(ru, "На этой карте нет дверей.", "No doors on this map.")); return true; }
                    OpenableDoor d = null; for (int k = 0; k < 10 && d == null; k++) d = doors[rng.Next(doors.Length)];
                    if (d == null) return true;
                    Stamp("Mystifier", p); var dp = (Vector2)d.transform.position; int n = 0;
                    foreach (var o in Players())
                        if (o.PlayerId != id && !IsImp(o)) { Say(o, T(Translator.IsRu(o), $"Тревога! В комнате «{Room(dp, true)}» кто-то кричит!", $"Alert! Someone is screaming in {Room(dp, false)}!")); n++; }
                    Say(p, T(ru, $"Ложная тревога разослана ({n}): {Room(dp, true)}.", $"False alert sent ({n}): {Room(dp, false)}."));
                    return true;
                }
            case "Stalker":
                {
                    if (Wait("Stalker", p, G("StalkerCd"), ru)) return true;
                    Stamp("Stalker", p); var all = Players(); var me = p.GetTruePosition();
                    var alone = all.Where(o => o.PlayerId != id && !IsImp(o) && !all.Any(q => q.PlayerId != o.PlayerId && Vector2.Distance(q.GetTruePosition(), o.GetTruePosition()) < 6f)).ToList();
                    if (alone.Count == 0) { Say(p, T(ru, "Сейчас никто из мирных не один.", "No crewmate is alone right now.")); return true; }
                    var near = alone.OrderBy(o => Vector2.Distance(me, o.GetTruePosition())).First();
                    Say(p, T(ru, $"В одиночестве: {alone.Count}. Ближайший — {Room(near.GetTruePosition(), true)}, ~{Vector2.Distance(me, near.GetTruePosition()):F0} м.", $"Alone: {alone.Count}. Nearest - {Room(near.GetTruePosition(), false)}, ~{Vector2.Distance(me, near.GetTruePosition()):F0} m."));
                    return true;
                }
            case "Magnet":
                {
                    if (Wait("Magnet", p, G("MagnetCd"), ru)) return true;
                    Stamp("Magnet", p); var me = p.GetTruePosition(); int n = 0;
                    foreach (var o in Players())
                        if (o.PlayerId != id && Vector2.Distance(me, o.GetTruePosition()) <= G("MagnetRadius")) { Commands.Tp(o, me + new Vector2((float)(rng.NextDouble() - 0.5) * 1.2f, (float)(rng.NextDouble() - 0.5) * 1.2f)); n++; }
                    Say(p, T(ru, $"Магнит притянул игроков: {n}.", $"The magnet pulled {n} players."));
                    return true;
                }
            case "Eclipser": return Sabotage(p, "Eclipser", G("EclipserCd"), SystemTypes.Electrical, T(ru, "Свет погас.", "Lights are off."), ru);
            case "Jammer": return Sabotage(p, "Jammer", G("JammerCd"), SystemTypes.Comms, T(ru, "Связь отключена.", "Comms are down."), ru);
            case "Returner":
                {
                    if (Wait("Returner", p, G("ReturnerCd"), ru)) return true;
                    Stamp("Returner", p); home[id] = p.GetTruePosition();
                    Say(p, T(ru, $"Метка поставлена: {Room(p.GetTruePosition(), true)}. После убийства вернёшься сюда.", "Mark set."));
                    return true;
                }
            case "Auditor":
                {
                    if (Wait("Auditor", p, G("AuditorCd"), ru)) return true;
                    Stamp("Auditor", p);
                    int total = 0; PlayerControl top = null; int tm = 0;
                    foreach (var o in Players())
                    {
                        if (IsImp(o) || o.Data.Tasks == null) continue;
                        int left = o.Data.Tasks.ToArray().Count(x => !x.Complete);
                        total += left; if (left > tm) { tm = left; top = o; }
                    }
                    Say(p, T(ru, $"Заданий у экипажа осталось: {total}." + (top != null ? $" Больше всего у {top.Data.PlayerName} ({tm})." : ""), $"Crew tasks left: {total}." + (top != null ? $" Most: {top.Data.PlayerName} ({tm})." : "")));
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
            case "Breaker":
                {
                    if (Wait("Breaker", p, G("BreakerCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => RoleLogic.ShieldArmed.Contains(o.PlayerId));
                    if (t == null) { Say(p, T(ru, "Рядом нет игрока со щитом.", "No shielded player nearby.")); return true; }
                    Stamp("Breaker", p); RoleLogic.ShieldArmed.Remove(t.PlayerId);
                    Say(p, T(ru, $"Щит сломан: {t.Data.PlayerName}.", $"Shield broken: {t.Data.PlayerName}."));
                    return true;
                }
            case "Marauder":
                {
                    if (NoUses("maraud", p, G("MarauderUses"), ru)) return true;
                    var me = p.GetTruePosition(); (byte victim, Vector2 pos) best = default; float bd = 2f; bool found = false;
                    foreach (var b in bodies) { if (looted.Contains(b.victim)) continue; float d = Vector2.Distance(me, b.pos); if (d < bd) { bd = d; best = b; found = true; } }
                    if (!found) { Say(p, T(ru, "Рядом нет необысканного трупа.", "No unlooted body nearby.")); return true; }
                    looted.Add(best.victim); Use("maraud", id);
                    if (RoleLogic.ShieldArmed.Contains(id)) Say(p, T(ru, "Ты обыскал тело, но щит у тебя уже есть.", "You looted the body, but you already have a shield."));
                    else { GiveShield(p); Say(p, T(ru, "Ты обыскал тело и нашёл защиту: щит готов.", "You looted the body: shield ready.")); }
                    return true;
                }
            case "Adventurer":
                {
                    if (NoUses("adv", p, G("AdvUses"), ru) || Wait("Adventurer", p, G("AdvCd"), ru)) return true;
                    Stamp("Adventurer", p); Use("adv", id);
                    int roll = rng.Next(1, 7);
                    var others = Players().Where(o => o.PlayerId != id).ToList();
                    if (roll <= 2 || others.Count == 0) { RoleLogic.FlashPlayer(p, 3f); Say(p, T(ru, $"Выпало {roll}: не повезло — экран мигает.", $"Rolled {roll}: bad luck - your screen flashes.")); }
                    else if (roll == 3) { var o = others[rng.Next(others.Count)]; Say(p, T(ru, $"Выпало {roll}: кто-то сейчас здесь — {Room(o.GetTruePosition(), true)}.", $"Rolled {roll}: someone is in {Room(o.GetTruePosition(), false)}.")); }
                    else if (roll == 4 || roll == 5) { if (!RoleLogic.ShieldArmed.Contains(id)) GiveShield(p); Say(p, T(ru, $"Выпало {roll}: удача — щит готов.", $"Rolled {roll}: lucky - shield ready.")); }
                    else { var o = others[rng.Next(others.Count)]; Commands.Tp(p, o.GetTruePosition() + new Vector2(0.5f, 0f)); Say(p, T(ru, $"Выпало {roll}: тебя унесло к игроку.", $"Rolled {roll}: you teleport to a player.")); }
                    return true;
                }
            case "Trader":
                {
                    if (Wait("Trader", p, G("TraderCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => RoleLogic.ShieldArmed.Contains(o.PlayerId));
                    if (t == null) { Say(p, T(ru, "Рядом нет игрока со щитом.", "No shielded player nearby.")); return true; }
                    Stamp("Trader", p); RoleLogic.ShieldArmed.Remove(t.PlayerId); GiveShield(p);
                    Say(p, T(ru, $"Сделка! Щит {t.Data.PlayerName} теперь твой.", $"Deal! {t.Data.PlayerName}'s shield is yours."));
                    return true;
                }
            case "Mole":
                {
                    if (NoUses("mole", p, G("MoleUses"), ru) || Wait("Mole", p, G("MoleCd"), ru)) return true;
                    var doors = ss != null ? ss.AllDoors : null;
                    if (doors == null || doors.Length == 0) { Say(p, T(ru, "На этой карте нет дверей.", "No doors on this map.")); return true; }
                    OpenableDoor d = null; for (int k = 0; k < 10 && d == null; k++) d = doors[rng.Next(doors.Length)];
                    if (d == null) return true;
                    Stamp("Mole", p); Use("mole", id);
                    var pos = (Vector2)d.transform.position;
                    Commands.Tp(p, pos);
                    Say(p, T(ru, $"Ты вынырнул у двери: {Room(pos, true)}.", "You popped out at a door."));
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
            case "Swindler":
                {
                    if (NoUses("swindle", p, G("SwindlerUses"), ru) || Wait("Swindler", p, G("SwindlerCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Swindler", p); Use("swindle", id);
                    Say(t, T(Translator.IsRu(t), "Ты получил защиту!", "You received a shield!"));
                    if (!RoleLogic.ShieldArmed.Contains(id)) GiveShield(p);
                    Say(p, T(ru, $"Ты обманул {t.Data.PlayerName}: он думает, что у него щит, а настоящий — у тебя.", $"You tricked {t.Data.PlayerName}: the real shield is yours."));
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

    // ---------------------------------------------------------------- Команда на собрании (/ab)
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
        if (role.Id == "Investigator")
        {
            if (p.Data.IsDead) { Secret(p, T(ru, "Мёртвые не расследуют.", "The dead can't investigate.")); return true; }
            if (MeetingHud.Instance == null) { Secret(p, T(ru, "Работает только на собрании.", "Meeting only.")); return true; }
            if (args.Length == 0) { Secret(p, T(ru, "/ab ID — где был игрок в момент последнего убийства.", "/ab ID.")); return true; }
            PlayerControl t = null; string q = string.Join(" ", args);
            if (byte.TryParse(q, out var tid)) t = Utils.GetPlayer(tid);
            if (t == null) t = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x != null && x.Data != null && x.Data.PlayerName != null && x.Data.PlayerName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            if (t == null || t.Data == null) { Secret(p, T(ru, "Игрок не найден.", "Player not found.")); return true; }
            if (Used("inv", id) >= G("InvUses")) { Secret(p, T(ru, "Проверки закончились.", "No checks left.")); return true; }
            if (snapshot.Count == 0) { Secret(p, T(ru, "Убийств ещё не было.", "No kills yet.")); return true; }
            Use("inv", id);
            string where = snapshot.TryGetValue(t.PlayerId, out var rm) ? Room(rm, ru) : T(ru, "неизвестно (не был в живых или вне комнат)", "unknown");
            Secret(p, T(ru, $"{t.Data.PlayerName} в момент последнего убийства: {where}. Проверок осталось: {Math.Max(0, G("InvUses") - Used("inv", id))}.", $"{t.Data.PlayerName} at the last kill: {where}. Checks left: {Math.Max(0, G("InvUses") - Used("inv", id))}."));
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- Отмена убийства (вызывается из CheckMurder)
    public static bool BlockKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            if (killer == null || target == null || killer.Data == null || target.Data == null) return false;
            // Миротворец: в зоне мира убийства невозможны
            var kp = killer.GetTruePosition();
            foreach (var kv in peace.ToList())
            {
                if (Time.time > kv.Value) { peace.Remove(kv.Key); continue; }
                var pm = Utils.GetPlayer(kv.Key);
                if (!Alive(pm)) continue;
                if (Vector2.Distance(kp, pm.GetTruePosition()) <= G("PeaceRadius"))
                {
                    RoleLogic.Kill(killer, target, false);
                    Say(killer, T(Translator.IsRu(killer), "Здесь действует зона мира — убить не получилось.", "A peace zone is active here - the kill failed."));
                    return true;
                }
            }
            // Заступник: гибнет вместо подопечного
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
            // Везунчик
            if (RoleLogic.Has(target, "Lucky") && Used("lucky", target.PlayerId) < G("LuckyMax") && rng.Next(100) < G("LuckyFail"))
            {
                Use("lucky", target.PlayerId);
                RoleLogic.Kill(killer, target, false);
                Say(target, T(Translator.IsRu(target), "Тебе повезло: нападение сорвалось!", "Lucky you: the attack failed!"));
                return true;
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.BlockKill: " + e.Message); }
        return false;
    }

    // ---------------------------------------------------------------- Хуки
    public static void OnKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            if (target == null || target.Data == null) return;
            bool self = killer == null || killer.PlayerId == target.PlayerId;
            var tpos = target.GetTruePosition();
            bodies.Add((target.PlayerId, tpos));
            deaths.Add((target.PlayerId, killer != null ? killer.PlayerId : target.PlayerId, tpos));
            snapshot = new Dictionary<byte, Vector2>();
            foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                if (o != null && o.Data != null && !o.Data.Disconnected && !WallVisual.IsPuppet(o) && (!o.Data.IsDead || o.PlayerId == target.PlayerId))
                    snapshot[o.PlayerId] = o.PlayerId == target.PlayerId ? tpos : o.GetTruePosition();
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
                Say(w, T(Translator.IsRu(w), $"Ты видел убийство! Убийца — {named.Data.PlayerName}.", $"You saw a kill! The killer is {named.Data.PlayerName}."));
            }
            if (RoleLogic.Has(target, "Marked")) markedBy[target.PlayerId] = killer.PlayerId;
            if (RoleLogic.Has(target, "Avenger")) { avenge.Add((killer.PlayerId, Time.time + G("AvengerDelay"))); Say(killer, T(Translator.IsRu(killer), "Ты чувствуешь, что за эту смерть придётся расплатиться…", "You feel you will pay for this death...")); }
            if (RoleLogic.Has(killer, "Ghoul") && rng.Next(100) < G("GhoulEat"))
            {
                RoleLogic.Eaten.Add(target.PlayerId);
                Say(killer, T(Translator.IsRu(killer), "Ты съел жертву целиком.", "You ate the victim whole."));
            }
            if (RoleLogic.Has(killer, "Reaper") && rng.Next(100) < G("ReaperShield") && !RoleLogic.ShieldArmed.Contains(killer.PlayerId))
            { GiveShield(killer); Say(killer, T(Translator.IsRu(killer), "Жатва удалась: щит готов.", "The harvest worked: shield ready.")); }
            if (RoleLogic.Has(killer, "Returner") && home.TryGetValue(killer.PlayerId, out var h))
                tpQueue.Add((killer.PlayerId, h, Time.time + 0.8f));
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.OnKill: " + e.Message); }
    }

    public static void OnMeeting()
    {
        try
        {
            var alive = Players();
            // чистим всё, что живёт только до собрания
            posts.Clear(); mines.Clear(); ward.Clear(); peace.Clear(); home.Clear(); tpQueue.Clear(); freezes.Clear(); bodies.Clear(); hermit.Clear(); lastRoomOf.Clear();
            foreach (var kv in RoleState.Assigned.ToList())
            {
                var p = Utils.GetPlayer(kv.Key);
                if (!Alive(p)) continue;
                bool ru = Translator.IsRu(p);
                if (kv.Value.Id == "Archivist")
                {
                    if (deaths.Count == 0) Secret(p, T(ru, "Архив: со времени прошлого собрания никто не погиб.", "Archive: nobody has died since the last meeting."));
                    else
                    {
                        var parts = deaths.Select(d => { var v = Utils.GetPlayer(d.victim); return $"{(v != null && v.Data != null ? v.Data.PlayerName : "?")} — {Room(d.pos, ru)}"; });
                        Secret(p, T(ru, "Архив смертей: ", "Death archive: ") + string.Join("; ", parts));
                    }
                }
                else if (kv.Value.Id == "Lottery" && rng.Next(100) < G("LotteryWin"))
                {
                    if (!RoleLogic.ShieldArmed.Contains(p.PlayerId)) { GiveShield(p); Secret(p, T(ru, "Лотерея: ты выиграл щит!", "Lottery: you won a shield!")); }
                }
                else if (kv.Value.Id == "Rumormonger" && rng.Next(100) < G("RumorChance"))
                {
                    var subj = alive.Where(o => o.PlayerId != p.PlayerId && !IsImp(o)).ToList();
                    if (subj.Count == 0) continue;
                    var s = subj[rng.Next(subj.Count)];
                    foreach (var o in alive)
                        if (!IsImp(o)) Secret(o, T(Translator.IsRu(o), $"Слух: говорят, {s.Data.PlayerName} вёл себя подозрительно.", $"Rumor: they say {s.Data.PlayerName} was acting suspicious."));
                    Say(p, T(ru, $"Слух о {s.Data.PlayerName} пущен.", $"The rumor about {s.Data.PlayerName} is out."));
                }
            }
            // Меченый
            foreach (var kv in markedBy.ToList())
            {
                var real = Utils.GetPlayer(kv.Value);
                if (real == null || real.Data == null) continue;
                var named = real;
                if (rng.Next(100) >= G("MarkedTrue"))
                {
                    var pool = alive.Where(o => o.PlayerId != real.PlayerId && !IsImp(o)).ToList();
                    if (pool.Count > 0) named = pool[rng.Next(pool.Count)];
                }
                foreach (var o in alive)
                    if (!IsImp(o)) Secret(o, T(Translator.IsRu(o), $"Меченый перед смертью увидел убийцу: {named.Data.PlayerName}.", $"The Marked saw his killer before dying: {named.Data.PlayerName}."));
            }
            markedBy.Clear(); deaths.Clear();
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.OnMeeting: " + e.Message); }
    }

    // ---------------------------------------------------------------- Тик (мины, посты, заморозка, пассивные роли)
    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null || RoleState.Assigned.Count == 0) return;
        try
        {
            float now = Time.time;
            var players = Players();

            // отложенные телепорты (Возвращенец)
            for (int i = tpQueue.Count - 1; i >= 0; i--)
                if (now >= tpQueue[i].at) { var t = Utils.GetPlayer(tpQueue[i].id); if (Alive(t)) Commands.Tp(t, tpQueue[i].pos); tpQueue.RemoveAt(i); }

            // Мститель: расплата убийцы
            for (int i = avenge.Count - 1; i >= 0; i--)
                if (now >= avenge[i].at)
                {
                    var k = Utils.GetPlayer(avenge[i].killer); avenge.RemoveAt(i);
                    if (Alive(k)) { Say(k, T(Translator.IsRu(k), "Месть настигла тебя.", "Revenge has caught up with you.")); RoleLogic.Kill(k, k, true); }
                }

            // мины
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

            // капканы
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

            // посты Часового
            for (int i = posts.Count - 1; i >= 0; i--)
            {
                var po = posts[i]; var owner = Utils.GetPlayer(po.owner);
                if (!Alive(owner)) { posts.RemoveAt(i); continue; }
                var who = players.FirstOrDefault(o => o.PlayerId != po.owner && Vector2.Distance(o.GetTruePosition(), po.pos) < 1.2f);
                if (who == null) continue;
                posts.RemoveAt(i);
                Say(owner, T(Translator.IsRu(owner), $"Пост сработал: {who.Data.PlayerName} прошёл у поста ({Room(po.pos, true)}).", $"Post triggered: {who.Data.PlayerName} passed ({Room(po.pos, false)})."));
            }

            // заморозка Гипнотизёра
            for (int i = freezes.Count - 1; i >= 0; i--)
            {
                var f = freezes[i]; var t = Utils.GetPlayer(f.Id);
                if (now >= f.Until || !Alive(t)) { freezes.RemoveAt(i); continue; }
                if (now >= f.NextTp && Vector2.Distance(t.GetTruePosition(), f.Pos) > 0.15f) { Commands.Tp(t, f.Pos); f.NextTp = now + 0.4f; }
            }

            // Скупщик: щит за задания экипажа
            if (RoleState.Assigned.Values.Any(r => r.Id == "Fence"))
            {
                int total = 0;
                foreach (var o in PlayerControl.AllPlayerControls.ToArray())
                    if (o != null && o.Data != null && !o.Data.Disconnected && !WallVisual.IsPuppet(o) && !IsImp(o) && o.Data.Tasks != null)
                        total += o.Data.Tasks.ToArray().Count(x => x.Complete);
                if (fenceLast < 0) fenceLast = total;
                fenceAcc += Math.Max(0, total - fenceLast); fenceLast = total;
                if (fenceAcc >= G("FenceTasks"))
                {
                    fenceAcc -= G("FenceTasks");
                    foreach (var kv in RoleState.Assigned.ToList())
                    {
                        if (kv.Value.Id != "Fence") continue;
                        var fp = Utils.GetPlayer(kv.Key);
                        if (Alive(fp) && !RoleLogic.ShieldArmed.Contains(kv.Key)) { GiveShield(fp); Say(fp, T(Translator.IsRu(fp), "Скупка удалась: щит готов.", "The fencing paid off: shield ready.")); }
                    }
                }
            }

            // пассивные роли
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
                    case "Hermit":
                        {
                            bool alone = !players.Any(o => o.PlayerId != id && Vector2.Distance(o.GetTruePosition(), pos) < 6f);
                            if (!alone) { hermit.Remove(id); break; }
                            if (!hermit.TryGetValue(id, out var since)) { hermit[id] = now; break; }
                            if (now - since >= G("HermitSecs"))
                            {
                                hermit[id] = now;
                                if (!RoleLogic.ShieldArmed.Contains(id)) { GiveShield(p); Say(p, T(ru, "Одиночество придало сил: щит готов.", "Solitude gave you strength: shield ready.")); }
                            }
                            break;
                        }
                    case "Tourist":
                        {
                            string key = NR.RoomKey(pos);
                            if (key == null) break;
                            if (lastRoomOf.TryGetValue(id, out var prev) && prev == key) break;
                            lastRoomOf[id] = key;
                            if (!visited.TryGetValue(id, out var set)) visited[id] = set = new HashSet<string>();
                            set.Add(key);
                            if (set.Count >= G("TouristRooms"))
                            {
                                set.Clear();
                                if (!RoleLogic.ShieldArmed.Contains(id)) { GiveShield(p); Say(p, T(ru, "Ты обошёл много комнат: щит готов.", "You visited many rooms: shield ready.")); }
                            }
                            break;
                        }
                    case "Sleepwalker":
                        {
                            if (!sleepNext.TryGetValue(id, out var nx)) { sleepNext[id] = now + rng.Next(G("SleepMin"), Math.Max(G("SleepMin") + 1, G("SleepMax"))); break; }
                            if (now < nx) break;
                            sleepNext[id] = now + rng.Next(G("SleepMin"), Math.Max(G("SleepMin") + 1, G("SleepMax")));
                            var others = players.Where(o => o.PlayerId != id).ToList();
                            if (others.Count == 0) break;
                            var t = others[rng.Next(others.Count)];
                            Commands.Tp(p, t.GetTruePosition() + new Vector2(0.5f, 0f));
                            Say(p, T(ru, $"Ты проснулся в другом месте: {Room(t.GetTruePosition(), true)}.", "You woke up somewhere else."));
                            break;
                        }
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles30.Tick: " + e.Message); }
    }
}
