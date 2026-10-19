using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

/// Заставка мода при запуске: корабль падает на Марс, вспышка, кратер, пепел и название.
/// Рисуется обычными спрайтами (IMGUI в этой игре вырезан), любая клавиша или клик пропускает. Около 10 секунд, один раз за запуск.
public static class Intro
{
    public static BoolOpt On;
    const float Impact = 3.6f, Total = 10.5f;
    static bool done, started, failed;
    static float t0 = -1f, firstTick = -1f;
    static int W, H;

    public static void Define(ConfigFile c)
    {
        On = new BoolOpt("IntroScreen", OptTab.System, true, c);
        On.NameRu = "Заставка мода при запуске"; On.NameEn = "Mod intro screen at startup";
        On.GroupRu = "Лобби"; On.GroupEn = "Lobby";
    }

    // ---------------------------------------------------------------- Спрайты-квадраты
    class Px { public GameObject Go; public Transform Tr; public SpriteRenderer Sr; public bool Active; }
    static readonly List<Px> pool = new();
    static int next, lastNext;
    static Sprite square;
    static Camera cam;
    static float camX, camY, camZ, halfW, halfH, ox, oy;

    static void Setup()
    {
        cam = Camera.main;
        if (cam == null && Camera.allCamerasCount > 0) cam = Camera.allCameras[0];
        if (cam == null) throw new Exception("no camera");
        W = Screen.width; H = Screen.height;
        halfH = cam.orthographicSize; halfW = halfH * cam.aspect;
        var p = cam.transform.position; camX = p.x; camY = p.y; camZ = p.z + 5f;
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        Plugin.Logger.LogInfo($"Intro: sprites ready (ortho={cam.orthographic}, size={halfH}, screen {W}x{H})");
    }

    static Px Make(int order)
    {
        var go = new GameObject("AoD_px");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square; sr.sortingOrder = 30000 + order;
        return new Px { Go = go, Tr = go.transform, Sr = sr, Active = true };
    }

    /// Прямоугольник в пикселях экрана (левый верхний угол) цветом c
    static void Box(float x, float y, float w, float h, Color c)
    {
        if (c.a <= 0.003f) return;
        var p = next < pool.Count ? pool[next] : null;
        if (p == null) { p = Make(next); pool.Add(p); }
        next++;
        if (!p.Active) { p.Go.SetActive(true); p.Active = true; }
        float cx = (x + ox + w * 0.5f) / W, cy = (y + oy + h * 0.5f) / H;
        p.Tr.position = new Vector3(camX + (cx - 0.5f) * 2f * halfW, camY + (0.5f - cy) * 2f * halfH, camZ);
        p.Tr.localScale = new Vector3(w / W * 2f * halfW, h / H * 2f * halfH, 1f);
        p.Sr.color = c;
    }
    static void Dot(float x, float y, float r, Color c) => Box(x - r, y - r, r * 2f, r * 2f, c);

    static void Hide()
    {
        for (int i = next; i < pool.Count; i++) if (pool[i].Active) { pool[i].Go.SetActive(false); pool[i].Active = false; }
    }
    static void Cleanup()
    {
        foreach (var p in pool) if (p.Go != null) UnityEngine.Object.Destroy(p.Go);
        pool.Clear();
    }

    // ---------------------------------------------------------------- Сцена
    static float[] sx, sy, sr, ax, ay, avy, avx, ap, ar, rx, rh, rw;
    static float[] rwin;
    static bool built;

    static void Build()
    {
        built = true;
        var rng = new System.Random(7);
        float F() => (float)rng.NextDouble();
        sx = new float[70]; sy = new float[70]; sr = new float[70];
        for (int i = 0; i < 70; i++) { sx[i] = F() * W; sy[i] = F() * H * 0.5f; sr[i] = 2f + F() * 2f; }
        ax = new float[120]; ay = new float[120]; avy = new float[120]; avx = new float[120]; ap = new float[120]; ar = new float[120];
        for (int i = 0; i < 120; i++) { ax[i] = F() * W; ay[i] = F() * H; avy[i] = (20f + F() * 70f) * H / 800f + 12f; avx[i] = -12f + F() * 20f; ap[i] = F() * 6.28f; ar[i] = 2f + F() * 3f; }
        int n = 14; rx = new float[n]; rh = new float[n]; rw = new float[n];
        float x = -10f;
        for (int i = 0; i < n; i++) { rw[i] = W * (0.05f + F() * 0.06f); rh[i] = H * (0.05f + F() * 0.08f); rx[i] = x; x += rw[i] + W * (0.01f + F() * 0.05f); }
    }

