using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resources/bgm 폴더의 AudioClip 중 하나를 무작위로 루프 재생합니다.
/// 재생 시점은 <see cref="Timer.StartTimer"/> 호출 시(설명 팝업이 닫히고 타이머가 시작될 때)입니다.
/// 00StartUI, 99StageResult 씬에서는 재생하지 않습니다.
/// </summary>
public class StageRandomBGMController : MonoBehaviour
{
    private const string ResourcesBgmFolder = "bgm";

    /// <summary>메뉴·결과 씬에서는 스테이지 BGM을 켜지 않음 (결과 씬은 StageResultBGMPlayer 사용)</summary>
    private static bool IsStageBgmScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return sceneName != "00StartUI" && sceneName != "99StageResult" && sceneName != "88Leaderboard";
    }

    private static StageRandomBGMController _instance;

    /// <summary>다음 BGM 시작 시 go.wav 와 겹치면 이 시간(초) 동안 BGM 볼륨을 낮춥니다.</summary>
    private static float _pendingGoOverlapSeconds;

    private AudioSource _audioSource;
    private Coroutine _bgmDuckCoroutine;
    private float _baseBgmVolume = 0.3f;

    private const float BgmVolumeWhileGoSfx = 0.22f;

    private static readonly System.Collections.Generic.Dictionary<string, string> _sceneBgmMapping = 
        new System.Collections.Generic.Dictionary<string, string>
    {
        { "01Scarecrow", "Grasslands Theme" },
        { "02Leaf", "Grasslands Theme" },
        { "03MenTarget", "Dungeon Theme" },
        { "04Balloon", "Mushroom Theme" },
        { "05RubberDuck", "Mushroom Theme" },
        { "06AppleOneShot", "Dungeon Theme" },
        { "07Container", "Desert Theme" },
        { "08Coconut", "Desert Theme" },
        { "09Watermelon", "Desert Theme" },
        { "10Star", "Battle in the Stars" },
        { "11Candle", "Dungeon Theme" },
        { "12Snowman", "Iceland Theme" }
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var go = new GameObject(nameof(StageRandomBGMController));
        go.AddComponent<StageRandomBGMController>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f;
        _audioSource.priority = 200;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyForActiveScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForSceneName(scene.name);
    }

    private void ApplyForActiveScene()
    {
        ApplyForSceneName(SceneManager.GetActiveScene().name);
    }

    private void ApplyForSceneName(string sceneName)
    {
        if (_audioSource == null) return;

        if (!IsStageBgmScene(sceneName))
        {
            StopBgmDuckCoroutine();
            _audioSource.volume = _baseBgmVolume;
            _audioSource.Stop();
            _audioSource.clip = null;
            return;
        }

        // 스테이지 진입 직후(설명 팝업 표시 중)에는 재생하지 않음 — Timer.StartTimer 시점에 NotifyStageGameplayStarted
        StopBgmDuckCoroutine();
        _audioSource.volume = _baseBgmVolume;
        _audioSource.Stop();
        _audioSource.clip = null;
    }

    /// <summary>
    /// 타이머가 처음 시작될 때 호출합니다. (<see cref="Timer.StartTimer"/>에서 연결)
    /// </summary>
    public static void NotifyStageGameplayStarted()
    {
        if (_instance == null) return;
        _instance.PlayStageBgm();
    }

    /// <summary>
    /// go.wav 재생 직후에 호출됩니다. 이어지는 BGM 시작 시 일정 시간 BGM을 낮춰 효과음이 묻히지 않게 합니다.
    /// </summary>
    public static void RegisterPendingGoOverlap(float durationSeconds)
    {
        if (durationSeconds > 0f)
            _pendingGoOverlapSeconds = durationSeconds;
    }

    private void StopBgmDuckCoroutine()
    {
        if (_bgmDuckCoroutine == null) return;
        StopCoroutine(_bgmDuckCoroutine);
        _bgmDuckCoroutine = null;
    }

    private IEnumerator RestoreBgmVolumeAfterGo(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        if (_audioSource != null)
            _audioSource.volume = _baseBgmVolume;
        _bgmDuckCoroutine = null;
    }

    private void PlayStageBgm()
    {
        if (_audioSource == null) return;
        string sceneName = SceneManager.GetActiveScene().name;
        if (!IsStageBgmScene(sceneName)) return;

        // 매핑된 BGM이 없으면 재생 생략 (혹은 기본 BGM 설정 가능)
        if (!_sceneBgmMapping.TryGetValue(sceneName, out string bgmName))
        {
            Debug.LogWarning($"[StageBGM] '{sceneName}'에 대한 BGM 매핑이 없습니다.");
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(ResourcesBgmFolder + "/" + bgmName);
        if (clip == null)
        {
            Debug.LogWarning($"[StageBGM] Resources/{ResourcesBgmFolder}/{bgmName} 파일을 찾지 못했습니다.");
            return;
        }

        float duckSeconds = _pendingGoOverlapSeconds;
        _pendingGoOverlapSeconds = 0f;

        StopBgmDuckCoroutine();
        _audioSource.Stop();
        _audioSource.clip = clip;

        if (duckSeconds > 0f)
        {
            _audioSource.volume = _baseBgmVolume * BgmVolumeWhileGoSfx;
            _audioSource.Play();
            _bgmDuckCoroutine = StartCoroutine(RestoreBgmVolumeAfterGo(duckSeconds));
        }
        else
        {
            _audioSource.volume = _baseBgmVolume;
            _audioSource.Play();
        }
    }
}
