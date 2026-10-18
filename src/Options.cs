using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace AshesOfDisaster;

public enum OptTab { System, Game, Crewmate, Impostor, Neutral, Other }

public abstract class Opt
{
    public static readonly List<Opt> All = new();
    public string Id;
    public OptTab Tab;
    protected Opt(string id, OptTab tab) { Id = id; Tab = tab; All.Add(this); }
    public string NameRu, NameEn;   // для настроек, созданных кодом (роли)
    public string ShortRu, ShortEn; // короткое имя для строки под заголовком роли
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
    public List<Opt> Extras = new();   // настройки именно этой роли
}

// Добавить новую настройку = одна строка здесь + строки перевода "opt.<id>" в Translator.cs
// Добавить роль = одна строка в Opts.Init (AddRole)
public static class Opts
{
    public static ConfigEntry<string> ChatPrivate;
    public static ConfigEntry<bool> NativeMenu, ReplacePresets, UseMask, NativeScroll, HostBadge, HostOnlyForce, NameRefresh, NameViaRpc, OptEnable, OptNoVsync, OptBackground, OptCleanup;
    public static ConfigEntry<int> OptFps;
    public static ConfigEntry<float> ViewHeight, ClipMargin, ScrollStep;
    // Раскладка родного меню (можно подгонять в конфиге без пересборки)
    public static ConfigEntry<float> RowX, RowY, RowSpacing;
    public static readonly List<RoleDef> Roles = new();

    // Системные (лобби + защита)
    public static BoolOpt AnyPlayers, AutoStart, Welcome, PlayerCommands;
    public static IntOpt AutoStartPlayers, AutoStartDelay;
    public static BoolOpt UseBanList, UseDenyNames, KickLowLevel, KickNoFriendCode;
    public static IntOpt MinLevel;
    // Игра
    public static BoolOpt NoEndFewPlayers, NoGameEnd, SyncButtonMode, DisableCloseDoors, GhostSeeRoles, NameStatus, RevealOnExile, PetForAll, TeamSwap;
    public static IntOpt SyncedButtonCount, KillCooldownPercent;
    // Настройки ролей
    public static IntOpt DyerDuration, BuilderCooldown, CloudMax, CloudRadius, CloudFlash, CloudStormChance, PeasantTasks, SpiderMaxWebs, VigilanteMaxPenalty, VigilanteUnlockTime, VigilanteCooldown, PsyCooldown, PsyMaxUses, PsyFailChance, WelderMax, WelderCooldown, DowserCooldown, DowserRadius, PinkCook, PinkMax, PinkPurity, PinkSaleCooldown, SmokeMax, SmokeLife, SmokeKill, SmokeRadius, SmokeCooldown, ShroomUses, ShroomCooldown, ShroomSeconds, PlumberUses, PlumberCooldown, TwinRadius, TwinCooldown, LogCooldown, LogLines, LogAnon, ForemanChance, NavCooldown, HoundCooldown, PrankCooldown, CrowCooldown, CrowUses, DefectorWrong, ReflectChance;
    public static BoolOpt SpiderPassThrough, CloudStormMarker, HostAuthority;
    // Другое
    public static BoolOpt ShowCredits;

    internal static void AddRole(ConfigFile c, string id, string ru, string en, string color, OptTab tab, string descRu, string descEn)
    {
        var r = new RoleDef { Id = id, Ru = ru, En = en, Color = color, Tab = tab, DescRu = descRu, DescEn = descEn };
        r.Chance = new IntOpt(id + "Chance", tab, 0, 0, 100, 10, c);
        r.Count = new IntOpt(id + "Count", tab, 1, 1, 15, 1, c);
        r.Chance.ShortRu = "Шанс появления (%)"; r.Chance.ShortEn = "Spawn chance (%)";
        r.Count.ShortRu = "Количество"; r.Count.ShortEn = "Count";
        r.Chance.NameRu = $"<color={color}>{ru}</color>: шанс (%)";
        r.Chance.NameEn = $"<color={color}>{en}</color>: chance (%)";
        r.Count.NameRu = $"<color={color}>{ru}</color>: количество";
        r.Count.NameEn = $"<color={color}>{en}</color>: count";
        r.Chance.GroupRu = r.Count.GroupRu = $"<color={color}>{ru}</color>";
        r.Chance.GroupEn = r.Count.GroupEn = $"<color={color}>{en}</color>";
        Roles.Add(r);
    }

