using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        var direct = Opts.Roles.Where(r =>
        {
            string a = Norm(r.Ru), b = Norm(r.En);
            return words.Any(w => w.StartsWith(Stem(a)) || w.StartsWith(Stem(b)) || a.StartsWith(w) || b.StartsWith(w));
        }).Take(2).ToList();
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
