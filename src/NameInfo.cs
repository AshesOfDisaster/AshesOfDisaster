using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

/// Статус над ником для игроков БЕЗ мода: роль и что осталось сделать («до щита: 2 задания», «убийство открыто» ...).
/// ЭКСПЕРИМЕНТ (настройка «Статус над ником», по умолчанию ВЫКЛ): личный SetName другим игрокам в раунде.
/// Сообщение уходит ОДНОМУ клиенту (StartRpcImmediately с его ClientId), остальные ник не меняют.
/// Раньше был перехват игрового RpcSetName; в этой версии игры он не срабатывал, и аварийная ветка рассылала ник всем — это кикало хоста.
/// Если кикнет — выключи настройку и пришли строки "NameInfo: ..." и "Disconnect via" из LogOutput.log.
/// У хоста статус всегда рисуется только локально. Выключается настройкой «Статус над ником» (вкладка «Игра»).
public static class NameInfo
{
    static readonly Dictionary<byte, string> real = new();       // настоящие ники
    static readonly Dictionary<byte, string> shown = new();      // что сейчас показано игроку (без ника)
    static readonly Dictionary<byte, float> lastSent = new();
    static readonly Queue<byte> restoreQ = new();
    static float nextTick, nextRestore;

    static bool installed, works, broken;
    static byte setNameCall = 6;

    /// Перехвата больше нет: сообщение собирается вручную и сразу уходит одному получателю .
    public static void Install(Harmony h)
    {
        try { setNameCall = (byte)Convert.ToInt32(Enum.Parse(typeof(RpcCalls), "SetName")); } catch { }
        installed = true; works = true;
        Plugin.Logger.LogInfo("NameInfo: direct private SetName (no redirect)");
    }

    // ---------------------------------------------------------------- отправка

    static byte shiftCall = 46;

    /// Ник одному клиенту так, как это делается на ванильных серверах: НЕ RPC SetName (он даёт Hacking в раунде),
    /// а сообщение с данными игрока (GameData, тип 1) внутри GameDataTo (тип 6) — хост владеет этими данными, поэтому сервер их принимает.
    /// После данных идёт Shapeshift «в себя» без анимации — клиент перерисовывает ник.
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
                // Адресный RPC SetName одному клиенту (GameDataTo): ник над головой меняется сразу, в раунде тоже
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
                // Shapeshift «в себя» без анимации: клиент перерисовывает ник над головой (без этого новый ник виден только в списке собрания)
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

    // --- Оформление ников для ВСЕХ клиентов: хост (голубым + «Хост») и игроки с тегом ---
    static float nextBadge; static int badgeIdx;
    static readonly Dictionary<int, float> hostBadgeAt = new();
    static readonly Dictionary<byte, float> undecorate = new();      // у кого сняли тег — один раз разослать обычный ник