    static readonly Dictionary<char, string[]> Font = new()
    {
        ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
        ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
        ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
        ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
        ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
        ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
    };

    static void Title(float ti)
    {
        string text = "ASHES OF DISASTER";
        float c = Mathf.Max(2f, Mathf.Floor(W * 0.8f / (text.Length * 6f - 1f)));
        float totalW = (text.Length * 6f - 1f) * c;
        float x0 = (W - totalW) * 0.5f, y0 = H * 0.18f;
        for (int i = 0; i < text.Length; i++)
        {
            if (!Font.TryGetValue(text[i], out var g)) continue;
            var col = i >= 9 ? new Color(1f, 0.55f, 0.24f, ti) : new Color(0.93f, 0.86f, 0.8f, ti);
            for (int r = 0; r < 7; r++)
                for (int k = 0; k < 5; k++)
                    if (g[r][k] == '#') Box(x0 + (i * 6 + k) * c, y0 + r * c, c, c, col);
        }
    }

    static Vector2 Ship(float u, float ix, float iy)
    {
        float e = Mathf.Pow(Mathf.Clamp01(u), 1.5f);
        return new Vector2(-W * 0.08f + (ix + W * 0.08f) * e, H * 0.04f + (iy - H * 0.04f) * e - Mathf.Sin(e * Mathf.PI) * H * 0.05f);
    }