    internal static T Extra<T>(string id, T o, string ru, string en) where T : Opt
    {
        var r = Roles.Find(x => x.Id == id);
        o.ShortRu = ru; o.ShortEn = en;
        o.NameRu = $"{r.Colored(true)}: {ru}";
        o.NameEn = $"{r.Colored(false)}: {en}";
        r.Extras.Add(o);
        return o;
    }

    static void Grp(string ru, string en, params Opt[] os) { foreach (var o in os) { o.GroupRu = ru; o.GroupEn = en; } }

    public static void Init(ConfigFile c)
    {
        ChatPrivate = c.Bind("Chat", "ReplyTarget", "Sender", "Sender — ответ на команду получает только тот, кто её написал. All — ответ видят все.");

        NativeMenu    = c.Bind("Layout", "NativeMenu", true, "Вкладки мода в меню настроек лобби ");
        ReplacePresets = c.Bind("Layout", "ReplacePresets", false, "true — занять кнопку «Пресеты» вместо новой кнопки (если новая не влезает)");
        HostOnlyForce = c.Bind("Experimental", "HostOnlyMode", true, "Режим host-only мод: +25 к версии и IsVersionModded=true (только онлайн). false — обычный клиент.");
        NameViaRpc    = c.Bind("Experimental", "NameViaRpc", true, "Ник другим игрокам — адресным RPC SetName (true) или через данные игрока GameData (false, видно только на собрании).");
        NameRefresh   = c.Bind("Experimental", "NameRefresh", true, "Обновлять ник над головой в раунде (Shapeshift в себя без анимации после смены имени). Если кикает — поставь false.");
        OptEnable     = c.Bind("Optimizer", "Enabled", true, "Встроенный оптимизатор (вместо AmongUsStable)");
        OptFps        = c.Bind("Optimizer", "FpsLimit", 0, "Лимит FPS (0 — без лимита; например 60 или 144)");
        OptNoVsync    = c.Bind("Optimizer", "DisableVsync", true, "Выключить вертикальную синхронизацию");
        OptBackground = c.Bind("Optimizer", "RunInBackground", true, "Игра не замирает в свёрнутом окне (удобно хосту)");
        OptCleanup    = c.Bind("Optimizer", "CleanupInLobby", true, "Очищать неиспользуемые ресурсы при входе в лобби");
        HostBadge     = c.Bind("Layout", "HostBadge", true, "Имя хоста голубым с пометкой «Хост»");
        NativeScroll  = c.Bind("Layout", "NativeScroll", true, "Родная прокрутка списка (false — старая ручная: колесо мыши/стрелки)");
        UseMask       = c.Bind("Layout", "UseMask", false, "Обрезать строки по рамке (если строки пропадают — оставь false)");
        ClipMargin    = c.Bind("Layout", "ClipMargin", 0.25f, "Запас сверху/снизу при скрытии строк, вышедших за рамку");
        RowX          = c.Bind("Layout", "RowXOffset", 0f, "Сдвиг строк по X");
        RowY          = c.Bind("Layout", "RowYOffset", 0f, "Сдвиг строк по Y");
        RowSpacing    = c.Bind("Layout", "RowSpacingMult", 1f, "Множитель шага строк");
        ViewHeight    = c.Bind("Layout", "ViewHeight", 3.1f, "Высота видимой области списка настроек (по высоте рамки ноутбука)");
        ScrollStep    = c.Bind("Layout", "ScrollStep", 0.6f, "Шаг прокрутки колесом мыши");

        // --- Системные ---
        AnyPlayers       = new("AnyPlayers",       OptTab.System, true,  c);
        AutoStart        = new("AutoStart",        OptTab.System, false, c);
        AutoStartPlayers = new("AutoStartPlayers", OptTab.System, 10, 2, 15, 1, c);
        AutoStartDelay   = new("AutoStartDelay",   OptTab.System, 20, 5, 120, 5, c);
        Welcome          = new("Welcome",          OptTab.System, true,  c);
        PlayerCommands   = new("PlayerCommands",   OptTab.System, true,  c);
        UseBanList       = new("UseBanList",       OptTab.System, true,  c);
        UseDenyNames     = new("UseDenyNames",     OptTab.System, false, c);
        KickLowLevel     = new("KickLowLevel",     OptTab.System, false, c);
        MinLevel         = new("MinLevel",         OptTab.System, 5, 1, 100, 1, c);
        KickNoFriendCode = new("KickNoFriendCode", OptTab.System, false, c);

        // --- Игра ---
        NoGameEnd           = new("NoGameEnd",           OptTab.Game, false, c);
        NoEndFewPlayers     = new("NoEndFewPlayers",     OptTab.Game, true, c);
        SyncButtonMode      = new("SyncButtonMode",      OptTab.Game, false, c);
        SyncedButtonCount   = new("SyncedButtonCount",   OptTab.Game, 10, 1, 100, 1, c);
        KillCooldownPercent = new("KillCooldownPercent", OptTab.Game, 100, 25, 200, 5, c);
        DisableCloseDoors   = new("DisableCloseDoors",   OptTab.Game, false, c);
        GhostSeeRoles       = new("GhostSeeRoles",       OptTab.Game, true, c);
        NameStatus          = new("NameStatus",          OptTab.Game, true, c);
        TeamSwap            = new("TeamSwap",            OptTab.Game, true, c);
        PetForAll           = new("PetForAll",           OptTab.Game, true, c);
        RevealOnExile       = new("RevealOnExile",       OptTab.Game, true, c);

        Grp("Лобби", "Lobby", AnyPlayers, AutoStart, AutoStartPlayers, AutoStartDelay, Welcome, PlayerCommands);
        Grp("Защита", "Protection", UseBanList, UseDenyNames, KickLowLevel, MinLevel, KickNoFriendCode);
        Grp("Настройки игры", "Game settings", NoGameEnd, NoEndFewPlayers, SyncButtonMode, SyncedButtonCount, KillCooldownPercent, DisableCloseDoors, GhostSeeRoles, NameStatus, RevealOnExile, PetForAll, TeamSwap);

        // --- Роли (пока только настройки; логика ролей подключается отдельно) ---
        AddRole(c, "Peasant", "Крестьянин", "Peasant", "#7CFC00", OptTab.Crewmate,
            "Живой член экипажа. Один раз за игру может поставить себе защиту. Защита включается сама после нескольких выполненных заданий и отменяет первое нападение на него.",
            "A living crewmate. The shield turns on by itself after a few completed tasks and cancels the first attack on him.");
        AddRole(c, "Builder", "Строитель", "Builder", "#C98B3E", OptTab.Crewmate,
            "Член экипажа. Погладь питомца рядом с дверью — ближайшая дверь закрывается (как при саботаже двери), через несколько секунд она откроется сама. Дверь видят и слышат все игроки. Нужен питомец!",
            "A crewmate. Pet your pet next to a door - the nearest door closes (like a door sabotage) and opens by itself after a few seconds. Everyone sees and hears the door. A pet is required!");
        AddRole(c, "Psychologist", "Психолог", "Psychologist", "#B388FF", OptTab.Crewmate,
            "Член экипажа. На собрании командой /ab ID проверяет психическое состояние игрока: 0-39 — спокоен, 40-59 — видел труп, 60-100 — убийца. Способность перезаряжается (по умолчанию 2 минуты), число проверок ограничено, а иногда проверка не удаётся.",
            "A crewmate. In a meeting use /ab ID to check a player's mental state: 0-39 calm, 40-59 saw a body, 60-100 killer. The ability recharges (2 minutes by default), the number of checks is limited, and sometimes a check fails.");
        AddRole(c, "Welder", "Сварщик", "Welder", "#FF8A3D", OptTab.Impostor,
            "Предатель. Погладь питомца у двери — комната запирается и остаётся закрытой до собрания. Одновременно можно держать запертыми ограниченное число комнат (по умолчанию 1), столовую запереть нельзя. Нужен питомец!",
            "Impostor. Pet your pet next to a door - the room is locked and stays closed until the meeting. Only a limited number of rooms can be locked at once (1 by default), the cafeteria can't be locked. A pet is required!");
        AddRole(c, "Foreman", "Прораб", "Foreman", "#FFB74D", OptTab.Crewmate,
            "Член экипажа. Каждое выполненное им задание с настраиваемым шансом засчитывает одно задание случайному члену экипажа — команде проще победить по заданиям.",
            "A crewmate. Each task he completes has a configurable chance to also complete one task of a random crewmate, helping the team win by tasks.");
        AddRole(c, "Logger", "Логер", "Logger", "#26C6DA", OptTab.Crewmate,
            "Член экипажа. Читает журнал входов в комнаты на карте: питомец показывает последние записи («через сколько секунд кто вошёл в какую комнату»), а на каждом собрании приходят свежие. Часть записей приходит без имени (шанс настраивается). Нужен питомец!",
            "A crewmate. Reads the room-entry log of the map: pet shows the latest entries (how many seconds ago who entered which room) and fresh ones arrive at every meeting. Some entries come without a name (chance configurable). A pet is required!");
        AddRole(c, "Plumber", "Сантехник", "Plumber", "#5C6BC0", OptTab.Crewmate,
            "Член экипажа. Питомец чинит активный саботаж (свет, реактор, кислород, связь), а если саботажа нет — открывает все закрытые двери (в том числе запертые Сварщиком). Число починок и перезарядка настраиваются. Нужен питомец!",
            "A crewmate. Pet fixes an active sabotage (lights, reactor, oxygen, comms), or opens all closed doors if there is none (including doors locked by the Welder). Number of repairs and cooldown are configurable. A pet is required!");
        AddRole(c, "Dowser", "Лозоходец", "Dowser", "#8D6E63", OptTab.Crewmate,
            "Член экипажа. Питомец показывает комнату и расстояние до ближайшего непоказанного трупа и сколько живых игроков рядом. Перезарядка и радиус поиска настраиваются. Нужен питомец!",
            "A crewmate. Pet shows the room and distance of the nearest body and how many living players are nearby. Cooldown and search radius are configurable. A pet is required!");
        AddRole(c, "Navigator", "Штурман", "Navigator", "#4FC3F7", OptTab.Crewmate,
            "Член экипажа. Питомец ставит метку, повторное поглаживание возвращает тебя к метке (перезарядка настраивается). Метка пропадает на собрании. Нужен питомец!",
            "A crewmate. Pet sets a mark, petting again returns you to it (cooldown configurable). The mark vanishes at a meeting. A pet is required!");
        AddRole(c, "Twin", "Близнец", "Twin", "#E040FB", OptTab.Impostor,
            "Предатель. Когда он убивает, вторым умирает ближайший к жертве игрок (в настраиваемом радиусе). Двойное убийство срабатывает не чаще, чем раз в перезарядку (по умолчанию 60 с); если у второго есть защита, она спасает его.",
            "Impostor. When he kills, the player nearest to the victim (within a configurable radius) dies too. The double kill triggers no more often than the cooldown (60 s by default); if the second player has a shield, it saves him.");
        AddRole(c, "Shroomer", "Грибник", "Shroomer", "#AB47BC", OptTab.Impostor,
            "Предатель. Питомец запускает грибной саботаж на любой карте: на Фангле — настоящий, на остальных картах все живые игроки становятся серыми, а ники перемешиваются на несколько секунд. Число запусков и перезарядка настраиваются. Нужен питомец!",
            "Impostor. Pet starts the mushroom sabotage on any map: the real one on Fungle, and on other maps all living players turn gray and their names are shuffled for a few seconds. Number of starts and cooldown are configurable. A pet is required!");
        AddRole(c, "Hound", "Ищейка", "Hound", "#FF7043", OptTab.Impostor,
            "Предатель. Питомец показывает комнату и расстояние до ближайшего члена экипажа (перезарядка настраивается). Нужен питомец!",
            "Impostor. Pet shows the room and distance to the nearest crewmate (cooldown configurable). A pet is required!");
        AddRole(c, "Smoker", "Курильщик", "Smoker", "#BDBDBD", OptTab.Neutral,
            "Нейтрал. Питомец — выкурить сигарету в любом месте: там остаётся облако дыма. Кто просидит в дыму нужное время подряд (по умолчанию 10 с, настраивается) — умрёт; предупреждение приходит тому, кто в дыму. Буст: в своём дыму курильщика нельзя убить, а после убийства дымом сигарета готова сразу. Для сервера он настоящий предатель. Нужен питомец!",
            "Neutral. Pet — smoke a cigarette anywhere, leaving a smoke cloud. Whoever stays in the smoke for the set time in a row (10 s by default, configurable) dies; the player inside gets a warning. Boost: the smoker can't be killed inside his own smoke, and after a smoke kill his cigarette is ready at once. A real impostor for the server. A pet is required!");
        AddRole(c, "Pinkman", "Пинкман", "Pinkman", "#FF6FB5", OptTab.Neutral,
            "Нейтрал, по мотивам «Во все тяжкие». Питомец вдали от людей — варить партию (по умолчанию 30 с), рядом с игроком — продать ему товар. Чистый товар (шанс настраивается) даёт покупателю щит, брак — мигание экрана. Каждые две удачные продажи дают щит самому Пинкману. Нужен питомец! Yeah, science!",
            "Neutral, inspired by Breaking Bad. Pet away from people — cook a batch (30 s by default), next to a player — sell to him. Pure product (chance configurable) gives the buyer a shield, a bad batch flashes his screen. Every two good sales give Pinkman himself a shield. A pet is required! Yeah, science!");
        AddRole(c, "Prankster", "Шутник", "Prankster", "#F06292", OptTab.Neutral,
            "Нейтрал. Питомец меняет тебя местами с ближайшим игроком (до 3,5 м). Нужен питомец!",
            "Neutral. Pet swaps you with the nearest player (within 3.5 m). A pet is required!");
        AddRole(c, "Crow", "Ворон", "Crow", "#455A64", OptTab.Neutral,
            "Нейтрал. Питомец переносит тебя к последнему трупу, пока он не доложен. Число перелётов и перезарядка настраиваются. Нужен питомец!",
            "Neutral. Pet teleports you to the last unreported body. Number of flights and cooldown are configurable. A pet is required!");
        AddRole(c, "Defector", "Перебежчик", "Defector", "#A1887F", OptTab.Neutral,
            "Нейтрал. На первом собрании узнаёт имя одного из игроков как предателя, но с настраиваемым шансом информация ложная.",
            "Neutral. At the first meeting learns a player's name as an impostor, but with a configurable chance the info is false.");
        AddRole(c, "Reflector", "Отражатель", "Reflector", "#B0BEC5", OptTab.Neutral,
            "Нейтрал. Первое нападение на него с настраиваемым шансом отражается: убийца гибнет сам. Работает один раз за игру.",
            "Neutral. The first attack on him is reflected with a configurable chance: the killer dies instead. Works once per game.");
        AddRole(c, "Dyer", "Маляр", "Painter", "#FF4D4D", OptTab.Impostor,
            "Предатель. Трупы его жертв закрашены: доложить о них нельзя, пока краска не стечёт (время настраивается).",
            "Impostor. Bodies of his victims are painted over: they can't be reported until the paint runs off (configurable).");
        AddRole(c, "Spider", "Паук", "Spider", "#FF4D4D", OptTab.Impostor,
            "Предатель. Погладь питомца у дверного проёма — там появится паутина. Член экипажа, наступивший на неё, застревает, а паук узнаёт об этом на собрании. Если убить застрявшего, паук съедает его целиком: тело нельзя доложить. Экипаж освобождает застрявшего, постояв рядом несколько секунд. Предатели и нейтралы проходят сквозь паутину (настройка). Нужен питомец!",
            "Impostor. Pet your pet next to a doorway to place a web. A crewmate who steps on it gets stuck; the spider learns about it at the meeting. Killing the stuck player eats them whole: the body can't be reported. Crew frees the victim by standing nearby for a few seconds. Impostors and neutrals pass through webs (setting). A pet is required!");
        AddRole(c, "Cloud", "Облако", "Cloud", "#9FD8FF", OptTab.Neutral,
            "Нейтрал. Погладь питомца — на этом месте останется невидимое облако (лимит настраивается). Когда кто-то другой наступит на него, облако исчезнет, а тебе придёт сигнал паники: экран мигает красным (на собрании придёт сообщение). С небольшим шансом (настраивается) облако оказывается грозой: наступивший в её зону погибнет. Владелец узнаёт, какое облако — гроза. Облака пропадают на собрании. Нужен питомец!",
            "Neutral. Pet your pet - an invisible cloud stays at that spot (limit is configurable). When someone else steps on it the cloud vanishes and you get a panic signal: your screen flashes red (a message arrives at the meeting). With a small (configurable) chance a cloud turns out to be a thunderstorm: whoever steps into its zone dies. The owner is told which cloud is a storm. Clouds vanish at a meeting. A pet is required!");
        AddRole(c, "Vigilante", "Виджилант", "Vigilante", "#FFB000", OptTab.Neutral,
            "Нейтрал. Для сервера — настоящий предатель: видит других предателей, задания не нужны. Убийство открывается по таймеру: погладь питомца рядом с целью. Убийство предателя штрафа не даёт, убийство невиновного даёт штраф (0/2); если штрафов больше допустимого, убийство блокируется. Нужен питомец!",
            "Neutral. A real impostor for the server: sees other impostors, no tasks. The kill unlocks on a timer: pet your pet next to a target. Killing an impostor gives no penalty, killing an innocent gives a penalty (0/2); too many penalties lock the kill. A pet is required!");

        // Настройки конкретных ролей
        DyerDuration        = Extra("Dyer",   new IntOpt("DyerDuration", OptTab.Impostor, 45, 10, 120, 5, c), "Время краски на трупе (сек)", "Paint duration on body (sec)");
        CloudMax            = Extra("Cloud", new IntOpt("CloudMax", OptTab.Neutral, 3, 1, 10, 1, c), "Облаков одновременно", "Clouds at a time");
        CloudRadius         = Extra("Cloud", new IntOpt("CloudRadius", OptTab.Neutral, 7, 3, 20, 1, c), "Радиус облака (в десятых долях)", "Cloud radius (tenths)");
        CloudStormChance    = Extra("Cloud", new IntOpt("CloudStormChance", OptTab.Neutral, 10, 0, 100, 5, c), "Шанс, что облако окажется грозой (%)", "Chance that a cloud is a thunderstorm (%)");
        CloudFlash          = Extra("Cloud", new IntOpt("CloudFlash", OptTab.Neutral, 3, 1, 10, 1, c), "Сколько секунд мигает экран", "Panic flash duration (sec)");
        CloudStormMarker    = Extra("Cloud", new BoolOpt("CloudStormMarker", OptTab.Neutral, true, c), "Метка грозы в статусе владельца (⚡ у комнаты с грозой)", "Storm mark in the owner status (⚡ next to the storm room)");
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
        LogCooldown         = Extra("Logger", new IntOpt("LogCooldown", OptTab.Crewmate, 30, 5, 120, 5, c), "Перезарядка просмотра журнала (сек)", "Log view cooldown (sec)");
        LogLines            = Extra("Logger", new IntOpt("LogLines", OptTab.Crewmate, 6, 3, 15, 1, c), "Сколько записей показывать", "Entries shown");
        LogAnon             = Extra("Logger", new IntOpt("LogAnon", OptTab.Crewmate, 25, 0, 100, 5, c), "Шанс записи без имени (%)", "Chance an entry has no name (%)");
        ForemanChance       = Extra("Foreman", new IntOpt("ForemanHelp", OptTab.Crewmate, 35, 5, 100, 5, c), "Шанс засчитать чужое задание (%)", "Chance to complete a crewmate's task (%)");
        NavCooldown         = Extra("Navigator", new IntOpt("NavCooldown", OptTab.Crewmate, 30, 5, 120, 5, c), "Перезарядка возврата (сек)", "Return cooldown (sec)");
        HoundCooldown       = Extra("Hound", new IntOpt("HoundCooldown", OptTab.Impostor, 25, 5, 120, 5, c), "Перезарядка поиска (сек)", "Search cooldown (sec)");
        PrankCooldown       = Extra("Prankster", new IntOpt("PrankCooldown", OptTab.Neutral, 45, 10, 180, 5, c), "Перезарядка обмена (сек)", "Swap cooldown (sec)");
        CrowCooldown        = Extra("Crow", new IntOpt("CrowCooldown", OptTab.Neutral, 40, 10, 180, 5, c), "Перезарядка перелёта (сек)", "Flight cooldown (sec)");
        CrowUses            = Extra("Crow", new IntOpt("CrowUses", OptTab.Neutral, 3, 1, 10, 1, c), "Перелётов за игру", "Flights per game");
        DefectorWrong       = Extra("Defector", new IntOpt("DefectorWrong", OptTab.Neutral, 40, 0, 100, 5, c), "Шанс ложной информации (%)", "Chance the info is false (%)");
        ReflectChance       = Extra("Reflector", new IntOpt("ReflectChance", OptTab.Neutral, 50, 0, 100, 5, c), "Шанс отражения (%)", "Reflect chance (%)");
        BuilderCooldown     = Extra("Builder", new IntOpt("BuilderCooldown", OptTab.Crewmate, 25, 5, 90, 5, c), "Перезарядка закрытия двери (сек)", "Door closing cooldown (sec)");
        PeasantTasks        = Extra("Peasant", new IntOpt("PeasantTasks", OptTab.Crewmate, 3, 1, 20, 1, c), "Заданий до способности", "Tasks until ability");
        SpiderMaxWebs       = Extra("Spider", new IntOpt("SpiderMaxWebs", OptTab.Impostor, 3, 1, 10, 1, c), "Паутин одновременно", "Webs at a time");
        SpiderPassThrough   = Extra("Spider", new BoolOpt("SpiderPassThrough", OptTab.Impostor, true, c), "Предатели и нейтралы проходят паутину", "Impostors and neutrals pass webs");
        VigilanteMaxPenalty = Extra("Vigilante", new IntOpt("VigilanteMaxPenalty", OptTab.Neutral, 2, 1, 10, 1, c), "Допустимо штрафов", "Allowed penalties");
        VigilanteCooldown   = Extra("Vigilante", new IntOpt("VigilanteCooldown", OptTab.Neutral, 25, 5, 90, 5, c), "Перезарядка убийства (сек)", "Kill cooldown (sec)");
        VigilanteUnlockTime = Extra("Vigilante", new IntOpt("VigilanteUnlockTime", OptTab.Neutral, 60, 0, 300, 10, c), "Через сколько секунд открывается убийство", "Seconds until the kill unlocks");

        Roles30.Define(c);
        Roles75.Define(c);
        Mars.Define(c);

        // --- Другое ---
        ShowCredits = new("ShowCredits", OptTab.Other, true, c);

        // Однократная миграция (0.1.47): с режимом host-only (+25) статус над ником и метка-кукла снова включены
        // (в 0.1.40 они были выключены из-за киков на официальном сервере без +25). Без +25 они сами отключаются (Utils.AnticheatLikely).
        // Дальше значения меняются только вручную.
        try
        {
            var mig = c.Bind("Internal", "Migrated47", false);
            if (!mig.Value)
            {
                mig.Value = true;
                NameStatus.Value = true;
                CloudStormMarker.Value = true;
            }
        }
        catch { }
    }
}
