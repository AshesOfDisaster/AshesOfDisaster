using System;
using System.Collections.Generic;
using System.Linq;

namespace AshesOfDisaster;

// Карточка роли: простым языком, кто ты, в чём цель, как пользоваться, что делать сейчас.
public static class RoleGuide
{
    static readonly System.Random rng = new();

    public static bool IsKiller(RoleDef r) => r != null && RoleLogic.RealImpNeutrals.Contains(r.Id);

    public static string Team(RoleDef r, bool ru)
    {
        switch (r.Tab)
        {
            case OptTab.Impostor: return ru ? "<color=#FF5555>предатель</color>" : "<color=#FF5555>impostor</color>";
            case OptTab.Neutral:
                return IsKiller(r) ? (ru ? "<color=#FFA726>нейтрал-убийца</color>" : "<color=#FFA726>neutral killer</color>")
                                   : (ru ? "<color=#FFEE58>нейтрал</color>" : "<color=#FFEE58>neutral</color>");
            case OptTab.Mystic: return ru ? "<color=#BA68C8>мистик</color>" : "<color=#BA68C8>mystic</color>";
            default: return ru ? "<color=#69F0AE>мирный</color>" : "<color=#69F0AE>crewmate</color>";
        }
    }

    public static string Goal(RoleDef r, bool ru)
    {
        switch (r.Tab)
        {
            case OptTab.Impostor: return ru ? "убить мирных и не выдать себя" : "kill the crew without being caught";
            case OptTab.Neutral:
                return IsKiller(r) ? (ru ? "остаться последним: убивай питомцем, предатели тебе враги" : "be the last one alive: kill with the pet, impostors are your enemies")
                                   : (ru ? "выполнить свою особую цель (смотри /m)" : "reach your own special goal (see /m)");
            case OptTab.Mystic: return ru ? "помочь мирным найти предателей" : "help the crew find the impostors";
            default: return ru ? "выполнить задания или вычислить предателей на собрании" : "finish tasks or find the impostors at a meeting";
        }
    }

    static string Tag(RoleDef r, bool ru)
    {
        string d = ru ? r.DescRu : r.DescEn;
        if (string.IsNullOrEmpty(d)) return "";
        int a = d.LastIndexOf('['), b = d.LastIndexOf(']');
        return a >= 0 && b > a ? d.Substring(a + 1, b - a - 1).Trim() : "";
    }

    public static string How(RoleDef r, bool ru)
    {
        string t = Tag(r, ru).ToLowerInvariant();
        if (t.StartsWith("пит") || t.StartsWith("pet")) return ru ? "погладь питомца (кнопка питомца)" : "pet your pet (the pet button)";
        if (t.StartsWith("сам") || t.StartsWith("pas")) return ru ? "ничего нажимать не надо, роль работает сама" : "nothing to press, the role works by itself";
        if (t.StartsWith("соб") || t.StartsWith("meet")) return ru ? "на собрании напиши /ab" : "at a meeting type /ab";
        return ru ? "смотри описание: /m" : "see the description: /m";
    }

    public static string Description(RoleDef r, bool ru)
    {
        string d = ru ? r.DescRu : r.DescEn;
        if (string.IsNullOrEmpty(d)) return "";
        int a = d.LastIndexOf('[');
        return (a > 0 ? d.Substring(0, a) : d).Trim();
    }

    // Короткая строка для ника
    public static string Line(RoleDef r, bool ru) => r.Colored(ru) + " <color=#BBBBBB>· " + Team(r, ru) + "</color>";

    // Полная карточка (в чат: /m и старт игры)
    public static string Card(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        var sb = new System.Text.StringBuilder();
        sb.Append(ru ? "Ты: " : "You are: ").Append(r.Colored(ru)).Append(" (").Append(Team(r, ru)).Append(")\n");
        sb.Append(ru ? "Цель: " : "Goal: ").Append(Goal(r, ru)).Append('\n');
        sb.Append(ru ? "Как пользоваться: " : "How to use: ").Append(How(r, ru)).Append('\n');
        string hint = RoleHints.Get(r.Id, ru);
        if (!string.IsNullOrEmpty(hint)) sb.Append(ru ? "Способность: " : "Ability: ").Append(hint).Append('\n');
        string d = Description(r, ru);
        if (!string.IsNullOrEmpty(d)) sb.Append(ru ? "Подробно: " : "Details: ").Append(d).Append('\n');
        sb.Append(ru ? "Совет: " : "Tip: ").Append(TipFor(r, ru));
        return sb.ToString();
    }

    static readonly string[] CrewRu =
    {
        "Не стой на месте: свидетель убийства стоит дороже любого задания.",
        "Если видишь подозрительного, держись рядом с другими, в одиночку убивают чаще.",
        "На собрании называй комнату и время: по этому проще поймать вранье.",
        "Не обвиняй без причины: у предателей много союзников среди нервных мирных.",
    };
    static readonly string[] CrewEn =
    {
        "Do not stand still: a witness of a kill is worth more than any task.",
        "If someone looks suspicious, stay near others: lone players die more often.",
        "At a meeting give the room and the time: lies are easier to catch that way.",
        "Do not accuse without a reason: impostors love nervous crewmates.",
    };
    static readonly string[] ImpRu =
    {
        "Убивай там, где нет свидетелей, и сразу уходи в другую комнату.",
        "Не убивай рядом с камерами и админом, если там кто-то сидит.",
        "На собрании назови комнату, где ты был, и придерживайся её.",
        "Подставь кого-то, кто рядом с телом, но не перегибай.",
    };
    static readonly string[] ImpEn =
    {
        "Kill where there are no witnesses and leave the room at once.",
        "Do not kill near cameras or admin when somebody watches them.",
        "At a meeting name the room you were in and stick to it.",
        "Frame somebody near the body, but do not overdo it.",
    };
    static readonly string[] NeuRu =
    {
        "Ты играешь сам за себя: не раскрывай роль, пока это не выгодно.",
        "Следи, кого мирные и предатели считают угрозой, и держись в тени.",
        "Читай описание роли: условие победы у каждого нейтрала своё.",
    };
    static readonly string[] NeuEn =
    {
        "You play for yourself: hide your role until it pays off.",
        "Watch whom crew and impostors treat as a threat and stay in the shadows.",
        "Read your role description: each neutral has its own win condition.",
    };

    public static string TipFor(RoleDef r, bool ru)
    {
        string[] pool = r.Tab == OptTab.Impostor ? (ru ? ImpRu : ImpEn) : r.Tab == OptTab.Neutral ? (ru ? NeuRu : NeuEn) : (ru ? CrewRu : CrewEn);
        return pool[rng.Next(pool.Length)];
    }

    // Совет по команде /tip (без роли, для призраков)
    public static string AnyTip(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        if (RoleState.Assigned.TryGetValue(p.PlayerId, out var r)) return (ru ? "Совет: " : "Tip: ") + TipFor(r, ru);
        return ru ? "Совет: ты призрак. Помогай своей команде, у призраков есть свои способности: /m" : "Tip: you are a ghost. Help your team, ghosts have abilities: /m";
    }
}
