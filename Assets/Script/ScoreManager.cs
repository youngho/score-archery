using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI Settings")]
    public TextMeshProUGUI scoreText;

    private int _currentScore = 0;
    private int _totalArrowsShot = 0;
    private int _totalHits = 0;

    /// <summary>
    /// 현재 점수 (스테이지 종료 시 API 기록용)
    /// </summary>
    public int CurrentScore => _currentScore;

    /// <summary>
    /// 총 발사한 화살 수
    /// </summary>
    public int TotalArrowsShot => _totalArrowsShot;

    /// <summary>
    /// 총 명중 횟수
    /// </summary>
    public int TotalHits => _totalHits;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    public void AddScore(int points)
    {
        _currentScore += points;
        _totalHits++; // 점수가 오를 때 명중 획수도 증가 (보통 1:1)
        UpdateScoreUI();
        Debug.Log($"Score updated: {_currentScore}, TotalHits: {_totalHits}");
    }

    public void OnArrowShot()
    {
        _totalArrowsShot++;
        Debug.Log($"Arrow shot: {_totalArrowsShot}");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{_currentScore}";
        }
    }
}
