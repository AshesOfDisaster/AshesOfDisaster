using HarmonyLib;
using InnerNet;

namespace AshesOfDisaster;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
public static class HostOnlyVersionPatch
{
    static int lastLogged = -1;

    public static void Postfix(ref int __result)
    {
        try
        {
            var c = AmongUsClient.Instance;
            if (c == null || c.NetworkMode != NetworkModes.OnlineGame) return;
            if (__result % 50 < 25) __result += 25;
            if (lastLogged != __result) { lastLogged = __result; Plugin.Logger.LogInfo($"HostOnly: broadcast version = {__result} (+25 flag on)"); }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(Constants), "IsVersionModded")]
public static class IsVersionModdedPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(Constants), "IsVersionModded");

    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
