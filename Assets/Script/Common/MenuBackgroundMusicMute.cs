using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메뉴 씬(00StartUI)의 BackGroundMusic AudioSource를 AudioSettings.MusicEnabled에 따라 뮤트/복구합니다.
/// SettingsUIController에서 GameObject.Find 하던 의존을 제거했기 때문에, 전역 부트스트랩으로 대체합니다.
/// </summary>
public static class MenuBackgroundMusicMute
{
    private static AudioSource _audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        AudioSettings.MusicEnabledChanged += enabled =>
        {
            if (_audioSource != null)
                _audioSource.mute = !enabled;
        };

        SceneManager.sceneLoaded += (_, __) => TryHook();
        TryHook();
    }

    private static void TryHook()
    {
        var bgmObj = GameObject.Find("BackGroundMusic");
        _audioSource = bgmObj != null ? bgmObj.GetComponent<AudioSource>() : null;

        if (_audioSource != null)
            _audioSource.mute = !AudioSettings.IsMusicEnabled;
    }
}

