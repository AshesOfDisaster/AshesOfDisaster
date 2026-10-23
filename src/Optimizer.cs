using System;
using UnityEngine;

namespace AshesOfDisaster;

public static class Optimizer
{
    static float nextApply;
    static bool wasLobby;

    public static void Tick()
    {
        try
        {
            if (!Opts.OptEnable.Value) return;
            if (Time.unscaledTime < nextApply) return;
            nextApply = Time.unscaledTime + 5f;

            int fps = Opts.OptFps.Value;
            if (Application.targetFrameRate != (fps <= 0 ? -1 : fps)) Application.targetFrameRate = fps <= 0 ? -1 : fps;
            if (Opts.OptNoVsync.Value && QualitySettings.vSyncCount != 0) QualitySettings.vSyncCount = 0;
            if (Opts.OptBackground.Value && !Application.runInBackground) Application.runInBackground = true;

            bool lobby = Utils.InLobby;
            if (lobby && !wasLobby && Opts.OptCleanup.Value)
            {
                Resources.UnloadUnusedAssets();
                Plugin.Logger.LogInfo("Optimizer: unused assets unloaded");
            }
            wasLobby = lobby;
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Optimizer: " + e.Message); }
    }
}
