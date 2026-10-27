using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace AshesOfDisaster;

public enum OptTab { System, Game, Crewmate, Impostor, Neutral, Other, Ghost, Mini, Hidden, Mystic }

public abstract class Opt
{
    public static readonly List<Opt> All = new();
    public string Id;
    public OptTab Tab;
    protected Opt(string id, OptTab tab) { Id = id; Tab = tab; All.Add(this); }
    public string NameRu, NameEn;
    public string ShortRu, ShortEn;
    public string Short => ShortRu == null ? Name : (Translator.GameIsRu ? ShortRu : ShortEn);
    public string Name => NameRu != null ? (Translator.GameIsRu ? NameRu : NameEn) : Translator.T("opt." + Id);
    public abstract string ValueText { get; }
    public abstract void Set(string value);
    public abstract void Next();
    public abstract void Inc(int dir);
    public string GroupRu, GroupEn;
    public string Group => GroupRu == null ? null : (Translator.GameIsRu ? GroupRu : GroupEn);
}

public class BoolOpt : Opt
{
    readonly ConfigEntry<bool> e;
    public bool Value { get => e.Value; set => e.Value = value; }
    public BoolOpt(string id, OptTab tab, bool def, ConfigFile cfg) : base(id, tab) => e = cfg.Bind(tab.ToString(), id, def);
    public override string ValueText => Value ? "ON" : "OFF";
    public override void Set(string v) => Value = v is "1" or "on" or "true" or "вкл" or "да";
    public override void Next() => Value = !Value;
    public override void Inc(int dir) => Value = dir > 0;
    public static implicit operator bool(BoolOpt o) => o.Value;
}

public class IntOpt : Opt
{
    readonly ConfigEntry<int> e;
    public readonly int Min, Max, Step;
    public int Value { get => e.Value; set => e.Value = Math.Clamp(value, Min, Max); }
    public IntOpt(string id, OptTab tab, int def, int min, int max, int step, ConfigFile cfg) : base(id, tab)
    { Min = min; Max = max; Step = step; e = cfg.Bind(tab.ToString(), id, def); }
    public override string ValueText => Value.ToString();
    public override void Set(string v) { if (int.TryParse(v, out var n)) Value = n; }
    public override void Next() => Value = Value + Step > Max ? Min : Value + Step;
    public override void Inc(int dir) => Value += dir * Step;
    public static implicit operator int(IntOpt o) => o.Value;
}

public class RoleDef
{
    public string Id, Ru, En, Color, DescRu, DescEn;
    public OptTab Tab;
    public string Colored(bool ru) => $"<color={Color}>{(ru ? Ru : En)}</color>";
    public IntOpt Chance, Count;
    public List<Opt> Extras = new();
}

public static class Opts
{
    public static ConfigEntry<string> ChatPrivate;
    public static ConfigEntry<bool> MapMarkers, MarkersAll, NeutralSuicide, AutoTest, HorseCostume;
    public static ConfigEntry<int> HorseSize, HorseShift;
    public static ConfigEntry<string> RoomShareUrl;
    public static BoolOpt ShareRoom;
    public static ConfigEntry<bool> NativeMenu, ReplacePresets, UseMask, NativeScroll, HostBadge, HostOnlyForce, NameRefresh, NameViaRpc, OptEnable, OptNoVsync, OptBackground, OptCleanup;
    public static ConfigEntry<int> OptFps;
    public static ConfigEntry<float> ViewHeight, ClipMargin, ScrollStep;
    public static ConfigEntry<float> RowX, RowY, RowSpacing, RowShift;
    public static readonly List<RoleDef> Roles = new();

