using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

public static partial class Roles75
{
    static readonly System.Random rng = new();
    static readonly Dictionary<string, IntOpt> X = new();
    static int G(string k) => X.TryGetValue(k, out var v) ? v.Value : 0;

    public static void Define(ConfigFile c)
    {
        void R(string id, string ru, string en, string color, OptTab tab, string dru, string den) => Opts.AddRole(c, id, ru, en, color, tab, dru, den);
        IntOpt E(string role, string key, OptTab tab, int def, int min, int max, int step, string ru, string en)
        { var v = Opts.Extra(role, new IntOpt(key, tab, def, min, max, step, c), ru, en); X[key] = v; return v; }
        const OptTab C = OptTab.Crewmate, I = OptTab.Impostor, N = OptTab.Neutral;

        R("Watcher", "Наблюдатель", "Watcher", "#80DEEA", C,
            "Мирный. Питомец ставит камеру в комнате, где ты стоишь (число камер в настройках). На собрании узнаешь, кто заходил в эти комнаты. [Питомец]",
            "Crewmate. The pet places a camera in the room you stand in (number of cameras is in the settings). At the meeting you learn who entered those rooms. [Pet]");
        R("Radioman", "Радист", "Radioman", "#4DB6AC", C,
            "Мирный. Питомец включает радар: ты узнаешь, как близко ближайший предатель (холодно, тепло или горячо). Имя и комнату радар не показывает. [Питомец]",
            "Crewmate. The pet switches on a radar: you learn how close the nearest impostor is (cold, warm or hot). The radar does not show a name or a room. [Pet]");
        R("Companion", "Спутник", "Companion", "#A5D6A7", C,
            "Мирный. Считает, сколько секунд каждый игрок провёл рядом с тобой. На собрании называет того, кто был рядом дольше всех. [Сама]",
            "Crewmate. Counts how many seconds each player stayed near you. At the meeting names the player who was near you the longest. [Passive]");
        R("Seismograph", "Сейсмограф", "Seismograph", "#BCAAA4", C,
            "Мирный. Когда кого-то убивают, ты сразу чувствуешь толчок и узнаёшь, в какой стороне и как далеко это случилось. Комнату он не называет. [Сама]",
            "Crewmate. When someone is killed you feel a tremor at once and learn in which direction and how far it happened. The room is not named. [Passive]");
        R("Tracker", "Следопыт", "Tracker", "#FFCC80", C,
            "Мирный. Питомец рядом с игроком ставит на него метку. Потом питомец показывает, в какой комнате этот игрок сейчас, а на каждом собрании ты получаешь отчёт о нём. [Питомец]",
            "Crewmate. The pet next to a player tags him. After that the pet shows which room the tagged player is in right now, and at every meeting you get a report about him. [Pet]");
        R("Accountant", "Бухгалтер", "Accountant", "#F48FB1", C,
            "Мирный. На каждом собрании узнаёшь, кто выполнил меньше всех заданий и сколько именно. [Сама]",
            "Crewmate. At every meeting you learn who has completed the fewest tasks and how many. [Passive]");
        R("Beacon", "Маяк", "Beacon", "#FFF59D", C,
            "Мирный. Питомец ставит маяк в комнате. Пока он стоит, ты получаешь сигнал каждый раз, когда туда входит предатель (без имени). На собрании маяк гаснет. [Питомец]",
            "Crewmate. The pet places a beacon in the room. While it stands you get a signal every time an impostor enters that room (no name). The beacon goes out at a meeting. [Pet]");
        R("Sapper", "Сапёр", "Sapper", "#FFAB91", C,
            "Мирный. Питомец обезвреживает мины и капканы предателей в радиусе 3 м. Обезвреживаний: 3, перезарядка 20 с. [Питомец]",
            "Crewmate. The pet defuses impostors' mines and traps within 3 m. Uses: 3, cooldown 20 s. [Pet]");
        R("Herald", "Глашатай", "Herald", "#90CAF9", C,
            "Мирный. Питомец сообщает всем живым игрокам, в какой ты комнате и сколько людей рядом. Объявлений: 2. [Питомец]",
            "Crewmate. The pet tells every living player which room you are in and how many people are near you. Announcements: 2. [Pet]");
        R("Coach", "Тренер", "Coach", "#CE93D8", C,
            "Мирный. Питомец рядом с членом экипажа (до 3 м) сбрасывает ему перезарядки способностей. Тренировок: 3. [Питомец]",
            "Crewmate. The pet next to a crewmate (within 3 m) resets his ability cooldowns. Trainings: 3. [Pet]");
        R("Telegraphist", "Телеграфист", "Telegraphist", "#90A4AE", C,
            "Мирный. Питомец рядом с игроком (до 3 м) связывает вас телеграфом. Если один из вас погибнет, второй сразу узнает, кто и где. Связь одна. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) links you by telegraph. If one of you is killed the other learns at once who and where. One link at a time. [Pet]");

        R("Banisher", "Выселитель", "Banisher", "#EF5350", I,
            "Предатель. Питомец выбрасывает ближайшего члена экипажа (до 4 м) к случайной двери на карте. Использований: 3, перезарядка 45 с. [Питомец]",
            "Impostor. The pet throws the nearest crewmate (within 4 m) to a random door on the map. Uses: 3, cooldown 45 s. [Pet]");
        R("Hush", "Беззвучный", "Hush", "#78909C", I,
            "Предатель. Питомец включает тишину на несколько секунд: журнал Логера, камеры Наблюдателя, посты Часового, маяки и радары тебя не замечают. [Питомец]",
            "Impostor. The pet switches on silence for a few seconds: the Logger's log, the Watcher's cameras, Sentry posts, beacons and radars do not notice you. [Pet]");
        R("Bomber", "Подрывник", "Bomber", "#FF7043", I,
            "Предатель. Питомец рядом с членом экипажа (до 3 м) цепляет ему бомбу. Через некоторое время она взрывается и убивает всех мирных вокруг (щит спасает). [Питомец]",
            "Impostor. The pet next to a crewmate (within 3 m) sticks a bomb on him. After a while it explodes and kills every crewmate around (a shield saves). [Pet]");
        R("Forger", "Фальсификатор", "Forger", "#BA68C8", I,
            "Предатель. Питомец вписывает в журнал Логера ложную запись: будто случайный член экипажа вошёл в твою комнату. Подделок: 4. [Питомец]",
            "Impostor. The pet writes a false entry into the Logger's log: as if a random crewmate entered your room. Forgeries: 4. [Pet]");
        R("Scatterer", "Разгонщик", "Scatterer", "#FFB74D", I,
            "Предатель. После твоего убийства все члены экипажа рядом с телом (радиус в настройках) разлетаются к случайным дверям. Срабатывает не чаще раза в 40 с. [Сама]",
            "Impostor. After your kill every crewmate near the body (radius in the settings) is thrown to random doors. Works once per 40 s at most. [Passive]");
        R("Spy", "Шпион", "Spy", "#4DD0E1", I,
            "Предатель. Питомец рядом с игроком (до 4 м) раскрывает тебе его роль. Проверок: 3. [Питомец]",
            "Impostor. The pet next to a player (within 4 m) reveals his role to you. Checks: 3. [Pet]");
        R("Spotter", "Наводчик", "Spotter", "#9575CD", I,
            "Предатель. Питомец показывает, сколько членов экипажа сейчас в каждой четверти карты относительно тебя: северо-восток, северо-запад, юго-восток, юго-запад. [Питомец]",
            "Impostor. The pet shows how many crewmates are in each quarter of the map relative to you: north-east, north-west, south-east, south-west. [Pet]");
        R("Fugitive", "Беглец", "Fugitive", "#A1887F", I,
            "Предатель. Через секунду после твоего убийства тебя уносит к случайной двери. Срабатывает не чаще раза в 30 с. [Сама]",
            "Impostor. A second after your kill you are carried to a random door. Works once per 30 s at most. [Passive]");
        R("Overseer", "Надзиратель", "Overseer", "#FFCA28", I,
            "Предатель. Когда член экипажа рядом с тобой (радиус в настройках) заканчивает задание, ты сразу узнаёшь, кто это и в какой он комнате. [Сама]",
            "Impostor. When a crewmate near you (radius in the settings) finishes a task you learn at once who it is and which room he is in. [Passive]");
        R("Gravedigger", "Могильщик", "Gravedigger", "#6D4C41", I,
            "Предатель. Питомец рядом с телом (до 2 м) закапывает его, и о нём нельзя доложить. Захоронений: 2. [Питомец]",
            "Impostor. The pet next to a body (within 2 m) buries it and it cannot be reported. Burials: 2. [Pet]");
        R("Meltdown", "Расплав", "Meltdown", "#E53935", I,
            "Предатель. Питомец запускает тяжёлый саботаж (реактор, лаборатория, вертолёт или кислород - что есть на карте), если сейчас нет другого саботажа. [Питомец]",
            "Impostor. The pet starts a heavy sabotage (reactor, laboratory, helicopter or oxygen, whichever the map has) if no other sabotage is active. [Pet]");
        R("Appraiser", "Оценщик", "Appraiser", "#FFD54F", I,
            "Предатель. Питомец показывает, сколько игроков сейчас со щитом и в какой комнате ближайший из них. Перезарядка 35 с. [Питомец]",
            "Impostor. The pet shows how many players have a shield right now and which room the nearest one is in. Cooldown: 35 s. [Pet]");
        R("Silencer", "Молчун", "Silencer", "#455A64", I,
            "Предатель. Питомец рядом с членом экипажа (до 3 м) на несколько секунд глушит его способности питомцем. Использований: 3. [Питомец]",
            "Impostor. The pet next to a crewmate (within 3 m) silences his pet abilities for a few seconds. Uses: 3. [Pet]");
        R("Distracter", "Отвлекатель", "Distracter", "#26A69A", I,
            "Предатель. Питомец захлопывает двери случайной комнаты далеко от тебя (от 15 м), чтобы отвлечь экипаж. Использований: 4, перезарядка 60 с. [Питомец]",
            "Impostor. The pet slams the doors of a random room far from you (15 m or more) to distract the crew. Uses: 4, cooldown 60 s. [Pet]");

        R("Collector", "Коллекционер", "Collector", "#FFE082", C,
            "Мирный. Питомец рядом с телом (до 2 м) забирает трофей (с одного тела один раз). Когда наберёшь нужное число трофеев, узнаешь имя настоящего предателя. [Питомец]",
            "Crewmate. The pet next to a body (within 2 m) takes a trophy (once per body). When you have enough trophies you learn the name of a real impostor. [Pet]");
        R("Whirlwind", "Вихрь", "Whirlwind", "#80CBC4", C,
            "Мирный. Питомец закручивает вихрь: все игроки вокруг (радиус в настройках) разлетаются к случайным дверям. Использований: 3, перезарядка 60 с. [Питомец]",
            "Crewmate. The pet spins up a whirlwind: every player around (radius in the settings) is thrown to random doors. Uses: 3, cooldown 60 s. [Pet]");
        R("Timestop", "Стоп-кадр", "Timestop", "#B0BEC5", C,
            "Мирный. Питомец останавливает время: все игроки вокруг, кроме тебя, замирают на несколько секунд. [Питомец]",
            "Crewmate. The pet stops time: every player around you, except you, freezes for a few seconds. [Pet]");
        R("Oracle", "Прорицатель", "Oracle", "#B39DDB", C,
            "Мирный. Питомец запускает пророчество: через заданное время ты узнаёшь, сколько убийств за это время случилось. [Питомец]",
            "Crewmate. The pet starts a prophecy: after the set time you learn how many kills happened in that period. [Pet]");
        R("Meteor", "Метеорит", "Meteor", "#FF7043", N,
            "Нейтрал. Для сервера ты настоящий предатель. Питомец отмечает точку: через несколько секунд туда падает метеорит и убивает всех в радиусе (щит спасает). [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet marks a spot: a few seconds later a meteor hits it and kills everyone in the radius (a shield saves). [Pet]");
        R("Siren", "Сирена", "Siren", "#FF8A80", C,
            "Мирный. Питомец включает сирену: у всех предателей рядом (радиус в настройках) мигает экран, а ты узнаёшь, сколько их было. [Питомец]",
            "Crewmate. The pet sounds a siren: the screens of all impostors nearby (radius in the settings) flash and you learn how many there were. [Pet]");
        R("Insider", "Знаток", "Insider", "#AED581", C,
            "Мирный. На первом собрании ты узнаёшь имена остальных нейтралов. [Сама]",
            "Crewmate. At the first meeting you learn the names of the other neutrals. [Passive]");
        R("Broker", "Брокер", "Broker", "#FFF176", C,
            "Мирный. Питомец рядом с игроком (до 3 м) заключает сделку: ты узнаёшь его роль, а он узнаёт, в какой комнате ближайшее тело. Сделок: 3. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) makes a deal: you learn his role and he learns which room the nearest body is in. Deals: 3. [Pet]");
        R("Portalist", "Портальщик", "Portalist", "#4FC3F7", C,
            "Мирный. Питомец ставит первый портал, потом второй. Когда оба стоят, питомец у одного портала переносит тебя ко второму. Питомец вдали от порталов начинает новую пару. [Питомец]",
            "Crewmate. The pet places a first portal and then a second one. When both stand, the pet next to one portal carries you to the other. The pet away from the portals starts a new pair. [Pet]");
        R("Photographer", "Фотограф", "Photographer", "#CE93D8", C,
            "Мирный. Питомец делает снимок: кто где был в этот момент. На ближайшем собрании ты получаешь все свои снимки. Снимков: 3. [Питомец]",
            "Crewmate. The pet takes a snapshot of who was where at that moment. At the next meeting you receive all your snapshots. Snapshots: 3. [Pet]");
        R("Boomerang", "Бумеранг", "Boomerang", "#FFB74D", C,
            "Мирный. Питомец рядом с игроком (до 5 м) запускает бумеранг: через несколько секунд игрок возвращается к тебе, туда, где ты стоишь. [Питомец]",
            "Crewmate. The pet next to a player (within 5 m) throws a boomerang: a few seconds later that player is carried back to wherever you stand. [Pet]");
        R("Chronicler", "Летописец", "Chronicler", "#BCAAA4", C,
            "Мирный. Когда кого-то убивают, ты сразу узнаёшь, кто погиб и к какой группе принадлежал убийца (предатель или нейтрал). [Сама]",
            "Crewmate. When someone is killed you learn at once who died and which group the killer belonged to (impostor or neutral). [Passive]");
        R("Sorcerer", "Колдун", "Sorcerer", "#7E57C2", C,
            "Мирный. Питомец рядом с игроком (до 3 м) накладывает проклятие: его следующая способность питомцем не сработает, а экран замигает. [Питомец]",
            "Crewmate. The pet next to a player (within 3 m) casts a curse: his next pet ability fails and his screen flashes. [Pet]");
        R("Rescuer", "Спасатель", "Rescuer", "#A5D6A7", C,
            "Мирный. Питомец рядом с игроком, застрявшим в паутине (до 2,5 м), сразу освобождает его. Спасений: 3. [Питомец]",
            "Crewmate. The pet next to a player stuck in a web (within 2.5 m) frees him at once. Rescues: 3. [Pet]");

        E("Watcher", "WatcherCd", C, 30, 5, 120, 5, "Перезарядка камеры (сек)", "Camera cooldown (sec)");
        E("Watcher", "WatcherMax", C, 2, 1, 4, 1, "Камер одновременно", "Cameras at a time");
        E("Radioman", "RadioCd", C, 30, 5, 120, 5, "Перезарядка радара (сек)", "Radar cooldown (sec)");
        E("Companion", "CompanionRadius", C, 4, 2, 10, 1, "Радиус «рядом» (м)", "\"Near\" radius (m)");
        E("Tracker", "TrackerCd", C, 30, 5, 120, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Beacon", "BeaconCd", C, 25, 5, 120, 5, "Перезарядка маяка (сек)", "Beacon cooldown (sec)");
        E("Sapper", "SapperCd", C, 20, 5, 90, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Sapper", "SapperUses", C, 3, 1, 10, 1, "Обезвреживаний за игру", "Defusals per game");
        E("Herald", "HeraldCd", C, 40, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Herald", "HeraldUses", C, 2, 1, 8, 1, "Объявлений за игру", "Announcements per game");
        E("Coach", "CoachCd", C, 60, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Coach", "CoachUses", C, 3, 1, 10, 1, "Тренировок за игру", "Trainings per game");
        E("Telegraphist", "TelegraphCd", C, 30, 5, 120, 5, "Перезарядка связи (сек)", "Link cooldown (sec)");

        E("Banisher", "BanishCd", I, 45, 10, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Banisher", "BanishUses", I, 3, 1, 10, 1, "Использований за игру", "Uses per game");
        E("Hush", "HushCd", I, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Hush", "HushSecs", I, 15, 5, 60, 1, "Длительность тишины (сек)", "Silence duration (sec)");
        E("Bomber", "BombCd", I, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Bomber", "BombUses", I, 2, 1, 6, 1, "Бомб за игру", "Bombs per game");
        E("Bomber", "BombSecs", I, 25, 10, 90, 5, "Секунд до взрыва", "Seconds until the explosion");
        E("Bomber", "BombRadius", I, 3, 1, 8, 1, "Радиус взрыва (м)", "Blast radius (m)");
        E("Forger", "ForgerCd", I, 45, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Forger", "ForgerUses", I, 4, 1, 12, 1, "Подделок за игру", "Forgeries per game");
        E("Scatterer", "ScatterCd", I, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Scatterer", "ScatterRadius", I, 5, 2, 12, 1, "Радиус разлёта (м)", "Scatter radius (m)");
        E("Spy", "SpyCd", I, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Spy", "SpyUses", I, 3, 1, 10, 1, "Проверок за игру", "Checks per game");
        E("Spotter", "SpotterCd", I, 40, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Fugitive", "FugitiveCd", I, 30, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Overseer", "OverseerRadius", I, 10, 3, 25, 1, "Радиус наблюдения (м)", "Watch radius (m)");
        E("Gravedigger", "GraveCd", I, 30, 5, 120, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Gravedigger", "GraveUses", I, 2, 1, 8, 1, "Захоронений за игру", "Burials per game");
        E("Meltdown", "MeltCd", I, 120, 30, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Meltdown", "MeltUses", I, 2, 1, 5, 1, "Запусков за игру", "Starts per game");
        E("Appraiser", "AppraiserCd", I, 35, 5, 180, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Silencer", "SilenceCd", I, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Silencer", "SilenceUses", I, 3, 1, 8, 1, "Использований за игру", "Uses per game");
        E("Silencer", "SilenceSecs", I, 30, 10, 90, 5, "Сколько секунд глушит", "Silence duration (sec)");
        E("Distracter", "DistractCd", I, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Distracter", "DistractUses", I, 4, 1, 12, 1, "Использований за игру", "Uses per game");

        E("Collector", "CollectorCd", N, 15, 5, 90, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Collector", "CollectorNeed", N, 3, 1, 8, 1, "Трофеев до имени предателя", "Trophies until an impostor's name");
        E("Whirlwind", "WhirlCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Whirlwind", "WhirlUses", N, 3, 1, 8, 1, "Вихрей за игру", "Whirlwinds per game");
        E("Whirlwind", "WhirlRadius", N, 8, 3, 15, 1, "Радиус вихря (м)", "Whirlwind radius (m)");
        E("Timestop", "StopCd", N, 90, 20, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Timestop", "StopUses", N, 2, 1, 6, 1, "Остановок за игру", "Stops per game");
        E("Timestop", "StopSecs", N, 3, 1, 10, 1, "Сколько секунд игроки замирают", "Seconds the players freeze");
        E("Timestop", "StopRadius", N, 8, 3, 15, 1, "Радиус (м)", "Radius (m)");
        E("Oracle", "OracleCd", N, 90, 20, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Oracle", "OracleSecs", N, 30, 10, 120, 5, "Срок пророчества (сек)", "Prophecy period (sec)");
        E("Meteor", "MeteorCd", N, 120, 30, 300, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Meteor", "MeteorUses", N, 2, 1, 5, 1, "Метеоритов за игру", "Meteors per game");
        E("Meteor", "MeteorDelay", N, 5, 2, 15, 1, "Секунд до падения", "Seconds until impact");
        E("Meteor", "MeteorRadius", N, 3, 1, 8, 1, "Радиус удара (м)", "Impact radius (m)");
        E("Siren", "SirenCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Siren", "SirenUses", N, 3, 1, 8, 1, "Сирен за игру", "Sirens per game");
        E("Siren", "SirenRadius", N, 15, 5, 30, 1, "Радиус сирены (м)", "Siren radius (m)");
        E("Broker", "BrokerCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Broker", "BrokerUses", N, 3, 1, 8, 1, "Сделок за игру", "Deals per game");
        E("Portalist", "PortalCd", N, 10, 3, 60, 1, "Перезарядка переноса (сек)", "Teleport cooldown (sec)");
        E("Photographer", "PhotoCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Photographer", "PhotoUses", N, 3, 1, 8, 1, "Снимков за игру", "Snapshots per game");
        E("Boomerang", "BoomerangCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Boomerang", "BoomerangUses", N, 3, 1, 8, 1, "Бросков за игру", "Throws per game");
        E("Boomerang", "BoomerangSecs", N, 8, 3, 30, 1, "Секунд до возврата", "Seconds until the return");
        E("Sorcerer", "CurseCd", N, 60, 10, 240, 10, "Перезарядка (сек)", "Cooldown (sec)");
        E("Sorcerer", "CurseUses", N, 3, 1, 8, 1, "Проклятий за игру", "Curses per game");
        E("Rescuer", "RescueCd", N, 30, 5, 120, 5, "Перезарядка (сек)", "Cooldown (sec)");
        E("Rescuer", "RescueUses", N, 3, 1, 10, 1, "Спасений за игру", "Rescues per game");
    }

    static readonly Dictionary<string, float> lastUse = new();
    static readonly Dictionary<string, int> lastCd = new();
    static readonly Dictionary<string, int> useMax = new();
    static readonly Dictionary<string, int> uses = new();

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => p != null && p.Data != null && !RoleLogic.Demoted.Contains(p.PlayerId) && (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId));
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
    static bool Ready(string key, PlayerControl p, int cd) => !(lastUse.TryGetValue(key + ":" + p.PlayerId, out var l) && Time.time - l < cd);
    static bool NoUses(string key, PlayerControl p, int max, bool ru)
    {
        useMax[key + p.PlayerId] = max;
        if (Used(key, p.PlayerId) < max) return false;
        Say(p, T(ru, "Использования закончились.", "No uses left.")); return true;
    }

    static PlayerControl Nearest(PlayerControl me, float maxDist, Func<PlayerControl, bool> ok)
    {
        PlayerControl best = null; float bd = maxDist; var pos = me.GetTruePosition();
        foreach (var cand in Players())
        {
            if (cand.PlayerId == me.PlayerId || !ok(cand)) continue;
            float d = Vector2.Distance(pos, cand.GetTruePosition());
            if (d < bd) { bd = d; best = cand; }
        }
        return best;
    }

    static bool RandomDoor(out Vector2 pos, Func<Vector2, bool> ok = null)
    {
        pos = default;
        var ss = ShipStatus.Instance; var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) return false;
        for (int k = 0; k < 15; k++)
        {
            var door = doors[rng.Next(doors.Length)];
            if (door == null) continue;
            var dp = (Vector2)door.transform.position;
            if (ok != null && !ok(dp)) continue;
            pos = dp; return true;
        }
        return false;
    }

    static string Compass(Vector2 d, bool ru)
    {
        double ang = Math.Atan2(d.y, d.x) * 180.0 / Math.PI;
        int sector = (int)Math.Floor(((ang + 22.5 + 360.0) % 360.0) / 45.0);
        string[] ruN = { "востоке", "северо-востоке", "севере", "северо-западе", "западе", "юго-западе", "юге", "юго-востоке" };
        string[] enN = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        return ru ? ruN[sector] : enN[sector];
    }

    static string PlayerName(byte id) { var p = Utils.GetPlayer(id); return p != null && p.Data != null ? p.Data.PlayerName : "?"; }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p); string n = r.Colored(ru); byte id = p.PlayerId;
        int L(string k, int max) => Math.Max(0, max - Used(k, id));
        switch (r.Id)
        {
            case "Watcher": return T(ru, $"{n}: питомец ставит камеру в комнате. Камер: {(cams.TryGetValue(id, out var cl) ? cl.Count : 0)}/{G("WatcherMax")}. На собрании узнаёшь, кто заходил.", $"{n}: pet places a camera in a room. Cameras: {(cams.TryGetValue(id, out var cl2) ? cl2.Count : 0)}/{G("WatcherMax")}. At the meeting you learn who entered.");
            case "Radioman": return T(ru, $"{n}: питомец включает радар: холодно, тепло или горячо до ближайшего предателя.", $"{n}: the pet switches on a radar: cold, warm or hot to the nearest impostor.");
            case "Companion": return T(ru, $"{n}: на собрании узнаёшь, кто дольше всех был рядом с тобой (до {G("CompanionRadius")} м).", $"{n}: at the meeting you learn who stayed near you the longest (within {G("CompanionRadius")} m).");
            case "Seismograph": return T(ru, $"{n}: при каждом убийстве чувствуешь, в какой стороне и как далеко это случилось.", $"{n}: at every kill you feel in which direction and how far it happened.");
            case "Tracker": return T(ru, $"{n}: питомец рядом с игроком метит его. Метка: {(tagged.TryGetValue(id, out var tg) ? PlayerName(tg) : "нет")}.", $"{n}: pet next to a player tags him. Tag: {(tagged.TryGetValue(id, out var tg2) ? PlayerName(tg2) : "none")}.");
            case "Accountant": return T(ru, $"{n}: на каждом собрании узнаёшь, кто выполнил меньше всех заданий.", $"{n}: at every meeting you learn who completed the fewest tasks.");
            case "Beacon": return T(ru, $"{n}: питомец ставит маяк; сигнал, когда в комнату входит предатель. Маяк: {(beacon.ContainsKey(id) ? "стоит" : "нет")}.", $"{n}: pet places a beacon; a signal when an impostor enters the room. Beacon: {(beacon.ContainsKey(id) ? "placed" : "none")}.");
            case "Sapper": return T(ru, $"{n}: питомец обезвреживает мины и капканы (до 3 м). Осталось: {L("sapper", G("SapperUses"))}.", $"{n}: pet defuses mines and traps (within 3 m). Left: {L("sapper", G("SapperUses"))}.");
            case "Herald": return T(ru, $"{n}: питомец рассылает всем, в какой ты комнате и сколько людей рядом. Осталось: {L("herald", G("HeraldUses"))}.", $"{n}: pet tells everyone your room and how many people are near. Left: {L("herald", G("HeraldUses"))}.");
            case "Coach": return T(ru, $"{n}: питомец рядом с членом экипажа сбрасывает ему перезарядки. Осталось: {L("coach", G("CoachUses"))}.", $"{n}: pet next to a crewmate resets his cooldowns. Left: {L("coach", G("CoachUses"))}.");
            case "Telegraphist": return T(ru, $"{n}: питомец рядом с игроком связывает вас. Связь: {(link.TryGetValue(id, out var lk) ? PlayerName(lk) : "нет")}.", $"{n}: pet next to a player links you. Link: {(link.TryGetValue(id, out var lk2) ? PlayerName(lk2) : "none")}.");
            case "Banisher": return T(ru, $"{n}: питомец выбрасывает ближайшего члена экипажа к случайной двери. Осталось: {L("banish", G("BanishUses"))}.", $"{n}: pet throws the nearest crewmate to a random door. Left: {L("banish", G("BanishUses"))}.");
            case "Hush": return T(ru, $"{n}: питомец включает тишину на {G("HushSecs")} с - камеры, журнал, посты и маяки тебя не видят.", $"{n}: pet switches on silence for {G("HushSecs")} s - cameras, log, posts and beacons do not see you.");
            case "Bomber": return T(ru, $"{n}: питомец цепляет бомбу члену экипажа; взрыв через {G("BombSecs")} с. Осталось: {L("bomb", G("BombUses"))}.", $"{n}: pet sticks a bomb on a crewmate; it explodes in {G("BombSecs")} s. Left: {L("bomb", G("BombUses"))}.");
            case "Forger": return T(ru, $"{n}: питомец вписывает ложную запись в журнал Логера. Осталось: {L("forger", G("ForgerUses"))}.", $"{n}: pet writes a false entry into the Logger's log. Left: {L("forger", G("ForgerUses"))}.");
            case "Scatterer": return T(ru, $"{n}: после убийства свидетелей рядом ({G("ScatterRadius")} м) разбрасывает по дверям.", $"{n}: after a kill the witnesses nearby ({G("ScatterRadius")} m) are thrown to doors.");
            case "Spy": return T(ru, $"{n}: питомец рядом с игроком раскрывает его роль. Осталось: {L("spy", G("SpyUses"))}.", $"{n}: pet next to a player reveals his role. Left: {L("spy", G("SpyUses"))}.");
            case "Spotter": return T(ru, $"{n}: питомец показывает, сколько мирных в каждой четверти карты.", $"{n}: pet shows how many crewmates are in each quarter of the map.");
            case "Fugitive": return T(ru, $"{n}: после убийства тебя уносит к случайной двери.", $"{n}: after a kill you are carried to a random door.");
            case "Overseer": return T(ru, $"{n}: когда мирный рядом ({G("OverseerRadius")} м) заканчивает задание, ты узнаёшь, кто и где.", $"{n}: when a crewmate near you ({G("OverseerRadius")} m) finishes a task you learn who and where.");
            case "Gravedigger": return T(ru, $"{n}: питомец у тела закапывает его, о теле нельзя доложить. Осталось: {L("grave", G("GraveUses"))}.", $"{n}: the pet next to a body buries it, so it cannot be reported. Left: {L("grave", G("GraveUses"))}.");
            case "Meltdown": return T(ru, $"{n}: питомец запускает тяжёлый саботаж. Осталось: {L("melt", G("MeltUses"))}.", $"{n}: pet starts a heavy sabotage. Left: {L("melt", G("MeltUses"))}.");
            case "Appraiser": return T(ru, $"{n}: питомец показывает, сколько игроков со щитом и где ближайший.", $"{n}: pet shows how many players have shields and where the nearest one is.");
            case "Silencer": return T(ru, $"{n}: питомец рядом с мирным глушит его способности на {G("SilenceSecs")} с. Осталось: {L("silence", G("SilenceUses"))}.", $"{n}: pet next to a crewmate silences his abilities for {G("SilenceSecs")} s. Left: {L("silence", G("SilenceUses"))}.");
            case "Distracter": return T(ru, $"{n}: питомец захлопывает двери далёкой комнаты. Осталось: {L("distract", G("DistractUses"))}.", $"{n}: pet slams the doors of a distant room. Left: {L("distract", G("DistractUses"))}.");
            case "Collector": return T(ru, $"{n}: питомец у тела забирает трофей. Трофеев: {(trophies.TryGetValue(id, out var tr) ? tr : 0)}/{G("CollectorNeed")}.", $"{n}: pet next to a body takes a trophy. Trophies: {(trophies.TryGetValue(id, out var tr2) ? tr2 : 0)}/{G("CollectorNeed")}.");
            case "Whirlwind": return T(ru, $"{n}: питомец разбрасывает всех в радиусе {G("WhirlRadius")} м по дверям. Осталось: {L("whirl", G("WhirlUses"))}.", $"{n}: pet throws everyone within {G("WhirlRadius")} m to doors. Left: {L("whirl", G("WhirlUses"))}.");
            case "Timestop": return T(ru, $"{n}: питомец замораживает всех вокруг на {G("StopSecs")} с. Осталось: {L("stop", G("StopUses"))}.", $"{n}: pet freezes everyone around for {G("StopSecs")} s. Left: {L("stop", G("StopUses"))}.");
            case "Oracle": return T(ru, $"{n}: питомец запускает пророчество на {G("OracleSecs")} с - узнаешь число убийств за срок.", $"{n}: pet starts a prophecy for {G("OracleSecs")} s - you learn the number of kills in that period.");
            case "Meteor": return T(ru, $"{n}: питомец отмечает точку; через {G("MeteorDelay")} с там падает метеорит. Осталось: {L("meteor", G("MeteorUses"))}.", $"{n}: pet marks a spot; a meteor hits it in {G("MeteorDelay")} s. Left: {L("meteor", G("MeteorUses"))}.");
            case "Siren": return T(ru, $"{n}: питомец включает сирену: у предателей рядом мигает экран. Осталось: {L("siren", G("SirenUses"))}.", $"{n}: pet sounds a siren: nearby impostors' screens flash. Left: {L("siren", G("SirenUses"))}.");
            case "Insider": return T(ru, $"{n}: на первом собрании узнаёшь имена остальных нейтралов.", $"{n}: at the first meeting you learn the names of the other neutrals.");
            case "Broker": return T(ru, $"{n}: питомец рядом с игроком - сделка: ты узнаёшь его роль, он - где ближайшее тело. Осталось: {L("broker", G("BrokerUses"))}.", $"{n}: pet next to a player - a deal: you learn his role, he learns where the nearest body is. Left: {L("broker", G("BrokerUses"))}.");
            case "Portalist": return T(ru, $"{n}: питомец ставит порталы и переносит между ними. Порталы: {(portalA.ContainsKey(id) ? 1 : 0) + (portalB.ContainsKey(id) ? 1 : 0)}/2.", $"{n}: pet places portals and carries you between them. Portals: {(portalA.ContainsKey(id) ? 1 : 0) + (portalB.ContainsKey(id) ? 1 : 0)}/2.");
            case "Photographer": return T(ru, $"{n}: питомец делает снимок: кто где стоит. Осталось: {L("photo", G("PhotoUses"))}.", $"{n}: the pet takes a snapshot of who is where. Left: {L("photo", G("PhotoUses"))}.");
            case "Boomerang": return T(ru, $"{n}: питомец рядом с игроком - через {G("BoomerangSecs")} с он вернётся к тебе. Осталось: {L("boom", G("BoomerangUses"))}.", $"{n}: pet next to a player - in {G("BoomerangSecs")} s he returns to you. Left: {L("boom", G("BoomerangUses"))}.");
            case "Chronicler": return T(ru, $"{n}: при каждом убийстве узнаёшь, кто погиб и к какой группе принадлежал убийца.", $"{n}: at every kill you learn who died and which group the killer belonged to.");
            case "Sorcerer": return T(ru, $"{n}: питомец рядом с игроком проклинает его: следующая способность сорвётся. Осталось: {L("curse", G("CurseUses"))}.", $"{n}: pet next to a player curses him: his next ability fails. Left: {L("curse", G("CurseUses"))}.");
            case "Rescuer": return T(ru, $"{n}: питомец рядом с застрявшим в паутине освобождает его. Осталось: {L("rescue", G("RescueUses"))}.", $"{n}: pet next to a player stuck in a web frees him. Left: {L("rescue", G("RescueUses"))}.");
        }
        return null;
    }
}
