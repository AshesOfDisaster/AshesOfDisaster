using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace AshesOfDisaster;

[BepInPlugin(Id, Name, Version)]
[BepInProcess("Among Us.exe")]
public class Plugin : BasePlugin
{
    public const string Id = "com.niko.ashesofdisaster";
    public const string Name = "Ashes of Disaster";
    public const string Version = "2.0";
    public const string Creators = "niko & Акакиш";
    public const string Discord = "discord.gg/CCB2rg3CHz";

    public static Plugin Instance;
    public static BepInEx.Logging.ManualLogSource Logger;

    public override void Load()
    {
        Instance = this;
        Logger = Log;
        Opts.Init(Config);
        Lists.Init();

        ClassInjector.RegisterTypeInIl2Cpp<ModGui>();
        AddComponent<ModGui>();

        var harmony = new Harmony(Id);
        ModHooks.PatchAllSafe(harmony);
        PetHooks.Install(harmony);
        NameInfo.Install(harmony);
        Log.LogInfo($"{Name} {Version} loaded");
    }
}
