using UnityEngine;

/// <summary>
/// Coconut 씬 전용 점수 담당.
/// - 코코넛이 화살에 맞으면 1회 점수 반영
/// - 실제 점수 UI/기록은 전역 `ScoreManager`를 사용
/// </summary>
[DisallowMultipleComponent]
public class CoconutScoreManager : MonoBehaviour
{
    private static CoconutScoreManager _instance;

    public static CoconutScoreManager Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindObjectOfType<CoconutScoreManager>();
            if (_instance != null) return _instance;

            var go = new GameObject("CoconutScoreManager");
            _instance = go.AddComponent<CoconutScoreManager>();
            return _instance;
        }
    }

    [Header("Scoring")]
    [Tooltip("코코넛 1개당 올릴 점수")]
    public int pointsPerCoconut = 1;

    private int _coconutsHit;
    public int CoconutsHit => _coconutsHit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _coconutsHit = 0;
    }

    public void AddCoconutScore(int? overridePoints = null)
    {
        int add = overridePoints.HasValue ? overridePoints.Value : pointsPerCoconut;
        if (add <= 0) return;

        _coconutsHit++;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(add);
        }
        else
        {
            Debug.LogWarning("[CoconutScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}

