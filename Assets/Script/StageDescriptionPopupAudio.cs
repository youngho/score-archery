using UnityEngine;

/// <summary>
/// 스테이지 설명 팝업 표시/닫힘 시 Resources의 ready.wav, go.wav 재생.
/// BGM과 동시에 들리도록 전용 AudioSource(우선순위 최상)를 쓰고, go 재생 시 BGM은 잠시 덕킹됩니다.
/// </summary>
public static class StageDescriptionPopupAudio
{
    private static AudioClip _readyClip;
    private static AudioClip _goClip;
    private static bool _loaded;
    private static AudioSource _sfxSource;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _readyClip = Resources.Load<AudioClip>("ready");
        _goClip = Resources.Load<AudioClip>("go");
    }

    private static void EnsureSfxSource()
    {
        if (_sfxSource != null) return;
        var go = new GameObject(nameof(StageDescriptionPopupAudio) + "_Sfx");
        Object.DontDestroyOnLoad(go);
        _sfxSource = go.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;
        _sfxSource.priority = 0;
        _sfxSource.loop = false;
    }

    private static void PlayClip2D(AudioClip clip)
    {
        if (clip == null) return;
        if (!AudioSettings.IsSoundEnabled) return; // 전역 설정 체크
        EnsureSfxSource();
        _sfxSource.PlayOneShot(clip);
    }

    public static void PlayReady()
    {
        EnsureLoaded();
        PlayClip2D(_readyClip);
    }

    public static void PlayGo()
    {
        EnsureLoaded();
        if (_goClip != null)
            StageRandomBGMController.RegisterPendingGoOverlap(_goClip.length);
        PlayClip2D(_goClip);
    }
}
