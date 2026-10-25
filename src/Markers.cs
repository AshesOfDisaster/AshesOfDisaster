using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

// Значки на карте: то, что поставили роли (силки, мины, печати, очаги, руны, бомбы, цветы, паутина, тучи, котёл).
// Создаётся «ненастоящий игрок» с id 254, его ник - это текст значка.
// Видит значок только тот, кто поставил. Всё создаётся только у хоста.
public static class Markers
{
    class M
    {
        public string Key, Text; public byte Owner; public Vector2 Pos;
        public PlayerControl Pc; public int Stage; public float At;
    }

    static readonly Dictionary<string, M> all = new();
    static float nextSync, blockUntil;
    const int MaxMarkers = 24;

    static int ClientOf(byte id) { var p = Utils.GetPlayer(id); return p != null ? p.OwnerId : -1; }

    public static void Clear()
    {
        foreach (var m in all.Values.ToList()) Despawn(m);
        all.Clear();
        blockUntil = Time.time + 8f;
    }

    static void Despawn(M m)
    {
        try
        {
            if (m.Pc == null) return;
            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(5);
            w.Write(AmongUsClient.Instance.GameId);
            w.StartMessage(5);
            w.WritePacked(m.Pc.NetId);
            w.EndMessage();
            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
            w.Recycle();
            AmongUsClient.Instance.RemoveNetObject(m.Pc);
            UnityEngine.Object.Destroy(m.Pc.gameObject);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Markers.Despawn: " + e.Message); }
        m.Pc = null;
    }

    static void Spawn(M m)
    {
        var pc = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
        pc.PlayerId = 254;
        pc.isNew = false;
        pc.notRealPlayer = true;
        try { pc.NetTransform.SnapTo(new Vector2(50f, 50f)); } catch (Exception e) { Plugin.Logger.LogWarning("Markers.Snap0: " + e.Message); }
        AmongUsClient.Instance.NetIdCnt += 1U;
        var msg = MessageWriter.Get(SendOption.Reliable);
        msg.StartMessage(5);
        msg.Write(AmongUsClient.Instance.GameId);
        msg.StartMessage(4);
        var item = AmongUsClient.Instance.CreateSpawnMessage(pc, -2, SpawnFlags.None);
        item.SerializeValues(msg);
        msg.EndMessage();
        for (uint i = 1; i <= 3; ++i)
        {
            msg.StartMessage(4);
            msg.WritePacked(2U);
            msg.WritePacked(-2);
            msg.Write((byte)SpawnFlags.None);
            msg.WritePacked(1);
            msg.WritePacked(AmongUsClient.Instance.NetIdCnt - i);
            msg.StartMessage(1);
            msg.EndMessage();
            msg.EndMessage();
        }
        msg.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(msg);
        msg.Recycle();
        m.Pc = pc;
        try { if (PlayerControl.AllPlayerControls.Contains(pc)) PlayerControl.AllPlayerControls.Remove(pc); } catch { }
        try { pc.cosmetics.currentBodySprite.BodySprite.color = Color.clear; } catch { }
        try { pc.cosmetics.colorBlindText.color = Color.clear; } catch { }
    }

    static void InitForOthers(M m)
    {
        var pc = m.Pc;
        var host = PlayerControl.LocalPlayer;
        foreach (var o in PlayerControl.AllPlayerControls.ToArray())
        {
            if (o == null || o.AmOwner || o.Data == null || o.Data.Disconnected) continue;
            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(26);
            w.WritePacked(AmongUsClient.Instance.GameId);
            w.StartMessage(6);
            w.Write(AmongUsClient.Instance.GameId);
            w.WritePacked(o.OwnerId);
            w.StartMessage(1);
            w.WritePacked(pc.NetId);
            w.Write(o.PlayerId);
            w.EndMessage();
            w.StartMessage(2);
            w.WritePacked(pc.NetId);
            w.Write((byte)RpcCalls.MurderPlayer);
            w.WritePacked(pc.NetId);
            w.Write((int)MurderResultFlags.FailedError);
            w.EndMessage();
            w.StartMessage(1);
            w.WritePacked(pc.NetId);
            w.Write((byte)254);
            w.EndMessage();
            w.EndMessage();
            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
            w.Recycle();
        }
        try { pc.CachedPlayerData = host.Data; } catch { }
    }

    static void HideFromOthers(M m)
    {
        if (Opts.MarkersAll != null && Opts.MarkersAll.Value) return;
        var pc = m.Pc;
        foreach (var o in PlayerControl.AllPlayerControls.ToArray())
        {
            if (o == null || o.Data == null || o.Data.Disconnected || o.PlayerId == m.Owner) continue;
            if (o.AmOwner)
            {
                try { pc.transform.Find("Names").Find("NameText_TMP").gameObject.SetActive(false); } catch { }
                try { pc.Visible = false; } catch { }
                continue;
            }
            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(6);
            w.Write(AmongUsClient.Instance.GameId);
            w.WritePacked(o.OwnerId);
            w.StartMessage(5);
            w.WritePacked(pc.NetId);
            w.EndMessage();
            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
            w.Recycle();
        }
    }

    static void ApplySprite(M m)
    {
        var pc = m.Pc;
        var host = PlayerControl.LocalPlayer;
        var outfit = host.Data.Outfits[PlayerOutfitType.Default];
        string name = outfit.PlayerName; int colorId = outfit.ColorId;
        string hat = outfit.HatId, skin = outfit.SkinId, pet = outfit.PetId, visor = outfit.VisorId;
        int target = Opts.MarkersAll != null && Opts.MarkersAll.Value ? -1 : ClientOf(m.Owner);
        var w = MessageWriter.Get(SendOption.Reliable);
        if (target >= 0 && !(Utils.GetPlayer(m.Owner)?.AmOwner ?? false))
        {
            w.StartMessage(6); w.Write(AmongUsClient.Instance.GameId); w.WritePacked(target);
        }
        else { w.StartMessage(5); w.Write(AmongUsClient.Instance.GameId); }

        outfit.PlayerName = "<size=14><br></size>" + m.Text;
        outfit.ColorId = 0; outfit.HatId = ""; outfit.SkinId = ""; outfit.PetId = ""; outfit.VisorId = "";
        w.StartMessage(1);
        w.WritePacked(host.Data.NetId);
        host.Data.Serialize(w, false);
        w.EndMessage();
        try { pc.Shapeshift(host, false); } catch (Exception e) { Plugin.Logger.LogWarning("Markers.Shapeshift: " + e.Message); }
        w.StartMessage(2);
        w.WritePacked(pc.NetId);
        w.Write((byte)RpcCalls.Shapeshift);
        w.WritePacked(host.NetId);
        w.Write(false);
        w.EndMessage();

        outfit.PlayerName = name; outfit.ColorId = colorId; outfit.HatId = hat; outfit.SkinId = skin; outfit.PetId = pet; outfit.VisorId = visor;
        w.StartMessage(1);
        w.WritePacked(host.Data.NetId);
        host.Data.Serialize(w, false);
        w.EndMessage();

        try { pc.NetTransform.SnapTo(m.Pos); } catch (Exception e) { Plugin.Logger.LogWarning("Markers.Snap: " + e.Message); }
        w.StartMessage(2);
        w.WritePacked(pc.NetTransform.NetId);
        w.Write((byte)RpcCalls.SnapTo);
        NetHelpers.WriteVector2(m.Pos, w);
        w.Write(pc.NetTransform.lastSequenceId);
        w.EndMessage();
        w.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(w);
        w.Recycle();
    }

    static bool Busy()
    {
        try { if (MeetingHud.Instance != null) return true; } catch { }
        try { if (UnityEngine.Object.FindObjectOfType<ExileController>() != null) return true; } catch { }
        return false;
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Opts.MapMarkers == null || !Opts.MapMarkers.Value || Time.time < nextSync) return;
        nextSync = Time.time + 0.2f;
        try
        {
            var c = AmongUsClient.Instance;
            if (c == null || c.GameState != InnerNetClient.GameStates.Started || ShipStatus.Instance == null || PlayerControl.LocalPlayer == null)
            {
                if (all.Count > 0) { foreach (var m in all.Values) m.Pc = null; all.Clear(); }
                blockUntil = Time.time + 8f;
                return;
            }
            if (Busy())
            {
                if (all.Count > 0) { foreach (var m in all.Values.ToList()) Despawn(m); all.Clear(); }
                blockUntil = Time.time + 6f;
                return;
            }

            // выжившие шаги создания
            foreach (var m in all.Values.ToList())
            {
                if (m.Stage >= 4 || Time.time < m.At) continue;
                try
                {
                    if (m.Stage == 0) { Spawn(m); m.Stage = 1; m.At = Time.time + 0.1f; }
                    else if (m.Stage == 1) { InitForOthers(m); m.Stage = 2; m.At = Time.time + 0.3f; }
                    else if (m.Stage == 2) { HideFromOthers(m); m.Stage = 3; m.At = Time.time + 0.15f; }
                    else if (m.Stage == 3) { ApplySprite(m); m.Stage = 4; }
                }
                catch (Exception e)
                {
                    Plugin.Logger.LogWarning($"Markers stage {m.Stage}: {e.Message}");
                    Despawn(m); all.Remove(m.Key);
                    failures++;
                }
            }
            if (failures >= 3) return;

            var want = Time.time < blockUntil ? new Dictionary<string, (byte owner, Vector2 pos, string ru, string en, string color)>() : Gather();
            foreach (var m in all.Values.ToList())
                if (!want.ContainsKey(m.Key)) { Despawn(m); all.Remove(m.Key); }
            foreach (var kv in want)
            {
                if (all.ContainsKey(kv.Key) || all.Count >= MaxMarkers) continue;
                var ow = Utils.GetPlayer(kv.Value.owner);
                if (ow == null || ow.Data == null || ow.Data.IsDead || ow.Data.Disconnected) continue;
                bool ru = Translator.IsRu(ow);
                all[kv.Key] = new M
                {
                    Key = kv.Key, Owner = kv.Value.owner, Pos = kv.Value.pos,
                    Text = $"<b><color={kv.Value.color}>[ {(ru ? kv.Value.ru : kv.Value.en)} ]</color></b>",
                    Stage = 0, At = Time.time
                };
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Markers.Tick: " + e.Message); }
    }

    static int failures;

    static Dictionary<string, (byte owner, Vector2 pos, string ru, string en, string color)> Gather()
    {
        var d = new Dictionary<string, (byte, Vector2, string, string, string)>();
        void Add(List<(string key, byte owner, Vector2 pos, string ru, string en, string color)> src)
        {
            foreach (var it in src) d[it.key] = (it.owner, it.pos, it.ru, it.en, it.color);
        }
        Add(Extra33.Items()); Add(Roles30.Items()); Add(Roles75.Items()); Add(Smoker.Items()); Add(NR.Items()); Add(Freeman.Items());
        return d;
    }
}
