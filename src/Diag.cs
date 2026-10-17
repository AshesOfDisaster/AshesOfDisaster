using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using InnerNet;

namespace AshesOfDisaster;

// Диагностика: пишет в LogOutput.log, почему игра разорвала соединение и кто кого кикает.
// Патчатся все перегрузки по имени, чтобы причина попала в лог при любой версии игры.
[HarmonyPatch]
public static class DisconnectLogPatch
{
    static readonly string[] Names = { "HandleDisconnect", "DisconnectInternal" };

    static IEnumerable<MethodBase> TargetMethods()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (var t in new[] { typeof(InnerNetClient), typeof(AmongUsClient) })
            foreach (var m in t.GetMethods(all))
                if (Names.Contains(m.Name) && !m.IsAbstract) yield return m;
    }

    [HarmonyPrepare]
    public static bool Prepare() => TargetMethods().Any();

    public static void Prefix(object[] __args, MethodBase __originalMethod)
    {
        string a = __args == null ? "" : string.Join(", ", __args.Select(x => x == null ? "null" : x.ToString()));
        Plugin.Logger.LogWarning($"Disconnect via {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}({a})");
    }
}

[HarmonyPatch(typeof(InnerNetClient), "KickPlayer")]
public static class KickLogPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(InnerNetClient), "KickPlayer");

    public static void Prefix(object[] __args)
        => Plugin.Logger.LogWarning($"KickPlayer({string.Join(", ", __args.Select(x => x?.ToString() ?? "null"))})\n{new System.Diagnostics.StackTrace(1, false)}");
}
