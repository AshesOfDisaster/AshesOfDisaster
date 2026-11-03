using InnerNet;
using UnityEngine;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace AshesOfDisaster;

public static class RoomCode
{
    static readonly HttpClient http = Make();
    static int sentFor;
    static bool wasOn;
    static readonly System.Collections.Generic.Dictionary<string, float> sentCodes = new();
    static readonly System.Collections.Generic.Queue<float> hourly = new();
    static float lastSend = -999f;
    static string lastCode = "";
    static string msgId, msgUrl, msgCode, msgHost, lastText;
    static int msgGame;
    static float nextUpd, waitSince = -1f;
    const float SameCodeBlock = 3600f, MinGap = 30f;
    const int MaxPerHour = 6;

    public static bool Active => Opts.ShareRoom != null && Opts.ShareRoom.Value && Utils.AmHost;

    // null - можно отправлять, иначе причина отказа
    static string Refuse(string code, bool ru)
    {
        float now = Time.time;
        while (hourly.Count > 0 && now - hourly.Peek() > 3600f) hourly.Dequeue();
        if (sentCodes.TryGetValue(code, out var t) && now - t < SameCodeBlock)
            return ru ? $"Код {code} уже отправлен ({Mathf.CeilToInt((now - t) / 60f)} мин назад), повторно нельзя." : $"Code {code} was already sent ({Mathf.CeilToInt((now - t) / 60f)} min ago), no repeats.";
        if (now - lastSend < MinGap)
            return ru ? $"Защита от спама: подожди ещё {Mathf.CeilToInt(MinGap - (now - lastSend))} с." : $"Spam protection: wait {Mathf.CeilToInt(MinGap - (now - lastSend))} s more.";
        if (hourly.Count >= MaxPerHour)
            return ru ? $"Защита от спама: не больше {MaxPerHour} отправок в час." : $"Spam protection: at most {MaxPerHour} sends per hour.";
        return null;
    }

    public static string StatusLine(bool ru)
    {
        bool on = Opts.ShareRoom != null && Opts.ShareRoom.Value;
        string state = on ? (ru ? "<color=#69F0AE>ВКЛЮЧЕНА</color>" : "<color=#69F0AE>ON</color>") : (ru ? "<color=#FF5555>ВЫКЛЮЧЕНА</color>" : "<color=#FF5555>OFF</color>");
        bool hasUrl = Secrets.RoomCodes.Length > 0 || (Opts.RoomShareUrl != null && Opts.RoomShareUrl.Value.Trim().Length > 0);
        string last = lastCode.Length == 0 ? (ru ? "ещё ничего" : "nothing yet") : lastCode + (ru ? $" ({Mathf.CeilToInt((Time.time - lastSend) / 60f)} мин назад)" : $" ({Mathf.CeilToInt((Time.time - lastSend) / 60f)} min ago)");
        float now = Time.time;
        int left = MaxPerHour - hourly.Count(x => now - x <= 3600f);
        return ru
            ? $"Отправка кода комнаты в Discord: {state}. Адрес вебхука: {(hasUrl ? "есть" : "<color=#FF5555>не указан</color>")}. Последний отправленный: {last}. Защита: один код раз в час, пауза {MinGap:0} с, осталось отправок в этот час: {left}."
            : $"Room code sharing to Discord: {state}. Webhook address: {(hasUrl ? "set" : "<color=#FF5555>missing</color>")}. Last sent: {last}. Protection: one code per hour, {MinGap:0} s gap, sends left this hour: {left}.";
    }

    static HttpClient Make()
    {
        var h = new HttpClient();
        try { h.Timeout = TimeSpan.FromSeconds(15); h.DefaultRequestHeaders.UserAgent.ParseAdd("AshesOfDisaster/1.0 (Among Us mod)"); } catch { }
        return h;
    }

