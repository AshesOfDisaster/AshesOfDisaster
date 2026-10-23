using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hazel;
using UnityEngine;

namespace AshesOfDisaster;

public class CmdInfo
{
    public string Name;
    public string[] Aliases = Array.Empty<string>();
    public bool HostOnly;
    public string UsageRu = "", UsageEn = "", DescRu, DescEn;
    public bool Is(string n) => Name == n || Aliases.Contains(n);
}

public static class RoleState
{
    public static readonly Dictionary<byte, RoleDef> Assigned = new();

    public static readonly Dictionary<byte, RoleDef> Preset = new();
}

public static class Commands
{
    public static readonly List<CmdInfo> All = new()
    {
        new() { Name = "h", Aliases = new[] { "help" }, UsageRu = "[команда]", UsageEn = "[command]", DescRu = "список всех команд, а /h m расскажет подробно про команду /m", DescEn = "list of all commands; /h m explains the /m command in detail" },
        new() { Name = "r", Aliases = new[] { "roles" }, UsageRu = "[роль]", UsageEn = "[role]", DescRu = "все роли по группам, а /r Сантехник покажет, что умеет эта роль", DescEn = "all roles by group; /r Plumber shows what that role does" },
        new() { Name = "m", Aliases = new[] { "myrole" }, DescRu = "твоя роль: что она делает, готова ли способность и сколько использований осталось", DescEn = "your role: what it does, whether the ability is ready and how many uses are left" },
        new() { Name = "id", DescRu = "номера игроков в комнате (они нужны для /kick /ban /role и других команд)", DescEn = "player numbers in the room (needed for /kick /ban /role and other commands)" },
        new() { Name = "lang", UsageRu = "ru|en", UsageEn = "ru|en", DescRu = "на каком языке тебе отвечает мод: /lang ru или /lang en", DescEn = "which language the mod answers you in: /lang ru or /lang en" },
        new() { Name = "v", Aliases = new[] { "version" }, DescRu = "версия мода и кто его сделал", DescEn = "mod version and who made it" },
        new() { Name = "now", DescRu = "все текущие настройки мода", DescEn = "all current mod settings" },
        new() { Name = "t", Aliases = new[] { "time" }, DescRu = "сколько секунд осталось до автозапуска игры", DescEn = "seconds left until the game starts by itself" },
        new() { Name = "color", UsageRu = "0-17", UsageEn = "0-17", DescRu = "сменить цвет своего персонажа, например /color 5 (число от 0 до 17, только в лобби)", DescEn = "change your character color, for example /color 5 (a number from 0 to 17, lobby only)" },
        new() { Name = "o", Aliases = new[] { "opt" }, HostOnly = true, UsageRu = "[№ [знач]]", UsageEn = "[# [val]]", DescRu = "настройки мода: /o покажет список с номерами, /o 5 покажет настройку 5, /o 5 30 поставит ей значение 30", DescEn = "mod settings: /o shows the numbered list, /o 5 shows setting 5, /o 5 30 sets it to 30" },
        new() { Name = "kick", HostOnly = true, UsageRu = "ID", UsageEn = "ID", DescRu = "выгнать игрока из комнаты, например /kick 3 (номер смотри в /id)", DescEn = "kick a player out of the room, for example /kick 3 (get the number from /id)" },
        new() { Name = "ban", HostOnly = true, UsageRu = "ID", UsageEn = "ID", DescRu = "выгнать игрока и забанить его насовсем, например /ban 3 (номер смотри в /id)", DescEn = "kick a player and ban him for good, for example /ban 3 (get the number from /id)" },
        new() { Name = "unban", HostOnly = true, UsageRu = "friendcode", UsageEn = "friendcode", DescRu = "убрать игрока из бан-листа по его friend code", DescEn = "remove a player from the ban list by his friend code" },
        new() { Name = "say", Aliases = new[] { "s" }, HostOnly = true, UsageRu = "текст", UsageEn = "text", DescRu = "написать в чат от имени хоста, например /say всем привет", DescEn = "write to the chat as the host, for example /say hello everyone" },
        new() { Name = "start", HostOnly = true, DescRu = "запустить игру прямо сейчас", DescEn = "start the game right now" },
        new() { Name = "cancel", HostOnly = true, DescRu = "отменить запуск игры или автозапуск", DescEn = "cancel the game start or the auto-start" },
        new() { Name = "end", HostOnly = true, DescRu = "закончить игру прямо сейчас", DescEn = "end the game right now" },
        new() { Name = "tag", HostOnly = true, UsageRu = "ID текст | ID", UsageEn = "ID text | ID", DescRu = "надпись над ником игрока: /tag 3 Главный поставит её, /tag 3 уберёт (хранится, пока жива комната)", DescEn = "a label above a player name: /tag 3 Boss sets it, /tag 3 removes it (kept while the room lives)" },
        new() { Name = "tpout", UsageRu = "", UsageEn = "", DescRu = "выйти за пределы корабля (только в лобби)", DescEn = "teleport outside the ship (lobby only)" },
        new() { Name = "tpin", UsageRu = "", UsageEn = "", DescRu = "вернуться в корабль (только в лобби)", DescEn = "teleport back into the ship (lobby only)" },
        new() { Name = "kill", Aliases = new[] { "exe" }, HostOnly = true, UsageRu = "ID", UsageEn = "ID", DescRu = "убить игрока, например /kill 3 (номер смотри в /id)", DescEn = "kill a player, for example /kill 3 (get the number from /id)" },
        new() { Name = "ab", Aliases = new[] { "psy" }, DescRu = "применить способность роли командой: Психолог на собрании пишет /ab 3, Статистик просто /ab, а хост может так включить любую способность без питомца", DescEn = "use a role ability by command: the Psychologist writes /ab 3 in a meeting, the Statistician just /ab, and the host can trigger any ability without a pet" },
        new() { Name = "boo", Aliases = new[] { "poltergeist" }, UsageRu = "[текст]", UsageEn = "[text]", DescRu = "призрак-полтергейст: шепнуть всем случайное слово на собрании (или своё: /boo слово)", DescEn = "Poltergeist ghost: whisper a random word to everyone in a meeting (or your own: /boo word)" },
        new() { Name = "haunt", Aliases = new[] { "possess" }, UsageRu = "[ID]", UsageEn = "[ID]", DescRu = "призрак-предатель: вселиться в живого предателя, /haunt покажет список, /haunt 3 вселит в игрока 3", DescEn = "impostor ghost: possess a living impostor, /haunt shows the list, /haunt 3 possesses player 3" },
        new() { Name = "hk", DescRu = "вселенец: убить ближайшего мирного руками того, в кого ты вселился", DescEn = "possessor: kill the nearest crewmate with the hands of the one you possess" },
        new() { Name = "task", Aliases = new[] { "helper" }, UsageRu = "[ID]", UsageEn = "[ID]", DescRu = "призрак-мирный: помочь игроку с заданием, /task покажет список, /task 3 выберет игрока 3", DescEn = "crewmate ghost: help a player with a task, /task shows the list, /task 3 picks player 3" },
        new() { Name = "solve", UsageRu = "число", UsageEn = "number", DescRu = "ответить на задачу помощника, например /solve 42", DescEn = "answer the helper puzzle, for example /solve 42" },
        new() { Name = "who", HostOnly = true, DescRu = "кто какую роль получил (только хост)", DescEn = "who got which role (host only)" },
        new() { Name = "role", HostOnly = true, UsageRu = "ID|ник роль | none | clear", UsageEn = "ID|name role | none | clear", DescRu = "выдать роль игроку, например /role 3 Сантехник (в лобби заранее или сразу в игре), /role 3 none забирает роль, /role clear сбрасывает всё, /role без аргументов показывает список", DescEn = "give a player a role, for example /role 3 Plumber (in the lobby in advance or right now), /role 3 none takes it away, /role clear resets everything, /role alone shows the list" },
        new() { Name = "perk", DescRu = "твой перк: случайный довесок на игру, а Шустрый применяет его этой же командой", DescEn = "your perk: a random bonus for the game, the Quick perk is used by this same command" },
        new() { Name = "bounty", DescRu = "твоя тайная цель: если она погибнет к собранию, получишь щит", DescEn = "your secret target: if it is dead by a meeting you get a shield" },
        new() { Name = "games", DescRu = "мини-игры в лобби: угадай число, кубик, викторина, монетка, слоты и другие", DescEn = "lobby mini-games: guess, dice, quiz, coin, slots and more" },
        new() { Name = "dump", HostOnly = true, DescRu = "сохранить список игроков в файл", DescEn = "save the player list to a file" },
        new() { Name = "block", HostOnly = true, UsageRu = "ID [причина]", UsageEn = "ID [reason]", DescRu = "заблокировать игрока: кик и бан-лист, например /block 3 читер", DescEn = "block a player: kick and ban list, for example /block 3 cheater" },
        new() { Name = "report", HostOnly = true, UsageRu = "ID [причина]", UsageEn = "ID [reason]", DescRu = "отправить жалобу на игрока в Discord, например /report 3 читер", DescEn = "send a report about a player to Discord, for example /report 3 cheater" },
        new() { Name = "ac", HostOnly = true, DescRu = "что нашёл античит и включён ли он", DescEn = "what the anticheat found and whether it is on" },
    };

