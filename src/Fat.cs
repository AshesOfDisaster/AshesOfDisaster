using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Hazel;
using InnerNet;
using UnityEngine;

namespace AshesOfDisaster;

public static class Fat
{
    public static IntOpt StartFat, MaxShields, SpawnEvery, MaxFood, MaxSize;

    class Food { public int Kind; public Vector2 Pos; }
    class St { public int Fat, Shields; public float Scale = 1f; }

    static readonly (string ru, string en, int fat, int sh)[] Kinds =
    {
        ("Торт", "Cake", 20, 1),
        ("Бургер", "Burger", 50, 2),
        ("Пицца", "Pizza", 35, 1),
        ("Брокколи", "Broccoli", -30, -1),
        ("Яблоко", "Apple", -20, -1),
        ("Морковь", "Carrot", -10, 0),
    };

    static readonly System.Random rng = new();
    static readonly Dictionary<byte, St> st = new();
    static readonly List<Food> foods = new();
    static float nextTick, nextSpawn, nextHint, nextScale;
    static byte scaleCall = 254;

    public static void Define(ConfigFile c)
    {
        Opts.AddRole(c, "Fat", "Толстяк", "Fat", "#FF9E80", OptTab.Crewmate,
            "Мирный. С начала игры у тебя есть щит (жир), он защищает от убийства. По карте разбросана еда, и видишь её только ты: подсказки о комнатах приходят над головой, а подойдя к еде, ты её съедаешь. Торт: жир +20 и щит +1. Бургер: жир +50 и щиты +2. Пицца: жир +35 и щит +1. Овощи и фрукты забирают жир и щиты: брокколи -30 и щит -1, яблоко -20 и щит -1, морковь -10. Чем больше жира, тем крупнее ты, и это видно всем. [Питомец: показать, где еда]",
            "Crewmate. You start the game with a shield (fat) that protects you from a kill. Food is scattered around the map and only you can see it: room hints appear over your head, and you eat the food when you walk up to it. Cake: fat +20 and shield +1. Burger: fat +50 and shields +2. Pizza: fat +35 and shield +1. Vegetables and fruit take fat and shields away: broccoli -30 and shield -1, apple -20 and shield -1, carrot -10. The more fat you have the bigger you are, and everyone can see it. [Pet: show where the food is]");
        StartFat = Opts.Extra("Fat", new IntOpt("FatStart", OptTab.Crewmate, 30, 0, 100, 5, c), "Жир в начале игры", "Fat at the start");
        MaxShields = Opts.Extra("Fat", new IntOpt("FatMaxShields", OptTab.Crewmate, 6, 1, 15, 1, c), "Максимум щитов", "Max shields");
        SpawnEvery = Opts.Extra("Fat", new IntOpt("FatSpawn", OptTab.Crewmate, 20, 5, 120, 5, c), "Новая еда раз в (сек)", "New food every (sec)");
        MaxFood = Opts.Extra("Fat", new IntOpt("FatMaxFood", OptTab.Crewmate, 5, 1, 15, 1, c), "Еды на карте одновременно", "Food on the map at a time");
        MaxSize = Opts.Extra("Fat", new IntOpt("FatMaxSize", OptTab.Crewmate, 180, 100, 300, 10, c), "Максимальный размер (%)", "Max size (%)");
    }

    public static void Reset()
    {
        foreach (var kv in st.ToList()) { var p = Utils.GetPlayer(kv.Key); if (p != null) SetScale(p, 1f); NameInfo.RefreshDecor(kv.Key, true); }
        st.Clear(); foods.Clear(); nextSpawn = 0f; nextHint = 0f;
    }

    static bool Alive(PlayerControl p) => p != null && p.Data != null && !p.Data.IsDead && !p.Data.Disconnected;

    public static int NameSize(PlayerControl p)
    {
        if (p == null || !st.TryGetValue(p.PlayerId, out var s) || !Alive(p)) return 100;
        return Mathf.Clamp((int)(s.Scale * 100f), 100, 220);
    }

    static string Nm(int k, bool ru) => ru ? Kinds[k].ru : Kinds[k].en;

    static float ScaleFor(int fat) => Mathf.Clamp(1f + fat / 100f * 0.5f, 1f, MaxSize.Value / 100f);