    static void Render(float t)
    {
        if (!built) Build();
        next = 0;
        float groundY = H * 0.78f, ix = W * 0.62f, iy = groundY + (H - groundY) * 0.35f;
        float smoke = Mathf.Clamp01((t - Impact) / 3f), hot = Mathf.Clamp01((t - Impact) / 1.2f);
        ox = oy = 0f;
        if (t > Impact && t < Impact + 1.4f) { float m = (1f - (t - Impact) / 1.4f) * 12f; ox = UnityEngine.Random.Range(-m, m); oy = UnityEngine.Random.Range(-m, m); }

        // небо Марса полосами
        int strips = 24;
        for (int i = 0; i < strips; i++)
        {
            float k = (float)i / (strips - 1);
            var top = new Color(0.14f, 0.06f, 0.07f);
            var low = new Color(0.74f - 0.4f * smoke, 0.38f - 0.2f * smoke, 0.22f - 0.12f * smoke);
            Box(-40f, k * groundY, W + 80f, groundY / strips + 3f, Color.Lerp(top, low, k * k));
        }
        for (int i = 0; i < sx.Length; i++)
            Box(sx[i], sy[i], sr[i], sr[i], new Color(0.95f, 0.9f, 0.85f, (0.3f + 0.3f * Mathf.Sin(t * 1.6f + i)) * (1f - smoke * 0.9f)));
        // две луны
        Dot(W * 0.2f, H * 0.16f, H * 0.03f, new Color(0.7f, 0.62f, 0.55f, 1f - smoke * 0.7f));
        Dot(W * 0.3f, H * 0.1f, H * 0.016f, new Color(0.6f, 0.56f, 0.52f, 1f - smoke * 0.7f));

        // свечение горизонта
        if (hot > 0f) Dot(ix, groundY, W * 0.7f, new Color(1f, 0.35f, 0.12f, 0.35f * hot));
        // земля и колония
        Box(-40f, groundY, W + 80f, H - groundY + 40f, new Color(0.5f, 0.24f, 0.15f));
        Box(-40f, groundY, W + 80f, 4f, new Color(0.66f, 0.34f, 0.22f));
        var wall = new Color(0.2f, 0.09f, 0.07f); var roof = new Color(0.32f, 0.16f, 0.12f);
        for (int i = 0; i < rx.Length; i++)
        {
            if (rw[i] <= 0f) continue;
            float cxm = rx[i] + rw[i] * 0.5f, d = Mathf.Abs(cxm - ix);
            float bt = Impact + d / (W * 0.8f) * 1.6f;           // когда дойдёт ударная волна
            float tau = t - bt, k2 = Mathf.Clamp01(tau / 0.45f);
            float rem = d < W * 0.25f ? 0.15f : (d < W * 0.5f ? 0.4f : 0.7f);
            float hh = rh[i] * (1f - (1f - rem) * k2);
            Box(rx[i], groundY - hh, rw[i], hh + 3f, wall);
            if (k2 < 0.6f) Box(rx[i] - 3f, groundY - hh - 5f, rw[i] + 6f, 6f, roof);
            else { Box(rx[i], groundY - hh - 6f, rw[i] * 0.35f, 7f, roof); Box(rx[i] + rw[i] * 0.55f, groundY - hh - 3f, rw[i] * 0.3f, 4f, roof); }
            if (k2 < 0.2f)
                for (float wx = rw[i] * 0.15f; wx < rw[i] * 0.8f; wx += rw[i] * 0.3f)
                {
                    bool lit = ((i * 13 + (int)wx) % 10) >= hot * 7f;
                    Box(rx[i] + wx, groundY - rh[i] * 0.7f, 8f, 6f, lit ? new Color(1f, 0.84f, 0.5f, 0.9f) : new Color(1f, 0.84f, 0.5f, 0.2f));
                }
            if (tau > 0f)
            {
                float dir = cxm < ix ? -1f : 1f;
                for (int j = 0; j < 6; j++)            // обломки стен летят и ложатся
                {
                    float px = cxm + ((j - 2.5f) * 35f + dir * 70f) * tau, py = groundY - rh[i] * 0.5f - (110f + j * 45f) * tau + 520f * tau * tau;
                    Box(px, Mathf.Min(py, groundY - 6f - (j % 2) * 4f), 9f, 9f, j % 2 == 0 ? wall : roof);
                }
                Dot(cxm, groundY - hh * 0.5f, rw[i] * 0.7f * k2, new Color(0.45f, 0.3f, 0.22f, 0.4f * (1f - Mathf.Clamp01(tau / 2.5f))));
            }
        }
        // корабль
        float u = (t - 0.4f) / 3.2f;
        if (u > 0f && u < 1f)
        {
            var hp = Ship(u, ix, iy); var pp = Ship(u - 0.02f, ix, iy);
            var dir = (hp - pp).normalized; var nrm = new Vector2(-dir.y, dir.x);
            float cell = H * (0.02f + 0.014f * u);
            for (int k = 0; k < 22; k++)           // огненный хвост
            {
                var p = hp - dir * (k + 6) * cell * 0.8f; float f = 1f - k / 22f;
                var col = k < 7 ? new Color(1f, 0.9f, 0.6f) : (k < 14 ? new Color(1f, 0.6f, 0.25f) : new Color(1f, 0.3f, 0.1f));
                col.a = f * 0.75f;
                Dot(p.x, p.y, cell * (0.9f * f + 0.3f), col);
            }
            Dot(hp.x + dir.x * cell * 2f, hp.y + dir.y * cell * 2f, cell * 4.2f, new Color(1f, 0.55f, 0.2f, 0.4f));   // раскалённый воздух
            for (int k = 0; k < 13; k++)             // корпус
            {
                var p = hp - dir * k * cell;
                var col = k == 0 ? new Color(1f, 0.97f, 0.88f) : (k % 3 == 2 ? new Color(0.3f, 0.7f, 1f) : (k > 9 ? new Color(0.4f, 0.45f, 0.55f) : (k % 2 == 0 ? new Color(0.78f, 0.8f, 0.86f) : new Color(0.66f, 0.7f, 0.78f))));
                Dot(p.x, p.y, cell * (k == 0 ? 0.6f : 1.05f), col);
                if (k == 6) Dot(p.x + nrm.x * cell * 1.6f, p.y + nrm.y * cell * 1.6f, cell * 0.35f, new Color(0.9f, 0.9f, 0.95f));   // антенна
            }
            for (int s = -1; s <= 1; s += 2)         // крылья
                for (int k = 5; k < 11; k++)
                {
                    var p = hp - dir * k * cell + nrm * s * cell * (1.2f + (k - 5) * 0.7f);
                    Dot(p.x, p.y, cell * 0.7f, new Color(0.55f, 0.58f, 0.66f));
                }
        }
        // кратер и обломки
        if (t >= Impact)
        {
            float grow = 1f - Mathf.Pow(1f - Mathf.Clamp01((t - Impact) / 0.8f), 3f);
            float crx = W * 0.085f * grow, cry = (H - groundY) * 0.24f * grow;
            Box(ix - crx, iy - cry * 0.7f, crx * 2f, cry * 1.4f, new Color(0.09f, 0.035f, 0.025f));
            Box(ix - crx * 0.6f, iy - cry * 0.3f, crx * 1.2f, cry * 0.6f, new Color(1f, 0.45f, 0.15f, 0.55f * Mathf.Clamp01(1.4f - (t - Impact) * 0.18f)));
            for (int i = 0; i < 30; i++)
            {
                float a = i / 30f * 6.2832f;
                float px = ix + Mathf.Cos(a) * crx, py = iy + Mathf.Sin(a) * cry;
                Box(px - 4f, py - 4f, 8f, 8f, py < iy ? new Color(0.72f, 0.4f, 0.27f) : new Color(0.4f, 0.2f, 0.14f));
            }
            for (int i = 0; i < 6; i++)
                Box(ix - crx * 0.5f + i * crx * 0.22f, iy - cry * 0.2f - (i % 3) * 5f, 9f, 6f, new Color(0.45f, 0.48f, 0.55f));
        }
        // дым из кратера
        if (t >= Impact)
            for (int i = 0; i < 20; i++)
            {
                float age = (t - Impact) * (0.4f + 0.05f * (i % 5)) + i * 0.3f;
                float life = 6f; float ph = (age % life) / life;
                float px = ix + Mathf.Sin(i * 7.1f) * W * 0.05f + ph * W * 0.06f, py = iy - ph * H * 0.55f, r = H * (0.03f + 0.07f * ph);
                Dot(px, py, r, new Color(0.18f, 0.12f, 0.1f, 0.35f * (1f - ph) * Mathf.Min(1f, (t - Impact) * 0.5f)));
            }
        // ударная волна
        if (t >= Impact && t < Impact + 1.6f)
        {
            float sw = (t - Impact) / 1.6f;
            Box(ix - sw * W * 0.8f, iy - 2f, sw * W * 1.6f, 4f + 8f * (1f - sw), new Color(1f, 0.78f, 0.55f, (1f - sw) * 0.7f));
        }
        // искры
        if (t >= Impact)
        {
            float tau = t - Impact; var rng = new System.Random(11);
            for (int b = 0; b < 50; b++)
            {
                float ang = Mathf.PI * (1.05f + 0.9f * (float)rng.NextDouble()), sp = 160f + 460f * (float)rng.NextDouble(), life = 1f + 1.6f * (float)rng.NextDouble();
                if (tau > life) continue;
                float px = ix + Mathf.Cos(ang) * sp * tau, py = iy + Mathf.Sin(ang) * sp * tau + 320f * tau * tau;
                if (py > iy + 6f) continue;
                Box(px, py, 4f, 4f, new Color(1f, 0.69f, 0.35f, 1f - tau / life));
            }
        }
        // пыль и пепел
        float amt = 0.12f + 0.88f * smoke;
        int count = Mathf.FloorToInt(ax.Length * amt);
        for (int i = 0; i < count; i++)
        {
            float y = ((ay[i] + avy[i] * t) % (H + 12f)) - 6f;
            float x = ax[i] + avx[i] * t + Mathf.Sin(t * 1.3f + ap[i]) * 14f;
            x = ((x % W) + W) % W;
            Dot(x, y, ar[i] * 0.6f, i % 2 == 0 ? new Color(0.85f, 0.6f, 0.45f, 0.55f) : new Color(0.62f, 0.58f, 0.55f, 0.5f));
        }
        ox = oy = 0f;
        // вспышка
        if (t >= Impact && t < Impact + 0.7f) Box(-40f, -40f, W + 80f, H + 80f, new Color(1f, 0.94f, 0.84f, (1f - (t - Impact) / 0.7f) * 0.9f));
        // название
        float ti = Mathf.Clamp01((t - (Impact + 1.6f)) / 2.2f);
        if (ti > 0f) Title(ti);
        // затухание в конце
        float fade = Mathf.Clamp01((t - (Total - 0.8f)) / 0.8f);
        if (fade > 0f) Box(-40f, -40f, W + 80f, H + 80f, new Color(0f, 0f, 0f, fade));
        Hide();
    }

