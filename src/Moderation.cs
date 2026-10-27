using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AshesOfDisaster;

public static class Anticheat
{
    public static BoolOpt On, Kick, Ban, Report;
    public static ConfigEntry<string> Webhook;

    static readonly HashSet<byte> vanilla = new();
    static readonly HashSet<byte> hostOnly = new();
    static readonly Dictionary<byte, string> cheats = new();
    static readonly Dictionary<string, string> flagged = new();
    static readonly Dictionary<string, float> seen = new();
    static bool loaded;

    public static void Define(ConfigFile c)
    {
        On = new BoolOpt("Anticheat", OptTab.System, false, c);
        On.NameRu = "Античит: искать читеров"; On.NameEn = "Anticheat: look for cheaters";
        Kick = new BoolOpt("AnticheatKick", OptTab.System, true, c);
        Kick.NameRu = "Античит: кикать сразу"; Kick.NameEn = "Anticheat: kick at once";
        Ban = new BoolOpt("AnticheatBan", OptTab.System, false, c);
        Ban.NameRu = "Античит: ещё и банить (friend code в бан-лист)"; Ban.NameEn = "Anticheat: also ban (friend code goes to the ban list)";
        Report = new BoolOpt("AnticheatReport", OptTab.System, false, c);
        Report.NameRu = "Античит: слать жалобу в Discord"; Report.NameEn = "Anticheat: send a report to Discord";
        foreach (var o in new Opt[] { On, Kick, Ban, Report }) { o.GroupRu = "Античит"; o.GroupEn = "Anticheat"; }
        Webhook = c.Bind("Moderation", "DiscordWebhook", "", "Вебхук дискорда для жалоб");
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        foreach (var v in Enum.GetValues(typeof(RpcCalls))) vanilla.Add((byte)Convert.ToInt32(v));
        foreach (var n in new[] { "MurderPlayer", "StartMeeting", "Exiled" })
        {
            try { hostOnly.Add((byte)Convert.ToInt32(Enum.Parse(typeof(RpcCalls), n))); }
            catch { }
        }
        try
        {
            string file = Path.Combine(Lists.Dir, "CheatRpc.txt");
            if (!File.Exists(file))
                File.WriteAllText(file, "# номер RPC|название чита (номер смотри в логе или в /ac)\n# RPC id|cheat name (the id is in the log or in /ac)\n");
            foreach (var line in File.ReadAllLines(file))
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;
                var parts = t.Split('|');
                if (parts.Length >= 2 && byte.TryParse(parts[0].Trim(), out var id)) cheats[id] = parts[1].Trim();
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("CheatRpc.txt: " + e.Message); }
    }

    static string KeyOf(PlayerControl p)
    {
        string fc = Utils.FriendCodeOf(p);
        return fc.Length > 0 ? fc : "id" + p.PlayerId;
    }

    public static string Flagged(PlayerControl p) => p == null ? null : flagged.TryGetValue(KeyOf(p), out var why) ? why : null;

    public static string Summary(bool ru)
    {
        var sb = new StringBuilder();
        sb.Append(On != null && On.Value ? (ru ? "Античит включён" : "Anticheat is on") : (ru ? "Античит выключен (включается в настройках лобби, вкладка Система)" : "Anticheat is off (turn it on in the lobby settings, System tab)"));
        if (flagged.Count == 0) sb.Append('\n').Append(ru ? "Нарушителей нет" : "No cheaters found");
        foreach (var kv in flagged) sb.Append('\n').Append(kv.Key).Append(" - ").Append(kv.Value);
        return sb.ToString();
    }

    public static void Check(PlayerControl p, byte callId)
    {
        if (On == null || !On.Value || !Utils.AmHost || p == null || p.AmOwner || p.Data == null) return;
        Load();
        string why = null;
        if (cheats.TryGetValue(callId, out var cheat)) why = $"чит {cheat} (RPC {callId})";
        else if (hostOnly.Contains(callId)) why = $"подделка действия хоста (RPC {callId})";
        else if (!vanilla.Contains(callId)) why = $"неизвестный RPC {callId}";
        if (why != null) Flag(p, why);
    }

    static void Flag(PlayerControl p, string why)
    {
        string key = KeyOf(p);
        if (seen.Count > 300) seen.Clear();
        if (seen.TryGetValue(key + "*", out var lastAny) && Time.time - lastAny < 10f) return;
        if (seen.TryGetValue(key + why, out var lastTime) && Time.time - lastTime < 60f) return;
        seen[key + "*"] = Time.time;
        seen[key + why] = Time.time;
        flagged[key] = why;
        var cl = AmongUsClient.Instance.GetClientFromCharacter(p);
        string name = p.Data.PlayerName;
        string fc = cl == null ? "" : cl.FriendCode ?? "";
        Plugin.Logger.LogWarning($"Anticheat: {name} ({fc}) - {why}");
        Utils.Toast($"Античит: {name} - {why}");
        if (Report.Value) Reports.Send(name, fc, why);
        if (Ban.Value && fc.Length > 0) Lists.Ban(fc);
        if (Kick.Value && cl != null) AmongUsClient.Instance.KickPlayer(cl.Id, Ban.Value);
    }
}

