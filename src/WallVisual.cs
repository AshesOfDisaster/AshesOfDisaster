using System;
using System.Collections.Generic;
using System.Linq;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

/// ЭКСПЕРИМЕНТ (настройки «Куклы-подписи», «Кукла-метка на грозе»; по умолчанию выключено).
/// «Кукла» — настоящий дополнительный игрок-двойник, созданный ТАК ЖЕ, как в моде PetControl пользователя:
/// Instantiate(PlayerPrefab) → свободный PlayerId → GameData.AddDummy → AmongUsClient.Spawn → isDummy,
/// затем имя/цвет/позиция обычными RPC куклы. Никаких сырых сообщений и PlayerId 254 (так раньше кикало «Hacking»).
/// Ошибка на любом шаге отключает куклы до конца сессии. Все шаги пишутся в LogOutput.log строками "WallVisual: ...".
public class WallVisualObj
{
    public PlayerControl Pc;
    public Vector2 Pos;
    public string Text;
    public int Stage;          // 0 — ждёт спавна, 1 — создан (ждём, пока клиенты его создадут), 2 — настроен
    public float At;
    public bool Dead;
    public float Until;        // когда убрать куклу
    public PlayerControl Follow;   // кукла-подпись ходит за этим игроком
    public float NextMove;
    public byte Color = 5;
    public float LastLabel;
    public int Setups;         // сколько раз уже разослали внешность/позицию (повторяем, чтобы дошло до всех клиентов)
    public float NextSetup;
    public float NextBeat;     // периодический повтор позиции (если клиент создал куклу позже)
}

public static class WallVisual
{
    static readonly List<WallVisualObj> all = new();
    static bool broken;

    /// Кукла — не игрок: её нельзя убивать грозой, ловить в паутину, считать в списках и т.д.
    public static bool IsPuppet(PlayerControl p)
    {
        if (p == null) return false;
        try { if (p.isDummy) return true; } catch { }
        foreach (var o in all) if (o.Pc == p) return true;
        return false;
    }

    /// ОТКЛЮЧЕНО на официальных серверах (fix35): там куклы не доходят до клиентов (у других «???», фортегрин) и хост получает Hacking.
    /// На своём сервере (Impostor без античита) куклы можно включать настройками.
    public static bool HardOff => Utils.AnticheatLikely;
    public static bool Enabled => !HardOff && !broken && Utils.AmHost && Opts.CloudStormMarker.Value;

    public static WallVisualObj Spawn(Vector2 pos, string text, float life = 10f, PlayerControl follow = null)
    {
        if (!Enabled) return null;
        var o = new WallVisualObj { Pos = pos, Text = string.IsNullOrEmpty(text) ? "Door" : text, Stage = 0, At = Time.time, Until = Time.time + life, Follow = follow };
        all.Add(o);
        return o;
    }

    public static void Despawn(WallVisualObj o)
    {
        if (o == null) return;
        o.Dead = true;
        try
        {
            if (o.Pc != null)
            {
                byte id = o.Pc.PlayerId;
                GameData.Instance.RemovePlayer(id);
                AmongUsClient.Instance.Despawn(o.Pc);
                Plugin.Logger.LogInfo($"WallVisual: despawned puppet {id}");
            }
        }
        catch (Exception e) { Plugin.Logger.LogError("WallVisual.Despawn: " + e.GetBaseException().Message); }
        o.Pc = null;
        all.Remove(o);
    }

    public static void DespawnAll()
    {
        foreach (var o in all.ToList()) Despawn(o);
        all.Clear();
    }

    /// Раз в кадр из RoleLogic.Tick (только хост, игра идёт)
    public static void Tick()
    {
        if (all.Count == 0) return;
        if (broken || !Utils.AmHost) { all.Clear(); return; }
        var c = AmongUsClient.Instance;
        if (c == null || c.GameState != InnerNetClient.GameStates.Started || MeetingHud.Instance != null) { DespawnAll(); return; }
        foreach (var o in all.ToList())
        {
            if (o.Dead) continue;
            if (Time.time >= o.Until) { Despawn(o); continue; }
            if (Time.time < o.At) continue;
            try
            {
                switch (o.Stage)
                {
                    case 0: DoSpawn(o); o.Stage = 1; o.At = Time.time + 1.0f; break;      // клиентам нужно время создать куклу у себя
                    case 1: DoSetup(o); o.Stage = 2; o.Setups = 1; o.NextSetup = Time.time + 1.5f; break;
                    case 2:
                        // повторная рассылка данных/внешности/позиции: у клиентов с большим пингом первая могла прийти раньше создания куклы
                        if (o.Setups < 4 && Time.time >= o.NextSetup)
                        {
                            o.Setups++; o.NextSetup = Time.time + (o.Setups == 2 ? 2.0f : 3.0f);
                            DoSetup(o);
                        }
                        DoFollow(o);
                        break;
                }
            }
            catch (Exception e)
            {
                broken = true;
                Plugin.Logger.LogError("WallVisual failed, puppets are OFF for this session: " + e);
                DespawnAll();
                return;
            }
        }
    }

