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
    public const string Version = "4.0.0";
    public const string Creators = "niko";
    public const string Discord = "discord.gg/rzrph6v2rV";

    public static Plugin Instance;
    public static BepInEx.Logging.ManualLogSource Logger;

    public override void Load()
    {
        Instance = this;
        Logger = Log;
        SelfTest.Install();
        Opts.Init(Config);
        Music.Init(Config);
        Lists.Init();

        ClassInjector.RegisterTypeInIl2Cpp<ModGui>();
        AddComponent<ModGui>();

        var harmony = new Harmony(Id);
        ModHooks.PatchAllSafe(harmony);
        PetHooks.Install(harmony);
        NameInfo.Install(harmony);
        SelfTest.RunStatic();
        Log.LogInfo($"{Name} {Version} loaded");
    }
}
