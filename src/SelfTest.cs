using UnityEngine;
using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using InnerNet;

namespace AshesOfDisaster;

public static class SelfTest
{
    static string path;
    static readonly object gate = new();

    static string Path_()
    {
        if (path == null) path = System.IO.Path.Combine(Paths.BepInExRootPath, "AshesOfDisaster_selftest.log");
        return path;
    }

    public static void Append(string line)
    {
        try { lock (gate) File.AppendAllText(Path_(), DateTime.Now.ToString("HH:mm:ss") + " " + line + Environment.NewLine); }
        catch { }
    }

    static int problems;
    static void Bad(string s) { problems++; Append("PROBLEM: " + s); }
    static void Ok(string s) => Append("ok: " + s);

    public static void Install()
    {
        try
        {
            try { File.WriteAllText(Path_(), ""); } catch { }
            Append($"Ashes of Disaster {Plugin.Version} self-test log. Send this file to the developer.");
            Plugin.Logger.LogEvent += OnLog;
        }
        catch (Exception e) { Plugin.Logger.LogWarning("SelfTest.Install: " + e.Message); }
    }

    static void OnLog(object sender, LogEventArgs e)
    {
        try
        {
            if (e.Level != LogLevel.Warning && e.Level != LogLevel.Error && e.Level != LogLevel.Fatal) return;
            Hits++;
            Append($"[{e.Level}] [{e.Source.SourceName}]{(Context != null ? " {" + Context + "}" : "")} {e.Data}");
        }
        catch { }
    }

    public static void RunStatic()
    {
        try
        {
            problems = 0;
            try { Append("game version: " + UnityEngine.Application.version + ", platform: " + UnityEngine.Application.platform); } catch { }

            foreach (var m in new[] { "CheckShapeshift", "RpcRejectShapeshift", "CheckMurder", "RpcMurderPlayer", "RpcSetRole", "Shapeshift" })
                if (ModHooks.Exists(typeof(PlayerControl), m)) Ok("PlayerControl." + m); else Bad("PlayerControl." + m + " not found in this game version");
            if (ModHooks.Exists(typeof(PlayerPhysics), "CoEnterVent")) Ok("PlayerPhysics.CoEnterVent"); else Bad("PlayerPhysics.CoEnterVent not found");
            if (ModHooks.Exists(typeof(MeetingHud), "VotingComplete")) Ok("MeetingHud.VotingComplete"); else Bad("MeetingHud.VotingComplete not found");

            var calls = Enum.GetNames(typeof(RpcCalls));
            foreach (var n in new[] { "Pet", "SetRole", "Shapeshift", "SendChat", "SetName", "SnapTo", "UpdateSystem", "SetScale" })
                if (calls.Contains(n)) Ok("RpcCalls." + n); else Bad("RpcCalls." + n + " missing (features that use it are off)");
            var types = Enum.GetNames(typeof(RoleTypes));
            foreach (var n in new[] { "Impostor", "Crewmate", "Shapeshifter" })
                if (!types.Contains(n)) Bad("RoleTypes." + n + " missing");

            try
            {
                int mine = 0;
                foreach (var m in Harmony.GetAllPatchedMethods())
                {
                    var info = Harmony.GetPatchInfo(m);
                    if (info != null && info.Owners.Contains(Plugin.Id)) mine++;
                }
                Append("harmony: patched methods by this mod: " + mine);
                if (mine < 20) Bad("very few patches applied (" + mine + "), something failed to patch, see errors above");
            }
            catch (Exception e) { Append("harmony check failed: " + e.Message); }

            var roles = Opts.Roles;
            Append("roles registered: " + roles.Count);
            foreach (var g in roles.GroupBy(r => r.Id).Where(g => g.Count() > 1)) Bad("duplicate role id: " + g.Key);
            foreach (var g in roles.GroupBy(r => r.Tab + ":" + r.Ru).Where(g => g.Count() > 1)) Bad("duplicate role name in one group: " + g.Key);
            var hex = new Regex("^#[0-9A-Fa-f]{6}$");
            foreach (var r in roles)
            {
                if (string.IsNullOrWhiteSpace(r.Ru) || string.IsNullOrWhiteSpace(r.En)) Bad($"role {r.Id}: empty name");
                if (string.IsNullOrWhiteSpace(r.DescRu) || string.IsNullOrWhiteSpace(r.DescEn)) Bad($"role {r.Id}: empty description");
                else
                {
                    if (!r.DescRu.TrimEnd().EndsWith("]") || !r.DescEn.TrimEnd().EndsWith("]")) Bad($"role {r.Id}: description has no [tag] at the end");
                    if (r.DescRu.Length > 900 || r.DescEn.Length > 900) Append($"note: role {r.Id}: very long description ({r.DescRu.Length}/{r.DescEn.Length})");
                }
                if (!hex.IsMatch(r.Color ?? "")) Bad($"role {r.Id}: bad color {r.Color}");
            }
            foreach (var id in RoleLogic.RealImpNeutrals)
            {
                var r = roles.FirstOrDefault(x => x.Id == id);
                if (r == null) Bad("RealImpNeutrals has unknown role " + id);
                else if (r.Tab != OptTab.Neutral) Bad($"RealImpNeutrals role {id} is not in the Neutral group");
            }
            Append(problems == 0 ? "STATIC CHECK: no problems" : "STATIC CHECK: problems found: " + problems);
        }
        catch (Exception e) { Append("SelfTest.RunStatic crashed: " + e); }
    }


