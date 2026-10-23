using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class Utils
{
    public static bool OnOfficialServer
    {
        get
        {
            try
            {
                var r = ServerManager.Instance.CurrentRegion;
                return r == null || (r.PingServer ?? "").Contains(".among.us");
            }
            catch { return true; }
        }
    }

    public static bool AnticheatLikely
    {
        get
        {
            return false;
        }
    }

    static readonly Dictionary<string, (string ru, string en)> RoomNames = new()
    {
        ["Hallway"] = ("коридор", "hallway"), ["Cafeteria"] = ("столовая", "cafeteria"), ["Storage"] = ("склад", "storage"),
        ["Reactor"] = ("реактор", "reactor"), ["UpperEngine"] = ("верхний двигатель", "upper engine"), ["LowerEngine"] = ("нижний двигатель", "lower engine"),
        ["Security"] = ("охрана", "security"), ["MedBay"] = ("медотсек", "medbay"), ["Electrical"] = ("электрощитовая", "electrical"),
        ["Admin"] = ("админ", "admin"), ["Comms"] = ("связь", "comms"), ["Weapons"] = ("оружейная", "weapons"),
        ["Shields"] = ("щиты", "shields"), ["Nav"] = ("навигация", "navigation"), ["LifeSupport"] = ("жизнеобеспечение", "life support"),
        ["Launchpad"] = ("стартовая площадка", "launchpad"), ["Balcony"] = ("балкон", "balcony"), ["Office"] = ("офис", "office"),
        ["Greenhouse"] = ("оранжерея", "greenhouse"), ["Laboratory"] = ("лаборатория", "laboratory"), ["Decontamination"] = ("дезинфекция", "decontamination"),
        ["LockerRoom"] = ("раздевалка", "locker room"), ["Dropship"] = ("посадочный модуль", "dropship"), ["Specimens"] = ("образцы", "specimens"),
        ["BoilerRoom"] = ("котельная", "boiler room"), ["Vault"] = ("хранилище", "vault"), ["Outside"] = ("снаружи", "outside"),
    };

    public static string RoomName(Vector2 pos, bool ru)
    {
        try
        {
            var rooms = ShipStatus.Instance != null ? ShipStatus.Instance.AllRooms : null;
            if (rooms != null)
                foreach (var room in rooms)
                {
                    if (room == null || room.roomArea == null) continue;
                    string id = room.RoomId.ToString();
                    if (id == "Hallway" || id == "Outside") continue;
                    if (!room.roomArea.OverlapPoint(pos)) continue;
                    return RoomNames.TryGetValue(id, out var n) ? (ru ? n.ru : n.en) : id;
                }
        }
        catch { }
        return ru ? "коридор" : "hallway";
    }

    public static bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
    public static bool InLobby => AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined;

    public static PlayerControl GetPlayer(byte id)
    {
        foreach (var p in PlayerControl.AllPlayerControls) if (p != null && p.PlayerId == id) return p;
        return null;
    }

    public static int PlayerCount()
    {
        int n = 0;
        foreach (var p in PlayerControl.AllPlayerControls) if (p != null) n++;
        return n;
    }

    static readonly Queue<(byte id, string text, bool broadcast, float at)> outbox = new();
    static readonly Queue<(byte id, string text, bool broadcast, float at)> urgent = new();
    static float nextSendAt;
    public static bool CommandContext;
    const int MaxLen = 95;
    static int MaxMessagesPerReply => AnticheatLikely ? 3 : 8;
    static float SendGap => AnticheatLikely ? 3.2f : 0.6f;

    static IEnumerable<string> SplitLines(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            while (line.Length > MaxLen)
            {
                int cut = line.LastIndexOf(' ', MaxLen);
                if (cut < 20) cut = MaxLen;
                yield return line.Substring(0, cut).TrimEnd();
                line = line.Substring(cut).TrimStart();
            }
            if (line.Length > 0) yield return line;
        }
    }

    static List<string> Pack(string text, int max = -1)
    {
        if (max < 0) max = MaxMessagesPerReply;
        var res = new List<string>();
        string cur = "";
        foreach (var line in SplitLines(text))
        {
            if (cur.Length == 0) cur = line;
            else if (cur.Length + 1 + line.Length <= MaxLen) cur += "\n" + line;
            else { res.Add(cur); cur = line; }
        }
        if (cur.Length > 0) res.Add(cur);
        if (res.Count > max)
        {
            res = res.GetRange(0, max);
            var last = res[max - 1];
            res[max - 1] = (last.Length > MaxLen - 2 ? last.Substring(0, MaxLen - 4) : last) + " …";
        }
        return res;
    }

    static bool TargetedMode => !string.Equals(Opts.ChatPrivate.Value, "All", StringComparison.OrdinalIgnoreCase);

    public static void SendPrivate(PlayerControl to, string text, int maxMessages = -1) => Send(to, text, false, maxMessages);

    public static List<string> PackText(string text, int max) => Pack(text, max);

    public const int ChatLimit = MaxLen;

    public static void SendRich(PlayerControl to, IEnumerable<string> messages)
    {
        if (to == null || PlayerControl.LocalPlayer == null || messages == null) return;
        bool colors = Opts.ChatColors == null || Opts.ChatColors.Value;
        var list = new List<string>();
        foreach (var m in messages)
        {
            if (string.IsNullOrWhiteSpace(m)) continue;
            string t = colors ? m : Regex.Replace(m, "<[^>]*>", "");
            list.Add(Unblur(t));
        }
        if (list.Count == 0) return;
        if (to.AmOwner) { LocalChat(string.Join("\n", list)); return; }
        var q = CommandContext ? urgent : outbox;
        foreach (var m in list)
        {
            if (q.Count >= (AnticheatLikely ? 16 : 40)) { Plugin.Logger.LogInfo("Chat queue full, message dropped"); break; }
            q.Enqueue((to.PlayerId, m, !TargetedMode, UnityEngine.Time.time));
        }
    }

    public static void SendSecret(PlayerControl to, string text, int maxMessages = -1, bool defer = true) => Send(to, text, true, maxMessages, defer);

    public static void Announce(string text)
    {
        if (string.IsNullOrEmpty(text) || PlayerControl.LocalPlayer == null) return;
        text = Regex.Replace(text, "<[^>]*>", "");
        Toast(text);
        if (PickSender(null) == null) return;
        foreach (var m in Pack(text, 2))
            urgent.Enqueue((PlayerControl.LocalPlayer.PlayerId, m, true, UnityEngine.Time.time));
    }

    static readonly List<(byte id, string text, int max, float at)> deferred = new();
    const int MaxDeferredPerPlayer = 3;

    static void Defer(PlayerControl to, string text, int max)
    {
        foreach (var d in deferred) if (d.id == to.PlayerId && d.text == text) return;
        int n = 0, oldest = -1;
        for (int i = 0; i < deferred.Count; i++)
            if (deferred[i].id == to.PlayerId) { n++; if (oldest < 0) oldest = i; }
        if (n >= MaxDeferredPerPlayer) deferred.RemoveAt(oldest);
        deferred.Add((to.PlayerId, text, max, UnityEngine.Time.time));
        Plugin.Logger.LogInfo($"Secret message to {to.Data?.PlayerName} saved until the next meeting");
    }

    public static void ClearDeferred() => deferred.Clear();

    public static void FlushDeferred()
    {
        if (deferred.Count == 0 || !ChatAllowed()) return;
        var list = new List<(byte id, string text, int max, float at)>(deferred);
        deferred.Clear();
        foreach (var d in list)
        {
            if (UnityEngine.Time.time - d.at > 1200f) continue;
            var p = GetPlayer(d.id);
            if (p == null || p.Data == null || p.Data.Disconnected) continue;
            Send(p, d.text, true, d.max, false);
        }
    }

    public static string Unblur(string text)
        => string.IsNullOrEmpty(text) ? text : Regex.Replace(text, @"(?i)(discord)(\.|\s?\.\s?)(gg|com|app)", m => m.Groups[1].Value + "\u200B" + m.Groups[2].Value + "\u200B" + m.Groups[3].Value);

    static void Send(PlayerControl to, string text, bool secret, int maxMessages, bool defer = true)
    {
        if (to == null || PlayerControl.LocalPlayer == null || string.IsNullOrEmpty(text)) return;
        if (to.AmOwner) { LocalChat(text); Toast(text); return; }
        text = Regex.Replace(text, "<[^>]*>", "").Replace("<", "(").Replace(">", ")");
        text = Unblur(text);
        if (secret && PickSender(to) == null)
        {
            NameInfo.Notify(to, text);
            if (defer) Defer(to, text, maxMessages);
            else Plugin.Logger.LogInfo($"Hint to {to.Data?.PlayerName} dropped (chat is closed now)");
            return;
        }
        bool targeted = secret || TargetedMode;
        var q = CommandContext ? urgent : outbox;
        foreach (var m in Pack(text, maxMessages))
        {
            if (q.Count >= (AnticheatLikely ? 16 : 40)) { Plugin.Logger.LogInfo("Chat queue full, message dropped"); break; }
            q.Enqueue((to.PlayerId, m, !targeted, UnityEngine.Time.time));
        }
    }

    const byte GhostId = 254;

    public static void SendGhosts(string text)
    {
        if (!AmHost || string.IsNullOrWhiteSpace(text)) return;
        text = Regex.Replace(text, "<[^>]*>", "").Replace("<", "(").Replace(">", ")");
        try { var h = PlayerControl.LocalPlayer; if (h != null && h.Data != null && !h.Data.IsDead) Toast(text); } catch { }
        foreach (var m in Pack(text, 1))
        {
            if (outbox.Count >= 16) { Plugin.Logger.LogInfo("Chat queue full, ghost message dropped"); break; }
            outbox.Enqueue((GhostId, m, true, UnityEngine.Time.time));
        }
    }

    static float lastWaitLog;

    static bool InRound => AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && MeetingHud.Instance == null;

    static PlayerControl AnySender(bool alive, PlayerControl prefer)
    {
        PlayerControl found = null;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.Data.Disconnected) continue;
            if (p.Data.IsDead == alive) continue;
            if (p.AmOwner) continue;
            if (p != prefer) return p;
            found = p;
        }
        return found;
    }

    public static PlayerControl PickSender(PlayerControl to)
    {
        var host = PlayerControl.LocalPlayer;
        if (host == null || host.Data == null) return null;
        bool hostDead = host.Data.IsDead;
        bool toDead = to != null && to.Data != null && to.Data.IsDead;
        bool relaxed = !AnticheatLikely;
        if (!InRound)
        {
            if (!hostDead || toDead) return host;
            return relaxed ? AnySender(true, to) : null;
        }
        if (to == null) return hostDead ? host : (relaxed ? AnySender(false, null) : null);
        if (!toDead) return null;
        if (hostDead) return host;
        return relaxed ? AnySender(false, to) : null;
    }

    static bool ChatAllowed()
    {
        var c = AmongUsClient.Instance;
        if (c == null) return false;
        if (c.GameState != InnerNetClient.GameStates.Started) return true;
        return MeetingHud.Instance != null;
    }

    public static void PumpOutbox()
    {
        if (outbox.Count == 0 && urgent.Count == 0) return;
        if (!AmHost || PlayerControl.LocalPlayer == null) { outbox.Clear(); urgent.Clear(); return; }
        if (UnityEngine.Time.time < nextSendAt) return;
        bool fromUrgent = urgent.Count > 0;
        var item = fromUrgent ? urgent.Dequeue() : outbox.Dequeue();
        var (id, text, broadcast, at) = item;
        if (UnityEngine.Time.time - at > 90f) return;
        PlayerControl to = null;
        if (!broadcast)
        {
            to = GetPlayer(id);
            if (to == null || to.Data == null || to.Data.Disconnected) { Plugin.Logger.LogWarning($"Targeted chat dropped: player id={id} gone"); return; }
        }
        PlayerControl sender;
        if (id == GhostId && broadcast)
        {
            var hp = PlayerControl.LocalPlayer;
            sender = hp.Data != null && hp.Data.IsDead ? hp : (!AnticheatLikely ? AnySender(false, null) : null);
        }
        else sender = PickSender(to);
        if (sender == null)
        {
            if (UnityEngine.Time.time - lastWaitLog > 5f)
            {
                lastWaitLog = UnityEngine.Time.time;
                var hh = PlayerControl.LocalPlayer;
                Plugin.Logger.LogInfo($"Chat waits: no suitable sender (hostDead={hh?.Data?.IsDead}, toDead={to?.Data?.IsDead}, inRound={InRound}, anticheatLikely={AnticheatLikely}, ghostOnly={id == GhostId})");
            }
            (fromUrgent ? urgent : outbox).Enqueue(item);
            return;
        }
        nextSendAt = UnityEngine.Time.time + SendGap;
        try
        {
            if (broadcast)
            {
                if (sender.AmOwner) { sender.RpcSendChat(text); return; }
                Plugin.Logger.LogInfo($"Chat as {sender.Data.PlayerName} (host cannot write now): {text}");
                var wb = AmongUsClient.Instance.StartRpcImmediately(sender.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, -1);
                wb.Write(text);
                AmongUsClient.Instance.FinishRpcImmediately(wb);
                return;
            }
            Plugin.Logger.LogInfo($"Targeted chat -> client {to.OwnerId}{(sender.AmOwner ? "" : " as " + sender.Data.PlayerName)}: {text}");
            var w = AmongUsClient.Instance.StartRpcImmediately(sender.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, to.OwnerId);
            w.Write(text);
            AmongUsClient.Instance.FinishRpcImmediately(w);
        }
        catch (Exception e) { Plugin.Logger.LogError(e); }
    }
    static void LocalChat(string text)
    {
        try
        {
            if (HudManager.Instance == null || HudManager.Instance.Chat == null) return;
            var m = typeof(ChatController).GetMethod("AddChat");
            var n = m.GetParameters().Length;
            m.Invoke(HudManager.Instance.Chat, n >= 3 ? new object[] { PlayerControl.LocalPlayer, text, true } : new object[] { PlayerControl.LocalPlayer, text });
        }
        catch (Exception e) { Plugin.Logger.LogError(e); }
    }

    public static void Toast(string text)
    {
        try
        {
            var n = HudManager.Instance != null ? HudManager.Instance.Notifier : null;
            if (n == null) return;
            var m = typeof(NotificationPopper).GetMethod("AddDisconnectMessage");
            if (m != null) m.Invoke(n, new object[] { text });
        }
        catch (Exception e) { Plugin.Logger.LogError("Toast: " + e.Message); }
    }

    public static bool EndGame()
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;
        var m = gm.GetType().GetMethod("RpcEndGame");
        if (m == null) return false;
        var ps = m.GetParameters();
        var args = new object[ps.Length];
        for (int i = 0; i < ps.Length; i++)
            args[i] = ps[i].ParameterType.IsEnum ? Enum.ToObject(ps[i].ParameterType, 3) : (ps[i].ParameterType == typeof(bool) ? false : null);
        m.Invoke(gm, args);
        return true;
    }

    public static string FriendCodeOf(PlayerControl p)
    {
        var c = p == null ? null : AmongUsClient.Instance.GetClientFromCharacter(p);
        return c == null ? "" : c.FriendCode ?? "";
    }
}

