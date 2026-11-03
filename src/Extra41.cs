using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// Принтер (метки, выбор люком) и Кнопка (касание запускает способность игрока).
public static partial class Extra33
{
    public static readonly HashSet<string> VirusTeam = new() { "Skibidi", "Spore", "Ransom" };
    static readonly Dictionary<byte, int> psel = new();
    static int meetN;
    static readonly string[] PrintRu = { "Ловушка", "Вспышка", "Журнал" };
    static readonly string[] PrintEn = { "Trap", "Flash", "Log" };

    static bool IsRole(PlayerControl p, string id) => p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && r.Id == id;
    static bool PrintOpen(int k) => k != 2 || meetN >= 2;

    static void Vent41(PlayerControl p, int vid)
    {
        if (!IsRole(p, "Printer")) return;
        bool ru = Translator.IsRu(p);
        int k = psel.TryGetValue(p.PlayerId, out var c) ? c : 0;
        for (int i = 0; i < 3; i++) { k = (k + 1) % 3; if (PrintOpen(k)) break; }
        psel[p.PlayerId] = k;
        QueueBoot(p.PlayerId, vid);
        H(p, T(ru, $"Метка: {PrintRu[k]}." + (meetN < 2 ? " Журнал откроется после 2 собраний." : ""), $"Mark: {PrintEn[k]}." + (meetN < 2 ? " The Log unlocks after 2 meetings." : "")));
    }

    static void Tick41(float now)
    {
        Virus42Tick(now);
        RansomTick(now);
        foreach (var sp in spots.ToList())
        {
            if (sp.Kind != "ptrap" && sp.Kind != "pflash" && sp.Kind != "plog") continue;
            var owner = Utils.GetPlayer(sp.Owner);
            if (!Alive(owner)) { spots.Remove(sp); continue; }
            bool ru = Translator.IsRu(owner);
            if (sp.Kind == "plog")
            {
                foreach (var o in Others(owner))
                    if (!sp.Seen.Contains(o.PlayerId) && Vector2.Distance(o.GetTruePosition(), sp.Pos) <= 2.5f) sp.Seen.Add(o.PlayerId);
                continue;
            }
            if (now > sp.Until) { spots.Remove(sp); continue; }
            var t = Others(owner).FirstOrDefault(x => Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 1.5f);
            if (t == null) continue;
            spots.Remove(sp);
            if (sp.Kind == "ptrap") { Roles30.FreezePlayer(t, 4f); H(owner, T(ru, $"В твою ловушку попал {Nm(t)}: замер на 4 с.", $"{Nm(t)} hit your trap: frozen for 4 s.")); }
            else { RoleLogic.FlashPlayer(t, 3f); H(owner, T(ru, $"Твоя вспышка сработала на {Nm(t)}.", $"Your flash went off on {Nm(t)}.")); }
        }
    }

    static void Meeting41()
    {
        Virus42Meeting();
        if (spots.Any(x => x.Kind == "ransom")) rMeet = Time.time;
        meetN++;
        foreach (var g in spots.Where(x => x.Kind == "plog").GroupBy(x => x.Owner).ToList())
        {
            var pr = Utils.GetPlayer(g.Key);
            if (Alive(pr))
            {
                bool ru = Translator.IsRu(pr);
                var names = g.SelectMany(x => x.Seen).Distinct().Select(Utils.GetPlayer).Where(o => o != null).Select(Nm).ToList();
                Utils.SendSecret(pr, T(ru, "Журнал метки: " + (names.Count == 0 ? "никто не проходил." : "проходили " + string.Join(", ", names) + "."),
                                           "Mark log: " + (names.Count == 0 ? "nobody passed." : "passed: " + string.Join(", ", names) + ".")));
            }
            spots.RemoveAll(x => x.Kind == "plog" && x.Owner == g.Key);
        }
    }

    static readonly Dictionary<(byte, byte), float> vexp = new();
    static readonly Dictionary<byte, float> sing = new();
    static float vLast, vwinAt;
    static bool vwin;
    static int VirusAlive(PlayerControl o) => Others(o).Count;
    static int VirusCount(PlayerControl o) => Others(o).Count(x => spots.Any(s => s.Kind == "virus" && s.Owner == o.PlayerId && s.Target == x.PlayerId));

