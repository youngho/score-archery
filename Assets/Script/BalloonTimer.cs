using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 04Balloon 씬 전용 타이머. 9초 카운트다운 후 00StartUI로 복귀.
/// </summary>
public class BalloonTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("시작 초 (9 → 0 카운트다운)")]
    public int startSeconds = 9;

    [Tooltip("0이 되면 로드할 씬 이름 (상위 메뉴)")]
    public string menuSceneName = "00StartUI";

    [SerializeField] private TextMeshProUGUI _timerText;

    private float _remainingSeconds;

    private void Start()
    {
        _remainingSeconds = startSeconds;
        if (_timerText == null)
            _timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        UpdateDisplay();
    }

    private void Update()
    {
        if (_remainingSeconds <= 0f) return;

        _remainingSeconds -= Time.deltaTime;
        if (_remainingSeconds <= 0f)
        {
            _remainingSeconds = 0f;
            UpdateDisplay();
            RecordScoreAndLoadMenu();
            return;
        }

        UpdateDisplay();
    }

    /// <summary>
    /// API에 점수 기록 후 메뉴 씬 로드
    /// </summary>
    private void RecordScoreAndLoadMenu()
    {
        var recorder = GetComponent<StageScoreApiService>() ?? gameObject.AddComponent<StageScoreApiService>();
        recorder.RecordCurrentStageAndThen(LoadMenuScene);
    }

    private void UpdateDisplay()
    {
        if (_timerText != null)
            _timerText.text = Mathf.CeilToInt(_remainingSeconds).ToString();
    }

    private void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName)) return;
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }
}
