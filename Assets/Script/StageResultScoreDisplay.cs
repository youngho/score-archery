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
        // UI 조작을 위한 EventSystem 확인 및 생성
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // 새로운 Input System 패키지가 있는지 확인하여 적절한 모듈 추가
            System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystem.AddComponent(inputModuleType);
            }
            else
            {
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }

    /// <summary>
    /// Canvas가 활성화된 후 호출. 점수/발사수/명중률 표시 및 버튼 이벤트 등록.
    /// StageResultBackgroundAnimator에서 canvasToReveal 활성화 후 호출됨.
    /// </summary>
    public void SetupDisplay()
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
            arrowCountText.text = $"Shooting : {StageResultData.TotalArrowsShot}";

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
            accuracyText.text = $"Accuracy : {accuracy:F1}%";
        }

        if (continueButton == null)
            continueButton = GameObject.Find("ContinueButton")?.GetComponent<Button>();

        if (continueButton != null)
            continueButton.onClick.AddListener(LoadReturnScene);
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