    static void Virus42Tick(float now)
    {
        if (vwin) { if (now >= vwinAt) { vwin = false; Utils.EndGame(); } return; }
        foreach (var kv in sing.ToList())
        {
            var so = Utils.GetPlayer(kv.Key);
            if (now >= kv.Value || so == null || so.Data == null || so.Data.IsDead || MeetingHud.Instance != null) { sing.Remove(kv.Key); continue; }
            foreach (var o in Others(so))
            {
                if (spots.Any(x => x.Kind == "virus" && x.Owner == so.PlayerId && x.Target == o.PlayerId)) continue;
                if (Vector2.Distance(so.GetTruePosition(), o.GetTruePosition()) > 3.5f) continue;
                spots.Add(new Spot { Kind = "virus", Owner = so.PlayerId, Target = o.PlayerId, Until = Time.time + 99999f, Seen = new List<byte> { so.PlayerId } });
                bool sru = Translator.IsRu(so);
                H(so, T(sru, $"Песня достала до {Nm(o)}. Заражено {VirusCount(so)} из {VirusAlive(so)}.", $"The song reached {Nm(o)}. Infected {VirusCount(so)} of {VirusAlive(so)}."));
                VirusCheck(so);
            }
        }
        foreach (var sp in spots.Where(x => x.Kind == "spore").ToList())
        {
            var so = Utils.GetPlayer(sp.Owner);
            if (so == null || so.Data == null || so.Data.IsDead || now > sp.Until) { spots.Remove(sp); continue; }
            var o = Others(so).FirstOrDefault(x => Vector2.Distance(x.GetTruePosition(), sp.Pos) <= 1.5f && !spots.Any(s => s.Kind == "virus" && s.Owner == so.PlayerId && s.Target == x.PlayerId));
            if (o == null) continue;
            spots.Remove(sp);
            spots.Add(new Spot { Kind = "virus", Owner = so.PlayerId, Target = o.PlayerId, Until = Time.time + 99999f, Seen = new List<byte> { so.PlayerId } });
            bool sru = Translator.IsRu(so);
            H(so, T(sru, $"На споре заразился {Nm(o)}. Заражено {VirusCount(so)} из {VirusAlive(so)}.", $"{Nm(o)} got infected by a spore. Infected {VirusCount(so)} of {VirusAlive(so)}."));
            VirusCheck(so);
        }
        float dt = vLast > 0 ? Mathf.Min(now - vLast, 0.5f) : 0f; vLast = now;
        foreach (var sp in spots.Where(x => x.Kind == "virus").ToList())
        {
            var owner = Utils.GetPlayer(sp.Owner);
            if (owner == null || owner.Data == null || owner.Data.Disconnected) { spots.Remove(sp); continue; }
            var v = Utils.GetPlayer(sp.Target);
            if (!Alive(v)) continue;
            bool ru = Translator.IsRu(owner);
            foreach (var o in Others(v))
            {
                if (o.PlayerId == sp.Owner) continue;
                if (spots.Any(x => x.Kind == "virus" && x.Owner == sp.Owner && x.Target == o.PlayerId)) continue;
                var k = (sp.Owner, o.PlayerId);
                if (Vector2.Distance(v.GetTruePosition(), o.GetTruePosition()) > 1.8f) { vexp.Remove(k); continue; }
                vexp[k] = (vexp.TryGetValue(k, out var e) ? e : 0f) + dt;
                if (vexp[k] < (IsRole(owner, "Skibidi") ? 10f : IsRole(owner, "Spore") || IsRole(owner, "Ransom") ? 6f : 3f)) continue;
                vexp.Remove(k);
                var src = new List<byte> { sp.Target };
                spots.Add(new Spot { Kind = "virus", Owner = sp.Owner, Target = o.PlayerId, Until = Time.time + 9999f, Seen = src });
                if (Alive(owner)) H(owner, T(ru, $"Вирус перекинулся: {Nm(v)} заразил {Nm(o)}. Заражено {VirusCount(owner)} из {VirusAlive(owner)}.", $"The virus spread: {Nm(v)} infected {Nm(o)}. Infected {VirusCount(owner)} of {VirusAlive(owner)}."));
                VirusCheck(owner);
            }
        }
    }

