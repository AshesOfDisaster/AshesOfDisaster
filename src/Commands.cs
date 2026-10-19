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
    /// Назначенные роли игроков: PlayerId -> роль. Заполняется логикой ролей (когда она будет подключена).
    public static readonly Dictionary<byte, RoleDef> Assigned = new();

    /// Роли, выданные хостом заранее (в лобби): PlayerId -> роль. Применяются при старте игры раньше случайной раздачи.
    public static readonly Dictionary<byte, RoleDef> Preset = new();
}

public static class Commands
{
    public static readonly List<CmdInfo> All = new()
    {
        new() { Name = "h", Aliases = new[] { "help" }, UsageRu = "[команда]", UsageEn = "[command]", DescRu = "список команд / помощь по команде", DescEn = "command list / help for a command" },
        new() { Name = "r", Aliases = new[] { "roles" }, UsageRu = "[роль]", UsageEn = "[role]", DescRu = "список ролей / описание роли", DescEn = "role list / role description" },
        new() { Name = "m", Aliases = new[] { "myrole" }, DescRu = "твоя роль и её описание", DescEn = "your role and its description" },
        new() { Name = "id", DescRu = "ID игроков", DescEn = "player IDs" },
        new() { Name = "lang", UsageRu = "ru|en", UsageEn = "ru|en", DescRu = "язык ответов", DescEn = "reply language" },
        new() { Name = "v", Aliases = new[] { "version" }, DescRu = "версия мода", DescEn = "mod version" },
        new() { Name = "now", DescRu = "текущие настройки мода", DescEn = "current mod settings" },
        new() { Name = "t", Aliases = new[] { "time" }, DescRu = "время до автозапуска", DescEn = "time until auto-start" },
        new() { Name = "color", UsageRu = "0-17", UsageEn = "0-17", DescRu = "сменить свой цвет (в лобби)", DescEn = "change your color (lobby)" },
        new() { Name = "o", Aliases = new[] { "opt" }, HostOnly = true, UsageRu = "[№ [знач]]", UsageEn = "[# [val]]", DescRu = "настройки мода", DescEn = "mod options" },
        new() { Name = "kick", HostOnly = true, UsageRu = "ID", UsageEn = "ID", DescRu = "кикнуть игрока", DescEn = "kick a player" },
        new() { Name = "ban", HostOnly = true, UsageRu = "ID", UsageEn = "ID", DescRu = "забанить игрока", DescEn = "ban a player" },
        new() { Name = "unban", HostOnly = true, UsageRu = "friendcode", UsageEn = "friendcode", DescRu = "убрать из бан-листа", DescEn = "remove from ban list" },
        new() { Name = "say", Aliases = new[] { "s" }, HostOnly = true, UsageRu = "текст", UsageEn = "text", DescRu = "сообщение от хоста", DescEn = "message from host" },
        new() { Name = "start", HostOnly = true, DescRu = "запустить игру сейчас", DescEn = "start the game now" },
        new() { Name = "cancel", HostOnly = true, DescRu = "отменить запуск / автозапуск", DescEn = "cancel start / auto-start" },
        new() { Name = "end", HostOnly = true, DescRu = "завершить игру", DescEn = "end the game" },
        new() { Name = "tag", HostOnly = true, UsageRu = "ID текст | ID", UsageEn = "ID text | ID", DescRu = "тег над ником игрока (без текста — снять); хранится, пока жива комната", DescEn = "tag above a player's name (no text — remove); kept while the room lives" },
        new() { Name = "tpout", UsageRu = "", UsageEn = "", DescRu = "телепорт за корабль (только в лобби)", DescEn = "teleport outside the ship (lobby only)" },
        new() { Name = "tpin", UsageRu = "", UsageEn = "", DescRu = "телепорт обратно в корабль (только в лобби)", DescEn = "teleport back into the ship (lobby only)" },
        new() { Name = "kill", Aliases = new[] { "exe" }, HostOnly = true, UsageRu = "ID", UsageEn = "ID", DescRu = "убить игрока", DescEn = "kill a player" },
        new() { Name = "ab", Aliases = new[] { "psy" }, DescRu = "применить способность роли (то же, что погладить питомца)", DescEn = "use your role ability (same as petting the pet)" },
        new() { Name = "who", HostOnly = true, DescRu = "кто какую роль получил (для хоста)", DescEn = "who got which role (host)" },
        new() { Name = "role", HostOnly = true, UsageRu = "ID|ник роль | none | clear", UsageEn = "ID|name role | none | clear", DescRu = "выдать роль игроку заранее (в лобби) или сейчас; без аргументов — список", DescEn = "give a player a role in advance (lobby) or now; no arguments — list" },
        new() { Name = "dump", HostOnly = true, DescRu = "сохранить список игроков в файл", DescEn = "save player list to a file" },
    };

    static CmdInfo Find(string n) => All.Find(c => c.Is(n));

    static string Loc(PlayerControl p, string ru, string en) => Translator.IsRu(p) ? ru : en;

    static void SayChunks(PlayerControl to, IEnumerable<string> lines)
        => Utils.SendPrivate(to, string.Join("\n", lines));

    static string CmdLine(CmdInfo c, PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        return $"/{c.Name} {(ru ? c.UsageRu : c.UsageEn)} — {(ru ? c.DescRu : c.DescEn)}".Replace("  ", " ");
    }

