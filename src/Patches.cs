using System;
using HarmonyLib;
using Hazel;
using InnerNet;

namespace AshesOfDisaster;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShowerPatch
{
    public static void Postfix(VersionShower __instance)
    {
        if (!Opts.ShowCredits.Value) return;
        __instance.text.text += $"\n<color=#ff5555>{Plugin.Name}</color> v{Plugin.Version}\n<size=70%>by {Plugin.Creators}</size>";
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class SendChatPatch
{
    public static bool Prefix(ChatController __instance)
    {
        if (!Utils.AmHost) return true;
        string text = __instance.freeChatField.textArea.text;
        if (!text.StartsWith("/")) return true;
        __instance.freeChatField.textArea.Clear();
        return !Commands.Handle(PlayerControl.LocalPlayer, text);
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class AddChatPatch
{
    public static bool Prefix([HarmonyArgument(0)] PlayerControl sourcePlayer, [HarmonyArgument(1)] string chatText)
    {
        try
        {
            if (!Utils.AmHost || sourcePlayer == null || sourcePlayer.AmOwner) return true;
            if (string.IsNullOrEmpty(chatText) || !chatText.StartsWith("/")) return true;
            Plugin.Logger.LogInfo($"Command from {sourcePlayer.Data?.PlayerName}: {chatText} (hostDead={PlayerControl.LocalPlayer?.Data?.IsDead}, senderDead={sourcePlayer.Data?.IsDead})");
            return !Commands.Handle(sourcePlayer, chatText);
        }
        catch (Exception e) { Plugin.Logger.LogError("AddChat: " + e); return true; }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
public static class OnPlayerJoinedPatch
{
    public static void Postfix([HarmonyArgument(0)] ClientData client)
    {
        if (!Utils.AmHost || client == null || client.Id == AmongUsClient.Instance.ClientId) return;
        ModGui.QueueJoin(client.Id);
    }
}

public static class JoinCheck
{
    public static bool Run(ClientData c)
    {
        string name = c.PlayerName ?? "";
        string fc = c.FriendCode ?? "";
        string reason = null;
        if (Opts.UseBanList.Value && Lists.IsBanned(fc)) reason = "kick.ban";
        else if (Opts.UseDenyNames.Value && Lists.IsDeniedName(name)) reason = "kick.name";
        else if (Opts.KickLowLevel.Value && c.PlayerLevel + 1 < (uint)Opts.MinLevel.Value) reason = "kick.level";
        else if (Opts.KickNoFriendCode.Value && string.IsNullOrEmpty(fc)) reason = "kick.fc";
        if (reason == null) return false;
        Plugin.Logger.LogInfo(Translator.T(reason, null, name));
        AmongUsClient.Instance.KickPlayer(c.Id, false);
        return true;
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerPatch
{
    public static void Prefix(GameStartManager __instance)
    {
        if (Utils.AmHost && Opts.AnyPlayers.Value) __instance.MinPlayers = 1;
    }
}