    // метка заражённых: строка над именем самого Вируса (другие игроки её не видят)
    static string VirusLine(PlayerControl p, bool ru)
    {
        bool sk = IsRole(p, "Skibidi");
        var names = spots.Where(x => x.Kind == "virus" && x.Owner == p.PlayerId).Select(x => Utils.GetPlayer(x.Target)).Where(Alive).Select(Nm).ToList();
        int n = VirusAlive(p);
        string head = T(ru, $"<color=#9CCC65>☣ заражено {names.Count} из {n}</color>", $"<color=#9CCC65>☣ infected {names.Count} of {n}</color>");
        string act = sk ? T(ru, ", питомец: спеть", ", pet: sing") : T(ru, ", питомец: заразить", ", pet: infect");
        if (sk && sing.TryGetValue(p.PlayerId, out var su) && su > Time.time) act = T(ru, $", поёшь ещё {Mathf.CeilToInt(su - Time.time)} с", $", singing {Mathf.CeilToInt(su - Time.time)} s more");
        if (IsRole(p, "Spore")) act = T(ru, $", питомец: спора (на земле {spots.Count(x => x.Kind == "spore" && x.Owner == p.PlayerId)})", $", pet: spore ({spots.Count(x => x.Kind == "spore" && x.Owner == p.PlayerId)} placed)");
        if (IsRole(p, "Ransom")) act = T(ru, $", питомец: взломать (ждут выкуп {spots.Count(x => x.Kind == "ransom" && x.Owner == p.PlayerId)})", $", pet: hack ({spots.Count(x => x.Kind == "ransom" && x.Owner == p.PlayerId)} awaiting ransom)");
        if (names.Count == 0) return head + act;
        return head + act + ": " + string.Join(", ", names);
    }

    static void VirusCheck(PlayerControl owner)
    {
        if (vwin || MeetingHud.Instance != null) return;
        int n = VirusAlive(owner);
        if (n == 0 || VirusCount(owner) < n) return;
        vwin = true; vwinAt = Time.time + 2f;
        Utils.Announce("Вирус заразил всех! Побеждает: " + Nm(owner));
    }

    static void Virus42Meeting()
    {
        vexp.Clear(); sing.Clear(); spots.RemoveAll(x => x.Kind == "spore");
        foreach (var g in spots.Where(x => x.Kind == "virus").GroupBy(x => x.Owner).ToList())
        {
            var vo = Utils.GetPlayer(g.Key);
            if (!Alive(vo)) continue;
            bool ru = Translator.IsRu(vo);
            var list = g.Select(x =>
            {
                var t = Utils.GetPlayer(x.Target); if (!Alive(t)) return null;
                var s = x.Seen.Count > 0 ? Utils.GetPlayer(x.Seen[0]) : null;
                return Nm(t) + (s != null && s.PlayerId != vo.PlayerId ? (ru ? " (от " : " (from ") + Nm(s) + ")" : "");
            }).Where(s => s != null).ToList();
            Utils.SendSecret(vo, T(ru, $"Заражено {list.Count} из {VirusAlive(vo)}: " + (list.Count == 0 ? "никто." : string.Join(", ", list) + "."), $"Infected {list.Count} of {VirusAlive(vo)}: " + (list.Count == 0 ? "nobody." : string.Join(", ", list) + ".")), 3);
        }
    }

    static readonly Dictionary<byte, float> rprog = new();
    static float rMeet;

    // выкуп: Pos.x = 0 стоять рядом с Вымогателем 3 с, 1 совершить убийство (только для предателей)
    static void RansomPay(Spot sp)
    {
        spots.Remove(sp); rprog.Remove(sp.Target);
        var v = Utils.GetPlayer(sp.Target); var o = Utils.GetPlayer(sp.Owner);
        if (Alive(v)) Utils.SendSecret(v, T(Translator.IsRu(v), "Выкуп оплачен. Вымогатель оставил тебя в покое.", "Ransom paid. The Ransom left you alone."), 3);
        if (Alive(o)) H(o, T(Translator.IsRu(o), $"{Nm(v)} заплатил выкуп.", $"{Nm(v)} paid the ransom."));
    }

    static void RansomKill(PlayerControl killer)
    {
        if (killer == null) return;
        var sp = spots.FirstOrDefault(x => x.Kind == "ransom" && x.Target == killer.PlayerId && x.Pos.x == 1f);
        if (sp != null) RansomPay(sp);
    }

