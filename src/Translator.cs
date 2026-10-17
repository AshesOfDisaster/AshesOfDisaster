using System.Collections.Generic;

namespace AshesOfDisaster;

public static class Translator
{
    // язык определяется по языку игры; игрок может сменить командой /lang
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
        ["opt.UseBanList"] = ("Бан-лист (BanList.txt)", "Ban list (BanList.txt)"),
        ["opt.UseDenyNames"] = ("Запрещённые ники (DenyName.txt)", "Denied names (DenyName.txt)"),
        ["opt.KickLowLevel"] = ("Кик низкого уровня", "Kick low level"),
        ["opt.MinLevel"] = ("Минимальный уровень", "Minimum level"),
        ["opt.KickNoFriendCode"] = ("Кик без friend code", "Kick without friend code"),
        ["opt.ShowCredits"] = ("Создатели в меню", "Credits in menu"),

        ["welcome"] = ("Привет! Это мод-рум {2} (создатели: {3}). Напиши /r, чтобы увидеть роли. Приятной игры!", "Hi! This is the {2} mod room (by {3}). Type /r to see the roles. Have fun!"),
        ["kick.level"] = ("{0} кикнут: низкий уровень", "{0} kicked: level too low"),
        ["kick.fc"] = ("{0} кикнут: нет friend code", "{0} kicked: no friend code"),
        ["kick.ban"] = ("{0} кикнут: бан-лист", "{0} kicked: banned"),
        ["kick.name"] = ("{0} кикнут: запрещённый ник", "{0} kicked: denied name"),

        ["cmd.noperm"] = ("Нет прав на эту команду.", "No permission for this command."),
        ["cmd.unknown"] = ("Неизвестная команда. /h — список.", "Unknown command. /h for list."),
        ["cmd.nolobby"] = ("Только в лобби.", "Lobby only."),
        ["cmd.noplayer"] = ("Игрок не найден.", "Player not found."),
        ["cmd.done"] = ("Готово.", "Done."),
        ["cmd.help.all"] = ("/h — помощь, /id — ID игроков, /lang ru|en — язык ответов", "/h help, /id player IDs, /lang ru|en reply language"),
        ["cmd.help.host"] = ("Хост: /o [№ [знач]] настройки, /kick ID, /ban ID, /unban FC, /say текст, /start, /cancel, /end, /dump", "Host: /o [# [val]] options, /kick ID, /ban ID, /unban FC, /say text, /start, /cancel, /end, /dump"),
        ["cmd.lang"] = ("Язык ответов: русский", "Reply language: English"),
        ["cmd.norole"] = ("Такой роли нет. /r — список ролей.", "No such role. /r for the list."),
        ["cmd.norolenow"] = ("У тебя сейчас нет роли.", "You have no role right now."),
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
