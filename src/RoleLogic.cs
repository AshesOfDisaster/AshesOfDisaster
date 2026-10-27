using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class RoleLogic
{
    static readonly System.Random rng = new();

    public static readonly HashSet<byte> Informed = new();
    public static readonly HashSet<string> RealImpNeutrals = new() { "Vigilante", "Cloud", "Smoker", "Meteor", "Joker" };
    public static readonly HashSet<byte> ShieldArmed = new();
    public static readonly HashSet<byte> ShieldUsed = new();
    public static readonly Dictionary<byte, float> Painted = new();
    public static readonly HashSet<byte> Eaten = new();
    public static readonly HashSet<byte> VigUnlocked = new();
    public static readonly Dictionary<byte, int> VigPenalty = new();
    public static readonly Dictionary<byte, float> VigLastKill = new();
    static readonly Dictionary<byte, float> petLast = new();
    static readonly Queue<(byte id, string src, float at)> pendingAbility = new();
    public static float VigStart;
    static bool VigReady(byte id, int total, int done)
        => VigWait(id) <= 0;

    static string VigLeft(PlayerControl p, bool ru) => $"{VigWait(p.PlayerId)} {(ru ? "с" : "s")}";

    static int VigWait(byte id) => Math.Max(0, (int)Math.Ceiling(Opts.VigilanteUnlockTime.Value - (Time.time - VigStart)));

    public class Web { public Vector2 Pos; public byte Owner; public int Trapped = -1; public float Rescue; public float LastSnap; }
    public static readonly List<Web> Webs = new();

    static readonly Dictionary<byte, float> BuilderLast = new();
    static int BuildWait(byte id) => BuilderLast.TryGetValue(id, out var t) ? Math.Max(0, (int)Math.Ceiling(Opts.BuilderCooldown.Value - (Time.time - t))) : 0;

    public static void PlaceWall(PlayerControl b)
    {
        bool ru = Translator.IsRu(b);
        int w = BuildWait(b.PlayerId);
        if (w > 0) { Hint(b, ru ? $"Закрытие двери перезаряжается: ещё {w} с." : "Door closing is cooling down."); return; }
        var doors = ShipStatus.Instance != null ? ShipStatus.Instance.AllDoors : null;
        if (doors == null || doors.Length == 0) { Hint(b, ru ? "На этой карте нет дверей." : "There are no doors on this map."); return; }
        Vector2 pos = b.GetTruePosition();
        OpenableDoor best = null; float bd = 999f;
        foreach (var d in doors)
        {
            if (d == null) continue;
            float dd = Vector2.Distance(pos, (Vector2)d.transform.position);
            if (dd < bd) { bd = dd; best = d; }
        }
        if (best == null || bd > 2.5f) { Hint(b, ru ? "Рядом нет двери." : "No door nearby."); return; }
        BuilderLast[b.PlayerId] = Time.time;
        Plugin.Logger.LogInfo($"Builder {b.Data.PlayerName}: closing doors of room {best.Room}");
        ShipStatus.Instance.RpcCloseDoorsOfType(best.Room);
        Hint(b, ru ? "Дверь закрыта." : "Door closed.");
    }

    public class Cloud { public Vector2 Pos; public byte Owner; public bool Storm; }
    static void RemoveCloud(Cloud c) { Clouds.Remove(c); }
    public static readonly List<Cloud> Clouds = new();
    static readonly List<(PlayerControl p, float end, SystemTypes sys)> flashes = new();
    static readonly Dictionary<byte, float> lastPanic = new();
    static float lastCloud;

    public static void PlaceCloud(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        if (Clouds.Count(cl0 => cl0.Owner == p.PlayerId) >= Opts.CloudMax.Value)
        { Hint(p, ru ? "Достигнут лимит облаков." : "Cloud limit reached."); return; }
        Vector2 pos = p.GetTruePosition();
        if (Clouds.Any(cl0 => Vector2.Distance(cl0.Pos, pos) < 0.8f)) { Hint(p, ru ? "Здесь уже есть облако." : "There is already a cloud here."); return; }
        bool storm = rng.Next(100) < Opts.CloudStormChance.Value;
        var newCloud = new Cloud { Pos = pos, Owner = p.PlayerId, Storm = storm };
        Clouds.Add(newCloud);
        Plugin.Logger.LogInfo($"Cloud placed by {p.Data.PlayerName} at ({pos.x:F1},{pos.y:F1}) storm={storm}");
        int n = Clouds.Count(cl0 => cl0.Owner == p.PlayerId);
        if (storm) Utils.SendSecret(p, ru ? $"ГРОЗА! Это облако ({n}/{Opts.CloudMax.Value}) - гроза: кто наступит, погибнет." : $"STORM! This cloud ({n}/{Opts.CloudMax.Value}) is a thunderstorm: whoever steps on it dies.");
        else Hint(p, ru ? $"Облако оставлено ({n}/{Opts.CloudMax.Value})." : "Cloud placed.");
    }

    static SystemTypes PanicSystem()
    {
        var ss = ShipStatus.Instance;
        if (ss == null) return SystemTypes.Reactor;
        if (ss.Type.ToString() == "Pb") return SystemTypes.Laboratory;
        try { if (ss.Systems != null && ss.Systems.ContainsKey(SystemTypes.HeliSabotage)) return SystemTypes.HeliSabotage; } catch { }
        return SystemTypes.Reactor;
    }

    static void SendSystem(PlayerControl to, SystemTypes sys, int amount)
    {
        var w = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, Hazel.SendOption.Reliable, to.OwnerId);
        w.Write((byte)sys);
        w.WritePacked(to.NetId);
        w.Write((byte)amount);
        AmongUsClient.Instance.FinishRpcImmediately(w);
    }

    static void Panic(PlayerControl owner, PlayerControl victim)
    {
        bool ru = Translator.IsRu(owner);
        Plugin.Logger.LogInfo($"Cloud triggered: owner={owner.Data.PlayerName} victim={victim.Data.PlayerName}");
        Utils.SendSecret(owner, ru ? $"Паника! На облако наступил: {victim.Data.PlayerName}" : $"Panic! Someone stepped on the cloud: {victim.Data.PlayerName}");
        if (owner.AmOwner) { Utils.Toast(ru ? "ПАНИКА! На облако наступили!" : "PANIC! Someone stepped on the cloud!"); return; }
        if (lastPanic.TryGetValue(owner.PlayerId, out var t) && Time.time - t < Opts.CloudFlash.Value + 1f) return;
        lastPanic[owner.PlayerId] = Time.time;
        var sys = PanicSystem();
        Plugin.Logger.LogInfo($"RPC UpdateSystem (panic flash): to={owner.Data.PlayerName} system={sys}");
        SendSystem(owner, sys, 128);
        flashes.Add((owner, Time.time + Opts.CloudFlash.Value, sys));
    }

    public static void FlashPlayer(PlayerControl p, float seconds)
    {
        try
        {
            if (p == null || p.Data == null || p.AmOwner || ShipStatus.Instance == null) return;
            var sys = PanicSystem();
            SendSystem(p, sys, 128);
            flashes.Add((p, Time.time + seconds, sys));
        }
        catch (Exception e) { Plugin.Logger.LogWarning("FlashPlayer: " + e.Message); }
    }

    static bool StormHit(PlayerControl owner, PlayerControl victim)
    {
        bool ru = Translator.IsRu(owner);
        Plugin.Logger.LogInfo($"Storm triggered: owner={owner.Data.PlayerName} victim={victim.Data.PlayerName}");
        if (ShieldArmed.Remove(victim.PlayerId))
        {
            ShieldUsed.Add(victim.PlayerId);
            Utils.SendSecret(victim, ru ? "Защита сработала: гроза не тронула тебя!" : "Shield triggered: the storm missed you!");
            Utils.SendSecret(owner, ru ? $"Гроза не убила {victim.Data.PlayerName} - сработала защита." : $"The storm did not kill {victim.Data.PlayerName}: a shield triggered.");
            return true;
        }
        if (!Kill(owner, victim, true)) return false;
        Utils.SendSecret(owner, ru ? $"Гроза убила: {victim.Data.PlayerName}" : $"The storm killed: {victim.Data.PlayerName}");
        return true;
    }

    static void CloudTick()
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            var f = flashes[i];
            if (Time.time < f.end) continue;
            flashes.RemoveAt(i);
            try
            {
                if (f.p != null && f.p.Data != null && !f.p.Data.Disconnected && ShipStatus.Instance != null)
                {
                    SendSystem(f.p, f.sys, 16);
                    if (f.sys == SystemTypes.HeliSabotage) SendSystem(f.p, f.sys, 17);
                }
            }
            catch (Exception e) { Plugin.Logger.LogError("Panic flash end: " + e.GetBaseException().Message); }
        }
        if (Clouds.Count == 0) return;
        if (MeetingHud.Instance != null) { Clouds.Clear(); return; }
        if (Time.time - lastCloud < 0.25f) return;
        lastCloud = Time.time;
        float r = Opts.CloudRadius.Value / 10f;
        foreach (var cloud in Clouds.ToList())
        {
            var owner = Utils.GetPlayer(cloud.Owner);
            if (owner == null || owner.Data == null || owner.Data.IsDead) { RemoveCloud(cloud); continue; }
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p == null || p.Data == null || p.PlayerId == cloud.Owner || p.Data.IsDead || p.Data.Disconnected) continue;
                if (Vector2.Distance(p.GetTruePosition(), cloud.Pos) > r) continue;
                if (cloud.Storm)
                {
                    if (StormHit(owner, p)) RemoveCloud(cloud);
                }
                else { RemoveCloud(cloud); Panic(owner, p); }
                break;
            }
        }
    }

    public static float StartRevealAt = -1f;
    public static float AssignAt = -1f;
    static float lastTick, lastFast;

    public static bool IsImpRole(RoleTypes r)
    {
        string s = r.ToString();
        return s == "Impostor" || s == "Shapeshifter" || s == "Phantom" || s == "Viper" || s == "ImpostorGhost";
    }

    public static void Hint(PlayerControl to, string text) => Utils.SendSecret(to, text, 1, false);

    public static void Reset()
    {
        RoleState.Assigned.Clear(); ForcedImp.Clear(); Demoted.Clear(); Informed.Clear(); ShieldArmed.Clear(); ShieldUsed.Clear(); Painted.Clear(); Eaten.Clear();
        Psy.Reset(); Welder.Reset(); NR.Reset(); Smoker.Reset(); Joker.Reset(); Alchemist.Reset(); MysticRoles.Reset(); Witch.Reset(); NeutralWin.Reset(); Fat.Reset(); Pinkman.Reset(); Plumber.Reset(); Shroomer.Reset(); VigUnlocked.Clear(); VigPenalty.Clear(); VigLastKill.Clear(); petLast.Clear(); pendingAbility.Clear(); Webs.Clear(); BuilderLast.Clear(); Clouds.Clear(); flashes.Clear(); lastPanic.Clear();
        StartRevealAt = -1f;
        Utils.ClearDeferred();
        deathNotices.Clear(); NameInfo.ClearNotes();
        NameInfo.QueueRestoreAll();
    }

    public static bool Has(PlayerControl p, string id)
        => p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && r.Id == id;

    static List<T> Shuffle<T>(IEnumerable<T> src)
    {
        var l = src.ToList();
        for (int i = l.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (l[i], l[j]) = (l[j], l[i]); }
        return l;
    }

    public static void SetVanillaRole(PlayerControl p, RoleTypes role)
    {
        try
        {
            bool done = false;
            var bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var m1 = typeof(PlayerControl).GetMethods(bf).FirstOrDefault(x => x.Name == "SetRole" && x.GetParameters().Length >= 1 && x.GetParameters()[0].ParameterType == typeof(RoleTypes));
            if (m1 != null) { var ps = m1.GetParameters(); var args = new object[ps.Length]; args[0] = role; for (int i = 1; i < ps.Length; i++) args[i] = ps[i].ParameterType == typeof(bool) ? true : (ps[i].HasDefaultValue ? ps[i].DefaultValue : null); m1.Invoke(p, args); done = true; }
            if (!done && RoleManager.Instance != null)
            {
                var m2 = typeof(RoleManager).GetMethods(bf).FirstOrDefault(x => x.Name == "SetRole" && x.GetParameters().Length >= 2 && x.GetParameters()[0].ParameterType == typeof(PlayerControl));
                if (m2 != null) { var ps = m2.GetParameters(); var args = new object[ps.Length]; args[0] = p; args[1] = role; for (int i = 2; i < ps.Length; i++) args[i] = ps[i].ParameterType == typeof(bool) ? true : (ps[i].HasDefaultValue ? ps[i].DefaultValue : null); m2.Invoke(RoleManager.Instance, args); done = true; }
            }
            if (!done) Plugin.Logger.LogWarning("SetVanillaRole: no local SetRole method found");
        }
        catch (Exception e) { Plugin.Logger.LogWarning("SetRole local: " + (e.InnerException ?? e).Message); }
        var w = AmongUsClient.Instance.StartRpcImmediately(p.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable);
        w.Write((ushort)role);
        w.Write(true);
        AmongUsClient.Instance.FinishRpcImmediately(w);
        Plugin.Logger.LogInfo($"SetVanillaRole: {p.Data.PlayerName} -> {role} (now {p.Data.RoleType})");
    }

    public static void Assign()
    {
        Reset();
        var all = new List<PlayerControl>();
        foreach (var p in PlayerControl.AllPlayerControls)
            if (p != null && p.Data != null && !p.Data.Disconnected) all.Add(p);

        var imps = Shuffle(all.Where(p => IsImpRole(p.Data.RoleType)));
        var crew = Shuffle(all.Where(p => !IsImpRole(p.Data.RoleType)));

        int initialImps = imps.Count;
        var toImp = new List<PlayerControl>(); var toCrew = new List<PlayerControl>();
        foreach (var kv in RoleState.Preset.ToList())
        {
            var pp = all.Find(x => x.PlayerId == kv.Key);
            if (pp == null) continue;
            bool wantImp = kv.Value.Tab == OptTab.Impostor;
            if (RealImpNeutrals.Contains(kv.Value.Id)) wantImp = imps.Contains(pp);
            var from = wantImp ? imps : crew;
            if (!from.Remove(pp))
            {
                if (Opts.TeamSwap.Value)
                {
                    (wantImp ? crew : imps).Remove(pp);
                    (wantImp ? toImp : toCrew).Add(pp);
                    RoleState.Assigned[pp.PlayerId] = kv.Value;
                    Plugin.Logger.LogInfo($"Preset role: {pp.Data.PlayerName} = {kv.Value.Id} (vanilla team will change to {(wantImp ? "Impostor" : "Crewmate")})");
                    continue;
                }
                string why = Translator.GameIsRu
                    ? $"Роль «{kv.Value.Ru}» не выдана игроку {pp.Data.PlayerName}: {(wantImp ? "он не предатель" : "он предатель")} в этой игре (включи «Смена команды»)."
                    : $"Role '{kv.Value.En}' not given to {pp.Data.PlayerName}: {(wantImp ? "he is not an impostor" : "he is an impostor")} this game (enable team swap).";
                Plugin.Logger.LogWarning("Preset: " + why);
                Utils.Toast(why);
                continue;
            }
            RoleState.Assigned[pp.PlayerId] = kv.Value;
            Plugin.Logger.LogInfo($"Preset role: {pp.Data.PlayerName} = {kv.Value.Id}");
        }

        foreach (var team in new[] { OptTab.Impostor, OptTab.Crewmate, OptTab.Neutral, OptTab.Mystic })
        {
            var pool = team == OptTab.Impostor ? imps : crew;
            var list = new List<RoleDef>();
            foreach (var r in Opts.Roles.Where(x => x.Tab == team && x.Chance.Value > 0))
                for (int i = 0; i < r.Count.Value; i++)
                    if (rng.Next(100) < r.Chance.Value) list.Add(r);
            foreach (var r in Shuffle(list))
            {
                if (pool.Count == 0)
                {
                    if (team == OptTab.Impostor && Opts.TeamSwap.Value && initialImps == 0 && toImp.Count == 0 && crew.Count >= 2)
                    {
                        var cv = crew[0]; crew.RemoveAt(0); toImp.Add(cv);
                        RoleState.Assigned[cv.PlayerId] = r;
                        Plugin.Logger.LogInfo($"No vanilla impostor left: {cv.Data.PlayerName} becomes Impostor for role {r.Id}");
                        continue;
                    }
                    break;
                }
                var p = pool[0]; pool.RemoveAt(0);
                RoleState.Assigned[p.PlayerId] = r;
            }
        }
        VigStart = Time.time;
        foreach (var cp in toImp)
        {
            try { SetVanillaRole(cp, RoleTypes.Impostor); ForcedImp.Add(cp.PlayerId); Plugin.Logger.LogInfo($"Team swap: {cp.Data.PlayerName} -> vanilla Impostor"); }
            catch (Exception e) { Plugin.Logger.LogError("TeamSwap to Impostor: " + e.GetBaseException().Message); }
        }
        foreach (var cp in toCrew)
        {
            try { SetVanillaRole(cp, RoleTypes.Crewmate); Demoted.Add(cp.PlayerId); Plugin.Logger.LogInfo($"Team swap: {cp.Data.PlayerName} -> vanilla Crewmate"); }
            catch (Exception e) { Plugin.Logger.LogError("TeamSwap to Crewmate: " + e.GetBaseException().Message); }
        }
        foreach (var kv in RoleState.Assigned.ToList())
        {
            if (!RealImpNeutrals.Contains(kv.Value.Id)) continue;
            var vp = Utils.GetPlayer(kv.Key);
            bool isGod = kv.Value.Id == "God";
            if (vp == null || vp.Data == null || (isGod ? vp.Data.RoleType == RoleTypes.Shapeshifter : IsImpRole(vp.Data.RoleType))) continue;
            try { SetVanillaRole(vp, isGod ? RoleTypes.Shapeshifter : RoleTypes.Impostor); ForcedImp.Add(vp.PlayerId); Plugin.Logger.LogInfo($"{kv.Value.Id} {vp.Data.PlayerName}: vanilla role set to Impostor (now {vp.Data.RoleType})"); }
            catch (Exception e) { Plugin.Logger.LogError("Vigilante RpcSetRole: " + e.GetBaseException().Message); }
        }
        NeutralMask.Apply();
        SelfTest.OnGameStart();
        StartRevealAt = Time.time + 6f;
        Plugin.Logger.LogInfo("Vanilla roles: " + string.Join(", ", all.Select(p => $"{p.PlayerId}:{p.Data.RoleType}")));
        Plugin.Logger.LogInfo("Roles assigned: " + string.Join(", ", RoleState.Assigned.Select(kv => $"{kv.Key}={kv.Value.Id}")));
    }

    public static string Describe(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        return $"{r.Colored(ru)}\n{(ru ? r.DescRu : r.DescEn)}";
    }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        byte id = p.PlayerId;
        switch (r.Id)
        {
            case "Vigilante":
                {
                    int pen = VigPenalty.TryGetValue(id, out var v) ? v : 0;
                    int max = Opts.VigilanteMaxPenalty.Value;
                    return ru
                        ? $"{r.Colored(true)}: убийство через питомца - {(VigUnlocked.Contains(id) ? "открыто" : $"осталось {VigLeft(p, true)}")}. Штрафы {pen}/{max}."
                        : $"{r.Colored(false)}: pet kill - {(VigUnlocked.Contains(id) ? "unlocked" : $"left: {VigLeft(p, false)}")}. Penalties {pen}/{max}.";
                }
            case "Peasant":
                {
                    string st = ShieldUsed.Contains(id) ? (ru ? "использована" : "used")
                              : ShieldArmed.Contains(id) ? (ru ? "готова" : "ready")
                              : (ru ? $"нужно {Opts.PeasantTasks.Value} заданий" : $"needs {Opts.PeasantTasks.Value} tasks");
                    return ru ? $"{r.Colored(true)}: защита - {st}." : $"{r.Colored(false)}: shield - {st}.";
                }
            case "Cloud":
                return ru ? $"{r.Colored(true)}: питомец оставляет невидимое облако; наступивший вызовет у тебя панику. Облаков {Clouds.Count(cl0 => cl0.Owner == id)}/{Opts.CloudMax.Value}." : $"{r.Colored(false)}: your pet leaves an invisible cloud; whoever steps on it triggers a panic. Clouds {Clouds.Count(cl0 => cl0.Owner == id)}/{Opts.CloudMax.Value}.";
            case "Psychologist": return Psy.Status(p, r);
            case "Welder": return Welder.Status(p, r);
            case "Builder":
                return ru ? $"{r.Colored(true)}: питомец закрывает ближайшую дверь." : $"{r.Colored(false)}: your pet closes the nearest door.";
            case "Spider":
                {
                    int n = Webs.Count(w => w.Owner == id);
                    return ru ? $"{r.Colored(true)}: питомец ставит паутину в дверном проёме. Паутин {n}/{Opts.SpiderMaxWebs.Value}."
                              : $"{r.Colored(false)}: pet places a web in a doorway. Webs {n}/{Opts.SpiderMaxWebs.Value}.";
                }
        }
        return NR.Status(p, r);
    }

    public static readonly HashSet<byte> Demoted = new();
    public static readonly HashSet<byte> ForcedImp = new();

    public static bool Kill(PlayerControl killer, PlayerControl target, bool succeeded)
    {
        if (killer == null || target == null || killer.Data == null || target.Data == null) return false;
        bool real = IsImpRole(killer.Data.RoleType) || ForcedImp.Contains(killer.PlayerId);
        if (!real)
        {
            Plugin.Logger.LogWarning($"Kill skipped: {killer.Data.PlayerName} is not a vanilla impostor (role={killer.Data.RoleType}, forced={ForcedImp.Contains(killer.PlayerId)}); server would kick the host");
            bool ru = Translator.IsRu(killer);
            Hint(killer, ru ? "Убийство недоступно: для сервера ты не предатель." : "Kill unavailable: the server does not see you as an impostor.");
            return false;
        }
        Plugin.Logger.LogInfo($"RPC MurderPlayer: sender={killer.Data.PlayerName}({killer.Data.RoleType}) target={target.Data.PlayerName} ok={succeeded}");
        bool hidden = Opts.NeutralSuicide != null && Opts.NeutralSuicide.Value && RoleState.Assigned.TryGetValue(killer.PlayerId, out var kr) && RealImpNeutrals.Contains(kr.Id);
        try { if (hidden) target.RpcMurderPlayer(target, succeeded); else killer.RpcMurderPlayer(target, succeeded); }
        catch (Exception e) { Plugin.Logger.LogError("Kill: " + e.GetBaseException().Message); return false; }
        Plugin.Logger.LogInfo("RPC MurderPlayer sent");
        return true;
    }

    static string TaskWord(int n)
    {
        int m = n % 100;
        if (m >= 11 && m <= 14) return "заданий";
        switch (n % 10) { case 1: return "задание"; case 2: case 3: case 4: return "задания"; default: return "заданий"; }
    }

    public static string NameStatus(PlayerControl p, RoleDef r, bool ru)
    {
        int total = 0, done = 0;
        foreach (var t in p.Data.Tasks) { total++; if (t.Complete) done++; }
        byte id = p.PlayerId;
        if (Extra33.Handles(r.Id)) return Extra33.Short(p, r.Id, ru);
        switch (r.Id)
        {
            case "Peasant":
                {
                    if (ShieldUsed.Contains(id)) return ru ? "щит уже использован" : "shield already used";
                    if (ShieldArmed.Contains(id)) return ru ? "<color=#7CFC00>щит включён: отменит нападение</color>" : "<color=#7CFC00>shield on: cancels an attack</color>";
                    int left = Math.Max(0, Opts.PeasantTasks.Value - done);
                    return ru ? $"до щита осталось: {left} {TaskWord(left)}" : $"{left} task(s) left until the shield";
                }
            case "Vigilante":
                {
                    if (!VigUnlocked.Contains(id))
                    {
                        return ru ? $"до убийства: {VigLeft(p, true)}" : $"until kill: {VigLeft(p, false)}";
                    }
                    int pen = VigPenalty.TryGetValue(id, out var v) ? v : 0;
                    string cd = VigLastKill.TryGetValue(id, out var lk) && Time.time - lk < Opts.VigilanteCooldown.Value
                        ? (ru ? "перезарядка" : "cooling down") : (ru ? "убийство готово (питомец)" : "kill ready (pet)");
                    return $"{cd}, {(ru ? "штраф" : "penalty")} {pen}/{Opts.VigilanteMaxPenalty.Value}";
                }
            case "Spider":
                {
                    int n = Webs.Count(w => w.Owner == id);
                    int caught = Webs.Count(w => w.Owner == id && w.Trapped >= 0);
                    string s = ru ? $"паутин {n}/{Opts.SpiderMaxWebs.Value} (питомец у двери)" : $"webs {n}/{Opts.SpiderMaxWebs.Value} (pet at a door)";
                    return caught > 0 ? s + (ru ? $", в паутине: {caught}" : $", caught: {caught}") : s;
                }
            case "Cloud":
                {
                    var mine = Clouds.Where(cl0 => cl0.Owner == id).ToList();
                    string rooms = string.Join(", ", mine.Select(cl0 => (cl0.Storm && Opts.CloudStormMarker.Value ? "<color=#FFD400></color>" : "") + Utils.RoomName(cl0.Pos, ru)));
                    return ru ? $"облаков {mine.Count}/{Opts.CloudMax.Value}" + (rooms.Length > 0 ? ": " + rooms : "") + ", питомец"
                              : $"clouds {mine.Count}/{Opts.CloudMax.Value}" + (rooms.Length > 0 ? ": " + rooms : "") + ", pet";
                }
            case "Builder":
                return BuildWait(id) > 0 ? (ru ? $"дверь: перезарядка {BuildWait(id)} с" : $"door cooling down {BuildWait(id)} s") : (ru ? "закрытие двери готово (питомец)" : "door closing ready (pet)");
            case "Dyer":
                return ru ? "трупы твоих жертв закрашены" : "your victims' bodies are painted";
            case "Joker":
                return Joker.Short(p, ru);
            case "Alchemist":
                return Alchemist.Short(p, ru);
            case "Witch":
                return Witch.Short(p, ru);
            case "Freeman":
                return Freeman.Short(p, ru);
            case "God":
                return God.Short(p, ru);
            case "Herbalist": case "FortuneTeller":
                return MysticRoles.Short(p, r.Id, ru);
            case "Fat":
                return Fat.Short(p, ru);
        }
        string live = Live(r, id, ru);
        string hint = RoleHints.Get(r.Id, ru);
        if (live == null) return hint;
        return hint == null ? live : hint + "\n" + live;
    }

    public static string Live(RoleDef r, byte id, bool ru)
    {
        int cd = Math.Max(NR.CdLeft(r.Id, id), Math.Max(Roles30.CdLeft(r.Id, id), Roles75.CdLeft(r.Id, id)));
        int left = Math.Max(Roles30.UsesLeft(r.Id, id), Roles75.UsesLeft(r.Id, id));
        var parts = new List<string>();
        if (left == 0) return ru ? "<color=#FF5555>использования закончились</color>" : "<color=#FF5555>no uses left</color>";
        if (cd > 0) parts.Add(ru ? $"<color=#FFB74D>перезарядка {cd} с</color>" : $"<color=#FFB74D>cooldown {cd} s</color>");
        else if (cd == 0) parts.Add(ru ? "<color=#7CFC00>готово</color>" : "<color=#7CFC00>ready</color>");
        if (left > 0) parts.Add(ru ? $"осталось {left}" : $"{left} left");
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    public static string RoleLabel(byte playerId, RoleTypes vanilla, bool ru)
    {
        if (RoleState.Assigned.TryGetValue(playerId, out var r)) return ru ? r.Ru : r.En;
        return IsImpRole(vanilla) ? (ru ? "Предатель" : "Impostor") : (ru ? "Член экипажа" : "Crewmate");
    }

    public static bool CanKill(PlayerControl k, PlayerControl t)
        => k != null && t != null && k.Data != null && t.Data != null && !k.Data.IsDead && !t.Data.IsDead && k != t
           && MeetingHud.Instance == null;

    public static void OnPet(PlayerControl p, string src = "")
    {
        if (!Utils.AmHost || p == null || p.Data == null) return;
        if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
        if (petLast.TryGetValue(p.PlayerId, out var t) && Time.time - t < 1f) return;
        petLast[p.PlayerId] = Time.time;
        pendingAbility.Enqueue((p.PlayerId, src, Time.time));
        Plugin.Logger.LogInfo($"Ability queued [{src}]: {p.Data.PlayerName}");
    }

    public static void UseAbility(PlayerControl p, string src)
    {
        bool has = RoleState.Assigned.TryGetValue(p.PlayerId, out var role);
        Plugin.Logger.LogInfo($"Ability trigger [{src}]: {p.Data.PlayerName} role={(has ? role.Id : "none")} dead={p.Data.IsDead} meeting={MeetingHud.Instance != null} roundsAssigned={RoleState.Assigned.Count}");
        bool ru = Translator.IsRu(p);
        if (p.Data.IsDead || MeetingHud.Instance != null) return;
        if (!has)
        {
            if (p.AmOwner) Utils.Toast(ru ? "Питомец погладил, но у тебя нет роли мода (проверь шанс роли и /who)." : "Pet event received, but you have no mod role (check role chance and /who).");
            return;
        }
        if (Roles75.IsMuted(p)) { Hint(p, ru ? "Твоя способность заглушена." : "Your ability is silenced."); return; }
        if (p.AmOwner) Utils.Toast((ru ? "Способность: " : "Ability: ") + role.Colored(ru));
        Plugin.Logger.LogInfo($"Pet ability: {p.Data.PlayerName} ({role.Id})");
        if (role.Id == "Spider") PlaceWeb(p);
        else if (role.Id == "Vigilante") VigilantePetKill(p);
        else if (role.Id == "Builder") PlaceWall(p);
        else if (role.Id == "Welder") Welder.Use(p);
        else if (NR.Pet(p, role)) { }
        else if (role.Id == "Cloud") PlaceCloud(p);
        else if (p.AmOwner) Utils.Toast(ru ? "У этой роли нет активной способности (она работает сама)." : "This role has no active ability (it works passively).");
    }

    static bool CanBeTrapped(PlayerControl p)
    {
        if (p == null || p.Data == null || p.Data.IsDead || p.Data.Disconnected) return false;
        if (Webs.Any(w => w.Trapped == p.PlayerId)) return false;
        if (!Opts.SpiderPassThrough.Value) return true;
        if (IsImpRole(p.Data.RoleType)) return false;
        return !(RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && r.Tab == OptTab.Neutral);
    }

    public static void PlaceWeb(PlayerControl spider)
    {
        bool ru = Translator.IsRu(spider);
        if (Webs.Count(w => w.Owner == spider.PlayerId) >= Opts.SpiderMaxWebs.Value)
        { Hint(spider, ru ? "Достигнут лимит паутин." : "Web limit reached."); return; }

        Vector2 pos = spider.GetTruePosition();
        var doors = ShipStatus.Instance != null ? ShipStatus.Instance.AllDoors : null;
        if (doors != null && doors.Length > 0)
        {
            OpenableDoor best = null; float bd = 999f;
            foreach (var d in doors)
            {
                if (d == null) continue;
                float dd = Vector2.Distance(pos, (Vector2)d.transform.position);
                if (dd < bd) { bd = dd; best = d; }
            }
            if (best == null || bd > 2.5f)
            { Hint(spider, ru ? "Паутину можно ставить только в дверных проёмах." : "Webs can only be placed in doorways."); return; }
            pos = (Vector2)best.transform.position;
        }
        if (Webs.Any(w => Vector2.Distance(w.Pos, pos) < 0.8f))
        { Hint(spider, ru ? "Здесь уже есть паутина." : "There is already a web here."); return; }

        Webs.Add(new Web { Pos = pos, Owner = spider.PlayerId });
        Hint(spider, ru ? $"Паутина поставлена ({Webs.Count(w => w.Owner == spider.PlayerId)}/{Opts.SpiderMaxWebs.Value})." : "Web placed.");
    }

    static void FastTick()
    {
        if (Webs.Count == 0 || MeetingHud.Instance != null) return;
        float dt = Math.Min(Time.time - lastFast, 0.5f);
        if (dt < 0.25f) return;
        lastFast = Time.time;

        foreach (var web in Webs.ToList())
        {
            var spider = Utils.GetPlayer(web.Owner);
            if (web.Trapped < 0)
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p == null || p.PlayerId == web.Owner || !CanBeTrapped(p)) continue;
                    if (Vector2.Distance(p.GetTruePosition(), web.Pos) > 0.6f) continue;
                    web.Trapped = p.PlayerId; web.Rescue = 0f;
                    if (spider != null) Utils.SendSecret(spider, Translator.IsRu(spider) ? $"Звон нитей! В паутине: {p.Data.PlayerName}" : $"The threads ring! Caught: {p.Data.PlayerName}");
                    Hint(p, Translator.IsRu(p) ? "Ты запутался в паутине! Нужна помощь." : "You are stuck in a web! You need help.");
                    break;
                }
            }
            else
            {
                var victim = Utils.GetPlayer((byte)web.Trapped);
                if (victim == null || victim.Data == null || victim.Data.IsDead) { Webs.Remove(web); continue; }
                if (Vector2.Distance(victim.GetTruePosition(), web.Pos) > 0.4f && Time.time - web.LastSnap >= 0.5f)
                {
                    web.LastSnap = Time.time;
                    Plugin.Logger.LogInfo($"RPC SnapTo: victim={victim.PlayerId} web=({web.Pos.x:F1},{web.Pos.y:F1})");
                    Commands.Tp(victim, web.Pos);
                }

                bool rescuer = false;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p == null || p == victim || p.PlayerId == web.Owner || p.Data == null || p.Data.IsDead) continue;
                    if (Vector2.Distance(p.GetTruePosition(), web.Pos) < 1.1f) { rescuer = true; break; }
                }
                web.Rescue = rescuer ? web.Rescue + dt : 0f;
                if (web.Rescue >= 3f)
                {
                    Webs.Remove(web);
                    Hint(victim, Translator.IsRu(victim) ? "Тебя освободили!" : "You were freed!");
                    if (spider != null) Hint(spider, Translator.IsRu(spider) ? "Жертву освободили, паутина пропала." : "The victim was freed; the web is gone.");
                }
            }
        }
    }

    static void VigilantePetKill(PlayerControl vig)
    {
        bool ru = Translator.IsRu(vig);
        Plugin.Logger.LogInfo($"Vigilante pet: unlocked={VigUnlocked.Contains(vig.PlayerId)} cooldownLeft={(VigLastKill.TryGetValue(vig.PlayerId, out var l0) ? Opts.VigilanteCooldown.Value - (Time.time - l0) : 0f):F0}");
        if (!VigUnlocked.Contains(vig.PlayerId))
        { Hint(vig, ru ? $"Убийство закрыто: осталось {VigLeft(vig, true)}." : $"Kill locked: {VigLeft(vig, false)} left."); return; }
        if (VigLastKill.TryGetValue(vig.PlayerId, out var last) && Time.time - last < Opts.VigilanteCooldown.Value)
        { Hint(vig, ru ? $"Перезарядка: ещё {(int)(Opts.VigilanteCooldown.Value - (Time.time - last))} с." : "Cooling down."); return; }

        PlayerControl target = null; float best = 1.5f;
        Vector2 me = vig.GetTruePosition();
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p == vig || p.Data == null || p.Data.IsDead || p.Data.Disconnected) continue;
            float d = Vector2.Distance(me, p.GetTruePosition());
            if (d < best) { best = d; target = p; }
        }
        if (target == null) { Plugin.Logger.LogInfo("Vigilante pet: no target within 1.5"); Hint(vig, ru ? "Рядом никого нет." : "Nobody is close."); return; }
        VigilanteKill(vig, target);
    }

    public static void VigilanteKill(PlayerControl vig, PlayerControl target)
    {
        if (!CanKill(vig, target)) return;
        bool ru = Translator.IsRu(vig);
        VigLastKill[vig.PlayerId] = Time.time;

        if (ShieldArmed.Remove(target.PlayerId))
        {
            ShieldUsed.Add(target.PlayerId);
            RoleLogic.Kill(vig, target, false);
            Utils.SendSecret(target, ru ? "Защита сработала!" : "Shield triggered!");
            return;
        }

        bool goodKill = IsImpRole(target.Data.RoleType);
        if (!RoleLogic.Kill(vig, target, true)) { VigLastKill.Remove(vig.PlayerId); return; }
        if (goodKill) return;

        int pen = (VigPenalty.TryGetValue(vig.PlayerId, out var v) ? v : 0) + 1;
        VigPenalty[vig.PlayerId] = pen;
        int max = Opts.VigilanteMaxPenalty.Value;
        if (pen > max)
        {
            Utils.SendSecret(vig, ru ? $"Штрафов больше допустимого ({pen}/{max}) - ты погибаешь." : $"Too many penalties ({pen}/{max}) - you die.");
            RoleLogic.Kill(vig, vig, true);
        }
        else Utils.SendSecret(vig, ru ? $"Штраф за убийство невиновного: {pen}/{max}." : $"Penalty for killing an innocent: {pen}/{max}.");
    }

    static readonly List<(byte victim, byte killer, float at)> deathNotices = new();
    public static void QueueDeathNotice(PlayerControl victim, PlayerControl killer)
    {
        if (victim == null || killer == null || victim == killer) return;
        deathNotices.Add((victim.PlayerId, killer.PlayerId, Time.time + 1f));
    }

    static void PumpDeathNotices()
    {
        for (int i = deathNotices.Count - 1; i >= 0; i--)
        {
            var d = deathNotices[i];
            if (Time.time < d.at) continue;
            deathNotices.RemoveAt(i);
            var v = Utils.GetPlayer(d.victim); var k = Utils.GetPlayer(d.killer);
            if (v == null || k == null || v.Data == null || k.Data == null) continue;
            bool ru = Translator.IsRu(v);
            string role = RoleLabel(k.PlayerId, k.Data.RoleType, ru);
            string text = ru ? $"Тебя убил: {k.Data.PlayerName} ({role}). Ты призрак: чат только для призраков."
                             : $"You were killed by {k.Data.PlayerName} ({role}). You are a ghost: chat is ghosts-only.";
            Utils.SendSecret(v, text, 2, false);
            if (PlayerControl.AllPlayerControls.ToArray().Any(g => g != null && g.Data != null && g.Data.IsDead && g.PlayerId != v.PlayerId && !g.Data.Disconnected))
            {
                bool rg = Translator.GameIsRu;
                string vr = RoleLabel(v.PlayerId, v.Data.RoleType, rg), kr = RoleLabel(k.PlayerId, k.Data.RoleType, rg);
                Utils.SendGhosts(rg ? $"{v.Data.PlayerName} ({vr}) убит: {k.Data.PlayerName} ({kr})" : $"{v.Data.PlayerName} ({vr}) was killed by {k.Data.PlayerName} ({kr})");
            }
        }
    }

    public static void Tick()
    {
        if (!Utils.AmHost || AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
        PumpDeathNotices();
        while (pendingAbility.Count > 0)
        {
            var (aid, asrc, aat) = pendingAbility.Dequeue();
            if (Time.time - aat > 5f) continue;
            var ap = Utils.GetPlayer(aid);
            if (ap == null) continue;
            try { UseAbility(ap, asrc); }
            catch (Exception e) { Plugin.Logger.LogError("PendingAbility: " + e); }
        }

        if (AssignAt > 0 && Time.time >= AssignAt)
        {
            AssignAt = -1f;
            try { Assign(); Extras.OnAssigned(); NameInfo.Refresh(); } catch (Exception e) { Plugin.Logger.LogError("Assign: " + e); }
        }

        if (StartRevealAt > 0 && Time.time >= StartRevealAt)
        {
            StartRevealAt = -1f;
            foreach (var kv in RoleState.Assigned.ToList())
            {
                var p = Utils.GetPlayer(kv.Key);
                if (p == null || p.Data == null || p.Data.Disconnected) continue;
                try { Utils.SendSecret(p, RoleGuide.Card(p, kv.Value), 4); }
                catch (Exception e) { Plugin.Logger.LogWarning("RoleGuide.Card: " + e.Message); }
            }
        }

        FastTick();
        CloudTick();

        if (Time.time - lastTick < 1f) return;
        lastTick = Time.time;

        foreach (var kv in RoleState.Assigned.ToList())
        {
            var p = Utils.GetPlayer(kv.Key);
            if (p == null || p.Data == null || p.Data.IsDead) continue;
            int total = 0, done = 0;
            foreach (var t in p.Data.Tasks) { total++; if (t.Complete) done++; }

            if (kv.Value.Id == "Vigilante" && !VigUnlocked.Contains(kv.Key)
                && VigReady(kv.Key, total, done))
            {
                VigUnlocked.Add(kv.Key);
                VigPenalty[kv.Key] = 0;
                Utils.SendSecret(p, Translator.IsRu(p) ? "Убийство открыто: погладь питомца рядом с целью." : "Kill unlocked: pet your pet next to a target.");
            }

            if (kv.Value.Id == "Peasant" && !ShieldArmed.Contains(kv.Key) && !ShieldUsed.Contains(kv.Key) && done >= Opts.PeasantTasks.Value)
            {
                ShieldArmed.Add(kv.Key);
                Utils.SendSecret(p, Translator.IsRu(p) ? "Защита готова: ближайшее нападение на тебя будет отменено." : "Shield ready: the next attack on you will be cancelled.");
            }
        }
    }
}

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
public static class SelectRolesPatch
{
    public static void Postfix()
    {
        if (!Utils.AmHost) return;
        RoleLogic.Reset();
        RoleLogic.AssignAt = Time.time + 2f;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
public static class CheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        if (!Utils.AmHost || target == null) return true;
        if (RoleState.Assigned.TryGetValue(__instance.PlayerId, out var killerRole) && RoleLogic.RealImpNeutrals.Contains(killerRole.Id)) return false;
        if (NR.TryReflect(__instance, target)) return false;
        if (Roles30.BlockKill(__instance, target)) return false;
        if (Smoker.InOwnSmoke(target))
        {
            RoleLogic.Kill(__instance, target, false);
            Utils.SendSecret(target, Translator.IsRu(target) ? "Дымовая завеса спасла тебя!" : "The smoke screen saved you!");
            return false;
        }
        if (RoleLogic.ShieldArmed.Remove(target.PlayerId))
        {
            RoleLogic.ShieldUsed.Add(target.PlayerId);
            if (NR.ShieldGiver.TryGetValue(target.PlayerId, out var giverId) && Utils.GetPlayer(giverId) is PlayerControl gp)
                RoleLogic.Hint(gp, Translator.IsRu(gp) ? $"Твой щит спас {target.Data.PlayerName}!" : $"Your shield saved {target.Data.PlayerName}!");
            RoleLogic.Kill(__instance, target, false);
            Fat.AfterShield(target);
            Utils.SendSecret(target, Translator.IsRu(target) ? "Защита сработала!" : "Shield triggered!");
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class MurderPlayerPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags)
    {
        if (!Utils.AmHost || target == null) return;
        if ((resultFlags & MurderResultFlags.Succeeded) == 0) return;
        RoleLogic.QueueDeathNotice(target, __instance);
        Psy.OnKill(__instance);
        NR.OnKill(__instance, target);
        if (RoleLogic.Has(__instance, "Dyer"))
            RoleLogic.Painted[target.PlayerId] = Time.time + Opts.DyerDuration.Value;
        if (RoleLogic.Has(__instance, "Spider"))
        {
            var web = RoleLogic.Webs.Find(w => w.Trapped == target.PlayerId);
            if (web != null)
            {
                RoleLogic.Webs.Remove(web);
                RoleLogic.Eaten.Add(target.PlayerId);
                Utils.SendSecret(__instance, Translator.IsRu(__instance) ? "Ты съел жертву целиком." : "You ate the victim whole.");
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
public static class ReportDeadBodyPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
    {
        if (!Utils.AmHost || target == null) return true;
        bool eaten = RoleLogic.Eaten.Contains(target.PlayerId);
        Psy.OnReport(__instance);
        bool painted = RoleLogic.Painted.TryGetValue(target.PlayerId, out var until) && Time.time < until;
        if (eaten || painted)
        {
            RoleLogic.Hint(__instance, Translator.IsRu(__instance)
                ? (eaten ? "От тела почти ничего не осталось - доложить нельзя." : "Труп закрашен - доложить о нём пока нельзя.")
                : (eaten ? "Nothing is left of the body - it can't be reported." : "The body is painted over - it can't be reported yet."));
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class MeetingStartPatch
{
    public static void Postfix()
    {
        if (!Utils.AmHost) return;
        MeetingIds.Add();
        Utils.FlushDeferred();
        NameInfo.OnMeetingStart();
        RoleLogic.Painted.Clear();
        Welder.OnMeeting();
        NR.OnMeeting();
        Extras.OnMeeting();
        RoleLogic.Webs.Clear();
        RoleLogic.Clouds.Clear();
        foreach (var kv in RoleState.Assigned.ToList())
        {
            var p = Utils.GetPlayer(kv.Key);
            if (p == null || p.Data == null || p.Data.IsDead) continue;
            bool first = RoleLogic.Informed.Add(kv.Key);
            bool ru = Translator.IsRu(p);
            string st = RoleLogic.Status(p, kv.Value);
            string msg = first
                ? (ru ? "Твоя роль: " : "Your role: ") + (st ?? kv.Value.Colored(ru)) + (ru ? " /m - описание." : " /m for details.")
                : st;
            if (!string.IsNullOrWhiteSpace(msg)) Utils.SendSecret(p, msg.Trim());
        }
    }
}
