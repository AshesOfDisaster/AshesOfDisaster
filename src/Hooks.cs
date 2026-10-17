using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AshesOfDisaster;

/// Устойчивая установка патчей. Раньше один неудачный патч (метод, которого нет в этой версии игры)
/// обрывал PatchAll: остальные патчи не ставились, и ломались команды, меню и способности ролей.
/// Теперь каждый класс патчится отдельно, а причина сбоя пишется в LogOutput.log.
public static class ModHooks
{
    public static bool Exists(Type t, string method)
    {
        try { return AccessTools.Method(t, method) != null; }
        catch { return false; }
    }

    public static void PatchAllSafe(Harmony h)
    {
        Type[] types;
        try { types = typeof(Plugin).Assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(x => x != null).ToArray(); }

        int ok = 0, bad = 0;
        foreach (var t in types)
        {
            try
            {
                if (t.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
                h.PatchAll(t);
                ok++;
            }
            catch (Exception e)
            {
                bad++;
                Plugin.Logger.LogError($"Patch {t.Name} FAILED: {e.GetBaseException().Message}");
            }
        }
        Plugin.Logger.LogInfo($"Harmony: patch classes ok={ok}, failed={bad}");
    }
}

/// Питомец = кнопка способности. В разных версиях игры погладивание приходит разными путями,
/// поэтому хуки ставятся по именам методов во время работы: чего нет в игре — пропускается (и пишется в лог),
/// а сборка от этого не зависит.
public static class PetHooks
{
    static byte petCall = 255;

    public static void Install(Harmony h)
    {
        try { petCall = (byte)Convert.ToInt32(Enum.Parse(typeof(RpcCalls), "Pet")); }
        catch { Plugin.Logger.LogWarning("RpcCalls.Pet not found in this game version: pets of other players will not be detected"); }

        // Диагностика: какие методы про питомцев есть в этой версии игры
        try
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var t in new[] { typeof(PlayerControl), typeof(PlayerPhysics) })
                foreach (var m in t.GetMethods(all))
                    if (m.Name.IndexOf("Pet", StringComparison.OrdinalIgnoreCase) >= 0)
                        Plugin.Logger.LogInfo($"Pet-related method: {t.Name}.{m.Name}");
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Pet scan: " + e.Message); }

        Hook(h, typeof(PlayerControl), "TryPet", nameof(LocalPet));          // свой питомец (хост)
        Hook(h, typeof(PlayerControl), "HandleRpc", nameof(RpcControl));     // питомец другого игрока приходит как RPC
        Hook(h, typeof(PlayerPhysics), "HandleRpc", nameof(RpcPhysics));     // запасной путь
        Hook(h, typeof(PlayerPhysics), "PetPet", nameof(PhysicsPet));        // если такие методы есть в этой версии
        Hook(h, typeof(PlayerPhysics), "CoPet", nameof(PhysicsPet));
    }

    static void Hook(Harmony h, Type t, string method, string postfix)
    {
        try
        {
            var orig = AccessTools.Method(t, method);
            if (orig == null) { Plugin.Logger.LogInfo($"Pet hook {t.Name}.{method}: not present in this game version"); return; }
            h.Patch(orig, postfix: new HarmonyMethod(typeof(PetHooks).GetMethod(postfix)));
            Plugin.Logger.LogInfo($"Pet hook {t.Name}.{method}: installed");
        }
        catch (Exception e) { Plugin.Logger.LogError($"Pet hook {t.Name}.{method} FAILED: {e.GetBaseException().Message}"); }
    }

    public static void LocalPet(PlayerControl __instance)
    {
        try { if (__instance != null && __instance.AmOwner) RoleLogic.OnPet(__instance, "TryPet"); }
        catch (Exception e) { Plugin.Logger.LogError("Pet: " + e); }
    }

    public static void RpcControl(PlayerControl __instance, [HarmonyArgument(0)] byte callId)
    {
        if (callId != petCall) return;
        try { RoleLogic.OnPet(__instance, "Rpc"); }
        catch (Exception e) { Plugin.Logger.LogError("Pet: " + e); }
    }

    public static void RpcPhysics(PlayerPhysics __instance, [HarmonyArgument(0)] byte callId)
    {
        if (callId != petCall) return;
        try { RoleLogic.OnPet(__instance.myPlayer, "PhysRpc"); }
        catch (Exception e) { Plugin.Logger.LogError("Pet: " + e); }
    }

    public static void PhysicsPet(PlayerPhysics __instance)
    {
        try { RoleLogic.OnPet(__instance.myPlayer, "PhysPet"); }
        catch (Exception e) { Plugin.Logger.LogError("Pet: " + e); }
    }
}
