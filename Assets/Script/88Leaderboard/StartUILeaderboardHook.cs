using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 00StartUI 로드 시 "랭킹" 버튼을 동으로 추가하고 00Leaderboard 씬으로 연결합니다.
/// </summary>
public class StartUILeaderboardHook : MonoBehaviour
{
    private const string LeaderboardSceneName = "00Leaderboard";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        if (SceneManager.GetActiveScene().name != "00StartUI") return;
        var host = new GameObject(nameof(StartUILeaderboardHook));
        host.AddComponent<StartUILeaderboardHook>();
    }

    private void Start()
    {
        var panel = GameObject.Find("StartUiPanel");
        if (panel == null)
        {
            Destroy(gameObject);
            return;
        }

        if (panel.transform.Find("Button_Leaderboard") != null)
        {
            Destroy(gameObject);
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var row = new GameObject("Button_Leaderboard");
        row.transform.SetParent(panel.transform, false);
        var rt = row.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(280f, 64f);

        var img = row.AddComponent<Image>();
        img.color = new Color(0.15f, 0.35f, 0.55f, 0.95f);

        var btn = row.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.5f, 0.75f, 1f);
        colors.pressedColor = new Color(0.1f, 0.25f, 0.4f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(() => SceneManager.LoadScene(LeaderboardSceneName, LoadSceneMode.Single));

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(row.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var label = textGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 28;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "랭킹";

        transform.SetParent(panel.transform, false);
        Destroy(gameObject);
    }
}