    static CmdInfo Find(string n) => All.Find(c => c.Is(n));

    static string Loc(PlayerControl p, string ru, string en) => Translator.IsRu(p) ? ru : en;

    static void SayChunks(PlayerControl to, IEnumerable<string> lines)
        => Utils.SendPrivate(to, string.Join("\n", lines));

    static string CmdLine(CmdInfo c, PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        return $"/{c.Name} {(ru ? c.UsageRu : c.UsageEn)} - {(ru ? c.DescRu : c.DescEn)}".Replace("  ", " ");
    }

    public static bool Handle(PlayerControl sender, string text)
    {
        Utils.CommandContext = true;
        try { return HandleInner(sender, text); }
        finally { Utils.CommandContext = false; }
    }

    static readonly Vector2 TpOutPos = new(0.1f, 3.8f);
    static readonly Vector2 TpInDefault = new(-0.2f, 1.3f);

    public static void Tp(PlayerControl pc, Vector2 pos)
    {
        try
        {
            var nt = pc.NetTransform;
            if (pc.AmOwner) { nt.SnapTo(pos); return; }
            nt.SnapTo(pos, (ushort)(nt.lastSequenceId + 328));
            nt.SetDirtyBit(uint.MaxValue);
            var sid = (ushort)(nt.lastSequenceId + 8);
            var w = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable);
            NetHelpers.WriteVector2(pos, w);
            w.Write(sid);
            AmongUsClient.Instance.FinishRpcImmediately(w);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Tp: " + e.Message); }
    }

