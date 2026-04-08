using System;
using UnityEngine;

public static class AudioSettings
{
    public const string MusicKey = "MusicEnabled";
    public const string SoundKey = "SoundEnabled";

    public static event Action<bool> MusicEnabledChanged;
    public static event Action<bool> SoundEnabledChanged;

    private static bool GetMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicKey, 1) == 1;
    }

    private static bool GetSoundEnabled()
    {
        return PlayerPrefs.GetInt(SoundKey, 1) == 1;
    }

    public static bool IsMusicEnabled
    {
        get => GetMusicEnabled();
        set
        {
            bool current = GetMusicEnabled();
            if (current == value) return;

            PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
            MusicEnabledChanged?.Invoke(value);
        }
    }

    public static bool IsSoundEnabled
    {
        get => GetSoundEnabled();
        set
        {
            bool current = GetSoundEnabled();
            if (current == value) return;

            PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
            SoundEnabledChanged?.Invoke(value);
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
