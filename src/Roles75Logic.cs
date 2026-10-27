using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

public static partial class Roles75
{
    class Cam { public Vector2 Pos; public HashSet<byte> Seen = new(); }
    static readonly Dictionary<byte, List<Cam>> cams = new();
    static readonly Dictionary<byte, byte> tagged = new();
    static readonly Dictionary<byte, Vector2> beacon = new();
    static readonly Dictionary<byte, byte> link = new();
    static readonly Dictionary<byte, int> trophies = new();
    static readonly Dictionary<byte, Vector2> portalA = new();
    static readonly Dictionary<byte, Vector2> portalB = new();
    static readonly Dictionary<byte, float> hush = new();
    static readonly Dictionary<byte, float> muted = new();
    static readonly HashSet<byte> cursed = new();
    static readonly List<(byte owner, byte target, float at)> bombs = new();
    static readonly List<(byte owner, Vector2 pos, float at)> meteors = new();
    static readonly List<(byte owner, float end, int start)> prophecies = new();
    static readonly List<(byte owner, byte target, float at)> boomerangs = new();
    static readonly List<(byte id, Vector2 pos, float at)> tpQueue = new();
    static readonly Dictionary<byte, List<string>> photos = new();
    static readonly Dictionary<byte, Dictionary<byte, float>> nearTime = new();
    static readonly Dictionary<byte, int> taskCounts = new();
    static readonly List<(byte victim, Vector2 pos)> bodies = new();
    static readonly HashSet<byte> insiderDone = new();
    static int killCount;
    static float nextTick;

    public static List<(string key, byte owner, Vector2 pos, string ru, string en, string color)> Items()
    {
        var l = new List<(string key, byte owner, Vector2 pos, string ru, string en, string color)>();
        foreach (var kv in cams) foreach (var c in kv.Value) l.Add(("cm" + kv.Key + "_" + c.Pos.x.ToString("0.0") + "_" + c.Pos.y.ToString("0.0"), kv.Key, c.Pos, "Камера", "Camera", "#4DD0E1"));
        foreach (var kv in beacon) l.Add(("bc" + kv.Key + "_" + kv.Value.x.ToString("0.0") + "_" + kv.Value.y.ToString("0.0"), kv.Key, kv.Value, "Маяк", "Beacon", "#FFF176"));
        foreach (var kv in portalA) l.Add(("pa" + kv.Key + "_" + kv.Value.x.ToString("0.0") + "_" + kv.Value.y.ToString("0.0"), kv.Key, kv.Value, "Портал 1", "Portal 1", "#4FC3F7"));
        foreach (var kv in portalB) l.Add(("pb" + kv.Key + "_" + kv.Value.x.ToString("0.0") + "_" + kv.Value.y.ToString("0.0"), kv.Key, kv.Value, "Портал 2", "Portal 2", "#CE93D8"));
        foreach (var x in meteors) l.Add(("mt" + x.owner + "_" + x.pos.x.ToString("0.0") + "_" + x.pos.y.ToString("0.0"), x.owner, x.pos, "Метеор", "Meteor", "#FF7043"));
        return l;
    }

    public static void Reset()
    {
        lastUse.Clear(); lastCd.Clear(); useMax.Clear(); uses.Clear(); cams.Clear(); tagged.Clear(); beacon.Clear(); link.Clear(); trophies.Clear();
        portalA.Clear(); portalB.Clear(); hush.Clear(); muted.Clear(); cursed.Clear(); bombs.Clear(); meteors.Clear();
        prophecies.Clear(); boomerangs.Clear(); tpQueue.Clear(); photos.Clear(); nearTime.Clear(); taskCounts.Clear();
        bodies.Clear(); insiderDone.Clear(); killCount = 0;
    }

    public static bool Stealth(byte id) => hush.TryGetValue(id, out var until) && Time.time < until;

    public static bool IsMuted(PlayerControl p)
    {
        if (p == null) return false;
        if (muted.TryGetValue(p.PlayerId, out var until) && Time.time < until) return true;
        if (cursed.Remove(p.PlayerId)) { RoleLogic.FlashPlayer(p, 2f); return true; }
        return false;
    }

    public static void ResetCooldowns(byte id)
    {
        string suffix = ":" + id;
        foreach (var k in lastUse.Keys.Where(x => x.EndsWith(suffix)).ToList()) lastUse.Remove(k);
    }