    static bool HandleInner(PlayerControl sender, string text)
    {
        if (sender == null || string.IsNullOrWhiteSpace(text) || !text.StartsWith("/")) return false;
        var parts = text.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        string cmd = parts[0].ToLowerInvariant();
        string[] a = parts[1..];
        bool host = sender.AmOwner;
        bool ru = Translator.IsRu(sender);

        if (!host && !Opts.PlayerCommands.Value) return false;

        string R(string k, params object[] p) => Translator.T(k, sender, p);
        void Say(string s) => Utils.SendPrivate(sender, s);

        if (LobbyGames.Handle(sender, cmd, a)) return true;
        if (Extras.Handle(sender, cmd, a)) return true;
        if (Ghosts.Handle(sender, cmd, a)) return true;

        var info = Find(cmd);
        if (info != null && info.HostOnly && !host) { Say(R("cmd.noperm")); return true; }

        try
        {
            switch (info?.Name)
            {
                case "h":
                    {
                        if (a.Length > 0)
                        {
                            var c = Find(a[0].TrimStart('/').ToLowerInvariant());
                            Say(c == null || (c.HostOnly && !host) ? R("cmd.unknown") : CmdLine(c, sender));
                            return true;
                        }
                        string Names(bool hostOnly) => string.Join(" ", All.Where(c => c.HostOnly == hostOnly).Select(c => "/" + c.Name));
                        var help = (ru ? "Команды: " : "Commands: ") + Names(false);
                        if (host) help += "\n" + (ru ? "Хост: " : "Host: ") + Names(true);
                        help += "\n" + (ru ? "Подробно про команду: /h команда" : "Details about a command: /h command");
                        Say(help);
                        return true;
                    }

                case "r":
                    {
                        if (a.Length == 0)
                        {
                            string Grp(OptTab t, string title)
                            {
                                var names = Opts.Roles.Where(r => r.Tab == t).Select(r => ru ? r.Ru : r.En).ToList();
                                return title + " (" + names.Count + "): " + string.Join(", ", names);
                            }
                            Utils.SendPrivate(sender, string.Join("\n", new[]
                            {
                                Grp(OptTab.Crewmate, ru ? "Мирные" : "Crew"),
                                Grp(OptTab.Impostor, ru ? "Предатели" : "Impostors"),
                                Grp(OptTab.Neutral, ru ? "Нейтралы" : "Neutrals"),
                                Ghosts.List(ru),
                                ru ? "Что умеет роль: /r Сантехник (своя роль: /m)" : "What a role does: /r Plumber (your own role: /m)"
                            }), 14);
                            return true;
                        }
                        string q = string.Join(" ", a).ToLowerInvariant();
                        var role = Opts.Roles.Find(r => r.Id.ToLowerInvariant().StartsWith(q) || r.Ru.ToLowerInvariant().StartsWith(q) || r.En.ToLowerInvariant().StartsWith(q));
                        if (role == null) { var gd = Ghosts.Find(q, ru); if (gd != null) { Utils.SendPrivate(sender, gd, 5); return true; } Say(R("cmd.norole")); return true; }
                        Utils.SendPrivate(sender, $"{role.Colored(ru)}\n{(ru ? role.DescRu : role.DescEn)}", 5);
                        return true;
                    }

                case "m":
                    {
                        if (RoleState.Assigned.TryGetValue(sender.PlayerId, out var mine))
                        {
                            string liveNow = RoleLogic.Live(mine, sender.PlayerId, ru);
                            Utils.SendSecret(sender, RoleLogic.Describe(sender, mine) + "\n" + (RoleLogic.Status(sender, mine) ?? "") + (liveNow != null ? "\n" + liveNow : ""), 5);
                        }
                        else { var gr = Ghosts.MyRole(sender, ru); Utils.SendSecret(sender, gr ?? R("cmd.norolenow")); }
                        return true;
                    }

                case "id":
                    {
                        var sb = new StringBuilder();
                        foreach (var p in PlayerControl.AllPlayerControls)
                            if (p != null) sb.Append(p.PlayerId).Append(" - ").Append(p.Data?.PlayerName).Append('\n');
                        Say(sb.ToString().TrimEnd());
                        return true;
                    }

                case "lang":
                    if (a.Length > 0) Translator.PlayerRu[sender.PlayerId] = a[0].ToLowerInvariant().StartsWith("ru");
                    Say(R("cmd.lang"));
                    return true;

                case "v":
                    Say($"{Plugin.Name} v{Plugin.Version} - {Plugin.Creators}");
                    return true;

                case "now":
                    {
                        var sb = new StringBuilder();
                        foreach (var o in Opt.All)
                            if (o.Tab != OptTab.Crewmate && o.Tab != OptTab.Impostor && o.Tab != OptTab.Neutral)
                                sb.Append(o.Name).Append(": ").Append(o.ValueText).Append('\n');
                        SayChunks(sender, sb.ToString().TrimEnd().Split('\n'));
                        return true;
                    }

                case "t":
                    {
                        float left = ModGui.AutoStartAt - UnityEngine.Time.time;
                        Say(Opts.AutoStart.Value && left > 0 ? Loc(sender, $"До автозапуска: {(int)left} с", $"Auto-start in {(int)left}s") : Loc(sender, "Автозапуск не идёт.", "Auto-start is not running."));
                        return true;
                    }

                case "color":
                    {
                        if (!Utils.InLobby) { Say(R("cmd.nolobby")); return true; }
                        if (a.Length == 0 || !byte.TryParse(a[0], out var col) || col > 17) { Say(CmdLine(info, sender)); return true; }
                        sender.RpcSetColor(col);
                        return true;
                    }

                case "o":
                    if (a.Length == 0)
                    {
                        var o = new StringBuilder(R("cmd.opts")).Append('\n');
                        for (int i = 0; i < Opt.All.Count; i++)
                            o.Append(i + 1).Append(". [").Append(Translator.T("tab." + Opt.All[i].Tab, sender)).Append("] ")
                             .Append(Opt.All[i].Name).Append(": ").Append(Opt.All[i].ValueText).Append('\n');
                        SayChunks(sender, o.ToString().TrimEnd().Split('\n'));
                    }
                    else if (int.TryParse(a[0], out var idx) && idx >= 1 && idx <= Opt.All.Count)
                    {
                        var opt = Opt.All[idx - 1];
                        if (a.Length > 1) opt.Set(a[1].ToLowerInvariant()); else opt.Next();
                        Say(opt.Name + ": " + opt.ValueText);
                    }
                    return true;

                case "kick":
                case "ban":
                    {
                        if (a.Length == 0 || !byte.TryParse(a[0], out var id)) { Say(R("cmd.noplayer")); return true; }
                        var t = Utils.GetPlayer(id);
                        var c = t == null ? null : AmongUsClient.Instance.GetClientFromCharacter(t);
                        if (c == null || t.AmOwner) { Say(R("cmd.noplayer")); return true; }
                        if (info.Name == "ban") Lists.Ban(c.FriendCode);
                        AmongUsClient.Instance.KickPlayer(c.Id, info.Name == "ban");
                        Say(R("cmd.done"));
                        return true;
                    }

                case "block":
                case "report":
                    {
                        if (a.Length == 0 || !byte.TryParse(a[0], out var tid)) { Say(R("cmd.noplayer")); return true; }
                        var tp = Utils.GetPlayer(tid);
                        if (tp == null || tp.AmOwner || tp.Data == null) { Say(R("cmd.noplayer")); return true; }
                        string reason = a.Length > 1 ? string.Join(" ", a.Skip(1)) : null;
                        if (info.Name == "block") Say(ModPanel.Block(tp, reason ?? "блок хостом") ? R("cmd.done") : R("cmd.noplayer"));
                        else Say(ModPanel.Complain(tp, reason));
                        return true;
                    }

                case "ac":
                    Say(Anticheat.Summary(ru));
                    return true;

                case "unban":
                    if (a.Length > 0) Lists.Unban(a[0]);
                    Say(R("cmd.done"));
                    return true;

                case "say":
                    if (a.Length > 0) PlayerControl.LocalPlayer.RpcSendChat(string.Join(" ", a));
                    return true;

                case "start":
                    if (!Utils.InLobby) { Say(R("cmd.nolobby")); return true; }
                    GameStartManager.Instance.BeginGame();
                    return true;

                case "cancel":
                    if (GameStartManager.Instance != null) GameStartManager.Instance.ResetStartState();
                    ModGui.CancelAutoStart();
                    Say(R("cmd.cancel"));
                    return true;

                case "end":
                    Utils.EndGame();
                    return true;

                case "tag":
                    {
                        if (a.Length == 0 || !byte.TryParse(a[0], out var tid)) { Say(R("cmd.noplayer")); return true; }
                        var tp = Utils.GetPlayer(tid);
                        if (tp == null) { Say(R("cmd.noplayer")); return true; }
                        string tx = string.Join(" ", a.Skip(1));
                        NameInfo.SetTag(tp, tx);
                        Say(string.IsNullOrWhiteSpace(tx) ? (ru ? "Тег снят." : "Tag removed.") : (ru ? "Тег выдан: " : "Tag set: ") + tx);
                        return true;
                    }

                case "tpout":
                case "tpin":
                    {
                        if (!Utils.InLobby) { Say(ru ? "Работает только в лобби." : "Lobby only."); return true; }
                        var me = sender;
                        if (me == null || me.Data == null) return true;
                        var dest = info.Name == "tpout" ? TpOutPos : TpInDefault;
                        Plugin.Logger.LogInfo($"{info.Name}: {me.Data.PlayerName} (id {me.PlayerId}, owner {me.OwnerId}) -> {dest}");
                        Tp(me, dest);
                        return true;
                    }

                case "kill":
                    {
                        if (a.Length == 0 || !byte.TryParse(a[0], out var kid)) { Say(R("cmd.noplayer")); return true; }
                        var t = Utils.GetPlayer(kid);
                        if (t == null) { Say(R("cmd.noplayer")); return true; }
                        t.RpcMurderPlayer(t, true);
                        return true;
                    }

                case "ab":
                    if (Psy.Use(sender, a) || Roles30.UseMeeting(sender, a)) return true;
                    if (!host) { Say(ru ? "Эту способность включает питомец: погладь его. Командой /ab работают только способности на собрании." : "This ability is used with the pet: pet it. Only meeting abilities work with /ab."); return true; }
                    RoleLogic.UseAbility(sender, "cmd");
                    return true;

                case "who":
                    {
                        var sb2 = new StringBuilder();
                        foreach (var kv in RoleState.Assigned)
                        {
                            var pl = Utils.GetPlayer(kv.Key);
                            sb2.Append(kv.Key).Append(' ').Append(pl?.Data?.PlayerName).Append(": ").Append(kv.Value.Id).Append(" (").Append(pl?.Data?.RoleType).Append(")\n");
                        }
                        Say(sb2.Length > 0 ? sb2.ToString().TrimEnd() : (ru ? "Роли ещё не розданы." : "No roles assigned yet."));
                        return true;
                    }

                case "role":
                    {
                        bool lobby = Utils.InLobby;
                        if (a.Length == 0)
                        {
                            if (RoleState.Preset.Count == 0) { Say(ru ? "Роли заранее не выданы. /role ID роль - выдать." : "No preset roles. /role ID role - assign."); return true; }
                            var lines = RoleState.Preset.Select(kv => $"{kv.Key} {Utils.GetPlayer(kv.Key)?.Data?.PlayerName ?? "?"} - {kv.Value.Colored(ru)}");
                            SayChunks(sender, new[] { ru ? "Выданные роли:" : "Preset roles:" }.Concat(lines));
                            return true;
                        }
                        string first = a[0].ToLowerInvariant();
                        if (first == "clear") { RoleState.Preset.Clear(); Say(R("cmd.done")); return true; }
                        PlayerControl target = null;
                        if (byte.TryParse(a[0], out var rid)) target = Utils.GetPlayer(rid);
                        if (target == null)
                        {
                            var found = PlayerControl.AllPlayerControls.ToArray().Where(x => x != null && x.Data != null && !x.Data.Disconnected
                                && (x.Data.PlayerName ?? "").IndexOf(a[0], StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                            if (found.Count == 1) target = found[0];
                        }
                        if (target == null || a.Length < 2) { Say(R("cmd.noplayer")); return true; }
                        if (a[1].ToLowerInvariant() == "none")
                        {
                            RoleState.Preset.Remove(target.PlayerId); RoleState.Assigned.Remove(target.PlayerId);
                            Say(R("cmd.done")); return true;
                        }
                        string q = a[1].ToLowerInvariant();
                        var role = Opts.Roles.Find(r => r.Id.ToLowerInvariant().StartsWith(q) || r.Ru.ToLowerInvariant().StartsWith(q) || r.En.ToLowerInvariant().StartsWith(q));
                        if (role == null) { Say(R("cmd.norole")); return true; }
                        RoleState.Preset[target.PlayerId] = role;
                        if (!lobby) RoleState.Assigned[target.PlayerId] = role;
                        Say(ru ? $"{target.Data.PlayerName}: {role.Colored(true)} - {(lobby ? "роль будет выдана на старте игры" : "роль выдана сейчас")}."
                               : $"{target.Data.PlayerName}: {role.Colored(false)} - {(lobby ? "will be given at game start" : "given now")}.");
                        return true;
                    }

                case "dump":
                    {
                        var path = Path.Combine(Lists.Dir, $"dump_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        var d = new StringBuilder();
                        foreach (var cl in AmongUsClient.Instance.allClients)
                            d.AppendLine($"{cl.Id}\t{cl.Character?.PlayerId}\t{cl.PlayerName}\t{cl.FriendCode}\tlvl {cl.PlayerLevel + 1}");
                        File.WriteAllText(path, d.ToString());
                        Say(R("cmd.dump", path));
                        return true;
                    }
            }
        }
        catch (Exception e) { Plugin.Logger.LogError(e); }

        Say(R("cmd.unknown"));
        return true;
    }
}
