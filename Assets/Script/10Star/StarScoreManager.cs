using UnityEngine;

/// <summary>
/// 10Star 씬 전용 점수 담당.
/// - 별이 화살에 맞으면 1회 점수 반영
/// - 실제 점수 UI/기록은 전역 `ScoreManager`를 사용
/// </summary>
[DisallowMultipleComponent]
public class StarScoreManager : MonoBehaviour
{
    private static StarScoreManager _instance;

    public static StarScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<StarScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("StarScoreManager");
            _instance = go.AddComponent<StarScoreManager>();
            return _instance;
        }
    }

    [Header("Scoring")]
    [Tooltip("별 1개당 올릴 점수")]
    public int pointsPerStar = 1;

    private int _starsHit;
    public int StarsHit => _starsHit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _starsHit = 0;
    }

    public void AddStarScore()
    {
        if (pointsPerStar <= 0) return;

        _starsHit++;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointsPerStar);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}

