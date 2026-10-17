using System;
using UnityEngine;

namespace AshesOfDisaster;

// На собрании рядом со значком уровня каждого игрока добавляется такой же значок с его ID (нужен для команд /ab, /role, /tag и др.).
// Это только интерфейс на экране хоста: другие игроки его не видят.
public static class MeetingIds
{
    // В разных версиях interop поле с ID игрока называется по-разному (или его нет) — ищем через reflection, запасной путь — по нику
    static string IdOf(PlayerVoteArea pva)
    {
        try
        {
            var bf = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var tp = pva.GetType();
            foreach (var f in tp.GetFields(bf))
                if (f.Name.IndexOf("TargetPlayerId", StringComparison.OrdinalIgnoreCase) >= 0 && f.FieldType.IsPrimitive) return Convert.ToInt32(f.GetValue(pva)).ToString();
            foreach (var p in tp.GetProperties(bf))
                if (p.Name.IndexOf("TargetPlayerId", StringComparison.OrdinalIgnoreCase) >= 0 && p.GetIndexParameters().Length == 0 && p.PropertyType.IsPrimitive) return Convert.ToInt32(p.GetValue(pva)).ToString();
        }
        catch { }
        try
        {
            string nm = pva.NameText != null ? pva.NameText.text : null;
            if (!string.IsNullOrEmpty(nm))
                foreach (var pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc.Data != null && pc.Data.PlayerName != null && nm.Contains(pc.Data.PlayerName)) return pc.PlayerId.ToString();
        }
        catch { }
        return "?";
    }

    public static void Add()
    {
        try
        {
            var m = MeetingHud.Instance;
            if (m == null || m.playerStates == null) return;
            foreach (var pva in m.playerStates)
            {
                if (pva == null) continue;
                var level = pva.transform.Find("PlayerLevel");
                if (level == null || pva.transform.Find("PlayerId") != null) continue;
                var disp = UnityEngine.Object.Instantiate(level, pva.transform);
                disp.localPosition = new Vector3(-1.21f, -0.15f, level.localPosition.z);
                disp.SetSiblingIndex(level.GetSiblingIndex() + 1);
                disp.gameObject.name = "PlayerId";
                var sr = disp.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Palette.Purple;
                var label = disp.Find("LevelLabel");
                var number = disp.Find("LevelNumber");
                if (label != null)
                {
                    var tr = label.GetComponent<TextTranslatorTMP>();
                    if (tr != null) UnityEngine.Object.Destroy(tr);
                    var t = label.GetComponent<TMPro.TextMeshPro>();
                    if (t != null) t.text = "ID";
                    label.name = "IdLabel";
                }
                if (number != null)
                {
                    var t = number.GetComponent<TMPro.TextMeshPro>();
                    if (t != null) t.text = IdOf(pva);
                    number.name = "IdNumber";
                }
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("MeetingIds: " + e.Message); }
    }
}
