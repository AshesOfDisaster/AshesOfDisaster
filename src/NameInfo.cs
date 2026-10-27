using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class NameInfo
{
    static readonly Dictionary<byte, string> real = new();
    static readonly Dictionary<byte, string> shown = new();
    static readonly Dictionary<byte, float> lastSent = new();
    static readonly Queue<byte> restoreQ = new();
    static float nextTick, nextRestore;

    static bool installed, works, broken;
    static byte setNameCall = 6;

    public static void Install(Harmony h)
    {
        try { setNameCall = (byte)Convert.ToInt32(Enum.Parse(typeof(RpcCalls), "SetName")); } catch { }
        installed = true; works = true;
        Plugin.Logger.LogInfo("NameInfo: direct private SetName (no redirect)");
    }

    static byte shiftCall = 46;

    static bool SendPrivate(PlayerControl p, string name)
    {
        if (p == null || p.Data == null || p.AmOwner || p.Data.Disconnected) return false;
        string orig = null; MessageWriter w = null; bool swapped = false;
        try
        {
            try { shiftCall = (byte)Convert.ToInt32(Enum.Parse(typeof(RpcCalls), "Shapeshift")); } catch { }
            Plugin.Logger.LogInfo($"NameInfo: private name -> {p.Data.PlayerName} (client {p.OwnerId})");
            orig = p.Data.PlayerName;
            w = MessageWriter.Get(SendOption.Reliable);
            if (Opts.NameViaRpc == null || Opts.NameViaRpc.Value)
            {
                w.StartMessage(6);
                w.Write(AmongUsClient.Instance.GameId);
                w.WritePacked(p.OwnerId);
                w.StartMessage(2);
                w.WritePacked(p.NetId);
                w.Write(setNameCall);
                w.Write(p.Data.NetId);
                w.Write(name);
                w.Write(false);
                w.EndMessage();
                w.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(w);
                return true;
            }
            p.Data.PlayerName = name; swapped = true;
            w.StartMessage(6);
            w.Write(AmongUsClient.Instance.GameId);
            w.WritePacked(p.OwnerId);
            w.StartMessage(1);
            w.WritePacked(p.Data.NetId);
            p.Data.Serialize(w, false);
            w.EndMessage();
            if (Opts.NameRefresh == null || Opts.NameRefresh.Value)
            {
                w.StartMessage(2);
                w.WritePacked(p.NetId);
                w.Write(shiftCall);
                w.WritePacked(p.NetId);
                w.Write(false);
                w.EndMessage();
            }
            w.EndMessage();
            p.Data.PlayerName = orig; swapped = false;
            AmongUsClient.Instance.SendOrDisconnect(w);
            return true;
        }
        catch (Exception e) { Plugin.Logger.LogError("NameInfo.SendPrivate: " + e.GetBaseException().Message); return false; }
        finally
        {
            try { if (swapped && orig != null) p.Data.PlayerName = orig; } catch { }
            try { if (w != null) w.Recycle(); } catch { }
        }
    }

    static float nextBadge; static int badgeIdx;
    static readonly Dictionary<int, float> hostBadgeAt = new();
    static readonly Dictionary<byte, float> undecorate = new();

    static string Decor(PlayerControl p, bool ru, bool plain = false)
    {
        string n = p.Data.PlayerName;
        if (plain) return n;
        string tg = GetTag(p);
        bool host = p.AmOwner && Opts.HostBadge.Value;
        int fs = Fat.NameSize(p);
        bool big = fs > 105;
        string horse = Extra33.HorseArt(p);
        if (!host && string.IsNullOrEmpty(tg) && !big && horse == null) return null;
        string res = host ? "<#00FFFF>" + n + "</color>" : n;
        if (big) res = "<size=" + fs + "%>" + res + "</size>";
        if (host) res += " <size=70%><#00FFFF>[" + (ru ? "Хост" : "Host") + "]</color></size>";
        if (horse != null) res += "\n" + horse;
        if (!string.IsNullOrEmpty(tg)) res = "<#FFD700>[" + tg + "]</color> " + res;
        return res;
    }

    public static void RefreshDecor(byte id, bool remove = false)
    {
        if (remove) undecorate[id] = Time.time; else undecorate.Remove(id);
        hostBadgeAt.Clear();
    }

    static void HostBadgeTick()
    {
        if (Shroomer.Active) return;
        if (Utils.AnticheatLikely || broken || !installed) return;
        if (Time.time < nextBadge) return;
        nextBadge = Time.time + 0.4f;
        var host = PlayerControl.LocalPlayer;
        if (host == null || host.Data == null) return;
        try
        {
            if (!shown.ContainsKey(host.PlayerId)) { var d = Decor(host, Translator.GameIsRu); if (d != null) SetLocal(host, d); }
            var all = PlayerControl.AllPlayerControls.ToArray().Where(x => x != null && x.Data != null && !x.Data.Disconnected).ToList();
            var clients = all.Where(x => !x.AmOwner).ToList();
            if (clients.Count == 0) return;
            foreach (var k in undecorate.Where(kv => Time.time - kv.Value > 30f).Select(kv => kv.Key).ToList()) undecorate.Remove(k);
            var t = clients[badgeIdx++ % clients.Count];
            if (hostBadgeAt.TryGetValue(t.OwnerId, out var last) && Time.time - last < 6f) return;
            hostBadgeAt[t.OwnerId] = Time.time;
            bool ru = Translator.IsRu(t);
            var w = MessageWriter.Get(SendOption.Reliable);
            int n = 0;
            try
            {
                w.StartMessage(6);
                w.Write(AmongUsClient.Instance.GameId);
                w.WritePacked(t.OwnerId);
                foreach (var subj in all)
                {
                    string txt = Decor(subj, ru, plain: undecorate.ContainsKey(subj.PlayerId) && GetTag(subj) == null && !(subj.AmOwner && Opts.HostBadge.Value) && Fat.NameSize(subj) <= 105);
                    if (txt == null) continue;
                    w.StartMessage(2);
                    w.WritePacked(subj.NetId);
                    w.Write(setNameCall);
                    w.Write(subj.Data.NetId);
                    w.Write(txt);
                    w.Write(false);
                    w.EndMessage();
                    n++;
                }
                w.EndMessage();
                if (n > 0) AmongUsClient.Instance.SendOrDisconnect(w);
            }
            finally { w.Recycle(); }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NameInfo.Decor: " + e.Message); }
    }

    static bool EnsureWorks(PlayerControl p) => installed && !broken;

    static void SetLocal(PlayerControl p, string text)
    {
        try { var t = p.cosmetics != null ? p.cosmetics.nameText : null; if (t != null) t.text = text; }
        catch (Exception e) { Plugin.Logger.LogError("NameInfo.SetLocal: " + e.Message); }
    }

    static readonly Dictionary<byte, List<(string text, float until)>> notes = new();

    public static bool CanNotify => Utils.AmHost && installed && !broken && !Utils.AnticheatLikely && Opts.NameStatus.Value
        && AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && MeetingHud.Instance == null;

    public static void ClearNotes() => notes.Clear();

    static readonly Dictionary<string, string> tags = new();
    static int tagGame;
    static string TagKey(PlayerControl p)
    {
        string fc = null;
        try { fc = p.Data?.FriendCode; } catch { }
        if (string.IsNullOrEmpty(fc)) try { fc = AmongUsClient.Instance.GetClient(p.OwnerId)?.FriendCode; } catch { }
        return !string.IsNullOrEmpty(fc) ? fc : "n:" + p.Data?.PlayerName;
    }
    public static void SetTag(PlayerControl p, string tag)
    {
        if (p == null) return;
        if (string.IsNullOrWhiteSpace(tag)) { if (tags.Remove(TagKey(p))) undecorate[p.PlayerId] = Time.time; }
        else { tags[TagKey(p)] = tag.Trim(); undecorate.Remove(p.PlayerId); }
        hostBadgeAt.Clear();
    }
    public static string GetTag(PlayerControl p) => p != null && tags.TryGetValue(TagKey(p), out var t) ? t : null;
    public static void CheckRoom()
    {
        int id = AmongUsClient.Instance != null ? AmongUsClient.Instance.GameId : 0;
        if (id != tagGame) { tagGame = id; tags.Clear(); real.Clear(); shown.Clear(); lastSent.Clear(); restoreQ.Clear(); notes.Clear(); }
    }

    public static void Notify(PlayerControl p, string text, float seconds = 8f)
    {
        try
        {
            if (p == null || string.IsNullOrWhiteSpace(text) || !CanNotify) return;
            if (!notes.TryGetValue(p.PlayerId, out var list)) notes[p.PlayerId] = list = new();
            list.RemoveAll(n => n.text == text);
            list.Add((text.Trim(), Time.time + seconds));
            while (list.Count > 3) list.RemoveAt(0);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NameInfo.Notify: " + e.Message); }
    }

    public static void Refresh() { nextTick = 0f; burstUntil = Time.time + 8f; lastSent.Clear(); }
    static float burstUntil;

    static string ShortDesc(RoleDef role, bool ru)
    {
        string d = ru ? role.DescRu : role.DescEn;
        if (string.IsNullOrEmpty(d)) return null;
        int br = d.IndexOf('[');
        if (br > 0) d = d.Substring(0, br);
        var parts = d.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
        string res = "";
        for (int i = parts.Length > 1 ? 1 : 0; i < parts.Length; i++)
        {
            string sentence = parts[i].Trim().TrimEnd('.');
            if (sentence.Length == 0) continue;
            if (res.Length > 0 && res.Length + sentence.Length > 85) break;
            res += (res.Length > 0 ? ". " : "") + sentence;
            if (res.Length >= 50) break;
        }
        if (res.Length > 95) res = res.Substring(0, 92) + "...";
        return res;
    }

    static string Build(PlayerControl p)
    {
        if (p == null || p.Data == null || p.Data.Disconnected) return null;
        bool ru = Translator.IsRu(p);
        var lines = new List<string>();
        var tg = GetTag(p);
        if (!string.IsNullOrEmpty(tg)) lines.Add("<color=#FFD700>[" + tg + "]</color>");
        if (!p.Data.IsDead && RoleState.Assigned.TryGetValue(p.PlayerId, out var role))
        {
            lines.Add(RoleGuide.Line(role, ru));
            lines.Add("<color=#DDDDDD>" + (ru ? "Цель: " : "Goal: ") + RoleGuide.Goal(role, ru) + "</color>");
            string st = RoleLogic.NameStatus(p, role, ru);
            string hint = RoleHints.Get(role.Id, ru);
            string now = !string.IsNullOrEmpty(st) ? st : hint;
            if (!string.IsNullOrEmpty(now)) lines.Add("<color=#9FD8FF>" + (ru ? "Сейчас: " : "Now: ") + "</color>" + now);
        }
        if (!p.Data.IsDead && RoleLogic.Webs.Any(w => w.Trapped == p.PlayerId))
            lines.Add(ru ? "<color=#FF5555>Ты в паутине! Нужна помощь</color>" : "<color=#FF5555>You are in a web! Need help</color>");
        if (notes.TryGetValue(p.PlayerId, out var nl))
        {
            nl.RemoveAll(n => Time.time >= n.until);
            foreach (var n in nl) lines.Add("<color=#FFE066>" + n.text + "</color>");
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    static void Apply(PlayerControl p, string info)
    {
        byte id = p.PlayerId;
        string name = p.Data.PlayerName;
        real[id] = name;
        string text = $"<size=55%>{info}</size>\r\n{name}";
        if (p.AmOwner) { SetLocal(p, text); shown[id] = info; return; }
        text = text.Replace("color=", "");
        if (!EnsureWorks(p)) return;
        if (SendPrivate(p, text)) { shown[id] = info; lastSent[id] = Time.time; }
    }

    public static void QueueRestore(byte id) { if (shown.ContainsKey(id) && !restoreQ.Contains(id)) restoreQ.Enqueue(id); }
    public static void QueueRestoreAll() { foreach (var id in shown.Keys.ToList()) QueueRestore(id); }
    public static void OnMeetingStart() => QueueRestoreAll();

    static void PumpRestore()
    {
        if (restoreQ.Count == 0 || Time.time < nextRestore) return;
        nextRestore = Time.time + 0.25f;
        byte id = restoreQ.Dequeue();
        shown.Remove(id);
        var p = Utils.GetPlayer(id);
        if (p == null || p.Data == null) return;
        string name = p.Data.PlayerName;
        if (p.AmOwner) SetLocal(p, name);
        else if (works && !p.Data.Disconnected) SendPrivate(p, name);
    }

    public static void Tick()
    {
        CheckRoom();
        if (!Utils.AmHost) return;
        if (Shroomer.Active) return;
        PumpRestore();
        HostBadgeTick();

        if (Utils.AnticheatLikely || !Opts.NameStatus.Value || broken) { if (shown.Count > 0) QueueRestoreAll(); return; }
        var c = AmongUsClient.Instance;
        if (c == null || c.GameState != InnerNetClient.GameStates.Started)
        {
            if (shown.Count > 0) QueueRestoreAll();
            return;
        }
        if (MeetingHud.Instance != null) return;
        if (Time.time - RoleLogic.VigStart < 15f) return;
        if (Time.time < nextTick) return;
        bool burst = Time.time < burstUntil;
        nextTick = Time.time + (burst ? 0.3f : 1f);

        int budget = burst ? 6 : 2;
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null) continue;
            string info = Build(p);
            bool has = shown.TryGetValue(p.PlayerId, out var cur);
            if (info == null) { if (has) QueueRestore(p.PlayerId); continue; }
            if (has && cur == info) { if (p.AmOwner) Apply(p, info); continue; }
            if (!p.AmOwner)
            {
                if (budget <= 0) continue;
                if (lastSent.TryGetValue(p.PlayerId, out var t) && Time.time - t < (burst ? 0.3f : 1.5f)) continue;
                budget--;
            }
            Apply(p, info);
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), "VotingComplete")]
public static class ExileRolePatch
{
    static float last;

    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(MeetingHud), "VotingComplete");

    public static void Postfix(object[] __args)
    {
        try
        {
            if (!Utils.AmHost || !Opts.RevealOnExile.Value || __args == null) return;
            if (Time.time - last < 2f) return;
            NetworkedPlayerInfo ex = null;
            foreach (var a in __args) if (a is NetworkedPlayerInfo ni) { ex = ni; break; }
            if (ex == null) return;
            last = Time.time;
            bool ru = Translator.GameIsRu;
            string who = ex.PlayerName ?? "?";
            string label = RoleLogic.RoleLabel(ex.PlayerId, ex.RoleType, ru);
            string text = ru ? $"{who} выброшен. Его роль: {label}." : $"{who} was ejected. Role: {label}.";
            Plugin.Logger.LogInfo("Exile reveal: " + text);
            Utils.Announce(text);
        }
        catch (Exception e) { Plugin.Logger.LogError("ExileRole: " + e); }
    }
}
