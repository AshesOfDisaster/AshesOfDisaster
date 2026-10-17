using HarmonyLib;
using InnerNet;

namespace AshesOfDisaster;

/// Официальный режим «host-only мод» (Innersloth, AmongUsModdingInformation): +25 к версии протокола в ОНЛАЙН-играх.
/// Сервер переводит лобби на авторитет хоста и ослабляет античит — именно так не кикает хоста на официальных серверах.
/// ВНИМАНИЕ: по умолчанию ВЫКЛ (с 0.1.41) — при включении на официальном сервере комната могла не создаваться (0 игроков, потом кик).
/// Версия берётся при подключении/создании комнаты, поэтому настройка действует со следующей комнаты.
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
            if (Opts.HostOnlyForce == null || !Opts.HostOnlyForce.Value) return;
            if (__result % 50 < 25) __result += 25;
            if (lastLogged != __result) { lastLogged = __result; Plugin.Logger.LogInfo($"HostOnly: broadcast version = {__result} (+25 flag on)"); }
        }
        catch { }
    }
}


/// вместе с +25 к версии клиент объявляет себя модифицированным.
/// Без этого сервер получал +25, но «немодифицированный» клиент — и кикал хоста (Hacking) при создании комнаты.
[HarmonyPatch(typeof(Constants), "IsVersionModded")]
public static class IsVersionModdedPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(Constants), "IsVersionModded");

    public static bool Prefix(ref bool __result)
    {
        if (Opts.HostOnlyForce == null || !Opts.HostOnlyForce.Value) return true;
        __result = true;
        return false;
    }
}
