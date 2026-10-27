using System;
using System.Linq;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class NeutralWin
{
    static float nextTick, endAt;
    static bool hadTrueImp, announced;

    public static void Reset() { hadTrueImp = false; announced = false; endAt = 0f; }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.5f;
        try
        {
            var c = AmongUsClient.Instance;
            if (c == null || c.GameState != InnerNetClient.GameStates.Started || ShipStatus.Instance == null) { Reset(); return; }
            if (announced)
            {
                if (Time.time >= endAt) { announced = false; endAt = float.MaxValue; Utils.EndGame(); }
                return;
            }
            if (endAt == float.MaxValue) return;
            if (MeetingHud.Instance != null || RoleState.Assigned.Count == 0) return;
            int trueImps = 0;
            var killers = new System.Collections.Generic.List<PlayerControl>();
            foreach (var p in PlayerControl.AllPlayerControls.ToArray())
            {
                if (!Alive(p)) continue;
                bool neutralKiller = RoleState.Assigned.TryGetValue(p.PlayerId, out var r) && RoleLogic.RealImpNeutrals.Contains(r.Id);
                if (neutralKiller) { killers.Add(p); continue; }
                if (RoleLogic.IsImpRole(p.Data.RoleType) || RoleLogic.ForcedImp.Contains(p.PlayerId)) trueImps++;
            }
            if (trueImps > 0) { hadTrueImp = true; return; }
            if (!hadTrueImp || killers.Count == 0) return;
            string names = string.Join(", ", killers.Select(k => k.Data.PlayerName + " (" + (RoleState.Assigned.TryGetValue(k.PlayerId, out var rr) ? rr.Ru : "?") + ")"));
            Plugin.Logger.LogInfo("NeutralWin: all impostors are dead, neutral killer wins: " + names);
            Utils.Announce("Все предатели убиты! Побеждает нейтрал: " + names);
            announced = true;
            endAt = Time.time + 2f;
        }
        catch (Exception e) { Plugin.Logger.LogWarning("NeutralWin.Tick: " + e.Message); }
    }
}
