using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 00StartUI에서 미리 배치된 "랭킹" 버튼(`Button_Leaderboard`)의 클릭 이벤트를
/// 88Leaderboard 씬으로 연결합니다.
/// </summary>
public class StartUILeaderboardHook : MonoBehaviour
{
    private const string LeaderboardSceneName = "88Leaderboard";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        if (SceneManager.GetActiveScene().name != "00StartUI") return;

        // 씬에 미리 배치된 버튼만 찾아서 클릭 이벤트만 연결합니다.
        var buttonGo = GameObject.Find("Button_Leaderboard");
        if (buttonGo == null) return;

        var button = buttonGo.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
            SceneManager.LoadScene(LeaderboardSceneName, LoadSceneMode.Single)
        );
    }
}
