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

    [Header("Explosion Defaults")]
    public bool useExplosion = true;
    public Color explosionColor = new Color(1.0f, 0.25f, 0.25f, 1f);
    public float explosionDuration = 0.12f;
    public float explosionLifetime = 0.35f;
    public float explosionRadius = 0.18f;
    public int explosionParticleCount = 50;
    public float explosionSpeedMin = 2.0f;
    public float explosionSpeedMax = 7.0f;

    private AppleBehavior _appleBehavior;

    private void Start()
    {
        // AppleOneShot 전용으로 제한 (씬 이름은 프로젝트에서 사용하는 실제 이름으로 맞춤)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "06AppleOneShot")
            return;

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
        _appleBehavior.useExplosion = useExplosion;
        _appleBehavior.explosionColor = explosionColor;
        _appleBehavior.explosionDuration = explosionDuration;
        _appleBehavior.explosionLifetime = explosionLifetime;
        _appleBehavior.explosionRadius = explosionRadius;
        _appleBehavior.explosionParticleCount = explosionParticleCount;
        _appleBehavior.explosionSpeedMin = explosionSpeedMin;
        _appleBehavior.explosionSpeedMax = explosionSpeedMax;
    }

    public void NotifyAppleDestroyed(AppleBehavior apple)
    {
        // 현재는 한 번만 등장하는 원샷 구조라 별도 추적은 하지 않음.
        // 필요하면 여기에서 리스폰/연출 등을 확장할 수 있음.
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "06AppleOneShot")
            return;

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

