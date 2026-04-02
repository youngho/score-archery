using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

/// <summary>
/// 06AppleOneShot 씬 전용:
/// - 씬 내 사과 오브젝트에 AppleBehavior를 붙이고 기본 설정을 적용
/// - AppleScoreManager/ScoreManager와 연동해 점수 처리를 담당
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-1000)]
public class AppleManager : MonoBehaviour
{
    [Header("Apple Target")]
    [Tooltip("사과 타겟 오브젝트 (비워두면 이름으로 자동 탐색: apple / Apple)")]
    public GameObject appleTarget;

    [Header("Scoring")]
    [Tooltip("사과 1개당 기본 점수")]
    public int pointsPerApple = 1;

    [Header("Apple hit — screen VFX")]
    [Tooltip("사과 명중 시 켤 화면용 VFX 루트(씬에 미리 두고 비활성화). ParticleSystem 등 자식 포함.")]
    public GameObject appleHitScreenVfx;

    [Header("Apple hit — audio")]
    [Tooltip("사과 명중 시 재생 (예: AppleHit.wav)")]
    public AudioClip appleHitClip;

    [Range(0f, 1f)]
    [Tooltip("사과 명중 효과음 볼륨")]
    public float appleHitVolume = 1f;

    [Tooltip("한 발 발사 후 이 시간(초) 뒤 Timer 종료와 동일한 흐름으로 스테이지 완료(명중 여부 무관, startSeconds=0 무제한 씬용)")]
    [FormerlySerializedAs("delayAfterHitBeforeStageEnd")]
    public float delayAfterShotBeforeStageEnd = 3f;

    private AppleBehavior _appleBehavior;
    private bool _appleHitHandled;
    private bool _postShotEndFlowStarted;
    private Coroutine _stageEndAfterShotRoutine;

    /// <summary>비활성 UI 아래 Timer 등: 활성 씬에 속한 Timer만 사용.</summary>
    private static Timer FindTimerInActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        var timers = Object.FindObjectsByType<Timer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in timers)
        {
            if (t != null && t.gameObject.scene == scene)
                return t;
        }
        return null;
    }

    private void Start()
    {
        // AppleOneShot 전용으로 제한 (씬 이름은 프로젝트에서 사용하는 실제 이름으로 맞춤)
        if (SceneManager.GetActiveScene().name != AppleOneShotStageRules.SceneName)
            return;

        AppleOneShotStageRules.Reset();
        _appleHitHandled = false;
        _postShotEndFlowStarted = false;
        if (_stageEndAfterShotRoutine != null)
        {
            StopCoroutine(_stageEndAfterShotRoutine);
            _stageEndAfterShotRoutine = null;
        }

        EnsureAppleSetup();

        // 점수 매니저 미리 생성해 두기
        _ = AppleScoreManager.Instance;
    }

    private void EnsureAppleSetup()
    {
        if (appleTarget == null)
        {
            appleTarget = GameObject.Find("apple") ?? GameObject.Find("Apple");
        }

        if (appleTarget == null)
        {
            Debug.LogWarning("[AppleManager] appleTarget을 찾을 수 없습니다. 씬에서 사과 오브젝트를 지정하거나 이름을 apple로 설정해주세요.");
            return;
        }

        _appleBehavior = appleTarget.GetComponent<AppleBehavior>();
        if (_appleBehavior == null)
        {
            _appleBehavior = appleTarget.AddComponent<AppleBehavior>();
        }

        _appleBehavior.SetOwner(this);
        _appleBehavior.points = pointsPerApple;

        var appleRb = appleTarget.GetComponent<Rigidbody>();
        if (appleRb == null)
            appleRb = appleTarget.AddComponent<Rigidbody>();
        appleRb.useGravity = true;
        appleRb.isKinematic = false;
    }

    public void NotifyAppleDestroyed(AppleBehavior apple)
    {
        // 사과 오브젝트가 파괴될 때만 호출됨 (씬 종료 등). 맞춤만으로는 파괴되지 않음.
    }

    /// <summary>ArcheryGestureManager → AppleOneShotStageRules 에서 한 발 발사 직후 호출. 타이머 일시정지 + 지연 후 종료.</summary>
    public static void NotifySingleArrowFiredInThisScene()
    {
        if (SceneManager.GetActiveScene().name != AppleOneShotStageRules.SceneName)
            return;

        var mgr = Object.FindFirstObjectByType<AppleManager>(FindObjectsInactive.Include);
        if (mgr != null)
            mgr.OnSingleArrowFired();
    }

    private void OnSingleArrowFired()
    {
        if (_postShotEndFlowStarted) return;
        _postShotEndFlowStarted = true;

        var timer = FindTimerInActiveScene();
        if (timer != null)
            timer.PauseCountdown();

        if (_stageEndAfterShotRoutine != null)
            StopCoroutine(_stageEndAfterShotRoutine);
        _stageEndAfterShotRoutine = StartCoroutine(StageEndAfterSingleShot());
    }

    /// <summary>사과 명중 시에만 화면 VFX. 스테이지 종료 타이밍은 발사 시점 기준(<see cref="OnSingleArrowFired"/>)과 동일.</summary>
    public void NotifyAppleHit(AppleBehavior apple)
    {
        if (_appleHitHandled) return;
        _appleHitHandled = true;

        if (appleHitClip != null)
        {
            Vector3 pos = apple != null ? apple.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(appleHitClip, pos, appleHitVolume);
        }

        if (appleHitScreenVfx != null)
        {
            appleHitScreenVfx.SetActive(true);
            foreach (var ps in appleHitScreenVfx.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play();
        }
    }

    private IEnumerator StageEndAfterSingleShot()
    {
        float wait = Mathf.Max(0f, delayAfterShotBeforeStageEnd);
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        // 발사 직후 캡처한 참조보다 종료 직전 재탐색이 안전(비활성·로딩 타이밍)
        var timer = FindTimerInActiveScene();
        if (timer != null)
            timer.FinishStageLikeTimerEnd();
        else
            Debug.LogError("[AppleManager] 활성 씬에서 Timer를 찾지 못했습니다. 스테이지 종료를 건너뜁니다. DialCountDown 등에 Timer가 있는지 확인하세요.");

        _stageEndAfterShotRoutine = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != AppleOneShotStageRules.SceneName)
            return;

        AppleOneShotStageRules.Reset();

        var mgr = Object.FindFirstObjectByType<AppleManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            var go = new GameObject("AppleManager");
            mgr = go.AddComponent<AppleManager>();
        }

        mgr.EnsureAppleSetup();
        _ = AppleScoreManager.Instance;
    }
}
