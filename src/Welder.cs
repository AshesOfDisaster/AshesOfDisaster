using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

public static class Welder
{
    static readonly Dictionary<SystemTypes, byte> locked = new();
    static readonly Dictionary<byte, float> lastUse = new();
    static float nextTick;

    public static void Reset() { locked.Clear(); lastUse.Clear(); }
    public static void OnMeeting() { locked.Clear(); }
    public static void Unlock(SystemTypes room) { locked.Remove(room); }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        int n = locked.Count(kv => kv.Value == p.PlayerId);
        return ru ? $"{r.Colored(true)}: питомец у двери запирает комнату до собрания. Заперто {n}/{Opts.WelderMax.Value}, столовую запереть нельзя."
                  : $"{r.Colored(false)}: pet next to a door locks the room until the meeting. Locked {n}/{Opts.WelderMax.Value}, the cafeteria can't be locked.";
    }

    public static void Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        void H(string s) => RoleLogic.Hint(p, s);
        var ss = ShipStatus.Instance;
        var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) { H(ru ? "На этой карте нет дверей." : "There are no doors on this map."); return; }
        if (lastUse.TryGetValue(p.PlayerId, out var l) && Time.time - l < Opts.WelderCooldown.Value)
        { H(ru ? $"Сварка перезаряжается: ещё {Mathf.CeilToInt(Opts.WelderCooldown.Value - (Time.time - l))} с." : "Welding is cooling down."); return; }

        Vector2 pos = p.GetTruePosition();
        OpenableDoor best = null; float bd = 999f;
        foreach (var d in doors)
        {
            if (d == null) continue;
            float dd = Vector2.Distance(pos, (Vector2)d.transform.position);
            if (dd < bd) { bd = dd; best = d; }
        }
        if (best == null || bd > 2.5f) { H(ru ? "Рядом нет двери." : "No door nearby."); return; }
        if (best.Room == SystemTypes.Cafeteria) { H(ru ? "Столовую запереть нельзя." : "The cafeteria can't be locked."); return; }
        if (locked.ContainsKey(best.Room)) { H(ru ? "Эта комната уже заперта." : "This room is already locked."); return; }
        if (locked.Count(kv => kv.Value == p.PlayerId) >= Opts.WelderMax.Value)
        { H(ru ? $"Лимит запертых комнат: {Opts.WelderMax.Value}." : $"Locked rooms limit: {Opts.WelderMax.Value}."); return; }

        locked[best.Room] = p.PlayerId;
        lastUse[p.PlayerId] = Time.time;
        Plugin.Logger.LogInfo($"Welder {p.Data.PlayerName}: locking room {best.Room}");
        try { ss.RpcCloseDoorsOfType(best.Room); } catch (Exception e) { Plugin.Logger.LogWarning("Welder close: " + e.Message); }
        H(ru ? "Комната заперта до собрания." : "Room locked until the meeting.");
    }

    public static void Tick()
    {
        if (!Utils.AmHost || locked.Count == 0 || Time.time < nextTick) return;
        nextTick = Time.time + 4f;
        if (MeetingHud.Instance != null || ShipStatus.Instance == null) { locked.Clear(); return; }
        foreach (var room in locked.Keys.ToList())
            try { ShipStatus.Instance.RpcCloseDoorsOfType(room); } catch (Exception e) { Plugin.Logger.LogWarning("Welder tick: " + e.Message); }
    }
}