[HarmonyPatch(typeof(PlayerControl), "HandleRpc")]
public static class AnticheatPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId)
    {
        try { Anticheat.Check(__instance, callId); }
        catch (Exception e) { Plugin.Logger.LogWarning("Anticheat: " + e.Message); }
    }
}

public static class Reports
{
    static readonly HttpClient http = MakeHttp();
    static HttpClient MakeHttp()
    {
        var h = new HttpClient();
        try { h.Timeout = TimeSpan.FromSeconds(15); h.DefaultRequestHeaders.UserAgent.ParseAdd("AshesOfDisaster/1.0 (Among Us mod)"); } catch { }
        return h;
    }

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");

    static readonly Dictionary<string, float> sentAt = new();
    static readonly Queue<float> recent = new();
    public static string Blocked = "";

    static string Cut(string s, int n) => s == null ? "" : s.Length > n ? s.Substring(0, n) : s;

    static bool Allowed(string key)
    {
        float now = Time.realtimeSinceStartup;
        if (sentAt.TryGetValue(key, out var t) && now - t < 600f) { Blocked = "Такая жалоба уже отправлена, подожди"; return false; }
        while (recent.Count > 0 && now - recent.Peek() > 60f) recent.Dequeue();
        if (recent.Count >= 5) { Blocked = "Слишком много жалоб, подожди минуту"; return false; }
        if (sentAt.Count > 200) sentAt.Clear();
        sentAt[key] = now;
        recent.Enqueue(now);
        return true;
    }

    public static bool Send(string name, string fc, string why)
    {
        Blocked = "";
        string url = Anticheat.Webhook == null ? "" : Anticheat.Webhook.Value.Trim();
        if (url.Length == 0) url = Secrets.Reports;
        if (url.Length == 0) return false;
        name = Cut(name, 40); fc = Cut(fc, 40); why = Cut(why, 200);
        if (!Allowed((fc.Length > 0 ? fc : name) + "|" + why)) return false;
        var me = PlayerControl.LocalPlayer;
        string hostName = me == null || me.Data == null ? "?" : Cut(me.Data.PlayerName, 40);
        string text = $"Жалоба от хоста {hostName}\nИгрок: {name}\nFriend code: {(fc.Length > 0 ? fc : "нет")}\nПричина: {why}";
        string body = "{\"content\":\"" + Esc(Cut(text, 1500)) + "\",\"allowed_mentions\":{\"parse\":[]}}";
        Task.Run(async () =>
        {
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "application/json");
                var res = await http.PostAsync(url, content);
                if (res.IsSuccessStatusCode) Plugin.Logger.LogInfo("Report sent, answer " + (int)res.StatusCode);
                else
                {
                    string answer = "";
                    try { answer = await res.Content.ReadAsStringAsync(); } catch { }
                    Plugin.Logger.LogWarning("Report NOT delivered, Discord answered " + (int)res.StatusCode + ": " + Cut(answer, 200));
                }
            }
            catch (Exception e) { Plugin.Logger.LogWarning("Report failed: " + e.GetBaseException().Message); }
        });
        return true;
    }
}

public static class ModPanel
{
    static bool open;
    static byte confirmId = 255;
    static float confirmUntil;
    static string notice = "";
    static float noticeUntil;

    public static void Toggle()
    {
        if (Utils.AmHost) open = !open;
    }

    static void Tell(string text)
    {
        notice = text;
        noticeUntil = Time.time + 4f;
    }

    public static bool Block(PlayerControl p, string why)
    {
        var cl = p == null ? null : AmongUsClient.Instance.GetClientFromCharacter(p);
        if (cl == null || p.AmOwner || p.Data == null) return false;
        string fc = cl.FriendCode ?? "";
        if (fc.Length > 0) Lists.Ban(fc);
        if (Anticheat.Report != null && Anticheat.Report.Value) Reports.Send(p.Data.PlayerName, fc, why);
        AmongUsClient.Instance.KickPlayer(cl.Id, true);
        return true;
    }

    public static string Complain(PlayerControl p, string why)
    {
        if (p == null || p.Data == null) return "Игрок не найден";
        string reason = why ?? Anticheat.Flagged(p) ?? "жалоба хоста";
        return Reports.Send(p.Data.PlayerName, Utils.FriendCodeOf(p), reason)
            ? "Жалоба отправлена в Discord"
            : Reports.Blocked.Length > 0 ? Reports.Blocked
            : "Вебхук не указан: строка DiscordWebhook в секции Moderation (папка BepInEx/config)";
    }

