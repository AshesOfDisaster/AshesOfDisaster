using AmongUs.GameOptions;
using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AshesOfDisaster;

public static class NeutralMask
{
    static bool Neutral(PlayerControl p) => p != null && RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && RoleLogic.RealImpNeutrals.Contains(r.Id);

    static void Send(PlayerControl to, PlayerControl who)
    {
        var w = AmongUsClient.Instance.StartRpcImmediately(who.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, to.OwnerId);
        w.Write((ushort)RoleTypes.Crewmate);
        w.Write(true);
        AmongUsClient.Instance.FinishRpcImmediately(w);
    }

    public static void Apply()
    {
        try
        {
            if (!Utils.AmHost || AmongUsClient.Instance == null) return;
            var all = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null && !p.Data.Disconnected && RoleLogic.IsImpRole(p.Data.RoleType)).ToList();
            int n = 0;
            foreach (var a in all)
            {
                if (a.AmOwner) continue;
                foreach (var b in all)
                {
                    if (a.PlayerId == b.PlayerId) continue;
                    try { Send(a, b); n++; }
                    catch (Exception e) { Plugin.Logger.LogWarning("NeutralMask: " + e.Message); }
                }
            }
            Plugin.Logger.LogInfo($"NeutralMask: hid {n} impostor-team names");
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NeutralMask.Apply: " + e.Message); }
    }

    public static bool ImpTeam(PlayerControl p) => p != null && p.Data != null && RoleLogic.IsImpRole(p.Data.RoleType);
}

[HarmonyPatch(typeof(PlayerControl), "FixedUpdate")]
public static class NeutralMaskNamePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        try
        {
            var me = PlayerControl.LocalPlayer;
            if (!Utils.AmHost || me == null || __instance == null || __instance.AmOwner) return;
            if (!NeutralMask.ImpTeam(me) || !NeutralMask.ImpTeam(__instance)) return;
            var t = __instance.cosmetics != null ? __instance.cosmetics.nameText : null;
            if (t != null) t.color = Color.white;
        }
        catch { }
    }
}
