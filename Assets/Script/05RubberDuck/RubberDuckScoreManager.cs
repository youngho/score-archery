using UnityEngine;

/// <summary>
/// 05RubberDuck 씬 전용 점수 담당.
/// - 오리가 화살에 맞으면 1회 점수 반영
/// - 실제 점수 UI/기록은 전역 `ScoreManager`를 사용
/// </summary>
[DisallowMultipleComponent]
public class RubberDuckScoreManager : MonoBehaviour
{
    private static RubberDuckScoreManager _instance;

    public static RubberDuckScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Object.FindFirstObjectByType<RubberDuckScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("RubberDuckScoreManager");
            _instance = go.AddComponent<RubberDuckScoreManager>();
            return _instance;
        }
    }

    [Header("Scoring")]
    [Tooltip("오리 1개당 올릴 점수")]
    public int pointsPerDuck = 1;

    private int _ducksHit;
    public int DucksHit => _ducksHit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _ducksHit = 0;
    }

    /// <summary>
    /// 오리 명중 시 점수 추가.
    /// overridePoints가 지정되면 그 값을 사용하고, 아니면 pointsPerDuck을 사용.
    /// </summary>
    public void AddRubberDuckScore(int? overridePoints = null)
    {
        int add = overridePoints.HasValue ? overridePoints.Value : pointsPerDuck;
        if (add <= 0) return;

        _ducksHit++;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(add);
        }
        else
        {
            Debug.LogWarning("[RubberDuckScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
