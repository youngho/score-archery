using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// AppSceneManager handles scene transitions and UI button assignments for stage selection.
/// </summary>
public class AppSceneManager : MonoBehaviour
{
    private void Awake()
    {
        // Find the container that holds all stage buttons
        GameObject container = GameObject.Find("StartUiPanel/StageButtonsContainer");
        if (container != null)
        {
            // For each child under the container, get its name and wire up a button
            foreach (Transform child in container.transform)
            {
                RegisterButton(child);
            }
        }
        else
        {
            Debug.LogWarning("StageButtonsContainer not found under StartUiPanel.");
        }

        // Specifically find Button_Leaderboard if it's outside the container
        GameObject leaderboardBtnGo = GameObject.Find("StartUiPanel/Button_Leaderboard");
        if (leaderboardBtnGo == null)
        {
            // Fallback for different hierarchy paths
            leaderboardBtnGo = GameObject.Find("Button_Leaderboard");
        }

        if (leaderboardBtnGo != null)
        {
             RegisterButton(leaderboardBtnGo.transform);
        }
    }

    private void RegisterButton(Transform child)
    {
        Button button = child.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        string sceneName = GetSceneNameFromButton(child.name);
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        // Capture local variable for closure
        string capturedSceneName = sceneName;
        button.onClick.AddListener(() => LoadStageScene(capturedSceneName));
    }

    /// <summary>
    /// Maps a button GameObject name to the corresponding stage scene name.
    /// By default, uses the button name itself.
    /// </summary>
    private string GetSceneNameFromButton(string buttonObjectName)
    {
        if (buttonObjectName == "Button_Leaderboard")
        {
            return "88Leaderboard";
        }
        // 01Scarecrow ~ 12Snowman 은 버튼 오브젝트 이름과 씬 이름이 동일하다고 가정
        return buttonObjectName;
    }

    /// <summary>
    /// 요청한 스테이지 씬을 단일 모드로 로드한다.
    /// (현재 씬은 언로드되고, DontDestroyOnLoad 객체만 유지됨)
    /// </summary>
    private void LoadStageScene(string stageSceneName)
    {
        SceneManager.LoadScene(stageSceneName, LoadSceneMode.Single);
    }
}
