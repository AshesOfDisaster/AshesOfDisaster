using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class Buttons
{
    public static bool On => Opts.ButtonAbilities != null && Opts.ButtonAbilities.Value;

    static readonly HashSet<string> PetRoles = new() { "Watcher", "Beacon", "Sapper", "Coach", "Telegraphist", "Banisher", "Bomber", "Spy", "Gravedigger", "Silencer", "Collector", "Broker", "Boomerang", "Sorcerer", "Sentry", "Intercessor", "Forensic", "Minelayer", "Trapper", "Hypnotist", "Joker", "Reaper", "Poisoner", "Hunter", "Executioner", "Detonator", "Sleeper", "Plague", "Teacher", "Gardener", "Thief", "Necromancer", "Runologist", "Interrogator", "Sealer", "Snarer", "Beguiler", "Pouncer", "Taskmaster", "Coroner", "Tripper", "Dazer", "Digger", "Liquidator", "Duelist", "Arsonist", "Herbalist", "Welder", "Smoker", "Cloud", "Witch", "Builder", "Freeman", "Ventbomber", "Ventsleuth", "Portalist", "Mirrorer", "Charon", "Switcher", "Pinkman", "Prankster", "Lurer", "Carousel", "Ghoul", "Meteor", "Returner", "Navigator", "Hearthkeeper", "Distracter", "Mercenary", "Pawnbroker", "Tracker", "Psychologist" };

    public static bool UsesPet(string id) => id != null && PetRoles.Contains(id);

    public static bool PetBlocked(PlayerControl p)
        => On && !(p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && PetRoles.Contains(r.Id));

    static string RT(PlayerControl p) => p == null || p.Data == null ? "" : p.Data.RoleType.ToString();

    static bool Ready() => Utils.AmHost && On && AmongUsClient.Instance != null
        && AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started && MeetingHud.Instance == null;

    public static void AssignVanilla(IEnumerable<PlayerControl> all)
    {
        if (!On) return;
        foreach (var p in all)
        {
            try
            {
                if (p == null || p.Data == null || !RoleState.Assigned.TryGetValue(p.PlayerId, out var ar) || PetRoles.Contains(ar.Id)) continue;
                string t = RT(p);
                if (t == "Impostor") RoleLogic.SetVanillaRole(p, RoleTypes.Shapeshifter);
                else if (t == "Crewmate") RoleLogic.SetVanillaRole(p, RoleTypes.Engineer);
            }
            catch (Exception e) { Plugin.Logger.LogWarning("Buttons.Assign: " + e.Message); }
        }
    }

    public static bool Shift(PlayerControl p)
    {
        if (!Ready() || p == null || p.Data == null || p.Data.IsDead) return false;
        if (!RoleState.Assigned.TryGetValue(p.PlayerId, out var r) || r.Id == "God" || PetRoles.Contains(r.Id)) return false;
        if (RT(p) != "Shapeshifter") return false;
        RoleLogic.OnPet(p, "shift");
        return true;
    }

    public static void Vent(PlayerControl p, int id)
    {
        if (!Ready() || p == null || p.Data == null || p.Data.IsDead) return;
        if (!RoleState.Assigned.TryGetValue(p.PlayerId, out var vr) || PetRoles.Contains(vr.Id) || RT(p) != "Engineer") return;
        RoleLogic.OnPet(p, "vent");
        Extra33.QueueBoot(p.PlayerId, id);
    }

    public static bool Ghost(PlayerControl p, PlayerControl target)
    {
        if (!Utils.AmHost || !On || p == null || p.Data == null || !p.Data.IsDead) return false;
        return Extra33.GhostButton(p, target);
    }

    public static bool NeutralBlocked(PlayerControl p)
        => p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && r.Tab == OptTab.Neutral;

    public static string HowTo(PlayerControl p)
    {
        if (!On || p == null || p.Data == null) return "";
        bool ru = Translator.IsRu(p);
        if (p.Data.IsDead) return ru ? "Способность: кнопка защиты (щит) на нужном игроке." : "Ability: the protect (shield) button on a player.";
        if (RoleState.Assigned.TryGetValue(p.PlayerId, out var hr) && PetRoles.Contains(hr.Id)) return ru ? "Способность: погладь питомца." : "Ability: pet your pet.";
        string t = RT(p);
        if (t == "Shapeshifter") return ru ? "Способность: кнопка смены облика (облик не меняется)." : "Ability: the shapeshift button (your look does not change).";
        if (t == "Engineer") return ru ? "Способность: войти в любой люк (сразу выкинет обратно)." : "Ability: enter any vent (you are thrown out at once).";
        return "";
    }

    static readonly (Regex re, string ru, string en)[] rules =
    {
        (new Regex(@"\[Питомец\]"), "[Кнопка]", null),
        (new Regex(@"\[Pet\]"), null, "[Button]"),
        (new Regex(@"Питомцем\b"), "Кнопкой способности", null),
        (new Regex(@"питомцем\b"), "кнопкой способности", null),
        (new Regex(@"Питомца\b"), "Кнопки способности", null),
        (new Regex(@"питомца\b"), "кнопки способности", null),
        (new Regex(@"Питомец\b"), "Кнопка способности", null),
        (new Regex(@"питомец\b"), "кнопка способности", null),
        (new Regex(@"\bThe pet\b"), null, "The ability button"),
        (new Regex(@"\bthe pet\b"), null, "the ability button"),
        (new Regex(@"\b[Yy]our pet\b"), null, "your ability button"),
        (new Regex(@"\bpet:"), null, "button:"),
    };

    public static string Fix(PlayerControl to, string text)
    {
        if (!On || to == null || !PetBlocked(to) || string.IsNullOrEmpty(text) || text.IndexOf("етом", StringComparison.Ordinal) < 0 && text.IndexOf("itom", StringComparison.Ordinal) < 0
            && text.IndexOf("pet", StringComparison.OrdinalIgnoreCase) < 0) return text;
        foreach (var (re, ru, en) in rules)
        {
            string rep = ru ?? en;
            text = re.Replace(text, rep);
        }
        return text;
    }
}

[HarmonyPatch(typeof(PlayerControl), "CheckShapeshift")]
public static class ButtonShiftPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        try
        {
            if (Buttons.Shift(__instance)) { __instance.RpcRejectShapeshift(); return false; }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("ButtonShift: " + e.Message); }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), "CheckProtect")]
public static class ButtonProtectPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        try { if (Buttons.Ghost(__instance, target)) return false; }
        catch (Exception e) { Plugin.Logger.LogWarning("ButtonProtect: " + e.Message); }
        return true;
    }
}

[HarmonyPatch(typeof(ShipStatus), "UpdateSystem", typeof(SystemTypes), typeof(PlayerControl), typeof(Hazel.MessageReader))]
public static class NeutralSabotagePatch
{
    public static bool Prefix([HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player)
    {
        try
        {
            if (!Utils.AmHost || systemType != SystemTypes.Sabotage || !Buttons.NeutralBlocked(player)) return true;
            RoleLogic.Hint(player, Translator.IsRu(player) ? "Нейтралам саботажи недоступны." : "Neutrals cannot sabotage.");
            return false;
        }
        catch { return true; }
    }
}