    static string Decor(PlayerControl p, bool ru, bool plain = false)
    {
        string n = p.Data.PlayerName;
        if (plain) return n;
        string tg = GetTag(p);
        bool host = p.AmOwner && Opts.HostBadge.Value;
        if (!host && string.IsNullOrEmpty(tg)) return null;
        string res = host ? "<#00FFFF>" + n + "</color>" : n;
        if (host) res += " <size=70%><#00FFFF>[" + (ru ? "Хост" : "Host") + "]</color></size>";
        if (!string.IsNullOrEmpty(tg)) res = "<#FFD700>[" + tg + "]</color> " + res;
        return res;
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
                    string txt = Decor(subj, ru, plain: undecorate.ContainsKey(subj.PlayerId) && GetTag(subj) == null && !(subj.AmOwner && Opts.HostBadge.Value));
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

    // ---------------------------------------------------------------- логика

    // --- Всплывающие сообщения над ником: то, что в раунде нельзя написать в чат ---
    static readonly Dictionary<byte, List<(string text, float until)>> notes = new();

    public static bool CanNotify => Utils.AmHost && installed && !broken && !Utils.AnticheatLikely && Opts.NameStatus.Value
        && AmongUsClient.Instance != null && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && MeetingHud.Instance == null;

    public static void ClearNotes() => notes.Clear();

    // --- Теги: хранятся на время существования комнаты (по коду друга, иначе по нику), переживают раунды и перезаход ---
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
        hostBadgeAt.Clear();      // разослать сразу
    }
    public static string GetTag(PlayerControl p) => p != null && tags.TryGetValue(TagKey(p), out var t) ? t : null;
    public static void CheckRoom()
    {
        int id = AmongUsClient.Instance != null ? AmongUsClient.Instance.GameId : 0;
        if (id != tagGame) { tagGame = id; tags.Clear(); }
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

    static string Build(PlayerControl p)
    {
        if (p == null || p.Data == null || p.Data.Disconnected) return null;
        bool ru = Translator.IsRu(p);
        var lines = new List<string>();
        var tg = GetTag(p);
        if (!string.IsNullOrEmpty(tg)) lines.Add("<color=#FFD700>[" + tg + "]</color>");
        if (!p.Data.IsDead && RoleState.Assigned.TryGetValue(p.PlayerId, out var role))
        {
            lines.Add(role.Colored(ru));
            string st = RoleLogic.NameStatus(p, role, ru);
            if (!string.IsNullOrEmpty(st)) lines.Add(st);
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
        if (!real.TryGetValue(id, out var name)) { name = p.Data.PlayerName; real[id] = name; }
        string text = $"<size=55%>{info}</size>\r\n{name}";
        if (p.AmOwner) { SetLocal(p, text); shown[id] = info; return; }
        text = text.Replace("color=", "");      //: сервер не любит «color=» в нике; <#FF0000> работает так же
        if (!EnsureWorks(p)) return;
        if (SendPrivate(p, text)) { shown[id] = info; lastSent[id] = Time.time; }
    }

    public static void QueueRestore(byte id) { if (shown.ContainsKey(id) && !restoreQ.Contains(id)) restoreQ.Enqueue(id); }
    public static void QueueRestoreAll() { foreach (var id in shown.Keys.ToList()) QueueRestore(id); }
    public static void OnMeetingStart() => QueueRestoreAll();      // в списке голосования ники должны быть обычными

    static void PumpRestore()
    {
        if (restoreQ.Count == 0 || Time.time < nextRestore) return;
        nextRestore = Time.time + 0.25f;
        byte id = restoreQ.Dequeue();
        shown.Remove(id);
        var p = Utils.GetPlayer(id);
        if (p == null || p.Data == null || !real.TryGetValue(id, out var name)) return;
        if (p.AmOwner) SetLocal(p, name);
        else if (works && !p.Data.Disconnected) SendPrivate(p, name);
    }

    /// Каждый кадр из ModGui.Update (только хост)
    public static void Tick()
    {
        CheckRoom();
        if (!Utils.AmHost) return;
        if (Shroomer.Active) return;      // во время грибов ники и цвета не трогаем
        PumpRestore();
        HostBadgeTick();

        if (Utils.AnticheatLikely || !Opts.NameStatus.Value || broken) { if (shown.Count > 0) QueueRestoreAll(); return; }
        var c = AmongUsClient.Instance;
        if (c == null || c.GameState != InnerNetClient.GameStates.Started)
        {
            if (shown.Count > 0) QueueRestoreAll();      // игра кончилась — вернуть ники
            return;
        }
        if (MeetingHud.Instance != null) return;
        if (Time.time - RoleLogic.VigStart < 15f) return;      // не в заставке и первые секунды игры
        if (Time.time < nextTick) return;
        nextTick = Time.time + 1f;

        int budget = 2;      // не больше двух сетевых обновлений за секунду
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null) continue;
            string info = Build(p);
            bool has = shown.TryGetValue(p.PlayerId, out var cur);
            if (info == null) { if (has) QueueRestore(p.PlayerId); continue; }
            if (has && cur == info) { if (p.AmOwner) Apply(p, info); continue; }     // у хоста рисуем заново: игра могла сбросить текст
            if (!p.AmOwner)
            {
                if (budget <= 0) continue;
                if (lastSent.TryGetValue(p.PlayerId, out var t) && Time.time - t < 1.5f) continue;
                budget--;
            }
            Apply(p, info);
        }
    }
}

/// При выкидывании на собрании в чат пишется роль выброшенного
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
            if (ex == null) return;      // ничья или пропуск — никого не выбросили
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