    static void SetScale(PlayerControl p, float s)
    {
        try { p.transform.localScale = new Vector3(s, s, 1f); } catch { }
        if (p.AmOwner) return;
        try
        {
            if (scaleCall == 254)
            {
                try { scaleCall = (byte)Convert.ToInt32(Enum.Parse(typeof(RpcCalls), "SetScale")); }
                catch { scaleCall = 255; Plugin.Logger.LogInfo("Fat: this game version has no SetScale RPC, size is visible to the host only"); }
            }
            if (scaleCall == 255) return;
            var w = AmongUsClient.Instance.StartRpcImmediately(p.NetId, scaleCall, SendOption.Reliable);
            w.Write(s);
            AmongUsClient.Instance.FinishRpcImmediately(w);
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Fat.SetScale: " + e.Message); }
    }

    static void Sync(PlayerControl p, St s)
    {
        if (s.Shields > 0) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
        else RoleLogic.ShieldArmed.Remove(p.PlayerId);
        s.Scale = ScaleFor(s.Fat);
        SetScale(p, s.Scale);
        NameInfo.RefreshDecor(p.PlayerId);
    }

    public static void AfterShield(PlayerControl p)
    {
        if (p == null || !st.TryGetValue(p.PlayerId, out var s)) return;
        s.Shields = Math.Max(0, s.Shields - 1);
        if (s.Shields > 0) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
        bool ru = Translator.IsRu(p);
        RoleLogic.Hint(p, ru ? $"Жир спас тебя! Щитов осталось: {s.Shields}" : $"Your fat saved you! Shields left: {s.Shields}");
    }

    static bool RandomSpot(out Vector2 pos)
    {
        pos = default;
        var ss = ShipStatus.Instance; var doors = ss != null ? ss.AllDoors : null;
        if (doors == null || doors.Length == 0) return false;
        var d = doors[rng.Next(doors.Length)];
        if (d == null) return false;
        pos = d.transform.position;
        return true;
    }

    static string FoodList(bool ru)
    {
        if (foods.Count == 0) return ru ? "Еды на карте пока нет" : "No food on the map yet";
        return (ru ? "Еда: " : "Food: ") + string.Join(" / ", foods.Select(f => Nm(f.Kind, ru) + " (" + Utils.RoomName(f.Pos, ru) + ")"));
    }

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        st.TryGetValue(p.PlayerId, out var s);
        int fat = s != null ? s.Fat : StartFat.Value, sh = s != null ? s.Shields : 1;
        return ru ? $"{r.Colored(true)}: жир {fat}, щитов {sh}. Ешь торты и бургеры (больше жира и щитов), не ешь овощи. Еду видишь только ты, питомец покажет, где она."
                  : $"{r.Colored(false)}: fat {fat}, shields {sh}. Eat cakes and burgers (more fat and shields), avoid vegetables. Only you can see the food, the pet shows where it is.";
    }

    public static string Short(PlayerControl p, bool ru)
    {
        st.TryGetValue(p.PlayerId, out var s);
        int fat = s != null ? s.Fat : StartFat.Value, sh = s != null ? s.Shields : 1;
        return ru ? $"жир {fat}, щитов {sh}, еды на карте {foods.Count}" : $"fat {fat}, shields {sh}, food on the map {foods.Count}";
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        RoleLogic.Hint(p, FoodList(ru));
        NameInfo.Notify(p, FoodList(ru), 10f);
        return true;
    }

    static void Eat(PlayerControl p, St s, Food f)
    {
        bool ru = Translator.IsRu(p);
        var k = Kinds[f.Kind];
        s.Fat = Mathf.Clamp(s.Fat + k.fat, 0, 1000);
        s.Shields = Mathf.Clamp(s.Shields + k.sh, 0, MaxShields.Value);
        Sync(p, s);
        string msg = ru ? $"Ты съел: {k.ru}. Жир {s.Fat}, щитов {s.Shields}" : $"You ate: {k.en}. Fat {s.Fat}, shields {s.Shields}";
        RoleLogic.Hint(p, msg);
        NameInfo.Notify(p, msg, 6f);
    }

    public static void Tick()
    {
        if (!Utils.AmHost || Time.time < nextTick) return;
        nextTick = Time.time + 0.25f;
        try
        {
            var c = AmongUsClient.Instance;
            bool running = c != null && c.GameState == InnerNetClient.GameStates.Started && ShipStatus.Instance != null;
            if (!running)
            {
                if (st.Count > 0) Reset();
                return;
            }
            var fats = new List<PlayerControl>();
            foreach (var kv in RoleState.Assigned)
                if (kv.Value.Id == "Fat") { var p = Utils.GetPlayer(kv.Key); if (p != null && p.Data != null) fats.Add(p); }
            if (fats.Count == 0) { if (st.Count > 0) Reset(); return; }
            float now = Time.time;

            foreach (var p in fats)
            {
                if (!st.TryGetValue(p.PlayerId, out var s))
                {
                    s = new St { Fat = StartFat.Value, Shields = 1 };
                    st[p.PlayerId] = s;
                    Sync(p, s);
                    Plugin.Logger.LogInfo($"Fat: {p.Data.PlayerName} starts with fat {s.Fat}, 1 shield");
                }
                if (!Alive(p)) continue;
                if (s.Shields > 0 && !RoleLogic.ShieldArmed.Contains(p.PlayerId))
                {
                    s.Shields--;
                    if (s.Shields > 0) { RoleLogic.ShieldArmed.Add(p.PlayerId); RoleLogic.ShieldUsed.Remove(p.PlayerId); }
                    RoleLogic.Hint(p, Translator.IsRu(p) ? $"Жир спас тебя! Щитов осталось: {s.Shields}" : $"Your fat saved you! Shields left: {s.Shields}");
                }
            }

            if (MeetingHud.Instance != null) return;

            if (nextSpawn <= 0f) nextSpawn = now + 3f;
            if (now >= nextSpawn)
            {
                nextSpawn = now + SpawnEvery.Value;
                if (foods.Count < MaxFood.Value && RandomSpot(out var sp))
                    foods.Add(new Food { Kind = rng.Next(100) < 55 ? rng.Next(3) : 3 + rng.Next(3), Pos = sp });
            }

            foreach (var p in fats)
            {
                if (!Alive(p) || !st.TryGetValue(p.PlayerId, out var s)) continue;
                var me = p.GetTruePosition();
                var near = foods.FirstOrDefault(f => Vector2.Distance(f.Pos, me) < 1.2f);
                if (near != null) { foods.Remove(near); Eat(p, s, near); }
            }

            if (now >= nextHint)
            {
                nextHint = now + 10f;
                foreach (var p in fats) if (Alive(p)) NameInfo.Notify(p, FoodList(Translator.IsRu(p)), 9f);
            }

            if (now >= nextScale)
            {
                nextScale = now + 5f;
                foreach (var p in fats) if (st.TryGetValue(p.PlayerId, out var s)) SetScale(p, s.Scale);
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Fat.Tick: " + e.Message); }
    }
}
