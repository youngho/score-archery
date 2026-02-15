using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 99StageResult 씬 전용. 점수 표시 및 확인 후 ReturnSceneName 씬으로 이동.
/// </summary>
public class StageResultScoreDisplay : MonoBehaviour
{
    [Tooltip("표시할 TextMeshProUGUI (비어 있으면 'ScoreText' 이름으로 검색)")]
    public TextMeshProUGUI scoreText;

    [Tooltip("총 발사 수 표시할 텍스트 (비어 있으면 'ArrowCountText' 검색)")]
    public TextMeshProUGUI arrowCountText;

    [Tooltip("명중률 표시할 텍스트 (비어 있으면 'AccuracyText' 검색)")]
    public TextMeshProUGUI accuracyText;

    [Tooltip("확인/계속 버튼 (비어 있으면 'ContinueButton' 검색 또는 런타임 생성)")]
    public Button continueButton;

    private void Start()
    {
        // 점수 표시
        if (scoreText == null)
            scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        
        if (scoreText != null)
            scoreText.text = $"{StageResultData.LastScore}";

        // 총 발사 수 표시
        if (arrowCountText == null)
            arrowCountText = GameObject.Find("ArrowCountText")?.GetComponent<TextMeshProUGUI>();
        
        if (arrowCountText != null)
            arrowCountText.text = $"발사 수: {StageResultData.TotalArrowsShot}";

        // 명중률 계산 및 표시
        if (accuracyText == null)
            accuracyText = GameObject.Find("AccuracyText")?.GetComponent<TextMeshProUGUI>();
        
        if (accuracyText != null)
        {
            float accuracy = 0f;
            if (StageResultData.TotalArrowsShot > 0)
            {
                accuracy = (float)StageResultData.TotalHits / StageResultData.TotalArrowsShot * 100f;
            }
            accuracyText.text = $"명중률: {accuracy:F1}%";
        }

        if (continueButton == null)
            continueButton = GameObject.Find("ContinueButton")?.GetComponent<Button>();
        if (continueButton == null)
            continueButton = CreateContinueButton();

        if (continueButton != null)
            continueButton.onClick.AddListener(LoadReturnScene);
    }

    private Button CreateContinueButton()
    {
        var canvas = GameObject.Find("StageResultBackgroundCanvas");
        if (canvas == null) return null;

        var btnGo = new GameObject("ContinueButton");
        btnGo.transform.SetParent(canvas.transform, false);

        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.2f);
        rect.anchorMax = new Vector2(0.5f, 0.2f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(400, 80);

        var image = btnGo.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 1f, 0.9f);

        var btn = btnGo.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "계속";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    /// <summary>
    /// StageResultData.ReturnSceneName 씬으로 이동 (버튼 onClick에서 호출)
    /// </summary>
    public void LoadReturnScene()
    {
        var scene = string.IsNullOrEmpty(StageResultData.ReturnSceneName) ? "00StartUI" : StageResultData.ReturnSceneName;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
