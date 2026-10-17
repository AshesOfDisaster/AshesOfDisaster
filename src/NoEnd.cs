using HarmonyLib;

namespace AshesOfDisaster;

/// «Игра не заканчивается»: отключаем автоматические проверки конца игры (убийства, задания, голосование, саботаж).
/// Закончить вручную можно командой /end.
/// Методы указаны строками и проверяются при запуске: если такого метода нет в этой версии игры,
/// патч просто пропускается (а не ломает сборку и остальные патчи).
public static class NoEnd
{
    public static bool Active
    {
        get
        {
            if (!Utils.AmHost) return false;
            if (Opts.NoGameEnd.Value) return true;
            return Opts.NoEndFewPlayers.Value && Utils.PlayerCount() <= 3;
        }
    }
}

[HarmonyPatch(typeof(LogicGameFlowNormal), "CheckEndCriteria")]
public static class NoEndCriteriaPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(LogicGameFlowNormal), "CheckEndCriteria");
    public static bool Prefix() => !NoEnd.Active;
}

[HarmonyPatch(typeof(LogicGameFlowNormal), "EndGameForSabotage")]
public static class NoEndSabotagePatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(LogicGameFlowNormal), "EndGameForSabotage");
    public static bool Prefix() => !NoEnd.Active;
}

[HarmonyPatch(typeof(GameManager), "CheckEndGameViaTasks")]
public static class NoEndTasksPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(GameManager), "CheckEndGameViaTasks");
    public static bool Prefix(ref bool __result)
    {
        if (!NoEnd.Active) return true;
        __result = false;
        return false;
    }
}

// В части версий игры проверка заданий называется иначе
[HarmonyPatch(typeof(GameManager), "CheckTaskCompletion")]
public static class NoEndTaskCompletionPatch
{
    [HarmonyPrepare]
    public static bool Prepare() => ModHooks.Exists(typeof(GameManager), "CheckTaskCompletion");
    public static bool Prefix(ref bool __result)
    {
        if (!NoEnd.Active) return true;
        __result = false;
        return false;
    }
}
