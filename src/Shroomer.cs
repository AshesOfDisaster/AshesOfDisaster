using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

public static class Shroomer
{
    static readonly Dictionary<byte, float> last = new();
    static readonly Dictionary<byte, int> uses = new();
    static readonly Dictionary<byte, byte> original = new();
    static readonly Dictionary<byte, string> originalNames = new();
    static readonly Dictionary<byte, (string hat, string skin, string visor, string pet)> originalCos = new();
    static float restoreAt = -1f;
    public static bool Active => restoreAt > 0f;
    static readonly System.Random rng = new();

    public static void Reset() { last.Clear(); uses.Clear(); original.Clear(); originalNames.Clear(); originalCos.Clear(); restoreAt = -1f; }
    static int Left(byte id) => Math.Max(0, Opts.ShroomUses.Value - (uses.TryGetValue(id, out var u) ? u : 0));

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        return ru ? $"{r.Colored(true)}: питомец запускает грибной саботаж (на любой карте). Осталось запусков: {Left(p.PlayerId)}."
                  : $"{r.Colored(false)}: pet starts the mushroom sabotage (on any map). Starts left: {Left(p.PlayerId)}.";
    }

    static bool TryMushroom(out SystemTypes mm)
    {
        mm = default;
        if (!Enum.TryParse<SystemTypes>("MushroomMixupSabotage", out mm)) return false;
        var ss = ShipStatus.Instance;
        try { return ss != null && ss.Systems != null && ss.Systems.ContainsKey(mm); } catch { return false; }
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        void H(string s) => RoleLogic.Hint(p, s);
        var ss = ShipStatus.Instance;
        if (ss == null) return true;
        if (Left(p.PlayerId) <= 0) { H(ru ? "Грибы закончились." : "No mushrooms left."); return true; }
        if (last.TryGetValue(p.PlayerId, out var l) && Time.time - l < Opts.ShroomCooldown.Value)
        { H(ru ? $"Перезарядка: ещё {Mathf.CeilToInt(Opts.ShroomCooldown.Value - (Time.time - l))} с." : "Cooling down."); return true; }
        if (restoreAt > 0f) { H(ru ? "Грибы уже действуют." : "Mushrooms are already active."); return true; }

        try
        {
            if (TryMushroom(out var mm))
            {
                ss.RpcUpdateSystem(SystemTypes.Sabotage, (byte)mm);
                Plugin.Logger.LogInfo($"Shroomer {p.Data.PlayerName}: real mushroom sabotage");
            }
            else Shuffle();
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Shroomer: " + e.Message); H(ru ? "Не получилось." : "Failed."); return true; }
        last[p.PlayerId] = Time.time; uses[p.PlayerId] = (uses.TryGetValue(p.PlayerId, out var u) ? u : 0) + 1;
        H(ru ? $"Грибы запущены. Осталось: {Left(p.PlayerId)}." : $"Mushrooms started. Left: {Left(p.PlayerId)}.");
        return true;
    }

    static void Shuffle()
    {
        var alive = PlayerControl.AllPlayerControls.ToArray().Where(o => o != null && o.Data != null && !o.Data.IsDead && !o.Data.Disconnected).ToList();
        if (alive.Count < 2) return;
        original.Clear(); originalNames.Clear();
        foreach (var o in alive) { original[o.PlayerId] = (byte)o.Data.DefaultOutfit.ColorId; originalNames[o.PlayerId] = o.Data.PlayerName; var oc = o.Data.DefaultOutfit; originalCos[o.PlayerId] = (oc.HatId, oc.SkinId, oc.VisorId, oc.PetId); }
        int shift = rng.Next(1, alive.Count);
        restoreAt = Time.time + Opts.ShroomSeconds.Value;
        for (int i = 0; i < alive.Count; i++)
        {
            try { alive[i].RpcSetColor(15); } catch (Exception e) { Plugin.Logger.LogWarning("Shroomer color: " + e.Message); }
            try { alive[i].RpcSetHat("hat_NoHat"); alive[i].RpcSetSkin("skin_None"); alive[i].RpcSetVisor("visor_EmptyVisor"); alive[i].RpcSetPet(""); } catch (Exception e) { Plugin.Logger.LogWarning("Shroomer cosmetics: " + e.Message); }
            try { alive[i].RpcSetName(originalNames[alive[(i + shift) % alive.Count].PlayerId]); } catch (Exception e) { Plugin.Logger.LogWarning("Shroomer name: " + e.Message); }
        }
        Plugin.Logger.LogInfo($"Shroomer: everyone gray with shuffled names for {Opts.ShroomSeconds.Value} s");
    }

    public static void Restore()
    {
        restoreAt = -1f;
        if (original.Count == 0) return;
        foreach (var kv in original.ToList())
        {
            var o = Utils.GetPlayer(kv.Key);
            if (o == null || o.Data == null || o.Data.Disconnected) continue;
            try { o.RpcSetColor(kv.Value); } catch (Exception e) { Plugin.Logger.LogWarning("Shroomer restore color: " + e.Message); }
            try { if (originalCos.TryGetValue(kv.Key, out var cs)) { o.RpcSetHat(cs.hat); o.RpcSetSkin(cs.skin); o.RpcSetVisor(cs.visor); o.RpcSetPet(cs.pet ?? ""); } } catch (Exception e) { Plugin.Logger.LogWarning("Shroomer restore cosmetics: " + e.Message); }
            try { if (originalNames.TryGetValue(kv.Key, out var nm)) o.RpcSetName(nm); } catch (Exception e) { Plugin.Logger.LogWarning("Shroomer restore name: " + e.Message); }
        }
        original.Clear(); originalNames.Clear(); originalCos.Clear();
        Plugin.Logger.LogInfo("Shroomer: colors, names and cosmetics restored");
    }

    public static void Tick()
    {
        if (restoreAt > 0f && (Time.time >= restoreAt || MeetingHud.Instance != null)) Restore();
    }
}
