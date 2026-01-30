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

    private float _remainingSeconds;
    private TextMeshProUGUI _timerText;
    private GameObject _timerCanvasRoot;

    private void Start()
    {
        _remainingSeconds = startSeconds;
        CreateTimerUI();
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
            LoadMenuScene();
            return;
        }

        UpdateDisplay();
    }

    private void CreateTimerUI()
    {
        _timerCanvasRoot = new GameObject("BalloonTimerCanvas");
        var canvas = _timerCanvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        _timerCanvasRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
        _timerCanvasRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var textGo = new GameObject("TimerText");
        textGo.transform.SetParent(_timerCanvasRoot.transform, false);

        _timerText = textGo.AddComponent<TextMeshProUGUI>();
        var textRt = _timerText.rectTransform;
        textRt.anchorMin = new Vector2(0.5f, 1f);
        textRt.anchorMax = new Vector2(0.5f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = new Vector2(0f, -40f);
        textRt.sizeDelta = new Vector2(200f, 80f);
        _timerText.fontSize = 64f;
        _timerText.alignment = TextAlignmentOptions.Center;
        _timerText.color = Color.white;
    }

    private void UpdateDisplay()
    {
        if (_timerText != null)
            _timerText.text = Mathf.CeilToInt(_remainingSeconds).ToString();
    }

    private void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName)) return;

        if (_timerCanvasRoot != null)
            Destroy(_timerCanvasRoot);

        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (_timerCanvasRoot != null)
            Destroy(_timerCanvasRoot);
    }
}