    public static BoolOpt AnyPlayers, AutoStart, Welcome, PlayerCommands, ChatColors, ChatNoLimit;
    public static IntOpt AutoStartPlayers, AutoStartDelay;
    public static BoolOpt UseBanList, UseDenyNames, KickLowLevel, KickNoFriendCode;
    public static IntOpt MinLevel;
    public static BoolOpt NoEndFewPlayers, NoGameEnd, SyncButtonMode, DisableCloseDoors, GhostSeeRoles, NameStatus, RevealOnExile, PetForAll, TeamSwap;
    public static IntOpt SyncedButtonCount, KillCooldownPercent;
    public static IntOpt BuilderCooldown, CloudMax, CloudRadius, CloudFlash, CloudStormChance, PsyCooldown, PsyMaxUses, PsyFailChance, WelderMax, WelderCooldown, DowserCooldown, DowserRadius, PinkCook, PinkMax, PinkPurity, PinkSaleCooldown, SmokeMax, SmokeLife, SmokeKill, SmokeRadius, SmokeCooldown, ShroomUses, ShroomCooldown, ShroomSeconds, PlumberUses, PlumberCooldown, TwinRadius, TwinCooldown, ForemanChance, NavCooldown, HoundCooldown, PrankCooldown, CrowCooldown, CrowUses, DefectorWrong, ReflectChance;
    public static BoolOpt CloudStormMarker, HostAuthority;
    public static BoolOpt ShowCredits;

    static readonly HashSet<string> Retired = new()
    {
        "Seismograph", "Oracle", "Watcher", "Photographer", "Companion", "Beacon", "Siren", "Timestop", "Defector", "Insider",
        "Boomerang", "Sorcerer", "Navigator", "Eclipser", "Spotter", "Trapper"
    };
    static readonly List<RoleDef> RetiredRoles = new();

    internal static void AddRole(ConfigFile c, string id, string ru, string en, string color, OptTab tab, string descRu, string descEn)
    {
        var r = new RoleDef { Id = id, Ru = ru, En = en, Color = color, Tab = tab, DescRu = descRu, DescEn = descEn };
        r.Chance = new IntOpt(id + "Chance", tab, 100, 0, 100, 10, c);
        r.Count = new IntOpt(id + "Count", tab, 1, 1, 15, 1, c);
        r.Chance.ShortRu = "Шанс появления (%)"; r.Chance.ShortEn = "Spawn chance (%)";
        r.Count.ShortRu = "Количество"; r.Count.ShortEn = "Count";
        r.Chance.NameRu = $"<color={color}>{ru}</color>: шанс (%)";
        r.Chance.NameEn = $"<color={color}>{en}</color>: chance (%)";
        r.Count.NameRu = $"<color={color}>{ru}</color>: количество";
        r.Count.NameEn = $"<color={color}>{en}</color>: count";
        r.Chance.GroupRu = r.Count.GroupRu = $"<color={color}>{ru}</color>";
        r.Chance.GroupEn = r.Count.GroupEn = $"<color={color}>{en}</color>";
        if (Retired.Contains(id)) { r.Chance.Value = 0; RetiredRoles.Add(r); return; }
        Roles.Add(r);
    }

    internal static T Extra<T>(string id, T o, string ru, string en) where T : Opt
    {
        var r = Roles.Find(x => x.Id == id) ?? RetiredRoles.Find(x => x.Id == id);
        o.ShortRu = ru; o.ShortEn = en;
        o.NameRu = $"{r.Colored(true)}: {ru}";
        o.NameEn = $"{r.Colored(false)}: {en}";
        r.Extras.Add(o);
        return o;
    }

    static void Grp(string ru, string en, params Opt[] os) { foreach (var o in os) { o.GroupRu = ru; o.GroupEn = en; } }

