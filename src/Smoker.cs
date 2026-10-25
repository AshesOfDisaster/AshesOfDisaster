using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

public static class Smoker
{
    class Smoke { public byte Owner; public Vector2 Pos; public float Until; }
    static readonly List<Smoke> clouds = new();
    static readonly Dictionary<byte, float> inside = new();
    static readonly Dictionary<byte, float> warned = new();
    static readonly Dictionary<byte, float> last = new();
    static float nextTick;

    public static List<(string key, byte owner, Vector2 pos, string ru, string en, string color)> Items()
    {
        var l = new List<(string key, byte owner, Vector2 pos, string ru, string en, string color)>();
        foreach (var c in clouds) if (Time.time < c.Until) l.Add(("sk" + c.Owner + "_" + c.Pos.x.ToString("0.0") + "_" + c.Pos.y.ToString("0.0"), c.Owner, c.Pos, "Дым", "Smoke", "#BDBDBD"));
        return l;
    }

    public static void Reset() { clouds.Clear(); inside.Clear(); warned.Clear(); last.Clear(); }
    public static bool InOwnSmoke(PlayerControl p)
    {
        if (p == null || p.Data == null || !RoleLogic.Has(p, "Smoker")) return false;
        var pos = p.GetTruePosition(); float r = Opts.SmokeRadius.Value / 10f;
        return clouds.Any(c => c.Owner == p.PlayerId && Time.time < c.Until && Vector2.Distance(c.Pos, pos) <= r);
    }
    public static void OnMeeting() { clouds.Clear(); inside.Clear(); }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        int n = clouds.Count(c => c.Owner == p.PlayerId);
        return ru ? $"{r.Colored(true)}: питомец - выкурить сигарету, на месте остаётся дым. Кто просидит в нём {Opts.SmokeKill.Value} с подряд - умрёт (в своём дыму тебя нельзя убить). Дымов {n}/{Opts.SmokeMax.Value}."
                  : $"{r.Colored(false)}: pet - smoke a cigarette, leaving a smoke cloud. Whoever stays inside {Opts.SmokeKill.Value} s in a row dies (you are safe in your own smoke). Clouds {n}/{Opts.SmokeMax.Value}.";
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        void H(string s) => RoleLogic.Hint(p, s);
        clouds.RemoveAll(c => Time.time >= c.Until);
        if (clouds.Count(c => c.Owner == p.PlayerId) >= Opts.SmokeMax.Value) { H(ru ? "Дымов уже максимум." : "Smoke limit reached."); return true; }
        if (last.TryGetValue(p.PlayerId, out var l) && Time.time - l < Opts.SmokeCooldown.Value)
        { H(ru ? $"Сигарета ещё не готова: {Mathf.CeilToInt(Opts.SmokeCooldown.Value - (Time.time - l))} с." : "Not ready yet."); return true; }
        var pos = p.GetTruePosition();
        if (clouds.Any(c => Vector2.Distance(c.Pos, pos) < 0.8f)) { H(ru ? "Здесь уже дымно." : "Already smoky here."); return true; }
        last[p.PlayerId] = Time.time;
        clouds.Add(new Smoke { Owner = p.PlayerId, Pos = pos, Until = Time.time + Opts.SmokeLife.Value });
        H(ru ? $"Ты закурил: {Utils.RoomName(pos, true)}. Дым будет {Opts.SmokeLife.Value} с." : $"You lit up in {Utils.RoomName(pos, false)}.");
        Plugin.Logger.LogInfo($"Smoker {p.Data.PlayerName}: smoke at ({pos.x:F1},{pos.y:F1})");
        return true;
    }

    public static void Tick()
    {
        if (!Utils.AmHost || clouds.Count == 0 || Time.time < nextTick) return;
        float dt = 0.25f; nextTick = Time.time + dt;
        if (MeetingHud.Instance != null) { OnMeeting(); return; }
        clouds.RemoveAll(c => Time.time >= c.Until);
        float r = Opts.SmokeRadius.Value / 10f;
        var inNow = new HashSet<byte>();
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (o == null || o.Data == null || o.Data.IsDead || o.Data.Disconnected) continue;
            var pos = o.GetTruePosition();
            var cl = clouds.FirstOrDefault(c => c.Owner != o.PlayerId && Vector2.Distance(c.Pos, pos) <= r);
            if (cl == null) continue;
            inNow.Add(o.PlayerId);
            inside[o.PlayerId] = (inside.TryGetValue(o.PlayerId, out var t) ? t : 0f) + dt;
            float left = Opts.SmokeKill.Value - inside[o.PlayerId];
            if (!warned.TryGetValue(o.PlayerId, out var w) || Time.time - w > 3f)
            {
                warned[o.PlayerId] = Time.time;
                RoleLogic.Hint(o, Translator.IsRu(o) ? $"Ты в дыму! Выйди, иначе умрёшь через {Mathf.Max(1, Mathf.CeilToInt(left))} с." : $"You are in smoke! Get out or die in {Mathf.Max(1, Mathf.CeilToInt(left))} s.");
            }
            if (left > 0f) continue;
            inside.Remove(o.PlayerId);
            var owner = Utils.GetPlayer(cl.Owner);
            if (owner == null || owner.Data == null || owner.Data.IsDead) { clouds.Remove(cl); continue; }
            if (RoleLogic.ShieldArmed.Remove(o.PlayerId))
            {
                RoleLogic.ShieldUsed.Add(o.PlayerId);
                Utils.SendSecret(o, Translator.IsRu(o) ? "Защита сработала: дым не убил тебя!" : "Shield triggered: the smoke did not kill you!");
                continue;
            }
            if (RoleLogic.Kill(owner, o, true))
            {
                last.Remove(owner.PlayerId);
                RoleLogic.Hint(owner, Translator.IsRu(owner) ? $"Дым убил: {o.Data.PlayerName}. Новая сигарета готова." : $"The smoke killed: {o.Data.PlayerName}. A new cigarette is ready.");
            }
        }
        foreach (var k in inside.Keys.ToList()) if (!inNow.Contains(k)) inside.Remove(k);
    }
}