    /// Возвращает true, если сообщение было командой (тогда оно не уходит в чат).
    public static bool Handle(PlayerControl sender, string text)
    {
        Utils.CommandContext = true;     // ответы на команды отправляются вне общей очереди
        try { return HandleInner(sender, text); }
        finally { Utils.CommandContext = false; }
    }

    static readonly Vector2 TpOutPos = new(0.1f, 3.8f);          // за кораблём в лобби
    static readonly Vector2 TpInDefault = new(-0.2f, 1.3f);     // внутри корабля

    // Телепорт: хост двигает свою копию с запасом sequence id и шлёт SnapTo с ещё большим номером
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
                        help += "\n" + (ru ? "/h <команда> — подробно" : "/h <command> for details");
                        Say(help);
                        return true;
                    }

                case "r":
                    {
                        string q0 = string.Join(" ", a).ToLowerInvariant();
                        OptTab? cls = q0 is "1" or "экипаж" or "мирные" or "crew" ? OptTab.Crewmate
                                    : q0 is "2" or "предатели" or "impostors" ? OptTab.Impostor
                                    : q0 is "3" or "нейтралы" or "neutrals" ? OptTab.Neutral : (OptTab?)null;
                        if (a.Length == 0 || cls != null)
                        {
                            // в одно сообщение все роли не влезают (чат режется до 3 сообщений) — список выдаётся по классам
                            if (cls == null)
                            {
                                int nc = Opts.Roles.Count(r => r.Tab == OptTab.Crewmate), ni = Opts.Roles.Count(r => r.Tab == OptTab.Impostor), nn = Opts.Roles.Count(r => r.Tab == OptTab.Neutral);
                                SayChunks(sender, new[]
                                {
                                    ru ? $"/r 1 — экипаж ({nc})" : $"/r 1 - crew ({nc})",
                                    ru ? $"/r 2 — предатели ({ni})" : $"/r 2 - impostors ({ni})",
                                    ru ? $"/r 3 — нейтралы ({nn})" : $"/r 3 - neutrals ({nn})",
                                    ru ? "/r <роль> — описание" : "/r <role> - description"
                                });
                                return true;
                            }
                            var names = Opts.Roles.Where(r => r.Tab == cls.Value).Select(r => ru ? r.Ru : r.En);
                            Utils.SendPrivate(sender, string.Join(", ", names), 6);
                            return true;
                        }
                        string q = string.Join(" ", a).ToLowerInvariant();
                        var role = Opts.Roles.Find(r => r.Id.ToLowerInvariant().StartsWith(q) || r.Ru.ToLowerInvariant().StartsWith(q) || r.En.ToLowerInvariant().StartsWith(q));
                        if (role == null) { Say(R("cmd.norole")); return true; }
                        Utils.SendPrivate(sender, $"{role.Colored(ru)}\n{(ru ? role.DescRu : role.DescEn)}", 5);
                        return true;
                    }

                case "m":
                    {
                        if (RoleState.Assigned.TryGetValue(sender.PlayerId, out var mine))
                            Utils.SendSecret(sender, RoleLogic.Describe(sender, mine) + "\n" + (RoleLogic.Status(sender, mine) ?? ""), 5);
                        else Utils.SendSecret(sender, R("cmd.norolenow"));
                        return true;
                    }

                case "id":
                    {
                        var sb = new StringBuilder();
                        foreach (var p in PlayerControl.AllPlayerControls)
                            if (p != null) sb.Append(p.PlayerId).Append(" — ").Append(p.Data?.PlayerName).Append('\n');
                        Say(sb.ToString().TrimEnd());
                        return true;
                    }

                case "lang":
                    if (a.Length > 0) Translator.PlayerRu[sender.PlayerId] = a[0].ToLowerInvariant().StartsWith("ru");
                    Say(R("cmd.lang"));
                    return true;

                case "v":
                    Say($"{Plugin.Name} v{Plugin.Version} — {Plugin.Creators}");
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
                    if (!host) { Say(R("cmd.noperm")); return true; }
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
                        // /role — список выданных заранее ролей
                        if (a.Length == 0)
                        {
                            if (RoleState.Preset.Count == 0) { Say(ru ? "Роли заранее не выданы. /role ID роль — выдать." : "No preset roles. /role ID role — assign."); return true; }
                            var lines = RoleState.Preset.Select(kv => $"{kv.Key} {Utils.GetPlayer(kv.Key)?.Data?.PlayerName ?? "?"} — {kv.Value.Colored(ru)}");
                            SayChunks(sender, new[] { ru ? "Выданные роли:" : "Preset roles:" }.Concat(lines));
                            return true;
                        }
                        string first = a[0].ToLowerInvariant();
                        if (first == "clear") { RoleState.Preset.Clear(); Say(R("cmd.done")); return true; }
                        // игрок: ID или часть ника
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
                        if (!lobby) RoleState.Assigned[target.PlayerId] = role;      // в игре — сразу (для тестов)
                        Say(ru ? $"{target.Data.PlayerName}: {role.Colored(true)} — {(lobby ? "роль будет выдана на старте игры" : "роль выдана сейчас")}."
                               : $"{target.Data.PlayerName}: {role.Colored(false)} — {(lobby ? "will be given at game start" : "given now")}.");
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
