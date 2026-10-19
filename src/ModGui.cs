using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshesOfDisaster;

/// Фоновая логика хоста: автозапуск, приветствия, тики ролей.
public class ModGui : MonoBehaviour
{
    public ModGui(IntPtr ptr) : base(ptr) { }

    static readonly List<(int id, float at, float deadline)> welcome = new();
    public static float AutoStartAt = -1f;
    static int pausedCount = -1;

    public static void QueueJoin(int clientId) => welcome.Add((clientId, Time.time + 3f, Time.time + 20f));

    public static void CancelAutoStart()
    {
        AutoStartAt = -1f;
        pausedCount = Utils.PlayerCount();
    }

    void Update()
    {
        Intro.Tick();
        Optimizer.Tick();
        Psy.Tick();
        Welder.Tick();
        PetAll.Tick();
        NR.Tick();
        ModMenu.Tick();
        Utils.PumpOutbox();
        if (Utils.AmHost && Utils.InLobby && RoleState.Assigned.Count > 0) RoleLogic.Reset();
        RoleLogic.Tick();
        NameInfo.Tick();
        if (!Utils.AmHost || !Utils.InLobby) { AutoStartAt = -1f; welcome.Clear(); return; }

        for (int i = welcome.Count - 1; i >= 0; i--)
        {
            var e = welcome[i];
            if (Time.time < e.at) continue;
            var c = AmongUsClient.Instance.GetClient(e.id);
            if (c == null) { welcome.RemoveAt(i); continue; }
            if (c.Character == null)
            {
                if (Time.time >= e.deadline) welcome.RemoveAt(i);
                continue;
            }
            welcome.RemoveAt(i);
            try
            {
                if (JoinCheck.Run(c)) continue;
                if (!Opts.Welcome.Value) continue;
                string host = PlayerControl.LocalPlayer?.Data?.PlayerName ?? "Host";
                string creators = Translator.IsRu(c.Character) ? Plugin.Creators.Replace(" & ", " и ") : Plugin.Creators;
                string msg = Translator.T("welcome", c.Character, c.PlayerName, host, Plugin.Name, creators);
                Utils.SendPrivate(c.Character, msg);
            }
            catch (Exception ex) { Plugin.Logger.LogError(ex); }
        }

        if (Opts.AutoStart.Value && GameStartManager.Instance != null)
        {
            int n = Utils.PlayerCount();
            if (pausedCount >= 0 && n != pausedCount) pausedCount = -1;
            if (pausedCount < 0 && n >= Opts.AutoStartPlayers.Value)
            {
                if (AutoStartAt < 0) AutoStartAt = Time.time + Opts.AutoStartDelay.Value;
                else if (Time.time >= AutoStartAt) { AutoStartAt = -1f; GameStartManager.Instance.BeginGame(); }
            }
            else AutoStartAt = -1f;
        }
    }
}
