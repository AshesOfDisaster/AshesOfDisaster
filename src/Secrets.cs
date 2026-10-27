using System;
using System.IO;
using BepInEx;

namespace AshesOfDisaster;

public static class Secrets
{
    static string reports, roomCodes;
    static bool loaded;

    static void Load()
    {
        if (loaded) return;
        loaded = true; reports = ""; roomCodes = "";
        try
        {
            string path = Path.Combine(Paths.ConfigPath, "AshesOfDisaster.secrets.txt");
            if (!File.Exists(path)) return;
            foreach (var line in File.ReadAllLines(path))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq).Trim().ToLowerInvariant(), v = line.Substring(eq + 1).Trim();
                if (k == "reports") reports = v;
                else if (k == "roomcodes") roomCodes = v;
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Secrets: " + e.Message); }
    }

    public static string Reports { get { Load(); return reports; } }
    public static string RoomCodes { get { Load(); return roomCodes; } }
}