    public static string Context;
    public static int Hits;
    static bool capture;
    static readonly Queue<(string name, Action act)> steps = new();
    static bool running, done;
    static float nextStep, startedAt = -1f;
    static int stepCount;
    static float nextDiag;
    public static bool Running => running;

    public static void OnHint(PlayerControl to, string text)
    {
        if (!capture) return;
        try { Append($"    hint to {(to != null && to.Data != null ? to.Data.PlayerName : "?")}: {Regex.Replace(text ?? "", "<[^>]+>", "")}"); } catch { }
    }

    static PlayerControl Other(PlayerControl host)
        => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(o => o != null && o.Data != null && !o.Data.IsDead && !o.Data.Disconnected && o.PlayerId != host.PlayerId);

    static bool IsPractice()
    {
        var c = AmongUsClient.Instance;
        return c != null && c.NetworkMode == NetworkModes.FreePlay && ShipStatus.Instance != null && PlayerControl.LocalPlayer != null;
    }

    public static void Start()
    {
        steps.Clear(); done = false; running = true; stepCount = 0; Hits = 0;
        Append("==== AUTO TEST START (practice mode) ====");
        var host = PlayerControl.LocalPlayer;
        if (host == null) { running = false; return; }
        foreach (var role in Opts.Roles.ToList())
        {
            var r = role;
            steps.Enqueue(("setup", () =>
            {
                Context = r.Id; capture = true;
                int before = Hits;
                Append($"-- role {r.Id} ({r.Ru}/{r.En}) group={r.Tab}");
                NR.Reset();
                RoleState.Assigned[host.PlayerId] = r;
            }));
            steps.Enqueue(("ability1", () => { Append("  ability use 1"); RoleLogic.UseAbility(host, "autotest"); }));
            steps.Enqueue(("ability2", () => { Append("  ability use 2 (cooldown path)"); RoleLogic.UseAbility(host, "autotest"); }));
            steps.Enqueue(("vent", () =>
            {
                if (r.Id == "Freeman" || r.Id == "God" || Extra33.Handles(r.Id))
                {
                    Append("  vent entry");
                    try { Extra33.OnVent(host, 0); } catch (Exception e) { Append("PROBLEM: Extra33.OnVent: " + e.GetBaseException().Message); }
                    try { Freeman.OnVent(host, 0); } catch (Exception e) { Append("PROBLEM: Freeman.OnVent: " + e.GetBaseException().Message); }
                    try { God.OnVent(host, 0); } catch (Exception e) { Append("PROBLEM: God.OnVent: " + e.GetBaseException().Message); }
                }
            }));
            steps.Enqueue(("morph", () =>
            {
                var o = Other(host);
                if (r.Id == "God" && o != null) { Append("  morph at " + o.Data.PlayerName); God.OnMorph(host, o); }
            }));
            steps.Enqueue(("status", () =>
            {
                foreach (var ru in new[] { true, false })
                {
                    var st = RoleLogic.NameStatus(host, r, ru);
                    var s2 = NR.Status(host, r);
                    Append($"  status {(ru ? "ru" : "en")}: {Regex.Replace(st ?? "<null>", "<[^>]+>", "")}");
                    if (string.IsNullOrEmpty(s2)) Append("PROBLEM: NR.Status is empty for " + r.Id);
                }
            }));
            steps.Enqueue(("kill hook", () =>
            {
                var o = Other(host);
                if (o != null) NR.OnKill(host, o);
            }));
            steps.Enqueue(("meeting hook", () => NR.OnMeeting()));
            steps.Enqueue(("tick", () => { }));
        }
        steps.Enqueue(("commands", () =>
        {
            Context = "commands"; capture = true;
            var skip = new HashSet<string> { "kick", "ban", "unban", "start", "cancel", "end", "kill", "say", "block", "report", "role", "tag" };
            foreach (var cmd in Commands.All)
            {
                if (skip.Contains(cmd.Name)) continue;
                foreach (var arg in new[] { "", " Plumber", " 1" })
                {
                    try { Append($"-- command /{cmd.Name}{arg}"); Commands.Handle(PlayerControl.LocalPlayer, "/" + cmd.Name + arg); }
                    catch (Exception e) { Append($"PROBLEM: command /{cmd.Name}{arg} crashed: {e.GetBaseException().Message}"); }
                }
            }
        }));
        steps.Enqueue(("finish", () =>
        {
            Context = null; capture = false; done = true; running = false;
            NR.Reset(); RoleState.Assigned.Clear();
            Append($"==== AUTO TEST END: warnings and errors during the test: {Hits} ====");
        }));
    }

