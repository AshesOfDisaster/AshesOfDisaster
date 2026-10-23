using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

public static class Psy
{
    static readonly System.Random rng = new();
    static readonly Dictionary<byte, float> last = new();
    static readonly Dictionary<byte, int> uses = new();
    static readonly HashSet<byte> sawBody = new();
    static readonly HashSet<byte> killers = new();
    static readonly Dictionary<byte, (int cat, int val)> cache = new();
    static float nextTick;

    public static void Reset() { last.Clear(); uses.Clear(); sawBody.Clear(); killers.Clear(); cache.Clear(); }
    public static void OnKill(PlayerControl killer) { if (killer != null) killers.Add(killer.PlayerId); }
    public static void OnReport(PlayerControl reporter) { if (reporter != null) sawBody.Add(reporter.PlayerId); }

    public static void Tick()
    {
        if (!Utils.AmHost || RoleState.Assigned.Count == 0 || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        if (MeetingHud.Instance != null) return;
        try
        {
            var bodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            if (bodies == null || bodies.Count == 0) return;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.Data.IsDead || p.Data.Disconnected) continue;
                Vector2 pos = p.GetTruePosition();
                foreach (var b in bodies)
                    if (b != null && Vector2.Distance(pos, (Vector2)b.transform.position) < 3.5f) { sawBody.Add(p.PlayerId); break; }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Psy.Tick: " + e.Message); }
    }

    public static int Left(byte id) => Math.Max(0, Opts.PsyMaxUses.Value - (uses.TryGetValue(id, out var u) ? u : 0));

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        return ru ? $"{r.Colored(true)}: на собрании /ab ID - проверить психическое состояние игрока. Осталось проверок: {Left(p.PlayerId)}."
                  : $"{r.Colored(false)}: in a meeting use /ab ID to check a player's mental state. Checks left: {Left(p.PlayerId)}.";
    }

    static PlayerControl Find(string q)
    {
        if (byte.TryParse(q, out var id)) { var p = Utils.GetPlayer(id); if (p != null) return p; }
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p != null && p.Data != null && p.Data.PlayerName != null && p.Data.PlayerName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return p;
        return null;
    }

    public static bool Use(PlayerControl p, string[] args)
    {
        if (!RoleState.Assigned.TryGetValue(p.PlayerId, out var role) || role.Id != "Psychologist") return false;
        bool ru = Translator.IsRu(p);
        void Say(string s) => Utils.SendSecret(p, s);
        if (p.Data.IsDead) { Say(ru ? "Мёртвые не проверяют." : "The dead can't check."); return true; }
        if (MeetingHud.Instance == null) { Say(ru ? "Проверка работает только на собрании." : "Checking works only during a meeting."); return true; }
        if (args.Length == 0) { Say(ru ? "/ab ID - кого проверить. ID можно посмотреть командой /id." : "/ab ID - who to check."); return true; }
        var t = Find(string.Join(" ", args));
        if (t == null || t.Data == null) { Say(ru ? "Игрок не найден." : "Player not found."); return true; }
        if (Left(p.PlayerId) <= 0) { Say(ru ? "Проверки закончились." : "No checks left."); return true; }
        float cd = Opts.PsyCooldown.Value;
        if (last.TryGetValue(p.PlayerId, out var l) && Time.time - l < cd)
        { Say((ru ? "Перезарядка: ещё " : "Cooldown: ") + Mathf.CeilToInt(cd - (Time.time - l)) + (ru ? " сек." : " s.")); return true; }

        uses[p.PlayerId] = (uses.TryGetValue(p.PlayerId, out var u) ? u : 0) + 1;
        last[p.PlayerId] = Time.time;
        string nm = t.Data.PlayerName;
        if (rng.Next(100) < Opts.PsyFailChance.Value)
        {
            Say(ru ? $"Не удалось понять состояние {nm}: он закрылся. Проверок осталось: {Left(p.PlayerId)}." : $"Couldn't read {nm}: he shut himself off. Checks left: {Left(p.PlayerId)}.");
            Plugin.Logger.LogInfo($"Psychologist {p.Data.PlayerName}: check on {nm} failed (chance)");
            return true;
        }
        int cat = killers.Contains(t.PlayerId) ? 2 : sawBody.Contains(t.PlayerId) ? 1 : 0;
        if (!cache.TryGetValue(t.PlayerId, out var c) || c.cat != cat)
        {
            int v = cat == 0 ? rng.Next(0, 40) : cat == 1 ? rng.Next(40, 60) : rng.Next(60, 101);
            cache[t.PlayerId] = c = (cat, v);
        }
        string col = c.cat == 0 ? "#7CFC00" : c.cat == 1 ? "#FFD700" : "#FF4D4D";
        string meaning = ru ? (c.cat == 0 ? "спокоен" : c.cat == 1 ? "видел труп" : "убийца") : (c.cat == 0 ? "calm" : c.cat == 1 ? "saw a body" : "killer");
        Say((ru ? "Психическое состояние " : "Mental state of ") + nm + $": <color={col}>{c.val}/100 - {meaning}</color>. " + (ru ? "Проверок осталось: " : "Checks left: ") + Left(p.PlayerId));
        Plugin.Logger.LogInfo($"Psychologist {p.Data.PlayerName}: {nm} = {c.val} (cat {c.cat})");
        return true;
    }
}
