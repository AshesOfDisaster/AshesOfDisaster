using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AshesOfDisaster;

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
public static class SettingMenuStartPatch
{
    public static void Postfix(GameSettingMenu __instance)
    {
        try { ModMenu.Build(__instance); }
        catch (Exception e) { Plugin.Logger.LogError("ModMenu.Build: " + e); }
    }
}

[HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
public static class SettingMenuChangeTabPatch
{
    public static void Postfix() => ModMenu.HideAll();
}

public static class ModMenu
{
    class Row { public Opt Opt; public TextMeshPro Title, Value; public SpriteRenderer Check; public GameObject Go; public float Y; public bool Short; }
    class Page { public string Ru, En; public Func<Opt, bool> Filter; }

    static readonly Page[] Pages =
    {
        new() { Ru = "Система",     En = "System",      Filter = o => o.Tab == OptTab.System },
        new() { Ru = "Игра",        En = "Game",        Filter = o => o.Tab == OptTab.Game || o.Tab == OptTab.Other },
        new() { Ru = "Мирные",      En = "Crewmates",   Filter = o => o.Tab == OptTab.Crewmate },
        new() { Ru = "Предатели",   En = "Impostors",   Filter = o => o.Tab == OptTab.Impostor },
        new() { Ru = "Нейтралы",    En = "Neutrals",    Filter = o => o.Tab == OptTab.Neutral },
        new() { Ru = "Призраки",    En = "Ghosts",      Filter = o => o.Tab == OptTab.Ghost },
        new() { Ru = "Мини-игры",   En = "Mini-games",  Filter = o => o.Tab == OptTab.Mini },
    };

    static GameSettingMenu menu;
    static GameObject panel;
    static Transform container;
    static CategoryHeaderMasked hdrOrigin;
    static ToggleOption chkOrigin;
    static NumberOption numOrigin;
    static int mask;
    static int page;
    static readonly List<Row> dynRows = new();
    
    static Scroller scroller;
    static float y0;
    static float scrollY, scrollMax;

    static string PageName(int i) => Translator.GameIsRu ? Pages[i].Ru : Pages[i].En;
    
    static readonly List<PassiveButton> pageButtons = new();

    public static void Build(GameSettingMenu m)
    {
        pageButtons.Clear(); panel = null; dynRows.Clear();
        if (!Utils.AmHost || !Opts.NativeMenu.Value) return;
        menu = m;
        Dump("Presets", m.GamePresetsButton);
        Dump("GameSettings", m.GameSettingsButton);
        Dump("RoleSettings", m.RoleSettingsButton);
        mask = GameOptionsMenu.MASK_LAYER;

        Vector3 pPre = m.GamePresetsButton.transform.localPosition;
        float colX = pPre.x, w = 1.5f, rowH = 0.3f;
        Vector3 btnScale = new Vector3(0.45f, 0.35f, 1f);
        Func<int, Vector3> cell = k => new Vector3(colX + (k % 2 == 0 ? -w * 0.5f : w * 0.5f), pPre.y - rowH * (k / 2), pPre.z);
        try
        {
            m.GamePresetsButton.gameObject.SetActive(false);
            m.GameSettingsButton.transform.localPosition = cell(0); m.GameSettingsButton.transform.localScale = btnScale;
            m.RoleSettingsButton.transform.localPosition = cell(1); m.RoleSettingsButton.transform.localScale = btnScale;
        }
        catch (Exception e) { Plugin.Logger.LogWarning("ModMenu shift buttons: " + e.Message); }
        Color[] cols =
        {
            new Color32(0, 165, 255, 255),
            new Color32(60, 200, 110, 255),
            new Color32(120, 220, 230, 255),
            new Color32(255, 25, 25, 255),
            new Color32(255, 171, 27, 255),
        };

        for (int i = 0; i < Pages.Length; i++)
        {
            var btn = UnityEngine.Object.Instantiate(m.GameSettingsButton, m.GameSettingsButton.transform.parent);
            btn.name = "AoD_Tab_" + i;
            btn.transform.localPosition = cell(i + 2);
            btn.transform.localScale = btnScale;
            foreach (var tr in btn.GetComponentsInChildren<TextTranslatorTMP>(true)) UnityEngine.Object.Destroy(tr);
            string label = PageName(i);
            if (btn.buttonText != null) btn.buttonText.text = label;
            btn.ChangeButtonText(label);
            try
            {
                var col = cols[i % cols.Length];
                foreach (var go in new[] { btn.inactiveSprites, btn.activeSprites, btn.selectedSprites })
                    if (go != null) { var sr = go.GetComponent<SpriteRenderer>(); if (sr != null) sr.color = col; }
                btn.activeTextColor = btn.inactiveTextColor = Color.white;
                btn.selectedTextColor = new Color(0.7f, 0.7f, 0.7f);
                if (btn.buttonText != null) btn.buttonText.color = Color.white;
            }
            catch (Exception e) { Plugin.Logger.LogWarning("ModMenu tab colors: " + e.Message); }
            int idx = i;
            btn.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            btn.OnClick.AddListener((Action)(() => Show(idx)));
            btn.SelectButton(false);
            btn.gameObject.SetActive(true);
            pageButtons.Add(btn);
        }
        BuildPanel(m);
        Plugin.Logger.LogInfo($"ModMenu: built {Pages.Length} tabs (grid layout)");
    }

    static void Style(PassiveButton b) { }

    static void Dump(string n, PassiveButton b)
    {
        try
        {
            Plugin.Logger.LogInfo($"ModMenu {n}: obj={b.name} parent={b.transform.parent.name} pos={b.transform.localPosition} scale={b.transform.localScale} active={b.gameObject.activeSelf} text='{(b.buttonText != null ? b.buttonText.text : "-")}'");
        }
        catch (Exception e) { Plugin.Logger.LogInfo($"ModMenu {n}: dump failed {e.Message}"); }
    }

    static void BuildPanel(GameSettingMenu m)
    {
        var src = m.GameSettingsTab;
        panel = UnityEngine.Object.Instantiate(src.gameObject, src.transform.parent);
        panel.name = "AoD_Panel";

        var cm = panel.GetComponent<GameOptionsMenu>();
        container = cm.settingsContainer;
        hdrOrigin = cm.categoryHeaderOrigin;
        chkOrigin = cm.checkboxOrigin;
        numOrigin = cm.numberOptionOrigin;
        var scroll = cm.scrollBar;

        var keep = new HashSet<int>();
        foreach (var t in new Component[] { hdrOrigin, chkOrigin, numOrigin, cm.stringOptionOrigin, cm.playerOptionOrigin })
            if (t != null) keep.Add(t.gameObject.GetInstanceID());
        var kill = new List<GameObject>();
        for (int i = 0; i < container.childCount; i++)
        {
            var ch = container.GetChild(i).gameObject;
            if (!keep.Contains(ch.GetInstanceID())) kill.Add(ch);
        }
        foreach (var g in kill) { g.SetActive(false); UnityEngine.Object.Destroy(g); }

        scroller = scroll;
        y0 = container.localPosition.y;
        try { scroller.enabled = Opts.NativeScroll.Value; } catch { }
        UnityEngine.Object.DestroyImmediate(cm);
        panel.SetActive(false);
    }

    static Row MakeToggle(Opt o, float x, float y)
    {
        var row = new Row { Opt = o };
        var t = UnityEngine.Object.Instantiate(chkOrigin, container);
        row.Go = t.gameObject; row.Title = t.TitleText; row.Check = t.CheckMark;
        UnityEngine.Object.DestroyImmediate(t);
        foreach (var pb in row.Go.GetComponentsInChildren<PassiveButton>(true))
        {
            pb.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            pb.OnClick.AddListener((Action)(() => { row.Opt.Next(); Refresh(row); }));
        }
        Place(row, "AoD_Row_" + o.Id, x, y);
        return row;
    }

    static Row MakeNumber(Opt o, float x, float y, Action<int> onChange)
    {
        var row = new Row { Opt = o };
        var n = UnityEngine.Object.Instantiate(numOrigin, container);
        row.Go = n.gameObject; row.Title = n.TitleText; row.Value = n.ValueText;
        var plus = n.PlusBtn; var minus = n.MinusBtn;
        UnityEngine.Object.DestroyImmediate(n);
        plus.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        plus.OnClick.AddListener((Action)(() => { onChange(+1); Refresh(row); }));
        minus.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        minus.OnClick.AddListener((Action)(() => { onChange(-1); Refresh(row); }));
        Place(row, "AoD_Row_" + o.Id, x, y);
        return row;
    }

    static void Place(Row row, string name, float x, float y)
    {
        var go = row.Go;
        go.name = name;
        go.transform.localPosition = new Vector3(x, y, -2f);
        go.SetActive(true);
        if (row.Title != null)
        {
            row.Title.enableAutoSizing = true;
            row.Title.fontSizeMax = row.Title.fontSize;
            row.Title.fontSizeMin = 1.2f;
        }
        if (Opts.UseMask.Value) ApplyMask(go);
    }

    static void ApplyMask(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<SpriteRenderer>(true))
            try { PlayerMaterial.SetMaskLayer(r.material, mask); } catch { }
        foreach (var t in go.GetComponentsInChildren<TextMeshPro>(true))
            try { t.fontMaterial.SetFloat("_StencilComp", 3f); t.fontMaterial.SetFloat("_Stencil", mask); } catch { }
        foreach (var b in go.GetComponentsInChildren<PassiveButton>(true))
            try { b.SetMaskLayer(mask); } catch { }
    }

    static Row MakeCategory(string title, float y)
    {
        var row = new Row();
        var h = UnityEngine.Object.Instantiate(hdrOrigin, container);
        h.name = "AoD_Header";
        h.transform.localPosition = new Vector3(GameOptionsMenu.HEADER_X + Opts.RowX.Value, y, -2f);
        h.transform.localScale = Vector3.one * GameOptionsMenu.HEADER_SCALE;
        h.Title.text = title;
        h.gameObject.SetActive(true);
        if (Opts.UseMask.Value) ApplyMask(h.gameObject);
        row.Go = h.gameObject;
        return row;
    }

    static void Add(Row r, float y) { r.Y = y; dynRows.Add(r); }

    static void Populate()
    {
        float keep = scrollY;
        foreach (var r in dynRows) if (r.Go != null) { r.Go.SetActive(false); UnityEngine.Object.Destroy(r.Go); }
        dynRows.Clear();

        bool ru = Translator.GameIsRu;
        float x = GameOptionsMenu.START_POS_X + Opts.RowX.Value;
        float startY = GameOptionsMenu.START_POS_Y + Opts.RowY.Value;
        float sp = GameOptionsMenu.SPACING_Y * Opts.RowSpacing.Value;
        float y = startY;

        if (page >= 2 && page <= 4)
        {
            var team = page == 2 ? OptTab.Crewmate : page == 3 ? OptTab.Impostor : OptTab.Neutral;
            foreach (var role in Opts.Roles.Where(r => r.Tab == team))
            {
                var rr = role;
                Add(MakeCategory(rr.Colored(ru), y), y);
                y -= GameOptionsMenu.HEADER_HEIGHT;

                var chance = MakeNumber(rr.Chance, x, y, d => { rr.Chance.Inc(d); Populate(); });
                chance.Short = true; Refresh(chance); Add(chance, y); y -= sp;

                if (rr.Chance.Value > 0)
                {
                    var count = MakeNumber(rr.Count, x, y, d => rr.Count.Inc(d));
                    count.Short = true; Refresh(count); Add(count, y); y -= sp;

                    foreach (var ex in rr.Extras)
                    {
                        var e = ex;
                        Row er = e is BoolOpt ? MakeToggle(e, x, y) : MakeNumber(e, x, y, d => e.Inc(d));
                        er.Short = true; Refresh(er); Add(er, y); y -= sp;
                    }
                }
            }
        }
        else if (page == 5)
        {
            foreach (var gr in Ghosts.Roles)
            {
                var g = gr;
                Add(MakeCategory($"<color={g.color}>{(ru ? g.ru : g.en)}</color>", y), y);
                y -= GameOptionsMenu.HEADER_HEIGHT;
                var enabled = MakeToggle(g.on, x, y);
                enabled.Short = true; Refresh(enabled); Add(enabled, y); y -= sp;
                foreach (var ex in g.extras)
                {
                    var e = ex;
                    var er = MakeNumber(e, x, y, d => e.Inc(d));
                    er.Short = true; Refresh(er); Add(er, y); y -= sp;
                }
            }
        }
        else
        {
            foreach (var o in Opt.All)
            {
                if (!Pages[page].Filter(o)) continue;
                Row row = o is BoolOpt ? MakeToggle(o, x, y) : MakeNumber(o, x, y, d => o.Inc(d));
                Refresh(row);
                Add(row, y);
                y -= sp;
            }
        }

        scrollMax = Mathf.Max(0f, (startY - y) - Opts.ViewHeight.Value + sp);
        scrollY = Mathf.Clamp(keep, 0f, scrollMax);
        if (Opts.NativeScroll.Value && scroller != null)
        {
            try { scroller.SetYBoundsMin(y0); scroller.SetYBoundsMax(y0 + scrollMax); }
            catch (Exception e) { Plugin.Logger.LogWarning("ModMenu scroll bounds: " + e.Message); }
        }
        ApplyScroll();
        Tick();
    }

    static void ApplyScroll()
    {
        if (container == null) return;
        container.localPosition = new Vector3(container.localPosition.x, y0 + scrollY, container.localPosition.z);
    }

    public static void Tick()
    {
        KeepTabs();
        if (panel == null || !panel.activeSelf || container == null) return;

        if (Opts.NativeScroll.Value) scrollY = Mathf.Clamp(container.localPosition.y - y0, 0f, scrollMax);
        else if (scrollMax > 0f)
        {
            float d = -Input.mouseScrollDelta.y * Opts.ScrollStep.Value;
            if (Input.GetKey(KeyCode.DownArrow)) d += 4f * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow)) d -= 4f * Time.deltaTime;
            if (d != 0f)
            {
                scrollY = Mathf.Clamp(scrollY + d, 0f, scrollMax);
                ApplyScroll();
            }
        }

        float startY = GameOptionsMenu.START_POS_Y + Opts.RowY.Value;
        float top = startY + 0.05f;
        float bottom = startY - Opts.ViewHeight.Value + 0.1f;
        foreach (var r in dynRows)
        {
            if (r.Go == null) continue;
            float ry = r.Y + scrollY;
            bool vis = ry <= top && ry >= bottom;
            if (r.Go.activeSelf != vis) r.Go.SetActive(vis);
        }
    }

    static void KeepTabs()
    {
        for (int i = 0; i < pageButtons.Count; i++)
        {
            var b = pageButtons[i];
            if (b == null || b.buttonText == null) continue;
            try
            {
                string label = PageName(i);
                if (b.buttonText.text != label) b.buttonText.text = label;
                if (!b.buttonText.gameObject.activeSelf) b.buttonText.gameObject.SetActive(true);
                if (!b.buttonText.enabled) b.buttonText.enabled = true;
                var c = b.buttonText.color;
                if (c.a < 0.99f) b.buttonText.color = new Color(c.r, c.g, c.b, 1f);
            }
            catch { }
        }
    }

    static void Refresh(Row r)
    {
        if (r.Opt == null) return;
        r.Title.text = r.Short ? r.Opt.Short : r.Opt.Name;
        if (r.Opt is BoolOpt b) { if (r.Check != null) r.Check.enabled = b.Value; }
        else if (r.Value != null) r.Value.text = r.Opt.ValueText;
    }

    static void Show(int idx)
    {
        if (menu == null) return;
        try
        {
            page = idx;
            menu.PresetsTab.gameObject.SetActive(false);
            menu.GameSettingsTab.gameObject.SetActive(false);
            menu.RoleSettingsTab.gameObject.SetActive(false);
            menu.GamePresetsButton.SelectButton(false);
            menu.GameSettingsButton.SelectButton(false);
            menu.RoleSettingsButton.SelectButton(false);
            panel.SetActive(true);
            for (int i = 0; i < pageButtons.Count; i++) { pageButtons[i].SelectButton(i == idx); Style(pageButtons[i]); }
            scrollY = 0f;
            Populate();
            if (menu.MenuDescriptionText != null)
                menu.MenuDescriptionText.text = PageName(idx) + (scrollMax > 0f ? (Translator.GameIsRu ? "\n(колесо мыши - прокрутка)" : "\n(mouse wheel to scroll)") : "");
        }
        catch (Exception e) { Plugin.Logger.LogError("ModMenu.Show: " + e); }
    }

    public static void HideAll()
    {
        try
        {
            if (panel != null) panel.SetActive(false);
            foreach (var b in pageButtons) if (b != null) { b.SelectButton(false); Style(b); }
        }
        catch { }
    }
}
