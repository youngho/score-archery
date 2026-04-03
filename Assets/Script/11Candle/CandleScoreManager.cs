using UnityEngine;

/// <summary>
/// 양초 스코어 담당 매니저.
/// - Candle이 실제로 쓰러져서 불이 꺼질 때 1회 점수를 올림
/// - 내부 UI는 ScoreManager를 그대로 사용 (기존 점수 UI 갱신 연동)
/// </summary>
[DisallowMultipleComponent]
public class CandleScoreManager : MonoBehaviour
{
    public static CandleScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Object.FindFirstObjectByType<CandleScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("CandleScoreManager");
            _instance = go.AddComponent<CandleScoreManager>();
            return _instance;
        }
    }

    private static CandleScoreManager _instance;

    [Header("Scoring")]
    [Tooltip("양초 1개당 올릴 점수 (기본 1점)")]
    public int pointsPerCandle = 1;

    private int _candleHits;

    public int CandleHits => _candleHits;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _candleHits = 0;
    }

    public void AddCandleScore()
    {
        _candleHits++;

        // 기존 점수 UI/로그는 ScoreManager를 활용
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsPerCandle);
        }
        else
        {
            Debug.LogWarning("[CandleScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
        }
    }
}

