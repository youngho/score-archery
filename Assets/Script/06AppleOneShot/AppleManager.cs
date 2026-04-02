using System.Collections;
using UnityEngine;

/// <summary>
/// 06AppleOneShot 씬 전용:
/// - 씬 내 사과 오브젝트에 AppleBehavior를 붙이고 기본 설정을 적용
/// - AppleScoreManager/ScoreManager와 연동해 점수 처리를 담당
/// </summary>
[DisallowMultipleComponent]
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

    [Tooltip("사과 명중 후 이 시간(초) 뒤 Timer 종료와 동일한 흐름으로 스테이지 완료")]
    public float delayAfterHitBeforeStageEnd = 3f;

    private AppleBehavior _appleBehavior;
    private bool _appleHitHandled;
    private Coroutine _stageEndAfterHitRoutine;

    private void Start()
    {
        // AppleOneShot 전용으로 제한 (씬 이름은 프로젝트에서 사용하는 실제 이름으로 맞춤)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != AppleOneShotStageRules.SceneName)
            return;

        AppleOneShotStageRules.Reset();
        _appleHitHandled = false;
        if (_stageEndAfterHitRoutine != null)
        {
            StopCoroutine(_stageEndAfterHitRoutine);
            _stageEndAfterHitRoutine = null;
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

    /// <summary>사과에 화살이 맞았을 때: 화면 VFX, 타이머 정지, 지연 후 기존 타이머 종료와 동일하게 마무리.</summary>
    public void NotifyAppleHit(AppleBehavior apple)
    {
        if (_appleHitHandled) return;
        _appleHitHandled = true;

        if (appleHitScreenVfx != null)
        {
            appleHitScreenVfx.SetActive(true);
            foreach (var ps in appleHitScreenVfx.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play();
        }

        var timer = FindFirstObjectByType<Timer>();
        if (timer != null)
            timer.PauseCountdown();

        if (_stageEndAfterHitRoutine != null)
            StopCoroutine(_stageEndAfterHitRoutine);
        _stageEndAfterHitRoutine = StartCoroutine(StageEndAfterAppleHit(timer));
    }

    private IEnumerator StageEndAfterAppleHit(Timer timer)
    {
        float wait = Mathf.Max(0f, delayAfterHitBeforeStageEnd);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        if (timer != null)
            timer.FinishStageLikeTimerEnd();
        _stageEndAfterHitRoutine = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != AppleOneShotStageRules.SceneName)
            return;

        AppleOneShotStageRules.Reset();

        var mgr = Object.FindFirstObjectByType<AppleManager>();
        if (mgr == null)
        {
            var go = new GameObject("AppleManager");
            mgr = go.AddComponent<AppleManager>();
        }

        mgr.EnsureAppleSetup();
        _ = AppleScoreManager.Instance;
    }
}
