using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshesOfDisaster;

// Пинкман (нейтрал, по мотивам «Во все тяжкие»): питомец вдали от людей — варить партию, рядом с игроком — продать ему товар.
// Чистый товар даёт покупателю щит (отменяет первое нападение), брак — мигание экрана. Каждые 2 удачные продажи — щит самому Пинкману.
public static class Pinkman
{
    static readonly System.Random rng = new();
    static readonly Dictionary<byte, int> batch = new();
    static readonly Dictionary<byte, float> cooking = new();      // когда будет готова партия
    static readonly Dictionary<byte, int> goodSales = new();
    static readonly Dictionary<byte, int> sales = new();
    static readonly Dictionary<byte, float> lastSale = new();
    static float nextTick;

    public static void Reset() { batch.Clear(); cooking.Clear(); goodSales.Clear(); sales.Clear(); lastSale.Clear(); }
    static int B(byte id) => batch.TryGetValue(id, out var b) ? b : 0;

    public static string Status(PlayerControl p, RoleDef r)
    {
        bool ru = Translator.IsRu(p);
        byte id = p.PlayerId;
        string cook = cooking.ContainsKey(id) ? (ru ? $", варится ({Mathf.CeilToInt(cooking[id] - Time.time)} с)" : $", cooking ({Mathf.CeilToInt(cooking[id] - Time.time)} s)") : "";
        return ru ? $"{r.Colored(true)}: питомец вдали от людей — варить партию, рядом с игроком — продать товар. Партий в запасе: {B(id)}{cook}. Продаж: {(sales.TryGetValue(id, out var s) ? s : 0)}. Yeah, science!"
                  : $"{r.Colored(false)}: pet away from people — cook a batch, next to a player — sell. Batches: {B(id)}{cook}. Sales: {(sales.TryGetValue(id, out var s2) ? s2 : 0)}. Yeah, science!";
    }

    public static bool Use(PlayerControl p)
    {
        bool ru = Translator.IsRu(p);
        void H(string s) => RoleLogic.Hint(p, s);
        byte id = p.PlayerId;
        var pos = p.GetTruePosition();
        PlayerControl buyer = null; float bd = 3f;
        foreach (var o in PlayerControl.AllPlayerControls)
        {
            if (o == null || o.Data == null || o.Data.IsDead || o.Data.Disconnected || o.PlayerId == id) continue;
            float d = Vector2.Distance(pos, o.GetTruePosition());
            if (d < bd) { bd = d; buyer = o; }
        }
        if (buyer == null)
        {
            if (cooking.ContainsKey(id)) { H(ru ? $"Партия ещё варится: {Mathf.CeilToInt(cooking[id] - Time.time)} с." : "Still cooking."); return true; }
            if (B(id) >= Opts.PinkMax.Value) { H(ru ? "Склад полон — продай товар." : "Stock is full — sell something."); return true; }
            cooking[id] = Time.time + Opts.PinkCook.Value;
            H(ru ? $"Ты варишь партию: {Opts.PinkCook.Value} с. Yeah, science!" : $"Cooking a batch: {Opts.PinkCook.Value} s. Yeah, science!");
            return true;
        }
        if (B(id) <= 0) { H(ru ? "Товара нет. Отойди от людей и свари партию." : "No product. Step away from people and cook a batch."); return true; }
        if (lastSale.TryGetValue(id, out var l) && Time.time - l < Opts.PinkSaleCooldown.Value)
        { H(ru ? $"Не так быстро: ещё {Mathf.CeilToInt(Opts.PinkSaleCooldown.Value - (Time.time - l))} с." : "Not so fast."); return true; }
        lastSale[id] = Time.time; batch[id] = B(id) - 1;
        sales[id] = (sales.TryGetValue(id, out var sc) ? sc : 0) + 1;
        bool pure = rng.Next(100) < Opts.PinkPurity.Value;
        bool bru = Translator.IsRu(buyer);
        if (pure)
        {
            if (!RoleLogic.ShieldArmed.Contains(buyer.PlayerId)) { RoleLogic.ShieldArmed.Add(buyer.PlayerId); RoleLogic.ShieldUsed.Remove(buyer.PlayerId); }
            RoleLogic.Hint(buyer, bru ? "Чистейший товар! Ближайшее нападение на тебя будет отменено." : "Pure product! The next attack on you will be cancelled.");
            H(ru ? $"Сделка: {buyer.Data.PlayerName} в восторге. Чистота 99,1%!" : $"Deal: {buyer.Data.PlayerName} is delighted.");
            int g = (goodSales.TryGetValue(id, out var gs) ? gs : 0) + 1; goodSales[id] = g;
            if (g % 2 == 0 && !RoleLogic.ShieldArmed.Contains(id))
            { RoleLogic.ShieldArmed.Add(id); RoleLogic.ShieldUsed.Remove(id); H(ru ? "Выручка пошла в дело: у тебя щит." : "Profits went into protection: you got a shield."); }
        }
        else
        {
            RoleLogic.FlashPlayer(buyer, 3);
            RoleLogic.Hint(buyer, bru ? "Брак! Тебя накололи." : "Bad batch! You got ripped off.");
            H(ru ? $"Брак: {buyer.Data.PlayerName} недоволен." : $"Bad batch: {buyer.Data.PlayerName} is unhappy.");
        }
        Plugin.Logger.LogInfo($"Pinkman {p.Data.PlayerName}: sold to {buyer.Data.PlayerName} pure={pure}");
        return true;
    }

    public static void Tick()
    {
        if (!Utils.AmHost || cooking.Count == 0 || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        foreach (var kv in cooking.ToList())
        {
            if (Time.time < kv.Value) continue;
            cooking.Remove(kv.Key);
            var p = Utils.GetPlayer(kv.Key);
            if (p == null || p.Data == null || p.Data.IsDead) continue;
            batch[kv.Key] = Math.Min(Opts.PinkMax.Value, B(kv.Key) + 1);
            RoleLogic.Hint(p, Translator.IsRu(p) ? $"Партия готова! В запасе: {B(kv.Key)}. Yeah, science!" : $"Batch ready! In stock: {B(kv.Key)}. Yeah, science!");
        }
    }
}
