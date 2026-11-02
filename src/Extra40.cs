using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// 40 новых ролей: по 10 мирных, предателей, нейтралов и мистиков.
public static partial class Extra33
{
    class Spot { public string Kind; public byte Owner, Target; public Vector2 Pos; public float Until, Next; public List<byte> Seen = new(); }
    static readonly List<Spot> spots = new();
    static readonly Dictionary<byte, float> scapegoats = new();
    static readonly Dictionary<byte, int> reapKills = new();
    static readonly Dictionary<byte, float> reapLast = new();
    static readonly List<float> deathTimes = new();
    static readonly HashSet<byte> looted = new();
    static readonly Dictionary<byte, float> wounded = new();
    static float lastDeathAt = -999f;
    static int deathCount;

    static void Clear40() { spots.Clear(); scapegoats.Clear(); reapKills.Clear(); reapLast.Clear(); lastDeathAt = -999f; deathCount = 0; deathTimes.Clear(); looted.Clear(); wounded.Clear(); psel.Clear(); meetN = 0; }

    static void Death40(PlayerControl killer, PlayerControl target)
    {
        lastDeathAt = Time.time; deathCount++; deathTimes.Add(Time.time);
        if (target == null) return;
        foreach (var kv in scapegoats.ToList())
        {
            if (Time.time > kv.Value) { scapegoats.Remove(kv.Key); continue; }
            var sg = Utils.GetPlayer(kv.Key);
            if (!Alive(sg) || sg.PlayerId == target.PlayerId) continue;
            scapegoats.Remove(kv.Key);
            foreach (var o in Others(sg)) Utils.SendSecret(o, T(Translator.IsRu(o), $"Рядом с телом заметили: {Nm(sg)}.", $"{Nm(sg)} was seen near the body."));
        }
    }

    static void Tick40(float now)
    {
        foreach (var sp in spots.ToList())
        {
            var owner = Utils.GetPlayer(sp.Owner);
            if (!Alive(owner) || now > sp.Until) { spots.Remove(sp); continue; }
            bool ru = Translator.IsRu(owner);
            if (sp.Kind == "alarm")
            {
                var o = Others(owner).FirstOrDefault(x => Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 2f);
                if (o == null) continue;
                spots.Remove(sp);
                H(owner, T(ru, $"Сигналка сработала: рядом с меткой {Nm(o)}.", $"The tripwire went off: {Nm(o)} is by the mark."));
            }
            else if (sp.Kind == "stash")
            {
                var o = Others(owner).FirstOrDefault(x => Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 1.5f);
                if (o == null) continue;
                spots.Remove(sp);
                RoleLogic.ShieldArmed.Add(o.PlayerId); RoleLogic.ShieldUsed.Remove(o.PlayerId);
                Utils.SendSecret(o, T(Translator.IsRu(o), "Ты нашёл тайник: у тебя щит!", "You found a stash: you have a shield!"));
                H(owner, T(ru, $"Твой тайник нашёл {Nm(o)}.", $"{Nm(o)} found your stash."));
            }
            else if (sp.Kind == "spikes")
            {
                var o = Others(owner).FirstOrDefault(x => !IsImp(x) && Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 1.5f);
                if (o == null) continue;
                spots.Remove(sp);
                RoleLogic.ShieldArmed.Remove(o.PlayerId);
                H(owner, T(ru, $"На шипы наступил {Nm(o)}, щит снят.", $"{Nm(o)} stepped on the spikes, the shield is gone."));
            }
            else if (sp.Kind == "fate")
            {
                if (now < sp.Next) continue;
                spots.Remove(sp);
                var tg = Utils.GetPlayer(sp.Target);
                bool alive = Alive(tg);
                H(owner, T(ru, alive ? $"Судьба {Nm(tg)}: жив." : "Судьба: игрок погиб.", alive ? $"Fate of {Nm(tg)}: alive." : "Fate: the player is dead."));
            }
            else if (sp.Kind == "tag")
            {
                var tg = Utils.GetPlayer(sp.Target);
                if (!Alive(tg)) { spots.Remove(sp); continue; }
                if (now < sp.Next) continue;
                sp.Next = now + 5f;
                var me = owner.GetTruePosition(); var tp = tg.GetTruePosition();
                H(owner, T(ru, $"{Nm(tg)}: {Where(true, me, tp)}, {Mathf.RoundToInt(Vector2.Distance(me, tp))} м.", $"{Nm(tg)}: {Where(false, me, tp)}, {Mathf.RoundToInt(Vector2.Distance(me, tp))} m."));
            }
        }
    }

