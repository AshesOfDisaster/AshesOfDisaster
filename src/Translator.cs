using System.Collections.Generic;

namespace AshesOfDisaster;

public static class Translator
{
    public static bool GameIsRu
    {
        get
        {
            try { return TranslationController.Instance != null && TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Russian; }
            catch { return false; }
        }
    }
    public static readonly Dictionary<byte, bool> PlayerRu = new();

    static readonly Dictionary<string, (string ru, string en)> D = new()
    {
        ["tab.System"] = ("Системные", "System"),
        ["tab.Game"] = ("Настройки игры", "Game settings"),
        ["tab.Crewmate"] = ("Мирные", "Crewmates"),
        ["tab.Impostor"] = ("Предатели", "Impostors"),
        ["tab.Neutral"] = ("Нейтралы", "Neutrals"),
        ["tab.Other"] = ("Другое", "Other"),

        ["opt.NoEndFewPlayers"] = ("Не заканчивать игру при 3 игроках и меньше (тест)", "Never end the game with 3 players or fewer (testing)"),
        ["opt.NoGameEnd"] = ("Игра не заканчивается", "No game end"),
        ["opt.SyncButtonMode"] = ("Общий лимит кнопок", "Synced emergency buttons"),
        ["opt.SyncedButtonCount"] = ("Кнопок на всех", "Buttons for everyone"),
        ["opt.KillCooldownPercent"] = ("Кулдаун убийства (%)", "Kill cooldown (%)"),
        ["opt.DisableCloseDoors"] = ("Отключить закрытие дверей", "Disable closing doors"),
        ["opt.GhostSeeRoles"] = ("Призраки видят роли", "Ghosts see roles"),
        ["opt.ButtonAbilities"] = ("Способности на кнопках игры (смена облика, люк, кнопка защиты у призраков) вместо питомца", "Abilities on game buttons (shapeshift, vent, ghost protect button) instead of the pet"),
        ["opt.HostAuthority"] = ("Режим «host-only мод» (+25 к версии: ослабляет античит сервера; действует со СЛЕДУЮЩЕГО создания комнаты)", "Host-only mod mode (+25 protocol version: relaxes server anticheat; applies from the NEXT lobby you create)"),
        ["opt.NameStatus"] = ("Статус над ником: ЛИЧНЫЙ, каждый видит только свой (нужен режим host-only или свой сервер)", "Status above nickname: PRIVATE, each player sees only their own (needs host-only mode or own server)"),
        ["opt.TeamSwap"] = ("Смена команды: роли предателей можно выдавать мирным (и наоборот), в игре вдвоём один становится предателем", "Team swap: impostor roles can go to crewmates (and vice versa); in a 2-player game one becomes an impostor"),
        ["opt.PetForAll"] = ("Всем в раунде выдать питомца, который стоит у хоста", "Give everyone in a round the host's pet"),
        ["opt.RevealOnExile"] = ("Писать роль выброшенного игрока", "Reveal role of ejected player"),

        ["opt.AnyPlayers"] = ("Старт с любого числа игроков", "Start with any player count"),
        ["opt.AutoStart"] = ("Автозапуск игры", "Auto-start game"),
        ["opt.AutoStartPlayers"] = ("Игроков для автозапуска", "Players for auto-start"),
        ["opt.AutoStartDelay"] = ("Задержка автозапуска (сек)", "Auto-start delay (sec)"),
        ["opt.Welcome"] = ("Приветствие при входе", "Welcome message"),
        ["opt.PlayerCommands"] = ("Команды для игроков", "Commands for players"),
        ["opt.ChatNoLimit"] = ("Без лимитов в чате: длина и число сообщений (выключи, если сообщения пропадают)", "No chat limits: message length and count (turn off if messages vanish)"),
        ["opt.ChatColors"] = ("Цветные роли в чате (выключи, если сервер ругается)", "Colored roles in chat (turn off if the server complains)"),
        ["opt.UseBanList"] = ("Бан-лист (BanList.txt)", "Ban list (BanList.txt)"),
        ["opt.UseDenyNames"] = ("Запрещённые ники (DenyName.txt)", "Denied names (DenyName.txt)"),
        ["opt.KickLowLevel"] = ("Кик низкого уровня", "Kick low level"),
        ["opt.MinLevel"] = ("Минимальный уровень", "Minimum level"),
        ["opt.KickNoFriendCode"] = ("Кик без friend code", "Kick without friend code"),
        ["opt.ShowCredits"] = ("Создатели в меню", "Credits in menu"),

        ["welcome"] = ("Привет, {0}! Это комната с модом «{1}» (сделали {2}). Тут больше 90 ролей и у каждой своя способность - погладь питомца и она сработает. Команды: /m - твоя роль, /r - список ролей, /h - все команды.", "Hi {0}! This is a room with the \"{1}\" mod (made by {2}). There are over 90 roles and each one has its own ability - pet your pet to use it. Commands: /m - your role, /r - role list, /h - all commands."),
        ["kick.level"] = ("{0} кикнут: низкий уровень", "{0} kicked: level too low"),
        ["kick.fc"] = ("{0} кикнут: нет friend code", "{0} kicked: no friend code"),
        ["kick.ban"] = ("{0} кикнут: бан-лист", "{0} kicked: banned"),
        ["kick.name"] = ("{0} кикнут: запрещённый ник", "{0} kicked: denied name"),

        ["cmd.noperm"] = ("Эта команда только для хоста.", "This command is for the host only."),
        ["cmd.unknown"] = ("Такой команды нет. Список команд: /h", "No such command. Command list: /h"),
        ["cmd.nolobby"] = ("Только в лобби.", "Lobby only."),
        ["cmd.noplayer"] = ("Игрок не найден. Номера игроков смотри в /id", "Player not found. Player numbers: /id"),
        ["cmd.done"] = ("Готово.", "Done."),
        ["cmd.help.all"] = ("/h - помощь, /id - ID игроков, /lang ru|en - язык ответов", "/h help, /id player IDs, /lang ru|en reply language"),
        ["cmd.help.host"] = ("Хост: /o [№ [знач]] настройки, /kick ID, /ban ID, /unban FC, /say текст, /start, /cancel, /end, /dump", "Host: /o [# [val]] options, /kick ID, /ban ID, /unban FC, /say text, /start, /cancel, /end, /dump"),
        ["cmd.lang"] = ("Язык ответов: русский", "Reply language: English"),
        ["cmd.norole"] = ("Такой роли нет. Список ролей: /r", "No such role. Role list: /r"),
        ["cmd.norolenow"] = ("У тебя сейчас нет роли (роли выдаются, когда начинается игра).", "You have no role right now (roles are given when the game starts)."),
        ["cmd.cancel"] = ("Запуск отменён.", "Start cancelled."),
        ["cmd.dump"] = ("Сохранено: {0}", "Saved: {0}"),
        ["cmd.opts"] = ("Настройки (/o № значение):", "Options (/o # value):"),
    };

    public static bool IsRu(PlayerControl p = null)
        => p != null && PlayerRu.TryGetValue(p.PlayerId, out var r) ? r : GameIsRu;

    public static string T(string key, PlayerControl p = null)
        => D.TryGetValue(key, out var v) ? (IsRu(p) ? v.ru : v.en) : key;

    public static string T(string key, PlayerControl p, params object[] a) => string.Format(T(key, p), a);
}
