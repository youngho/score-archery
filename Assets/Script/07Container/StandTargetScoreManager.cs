using UnityEngine;

/// <summary>
/// 07Container 씬 전용 스탠드 표적 점수 매니저.
/// - StandTargetBehavior에서 서클링에 따라 계산된 점수를 받아서 전역 ScoreManager에 반영
/// - 링별 히트 카운트 통계를 옵션으로 보관
/// </summary>
[DisallowMultipleComponent]
public class StandTargetScoreManager : MonoBehaviour
{
    private static StandTargetScoreManager _instance;

    public static StandTargetScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Object.FindFirstObjectByType<StandTargetScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("StandTargetScoreManager");
            _instance = go.AddComponent<StandTargetScoreManager>();
            return _instance;
        }
    }

    [Header("Debug / Stats")]
    [Tooltip("링 이름별 히트 수 (옵션)")]
    public int innerHits;
    public int middleHits;
    public int outerHits;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        ResetStats();
    }

    public void AddStandTargetScore(int points, string ringName = null)
    {
        if (points <= 0)
            return;

        // 기본 점수 시스템에 반영
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(points);
        }
        else
        {
            Debug.LogWarning("[StandTargetScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
        }

        // 간단한 링별 통계 (ringName이 null/빈 문자열이면 무시)
        if (!string.IsNullOrEmpty(ringName))
        {
            string lower = ringName.ToLowerInvariant();
            if (lower.Contains("inner"))
                innerHits++;
            else if (lower.Contains("middle"))
                middleHits++;
            else if (lower.Contains("outer"))
                outerHits++;
        }
    }

    public void ResetStats()
    {
        innerHits = 0;
        middleHits = 0;
        outerHits = 0;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}

