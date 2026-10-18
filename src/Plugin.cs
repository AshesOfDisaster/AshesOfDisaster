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
    public const string Version = "0.1.93";
    public const string Creators = "niko & Акакиш";

    public static Plugin Instance;
    public static BepInEx.Logging.ManualLogSource Logger;

    public override void Load()
    {
        Instance = this;
        Logger = Log;
        Opts.Init(Config);
        Lists.Init();

        // Сначала окно мода и логика (от патчей не зависят), потом патчи — по одному, ошибки уходят в лог
        ClassInjector.RegisterTypeInIl2Cpp<ModGui>();
        AddComponent<ModGui>();

        var harmony = new Harmony(Id);
        ModHooks.PatchAllSafe(harmony);
        PetHooks.Install(harmony);
        NameInfo.Install(harmony);      // только ставит перехват; отправка идёт лишь при включённой настройке «Статус над ником»
        Log.LogInfo($"{Name} {Version} loaded");
    }
}