    static string Dir8(Vector2 from, Vector2 to)
    {
        var d = to - from;
        if (d.magnitude < 0.5f) return "прямо здесь";
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        string[] n = { "восток", "северо-восток", "север", "северо-запад", "запад", "юго-запад", "юг", "юго-восток" };
        return n[((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8];
    }

    static string Dir8En(Vector2 from, Vector2 to)
    {
        var d = to - from;
        if (d.magnitude < 0.5f) return "right here";
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        string[] n = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        return n[((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8];
    }

    static string Where(bool ru, Vector2 from, Vector2 to) => ru ? Dir8(from, to) : Dir8En(from, to);

    static bool IsCrewSide(PlayerControl o)
        => o != null && !IsImp(o) && RoleState.Assigned.TryGetValue(o.PlayerId, out var r) && r.Tab != OptTab.Neutral && r.Tab != OptTab.Impostor;

    static bool NeutralKill(PlayerControl p, bool ru, float range, Func<PlayerControl, bool> ok, string noneRu, string noneEn)
    {
        var t = Nearest(p, range, ok);
        if (t == null) { H(p, T(ru, noneRu, noneEn)); return false; }
        return Strike(p, t);
    }

    static int Around(PlayerControl p, float r) => Others(p).Count(o => Vector2.Distance(o.GetTruePosition(), p.GetTruePosition()) <= r);

    static void Define40()
    {
        const OptTab N = OptTab.Neutral, M = OptTab.Mystic, C = OptTab.Crewmate, I = OptTab.Impostor;

        // ---------------- Мирные ----------------
        S("Lantern", "Фонарщик", "Lantern", "#FFD54F", C, 20, 0, "питомец: сколько людей рядом", "pet: how many people are near",
            "Мирный. Показывает, сколько живых игроков сейчас в радиусе 7 метров от тебя. Перезарядка 20 с. [Питомец]",
            "Crewmate. Shows how many living players are within 7 meters of you right now. Cooldown 20 s. [Pet]",
            (p, ru) => { int n = Around(p, 7f); H(p, T(ru, $"Рядом с тобой игроков: {n}.", $"Players near you: {n}.")); return true; });
        S("Dosimeter", "Дозиметр", "Dosimeter", "#FFF176", C, 30, 0, "питомец: сколько смертей за минуту", "pet: deaths in the last minute",
            "Мирный. Показывает, сколько игроков погибло за последние 60 секунд. Перезарядка 30 с. [Питомец]",
            "Crewmate. Shows how many players died in the last 60 seconds. Cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                int n = deathTimes.Count(x => Time.time - x <= 60f);
                H(p, T(ru, $"За последнюю минуту погибло: {n}.", $"Deaths in the last minute: {n}."));
                return true;
            });
        S("Xray", "Просвет", "X-ray", "#80DEEA", C, 40, 3, "питомец: есть ли у игрока щит", "pet: does the player have a shield",
            "Мирный. Просвечивает ближайшего игрока (до 2,5 м) и говорит, есть ли у него щит. Использований: 3, перезарядка 40 с. [Питомец]",
            "Crewmate. X-rays the nearest player (within 2.5 m) and tells whether he has a shield. Uses: 3, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool sh = RoleLogic.ShieldArmed.Contains(t.PlayerId);
                H(p, T(ru, sh ? $"У {Nm(t)} есть щит." : $"У {Nm(t)} нет щита.", sh ? $"{Nm(t)} has a shield." : $"{Nm(t)} has no shield."));
                return true;
            });
        S("Landmark", "Веха", "Landmark", "#AED581", C, 15, 0, "питомец: веха и путь назад", "pet: a landmark and the way back",
            "Мирный. Первое нажатие ставит веху на твоём месте, следующие показывают направление и расстояние до неё. Если нажать у самой вехи (ближе 1,5 м), она переставится на новое место. Перезарядка 15 с. [Питомец]",
            "Crewmate. The first press sets a landmark where you stand, the next ones show the direction and distance to it. If you press right next to it (within 1.5 m), it moves to the new spot. Cooldown 15 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var old = spots.FirstOrDefault(x => x.Kind == "mark" && x.Owner == p.PlayerId);
                if (old != null && Vector2.Distance(me, old.Pos) > 1.5f)
                {
                    H(p, T(ru, $"Веха: {Where(true, me, old.Pos)}, {Mathf.RoundToInt(Vector2.Distance(me, old.Pos))} м.", $"Landmark: {Where(false, me, old.Pos)}, {Mathf.RoundToInt(Vector2.Distance(me, old.Pos))} m."));
                    return true;
                }
                spots.RemoveAll(x => x.Kind == "mark" && x.Owner == p.PlayerId);
                spots.Add(new Spot { Kind = "mark", Owner = p.PlayerId, Pos = me, Until = Time.time + 9999f });
                H(p, T(ru, "Веха поставлена здесь.", "Landmark set here."));
                return true;
            });
        S("Compass", "Компас", "Compass", "#26A69A", C, 40, 4, "питомец: где ближайшее тело", "pet: where the nearest body is",
            "Мирный. Показывает направление на ближайшее тело и расстояние до него. Использований: 4, перезарядка 40 с. [Питомец]",
            "Crewmate. Shows the direction and distance to the nearest body. Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = bodies.Where(b => !eaten.Contains(b.id)).ToList();
                if (list.Count == 0) { H(p, T(ru, "Тел на карте нет.", "No bodies on the map.")); return false; }
                var nb = list.OrderBy(b => Vector2.Distance(b.pos, me)).First();
                H(p, T(ru, $"Ближайшее тело: {Where(true, me, nb.pos)}, примерно {Mathf.RoundToInt(Vector2.Distance(me, nb.pos))} м.", $"Nearest body: {Where(false, me, nb.pos)}, about {Mathf.RoundToInt(Vector2.Distance(me, nb.pos))} m."));
                return true;
            });
        S("Investigator", "Следователь", "Investigator", "#7986CB", C, 35, 3, "питомец: был ли рядом с телом", "pet: was he near a body",
            "Мирный. Проверяет ближайшего игрока (до 2,5 м): стоит ли он рядом с телом (до 5 м). Это только улика, не приговор. Использований: 3, перезарядка 35 с. [Питомец]",
            "Crewmate. Checks the nearest player (within 2.5 m): is he next to a body (within 5 m). Only a clue, not a verdict. Uses: 3, cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool near = bodies.Any(b => !eaten.Contains(b.id) && Vector2.Distance(b.pos, t.GetTruePosition()) <= 5f);
                H(p, T(ru, near ? $"{Nm(t)} стоит рядом с телом." : $"{Nm(t)} не рядом с телом.", near ? $"{Nm(t)} is next to a body." : $"{Nm(t)} is not near a body."));
                return true;
            });
        S("Seer", "Вещун", "Seer", "#9575CD", C, 60, 3, "питомец: назвать точно мирного", "pet: name a sure crewmate",
            "Мирный. Называет случайного живого игрока, который точно на стороне мирных. Использований: 3, перезарядка 60 с. [Питомец]",
            "Crewmate. Names a random living player who is surely on the crew side. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var l = Others(p).Where(IsCrewSide).ToList();
                if (l.Count == 0) { H(p, T(ru, "Некого назвать.", "Nobody to name.")); return false; }
                var t = l[rng.Next(l.Count)];
                H(p, T(ru, $"{Nm(t)} точно мирный.", $"{Nm(t)} is surely crew."));
                return true;
            });
        S("Tripwire", "Сигналка", "Tripwire", "#EF9A9A", C, 40, 3, "питомец: поставить сигналку", "pet: set a tripwire",
            "Мирный. Питомец ставит невидимую сигналку на твоём месте (до 90 секунд). Первый, кто подойдёт к ней ближе 2 м, назовётся тебе по имени. Использований: 3, перезарядка 40 с. [Питомец]",
            "Crewmate. The pet sets an invisible tripwire where you stand (up to 90 seconds). The first one to come within 2 m of it is named to you. Uses: 3, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                spots.Add(new Spot { Kind = "alarm", Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + 90f });
                H(p, T(ru, "Сигналка поставлена на 90 секунд.", "Tripwire set for 90 seconds."));
                return true;
            });
        S("Stash", "Тайник", "Stash", "#FFCC80", C, 60, 2, "питомец: спрятать щит", "pet: hide a shield",
            "Мирный. Питомец прячет на твоём месте тайник со щитом (до 120 секунд). Первый, кто подойдёт ближе 1,5 м, получает щит, а ты узнаёшь, кто. Использований: 2, перезарядка 60 с. [Питомец]",
            "Crewmate. The pet hides a shield stash where you stand (up to 120 seconds). The first one to come within 1.5 m gets the shield and you learn who. Uses: 2, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                spots.Add(new Spot { Kind = "stash", Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + 120f });
                H(p, T(ru, "Тайник спрятан на 120 секунд.", "Stash hidden for 120 seconds."));
                return true;
            });
        S("Chrono", "Хронометр", "Chronometer", "#B0BEC5", C, 20, 0, "питомец: сколько прошло с последней смерти", "pet: time since the last death",
            "Мирный. Показывает, сколько секунд прошло с последней смерти игрока. Перезарядка 20 с. [Питомец]",
            "Crewmate. Shows how many seconds have passed since a player last died. Cooldown 20 s. [Pet]",
            (p, ru) =>
            {
                if (lastDeathAt < 0) { H(p, T(ru, "Ещё никто не погиб.", "Nobody has died yet.")); return true; }
                int sec = Mathf.RoundToInt(Time.time - lastDeathAt);
                H(p, T(ru, $"С последней смерти прошло {sec} с.", $"{sec} s since the last death."));
                return true;
            });

        // ---------------- Предатели ----------------
        S("Discharger", "Разрядник", "Discharger", "#FFEE58", I, 60, 0, "питомец: сорвать щиты вокруг", "pet: break the shields around",
            "Предатель. Питомец снимает щиты со всех не-предателей в радиусе 6 м. Перезарядка 60 с. [Питомец]",
            "Impostor. The pet strips the shields off every non-impostor within 6 m. Cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && RoleLogic.ShieldArmed.Contains(o.PlayerId) && Vector2.Distance(o.GetTruePosition(), me) <= 6f).ToList();
                if (l.Count == 0) { H(p, T(ru, "Щитов рядом нет.", "No shields nearby.")); return false; }
                foreach (var o in l) RoleLogic.ShieldArmed.Remove(o.PlayerId);
                H(p, T(ru, $"Щитов снято: {l.Count}.", $"Shields removed: {l.Count}."));
                return true;
            });
        S("Handout", "Подачка", "Handout", "#A5D6A7", I, 45, 0, "питомец: ложный щит", "pet: a fake shield",
            "Предатель. Питомец говорит ближайшему не-предателю (до 2,5 м), что у него щит. Щита на самом деле нет. Перезарядка 45 с. [Питомец]",
            "Impostor. The pet tells the nearest non-impostor (within 2.5 m) that he has a shield. There is no real shield. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                Utils.SendSecret(t, T(Translator.IsRu(t), "Ты получил щит!", "You got a shield!"));
                H(p, T(ru, $"{Nm(t)} думает, что у него щит.", $"{Nm(t)} thinks he has a shield."));
                return true;
            });
        S("Shadower", "Следящий", "Shadower", "#546E7A", I, 60, 0, "питомец: метка слежки на 40 с", "pet: a tracking tag for 40 s",
            "Предатель. Питомец ставит метку слежки на ближайшего не-предателя (до 3 м). 40 секунд каждые 5 секунд ты видишь, где он (направление и расстояние). Перезарядка 60 с. [Питомец]",
            "Impostor. The pet tags the nearest non-impostor (within 3 m). For 40 seconds every 5 seconds you see where he is (direction and distance). Cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !IsImp(o));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                spots.RemoveAll(x => x.Kind == "tag" && x.Owner == p.PlayerId);
                spots.Add(new Spot { Kind = "tag", Owner = p.PlayerId, Target = t.PlayerId, Until = Time.time + 40f, Next = 0f });
                H(p, T(ru, $"Метка слежки на {Nm(t)}.", $"Tracking tag on {Nm(t)}."));
                return true;
            });
        S("Armor", "Броня", "Armor", "#90A4AE", I, 90, 3, "питомец: щит себе", "pet: a shield for yourself",
            "Предатель. Питомец надевает на тебя щит от одного нападения, если щита ещё нет. Использований: 3, перезарядка 90 с. [Питомец]",
            "Impostor. The pet puts a one-attack shield on you if you have none. Uses: 3, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                if (RoleLogic.ShieldArmed.Contains(p.PlayerId)) { H(p, T(ru, "Щит у тебя уже есть.", "You already have a shield.")); return false; }
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                H(p, T(ru, "Щит надет.", "Shield on."));
                return true;
            });
        S("Scarecrow", "Пугало", "Scarecrow", "#8D6E63", I, 45, 0, "питомец: напугать всех рядом", "pet: scare everyone near",
            "Предатель. Все не-предатели в радиусе 7 м получают сообщение, что у них за спиной кто-то стоит. Перезарядка 45 с. [Питомец]",
            "Impostor. Every non-impostor within 7 m gets a message that somebody stands behind them. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), me) <= 7f).ToList();
                if (l.Count == 0) { H(p, T(ru, "В радиусе 7 м никого нет.", "Nobody within 7 m.")); return false; }
                foreach (var o in l) Utils.SendSecret(o, T(Translator.IsRu(o), "Тебе кажется, что за спиной кто-то стоит...", "You feel like somebody stands behind you..."));
                H(p, T(ru, $"Напугано игроков: {l.Count}.", $"Players scared: {l.Count}."));
                return true;
            });
        S("Screamer", "Крикун", "Screamer", "#F06292", I, 70, 0, "питомец: ложный крик", "pet: a false scream",
            "Предатель. Все не-предатели в радиусе 12 м слышат, что где-то на случайной стороне кричат. Так можно увести их от тебя. Перезарядка 70 с. [Питомец]",
            "Impostor. Every non-impostor within 12 m hears a scream somewhere in a random direction. A way to lead them away from you. Cooldown 70 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var l = Others(p).Where(o => !IsImp(o) && Vector2.Distance(o.GetTruePosition(), me) <= 12f).ToList();
                if (l.Count == 0) { H(p, T(ru, "В радиусе 12 м никого нет.", "Nobody within 12 m.")); return false; }
                if (!RandomDoor(out var fake)) fake = me + new Vector2(5f, 0f);
                foreach (var o in l)
                    Utils.SendSecret(o, T(Translator.IsRu(o), $"Где-то на {Where(true, o.GetTruePosition(), fake)} слышен крик!", $"A scream somewhere to the {Where(false, o.GetTruePosition(), fake)}!"));
                H(p, T(ru, $"Крик услышали: {l.Count}.", $"Players who heard the scream: {l.Count}."));
                return true;
            });
        S("Looter", "Мародёр", "Looter", "#A1887F", I, 40, 0, "питомец: обшарить тело", "pet: loot a body",
            "Предатель. Питомец обшаривает ближайшее тело (до 2,5 м) и даёт тебе щит. Каждое тело можно обшарить один раз. Перезарядка 40 с. [Питомец]",
            "Impostor. The pet loots the nearest body (within 2.5 m) and gives you a shield. Each body can be looted once. Cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                var list = bodies.Where(b => !looted.Contains(b.id) && Vector2.Distance(b.pos, me) <= 2.5f).ToList();
                if (list.Count == 0) { H(p, T(ru, "Рядом нет необысканного тела.", "No unlooted body nearby.")); return false; }
                looted.Add(list[0].id);
                RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                H(p, T(ru, "Ты обшарил тело: у тебя щит.", "You looted the body: you have a shield."));
                return true;
            });
        S("Gatherer", "Сборщик", "Gatherer", "#9575CD", I, 90, 2, "питомец: созвать предателей", "pet: gather the impostors",
            "Предатель. Все остальные живые предатели переносятся к тебе. Использований: 2, перезарядка 90 с. [Питомец]",
            "Impostor. All the other living impostors are moved to you. Uses: 2, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                var l = Others(p).Where(IsImp).ToList();
                if (l.Count == 0) { H(p, T(ru, "Других предателей нет.", "No other impostors.")); return false; }
                foreach (var o in l) Commands.Tp(o, p.GetTruePosition());
                H(p, T(ru, $"К тебе перенесено предателей: {l.Count}.", $"Impostors moved to you: {l.Count}."));
                return true;
            });
        S("Spikes", "Шипы", "Spikes", "#B71C1C", I, 50, 3, "питомец: поставить шипы", "pet: set spikes",
            "Предатель. Питомец ставит шипы на твоём месте (до 120 секунд). Первый не-предатель, наступивший на них (до 1,5 м), теряет щит, а ты узнаёшь, кто это. Использований: 3, перезарядка 50 с. [Питомец]",
            "Impostor. The pet sets spikes where you stand (up to 120 seconds). The first non-impostor to step on them (within 1.5 m) loses his shield and you learn who it was. Uses: 3, cooldown 50 s. [Pet]",
            (p, ru) =>
            {
                spots.Add(new Spot { Kind = "spikes", Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + 120f });
                H(p, T(ru, "Шипы поставлены на 120 секунд.", "Spikes set for 120 seconds."));
                return true;
            });
        S("Scapegoat", "Козёл отпущения", "Scapegoat", "#BCAAA4", I, 80, 0, "питомец: назначить виноватого", "pet: pick a scapegoat",
            "Предатель. Питомец метит ближайшего не-предателя (до 3 м). Если в ближайшие 30 секунд кого-то убьют, всем живым придёт сообщение, что рядом с телом заметили именно его. Перезарядка 80 с. [Питомец]",
            "Impostor. The pet marks the nearest non-impostor (within 3 m). If anyone is killed in the next 30 seconds, every living player is told that he was seen near the body. Cooldown 80 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !IsImp(o) && !scapegoats.ContainsKey(o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет цели.", "No target nearby.")); return false; }
                scapegoats[t.PlayerId] = Time.time + 30f;
                H(p, T(ru, $"{Nm(t)} назначен виноватым на 30 секунд.", $"{Nm(t)} is the scapegoat for 30 seconds."));
                return true;
            });
        // ---------------- Нейтралы (настоящие убийцы) ----------------
        S("Hangman", "Висельник", "Hangman", "#6D4C41", N, 40, 0, "питомец: убить, если вы вдвоём", "pet: kill if you are alone together",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если рядом (до 7 м) нет других свидетелей. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if nobody else is within 7 m. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                var me = p.GetTruePosition();
                if (Others(p).Any(o => o.PlayerId != t.PlayerId && Vector2.Distance(o.GetTruePosition(), me) <= 7f)) { H(p, T(ru, "Рядом есть свидетели.", "There are witnesses nearby.")); return false; }
                return Strike(p, t);
            });
        S("Crowdbane", "Толпобой", "Crowdbane", "#EF5350", N, 45, 0, "питомец: убить в толпе", "pet: kill in a crowd",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если вокруг (до 6 м) есть ещё хотя бы двое. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if at least two more are within 6 m. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                if (Around(p, 6f) < 3) { H(p, T(ru, "Нужна толпа: хотя бы трое рядом.", "You need a crowd: at least three nearby.")); return false; }
                return Strike(p, t);
            });
        S("Bloodsucker", "Кровосос", "Bloodsucker", "#B71C1C", N, 50, 0, "питомец: убить и получить щит", "pet: kill and gain a shield",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), и ты получаешь щит. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m) and you gain a shield. [Pet]",
            (p, ru) =>
            {
                bool ok = NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
                if (ok) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
                return ok;
            });
        S("Wounder", "Подранок", "Wounder", "#C62828", N, 25, 0, "питомец: ранить, потом добить", "pet: wound, then finish",
            "Нейтрал. Для сервера ты настоящий предатель. Первое нажатие ранит ближайшего игрока (до 2,5 м) на 25 секунд, повторное нажатие рядом с раненым убивает его. [Питомец]",
            "Neutral. The server sees you as a real impostor. The first press wounds the nearest player (within 2.5 m) for 25 seconds, pressing again next to a wounded one kills him. [Pet]",
            (p, ru) =>
            {
                foreach (var k in wounded.Where(x => Time.time > x.Value).Select(x => x.Key).ToList()) wounded.Remove(k);
                var hit = Nearest(p, 2.5f, o => wounded.ContainsKey(o.PlayerId));
                if (hit != null) { wounded.Remove(hit.PlayerId); return Strike(p, hit); }
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                wounded[t.PlayerId] = Time.time + 25f;
                H(p, T(ru, $"{Nm(t)} ранен. Добей его в течение 25 секунд.", $"{Nm(t)} is wounded. Finish him within 25 seconds."));
                return true;
            });
        S("Sniper", "Снайпер", "Sniper", "#37474F", N, 90, 2, "питомец: убить в дальнем радиусе", "pet: a long range kill",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока в радиусе 5 м без промаха. Всего два выстрела за игру, перезарядка 90 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player within 5 m without a miss. Only two shots per game, cooldown 90 s. [Pet]",
            (p, ru) => NeutralKill(p, ru, 5f, o => true, "Никого нет ближе 5 м.", "Nobody within 5 m."));
        S("Judge", "Судья", "Judge", "#5D4037", N, 40, 0, "питомец: казнить подозрительного", "pet: execute a suspicious one",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только если он стоит рядом с телом (до 5 м). [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only if he stands next to a body (within 5 m). [Pet]",
            (p, ru) => NeutralKill(p, ru, 2f, o => bodies.Any(b => !eaten.Contains(b.id) && Vector2.Distance(b.pos, o.GetTruePosition()) <= 5f), "Рядом нет подозрительного (у тела, до 2 м).", "No suspicious player nearby (next to a body, within 2 m)."));
        S("Vulture", "Стервятник", "Vulture", "#4E342E", N, 25, 0, "питомец: убить сразу после смерти", "pet: kill right after a death",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только в течение 30 секунд после чьей-то смерти. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only within 30 seconds after somebody's death. [Pet]",
            (p, ru) =>
            {
                if (lastDeathAt < 0 || Time.time - lastDeathAt > 30f) { H(p, T(ru, "Нужна свежая смерть (не старше 30 секунд).", "You need a fresh death (not older than 30 seconds).")); return false; }
                return NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
            });
        S("Finalist", "Финалист", "Finalist", "#FFC107", N, 20, 0, "питомец: убить, когда игроков мало", "pet: kill when few are left",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), но только когда в живых осталось не больше шести. Зато перезарядка короткая. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m), but only when no more than six are alive. The cooldown is short. [Pet]",
            (p, ru) =>
            {
                if (PlayerControl.AllPlayerControls.ToArray().Count(Alive) > 6) { H(p, T(ru, "Рано: в живых больше шести.", "Too early: more than six are alive.")); return false; }
                return NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m.");
            });
        S("Trigger", "Курок", "Trigger", "#FF7043", N, 20, 3, "питомец: три быстрых убийства", "pet: three quick kills",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м). Всего три убийства за игру, зато перезарядка 20 с. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m). Only three kills per game, but the cooldown is 20 s. [Pet]",
            (p, ru) => NeutralKill(p, ru, 2f, o => true, "Никого нет ближе 2 м.", "Nobody within 2 m."));
        S("Breaker", "Пробойник", "Breaker", "#455A64", N, 55, 0, "питомец: убить сквозь щит", "pet: kill through a shield",
            "Нейтрал. Для сервера ты настоящий предатель. Питомец убивает ближайшего игрока (до 2 м), даже если у него есть щит: щит просто сгорает. [Питомец]",
            "Neutral. The server sees you as a real impostor. The pet kills the nearest player (within 2 m) even if he has a shield: the shield just burns out. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => true);
                if (t == null) { H(p, T(ru, "Никого нет ближе 2 м.", "Nobody within 2 m.")); return false; }
                RoleLogic.ShieldArmed.Remove(t.PlayerId);
                return Strike(p, t);
            });
        foreach (var id in new[] { "Hangman", "Crowdbane", "Bloodsucker", "Wounder", "Sniper", "Judge", "Vulture", "Finalist", "Trigger", "Breaker" })
            RoleLogic.RealImpNeutrals.Add(id);

        // ---------------- Мистики ----------------
        S("Blesser", "Благословитель", "Blesser", "#FFF59D", M, 70, 2, "питомец: благословить игрока", "pet: bless a player",
            "Мистик. Питомец даёт щит ближайшему игроку (до 2,5 м), у которого его нет. Использований: 2, перезарядка 70 с. [Питомец]",
            "Mystic. The pet gives a shield to the nearest player (within 2.5 m) who has none. Uses: 2, cooldown 70 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => !RoleLogic.ShieldArmed.Contains(o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет игрока без щита.", "Nobody without a shield nearby.")); return false; }
                RoleLogic.ShieldArmed.Add(t.PlayerId); RoleLogic.ShieldUsed.Remove(t.PlayerId);
                Utils.SendSecret(t, T(Translator.IsRu(t), "Тебя благословили: у тебя щит.", "You were blessed: you have a shield."));
                H(p, T(ru, $"Щит получил {Nm(t)}.", $"{Nm(t)} got a shield."));
                return true;
            });
        S("Scales", "Весы", "Scales", "#B0BEC5", M, 60, 3, "питомец: кого больше", "pet: who outnumbers whom",
            "Мистик. Говорит, чьих игроков в живых больше: мирных или остальных (предатели и нейтралы). Использований: 3, перезарядка 60 с. [Питомец]",
            "Mystic. Tells who has more living players: the crew or the rest (impostors and neutrals). Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var all = PlayerControl.AllPlayerControls.ToArray().Where(Alive).ToList();
                int crew = all.Count(IsCrewSide), rest = all.Count - crew;
                string a = crew > rest ? T(ru, "мирных больше", "the crew outnumbers") : crew < rest ? T(ru, "остальных больше", "the rest outnumbers") : T(ru, "поровну", "an even split");
                H(p, T(ru, $"Весы: {a}.", $"Scales: {a}."));
                return true;
            });
        S("Silhouette", "Силуэт", "Silhouette", "#78909C", M, 35, 0, "питомец: кто за стеной", "pet: who is behind a wall",
            "Мистик. Показывает, сколько игроков в радиусе 12 м прячутся за стенами, то есть скрыты от твоего взгляда. Перезарядка 35 с. [Питомец]",
            "Mystic. Shows how many players within 12 m hide behind walls, out of your sight. Cooldown 35 s. [Pet]",
            (p, ru) =>
            {
                var me = p.GetTruePosition();
                int n = Others(p).Count(o => Vector2.Distance(o.GetTruePosition(), me) <= 12f && Commands.WallBetween(me, o.GetTruePosition()));
                H(p, T(ru, $"За стенами вокруг тебя игроков: {n}.", $"Players behind walls around you: {n}."));
                return true;
            });
        S("Fate", "Судьба", "Fate", "#9575CD", M, 60, 3, "питомец: узнать судьбу игрока", "pet: learn a player's fate",
            "Мистик. Питомец метит ближайшего игрока (до 3 м). Через 45 секунд ты узнаешь, жив он или погиб. Использований: 3, перезарядка 60 с. [Питомец]",
            "Mystic. The pet marks the nearest player (within 3 m). After 45 seconds you learn whether he is alive or dead. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                spots.Add(new Spot { Kind = "fate", Owner = p.PlayerId, Target = t.PlayerId, Until = Time.time + 45f + 100f, Next = Time.time + 45f });
                H(p, T(ru, $"Судьба {Nm(t)} откроется через 45 секунд.", $"The fate of {Nm(t)} will be revealed in 45 seconds."));
                return true;
            });
        S("Omen", "Предвестник", "Omen", "#5E35B1", M, 40, 4, "питомец: светлый или тёмный", "pet: light or dark",
            "Мистик. Смотрит на ближайшего игрока (до 2,5 м): светлый (не предатель) или тёмный. Ответ верен в 70% случаев. Использований: 4, перезарядка 40 с. [Питомец]",
            "Mystic. Looks at the nearest player (within 2.5 m): light (not an impostor) or dark. The answer is right 70% of the time. Uses: 4, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool dark = IsImp(t);
                if (rng.Next(100) >= 70) dark = !dark;
                H(p, T(ru, dark ? $"{Nm(t)}: тёмный знак." : $"{Nm(t)}: светлый знак.", dark ? $"{Nm(t)}: a dark sign." : $"{Nm(t)}: a light sign."));
                return true;
            });
        S("Palmist", "Хиромант", "Palmist", "#CE93D8", M, 40, 3, "питомец: есть ли у роли способность", "pet: does the role have an ability",
            "Мистик. Смотрит на ближайшего игрока (до 2,5 м) и говорит, есть ли у его роли активная способность. Использований: 3, перезарядка 40 с. [Питомец]",
            "Mystic. Looks at the nearest player (within 2.5 m) and tells whether his role has an active ability. Uses: 3, cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2.5f, o => true);
                if (t == null) { H(p, T(ru, "Рядом никого нет.", "Nobody nearby.")); return false; }
                bool act = RoleState.Assigned.TryGetValue(t.PlayerId, out var r) && (!ById.TryGetValue(r.Id, out var sp) || sp.Fn != null);
                H(p, T(ru, act ? $"У {Nm(t)} есть способность." : $"У {Nm(t)} нет способности.", act ? $"{Nm(t)} has an ability." : $"{Nm(t)} has no ability."));
                return true;
            });
        S("Fortuna", "Фортуна", "Fortuna", "#FFD740", M, 90, 2, "питомец: шанс на щит", "pet: a chance for a shield",
            "Мистик. С шансом 50% даёт тебе щит. При неудаче перезарядка всё равно уходит. Использований: 2, перезарядка 90 с. [Питомец]",
            "Mystic. A 50% chance to give you a shield. On a miss the cooldown is still spent. Uses: 2, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                if (rng.Next(100) < 50)
                {
                    RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId);
                    H(p, T(ru, "Удача: у тебя щит.", "Luck: you have a shield."));
                }
                else H(p, T(ru, "Удача отвернулась.", "Luck looked away."));
                return true;
            });
        S("Census", "Перепись", "Census", "#90CAF9", M, 60, 3, "питомец: сколько нейтралов в живых", "pet: how many neutrals are alive",
            "Мистик. Говорит, сколько нейтралов сейчас живо. Использований: 3, перезарядка 60 с. [Питомец]",
            "Mystic. Tells how many neutrals are alive right now. Uses: 3, cooldown 60 s. [Pet]",
            (p, ru) =>
            {
                int n = PlayerControl.AllPlayerControls.ToArray().Count(o => Alive(o) && RoleState.Assigned.TryGetValue(o.PlayerId, out var r) && r.Tab == OptTab.Neutral);
                H(p, T(ru, $"Нейтралов в живых: {n}.", $"Neutrals alive: {n}."));
                return true;
            });
        S("Bodyecho", "Эхо тела", "Body Echo", "#B39DDB", M, 45, 0, "питомец: где лежит последнее тело", "pet: where the last body lies",
            "Мистик. Называет комнату, где лежит последнее тело. Перезарядка 45 с. [Питомец]",
            "Mystic. Names the room where the last body lies. Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var list = bodies.Where(b => !eaten.Contains(b.id)).ToList();
                if (list.Count == 0) { H(p, T(ru, "Тел на карте нет.", "No bodies on the map.")); return false; }
                string room = Utils.RoomLabel(Utils.RoomName(list[list.Count - 1].pos, false), ru);
                H(p, T(ru, $"Последнее тело лежит: {room}.", $"The last body lies: {room}."));
                return true;
            });
        S("Lot", "Жребий", "Lot", "#CE93D8", M, 75, 2, "питомец: три имени, среди них тёмный", "pet: three names, a dark one among them",
            "Мистик. Называет троих игроков, и среди них есть хотя бы один не-мирный (предатель или нейтрал), если такие остались. Использований: 2, перезарядка 75 с. [Питомец]",
            "Mystic. Names three players, and at least one of them is not crew (an impostor or a neutral) if any are left. Uses: 2, cooldown 75 s. [Pet]",
            (p, ru) =>
            {
                var all = Others(p);
                if (all.Count < 3) { H(p, T(ru, "Слишком мало игроков.", "Too few players.")); return false; }
                var dark = all.Where(o => !IsCrewSide(o)).OrderBy(o => rng.Next()).Take(1).ToList();
                var rest = all.Where(o => !dark.Contains(o)).OrderBy(o => rng.Next()).Take(3 - dark.Count);
                var pick = dark.Concat(rest).OrderBy(o => rng.Next()).Select(Nm);
                H(p, T(ru, "Жребий: " + string.Join(", ", pick) + ".", "Lot: " + string.Join(", ", pick) + "."));
                return true;
            });
    }
}
