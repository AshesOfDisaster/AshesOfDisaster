using System;
using System.Linq;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class PetAll
{
    static float next;

    public static string HostPet()
    {
        try { var id = PlayerControl.LocalPlayer?.Data?.DefaultOutfit?.PetId; if (!string.IsNullOrWhiteSpace(id)) return id; } catch { }
        return "pet_test";
    }

    public static void Tick()
    {
        if (Shroomer.Active) return;
        if (!Utils.AmHost || Opts.PetForAll == null || !Opts.PetForAll.Value) return;
        var c = AmongUsClient.Instance;
        if (c == null || c.GameState != InnerNetClient.GameStates.Started || Time.time < next) return;
        next = Time.time + 4f;
        Apply();
    }

    public static void Apply()
    {
        string pet = HostPet();
        foreach (var p in PlayerControl.AllPlayerControls.ToArray())
        {
            if (p == null || p.Data == null || p.AmOwner || p.Data.Disconnected || p.Data.IsDead) continue;
            try
            {
                if (p.Data.DefaultOutfit.PetId == pet) continue;
                p.Data.DefaultOutfit.PetId = pet;
                Sync(p);
                Plugin.Logger.LogInfo($"PetAll: {p.Data.PlayerName} -> {pet}");
            }
            catch (Exception e) { Plugin.Logger.LogWarning("PetAll: " + e.Message); }
        }
    }

    static void Sync(PlayerControl p)
    {
        var w = MessageWriter.Get(SendOption.Reliable);
        try
        {
            w.StartMessage(5);
            w.Write(AmongUsClient.Instance.GameId);
            w.StartMessage(1);
            w.WritePacked(p.Data.NetId);
            p.Data.Serialize(w, false);
            w.EndMessage();
            w.StartMessage(2);
            w.WritePacked(p.NetId);
            w.Write((byte)RpcCalls.Shapeshift);
            w.WritePacked(p.NetId);
            w.Write(false);
            w.EndMessage();
            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
        }
        finally { w.Recycle(); }
    }
}
