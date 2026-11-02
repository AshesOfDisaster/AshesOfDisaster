using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace AshesOfDisaster;

// /ai вопрос: мгновенный ответ из встроенных описаний ролей и справки по командам (без интернета).
public static class Ask
{
    static readonly Dictionary<byte, float> lastAsk = new();

    static string Norm(string s) => s.ToLowerInvariant().Replace('ё', 'е');

    static List<string> Tokens(string q)
    {
        var sb = new StringBuilder();
        foreach (var ch in Norm(q)) sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    static string Stem(string w) => w.Length > 6 ? w.Substring(0, w.Length - 2) : w.Length > 4 ? w.Substring(0, w.Length - 1) : w;

    static bool Has(List<string> tk, params string[] stems) => tk.Any(t => stems.Any(s => t.StartsWith(s)));

    public static bool Handle(PlayerControl sender, string question, bool host)
    {
        bool ru = Translator.IsRu(sender);
        if (string.IsNullOrWhiteSpace(question))
        {
            Utils.SendSecret(sender, ru ? "/ai вопрос. Например: /ai что делает Сантехник, /ai как включить музыку, /ai какие есть команды." : "/ai question. For example: /ai what does Plumber do, /ai how to turn on music, /ai what commands exist.");
            return true;
        }
        if (!host && lastAsk.TryGetValue(sender.PlayerId, out var t0) && Time.time - t0 < 4f) { Utils.SendSecret(sender, ru ? "Подожди пару секунд." : "Wait a couple of seconds."); return true; }
        lastAsk[sender.PlayerId] = Time.time;
        if (TryAi(sender, question, ru)) return true;
        return Local(sender, question, ru);
    }

    static readonly HttpClient http = MakeHttp();
    static HttpClient MakeHttp()
    {
        var h = new HttpClient();
        try { h.Timeout = TimeSpan.FromSeconds(20); } catch { }
        return h;
    }
    static readonly ConcurrentQueue<(byte pid, string text, string q)> done = new();
    static readonly Queue<float> calls = new();

    static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n").Replace("\t", " ");

    static bool TryAi(PlayerControl sender, string question, bool ru)
    {
        string key = Secrets.AiKey;
        if (string.IsNullOrEmpty(key)) return false;
        float now = Time.realtimeSinceStartup;
        while (calls.Count > 0 && now - calls.Peek() > 60f) calls.Dequeue();
        if (calls.Count >= 10) return false;
        calls.Enqueue(now);
        var ctx = new StringBuilder();
        foreach (var r in Direct(Tokens(question)))
            ctx.Append("Роль ").Append(r.Ru).Append(" / ").Append(r.En).Append(": ").Append(r.DescRu).Append(" ");
        string sys = "Ты помощник мода Ashes of Disaster для Among Us (хост-мод с 140+ ролями, команды /m /r /tip /ab /id /lang /report /ai, своя музыка в папке BepInEx/AshesMusic). "
            + "Отвечай коротко (до 300 символов), без markdown и списков, на языке вопроса. Отвечай только про мод и Among Us. Если не знаешь, так и скажи. "
            + (ctx.Length > 0 ? "Справка по ролям: " + Cut(ctx.ToString(), 1500) : "");
        string body = "{\"system_instruction\":{\"parts\":[{\"text\":\"" + Esc(sys) + "\"}]},\"contents\":[{\"parts\":[{\"text\":\"" + Esc(Cut(question, 300)) + "\"}]}],\"generationConfig\":{\"maxOutputTokens\":600}}";
        string url = "https://generativelanguage.googleapis.com/v1beta/models/" + Secrets.AiModel + ":generateContent";
        byte pid = sender.PlayerId;
        Task.Run(async () =>
        {
            string text = null;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                req.Headers.Add("x-goog-api-key", key);
                var res = await http.SendAsync(req);
                string answer = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(answer);
                    text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                }
                else Plugin.Logger.LogWarning("AI answered " + (int)res.StatusCode + ": " + Cut(answer, 200));
            }
            catch (Exception e) { Plugin.Logger.LogWarning("AI failed: " + e.GetBaseException().Message); }
            if (!string.IsNullOrWhiteSpace(text)) text = Cut(text.Replace("*", "").Replace("#", "").Trim(), 450);
            done.Enqueue((pid, string.IsNullOrWhiteSpace(text) ? null : text, question));
        });
        return true;
    }

    static string Cut(string s, int n) => s == null ? "" : s.Length > n ? s.Substring(0, n) : s;

    // вызывается из Update (главный поток): доставляет ответы нейросети или запасной локальный ответ
    public static void Tick()
    {
        while (done.TryDequeue(out var d))
        {
            try
            {
                var p = Utils.GetPlayer(d.pid);
                if (p == null || p.Data == null) continue;
                if (d.text != null) Utils.SendSecret(p, d.text, 3);
                else Local(p, d.q, Translator.IsRu(p));
            }
            catch (Exception e) { Plugin.Logger.LogWarning("Ask.Tick: " + e.Message); }
        }
    }

    static List<RoleDef> Direct(List<string> tk)
    {
        var words = tk.Where(w => w.Length >= 4).ToList();
        return Opts.Roles.Where(r =>
        {
            string a = Norm(r.Ru), b = Norm(r.En);
            return words.Any(w => w.StartsWith(Stem(a)) || w.StartsWith(Stem(b)) || a.StartsWith(w) || b.StartsWith(w));
        }).Take(2).ToList();
    }

    static bool Local(PlayerControl sender, string question, bool ru)
    {
        var tk = Tokens(question);
        // справка по моду
        if (Has(tk, "музык", "music", "трек"))
        { Utils.SendSecret(sender, ru ? "Своя музыка: положи mp3/mp4/m4a/wav в папку BepInEx/AshesMusic, в ноутбуке настроек (хост) открой зелёную вкладку «Музыка». Музыку слышит только хост." : "Own music: put mp3/mp4/m4a/wav into BepInEx/AshesMusic, open the green \"Music\" tab in the settings laptop (host). Only the host hears it.", 3); return true; }
        if (Has(tk, "жалоб", "репорт", "report"))
        { Utils.SendSecret(sender, ru ? "Жалоба на игрока: /report ID причина (ID смотри командой /id). Раз в минуту, жалоба уходит в Discord." : "Report a player: /report ID reason (see IDs with /id). Once a minute, it goes to Discord.", 3); return true; }
        if (Has(tk, "команд", "помощ", "help", "command"))
        { Utils.SendSecret(sender, ru ? "Команды: /m (моя роль), /r (все роли, /r название), /tip (совет), /ab, /id, /lang, /report, /ai вопрос. Полный список: /help." : "Commands: /m (my role), /r (all roles, /r name), /tip, /ab, /id, /lang, /report, /ai question. Full list: /help.", 3); return true; }
        if (Has(tk, "кто") && (tk.Contains("я") || Has(tk, "мо", "my")) || Has(tk, "myrole") || (Has(tk, "who") && tk.Contains("am")))
        {
            if (RoleState.Assigned.TryGetValue(sender.PlayerId, out var mine)) { Utils.SendSecret(sender, RoleGuide.Card(sender, mine), 5); return true; }
            Utils.SendSecret(sender, ru ? "Сейчас у тебя нет роли мода." : "You have no mod role right now.", 2); return true;
        }

        // роль по названию
        var words = tk.Where(w => w.Length >= 4).ToList();
        var direct = Direct(tk);
        if (direct.Count > 0)
        {
            var o = new List<string>();
            foreach (var r in direct) { o.Add(r.Colored(ru)); o.AddRange(Utils.PackText(ru ? r.DescRu : r.DescEn, 5)); }
            Utils.SendRich(sender, o);
            return true;
        }

        // поиск по описаниям
        var stems = words.Select(w => w.Length > 6 ? w.Substring(0, 6) : w).Distinct().ToList();
        var top = Opts.Roles.Select(r => (r, n: stems.Count(s => Norm(ru ? r.DescRu : r.DescEn).Contains(s)))).Where(x => x.n > 0)
            .OrderByDescending(x => x.n).Take(3).ToList();
        if (top.Count > 0)
        {
            Utils.SendSecret(sender, (ru ? "Похоже на роли: " : "Looks like these roles: ") + string.Join(", ", top.Select(x => x.r.Colored(ru))) + (ru ? ". Подробнее: /ai название роли." : ". More: /ai role name."), 3);
            return true;
        }
        Utils.SendSecret(sender, ru ? "Не нашёл ответа. Спроси про роль (/ai что делает Сантехник), команды или музыку." : "No answer found. Ask about a role (/ai what does Plumber do), commands or music.", 3);
        return true;
    }
}
