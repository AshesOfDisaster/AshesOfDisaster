using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace AshesOfDisaster;

// Own music: drop files into BepInEx/AshesMusic, pick a track in the settings laptop, tab "Music".
// Plays on the host's PC only (the mod is host-only, other players have no mod and hear nothing).
public static class Music
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciSendStringW")]
    static extern int Mci(string cmd, StringBuilder ret, int len, IntPtr cb);

    static readonly string[] Ext = { ".mp4", ".m4a", ".mp3", ".wav", ".wma" };
    public static string Dir => Path.Combine(Paths.BepInExRootPath, "AshesMusic");

    public static IntOpt Volume;
    public static BoolOpt Loop;
    public static TrackOpt Track;

    static string[] files = new string[0];
    static float nextScan, nextPoll;
    static int playing = -1, lastVol = -1;
    static bool broken;

    public static void Init(ConfigFile c)
    {
        try { Directory.CreateDirectory(Dir); } catch { }
        Track = new TrackOpt();
        Track.NameRu = "Трек (папка BepInEx/AshesMusic, листай стрелками)"; Track.NameEn = "Track (folder BepInEx/AshesMusic, use the arrows)";
        Track.ShortRu = "Трек"; Track.ShortEn = "Track";
        Volume = new IntOpt("MusicVolume", OptTab.Music, 50, 0, 100, 5, c);
        Volume.NameRu = "Громкость музыки"; Volume.NameEn = "Music volume";
        Loop = new BoolOpt("MusicLoop", OptTab.Music, true, c);
        Loop.NameRu = "Повторять трек"; Loop.NameEn = "Repeat the track";
        foreach (var o in new Opt[] { Track, Volume, Loop }) { o.GroupRu = "Музыка"; o.GroupEn = "Music"; }
        Scan();
    }

    public static void Scan()
    {
        nextScan = Time.realtimeSinceStartup + 3f;
        try
        {
            files = Directory.Exists(Dir)
                ? Directory.GetFiles(Dir).Where(f => Ext.Contains(Path.GetExtension(f).ToLowerInvariant())).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray()
                : new string[0];
        }
        catch { files = new string[0]; }
        if (playing >= files.Length) Stop();
    }

    public static int Count => files.Length;
    public static int Current => playing;
    public static string NameOf(int i) => i >= 0 && i < files.Length ? Path.GetFileNameWithoutExtension(files[i]) : "";

    static bool Cmd(string c, StringBuilder ret = null)
    {
        if (broken) return false;
        try { return Mci(c, ret, ret == null ? 0 : ret.Capacity, IntPtr.Zero) == 0; }
        catch (Exception e) { broken = true; Plugin.Logger.LogWarning("Music: winmm is not available: " + e.Message); return false; }
    }

    static readonly System.Collections.Generic.List<AudioSource> muted = new();

    // while our own track plays, the game's music is muted; after stopping it is restored
    static void MuteGameMusic()
    {
        var sm = SoundManager.Instance;
        if (sm == null) return;
        var ch = sm.MusicChannel;
        foreach (var src in UnityEngine.Object.FindObjectsOfType<AudioSource>())
        {
            try
            {
                if (src == null || src.mute || src.outputAudioMixerGroup == null || src.outputAudioMixerGroup != ch) continue;
                src.mute = true; muted.Add(src);
            }
            catch { }
        }
    }

    static void RestoreGameMusic()
    {
        foreach (var src in muted) { try { if (src != null) src.mute = false; } catch { } }
        muted.Clear();
    }

    public static void Stop()
    {
        RestoreGameMusic();
        if (playing >= 0) { Cmd("stop aodmusic"); Cmd("close aodmusic"); }
        playing = -1;
    }

    public static void Play(int i)
    {
        Scan();
        Stop();
        if (i < 0 || i >= files.Length) return;
        string p = files[i];
        string type = Path.GetExtension(p).ToLowerInvariant() == ".wav" ? "waveaudio" : "mpegvideo";
        if (!Cmd($"open \"{p}\" type {type} alias aodmusic"))
        {
            Utils.Toast(Translator.GameIsRu ? "Не удалось открыть файл: " + Path.GetFileName(p) : "Could not open the file: " + Path.GetFileName(p));
            Plugin.Logger.LogWarning("Music: cannot open " + p);
            return;
        }
        playing = i; lastVol = -1;
        SetVolume();
        Cmd("play aodmusic from 0");
        try { MuteGameMusic(); } catch { }
    }

    static void SetVolume()
    {
        if (playing < 0 || Volume == null) return;
        lastVol = Volume.Value;
        Cmd($"setaudio aodmusic volume to {lastVol * 10}");
    }

    public static void Tick()
    {
        try
        {
            float now = Time.realtimeSinceStartup;
            if (now >= nextScan) Scan();
            if (playing < 0 || now < nextPoll) return;
            nextPoll = now + 0.5f;
            MuteGameMusic();
            if (lastVol != Volume.Value) SetVolume();
            var sb = new StringBuilder(64);
            if (Cmd("status aodmusic mode", sb) && sb.ToString().Trim() == "stopped")
            {
                if (Loop.Value) Cmd("play aodmusic from 0"); else Stop();
            }
        }
        catch (Exception e) { Plugin.Logger.LogWarning("Music.Tick: " + e.Message); }
    }
}

public class TrackOpt : Opt
{
    public TrackOpt() : base("MusicTrack", OptTab.Music) { }

    public override string ValueText
    {
        get
        {
            bool ru = Translator.GameIsRu;
            if (Music.Count == 0) return ru ? "нет файлов" : "no files";
            if (Music.Current < 0) return ru ? "выкл" : "off";
            string n = Music.NameOf(Music.Current);
            return (Music.Current + 1) + ". " + (n.Length > 18 ? n.Substring(0, 18) + "…" : n);
        }
    }

    public override void Set(string v)
    {
        if (int.TryParse(v, out var n)) Music.Play(n - 1);
        else Music.Stop();
    }

    public override void Next() => Inc(1);

    public override void Inc(int dir)
    {
        Music.Scan();
        int n = Music.Count;
        if (n == 0) return;
        int cur = Music.Current + 1;            // 0 = off, 1..n = tracks
        cur = ((cur + dir) % (n + 1) + (n + 1)) % (n + 1);
        Music.Play(cur - 1);
    }
}