    public static void Tick()
    {
        try
        {
            if (!running && !done && Time.time > nextDiag)
            {
                nextDiag = Time.time + 20f;
                var ac = AmongUsClient.Instance;
                if (ac != null && ac.NetworkMode == NetworkModes.FreePlay)
                    Append($"autotest waiting: mode={ac.NetworkMode} state={ac.GameState} host={Utils.AmHost} ship={(ShipStatus.Instance != null)} local={(PlayerControl.LocalPlayer != null)} enabled={(Opts.AutoTest != null && Opts.AutoTest.Value)}");
            }
            if (!running && !done && Opts.AutoTest != null && Opts.AutoTest.Value && IsPractice())
            {
                if (startedAt < 0) startedAt = Time.time;
                if (Time.time - startedAt > 10f) Start();
                return;
            }
            if (!running || Time.time < nextStep) return;
            if (MeetingHud.Instance != null) return;
            var host = PlayerControl.LocalPlayer;
            if (host == null || host.Data == null || host.Data.IsDead) { Append("auto test stopped: host is dead or missing"); running = false; done = true; capture = false; Context = null; return; }
            if (steps.Count == 0) { running = false; return; }
            nextStep = Time.time + 0.2f;
            var (name, act) = steps.Dequeue();
            stepCount++;
            try { act(); }
            catch (Exception e) { Append($"PROBLEM: step '{name}' in role {Context}: {e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}"); }
        }
        catch (Exception e) { Append("SelfTest.Tick crashed: " + e); running = false; }
    }

    // после раздачи ролей: пробуем описание и статус каждой выданной роли на живых данных
    public static void OnGameStart()
    {
        try
        {
            Append("---- game start ----");
            int n = 0, bad = 0;
            foreach (var p in PlayerControl.AllPlayerControls.ToArray())
            {
                if (p == null || p.Data == null) continue;
                RoleState.Assigned.TryGetValue(p.PlayerId, out var role);
                Append($"player {p.PlayerId} {p.Data.PlayerName}: role={(role != null ? role.Id : "-")} vanilla={p.Data.RoleType}");
                if (role == null) continue;
                n++;
                foreach (var ru in new[] { true, false })
                {
                    try
                    {
                        var s1 = RoleLogic.NameStatus(p, role, ru);
                        var s2 = NR.Status(p, role);
                        var s3 = RoleGuide.Line(role, ru);
                        if (string.IsNullOrEmpty(s2)) { bad++; Append($"PROBLEM: role {role.Id}: empty status text"); }
                    }
                    catch (Exception e) { bad++; Append($"PROBLEM: role {role.Id} ({(ru ? "ru" : "en")}) status crashed: {e.GetBaseException().Message}"); }
                }
                if (RoleLogic.RealImpNeutrals.Contains(role.Id) && !RoleLogic.IsImpRole(p.Data.RoleType))
                    Append($"PROBLEM: neutral killer {role.Id} is not a vanilla impostor-team role (vanilla={p.Data.RoleType}), kills will not work");
            }
            Append($"GAME START CHECK: roles tested {n}, problems {bad}");
        }
        catch (Exception e) { Append("SelfTest.OnGameStart crashed: " + e); }
    }
}
