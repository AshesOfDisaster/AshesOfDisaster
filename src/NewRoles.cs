using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// Роли: мирные (Прораб, Логер, Штурман, Лозоходец, Сантехник), предатели (Близнец, Ищейка, Грибник),
// нейтралы (Шутник, Ворон, Перебежчик, Отражатель, Курильщик, Пинкман).
public static class NR
{
    static readonly System.Random rng = new();
    static readonly Dictionary<string, float> lastUse = new();      // "роль:id" -> время
    static readonly Dictionary<byte, int> uses = new();
    static readonly Dictionary<byte, Vector2> anchors = new();
    public static readonly Dictionary<byte, byte> ShieldGiver = new();      // кому выдан щит -> кто выдал
    static readonly Dictionary<byte, int> foreDone = new();
    static float nextTask, nextLog;
    static bool inTwin;
    static readonly List<(float t, byte who, Vector2 pos, bool anon)> logBook = new();
    static readonly Dictionary<byte, string> lastRoom = new();
    static readonly List<(byte victim, Vector2 pos)> bodies = new();
    static readonly HashSet<byte> reflectTried = new();
    static bool defectorDone;
    static float nextTick;

    public static void Reset() { lastUse.Clear(); uses.Clear(); anchors.Clear(); ShieldGiver.Clear(); foreDone.Clear(); Roles30.Reset(); Roles75.Reset(); logBook.Clear(); lastRoom.Clear(); bodies.Clear(); reflectTried.Clear(); defectorDone = false; }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool IsImp(PlayerControl p) => !RoleLogic.Demoted.Contains(p.PlayerId) && (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId));
    static string Room(Vector2 pos, bool ru) => Utils.RoomName(pos, ru);
    static void Say(PlayerControl p, string s) => RoleLogic.Hint(p, s);

    static bool Wait(string role, PlayerControl p, int cd, bool ru)
    {
        string k = role + ":" + p.PlayerId;
        if (lastUse.TryGetValue(k, out var l) && Time.time - l < cd)
        { Say(p, ru ? $"Перезарядка: ещё {Mathf.CeilToInt(cd - (Time.time - l))} с." : "Cooling down."); return true; }
        return false;
    }
    static void Stamp(string role, PlayerControl p) => lastUse[role + ":" + p.PlayerId] = Time.time;
    static int Used(byte id) => uses.TryGetValue(id, out var u) ? u : 0;

    static PlayerControl Nearest(PlayerControl me, float maxDist, Func<PlayerControl, bool> ok)
    {
        PlayerControl best = null; float bd = maxDist;
        var pos = me.GetTruePosition();
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (!Alive(o) || o.PlayerId == me.PlayerId || !ok(o)) continue;
            float d = Vector2.Distance(pos, o.GetTruePosition());
            if (d < bd) { bd = d; best = o; }
        }
        return best;
    }

    // ---------------------------------------------------------------- Статус (краткая сводка на собрании)
    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p); string n = r.Colored(ru);
        switch (r.Id)
        {
            case "Foreman": return ru ? $"{n}: каждое выполненное тобой задание с шансом {Opts.ForemanChance.Value}% засчитывает одно задание случайному члену экипажа." : $"{n}: each task you complete has a {Opts.ForemanChance.Value}% chance to complete a task of a random crewmate.";
            case "Logger": return ru ? $"{n}: журнал входов в комнаты. Питомец показывает последние записи ({Opts.LogLines.Value}), на каждом собрании ты получаешь свежие. Часть записей без имени." : $"{n}: a log of room entries. Pet shows the latest entries ({Opts.LogLines.Value}); you also get fresh ones at each meeting. Some entries are anonymous.";
            case "Twin": return ru ? $"{n}: после убийства вторым умирает ближайший к жертве игрок (в радиусе {Opts.TwinRadius.Value / 10f:F1} м). Срабатывает раз в {Opts.TwinCooldown.Value} с." : $"{n}: after a kill the player nearest to the victim (within {Opts.TwinRadius.Value / 10f:F1} m) dies too. Triggers once per {Opts.TwinCooldown.Value} s.";
            case "Plumber": return Plumber.Status(p, r);
            case "Shroomer": return Shroomer.Status(p, r);
            case "Smoker": return Smoker.Status(p, r);
            case "Pinkman": return Pinkman.Status(p, r);
            case "Dowser": return ru ? $"{n}: питомец показывает комнату ближайшего трупа и сколько живых игроков рядом (в радиусе {Opts.DowserRadius.Value} м). Перезарядка {Opts.DowserCooldown.Value} с." : $"{n}: pet shows the room of the nearest body and how many living players are nearby (within {Opts.DowserRadius.Value} m). Cooldown {Opts.DowserCooldown.Value} s.";
            case "Navigator": return ru ? $"{n}: питомец ставит метку, повторно — возвращает тебя к метке." : $"{n}: pet sets a mark, pet again returns you to it.";
            case "Hound": return ru ? $"{n}: питомец показывает, в какой комнате и как далеко ближайший член экипажа." : $"{n}: pet shows the room and distance of the nearest crewmate.";
            case "Prankster": return ru ? $"{n}: питомец меняет тебя местами с ближайшим игроком." : $"{n}: pet swaps you with the nearest player.";
            case "Crow": return ru ? $"{n}: питомец переносит тебя к последнему трупу. Осталось {Math.Max(0, Opts.CrowUses.Value - Used(p.PlayerId))}." : $"{n}: pet teleports you to the last body. Left: {Math.Max(0, Opts.CrowUses.Value - Used(p.PlayerId))}.";
            case "Defector": return ru ? $"{n}: на первом собрании ты получишь имя одного из предателей (с вероятностью {Opts.DefectorWrong.Value}% оно ложное)." : $"{n}: at the first meeting you get a name of an impostor ({Opts.DefectorWrong.Value}% chance it's false).";
            case "Reflector": return ru ? $"{n}: первое нападение на тебя с шансом {Opts.ReflectChance.Value}% отражается — убийца гибнет сам." : $"{n}: the first attack on you has a {Opts.ReflectChance.Value}% chance to be reflected — the killer dies.";
        }
        return Roles30.Status(p, r) ?? Roles75.Status(p, r);
    }

    // ---------------------------------------------------------------- Способности питомца
    public static bool Pet(PlayerControl p, RoleDef role)
    {
        bool ru = Translator.IsRu(p);
        switch (role.Id)
        {
            case "Plumber": return Plumber.Use(p);
            case "Pinkman": return Pinkman.Use(p);
            case "Smoker": return Smoker.Use(p);
            case "Shroomer": return Shroomer.Use(p);
            case "Dowser":
                {
                    if (Wait("Dowser", p, Opts.DowserCooldown.Value, ru)) return true;
                    Stamp("Dowser", p);
                    var me = p.GetTruePosition();
                    int near = PlayerControl.AllPlayerControls.ToArray().Count(o => Alive(o) && o.PlayerId != p.PlayerId && Vector2.Distance(me, o.GetTruePosition()) <= Opts.DowserRadius.Value);
                    string body;
                    if (bodies.Count == 0) body = ru ? "трупов нет" : "no bodies";
                    else
                    {
                        var b = bodies.OrderBy(x => Vector2.Distance(me, x.pos)).First();
                        body = (ru ? "ближайший труп: " : "nearest body: ") + Room(b.pos, ru) + $", {Vector2.Distance(me, b.pos):F0} " + (ru ? "м" : "m");
                    }
                    Say(p, ru ? $"Лоза дрожит: {body}. Рядом живых: {near}." : $"The rod trembles: {body}. Living nearby: {near}.");
                    return true;
                }
            case "Logger":
                {
                    if (Wait("Logger", p, Opts.LogCooldown.Value, ru)) return true;
                    Stamp("Logger", p);
                    ShowLog(p, Opts.LogLines.Value);
                    return true;
                }
            case "Navigator":
                {
                    if (!anchors.TryGetValue(p.PlayerId, out var a))
                    { anchors[p.PlayerId] = p.GetTruePosition(); Say(p, ru ? $"Метка поставлена: {Room(p.GetTruePosition(), true)}." : "Mark set."); return true; }
                    if (Wait("Navigator", p, Opts.NavCooldown.Value, ru)) return true;
                    Stamp("Navigator", p); anchors.Remove(p.PlayerId);
                    Commands.Tp(p, a);
                    Say(p, ru ? "Ты вернулся к метке." : "Returned to the mark.");
                    return true;
                }
            case "Hound":
                {
                    if (Wait("Hound", p, Opts.HoundCooldown.Value, ru)) return true;
                    var t = Nearest(p, 999f, o => !IsImp(o));
                    Stamp("Hound", p);
                    if (t == null) { Say(p, ru ? "Никого не найдено." : "Nobody found."); return true; }
                    float d = Vector2.Distance(p.GetTruePosition(), t.GetTruePosition());
                    Say(p, ru ? $"Ближайший член экипажа: {Room(t.GetTruePosition(), true)}, примерно {d:F0} м." : $"Nearest crewmate: {Room(t.GetTruePosition(), false)}, ~{d:F0} m.");
                    return true;
                }
            case "Prankster":
                {
                    if (Wait("Prankster", p, Opts.PrankCooldown.Value, ru)) return true;
                    var t = Nearest(p, 3.5f, o => true);
                    if (t == null) { Say(p, ru ? "Рядом никого нет." : "Nobody nearby."); return true; }
                    Stamp("Prankster", p);
                    Vector2 a = p.GetTruePosition(), b = t.GetTruePosition();
                    Commands.Tp(p, b); Commands.Tp(t, a);
                    Say(p, ru ? "Ты поменялся местами." : "Swapped places.");
                    return true;
                }
            case "Crow":
                {
                    if (Used(p.PlayerId) >= Opts.CrowUses.Value) { Say(p, ru ? "Перелёты закончились." : "No flights left."); return true; }
                    if (Wait("Crow", p, Opts.CrowCooldown.Value, ru)) return true;
                    if (bodies.Count == 0) { Say(p, ru ? "Трупов нет." : "No bodies."); return true; }
                    Stamp("Crow", p); uses[p.PlayerId] = Used(p.PlayerId) + 1;
                    var b = bodies[bodies.Count - 1];
                    Commands.Tp(p, b.pos + new Vector2(0.4f, 0f));
                    Say(p, ru ? $"Ты перелетел к трупу: {Room(b.pos, true)}." : $"You flew to a body: {Room(b.pos, false)}.");
                    return true;
                }
        }
        return Roles30.Pet(p, role) || Roles75.Pet(p, role);
    }

    // ---------------------------------------------------------------- Логер: журнал входов в комнаты
    public static string RoomKey(Vector2 pos)
    {
        try
        {
            var rooms = ShipStatus.Instance != null ? ShipStatus.Instance.AllRooms : null;
            if (rooms != null)
                foreach (var room in rooms)
                {
                    if (room == null || room.roomArea == null) continue;
                    string id = room.RoomId.ToString();
                    if (id == "Hallway" || id == "Outside" || id == "None") continue;
                    if (room.roomArea.OverlapPoint(pos)) return id;
                }
        }
        catch { }
        return null;
    }

    public static void AddFakeLog(byte who, Vector2 pos)
    {
        logBook.Add((Time.time, who, pos, false));
        if (logBook.Count > 80) logBook.RemoveAt(0);
    }

    public static void ResetCooldowns(byte id)
    {
        string suffix = ":" + id;
        foreach (var k in lastUse.Keys.Where(x => x.EndsWith(suffix)).ToList()) lastUse.Remove(k);
    }

    static void LogTick()
    {
        if (Time.time < nextLog) return;
        nextLog = Time.time + 0.5f;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null) return;
        if (!RoleState.Assigned.Values.Any(r => r.Id == "Logger")) return;
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (!Alive(o) || Roles75.Stealth(o.PlayerId)) continue;
            var pos = o.GetTruePosition();
            string key = RoomKey(pos);
            lastRoom.TryGetValue(o.PlayerId, out var prev);
            if (key == prev) continue;
            if (key == null) { lastRoom.Remove(o.PlayerId); continue; }      // вышел в коридор — запись не нужна, но следующий вход в комнату запишется
            lastRoom[o.PlayerId] = key;
            logBook.Add((Time.time, o.PlayerId, pos, rng.Next(100) < Opts.LogAnon.Value));
            if (logBook.Count > 80) logBook.RemoveAt(0);
        }
    }

    static void ShowLog(PlayerControl p, int lines)
    {
        bool ru = Translator.IsRu(p);
        var recent = logBook.Where(x => x.who != p.PlayerId).Reverse().Take(lines).Reverse().ToList();
        if (recent.Count == 0) { Utils.SendSecret(p, ru ? "Журнал пуст." : "The log is empty."); return; }
        var sb = new System.Text.StringBuilder(ru ? "Журнал:" : "Log:");
        foreach (var e in recent)
        {
            int ago = Mathf.RoundToInt(Time.time - e.t);
            var pl = Utils.GetPlayer(e.who);
            string nm = e.anon || pl == null ? (ru ? "Кто-то" : "Someone") : pl.Data.PlayerName;
            sb.Append($"\n{ago} {(ru ? "с назад" : "s ago")}: {nm} → {Room(e.pos, ru)}");
        }
        Utils.SendSecret(p, sb.ToString());
    }

    // ---------------------------------------------------------------- Хуки
    public static void OnKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            Roles30.OnKill(killer, target); Roles75.OnKill(killer, target);
            var vpos = target.GetTruePosition();
            bodies.Add((target.PlayerId, vpos));
            if (!inTwin && killer != null && killer.PlayerId != target.PlayerId && RoleLogic.Has(killer, "Twin")
                && !(lastUse.TryGetValue("Twin:" + killer.PlayerId, out var tl) && Time.time - tl < Opts.TwinCooldown.Value))
            {
                PlayerControl second = null; float bd = Opts.TwinRadius.Value / 10f;
                foreach (var o in PlayerControl.AllPlayerControls)
                {
                    if (!Alive(o) || o.PlayerId == killer.PlayerId || o.PlayerId == target.PlayerId || IsImp(o)) continue;
                    float d = Vector2.Distance(vpos, o.GetTruePosition());
                    if (d < bd) { bd = d; second = o; }
                }
                if (second != null)
                {
                    bool ru = Translator.IsRu(killer);
                    Stamp("Twin", killer);
                    if (RoleLogic.ShieldArmed.Remove(second.PlayerId))
                    {
                        RoleLogic.ShieldUsed.Add(second.PlayerId);
                        Utils.SendSecret(second, Translator.IsRu(second) ? "Защита сработала!" : "Shield triggered!");
                        Say(killer, ru ? $"Второй удар не прошёл: у {second.Data.PlayerName} сработала защита." : $"Second strike failed: {second.Data.PlayerName} had a shield.");
                    }
                    else
                    {
                        inTwin = true;
                        try { RoleLogic.Kill(killer, second, true); Say(killer, ru ? $"Близнец ударил второй раз: {second.Data.PlayerName}." : $"Twin struck again: {second.Data.PlayerName}."); }
                        finally { inTwin = false; }
                        Plugin.Logger.LogInfo($"Twin {killer.Data.PlayerName}: second victim {second.Data.PlayerName}");
                    }
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NR.OnKill: " + e.Message); }
    }

    /// Отражатель: true — убийство отменено
    public static bool TryReflect(PlayerControl attacker, PlayerControl target)
    {
        try
        {
            if (!RoleLogic.Has(target, "Reflector") || attacker == null || attacker.PlayerId == target.PlayerId) return false;
            if (!reflectTried.Add(target.PlayerId)) return false;
            if (rng.Next(100) >= Opts.ReflectChance.Value) return false;
            bool ru = Translator.IsRu(target);
            Say(target, ru ? "Нападение отражено!" : "Attack reflected!");
            Plugin.Logger.LogInfo($"Reflector {target.Data.PlayerName}: reflected attack of {attacker.Data.PlayerName}");
            RoleLogic.Kill(attacker, attacker, true);
            return true;
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NR.TryReflect: " + e.Message); return false; }
    }

    public static void OnMeeting()
    {
        try
        {
            Shroomer.Restore(); Smoker.OnMeeting(); Roles30.OnMeeting(); Roles75.OnMeeting();
            anchors.Clear(); bodies.Clear();
            var alive = PlayerControl.AllPlayerControls.ToArray().Where(Alive).ToList();
            foreach (var kv in RoleState.Assigned.ToList())
            {
                var p = Utils.GetPlayer(kv.Key);
                if (!Alive(p)) continue;
                bool ru = Translator.IsRu(p);
                if (kv.Value.Id == "Logger") ShowLog(p, Opts.LogLines.Value + 2);
                else if (kv.Value.Id == "Defector" && !defectorDone)
                {
                    defectorDone = true;
                    var imps = alive.Where(IsImp).ToList();
                    var inn = alive.Where(o => o.PlayerId != p.PlayerId && !IsImp(o)).ToList();
                    bool wrong = rng.Next(100) < Opts.DefectorWrong.Value;
                    var pick = (wrong || imps.Count == 0 ? inn : imps);
                    if (pick.Count == 0) continue;
                    var named = pick[rng.Next(pick.Count)];
                    Utils.SendSecret(p, ru ? $"Ты слышал, что предатель — {named.Data.PlayerName}. (информация может быть ложной)" : $"You heard that {named.Data.PlayerName} is an impostor. (could be false)");
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NR.OnMeeting: " + e.Message); }
    }

    /// Прораб: каждое выполненное им задание с шансом засчитывает задание случайному члену экипажа
    public static void Tick()
    {
        if (!Utils.AmHost) return;
        Shroomer.Tick(); Smoker.Tick(); Pinkman.Tick(); Roles30.Tick(); Roles75.Tick();
        try { LogTick(); } catch (Exception e) { Plugin.Logger.LogWarning("NR.LogTick: " + e.Message); }
        if (RoleState.Assigned.Count == 0 || Time.time < nextTask) return;
        nextTask = Time.time + 1f;
        if (MeetingHud.Instance != null) return;
        try
        {
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Foreman") continue;
                var f = Utils.GetPlayer(kv.Key);
                if (!Alive(f)) continue;
                int done = 0; foreach (var x in f.Data.Tasks) if (x.Complete) done++;
                int prev = foreDone.TryGetValue(kv.Key, out var pd) ? pd : done;
                foreDone[kv.Key] = done;
                for (int i = prev; i < done; i++)
                {
                    if (rng.Next(100) >= Opts.ForemanChance.Value) continue;
                    var cands = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && o.PlayerId != kv.Key && !IsImp(o) && o.Data.Tasks != null && o.Data.Tasks.ToArray().Any(x => !x.Complete)).ToList();
                    if (cands.Count == 0) break;
                    var t = cands[rng.Next(cands.Count)];
                    var task = t.Data.Tasks.ToArray().First(x => !x.Complete);
                    t.RpcCompleteTask(task.Id);
                    Plugin.Logger.LogInfo($"Foreman {f.Data.PlayerName}: completed task {task.Id} of {t.Data.PlayerName}");
                    Say(f, Translator.IsRu(f) ? "Ты помог закончить чужое задание." : "You helped finish someone's task.");
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NR.Tick: " + e.Message); }
    }
}