    public static void Init(ConfigFile c)
    {
        ChatPrivate = c.Bind("Chat", "ReplyTarget", "Sender", "Sender - ответ на команду получает только тот, кто её написал. All - ответ видят все.");

        NativeMenu    = c.Bind("Layout", "NativeMenu", true, "Вкладки мода в меню настроек лобби ");
        ReplacePresets = c.Bind("Layout", "ReplacePresets", false, "true - занять кнопку «Пресеты» вместо новой кнопки (если новая не влезает)");
        HostOnlyForce = c.Bind("Experimental", "HostOnlyMode", true, "Режим host-only мод: +25 к версии и IsVersionModded=true (только онлайн). false - обычный клиент.");
        NameViaRpc    = c.Bind("Experimental", "NameViaRpc", true, "Ник другим игрокам - адресным RPC SetName (true) или через данные игрока GameData (false, видно только на собрании).");
        NameRefresh   = c.Bind("Experimental", "NameRefresh", true, "Обновлять ник над головой в раунде (Shapeshift в себя без анимации после смены имени). Если кикает - поставь false.");
        OptEnable     = c.Bind("Optimizer", "Enabled", true, "Встроенный оптимизатор (вместо AmongUsStable)");
        OptFps        = c.Bind("Optimizer", "FpsLimit", 0, "Лимит FPS (0 - без лимита; например 60 или 144)");
        OptNoVsync    = c.Bind("Optimizer", "DisableVsync", true, "Выключить вертикальную синхронизацию");
        OptBackground = c.Bind("Optimizer", "RunInBackground", true, "Игра не замирает в свёрнутом окне (удобно хосту)");
        OptCleanup    = c.Bind("Optimizer", "CleanupInLobby", true, "Очищать неиспользуемые ресурсы при входе в лобби");
        HostBadge     = c.Bind("Layout", "HostBadge", true, "Имя хоста голубым с пометкой «Хост»");
        NativeScroll  = c.Bind("Layout", "NativeScroll", true, "Родная прокрутка списка (false - старая ручная: колесо мыши/стрелки)");
        UseMask       = c.Bind("Layout", "UseMask", false, "Обрезать строки по рамке (если строки пропадают - оставь false)");
        ClipMargin    = c.Bind("Layout", "ClipMargin", 0.25f, "Запас сверху/снизу при скрытии строк, вышедших за рамку");
        RowX          = c.Bind("Layout", "RowXOffset", 0f, "Сдвиг строк по X");
        RowShift      = c.Bind("Layout", "RowButtonsShift", -0.25f, "Сдвиг только строк с кнопками по X (минус - влево), чтобы кнопки не уходили за рамку");
        MapMarkers    = c.Bind("Experimental", "MapMarkersEnabled", true, "Значки на карте у того, кто что-то поставил (силки, мины, руны...). Если кикает или ломает игру - поставь false.");
        NeutralSuicide = c.Bind("Experimental", "NeutralKillsLookLikeSuicide", true, "Убийства нейтралов выглядят как самоубийство жертвы, и убийцу не видно. Если хоста кикает с официального сервера - поставь false.");
        AutoTest = c.Bind("Experimental", "AutoTestInPractice", true, "В режиме практики мод сам проверяет все роли, способности и команды и пишет результат в BepInEx/AshesOfDisaster_selftest.log.");
        RoomShareUrl = c.Bind("Experimental", "RoomShareUrl", "", "Адрес вебхука Discord для кода комнаты, если нет файла AshesOfDisaster.secrets.txt.");
        HorseCostume = c.Bind("Experimental", "HorseCostume", true, "Хобихорсер выглядит как лошадь: картинка из цветных блоков под ником. Если выглядит плохо - поставь false.");
        HorseSize = c.Bind("Experimental", "HorseCostumeSize", 140, "Размер лошади в процентах (меняй, чтобы закрывала персонажа).");
        HorseShift = c.Bind("Experimental", "HorseCostumeShift", 10, "Сдвиг лошади вниз в десятых долях высоты строки (чтобы она оказалась на персонаже).");
        MarkersAll    = c.Bind("Experimental", "MarkersVisibleToAll", true, "Значки на карте видят все игроки (false - только тот, кто поставил).");
        RowY          = c.Bind("Layout", "RowYOffset", 0f, "Сдвиг строк по Y");
        RowSpacing    = c.Bind("Layout", "RowSpacingMult", 1f, "Множитель шага строк");
        ViewHeight    = c.Bind("Layout", "ViewHeight", 3.1f, "Высота видимой области списка настроек (по высоте рамки ноутбука)");
        ScrollStep    = c.Bind("Layout", "ScrollStep", 0.6f, "Шаг прокрутки колесом мыши");

        AnyPlayers       = new("AnyPlayers",       OptTab.System, true,  c);
        AutoStart        = new("AutoStart",        OptTab.System, false, c);
        AutoStartPlayers = new("AutoStartPlayers", OptTab.System, 10, 2, 15, 1, c);
        AutoStartDelay   = new("AutoStartDelay",   OptTab.System, 20, 5, 120, 5, c);
        Welcome          = new("Welcome",          OptTab.System, true,  c);
        PlayerCommands   = new("PlayerCommands",   OptTab.System, true,  c);
        ChatColors       = new("ChatColors",       OptTab.Hidden, true,  c);
        ChatNoLimit      = new("ChatNoLimit",      OptTab.Hidden, true,  c);
        UseBanList       = new("UseBanList",       OptTab.System, true,  c);
        UseDenyNames     = new("UseDenyNames",     OptTab.System, false, c);
        KickLowLevel     = new("KickLowLevel",     OptTab.System, false, c);
        MinLevel         = new("MinLevel",         OptTab.System, 5, 1, 100, 1, c);
        KickNoFriendCode = new("KickNoFriendCode", OptTab.System, false, c);

        NoGameEnd           = new("NoGameEnd",           OptTab.Game, false, c);
        NoEndFewPlayers     = new("NoEndFewPlayers",     OptTab.Game, true, c);
        SyncButtonMode      = new("SyncButtonMode",      OptTab.Game, false, c);
        SyncedButtonCount   = new("SyncedButtonCount",   OptTab.Game, 10, 1, 100, 1, c);
        KillCooldownPercent = new("KillCooldownPercent", OptTab.Game, 100, 25, 200, 5, c);
        DisableCloseDoors   = new("DisableCloseDoors",   OptTab.Game, false, c);
        GhostSeeRoles       = new("GhostSeeRoles",       OptTab.Hidden, true, c);
        NameStatus          = new("NameStatus",          OptTab.Hidden, true, c);
        TeamSwap            = new("TeamSwap",            OptTab.Game, true, c);
        PetForAll           = new("PetForAll",           OptTab.Hidden, true, c);
        RevealOnExile       = new("RevealOnExile",       OptTab.Hidden, true, c);

        ShareRoom = new("ShareRoom", OptTab.System, false, c);
        ShareRoom.NameRu = "Отправить код комнаты в Discord мода (выкл по умолчанию)"; ShareRoom.NameEn = "Send the room code to the mod Discord (off by default)";
        ShareRoom.ShortRu = "Код комнаты в Discord"; ShareRoom.ShortEn = "Room code to Discord";
        Grp("Комната", "Room", ShareRoom);
        Grp("Лобби", "Lobby", AnyPlayers, AutoStart, AutoStartPlayers, AutoStartDelay, Welcome, PlayerCommands, ChatColors, ChatNoLimit);
        Grp("Защита", "Protection", UseBanList, UseDenyNames, KickLowLevel, MinLevel, KickNoFriendCode);
        Grp("Настройки игры", "Game settings", NoGameEnd, NoEndFewPlayers, SyncButtonMode, SyncedButtonCount, KillCooldownPercent, DisableCloseDoors, GhostSeeRoles, NameStatus, RevealOnExile, PetForAll, TeamSwap);

        AddRole(c, "Builder", "Строитель", "Builder", "#C98B3E", OptTab.Crewmate,
            "Мирный. Погладь питомца рядом с дверью: ближайшая дверь закроется на несколько секунд и откроется сама. Это видят и слышат все. [Питомец]",
            "Crewmate. Pet your pet next to a door: the nearest door closes for a few seconds and opens by itself. Everyone sees and hears it. [Pet]");
        AddRole(c, "Psychologist", "Психолог", "Psychologist", "#B388FF", OptTab.Crewmate,
            "Мирный. На собрании напиши /ab ID игрока и узнаешь его состояние: 0-39 - спокоен, 40-59 - видел труп, 60-100 - убийца. Проверок немного, они перезаряжаются (по умолчанию 2 минуты). Иногда проверка не удаётся. [Собрание /ab]",
            "Crewmate. In a meeting type /ab and the player's ID to learn his state: 0-39 calm, 40-59 saw a body, 60-100 killer. Checks are limited and recharge (2 minutes by default). Sometimes a check fails. [Meeting /ab]");
        AddRole(c, "Welder", "Сварщик", "Welder", "#FF8A3D", OptTab.Impostor,
            "Предатель. Питомец у двери запирает комнату, и она остаётся закрытой до собрания. Одновременно можно держать запертыми несколько комнат (по умолчанию 1). Столовую запереть нельзя. [Питомец]",
            "Impostor. The pet next to a door locks the room and it stays closed until the meeting. Only a few rooms can be locked at once (1 by default). The cafeteria cannot be locked. [Pet]");
        AddRole(c, "Foreman", "Прораб", "Foreman", "#FFB74D", OptTab.Crewmate,
            "Мирный. Когда ты выполняешь задание, с некоторым шансом одно задание засчитывается и случайному члену экипажа. Шанс 35%. [Сама]",
            "Crewmate. When you finish a task there is a chance that one task of a random crewmate is completed too. Chance: 35%. [Passive]");
        AddRole(c, "Plumber", "Сантехник", "Plumber", "#5C6BC0", OptTab.Crewmate,
            "Мирный. Питомец чинит текущий саботаж (свет, реактор, кислород, связь). Если саботажа нет, открывает все закрытые двери, в том числе запертые Сварщиком. Починок: 4, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet fixes the current sabotage (lights, reactor, oxygen, comms). If there is none, it opens all closed doors, including doors locked by the Welder. Repairs: 4, cooldown 40 s. [Pet]");
        AddRole(c, "Dowser", "Лозоходец", "Dowser", "#8D6E63", OptTab.Crewmate,
            "Мирный. Питомец показывает комнату и расстояние до ближайшего трупа, который тебе ещё не показывали, и сколько живых игроков рядом. Радиус поиска 8 м, перезарядка 30 с. [Питомец]",
            "Crewmate. The pet shows the room and distance of the nearest body you have not been shown yet and how many living players are nearby. Search radius 8 m, cooldown 30 s. [Pet]");
        AddRole(c, "Navigator", "Штурман", "Navigator", "#4FC3F7", OptTab.Crewmate,
            "Мирный. Питомец ставит метку. Погладь питомца ещё раз, и ты вернёшься к метке. На собрании метка пропадает. [Питомец]",
            "Crewmate. The pet sets a mark. Pet it again and you return to the mark. The mark vanishes at a meeting. [Pet]");
        AddRole(c, "Twin", "Близнец", "Twin", "#E040FB", OptTab.Impostor,
            "Предатель. Когда ты убиваешь, вместе с жертвой умирает ближайший к ней игрок (радиус в настройках). Двойное убийство срабатывает не чаще раза в перезарядку (по умолчанию 60 с). Если у второго игрока есть щит, он его спасёт. [Сама]",
            "Impostor. When you kill, the player nearest to the victim dies too (radius in the settings). The double kill works once per cooldown at most (60 s by default). If the second player has a shield it saves him. [Passive]");
        AddRole(c, "Shroomer", "Грибник", "Shroomer", "#AB47BC", OptTab.Impostor,
            "Предатель. Питомец запускает грибной саботаж на любой карте. На Фангле он настоящий, а на остальных картах все живые игроки на несколько секунд становятся серыми, и их ники перемешиваются. Запусков: 3, перезарядка 60 с. [Питомец]",
            "Impostor. The pet starts the mushroom sabotage on any map. It is the real one on Fungle, and on other maps all living players turn gray for a few seconds and their names are shuffled. Starts: 3, cooldown 60 s. [Pet]");
        AddRole(c, "Hound", "Ищейка", "Hound", "#FF7043", OptTab.Impostor,
            "Предатель. Питомец показывает комнату и расстояние до ближайшего члена экипажа. Перезарядка 25 с. [Питомец]",
            "Impostor. The pet shows the room and distance to the nearest crewmate. Cooldown: 25 s. [Pet]");
        AddRole(c, "Smoker", "Курильщик", "Smoker", "#BDBDBD", OptTab.Neutral,
            "Нейтрал. Питомец: выкури сигарету в любом месте, там останется облако дыма. Кто просидит в дыму подряд нужное время (по умолчанию 10 с), умрёт. Игрок в дыму получает предупреждение. Буст: в своём дыму тебя нельзя убить, а после убийства дымом сигарета готова сразу. Для сервера ты настоящий предатель. [Питомец]",
            "Neutral. Pet: smoke a cigarette anywhere and a smoke cloud stays there. Whoever stays in the smoke for the set time in a row (10 s by default) dies. The player in the smoke gets a warning. Boost: you cannot be killed inside your own smoke and after a smoke kill your cigarette is ready at once. The server sees you as a real impostor. [Pet]");
        AddRole(c, "Pinkman", "Пинкман", "Pinkman", "#FF6FB5", OptTab.Crewmate,
            "Мирный, по мотивам «Во все тяжкие». Питомец вдали от людей варит партию (по умолчанию 30 с). Питомец рядом с игроком продаёт ему товар. Чистый товар (шанс в настройках) даёт покупателю щит, а брак мигает ему экраном. Каждые две удачные продажи дают щит тебе. Yeah, science! [Питомец]",
            "Crewmate, inspired by Breaking Bad. The pet away from people cooks a batch (30 s by default). The pet next to a player sells him the product. Pure product (chance in the settings) gives the buyer a shield, a bad batch flashes his screen. Every two good sales give you a shield. Yeah, science! [Pet]");
        AddRole(c, "Prankster", "Шутник", "Prankster", "#F06292", OptTab.Crewmate,
            "Мирный. Питомец меняет тебя местами с ближайшим игроком (до 3,5 м). [Питомец]",
            "Crewmate. The pet swaps you with the nearest player (within 3.5 m). [Pet]");
        AddRole(c, "Crow", "Ворон", "Crow", "#455A64", OptTab.Crewmate,
            "Мирный. Питомец переносит тебя к последнему трупу, о котором ещё не доложили. Перелётов: 3, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet teleports you to the last body that has not been reported. Flights: 3, cooldown 40 s. [Pet]");
        AddRole(c, "Defector", "Перебежчик", "Defector", "#A1887F", OptTab.Crewmate,
            "Мирный. На первом собрании ты узнаёшь имя игрока, который якобы предатель. С некоторым шансом (настройка) информация ложная. [Сама]",
            "Crewmate. At the first meeting you learn the name of a player who is supposedly an impostor. With some chance (setting) the information is false. [Passive]");
        AddRole(c, "Reflector", "Отражатель", "Reflector", "#B0BEC5", OptTab.Neutral,
            "Нейтрал. Первое нападение на тебя с некоторым шансом (настройка) отражается: убийца погибает сам. Срабатывает один раз за игру. [Сама]",
            "Neutral. The first attack on you is reflected with some chance (setting): the killer dies instead. Works once per game. [Passive]");
        AddRole(c, "Cloud", "Облако", "Cloud", "#9FD8FF", OptTab.Neutral,
            "Нейтрал. Питомец оставляет на месте невидимое облако (лимит в настройках). Когда на него наступает другой игрок, облако исчезает, а тебе приходит сигнал паники: экран мигает красным (на собрании придёт сообщение). С небольшим шансом (настройка) облако оказывается грозой: тот, кто наступил, погибает. Ты узнаёшь, какое облако - гроза. На собрании облака пропадают. [Питомец]",
            "Neutral. The pet leaves an invisible cloud on the spot (limit in the settings). When another player steps on it the cloud vanishes and you get a panic signal: your screen flashes red (a message arrives at the meeting). With a small chance (setting) the cloud is a thunderstorm: whoever stepped on it dies. You are told which cloud is a storm. Clouds vanish at a meeting. [Pet]");
        CloudMax            = Extra("Cloud", new IntOpt("CloudMax", OptTab.Neutral, 3, 1, 10, 1, c), "Облаков одновременно", "Clouds at a time");
        CloudRadius         = Extra("Cloud", new IntOpt("CloudRadius", OptTab.Neutral, 7, 3, 20, 1, c), "Радиус облака (в десятых долях)", "Cloud radius (tenths)");
        CloudStormChance    = Extra("Cloud", new IntOpt("CloudStormChance", OptTab.Neutral, 10, 0, 100, 5, c), "Шанс, что облако окажется грозой (%)", "Chance that a cloud is a thunderstorm (%)");
        CloudFlash          = Extra("Cloud", new IntOpt("CloudFlash", OptTab.Neutral, 3, 1, 10, 1, c), "Сколько секунд мигает экран", "Panic flash duration (sec)");
        CloudStormMarker    = Extra("Cloud", new BoolOpt("CloudStormMarker", OptTab.Neutral, true, c), "Метка грозы в статусе владельца (у комнаты с грозой)", "Storm mark in the owner status (next to the storm room)");
        PsyCooldown         = Extra("Psychologist", new IntOpt("PsyCooldown", OptTab.Crewmate, 120, 10, 300, 10, c), "Перезарядка проверки (сек)", "Check cooldown (sec)");
        PsyMaxUses          = Extra("Psychologist", new IntOpt("PsyMaxUses", OptTab.Crewmate, 3, 1, 15, 1, c), "Сколько раз можно проверять", "Checks per game");
        PsyFailChance       = Extra("Psychologist", new IntOpt("PsyFailChance", OptTab.Crewmate, 25, 0, 100, 5, c), "Шанс, что проверка не удастся (%)", "Chance a check fails (%)");
        WelderMax           = Extra("Welder", new IntOpt("WelderMax", OptTab.Impostor, 1, 1, 6, 1, c), "Комнат запертыми одновременно", "Rooms locked at a time");
        WelderCooldown      = Extra("Welder", new IntOpt("WelderCooldown", OptTab.Impostor, 25, 5, 120, 5, c), "Перезарядка сварки (сек)", "Welding cooldown (sec)");
        DowserCooldown      = Extra("Dowser", new IntOpt("DowserCooldown", OptTab.Crewmate, 30, 5, 120, 5, c), "Перезарядка поиска (сек)", "Search cooldown (sec)");
        DowserRadius        = Extra("Dowser", new IntOpt("DowserRadius", OptTab.Crewmate, 8, 3, 20, 1, c), "Радиус подсчёта игроков (м)", "Player count radius (m)");
        PinkCook            = Extra("Pinkman", new IntOpt("PinkCook", OptTab.Neutral, 30, 5, 120, 5, c), "Время варки партии (сек)", "Cooking time (sec)");
        PinkMax             = Extra("Pinkman", new IntOpt("PinkMax", OptTab.Neutral, 3, 1, 8, 1, c), "Партий в запасе (максимум)", "Batches in stock (max)");
        PinkPurity          = Extra("Pinkman", new IntOpt("PinkPurity", OptTab.Neutral, 70, 0, 100, 5, c), "Шанс чистого товара (%)", "Pure product chance (%)");
        PinkSaleCooldown    = Extra("Pinkman", new IntOpt("PinkSaleCooldown", OptTab.Neutral, 20, 5, 120, 5, c), "Перезарядка продажи (сек)", "Sale cooldown (sec)");
        SmokeMax            = Extra("Smoker", new IntOpt("SmokeMax", OptTab.Neutral, 2, 1, 8, 1, c), "Дымов одновременно", "Smoke clouds at a time");
        SmokeLife           = Extra("Smoker", new IntOpt("SmokeLife", OptTab.Neutral, 30, 10, 120, 5, c), "Сколько секунд держится дым", "Smoke duration (sec)");
        SmokeKill           = Extra("Smoker", new IntOpt("SmokeKill", OptTab.Neutral, 10, 3, 30, 1, c), "Через сколько секунд в дыму умираешь", "Seconds in smoke until death");
        SmokeRadius         = Extra("Smoker", new IntOpt("SmokeRadius", OptTab.Neutral, 15, 5, 40, 1, c), "Радиус дыма (в десятых долях)", "Smoke radius (tenths)");
        SmokeCooldown       = Extra("Smoker", new IntOpt("SmokeCooldown", OptTab.Neutral, 20, 5, 120, 5, c), "Перезарядка сигареты (сек)", "Cigarette cooldown (sec)");
        ShroomUses          = Extra("Shroomer", new IntOpt("ShroomUses", OptTab.Impostor, 3, 1, 10, 1, c), "Запусков за игру", "Starts per game");
        ShroomCooldown      = Extra("Shroomer", new IntOpt("ShroomCooldown", OptTab.Impostor, 60, 10, 240, 10, c), "Перезарядка грибов (сек)", "Mushroom cooldown (sec)");
        ShroomSeconds       = Extra("Shroomer", new IntOpt("ShroomSeconds", OptTab.Impostor, 10, 3, 30, 1, c), "Длительность на других картах (сек)", "Duration on other maps (sec)");
        PlumberUses         = Extra("Plumber", new IntOpt("PlumberUses", OptTab.Crewmate, 4, 1, 15, 1, c), "Починок за игру", "Repairs per game");
        PlumberCooldown     = Extra("Plumber", new IntOpt("PlumberCooldown", OptTab.Crewmate, 40, 5, 180, 5, c), "Перезарядка починки (сек)", "Repair cooldown (sec)");
        TwinRadius          = Extra("Twin", new IntOpt("TwinRadius", OptTab.Impostor, 25, 10, 60, 5, c), "Радиус второго удара (в десятых долях)", "Second strike radius (tenths)");
        TwinCooldown        = Extra("Twin", new IntOpt("TwinCooldown", OptTab.Impostor, 60, 10, 240, 10, c), "Перезарядка двойного убийства (сек)", "Double kill cooldown (sec)");
        ForemanChance       = Extra("Foreman", new IntOpt("ForemanHelp", OptTab.Crewmate, 35, 5, 100, 5, c), "Шанс засчитать чужое задание (%)", "Chance to complete a crewmate's task (%)");
        NavCooldown         = Extra("Navigator", new IntOpt("NavCooldown", OptTab.Crewmate, 30, 5, 120, 5, c), "Перезарядка возврата (сек)", "Return cooldown (sec)");
        HoundCooldown       = Extra("Hound", new IntOpt("HoundCooldown", OptTab.Impostor, 25, 5, 120, 5, c), "Перезарядка поиска (сек)", "Search cooldown (sec)");
        PrankCooldown       = Extra("Prankster", new IntOpt("PrankCooldown", OptTab.Neutral, 45, 10, 180, 5, c), "Перезарядка обмена (сек)", "Swap cooldown (sec)");
        CrowCooldown        = Extra("Crow", new IntOpt("CrowCooldown", OptTab.Neutral, 40, 10, 180, 5, c), "Перезарядка перелёта (сек)", "Flight cooldown (sec)");
        CrowUses            = Extra("Crow", new IntOpt("CrowUses", OptTab.Neutral, 3, 1, 10, 1, c), "Перелётов за игру", "Flights per game");
        DefectorWrong       = Extra("Defector", new IntOpt("DefectorWrong", OptTab.Neutral, 40, 0, 100, 5, c), "Шанс ложной информации (%)", "Chance the info is false (%)");
        ReflectChance       = Extra("Reflector", new IntOpt("ReflectChance", OptTab.Neutral, 50, 0, 100, 5, c), "Шанс отражения (%)", "Reflect chance (%)");
        BuilderCooldown     = Extra("Builder", new IntOpt("BuilderCooldown", OptTab.Crewmate, 25, 5, 90, 5, c), "Перезарядка закрытия двери (сек)", "Door closing cooldown (sec)");

        Roles30.Define(c);
        Roles75.Define(c);
        Joker.Define(c);
        Alchemist.Define(c);
        MysticRoles.Define(c);
        Witch.Define(c); Extra33.Define(c); Freeman.Define(c); God.Define(c);
        Fat.Define(c);
        Anticheat.Define(c);
        Extras.Define(c);
        LobbyGames.Define(c); Ghosts.Define(c);

        ShowCredits = new("ShowCredits", OptTab.Other, true, c);

        try
        {
            var mig = c.Bind("Internal", "Migrated47", false);
            if (!mig.Value)
            {
                mig.Value = true;
                NameStatus.Value = true;
                CloudStormMarker.Value = true;
            }
            var mig2 = c.Bind("Internal", "Migrated48", false);
            if (!mig2.Value)
            {
                mig2.Value = true;
                foreach (var b in new[] { NameStatus, PetForAll, RevealOnExile, GhostSeeRoles, ChatColors, ChatNoLimit }) b.Value = true;
                HostOnlyForce.Value = true; NameViaRpc.Value = true; NameRefresh.Value = true;
            }
        }
        catch { }
    }
}
