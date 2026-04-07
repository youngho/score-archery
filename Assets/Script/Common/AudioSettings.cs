using UnityEngine;

public static class AudioSettings
{
    public const string MusicKey = "MusicEnabled";
    public const string SoundKey = "SoundEnabled";

    public static bool IsMusicEnabled
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set => PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
    }

    public static bool IsSoundEnabled
    {
        get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
        set => PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