    static void RansomTick(float now)
    {
        if (MeetingHud.Instance != null) return;
        if (rMeet > 0) { float d = now - rMeet; rMeet = 0; foreach (var s in spots.Where(x => x.Kind == "ransom")) { s.Until += d; if (s.Next > 0) s.Next += d; } }
        foreach (var sp in spots.Where(x => x.Kind == "ransom").ToList())
        {
            var o = Utils.GetPlayer(sp.Owner); var v = Utils.GetPlayer(sp.Target);
            if (!Alive(v)) { spots.Remove(sp); rprog.Remove(sp.Target); continue; }
            bool vru = Translator.IsRu(v);
            if (!Alive(o)) { spots.Remove(sp); rprog.Remove(sp.Target); Utils.SendSecret(v, T(vru, "Вымогатель мёртв: взлом снят.", "The Ransom is dead: the hack is lifted."), 3); continue; }
            if (sp.Pos.x == 0f)
            {
                rprog[sp.Target] = Vector2.Distance(o.GetTruePosition(), v.GetTruePosition()) <= 1.5f ? (rprog.TryGetValue(sp.Target, out var pr) ? pr : 0f) + Time.deltaTime : 0f;
                if (rprog[sp.Target] >= 3f) { RansomPay(sp); continue; }
            }
            if (sp.Next > 0 && now >= sp.Next) { sp.Next = 0; Utils.SendSecret(v, T(vru, "Осталось 30 секунд до расправы!", "30 seconds left before he strikes!"), 3); }
            if (now < sp.Until) continue;
            spots.Remove(sp); rprog.Remove(sp.Target);
            if (!Strike(o, v)) RoleLogic.Kill(o, v, true);
            H(o, T(Translator.IsRu(o), $"Время вышло: {Nm(v)} не заплатил.", $"Time is up: {Nm(v)} did not pay."));
        }
    }

