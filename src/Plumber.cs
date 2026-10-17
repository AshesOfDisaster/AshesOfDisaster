using System;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// Сантехник (мирный): питомец чинит активный саботаж, а если саботажа нет — открывает все закрытые двери.
public static class Plumber
{
    static readonly System.Collections.Generic.Dictionary<byte, float> last = new();
    static readonly System.Collections.Generic.Dictionary<byte, int> uses = new();

    public static void Reset() { last.Clear(); uses.Clear(); }
    static int Left(byte id) => Math.Max(0, Opts.PlumberUses.Value - (uses.TryGetValue(id, out var u) ? u : 0));

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        return ru ? $"{r.Colored(true)}: питомец чинит активный саботаж, а без саботажа открывает закрытые двери. Осталось починок: {Left(p.PlayerId)}."
                  : $"{r.Colored(false)}: pet fixes an active sabotage, or opens closed doors if there is none. Repairs left: {Left(p.PlayerId)}.";
    }

    static bool Active(SystemTypes t)
    {
        try
        {
            var ss = ShipStatus.Instance;
            if (ss == null || ss.Systems == null || !ss.Systems.ContainsKey(t)) return false;
            var act = ss.Systems[t].TryCast<IActivatable>();
            return act != null && act.IsActive;
        }
        catch { return false; }
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        void H(string s) => RoleLogic.Hint(p, s);
        var ss = ShipStatus.Instance;
        if (ss == null) return true;
        if (Left(p.PlayerId) <= 0) { H(ru ? "Починки закончились." : "No repairs left."); return true; }
        if (last.TryGetValue(p.PlayerId, out var l) && Time.time - l < Opts.PlumberCooldown.Value)
        { H(ru ? $"Перезарядка: ещё {Mathf.CeilToInt(Opts.PlumberCooldown.Value - (Time.time - l))} с." : "Cooling down."); return true; }

        string what = null;
        try
        {
            if (Active(SystemTypes.Electrical))
            {
                var sw = ss.Systems[SystemTypes.Electrical].TryCast<SwitchSystem>();
                if (sw != null) { sw.ActualSwitches = 0; sw.ExpectedSwitches = 0; what = ru ? "свет" : "lights"; }
            }
            else if (Active(SystemTypes.Reactor)) { ss.RpcUpdateSystem(SystemTypes.Reactor, 16); ss.RpcUpdateSystem(SystemTypes.Reactor, 17); what = ru ? "реактор" : "reactor"; }
            else if (Active(SystemTypes.Laboratory)) { ss.RpcUpdateSystem(SystemTypes.Laboratory, 67); ss.RpcUpdateSystem(SystemTypes.Laboratory, 66); what = ru ? "реактор" : "reactor"; }
            else if (Active(SystemTypes.HeliSabotage)) { ss.RpcUpdateSystem(SystemTypes.HeliSabotage, 16); ss.RpcUpdateSystem(SystemTypes.HeliSabotage, 17); what = ru ? "двигатели" : "engines"; }
            else if (Active(SystemTypes.LifeSupp)) { ss.RpcUpdateSystem(SystemTypes.LifeSupp, 67); ss.RpcUpdateSystem(SystemTypes.LifeSupp, 66); what = ru ? "кислород" : "oxygen"; }
            else if (Active(SystemTypes.Comms)) { ss.RpcUpdateSystem(SystemTypes.Comms, 16); ss.RpcUpdateSystem(SystemTypes.Comms, 17); what = ru ? "связь" : "comms"; }
            else
            {
                var doors = ss.AllDoors;
                int opened = 0;
                if (doors != null)
                {
                    ISystemType sys = ss.Systems.ContainsKey(SystemTypes.Doors) ? ss.Systems[SystemTypes.Doors] : null;
                    var auto = sys != null ? sys.TryCast<AutoDoorsSystemType>() : null;
                    for (int i = 0; i < doors.Length; i++)
                    {
                        var d = doors[i];
                        if (d == null || d.IsOpen) continue;
                        Welder.Unlock(d.Room);
                        if (auto != null) { d.SetDoorway(true); auto.dirtyBits |= (uint)(1 << i); }
                        else ss.RpcUpdateSystem(SystemTypes.Doors, (byte)(64 | i));
                        opened++;
                    }
                }
                if (opened > 0) what = ru ? $"двери ({opened})" : $"doors ({opened})";
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Plumber: " + e.Message); }

        if (what == null) { H(ru ? "Чинить нечего: саботажа и закрытых дверей нет." : "Nothing to fix: no sabotage and no closed doors."); return true; }
        last[p.PlayerId] = Time.time; uses[p.PlayerId] = (uses.TryGetValue(p.PlayerId, out var u) ? u : 0) + 1;
        H(ru ? $"Починено: {what}. Осталось починок: {Left(p.PlayerId)}." : $"Fixed: {what}. Repairs left: {Left(p.PlayerId)}.");
        Plugin.Logger.LogInfo($"Plumber {p.Data.PlayerName}: fixed {what}");
        return true;
    }
}
