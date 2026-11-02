using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// Принтер (метки, выбор люком) и Кнопка (касание запускает способность игрока).
public static partial class Extra33
{
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
    static float vLast, vwinAt;
    static bool vwin;
    static int VirusAlive(PlayerControl o) => Others(o).Count;
    static int VirusCount(PlayerControl o) => Others(o).Count(x => spots.Any(s => s.Kind == "virus" && s.Owner == o.PlayerId && s.Target == x.PlayerId));

    static void Virus42Tick(float now)
    {
        if (vwin) { if (now >= vwinAt) { vwin = false; Utils.EndGame(); } return; }
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
                if (vexp[k] < 3f) continue;
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
        var names = spots.Where(x => x.Kind == "virus" && x.Owner == p.PlayerId).Select(x => Utils.GetPlayer(x.Target)).Where(Alive).Select(Nm).ToList();
        int n = VirusAlive(p);
        string head = T(ru, $"<color=#9CCC65>☣ заражено {names.Count} из {n}</color>", $"<color=#9CCC65>☣ infected {names.Count} of {n}</color>");
        if (names.Count == 0) return head + T(ru, ", питомец: заразить", ", pet: infect");
        return head + ": " + string.Join(", ", names);
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
        vexp.Clear();
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
        S("Virus", "Вирус", "Virus", "#9CCC65", OptTab.Neutral, 40, 0, "питомец: заразить игрока", "pet: infect a player",
            "Нейтрал. Чтобы выиграть, нужно заразить всех живых игроков. Питомец заражает ближайшего игрока (до 3 м) безвредным вирусом. Заражённый сам передаёт его тем, кто пробудет рядом (до 1,8 м) 3 секунды. Вирус остаётся до смерти игрока и не снимается собранием; на собрании ты получаешь список заражённых и от кого каждый заразился. Когда заражены все живые, ты побеждаешь. Если ты умрёшь, вирус продолжит распространяться, и ты всё равно выиграешь. Перезарядка 40 с. [Питомец]",
            "Neutral. To win, infect every living player. The pet infects the nearest player (within 3 m) with a harmless virus. An infected player passes it to anyone who stays near (within 1.8 m) for 3 seconds. The virus stays until the player dies and a meeting does not remove it; at a meeting you get the list of infected players and who infected each. When all living players are infected, you win. If you die, the virus keeps spreading and you still win. Cooldown 40 s. [Pet]",
            (p, ru) =>
            {
                var t = Nearest(p, 3f, o => !spots.Any(x => x.Kind == "virus" && x.Owner == p.PlayerId && x.Target == o.PlayerId));
                if (t == null) { H(p, T(ru, "Рядом нет, кого заразить (до 3 м).", "Nobody to infect within 3 m.")); return false; }
                spots.Add(new Spot { Kind = "virus", Owner = p.PlayerId, Target = t.PlayerId, Until = Time.time + 99999f, Seen = new List<byte> { p.PlayerId } });
                H(p, T(ru, $"{Nm(t)} заражён. Заражено {VirusCount(p)} из {VirusAlive(p)}.", $"{Nm(t)} is infected. Infected {VirusCount(p)} of {VirusAlive(p)}."));
                VirusCheck(p);
                return true;
            });
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
