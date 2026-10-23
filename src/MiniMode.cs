using BepInEx.Configuration;

namespace AshesOfDisaster;

public static class MiniMode
{
    public static BoolOpt On;

    public static bool Active => On != null && On.Value;

    public static void Define(ConfigFile c)
    {
        On = new BoolOpt("MiniMode", OptTab.Mini, false, c);
        On.NameRu = "Режим мини-игр: без ролей, только мини-игры"; On.NameEn = "Mini-games mode: no roles, mini-games only";
        On.GroupRu = "Мини-игры"; On.GroupEn = "Mini-games";
    }
}