    static void Define41()
    {
        const OptTab C = OptTab.Crewmate;
        S("Printer", "Принтер", "Printer", "#4DD0E1", C, 20, 6, "питомец: напечатать метку, люк: сменить метку", "pet: print a mark, vent: change mark",
            "Мирный. Питомец печатает метку на твоём месте, вид метки выбирается входом в люк (тебя сразу выкинет обратно). Ловушка: первый, кто подойдёт ближе 1,5 м, замирает на 4 секунды. Вспышка: ослепляет подошедшего на 3 секунды. Журнал: открывается после 2 собраний; на следующем собрании называет, кто проходил рядом (до 2,5 м). Меток держится не больше 5. Использований 6, перезарядка 20 с. [Питомец]",
            "Crewmate. The pet prints a mark where you stand, the kind is chosen by entering a vent (you are thrown out at once). Trap: the first one within 1.5 m is frozen for 4 seconds. Flash: blinds the one who comes close for 3 seconds. Log: unlocks after 2 meetings; at the next meeting it names who passed nearby (within 2.5 m). Up to 5 marks at once. Uses 6, cooldown 20 s. [Pet]",
            (p, ru) =>
            {
                int k = psel.TryGetValue(p.PlayerId, out var c) ? c : 0;
                if (!PrintOpen(k)) k = 0;
                var mine = spots.Where(x => x.Owner == p.PlayerId && (x.Kind == "ptrap" || x.Kind == "pflash" || x.Kind == "plog")).ToList();
                if (mine.Count >= 5) spots.Remove(mine[0]);
                string kind = k == 0 ? "ptrap" : k == 1 ? "pflash" : "plog";
                spots.Add(new Spot { Kind = kind, Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + (k == 2 ? 9999f : 150f) });
                H(p, T(ru, $"Напечатана метка: {PrintRu[k]}.", $"Mark printed: {PrintEn[k]}."));
                return true;
            });
        S("TimeStop", "Таймстоп", "Time Stop", "#B39DDB", OptTab.Mystic, 90, 3, "питомец: остановить время", "pet: stop time",
            "Мистик. Питомец останавливает время: все остальные игроки замирают на несколько секунд (по умолчанию 5), а ты свободно ходишь. В это время таймеры меток, ловушек и отложенных смертей тоже стоят. Убивать замершим нельзя, но можно уйти или спрятаться. Использований: 3, перезарядка 90 с. [Питомец]",
            "Mystic. The pet stops time: all other players freeze for a few seconds (5 by default) while you move freely. Timers of marks, traps and delayed deaths stop too. You cannot kill frozen players, but you can leave or hide. Uses: 3, cooldown 90 s. [Pet]",
            (p, ru) =>
            {
                float dur = Mathf.Max(2, G("TimeStopSec"));
                int n = 0;
                foreach (var o in Others(p)) { Roles30.FreezePlayer(o, dur); Utils.SendSecret(o, T(Translator.IsRu(o), "Время остановлено!", "Time is stopped!"), 1, false); n++; }
                foreach (var tm in timers) tm.At += dur;
                foreach (var sp in spots) { sp.Until += dur; if (sp.Next > 0f) sp.Next += dur; }
                H(p, T(ru, $"Время остановлено на {dur:0} с. Замерло игроков: {n}.", $"Time stopped for {dur:0} s. Frozen players: {n}."));
                return true;
            });
        E("TimeStop", "TimeStopSec", 5, 2, 15, 1, "Длительность остановки (сек)", "Stop duration (sec)");
        S("Skibidi", "Скибиди", "Skibidi", "#80DEEA", OptTab.Neutral, 30, 0, "питомец: спеть песню", "pet: sing the song",
            "Нейтрал из команды вирусов. Побеждает, когда заражены все живые игроки. Питомец запускает песню на 10 секунд (раз в 30 секунд). Кто подошёл к тебе ближе 3,5 м, пока песня звучит, заражается. Заражённый передаёт вирус дальше медленно: ему надо стоять рядом с другим игроком (до 1,8 м) 10 секунд. Вирус не снимается собранием и остаётся до смерти игрока; если ты умрёшь, вирус продолжит распространяться и ты всё равно выиграешь. Над твоим именем видно, кто заражён. [Питомец]",
            "Neutral of the virus team. You win when every living player is infected. The pet starts the song for 10 seconds (once per 30 seconds). Anyone who comes within 3.5 m of you while the song plays gets infected. An infected player spreads the virus slowly: he must stand within 1.8 m of another player for 10 seconds. A meeting does not remove the virus, it stays until the player dies; if you die, the virus keeps spreading and you still win. You can see who is infected above your name. [Pet]",
            (p, ru) =>
            {
                sing[p.PlayerId] = Time.time + 10f;
                H(p, T(ru, "Ты поёшь 10 секунд: кто подойдёт ближе 3,5 м, заразится.", "You sing for 10 seconds: anyone within 3.5 m gets infected."));
                return true;
            });
        S("Spore", "Спора", "Spore", "#A1887F", OptTab.Neutral, 45, 0, "питомец: оставить спору", "pet: leave a spore",
            "Нейтрал из команды вирусов, слабая роль. Побеждает, когда заражены все живые игроки. Питомец оставляет на твоём месте невидимую спору на 60 секунд (не больше 2 одновременно, споры пропадают на собрании). Первый, кто пройдёт рядом (до 1,5 м), заражается, а ты узнаёшь кто. Заражённый передаёт вирус медленно: надо простоять рядом с другим игроком (до 1,8 м) 6 секунд. Вирус не снимается собранием, остаётся до смерти игрока и продолжает распространяться, если умрёшь ты (победа всё равно твоя). Перезарядка 45 с. [Питомец]",
            "Neutral of the virus team, a weak role. You win when every living player is infected. The pet leaves an invisible spore where you stand for 60 seconds (no more than 2 at once, spores vanish at a meeting). The first one to pass nearby (within 1.5 m) gets infected and you learn who. An infected player spreads the virus slowly: he must stand within 1.8 m of another player for 6 seconds. A meeting does not remove the virus, it stays until the player dies and keeps spreading if you die (the win is still yours). Cooldown 45 s. [Pet]",
            (p, ru) =>
            {
                var mine = spots.Where(x => x.Kind == "spore" && x.Owner == p.PlayerId).ToList();
                if (mine.Count >= 2) spots.Remove(mine[0]);
                spots.Add(new Spot { Kind = "spore", Owner = p.PlayerId, Pos = p.GetTruePosition(), Until = Time.time + 60f });
                H(p, T(ru, "Спора оставлена на 60 секунд.", "Spore left for 60 seconds."));
                return true;
            });
        S("Ransom", "Вымогатель", "Ransom", "#EF5350", OptTab.Neutral, 40, 0, "питомец: взломать игрока и потребовать выкуп", "pet: hack a player and demand a ransom",
            "Нейтрал из команды вирусов. Побеждает, когда заражены все живые игроки. Питомец взламывает ближайшего игрока (до 2,5 м, не больше 2 взломов одновременно): он сразу считается заражённым и получает требование выкупа. Выкуп: простоять рядом с тобой (до 1,5 м) 3 секунды, а если взломан предатель, то совершить убийство. Если за 60 секунд (настройка) выкуп не оплачен, ты убиваешь его (щит спасает от удара). Время идёт не во время собраний. Вирус передаётся дальше за 6 секунд рядом. Перезарядка 40 с. [Питомец]",
            "Neutral of the virus team. You win when every living player is infected. The pet hacks the nearest player (within 2.5 m, at most 2 hacks at once): he counts as infected at once and gets a ransom demand. The ransom: stand next to you (within 1.5 m) for 3 seconds, or, if an impostor is hacked, commit a kill. If the ransom is not paid within 60 seconds (option) you kill him (a shield stops the blow). The timer does not run during meetings. The virus spreads on by 6 seconds of standing close. Cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                if (spots.Count(x => x.Kind == "ransom" && x.Owner == p.PlayerId) >= 2) { H(p, T(ru, "Уже 2 взлома ждут выкупа.", "2 hacks are already awaiting ransom.")); return false; }
                var t = Nearest(p, 2.5f, o => !spots.Any(x => x.Kind == "ransom" && x.Target == o.PlayerId) && !(RoleState.Assigned.TryGetValue(o.PlayerId, out var r) && VirusTeam.Contains(r.Id)));
                if (t == null) { H(p, T(ru, "Рядом нет подходящего игрока (до 2,5 м).", "No suitable player within 2.5 m.")); return false; }
                float sec = G("RansomSec");
                int kind = IsImp(t) && UnityEngine.Random.value < 0.5f ? 1 : 0;
                spots.Add(new Spot { Kind = "ransom", Owner = p.PlayerId, Target = t.PlayerId, Pos = new Vector2(kind, 0f), Until = Time.time + sec, Next = sec > 40f ? Time.time + sec - 30f : 0f });
                if (!spots.Any(x => x.Kind == "virus" && x.Owner == p.PlayerId && x.Target == t.PlayerId))
                    spots.Add(new Spot { Kind = "virus", Owner = p.PlayerId, Target = t.PlayerId, Until = Time.time + 99999f, Seen = new List<byte> { p.PlayerId } });
                bool tru = Translator.IsRu(t);
                string dem = kind == 1 ? T(tru, "совершить 1 убийство", "commit 1 kill") : T(tru, $"простоять рядом с {Nm(p)} 3 секунды", $"stand next to {Nm(p)} for 3 seconds");
                Utils.SendSecret(t, T(tru, $"Тебя взломал Вымогатель! Выкуп: {dem}. Срок {sec:0} с, иначе он тебя убьёт.", $"You were hacked by the Ransom! Ransom: {dem}. {sec:0} s, or he kills you."), 3);
                string dem2 = kind == 1 ? T(ru, "1 убийство", "1 kill") : T(ru, "3 с рядом с тобой", "3 s next to you");
                H(p, T(ru, $"Взломан {Nm(t)}. Выкуп: {dem2}, срок {sec:0} с.", $"Hacked {Nm(t)}. Ransom: {dem2}, {sec:0} s."));
                VirusCheck(p);
                return true;
            });
        E("Ransom", "RansomSec", 60, 30, 120, 10, "Время на выкуп (сек)", "Ransom time (sec)");
        S("Button", "Кнопка", "Button", "#FF8A65", C, 30, 4, "питомец: коснуться игрока, его способность сработает", "pet: touch a player, his ability fires",
            "Мирный. Питомец касается ближайшего игрока (до 2 м), и его способность тут же срабатывает, как будто он сам нажал. Работает только с активными способностями, действуют перезарядка и лимит использований самого игрока. Использований 4, перезарядка 30 с. [Питомец]",
            "Crewmate. The pet touches the nearest player (within 2 m) and his ability fires at once, as if he pressed it himself. Works only with active abilities; the player's own cooldown and uses apply. Uses 4, cooldown 30 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 2f, o => RoleState.Assigned.TryGetValue(o.PlayerId, out var r) && r.Id != "Button" && r.Id != "God");
                if (t == null) { H(p, T(ru, "Рядом нет игрока с ролью (до 2 м).", "No player with a role within 2 m.")); return false; }
                var role = RoleState.Assigned[t.PlayerId];
                RoleLogic.UseAbility(t, "touch");
                H(p, T(ru, $"Ты коснулся {Nm(t)}: сработала способность ({role.Colored(true)}).", $"You touched {Nm(t)}: his ability fired ({role.Colored(false)})."));
                return true;
            });
    }
}