    static bool cOpen;
    static string cName = "", cFc = "", cReason = "", banFc = "";

    static void OpenComplaint(PlayerControl p)
    {
        cName = p.Data.PlayerName;
        cFc = Utils.FriendCodeOf(p);
        cReason = Anticheat.Flagged(p) ?? "";
        cOpen = true;
    }

    static string BanCode(string raw)
    {
        string fc = raw.Trim();
        if (fc.Length < 5 || !fc.Contains("#")) return "Код дружбы выглядит так: имя#1234";
        Lists.Ban(fc);
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.AmOwner) continue;
            if (Utils.FriendCodeOf(p).Equals(fc, StringComparison.OrdinalIgnoreCase)) Block(p, "бан по коду дружбы");
        }
        return "Код " + fc + " в бане";
    }

    public static void Draw()
    {
        if (!open || !Utils.AmHost || AmongUsClient.Instance == null) return;
        var list = new List<PlayerControl>();
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p != null && p.Data != null && !p.Data.Disconnected && !p.AmOwner) list.Add(p);
        float listH = Mathf.Max(1, list.Count) * 32f;
        float h = 74f + listH + 70f;
        GUI.Box(new Rect(30f, 60f, 660f, h), "Игроки в комнате (F8 - закрыть)");
        if (list.Count == 0) GUI.Label(new Rect(46f, 92f, 500f, 28f), "Кроме тебя в комнате никого нет");
        for (int i = 0; i < list.Count; i++)
        {
            var pl = list[i];
            float y = 92f + i * 32f;
            string fc = Utils.FriendCodeOf(pl);
            string cheat = Anticheat.Flagged(pl);
            string line = pl.PlayerId + "  " + pl.Data.PlayerName + (fc.Length > 0 ? "  " + fc : "") + (cheat != null ? "  [ЧИТ: " + cheat + "]" : "");
            GUI.Label(new Rect(46f, y, 400f, 28f), line);
            if (GUI.Button(new Rect(456f, y, 100f, 28f), "Жалоба")) OpenComplaint(pl);
            bool sure = confirmId == pl.PlayerId && Time.time < confirmUntil;
            if (GUI.Button(new Rect(564f, y, 110f, 28f), sure ? "Точно?" : "Блок"))
            {
                if (sure)
                {
                    confirmId = 255;
                    Tell(Block(pl, cheat ?? "блок хостом") ? "Игрок заблокирован" : "Не получилось");
                }
                else
                {
                    confirmId = pl.PlayerId;
                    confirmUntil = Time.time + 3f;
                }
            }
        }
        float by = 60f + 74f + listH;
        GUI.Label(new Rect(46f, by, 200f, 28f), "Бан по коду дружбы:");
        banFc = GUI.TextField(new Rect(236f, by, 250f, 28f), banFc, 40);
        if (GUI.Button(new Rect(494f, by, 90f, 28f), "Забанить")) Tell(BanCode(banFc));
        if (GUI.Button(new Rect(590f, by, 90f, 28f), "Снять бан"))
        {
            string code = banFc.Trim();
            if (code.Length > 0) { Lists.Unban(code); Tell("Код " + code + " убран из бана"); }
        }
        if (Time.time < noticeUntil) GUI.Label(new Rect(46f, by + 34f, 620f, 26f), notice);
        if (!cOpen) return;
        GUI.Box(new Rect(710f, 60f, 440f, 270f), "Жалоба на игрока");
        GUI.Label(new Rect(726f, 92f, 400f, 24f), "Ник: " + cName);
        GUI.Label(new Rect(726f, 120f, 400f, 24f), "Код дружбы:");
        cFc = GUI.TextField(new Rect(726f, 146f, 400f, 28f), cFc, 40);
        GUI.Label(new Rect(726f, 180f, 400f, 24f), "Причина:");
        cReason = GUI.TextField(new Rect(726f, 206f, 400f, 28f), cReason, 200);
        if (GUI.Button(new Rect(726f, 250f, 190f, 32f), "Отправить"))
        {
            if (cFc.Trim().Length == 0 || cReason.Trim().Length == 0) Tell("Впиши код дружбы и причину");
            else if (Reports.Send(cName, cFc.Trim(), cReason.Trim())) { Tell("Жалоба отправлена в Discord"); cOpen = false; }
            else Tell(Reports.Blocked.Length > 0 ? Reports.Blocked : "Вебхук не указан в конфиге");
        }
        if (GUI.Button(new Rect(936f, 250f, 190f, 32f), "Отмена")) cOpen = false;
    }
}