    static string Esc(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s ?? "")
        {
            if (ch == '"' || ch == '\\') sb.Append('\\').Append(ch);
            else if (ch == '\n') sb.Append("\\n");
            else if (ch < ' ') sb.Append(' ');
            else sb.Append(ch);
        }
        return sb.ToString();
    }

    static string StatusText(bool closed)
    {
        var c = AmongUsClient.Instance;
        string st = closed ? "🔴 Закрыта" : (c != null && c.GameState == InnerNet.InnerNetClient.GameStates.Started ? "🟡 Идёт игра" : "🟢 Открыта, лобби");
        int players = closed ? 0 : PlayerControl.AllPlayerControls.Count;
        return $"Комната: **{msgCode}**\nСтатус: {st}\nХост: {msgHost}\nИгроков: {(closed ? "-" : players.ToString())}\nМод Ashes of Disaster {Plugin.Version}";
    }

    static void PatchStatus(bool closed)
    {
        if (msgId == null || msgUrl == null) return;
        string text = StatusText(closed);
        if (text == lastText) return;
        lastText = text;
        string body = "{\"content\":\"" + Esc(text) + "\",\"allowed_mentions\":{\"parse\":[]}}";
        string url = msgUrl + "/messages/" + msgId;
        Task.Run(async () =>
        {
            try
            {
                using var req = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                var res = await http.SendAsync(req);
                if (!res.IsSuccessStatusCode) Plugin.Logger.LogWarning("RoomCode status update answered " + (int)res.StatusCode);
            }
            catch (Exception e) { Plugin.Logger.LogWarning("RoomCode status update failed: " + e.GetBaseException().Message); }
        });
    }

    public static void Tick()
    {
        try
        {
            if (Opts.ShareRoom == null) return;
            bool on = Opts.ShareRoom.Value;
            if (on && !wasOn) sentFor = 0;
            wasOn = on;
            if (msgId != null)
            {
                var c0 = AmongUsClient.Instance;
                bool open = on && Utils.AmHost && c0 != null && c0.NetworkMode == NetworkModes.OnlineGame && c0.GameId == msgGame
                    && (c0.GameState == InnerNet.InnerNetClient.GameStates.Joined || c0.GameState == InnerNet.InnerNetClient.GameStates.Started);
                if (!open) { PatchStatus(true); msgId = null; }
                else if (Time.time >= nextUpd) { nextUpd = Time.time + 30f; PatchStatus(false); }
            }
            if (!on || !Utils.AmHost) return;
            var c = AmongUsClient.Instance;
            if (c == null || c.NetworkMode != NetworkModes.OnlineGame || c.GameId == 0) return;
            if (c.GameState != InnerNet.InnerNetClient.GameStates.Joined && c.GameState != InnerNet.InnerNetClient.GameStates.Started) return;
            int id = c.GameId;
            if (id == sentFor) return;
            {
                var me0 = PlayerControl.LocalPlayer;
                string hn = me0 == null || me0.Data == null ? null : me0.Data.PlayerName;
                if (string.IsNullOrWhiteSpace(hn) && Time.time - (waitSince < 0 ? (waitSince = Time.time) : waitSince) < 8f) return;
                waitSince = -1f;
            }
            sentFor = id;
            string url = Secrets.RoomCodes.Length > 0 ? Secrets.RoomCodes : (Opts.RoomShareUrl != null ? Opts.RoomShareUrl.Value.Trim() : "");
            bool ru = Translator.GameIsRu;
            if (url.Length == 0) { Utils.Toast(ru ? "Код комнаты не отправлен: не указан адрес вебхука (файл AshesOfDisaster.secrets.txt)." : "Room code not sent: no webhook address (AshesOfDisaster.secrets.txt)."); return; }
            string code = GameCode.IntToGameName(id);
            string why = Refuse(code, ru);
            if (why != null) { Utils.Toast(why); Plugin.Logger.LogInfo("RoomCode: blocked - " + why); return; }
            sentCodes[code] = Time.time; lastSend = Time.time; lastCode = code; hourly.Enqueue(Time.time);
            var me = PlayerControl.LocalPlayer;
            string host = me == null || me.Data == null || string.IsNullOrWhiteSpace(me.Data.PlayerName) ? "неизвестно" : me.Data.PlayerName;
            msgCode = code; msgHost = host; msgGame = id; msgUrl = url; msgId = null; lastText = null; nextUpd = Time.time + 30f;
            string text = StatusText(false);
            string body = "{\"content\":\"" + Esc(text) + "\",\"allowed_mentions\":{\"parse\":[]}}";
            Plugin.Logger.LogInfo("RoomCode: sending room code " + code);
            Utils.Toast(ru ? "Код комнаты отправлен в Discord: " + code : "Room code sent to Discord: " + code);
            Task.Run(async () =>
            {
                try
                {
                    using var content = new StringContent(body, Encoding.UTF8, "application/json");
                    var res = await http.PostAsync(url + (url.Contains("?") ? "&" : "?") + "wait=true", content);
                    if (res.IsSuccessStatusCode)
                    {
                        Plugin.Logger.LogInfo("RoomCode: sent, answer " + (int)res.StatusCode);
                        try
                        {
                            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                            msgId = doc.RootElement.GetProperty("id").GetString();
                            lastText = text;
                        }
                        catch (Exception e2) { Plugin.Logger.LogWarning("RoomCode: no message id: " + e2.Message); }
                    }
                    else
                    {
                        string answer = "";
                        try { answer = await res.Content.ReadAsStringAsync(); } catch { }
                        Plugin.Logger.LogWarning("RoomCode NOT delivered, Discord answered " + (int)res.StatusCode + ": " + (answer.Length > 200 ? answer.Substring(0, 200) : answer));
                    }
                }
                catch (Exception e) { Plugin.Logger.LogWarning("RoomCode failed: " + e.GetBaseException().Message); }
            });
        }
        catch (Exception e) { Plugin.Logger.LogWarning("RoomCode.Tick: " + e.Message); }
    }
}