    static void HeavySabotage(PlayerControl p, bool ru)
    {
        if (Wait("Meltdown", p, G("MeltCd"), ru) || NoUses("melt", p, G("MeltUses"), ru)) return;
        var ss = ShipStatus.Instance; if (ss == null || ss.Systems == null) return;
        try
        {
            foreach (var st in new[] { SystemTypes.Electrical, SystemTypes.Comms, SystemTypes.Reactor, SystemTypes.Laboratory, SystemTypes.LifeSupp, SystemTypes.HeliSabotage })
                if (ss.Systems.ContainsKey(st))
                {
                    var act = ss.Systems[st].TryCast<IActivatable>();
                    if (act != null && act.IsActive) { Say(p, T(ru, "Сейчас уже идёт саботаж.", "A sabotage is already active.")); return; }
                }
            var pool = new List<SystemTypes>();
            foreach (var st in new[] { SystemTypes.Reactor, SystemTypes.Laboratory, SystemTypes.HeliSabotage, SystemTypes.LifeSupp })
                if (ss.Systems.ContainsKey(st)) pool.Add(st);
            if (pool.Count == 0) { Say(p, T(ru, "На этой карте нет тяжёлых саботажей.", "No heavy sabotage on this map.")); return; }
            var pick = pool[rng.Next(pool.Count)];
            Stamp("Meltdown", p); Use("melt", p.PlayerId);
            ss.RpcUpdateSystem(SystemTypes.Sabotage, (byte)pick);
            Say(p, T(ru, "Расплав запущен!", "Meltdown started!"));
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Meltdown: " + e.Message); }
    }

    static void CloseDistantDoors(PlayerControl p, bool ru)
    {
        if (Wait("Distracter", p, G("DistractCd"), ru) || NoUses("distract", p, G("DistractUses"), ru)) return;
        var ss = ShipStatus.Instance; var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) { Say(p, T(ru, "На карте нет дверей.", "No doors on this map.")); return; }
        var me = p.GetTruePosition();
        for (int k = 0; k < 20; k++)
        {
            var door = doors[rng.Next(doors.Length)];
            if (door == null || Vector2.Distance(me, (Vector2)door.transform.position) < 15f) continue;
            try
            {
                ss.RpcCloseDoorsOfType(door.Room);
                Stamp("Distracter", p); Use("distract", p.PlayerId);
                Say(p, T(ru, $"Двери захлопнулись: {Room(door.transform.position, true)}.", "Doors slammed shut."));
            }
            catch (Exception e) { Plugin.Logger.LogWarning("Distracter: " + e.Message); }
            return;
        }
        Say(p, T(ru, "Подходящей далёкой комнаты не нашлось.", "No suitable distant room found."));
    }

    static void Explode(PlayerControl owner, Vector2 pos, float radius, string what)
    {
        bool ru = Translator.IsRu(owner);
        foreach (var v in Players())
        {
            if (v.PlayerId == owner.PlayerId || IsImp(v) || Vector2.Distance(v.GetTruePosition(), pos) > radius) continue;
            if (RoleLogic.ShieldArmed.Remove(v.PlayerId))
            {
                RoleLogic.ShieldUsed.Add(v.PlayerId);
                Secret(v, T(Translator.IsRu(v), "Защита сработала: тебя пытались взорвать!", "Shield triggered: someone tried to blow you up!"));
                Say(owner, T(ru, $"{v.Data.PlayerName} уцелел: сработал щит.", $"{v.Data.PlayerName} survived: a shield triggered."));
            }
            else RoleLogic.Kill(owner, v, true);
        }
    }