public static class Lists
{
    public static string Dir;
    public static string BanFile, DenyFile;

    public static void Init()
    {
        Dir = Path.Combine(Paths.ConfigPath, "AshesOfDisaster");
        Directory.CreateDirectory(Dir);
        BanFile = Path.Combine(Dir, "BanList.txt");
        DenyFile = Path.Combine(Dir, "DenyName.txt");
        if (!File.Exists(BanFile)) File.WriteAllText(BanFile, "# friend code по одному в строке / one friend code per line\n");
        if (!File.Exists(DenyFile)) File.WriteAllText(DenyFile, "# запрещённые слова в нике / denied nick parts, one per line\n");
        LoadRoleInfo();
    }

    public static void LoadRoleInfo()
    {
        var f = Path.Combine(Dir, "RoleInfo.txt");
        if (!File.Exists(f))
        {
            var sb = new System.Text.StringBuilder("# Id|описание (рус)|description (eng)\n");
            foreach (var r in Opts.Roles) sb.Append(r.Id).Append('|').Append(r.DescRu).Append('|').Append(r.DescEn).Append('\n');
            File.WriteAllText(f, sb.ToString());
        }
        foreach (var line in File.ReadAllLines(f))
        {
            if (line.StartsWith("#")) continue;
            var parts = line.Split('|');
            if (parts.Length < 2) continue;
            var r = Opts.Roles.Find(x => x.Id.Equals(parts[0].Trim(), StringComparison.OrdinalIgnoreCase));
            if (r == null) continue;
            if (parts[1].Contains("пока не добавлено")) continue;
            r.DescRu = parts[1].Trim();
            r.DescEn = parts.Length > 2 ? parts[2].Trim() : parts[1].Trim();
        }
    }

    static List<string> Read(string f)
    {
        var l = new List<string>();
        try { foreach (var s in File.ReadAllLines(f)) { var t = s.Trim(); if (t.Length > 0 && !t.StartsWith("#")) l.Add(t); } } catch { }
        return l;
    }

    public static bool IsBanned(string fc) => !string.IsNullOrEmpty(fc) && Read(BanFile).Exists(x => x.Equals(fc, StringComparison.OrdinalIgnoreCase));
    public static bool IsDeniedName(string name) => !string.IsNullOrEmpty(name) && Read(DenyFile).Exists(x => name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
    public static void Ban(string fc) { if (!string.IsNullOrEmpty(fc) && !IsBanned(fc)) File.AppendAllText(BanFile, fc + "\n"); }
    public static void Unban(string fc)
    {
        var all = File.ReadAllLines(BanFile);
        var res = new List<string>();
        foreach (var s in all) if (!s.Trim().Equals(fc, StringComparison.OrdinalIgnoreCase)) res.Add(s);
        File.WriteAllLines(BanFile, res);
    }
}
