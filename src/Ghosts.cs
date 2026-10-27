using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class Ghosts
{
    public static BoolOpt On;
    public static IntOpt Uses, PossKills, PossCd;
    public static BoolOpt PossOn, HelperOn;
    public static IntOpt HelperMax, HelperCd, HelperTime;
    static readonly System.Random rng = new();
    static readonly Dictionary<byte, int> used = new();
    static readonly HashSet<byte> told = new();
    static float nextCheck;

    static readonly string[] WordsRu =
    {
        "тихо", "сзади", "кровь", "ложь", "шкаф", "холодно", "вентиляция", "предатель", "доверяй", "смотри", "тень", "бойся",
        "верь", "лампочка", "носок", "пельмень", "подвал", "тишина", "эхо", "капуста", "ключ", "спина", "огонь", "дверь",
        "голосуй", "не он", "это он", "молчи", "бу", "скоро", "рядом", "темно", "шёпот", "могила", "гвоздь",
        "пепел", "метеорит", "щупальца", "кекс",
        "он был в вентиляции", "не верь первому подозрению", "предатель стоит рядом с тобой", "я видел это в темноте",
        "голосуй не за него", "кто-то врёт на собрании", "лампочка погасла перед убийством", "ищи тело в электрической",
        "тишина скрывает предателя", "пепел падает на виноватого", "проверь каждого в медотсеке", "не доверяй тому кто молчит"
    };
    static readonly string[] WordsEn =
    {
        "behind", "blood", "liar", "closet", "cold", "vent", "impostor", "trust", "look", "shadow", "fear", "believe",
        "lightbulb", "sock", "dumpling", "basement", "silence", "echo", "cabbage", "key", "back", "fire", "door", "vote",
        "not him", "it is him", "hush", "boo", "soon", "near", "dark", "whisper", "cobweb", "grave", "nail", "ash",
        "meteor", "tentacles", "muffin", "nearby",
        "he was in the vent", "do not trust the first guess", "the impostor stands next to you", "i saw it in the dark",
        "do not vote for him", "someone is lying in this meeting", "the light went out before the kill", "look for the body in electrical",
        "silence hides the impostor", "ash falls on the guilty", "check everyone in medbay", "do not trust the quiet one"
    };

    public static (string ru, string en, string color, BoolOpt on, Opt[] extras)[] Roles => RolesBase.Concat(Extra33.GhostRoles()).ToArray();
    static (string ru, string en, string color, BoolOpt on, Opt[] extras)[] RolesBase => new[]
    {
        ("Полтергейст", "Poltergeist", "#CE93D8", On, new Opt[] { Uses }),
        ("Помощник по заданиям", "Task Helper", "#80CBC4", HelperOn, new Opt[] { HelperMax, HelperCd, HelperTime }),
        ("Вселенец", "Possessor", "#FF8A80", PossOn, new Opt[] { PossKills, PossCd }),
    };

    public static void Define(ConfigFile c)
    {
        On = new BoolOpt("Poltergeist", OptTab.Ghost, true, c);
        On.NameRu = "Полтергейст (роль призраков)"; On.NameEn = "Poltergeist (ghost role)";
        On.GroupRu = "Призраки"; On.GroupEn = "Ghosts";
        Uses = new IntOpt("PoltergeistUses", OptTab.Ghost, 2, 1, 5, 1, c);
        Uses.NameRu = "Полтергейст: использований за игру"; Uses.NameEn = "Poltergeist: uses per game";
        Uses.GroupRu = "Призраки"; Uses.GroupEn = "Ghosts";
        HelperOn = new BoolOpt("TaskHelper", OptTab.Ghost, true, c);
        HelperOn.NameRu = "Помощник по заданиям (призрак-мирный)"; HelperOn.NameEn = "Task Helper (crewmate ghost)";
        HelperOn.GroupRu = "Призраки"; HelperOn.GroupEn = "Ghosts";
        HelperMax = new IntOpt("TaskHelperMax", OptTab.Ghost, 5, 1, 15, 1, c);
        HelperMax.NameRu = "Помощник: заданий за игру"; HelperMax.NameEn = "Task Helper: tasks per game";
        HelperMax.GroupRu = "Призраки"; HelperMax.GroupEn = "Ghosts";
        HelperCd = new IntOpt("TaskHelperCooldown", OptTab.Ghost, 20, 5, 120, 5, c);
        HelperCd.NameRu = "Помощник: перерыв между заданиями (с)"; HelperCd.NameEn = "Task Helper: pause between tasks (s)";
        HelperCd.GroupRu = "Призраки"; HelperCd.GroupEn = "Ghosts";
        HelperTime = new IntOpt("TaskHelperTime", OptTab.Ghost, 45, 15, 120, 5, c);
        HelperTime.NameRu = "Помощник: время на решение (с)"; HelperTime.NameEn = "Task Helper: time to solve (s)";
        HelperTime.GroupRu = "Призраки"; HelperTime.GroupEn = "Ghosts";
        PossOn = new BoolOpt("Possessor", OptTab.Ghost, true, c);
        PossOn.NameRu = "Вселенец (призрак-предатель)"; PossOn.NameEn = "Possessor (impostor ghost)";
        PossOn.GroupRu = "Призраки"; PossOn.GroupEn = "Ghosts";
        PossKills = new IntOpt("PossessorKills", OptTab.Ghost, 2, 1, 5, 1, c);
        PossKills.NameRu = "Вселенец: убийств за игру"; PossKills.NameEn = "Possessor: kills per game";
        PossKills.GroupRu = "Призраки"; PossKills.GroupEn = "Ghosts";
        PossCd = new IntOpt("PossessorCooldown", OptTab.Ghost, 25, 5, 90, 5, c);
        PossCd.NameRu = "Вселенец: перезарядка убийства (с)"; PossCd.NameEn = "Possessor: kill cooldown (s)";
        PossCd.GroupRu = "Призраки"; PossCd.GroupEn = "Ghosts";
        On.ShortRu = "Включён"; On.ShortEn = "Enabled";
        Uses.ShortRu = "Использований за игру"; Uses.ShortEn = "Uses per game";
        HelperOn.ShortRu = "Включён"; HelperOn.ShortEn = "Enabled";
        HelperMax.ShortRu = "Заданий за игру"; HelperMax.ShortEn = "Tasks per game";
        HelperCd.ShortRu = "Перерыв между заданиями (с)"; HelperCd.ShortEn = "Pause between tasks (s)";
        HelperTime.ShortRu = "Время на решение (с)"; HelperTime.ShortEn = "Time to solve (s)";
        PossOn.ShortRu = "Включён"; PossOn.ShortEn = "Enabled";
        PossKills.ShortRu = "Убийств за игру"; PossKills.ShortEn = "Kills per game";
        PossCd.ShortRu = "Перезарядка убийства (с)"; PossCd.ShortEn = "Kill cooldown (s)";
    }

    static readonly Dictionary<byte, byte> host = new();
    static readonly Dictionary<byte, int> killsUsed = new();
    static readonly Dictionary<byte, float> lastKill = new(), nextReport = new();

    class Challenge { public byte Target; public long Answer; public float Ends; }
    static readonly Dictionary<byte, Challenge> chal = new();
    static readonly Dictionary<byte, int> helped = new();
    static readonly Dictionary<byte, float> helpNext = new();

    static bool IsNeutral(PlayerControl p) => p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && r.Tab == OptTab.Neutral;
    static bool IsHelper(PlayerControl p) => HelperOn != null && HelperOn.Value && Dead(p) && !ImpTeam(p) && !IsNeutral(p);
    static int Left(PlayerControl o) => o.Data.Tasks == null ? 0 : o.Data.Tasks.ToArray().Count(x => !x.Complete);

    public static void Reset() { chal.Clear(); helped.Clear(); helpNext.Clear(); used.Clear(); told.Clear(); host.Clear(); killsUsed.Clear(); lastKill.Clear(); nextReport.Clear(); }
    public static void OnMeeting() { chal.Clear(); host.Clear(); nextReport.Clear(); }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;
    static bool ImpTeam(PlayerControl p) => p != null && p.Data != null && !RoleLogic.Demoted.Contains(p.PlayerId) && (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId));

    static bool Dead(PlayerControl p) => p != null && p.Data != null && p.Data.IsDead && !p.Data.Disconnected;

    public static void Tick()
    {
        try { TickInner(); } catch (Exception e) { Plugin.Logger.LogWarning("Ghosts.Tick: " + e.Message); }
    }

    static void TickInner()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 1.5f;
        if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started) return;
        foreach (var p in PlayerControl.AllPlayerControls.ToArray())
        {
            if (!Dead(p) || p.AmOwner || told.Contains(p.PlayerId)) continue;
            bool ru = Translator.IsRu(p);
            if (Extra33.TryGhost(p, ImpTeam(p) ? "imp" : IsNeutral(p) ? "other" : "crew")) { told.Add(p.PlayerId); continue; }
            if (ImpTeam(p))
            {
                if (PossOn == null || !PossOn.Value) continue;
                told.Add(p.PlayerId); Plugin.Logger.LogInfo($"Ghost role: {p.Data.PlayerName} -> Possessor");
                Utils.SendSecret(p, ru
                    ? $"Ты Вселенец (призрак-предатель). В раунде напиши /haunt ID, чтобы вселиться в живого предателя (список: /haunt). Тогда ты видишь, где он и кто рядом, а /hk убивает ближайшего мирного его руками (убийств за игру: {PossKills.Value}). Вселение до собрания."
                    : $"You are the Possessor (impostor ghost). In a round type /haunt ID to possess a living impostor (list: /haunt). You then see where he is and who is near, and /hk kills the nearest crewmate with his hands (kills per game: {PossKills.Value}). Possession lasts until the next meeting.");
            }
            else if (IsHelper(p))
            {
                told.Add(p.PlayerId); Plugin.Logger.LogInfo($"Ghost role: {p.Data.PlayerName} -> Task Helper");
                Utils.SendSecret(p, ru
                    ? $"Ты Помощник по заданиям (призрак-мирный). В раунде /task покажет, у кого остались задания, /task ID даст сложную задачу. Реши её командой /solve ответ, и у игрока засчитается задание. Можно выполнить заданий за игру: {HelperMax.Value}."
                    : $"You are the Task Helper (crewmate ghost). In a round /task lists who still has tasks, /task ID gives a hard puzzle. Solve it with /solve answer and that player gets a task completed. Tasks you can do per game: {HelperMax.Value}.");
            }
            else
            {
                if (On == null || !On.Value) continue;
                told.Add(p.PlayerId); Plugin.Logger.LogInfo($"Ghost role: {p.Data.PlayerName} -> Poltergeist");
                Utils.SendSecret(p, ru
                    ? $"Ты Полтергейст (роль призраков). На собрании напиши /boo, и всем придёт случайное слово от тебя без имени. Осталось раз: {Uses.Value}."
                    : $"You are the Poltergeist (ghost role). In a meeting type /boo and everyone gets a random word from you, with no name. Uses left: {Uses.Value}.");
            }
        }
        foreach (var kv in host.ToList())
        {
            var ghost = Utils.GetPlayer(kv.Key); var body = Utils.GetPlayer(kv.Value);
            if (!Dead(ghost)) { host.Remove(kv.Key); continue; }
            bool ru = Translator.IsRu(ghost);
            if (!Alive(body))
            {
                host.Remove(kv.Key);
                Utils.SendSecret(ghost, ru ? "Носитель погиб, вселение закончилось. Можно вселиться в другого: /haunt." : "Your host died, possession ended. You can possess another: /haunt.", 1, false);
                continue;
            }
            if (nextReport.TryGetValue(kv.Key, out var t) && Time.time < t) continue;
            nextReport[kv.Key] = Time.time + 6f;
            var pos = body.GetTruePosition();
            var near = PlayerControl.AllPlayerControls.ToArray()
                .Where(o => Alive(o) && o.PlayerId != body.PlayerId && Vector2.Distance(pos, o.GetTruePosition()) < 5f)
                .Select(o => o.Data.PlayerName).Take(4).ToList();
            string room = Utils.RoomName(pos, ru);
            string list = near.Count == 0 ? (ru ? "никого" : "nobody") : string.Join(", ", near);
            Utils.SendSecret(ghost, ru ? $"[Вселение: {body.Data.PlayerName}] комната: {room}; рядом: {list}." : $"[Possessing {body.Data.PlayerName}] room: {room}; nearby: {list}.", 1, false);
        }
    }

    static Challenge MakePuzzle(byte target, out string text, bool ru)
    {
        long ans; int k = rng.Next(3);
        if (k == 0) { int a = rng.Next(23, 79), b = rng.Next(12, 49), c = rng.Next(13, 59), d = rng.Next(11, 39); ans = (long)a * b + (long)c * d; text = $"{a}*{b} + {c}*{d}"; }
        else if (k == 1) { int a = rng.Next(41, 99), b = rng.Next(31, 89), c = rng.Next(100, 999); ans = (long)a * b - c; text = $"{a}*{b} - {c}"; }
        else { int a = rng.Next(14, 39), b = rng.Next(14, 39), c = rng.Next(5, 19); ans = (long)a * a + (long)b * b - c; text = $"{a}^2 + {b}^2 - {c}"; }
        return new Challenge { Target = target, Answer = ans, Ends = Time.time + HelperTime.Value };
    }

    static bool HandleHelper(PlayerControl me, string cmd, string[] a)
    {
        bool ru = Translator.IsRu(me);
        void Tell(string r, string e) => Utils.SendPrivate(me, ru ? r : e);
        if (HelperOn == null || !HelperOn.Value) { Tell("Роль Помощник по заданиям выключена.", "The Task Helper role is off."); return true; }
        if (!IsHelper(me)) { Tell("Помощником может быть только призрак-мирный.", "Only a crewmate ghost can be the Task Helper."); return true; }
        if (MeetingHud.Instance != null || AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
        { Tell("Задания помогают только в раунде, не на собрании.", "Tasks can be helped only during a round, not in a meeting."); return true; }
        helped.TryGetValue(me.PlayerId, out int n);
        if (cmd == "task" || cmd == "helper")
        {
            var cand = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && !ImpTeam(o) && o.Data.Tasks != null && Left(o) > 0).ToList();
            if (a.Length == 0)
            {
                if (cand.Count == 0) { Tell("Ни у кого не осталось заданий.", "Nobody has tasks left."); return true; }
                Tell($"Заданий можно выполнить ещё: {HelperMax.Value - n}. Кому нужна помощь: " + string.Join(", ", cand.Select(o => $"{o.PlayerId}={o.Data.PlayerName} ({Left(o)})")) + ". Взять задачу: /task ID",
                     $"Tasks you can still do: {HelperMax.Value - n}. Who needs help: " + string.Join(", ", cand.Select(o => $"{o.PlayerId}={o.Data.PlayerName} ({Left(o)})")) + ". Take a puzzle: /task ID");
                return true;
            }
            if (n >= HelperMax.Value) { Tell("Ты уже помог столько, сколько мог.", "You have already helped as much as you can."); return true; }
            if (helpNext.TryGetValue(me.PlayerId, out var nx) && Time.time < nx) { int left = Mathf.CeilToInt(nx - Time.time); Tell($"Отдохни ещё {left} с.", $"Rest for {left} s."); return true; }
            var t = cand.FirstOrDefault(o => o.PlayerId.ToString() == a[0] || string.Equals(o.Data.PlayerName, string.Join(" ", a), StringComparison.OrdinalIgnoreCase));
            if (t == null) { Tell("У этого игрока нет заданий или его нет в списке: /task", "That player has no tasks or is not in the list: /task"); return true; }
            var ch = MakePuzzle(t.PlayerId, out string text, ru);
            chal[me.PlayerId] = ch;
            Tell($"Сложное задание для {t.Data.PlayerName}. Посчитай в уме: {text}. Ответ: /solve число. Время: {HelperTime.Value} с.",
                 $"A hard task for {t.Data.PlayerName}. Calculate: {text}. Answer: /solve number. Time: {HelperTime.Value} s.");
            return true;
        }
        if (!chal.TryGetValue(me.PlayerId, out var c)) { Tell("Нет активной задачи. Возьми: /task ID", "No active puzzle. Take one: /task ID"); return true; }
        if (Time.time > c.Ends) { chal.Remove(me.PlayerId); helpNext[me.PlayerId] = Time.time + HelperCd.Value; Tell("Время вышло, задача сгорела.", "Time is up, the puzzle burned."); return true; }
        if (a.Length == 0 || !long.TryParse(a[0], out long val)) { Tell("Напиши ответ числом: /solve 1234", "Write the answer as a number: /solve 1234"); return true; }
        chal.Remove(me.PlayerId);
        helpNext[me.PlayerId] = Time.time + HelperCd.Value;
        if (val != c.Answer) { Tell("Неверно. Задача сгорела, попробуй другую после перерыва.", "Wrong. The puzzle burned, try another after the pause."); return true; }
        var target = Utils.GetPlayer(c.Target);
        var task = Alive(target) && target.Data.Tasks != null ? target.Data.Tasks.ToArray().FirstOrDefault(x => !x.Complete) : null;
        if (task == null) { Tell("Верно, но у игрока уже нет заданий.", "Correct, but that player has no tasks left."); return true; }
        try { target.RpcCompleteTask(task.Id); }
        catch (Exception e) { Plugin.Logger.LogWarning("TaskHelper: " + e.Message); Tell("Не удалось засчитать задание.", "Could not complete the task."); return true; }
        helped[me.PlayerId] = ++n;
        Tell($"Верно! У {target.Data.PlayerName} засчитано задание. Осталось помочь раз: {HelperMax.Value - n}.", $"Correct! {target.Data.PlayerName} got a task completed. Helps left: {HelperMax.Value - n}.");
        return true;
    }

    static bool HandlePossess(PlayerControl me, string cmd, string[] a)
    {
        bool ru = Translator.IsRu(me);
        void Tell(string r, string e) => Utils.SendPrivate(me, ru ? r : e);
        if (PossOn == null || !PossOn.Value) { Tell("Роль Вселенец выключена.", "The Possessor role is off."); return true; }
        if (!Dead(me) || !ImpTeam(me)) { Tell("Вселенцем может быть только призрак-предатель.", "Only an impostor ghost can be the Possessor."); return true; }
        if (MeetingHud.Instance != null || AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
        { Tell("Вселяться можно только в раунде, не на собрании.", "You can possess only during a round, not in a meeting."); return true; }
        var imps = PlayerControl.AllPlayerControls.ToArray().Where(o => Alive(o) && ImpTeam(o)).ToList();
        if (cmd == "haunt" || cmd == "possess")
        {
            if (a.Length == 0)
            {
                if (imps.Count == 0) { Tell("Живых предателей нет.", "No living impostors."); return true; }
                Tell("Живые предатели: " + string.Join(", ", imps.Select(o => $"{o.PlayerId}={o.Data.PlayerName}")) + ". Вселиться: /haunt ID",
                     "Living impostors: " + string.Join(", ", imps.Select(o => $"{o.PlayerId}={o.Data.PlayerName}")) + ". Possess: /haunt ID");
                return true;
            }
            var t = imps.FirstOrDefault(o => o.PlayerId.ToString() == a[0] || string.Equals(o.Data.PlayerName, string.Join(" ", a), StringComparison.OrdinalIgnoreCase));
            if (t == null) { Tell("Такого живого предателя нет. Список: /haunt", "No such living impostor. List: /haunt"); return true; }
            host[me.PlayerId] = t.PlayerId; nextReport.Remove(me.PlayerId);
            Tell($"Ты вселился в {t.Data.PlayerName}. Отчёты придут сюда, убить: /hk. Вселение до собрания.", $"You possess {t.Data.PlayerName}. Reports will come here, kill: /hk. Lasts until the meeting.");
            return true;
        }
        if (!host.TryGetValue(me.PlayerId, out var hid)) { Tell("Сначала вселись: /haunt ID", "Possess first: /haunt ID"); return true; }
        var body = Utils.GetPlayer(hid);
        if (!Alive(body)) { host.Remove(me.PlayerId); Tell("Носитель погиб.", "Your host is dead."); return true; }
        killsUsed.TryGetValue(me.PlayerId, out int ku);
        if (ku >= PossKills.Value) { Tell("Убийства закончились.", "No kills left."); return true; }
        if (lastKill.TryGetValue(me.PlayerId, out var lk) && Time.time - lk < PossCd.Value)
        { int left = Mathf.CeilToInt(PossCd.Value - (Time.time - lk)); Tell($"Перезарядка: ещё {left} с.", $"Cooldown: {left} s."); return true; }
        PlayerControl victim = null; float bd = 2.2f; var bp = body.GetTruePosition();
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (!Alive(o) || o.PlayerId == body.PlayerId || ImpTeam(o)) continue;
            float d = Vector2.Distance(bp, o.GetTruePosition());
            if (d < bd) { bd = d; victim = o; }
        }
        if (victim == null) { Tell("Рядом с носителем нет мирных (нужно ближе 2 м).", "No crewmate close to your host (within 2 m)."); return true; }
        if (!RoleLogic.CanKill(body, victim)) { Tell("Сейчас убить нельзя.", "Cannot kill right now."); return true; }
        if (!RoleLogic.Kill(body, victim, true)) { Tell("Убийство не получилось.", "The kill failed."); return true; }
        killsUsed[me.PlayerId] = ku + 1; lastKill[me.PlayerId] = Time.time;
        Tell($"{victim.Data.PlayerName} убит. Осталось убийств: {PossKills.Value - ku - 1}.", $"{victim.Data.PlayerName} killed. Kills left: {PossKills.Value - ku - 1}.");
        return true;
    }

    public static string MyRole(PlayerControl p, bool ru)
    {
        if (!Dead(p)) return null;
        var xg = Extra33.GhostOf(p); if (xg != null) return Extra33.GhostDesc(xg, ru);
        if (ImpTeam(p)) return PossOn != null && PossOn.Value ? Desc("possessor", ru) : null;
        if (IsHelper(p)) return Desc("helper", ru);
        return On != null && On.Value ? Desc("poltergeist", ru) : null;
    }

    public static string Find(string q, bool ru)
    {
        q = q.ToLowerInvariant();
        foreach (var k in new[] { "poltergeist", "possessor", "helper" })
            if (Name(k, true).ToLowerInvariant().StartsWith(q) || Name(k, false).ToLowerInvariant().StartsWith(q)) return Name(k, ru) + " (" + (ru ? "роль призраков" : "ghost role") + ")\n" + Desc(k, ru);
        return Extra33.GhostFind(q, ru);
    }

    public static string List(bool ru) => (ru ? "Призраки: " : "Ghosts: ") + string.Join(", ", new[] { "poltergeist", "possessor", "helper" }.Select(k => Name(k, ru)));

    static string Name(string k, bool ru) => k switch
    {
        "possessor" => ru ? "Вселенец" : "Possessor",
        "helper" => ru ? "Помощник по заданиям" : "Task Helper",
        _ => ru ? "Полтергейст" : "Poltergeist"
    };

    static string Desc(string k, bool ru) => k switch
    {
        "possessor" => ru ? "Призрак-предатель. /haunt ID - вселиться в живого предателя до собрания, видеть где он и кто рядом; /hk - убить ближайшего мирного его руками." : "Impostor ghost. /haunt ID - possess a living impostor until the meeting and see where he is and who is near; /hk - kill the nearest crewmate with his hands.",
        "helper" => ru ? "Призрак-мирный. /task ID - сложная задача на счёт, /solve число - верный ответ засчитывает игроку задание." : "Crewmate ghost. /task ID - a hard maths puzzle, /solve number - a correct answer completes a task for that player.",
        _ => ru ? "Призрак (нейтрал). На собрании /boo [текст] - всем придёт рассыпанное слово или фраза без имени." : "Neutral ghost. In a meeting /boo [text] - everyone gets a scrambled word or sentence with no name."
    };

    public static bool Handle(PlayerControl me, string cmd, string[] a = null)
    {
        if (Extra33.HandleGhost(me, cmd, a ?? Array.Empty<string>())) return true;
        if (cmd == "haunt" || cmd == "possess" || cmd == "hk") return HandlePossess(me, cmd, a ?? Array.Empty<string>());
        if (cmd == "task" || cmd == "helper" || cmd == "solve") return HandleHelper(me, cmd, a ?? Array.Empty<string>());
        return HandleBoo(me, cmd, a);
    }

    static string Shuffle(string w, bool keepEdges)
    {
        var ch = w.ToLowerInvariant().ToCharArray();
        int lo = keepEdges ? 1 : 0, hi = keepEdges ? ch.Length - 2 : ch.Length - 1;
        if (hi - lo < 1 || ch.Skip(lo).Take(hi - lo + 1).Distinct().Count() < 2) return new string(ch);
        string orig = new string(ch);
        for (int tries = 0; tries < 12; tries++)
        {
            for (int i = hi; i > lo; i--) { int j = lo + rng.Next(i - lo + 1); (ch[i], ch[j]) = (ch[j], ch[i]); }
            if (new string(ch) != orig) break;
        }
        return new string(ch);
    }

    static string Scramble(string w)
    {
        var words = w.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            var arr = words.ToArray();
            if (arr.Distinct().Count() > 1)
                for (int tries = 0; tries < 12; tries++)
                {
                    for (int i = arr.Length - 1; i > 0; i--) { int j = rng.Next(i + 1); (arr[i], arr[j]) = (arr[j], arr[i]); }
                    if (!arr.SequenceEqual(words)) break;
                }
            return string.Join(" / ", arr.Select(x => Shuffle(x, false)));
        }
        return Shuffle(w, false);
    }

    public static string Mumble(string w) => Glitch(Scramble(w));

    static readonly string[] Fill = { ".", "..", "~", "'", "*", "-", "`", "," };

    static string Glitch(string s)
    {
        var sb = new System.Text.StringBuilder("~ ");
        foreach (char c in s)
        {
            if (c == '/') { sb.Append(" ~~~ "); continue; }
            if (c == ' ') { if (rng.Next(2) == 0) sb.Append(' '); continue; }
            sb.Append(rng.Next(2) == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            int k = rng.Next(4);
            if (k == 0) sb.Append(Fill[rng.Next(Fill.Length)]);
            else if (k == 1) sb.Append(Fill[rng.Next(Fill.Length)]).Append(Fill[rng.Next(Fill.Length)]);
            else if (k == 2) sb.Append(' ');
        }
        return sb.Append(" ~").ToString();
    }

    static bool HandleBoo(PlayerControl me, string cmd, string[] a)
    {
        if (cmd != "boo" && cmd != "poltergeist" && cmd != "бу" && cmd != "полтергейст") return false;
        bool ru = Translator.IsRu(me);
        void Tell(string r, string e) => Utils.SendPrivate(me, ru ? r : e);
        if (On == null || !On.Value) { Tell("Роль Полтергейст выключена.", "The Poltergeist role is off."); return true; }
        if (Dead(me) && ImpTeam(me) && PossOn != null && PossOn.Value) { Tell("Ты Вселенец: /haunt ID и /hk.", "You are the Possessor: /haunt ID and /hk."); return true; }
        if (IsHelper(me)) { Tell("Ты Помощник по заданиям: /task и /solve.", "You are the Task Helper: /task and /solve."); return true; }
        if (!Dead(me)) { Tell("Полтергейстом может быть только призрак.", "Only a ghost can be the Poltergeist."); return true; }
        if (MeetingHud.Instance == null) { Tell("Полтергейст шепчет только на собрании.", "The Poltergeist whispers only during a meeting."); return true; }
        used.TryGetValue(me.PlayerId, out int n);
        if (n >= Uses.Value) { Tell("Ты уже использовал все шепоты.", "You have used all your whispers."); return true; }
        used[me.PlayerId] = n + 1;
        var list = ru ? WordsRu : WordsEn;
        string w = list[rng.Next(list.Length)];
        if (a != null && a.Length > 0)
        {
            string own = string.Join(" ", a).Replace("<", "").Replace(">", "").Trim();
            if (own.Length > 60) own = own.Substring(0, 60).Trim();
            if (own.Length > 0) w = own;
        }
        Utils.Announce(Glitch(Scramble(w)));
        Tell($"Ты загадал: «{w}». Осталось шепотов: {Uses.Value - n - 1}.", $"Your word: \"{w}\". Whispers left: {Uses.Value - n - 1}.");
        return true;
    }
}
