using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI Settings")]
    public TextMeshProUGUI scoreText;

    private int _currentScore = 0;

    /// <summary>
    /// 현재 점수 (스테이지 종료 시 API 기록용)
    /// </summary>
    public int CurrentScore => _currentScore;

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
        UpdateScoreUI();
        Debug.Log($"Score updated: {_currentScore}");
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore}";
        }
    }
}