    static void DoSpawn(WallVisualObj o)
    {
        var cl = AmongUsClient.Instance;
        Plugin.Logger.LogInfo($"WallVisual: spawn puppet at ({o.Pos.x:F1},{o.Pos.y:F1}) text={o.Text}");
        int freeId = GameData.Instance.GetAvailableId();
        if (freeId < 0) { Plugin.Logger.LogWarning("WallVisual: no free player id for a puppet"); o.Dead = true; all.Remove(o); return; }
        var d = UnityEngine.Object.Instantiate(cl.PlayerPrefab);
        d.PlayerId = (byte)freeId;
        var info = GameData.Instance.AddDummy(d);
        try { info.PlayerName = "Puppet"; } catch { }
        try { d.SetName("Puppet"); } catch { }      // локальный SetName до Spawn — как в PetControl      // с пустым именем клиенты не могут загрузить игрока
        cl.Spawn(d, -2, SpawnFlags.None);
        d.isDummy = true;
        o.Pc = d;
        // как в PetControl: позиция сразу после Spawn (и локально, и по сети), остальное — позже
        try { d.transform.position = new Vector3(o.Pos.x, o.Pos.y, o.Pos.y / 1000f); } catch { }
        try { d.NetTransform.RpcSnapTo(o.Pos); } catch (Exception e) { Plugin.Logger.LogWarning("WallVisual: early snap: " + e.Message); }
        Plugin.Logger.LogInfo($"WallVisual: puppet spawned id={d.PlayerId}");
    }

    static void DoSetup(WallVisualObj o)
    {
        var d = o.Pc;
        if (d == null) return;
        // имя и цвет живут в данных игрока (GameData): пишем их туда и рассылаем клиентам (как FinishSetup в PetControl),
        // иначе у других игроков кукла остаётся пустой/невидимой
        try
        {
            var data = d.Data;
            data.PlayerName = o.Text;
            var oc = data.DefaultOutfit;
            oc.PlayerName = o.Text;
            oc.ColorId = o.Color;
            data.SetDirtyBit(uint.MaxValue);
            data.MarkDirty();
            GameData.Instance.DirtyAllData();
        }
        catch (Exception e) { Plugin.Logger.LogWarning("WallVisual: data sync: " + e.Message); }
        d.RpcSetName(o.Text);
        d.RpcSetColor(o.Color);
        d.RpcSetHat("hat_NoHat");
        d.RpcSetSkin("skin_None");
        d.RpcSetVisor("visor_EmptyVisor");
        d.NetTransform.RpcSnapTo(o.Pos);
        try { if (!d.Visible) d.Visible = true; } catch { }      // у хоста несозданный игрок по умолчанию скрыт (в PetControl так же)
        if (o.Setups <= 1 || o.Setups == 4) Plugin.Logger.LogInfo($"WallVisual: puppet set up #{o.Setups} id={d.PlayerId} visible={d.Visible} name='{d.Data.PlayerName}' color={d.Data.DefaultOutfit.ColorId} netId={d.NetId} dataNetId={d.Data.NetId} owner={d.OwnerId} pos=({o.Pos.x:F1},{o.Pos.y:F1})");
    }

    /// Сменить подпись куклы (не чаще раза в 3 секунды)
    public static void Relabel(WallVisualObj o, string text)
    {
        if (o == null || o.Dead || o.Pc == null || o.Stage < 2 || o.Text == text || Time.time - o.LastLabel < 3f) return;
        try
        {
            o.Text = text; o.LastLabel = Time.time;
            o.Pc.Data.PlayerName = text;
            o.Pc.Data.DefaultOutfit.PlayerName = text;
            o.Pc.Data.SetDirtyBit(uint.MaxValue);
            o.Pc.Data.MarkDirty();
            o.Pc.RpcSetName(text);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("WallVisual.Relabel: " + e.Message); }
    }

    static void DoFollow(WallVisualObj o)
    {
        if (o.Pc == null) return;
        // пульс позиции раз в 4 с: неподвижная кукла (метка грозы) иначе остаётся у клиента в точке (0,0)
        if (Time.time >= o.NextBeat && o.Setups >= 4)
        {
            o.NextBeat = Time.time + 4f;
            try { o.Pc.NetTransform.RpcSnapTo(o.Pos); } catch { }
        }
        if (o.Follow == null) return;
        if (o.Follow.Data == null || o.Follow.Data.IsDead || o.Follow.Data.Disconnected) { Despawn(o); return; }
        try { if (!o.Pc.Visible) o.Pc.Visible = true; } catch { }
        if (Time.time < o.NextMove) return;
        o.NextMove = Time.time + 0.5f;
        var np = o.Follow.GetTruePosition() + new Vector2(0f, 1.0f);
        if (Vector2.Distance(np, o.Pos) < 0.25f) return;      // не спамим сеть, пока игрок стоит
        o.Pos = np;
        o.Pc.NetTransform.RpcSnapTo(o.Pos);
    }
}