    // ---------------------------------------------------------------- Управление
    /// Раз в кадр из ModGui.Update: рисует кадр и ловит нажатие для пропуска
    public static void Tick()
    {
        if (done) return;
        try
        {
            if (On == null || !On.Value) { done = true; return; }
            if (firstTick < 0f) { firstTick = Time.realtimeSinceStartup; Plugin.Logger.LogInfo("Intro: Tick running, enabled=True"); }
            if (!started)
            {
                if (Camera.main == null && Camera.allCamerasCount == 0)
                {
                    if (Time.realtimeSinceStartup - firstTick > 8f) { done = true; Plugin.Logger.LogWarning("Intro: no camera found, skipped"); }
                    return;
                }
                Setup(); started = true; t0 = Time.realtimeSinceStartup;
                Plugin.Logger.LogInfo("Intro: started");
                StartMusic();
            }
            float t = Time.realtimeSinceStartup - t0;
            if (t > Total) { Finish(); return; }
            if (t > 0.6f && (Input.anyKeyDown || Input.GetMouseButtonDown(0))) { Finish(); return; }
            Render(t);
        }
        catch (Exception e)
        {
            if (!failed) { failed = true; Plugin.Logger.LogWarning("Intro: " + e.GetType().Name + ": " + e.Message); }
            Finish();
        }
    }

