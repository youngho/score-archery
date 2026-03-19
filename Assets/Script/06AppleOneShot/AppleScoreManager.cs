using UnityEngine;

/// <summary>
/// 06AppleOneShot 씬 전용 점수 담당.
/// - 사과가 화살에 맞으면 1회 점수 반영
/// - 실제 점수 UI/기록은 전역 `ScoreManager`를 사용
/// </summary>
[DisallowMultipleComponent]
public class AppleScoreManager : MonoBehaviour
{
    private static AppleScoreManager _instance;

    public static AppleScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<AppleScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("AppleScoreManager");
            _instance = go.AddComponent<AppleScoreManager>();
            return _instance;
        }
    }

    [Header("Scoring")]
    [Tooltip("사과 1개당 올릴 점수")]
    public int pointsPerApple = 1;

    private int _applesHit;
    public int ApplesHit => _applesHit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _applesHit = 0;
    }

    /// <summary>
    /// 사과 명중 시 점수 추가.
    /// overridePoints가 지정되면 그 값을 사용하고, 아니면 pointsPerApple을 사용.
    /// </summary>
    public void AddAppleScore(int? overridePoints = null)
    {
        int add = overridePoints.HasValue ? overridePoints.Value : pointsPerApple;
        if (add <= 0) return;

        _applesHit++;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(add);
        }
        else
        {
            Debug.LogWarning("[AppleScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}