    public static bool Pet(PlayerControl p, RoleDef role)
    {
        bool ru = Translator.IsRu(p); byte id = p.PlayerId; var pos = p.GetTruePosition();
        switch (role.Id)
        {
            case "Watcher":
                {
                    if (!cams.TryGetValue(id, out var cl)) cams[id] = cl = new List<Cam>();
                    if (Wait("Watcher", p, G("WatcherCd"), ru)) return true;
                    if (cl.Count >= G("WatcherMax")) { Say(p, T(ru, "Все камеры уже стоят.", "All cameras are placed.")); return true; }
                    Stamp("Watcher", p); cl.Add(new Cam { Pos = pos });
                    Say(p, T(ru, $"Камера поставлена: {Room(pos, true)}.", $"Camera placed: {Room(pos, false)}."));
                    return true;
                }
            case "Radioman":
                {
                    if (Wait("Radioman", p, G("RadioCd"), ru)) return true;
                    float best = 999f;
                    foreach (var o in Players())
                        if (o.PlayerId != id && IsImp(o) && !Stealth(o.PlayerId)) best = Math.Min(best, Vector2.Distance(pos, o.GetTruePosition()));
                    Stamp("Radioman", p);
                    string word = best < 5f ? T(ru, "ГОРЯЧО", "HOT") : best < 12f ? T(ru, "тепло", "warm") : T(ru, "холодно", "cold");
                    Say(p, T(ru, $"Радар: {word}.", $"Radar: {word}."));
                    return true;
                }
            case "Tracker":
                {
                    var t = Nearest(p, 3f, o => true);
                    if (t != null)
                    {
                        if (Wait("Tracker", p, G("TrackerCd"), ru)) return true;
                        Stamp("Tracker", p); tagged[id] = t.PlayerId;
                        Say(p, T(ru, $"Метка поставлена: {t.Data.PlayerName}.", $"Tag placed on {t.Data.PlayerName}."));
                        return true;
                    }
                    if (!tagged.TryGetValue(id, out var tg)) { Say(p, T(ru, "Рядом никого нет, метки тоже нет.", "Nobody nearby and no tag.")); return true; }
                    var tp = Utils.GetPlayer(tg);
                    if (!Alive(tp)) Say(p, T(ru, "Меченый мёртв или вышел.", "The tagged player is dead or gone."));
                    else Say(p, T(ru, $"{tp.Data.PlayerName} сейчас: {Room(tp.GetTruePosition(), true)}.", $"{tp.Data.PlayerName} is now in {Room(tp.GetTruePosition(), false)}."));
                    return true;
                }
            case "Beacon":
                {
                    if (Wait("Beacon", p, G("BeaconCd"), ru)) return true;
                    Stamp("Beacon", p); beacon[id] = pos;
                    Say(p, T(ru, $"Маяк стоит: {Room(pos, true)}.", $"Beacon placed: {Room(pos, false)}."));
                    return true;
                }
            case "Sapper":
                {
                    if (NoUses("sapper", p, G("SapperUses"), ru) || Wait("Sapper", p, G("SapperCd"), ru)) return true;
                    Stamp("Sapper", p);
                    int n = Roles30.Defuse(pos, 3f);
                    if (n > 0) Use("sapper", id);
                    Say(p, n > 0 ? T(ru, $"Обезврежено: {n}.", $"Defused: {n}.") : T(ru, "Рядом ничего не найдено.", "Nothing found nearby."));
                    return true;
                }
            case "Herald":
                {
                    if (NoUses("herald", p, G("HeraldUses"), ru) || Wait("Herald", p, G("HeraldCd"), ru)) return true;
                    Stamp("Herald", p); Use("herald", id);
                    int near = Players().Count(o => o.PlayerId != id && Vector2.Distance(o.GetTruePosition(), pos) < 5f);
                    foreach (var o in Players())
                    {
                        bool r2 = Translator.IsRu(o);
                        Say(o, T(r2, $"Глашатай {p.Data.PlayerName}: я в комнате «{Room(pos, true)}», рядом со мной {near}.", $"Herald {p.Data.PlayerName}: I am in \"{Room(pos, false)}\", {near} near me."));
                    }
                    return true;
                }
            case "Coach":
                {
                    if (NoUses("coach", p, G("CoachUses"), ru) || Wait("Coach", p, G("CoachCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => !IsImp(o));
                    if (t == null) { Say(p, T(ru, "Рядом нет члена экипажа.", "No crewmate nearby.")); return true; }
                    Stamp("Coach", p); Use("coach", id);
                    ResetCooldowns(t.PlayerId); Roles30.ResetCooldowns(t.PlayerId); NR.ResetCooldowns(t.PlayerId); Plumber.ResetCooldowns(t.PlayerId); Pinkman.ResetCooldowns(t.PlayerId);
                    Say(p, T(ru, $"Ты потренировал {t.Data.PlayerName}: перезарядки сброшены.", $"You trained {t.Data.PlayerName}: cooldowns reset."));
                    Say(t, T(Translator.IsRu(t), "Тренер сбросил тебе перезарядки!", "The Coach reset your cooldowns!"));
                    return true;
                }
            case "Telegraphist":
                {
                    if (Wait("Telegraphist", p, G("TelegraphCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Telegraphist", p); link[id] = t.PlayerId;
                    Say(p, T(ru, $"Телеграф связал тебя с {t.Data.PlayerName}.", $"The telegraph links you with {t.Data.PlayerName}."));
                    Say(t, T(Translator.IsRu(t), "Кто-то связал тебя телеграфом.", "Someone linked you by telegraph."));
                    return true;
                }
            case "Banisher":
                {
                    if (NoUses("banish", p, G("BanishUses"), ru) || Wait("Banisher", p, G("BanishCd"), ru)) return true;
                    var t = Nearest(p, 4f, o => !IsImp(o));
                    if (t == null) { Say(p, T(ru, "Рядом нет члена экипажа.", "No crewmate nearby.")); return true; }
                    if (!RandomDoor(out var dest)) { Say(p, T(ru, "На карте нет дверей.", "No doors on this map.")); return true; }
                    Stamp("Banisher", p); Use("banish", id); Commands.Tp(t, dest);
                    Say(p, T(ru, $"{t.Data.PlayerName} выброшен: {Room(dest, true)}.", $"{t.Data.PlayerName} thrown to {Room(dest, false)}."));
                    return true;
                }
            case "Hush":
                {
                    if (Wait("Hush", p, G("HushCd"), ru)) return true;
                    Stamp("Hush", p); hush[id] = Time.time + G("HushSecs");
                    Say(p, T(ru, $"Тишина на {G("HushSecs")} с.", $"Silence for {G("HushSecs")} s."));
                    return true;
                }
            case "Bomber":
                {
                    if (NoUses("bomb", p, G("BombUses"), ru) || Wait("Bomber", p, G("BombCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => !IsImp(o));
                    if (t == null) { Say(p, T(ru, "Рядом нет члена экипажа.", "No crewmate nearby.")); return true; }
                    Stamp("Bomber", p); Use("bomb", id); bombs.Add((id, t.PlayerId, Time.time + G("BombSecs")));
                    Say(p, T(ru, $"Бомба на {t.Data.PlayerName}: взрыв через {G("BombSecs")} с.", $"Bomb on {t.Data.PlayerName}: it explodes in {G("BombSecs")} s."));
                    return true;
                }
            case "Spy":
                {
                    if (NoUses("spy", p, G("SpyUses"), ru) || Wait("Spy", p, G("SpyCd"), ru)) return true;
                    var t = Nearest(p, 4f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Spy", p); Use("spy", id);
                    string rn = RoleState.Assigned.TryGetValue(t.PlayerId, out var rd) ? rd.Colored(ru) : T(ru, "без роли мода", "no mod role");
                    Say(p, T(ru, $"{t.Data.PlayerName}: {rn}.", $"{t.Data.PlayerName}: {rn}."));
                    return true;
                }
            case "Spotter":
                {
                    if (Wait("Spotter", p, G("SpotterCd"), ru)) return true;
                    int ne = 0, nw = 0, se = 0, sw = 0;
                    foreach (var o in Players())
                    {
                        if (o.PlayerId == id || IsImp(o)) continue;
                        var d = o.GetTruePosition() - pos;
                        if (d.y >= 0) { if (d.x >= 0) ne++; else nw++; } else { if (d.x >= 0) se++; else sw++; }
                    }
                    Stamp("Spotter", p);
                    Say(p, T(ru, $"Мирных: СВ {ne}, СЗ {nw}, ЮВ {se}, ЮЗ {sw}.", $"Crewmates: NE {ne}, NW {nw}, SE {se}, SW {sw}."));
                    return true;
                }
            case "Gravedigger":
                {
                    if (NoUses("grave", p, G("GraveUses"), ru) || Wait("Gravedigger", p, G("GraveCd"), ru)) return true;
                    int bi = bodies.FindIndex(b => Vector2.Distance(b.pos, pos) < 2f);
                    if (bi < 0) { Say(p, T(ru, "Рядом нет тела.", "No body nearby.")); return true; }
                    Stamp("Gravedigger", p); Use("grave", id);
                    RoleLogic.Eaten.Add(bodies[bi].victim); bodies.RemoveAt(bi);
                    Say(p, T(ru, "Тело закопано: доложить о нём нельзя.", "Body buried: it can't be reported."));
                    return true;
                }
            case "Meltdown": HeavySabotage(p, ru); return true;
            case "Appraiser":
                {
                    if (Wait("Appraiser", p, G("AppraiserCd"), ru)) return true;
                    int n = 0; float bd = 999f; Vector2 bp = pos;
                    foreach (var o in Players())
                    {
                        if (o.PlayerId == id || !RoleLogic.ShieldArmed.Contains(o.PlayerId)) continue;
                        n++; float d = Vector2.Distance(pos, o.GetTruePosition());
                        if (d < bd) { bd = d; bp = o.GetTruePosition(); }
                    }
                    Stamp("Appraiser", p);
                    Say(p, n == 0 ? T(ru, "Щитов сейчас нет.", "No shields right now.") : T(ru, $"Щитов: {n}. Ближайший - «{Room(bp, true)}».", $"Shields: {n}. Nearest in \"{Room(bp, false)}\"."));
                    return true;
                }
            case "Silencer":
                {
                    if (NoUses("silence", p, G("SilenceUses"), ru) || Wait("Silencer", p, G("SilenceCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => !IsImp(o));
                    if (t == null) { Say(p, T(ru, "Рядом нет члена экипажа.", "No crewmate nearby.")); return true; }
                    Stamp("Silencer", p); Use("silence", id); muted[t.PlayerId] = Time.time + G("SilenceSecs");
                    Say(p, T(ru, $"{t.Data.PlayerName} не сможет использовать питомца {G("SilenceSecs")} с.", $"{t.Data.PlayerName} can't use his pet for {G("SilenceSecs")} s."));
                    return true;
                }
            case "Distracter": CloseDistantDoors(p, ru); return true;
            case "Collector":
                {
                    if (Wait("Collector", p, G("CollectorCd"), ru)) return true;
                    int bi = bodies.FindIndex(b => Vector2.Distance(b.pos, pos) < 2f);
                    if (bi < 0) { Say(p, T(ru, "Рядом нет тела.", "No body nearby.")); return true; }
                    bodies.RemoveAt(bi); Stamp("Collector", p);
                    int n = (trophies.TryGetValue(id, out var have) ? have : 0) + 1; trophies[id] = n;
                    if (n >= G("CollectorNeed"))
                    {
                        var imps = Players().Where(o => o.PlayerId != id && IsImp(o)).ToList();
                        if (imps.Count > 0) { Say(p, T(ru, $"Трофей {n}. Один из предателей - {imps[rng.Next(imps.Count)].Data.PlayerName}.", $"Trophy {n}. One of the impostors is {imps[rng.Next(imps.Count)].Data.PlayerName}.")); return true; }
                    }
                    Say(p, T(ru, $"Трофей забран: {n}/{G("CollectorNeed")}.", $"Trophy taken: {n}/{G("CollectorNeed")}."));
                    return true;
                }
            case "Whirlwind":
                {
                    if (NoUses("whirl", p, G("WhirlUses"), ru) || Wait("Whirlwind", p, G("WhirlCd"), ru)) return true;
                    Stamp("Whirlwind", p); Use("whirl", id); int n = 0;
                    foreach (var o in Players())
                    {
                        if (o.PlayerId == id || Vector2.Distance(o.GetTruePosition(), pos) > G("WhirlRadius")) continue;
                        if (RandomDoor(out var dest)) { Commands.Tp(o, dest); n++; }
                    }
                    Say(p, T(ru, $"Вихрь разбросал игроков: {n}.", $"The whirlwind scattered {n} players."));
                    return true;
                }
            case "Timestop":
                {
                    if (NoUses("stop", p, G("StopUses"), ru) || Wait("Timestop", p, G("StopCd"), ru)) return true;
                    Stamp("Timestop", p); Use("stop", id); int n = 0;
                    foreach (var o in Players())
                    {
                        if (o.PlayerId == id || Vector2.Distance(o.GetTruePosition(), pos) > G("StopRadius")) continue;
                        Roles30.FreezePlayer(o, G("StopSecs")); n++;
                    }
                    Say(p, T(ru, $"Время остановлено: замерло {n}.", $"Time stopped: {n} frozen."));
                    return true;
                }
            case "Oracle":
                {
                    if (Wait("Oracle", p, G("OracleCd"), ru)) return true;
                    Stamp("Oracle", p); prophecies.Add((id, Time.time + G("OracleSecs"), killCount));
                    Say(p, T(ru, $"Пророчество начато: итог через {G("OracleSecs")} с.", $"Prophecy started: the result in {G("OracleSecs")} s."));
                    return true;
                }
            case "Meteor":
                {
                    if (NoUses("meteor", p, G("MeteorUses"), ru) || Wait("Meteor", p, G("MeteorCd"), ru)) return true;
                    Stamp("Meteor", p); Use("meteor", id); meteors.Add((id, pos, Time.time + G("MeteorDelay")));
                    Say(p, T(ru, $"Точка отмечена: метеорит упадёт через {G("MeteorDelay")} с. Отойди!", $"Spot marked: the meteor hits in {G("MeteorDelay")} s. Move away!"));
                    return true;
                }
            case "Siren":
                {
                    if (NoUses("siren", p, G("SirenUses"), ru) || Wait("Siren", p, G("SirenCd"), ru)) return true;
                    Stamp("Siren", p); Use("siren", id); int n = 0;
                    foreach (var o in Players())
                    {
                        if (o.PlayerId == id || !IsImp(o) || Vector2.Distance(o.GetTruePosition(), pos) > G("SirenRadius")) continue;
                        RoleLogic.FlashPlayer(o, 3f); n++;
                    }
                    Say(p, T(ru, $"Сирена услышана предателями: {n}.", $"Impostors who heard the siren: {n}."));
                    return true;
                }
            case "Broker":
                {
                    if (NoUses("broker", p, G("BrokerUses"), ru) || Wait("Broker", p, G("BrokerCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Broker", p); Use("broker", id);
                    string rn = RoleState.Assigned.TryGetValue(t.PlayerId, out var rd) ? rd.Colored(ru) : T(ru, "без роли мода", "no mod role");
                    Say(p, T(ru, $"Сделка: {t.Data.PlayerName} - {rn}.", $"Deal: {t.Data.PlayerName} is {rn}."));
                    float bd = 999f; Vector2 bp = default; bool found = false;
                    foreach (var b in bodies) { float d = Vector2.Distance(t.GetTruePosition(), b.pos); if (d < bd) { bd = d; bp = b.pos; found = true; } }
                    bool r2 = Translator.IsRu(t);
                    Say(t, found ? T(r2, $"Брокер сообщил: ближайшее тело в «{Room(bp, true)}».", $"The Broker says: the nearest body is in \"{Room(bp, false)}\".") : T(r2, "Брокер сообщил: тел пока нет.", "The Broker says: no bodies yet."));
                    return true;
                }
            case "Portalist":
                {
                    bool hasA = portalA.TryGetValue(id, out var a), hasB = portalB.TryGetValue(id, out var b);
                    if (hasA && hasB)
                    {
                        bool atA = Vector2.Distance(pos, a) < 2.5f, atB = Vector2.Distance(pos, b) < 2.5f;
                        if (atA || atB)
                        {
                            if (Wait("Portalist", p, G("PortalCd"), ru)) return true;
                            Stamp("Portalist", p); Commands.Tp(p, atA ? b : a);
                            return true;
                        }
                        portalA[id] = pos; portalB.Remove(id);
                        Say(p, T(ru, "Новая пара порталов: первый стоит.", "A new portal pair: the first is placed."));
                        return true;
                    }
                    if (hasA) { portalB[id] = pos; Say(p, T(ru, $"Второй портал: {Room(pos, true)}. Теперь можно переноситься.", "Second portal placed. You can travel now.")); return true; }
                    portalA[id] = pos; Say(p, T(ru, $"Первый портал: {Room(pos, true)}.", $"First portal placed: {Room(pos, false)}."));
                    return true;
                }
            case "Photographer":
                {
                    if (NoUses("photo", p, G("PhotoUses"), ru) || Wait("Photographer", p, G("PhotoCd"), ru)) return true;
                    Stamp("Photographer", p); Use("photo", id);
                    var parts = Players().Where(o => o.PlayerId != id).Select(o => o.Data.PlayerName + " - " + Room(o.GetTruePosition(), ru)).ToList();
                    if (!photos.TryGetValue(id, out var pl)) photos[id] = pl = new List<string>();
                    pl.Add(string.Join("; ", parts));
                    Say(p, T(ru, "Снимок сделан: получишь его на собрании.", "Snapshot taken: you will get it at the meeting."));
                    return true;
                }
            case "Boomerang":
                {
                    if (NoUses("boom", p, G("BoomerangUses"), ru) || Wait("Boomerang", p, G("BoomerangCd"), ru)) return true;
                    var t = Nearest(p, 5f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Boomerang", p); Use("boom", id); boomerangs.Add((id, t.PlayerId, Time.time + G("BoomerangSecs")));
                    Say(p, T(ru, $"Бумеранг запущен в {t.Data.PlayerName}.", $"Boomerang thrown at {t.Data.PlayerName}."));
                    return true;
                }
            case "Sorcerer":
                {
                    if (NoUses("curse", p, G("CurseUses"), ru) || Wait("Sorcerer", p, G("CurseCd"), ru)) return true;
                    var t = Nearest(p, 3f, o => true);
                    if (t == null) { Say(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return true; }
                    Stamp("Sorcerer", p); Use("curse", id); cursed.Add(t.PlayerId);
                    Say(p, T(ru, $"{t.Data.PlayerName} проклят: его следующая способность сорвётся.", $"{t.Data.PlayerName} is cursed: his next ability fails."));
                    return true;
                }
        }
        return false;
    }

    public static void OnKill(PlayerControl killer, PlayerControl target)
    {
        try
        {
            if (target == null || target.Data == null) return;
            bool self = killer == null || killer.PlayerId == target.PlayerId;
            var tpos = target.GetTruePosition();
            bodies.Add((target.PlayerId, tpos));
            killCount++;
            float now = Time.time;

            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Seismograph" || kv.Key == target.PlayerId) continue;
                var s = Utils.GetPlayer(kv.Key); if (!Alive(s)) continue;
                var d = tpos - s.GetTruePosition(); float dist = d.magnitude; bool ru = Translator.IsRu(s);
                string band = dist < 8f ? T(ru, "совсем близко", "very close") : dist < 18f ? T(ru, "недалеко", "not far") : T(ru, "далеко", "far away");
                Say(s, T(ru, $"Толчок на {Compass(d, true)}: {band}.", $"A tremor to the {Compass(d, false)}: {band}."));
            }

            foreach (var kv in link.ToList())
            {
                byte other;
                if (kv.Key == target.PlayerId) other = kv.Value; else if (kv.Value == target.PlayerId) other = kv.Key; else continue;
                var o = Utils.GetPlayer(other); if (!Alive(o)) continue;
                bool ru = Translator.IsRu(o);
                Say(o, T(ru, $"Телеграмма: {target.Data.PlayerName} погиб в «{Room(tpos, true)}».", $"Telegram: {target.Data.PlayerName} died in \"{Room(tpos, false)}\"."));
                link.Remove(kv.Key);
            }

            if (self) return;

            bool neutralKiller = RoleState.Assigned.TryGetValue(killer.PlayerId, out var kr) && kr.Tab == OptTab.Neutral;
            foreach (var kv in RoleState.Assigned.ToList())
            {
                if (kv.Value.Id != "Chronicler" || kv.Key == target.PlayerId) continue;
                var c = Utils.GetPlayer(kv.Key); if (!Alive(c)) continue;
                bool ru = Translator.IsRu(c);
                string grp = neutralKiller ? T(ru, "нейтрал", "a neutral") : T(ru, "предатель", "an impostor");
                Say(c, T(ru, $"Летопись: погиб {target.Data.PlayerName}. Убийца - {grp}.", $"Chronicle: {target.Data.PlayerName} died. The killer is {grp}."));
            }

            if (RoleLogic.Has(killer, "Scatterer") && Ready("Scatterer", killer, G("ScatterCd")))
            {
                Stamp("Scatterer", killer);
                foreach (var o in Players())
                {
                    if (o.PlayerId == killer.PlayerId || IsImp(o) || Vector2.Distance(o.GetTruePosition(), tpos) > G("ScatterRadius")) continue;
                    if (RandomDoor(out var dest)) tpQueue.Add((o.PlayerId, dest, now + 0.5f));
                }
            }
            if (RoleLogic.Has(killer, "Fugitive") && Ready("Fugitive", killer, G("FugitiveCd")) && RandomDoor(out var fdest))
            {
                Stamp("Fugitive", killer); tpQueue.Add((killer.PlayerId, fdest, now + 1f));
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles75.OnKill: " + e.Message); }
    }

    public static void OnMeeting()
    {
        try
        {
            foreach (var kv in RoleState.Assigned.ToList())
            {
                var p = Utils.GetPlayer(kv.Key);
                if (p == null || p.Data == null || p.Data.Disconnected) continue;
                bool ru = Translator.IsRu(p); byte id = p.PlayerId; bool alive = !p.Data.IsDead;
                switch (kv.Value.Id)
                {
                    case "Watcher":
                        if (cams.TryGetValue(id, out var cl))
                            foreach (var cam in cl)
                            {
                                string names = cam.Seen.Count == 0 ? T(ru, "никого", "nobody") : string.Join(", ", cam.Seen.Select(PlayerName));
                                Secret(p, T(ru, $"Камера «{Room(cam.Pos, true)}»: {names}.", $"Camera \"{Room(cam.Pos, false)}\": {names}."));
                            }
                        break;
                    case "Companion":
                        if (nearTime.TryGetValue(id, out var nt) && nt.Count > 0)
                        {
                            var top = nt.OrderByDescending(x => x.Value).First();
                            Secret(p, T(ru, $"Дольше всех рядом был {PlayerName(top.Key)} ({Mathf.RoundToInt(top.Value)} с).", $"{PlayerName(top.Key)} stayed near you the longest ({Mathf.RoundToInt(top.Value)} s)."));
                        }
                        break;
                    case "Tracker":
                        if (tagged.TryGetValue(id, out var tg))
                        {
                            var tp = Utils.GetPlayer(tg);
                            Secret(p, Alive(tp) ? T(ru, $"Метка: {tp.Data.PlayerName} был в «{Room(tp.GetTruePosition(), true)}».", $"Tag: {tp.Data.PlayerName} was in \"{Room(tp.GetTruePosition(), false)}\".")
                                                : T(ru, "Метка: меченый мёртв или вышел.", "Tag: the tagged player is dead or gone."));
                        }
                        break;
                    case "Accountant":
                        if (alive)
                        {
                            PlayerControl worst = null; int wn = 9999;
                            foreach (var o in Players())
                            {
                                if (o.PlayerId == id || IsImp(o) || o.Data.Tasks == null) continue;
                                int total = 0, done = 0;
                                foreach (var t in o.Data.Tasks) { total++; if (t.Complete) done++; }
                                if (total > 0 && done < wn) { wn = done; worst = o; }
                            }
                            if (worst != null) Secret(p, T(ru, $"Меньше всех заданий выполнил {worst.Data.PlayerName}: {wn}.", $"{worst.Data.PlayerName} completed the fewest tasks: {wn}."));
                        }
                        break;
                    case "Photographer":
                        if (photos.TryGetValue(id, out var pl))
                            for (int i = 0; i < pl.Count; i++) Secret(p, T(ru, $"Снимок {i + 1}: {pl[i]}.", $"Snapshot {i + 1}: {pl[i]}."));
                        break;
                    case "Insider":
                        if (alive && insiderDone.Add(id))
                        {
                            var names = RoleState.Assigned.Where(x => x.Key != id && x.Value.Tab == OptTab.Neutral).Select(x => PlayerName(x.Key) + " (" + x.Value.Colored(ru) + ")").ToList();
                            Secret(p, names.Count == 0 ? T(ru, "Других нейтралов нет.", "There are no other neutrals.") : T(ru, "Нейтралы: ", "Neutrals: ") + string.Join(", ", names) + ".");
                        }
                        break;
                    case "Oracle":
                        foreach (var pr in prophecies.Where(x => x.owner == id).ToList())
                            Secret(p, T(ru, $"Пророчество оборвано собранием: убийств было {killCount - pr.start}.", $"The prophecy was cut short by the meeting: {killCount - pr.start} kills."));
                        break;
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles75.OnMeeting: " + e.Message); }
        cams.Clear(); beacon.Clear(); portalA.Clear(); portalB.Clear(); photos.Clear(); nearTime.Clear();
        bombs.Clear(); meteors.Clear(); prophecies.Clear(); boomerangs.Clear(); tpQueue.Clear(); bodies.Clear(); hush.Clear();
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        float dt = 0.25f; nextTick = Time.time + dt;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null || RoleState.Assigned.Count == 0) return;
        try
        {
            float now = Time.time;

            for (int i = tpQueue.Count - 1; i >= 0; i--)
                if (now >= tpQueue[i].at) { var t = Utils.GetPlayer(tpQueue[i].id); if (Alive(t)) Commands.Tp(t, tpQueue[i].pos); tpQueue.RemoveAt(i); }

            for (int i = bombs.Count - 1; i >= 0; i--)
                if (now >= bombs[i].at)
                {
                    var b = bombs[i]; bombs.RemoveAt(i);
                    var owner = Utils.GetPlayer(b.owner); var tgt = Utils.GetPlayer(b.target);
                    if (Alive(owner) && Alive(tgt)) Explode(owner, tgt.GetTruePosition(), G("BombRadius"), "bomb");
                }

            for (int i = meteors.Count - 1; i >= 0; i--)
                if (now >= meteors[i].at)
                {
                    var m = meteors[i]; meteors.RemoveAt(i);
                    var owner = Utils.GetPlayer(m.owner);
                    if (Alive(owner))
                    {
                        foreach (var v in Players())
                        {
                            if (v.PlayerId == owner.PlayerId || Vector2.Distance(v.GetTruePosition(), m.pos) > G("MeteorRadius")) continue;
                            if (RoleLogic.ShieldArmed.Remove(v.PlayerId))
                            {
                                RoleLogic.ShieldUsed.Add(v.PlayerId);
                                Secret(v, T(Translator.IsRu(v), "Защита сработала: тебя задел метеорит!", "Shield triggered: a meteor hit you!"));
                            }
                            else RoleLogic.Kill(owner, v, true);
                        }
                    }
                }

            for (int i = prophecies.Count - 1; i >= 0; i--)
                if (now >= prophecies[i].end)
                {
                    var pr = prophecies[i]; prophecies.RemoveAt(i);
                    var o = Utils.GetPlayer(pr.owner);
                    if (Alive(o)) Say(o, T(Translator.IsRu(o), $"Пророчество сбылось: за {G("OracleSecs")} с убийств - {killCount - pr.start}.", $"The prophecy came true: {killCount - pr.start} kills in {G("OracleSecs")} s."));
                }

            for (int i = boomerangs.Count - 1; i >= 0; i--)
                if (now >= boomerangs[i].at)
                {
                    var b = boomerangs[i]; boomerangs.RemoveAt(i);
                    var owner = Utils.GetPlayer(b.owner); var tgt = Utils.GetPlayer(b.target);
                    if (Alive(owner) && Alive(tgt)) { Commands.Tp(tgt, owner.GetTruePosition()); Say(tgt, T(Translator.IsRu(tgt), "Бумеранг вернул тебя к владельцу.", "A boomerang carried you back to its owner.")); }
                }

            var players = Players();
            foreach (var kv in RoleState.Assigned.ToList())
            {
                var p = Utils.GetPlayer(kv.Key);
                if (!Alive(p)) continue;
                byte id = p.PlayerId; var pos = p.GetTruePosition(); bool ru = Translator.IsRu(p);
                switch (kv.Value.Id)
                {
                    case "Watcher":
                        if (cams.TryGetValue(id, out var cl))
                            foreach (var cam in cl)
                                foreach (var o in players)
                                    if (o.PlayerId != id && !Stealth(o.PlayerId) && Vector2.Distance(o.GetTruePosition(), cam.Pos) < 3.5f) cam.Seen.Add(o.PlayerId);
                        break;
                    case "Beacon":
                        if (beacon.TryGetValue(id, out var bp) && Ready("BeaconAlert", p, 10))
                            foreach (var o in players)
                                if (o.PlayerId != id && IsImp(o) && !Stealth(o.PlayerId) && Vector2.Distance(o.GetTruePosition(), bp) < 4f)
                                {
                                    Stamp("BeaconAlert", p);
                                    Say(p, T(ru, $"Маяк: предатель вошёл в «{Room(bp, true)}»!", $"Beacon: an impostor entered \"{Room(bp, false)}\"!"));
                                    break;
                                }
                        break;
                    case "Companion":
                        {
                            if (!nearTime.TryGetValue(id, out var nt)) nearTime[id] = nt = new Dictionary<byte, float>();
                            foreach (var o in players)
                                if (o.PlayerId != id && Vector2.Distance(o.GetTruePosition(), pos) < G("CompanionRadius"))
                                    nt[o.PlayerId] = (nt.TryGetValue(o.PlayerId, out var tv) ? tv : 0f) + dt;
                            break;
                        }
                }
            }

            var overseers = players.Where(o => RoleLogic.Has(o, "Overseer")).ToList();
            foreach (var o in players)
            {
                if (IsImp(o) || o.Data.Tasks == null) continue;
                int done = 0; foreach (var t in o.Data.Tasks) if (t.Complete) done++;
                bool known = taskCounts.TryGetValue(o.PlayerId, out var prev);
                taskCounts[o.PlayerId] = done;
                if (!known || done <= prev) continue;
                foreach (var ov in overseers)
                {
                    if (ov.PlayerId == o.PlayerId || Vector2.Distance(ov.GetTruePosition(), o.GetTruePosition()) > G("OverseerRadius")) continue;
                    bool r2 = Translator.IsRu(ov);
                    Say(ov, T(r2, $"{o.Data.PlayerName} закончил задание в «{Room(o.GetTruePosition(), true)}».", $"{o.Data.PlayerName} finished a task in \"{Room(o.GetTruePosition(), false)}\"."));
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Roles75.Tick: " + e.Message); }
    }
}
