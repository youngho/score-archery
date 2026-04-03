using UnityEngine;

/// <summary>
/// 09Watermelon 씬 전용 점수 담당.
/// - 수박이 화살에 맞으면 1회 점수 반영
/// - 실제 점수 UI/기록은 전역 `ScoreManager`를 사용
/// </summary>
[DisallowMultipleComponent]
public class WatermelonScoreManager : MonoBehaviour
{
    private static WatermelonScoreManager _instance;

    public static WatermelonScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = Object.FindFirstObjectByType<WatermelonScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("WatermelonScoreManager");
            _instance = go.AddComponent<WatermelonScoreManager>();
            return _instance;
        }
    }

    [Header("Scoring")]
    [Tooltip("수박 1개당 올릴 점수")]
    public int pointsPerWatermelon = 1;

    private int _watermelonsHit;
    public int WatermelonsHit => _watermelonsHit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _watermelonsHit = 0;
    }

    public void AddWatermelonScore(int? overridePoints = null)
    {
        int add = overridePoints.HasValue ? overridePoints.Value : pointsPerWatermelon;
        if (add <= 0) return;

        _watermelonsHit++;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(add);
        }
        else
        {
            Debug.LogWarning("[WatermelonScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}