    static void Finish()
    {
        done = true;
        StopMusic();
        try { Cleanup(); } catch (Exception) { }
    }

    // ---------------------------------------------------------------- Музыка
    static readonly float[] Arp = { 220f, 261.6f, 329.6f, 392f, 329.6f, 261.6f };
    static readonly float[] Bells = { 164.8f, 196f, 220f, 164.8f, 246.9f, 196f };
    static AudioSource src;
    static AudioClip MakeClip()
    {
        int sr = 22050, n = (int)(Total * sr);
        var d = new float[n];
        var rng = new System.Random(5);
        float noiseLp = 0f, boomPh = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr, v = 0f;
            float fadeIn = Mathf.Clamp01(t / 2.5f), fadeOut = Mathf.Clamp01((Total - t) / 0.8f);
            v += (Mathf.Sin(6.2832f * 55f * t) * 0.12f + Mathf.Sin(6.2832f * 55.4f * t) * 0.08f + Mathf.Sin(6.2832f * 82.4f * t) * 0.07f) * fadeIn;
            // арпеджио до удара
            if (t < Impact)
            {
                float tau = t % 0.6f; int k = (int)(t / 0.6f);
                v += Mathf.Sin(6.2832f * Arp[k % 6] * t) * 0.08f * Mathf.Exp(-3f * tau);
            }
            // свист метеорита
            if (t > 0.8f && t < Impact)
            {
                float u = (t - 0.8f) / (Impact - 0.8f);
                noiseLp += ((float)rng.NextDouble() * 2f - 1f - noiseLp) * (0.05f + 0.5f * u);
                v += noiseLp * 0.3f * u * u;
            }
            // удар
            if (t >= Impact)
            {
                float tau = t - Impact;
                noiseLp += ((float)rng.NextDouble() * 2f - 1f - noiseLp) * Mathf.Max(0.02f, 0.4f * Mathf.Exp(-3f * tau));
                boomPh += 6.2832f * Mathf.Lerp(90f, 32f, Mathf.Clamp01(tau / 1.5f)) / sr;
                v += (noiseLp * 0.9f + Mathf.Sin(boomPh) * 0.8f) * Mathf.Exp(-2.2f * tau);
                v += noiseLp * 0.05f;      // ветер
                float bt = tau - 1.6f;
                if (bt > 0f)
                {
                    int bi = (int)(bt / 1.3f); float bl = bt - bi * 1.3f;
                    v += Mathf.Sin(6.2832f * Bells[bi % 6] * t) * 0.1f * Mathf.Exp(-1.2f * bl);
                }
            }
            d[i] = Mathf.Clamp(v * fadeOut, -1f, 1f);
        }
        var clip = AudioClip.Create("AoD_Intro", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }

    static void StartMusic()
    {
        try
        {
            var go = new GameObject("AoD_IntroAudio");
            UnityEngine.Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>();
            src.clip = MakeClip(); src.volume = 0.8f; src.loop = false; src.Play();
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Intro music: " + e.Message); }
    }

    static void StopMusic()
    {
        try { if (src != null) { src.Stop(); UnityEngine.Object.Destroy(src.gameObject); src = null; } } catch (Exception) { }
    }
}
