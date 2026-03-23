using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 씬이 로드될 때마다 Resources/bgm 폴더의 AudioClip 중 하나를 무작위로 루프 재생합니다.
/// 00StartUI, 99StageResult 씬에서는 재생하지 않습니다.
/// </summary>
public class StageRandomBGMController : MonoBehaviour
{
    private const string ResourcesBgmFolder = "bgm";

    /// <summary>메뉴·결과 씬에서는 스테이지 BGM을 켜지 않음 (결과 씬은 StageResultBGMPlayer 사용)</summary>
    private static bool IsStageBgmScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        return sceneName != "00StartUI" && sceneName != "99StageResult";
    }

    private static StageRandomBGMController _instance;

    private AudioSource _audioSource;
    private AudioClip[] _clips;

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

        _clips = Resources.LoadAll<AudioClip>(ResourcesBgmFolder);
        if (_clips == null || _clips.Length == 0)
            Debug.LogWarning($"[StageRandomBGM] Resources/{ResourcesBgmFolder} 에서 AudioClip을 찾지 못했습니다.");
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
            _audioSource.Stop();
            _audioSource.clip = null;
            return;
        }

        if (_clips == null || _clips.Length == 0)
            return;

        int idx = Random.Range(0, _clips.Length);
        AudioClip clip = _clips[idx];
        if (clip == null)
            return;

        _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.Play();
    }
}
