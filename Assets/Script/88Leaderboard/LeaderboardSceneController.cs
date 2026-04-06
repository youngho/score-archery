using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 글로벌 리더보드 씬: GET /api/leaderboard (period=alltime|monthly|weekly|daily).
/// </summary>
public class LeaderboardSceneController : MonoBehaviour
{
    private const string StartSceneName = "00StartUI";
    private const int PageLimit = 50;

    [Serializable]
    private class LeaderboardEntryDto
    {
        public int rank;
        public string playerName;
        public long score;
        public string avatar;
        public string country;
    }

    [Serializable]
    private class LeaderboardResponseDto
    {
        public LeaderboardEntryDto[] entries;
        public long total;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject rowPrefab;

    private Font _font;
    private Text _statusText;
    private Text _totalText;
    private RectTransform _listContent;
    private string _period = "alltime";

    private void Awake()
    {
        EnsureEventSystem();
        // LegacyRuntime.ttf can miss Korean glyphs depending on platform/build.
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!TryBindExistingUi())
        {
            Debug.LogWarning("[Leaderboard] Canvas or UI components not found in scene. Please bake UI or add them manually.");
        }

        EnsureUiFonts();
        WireUiEvents();
    }

    private void OnEnable()
    {
        StartCoroutine(LoadAndRender());
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private bool TryBindExistingUi()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return false;

        var statusGo = GameObject.Find("Canvas/TopBar/Status");
        var totalGo = GameObject.Find("Canvas/TopBar/Total");
        var contentGo = GameObject.Find("Canvas/Scroll/Viewport/Content");

        if (statusGo == null || totalGo == null || contentGo == null)
            return false;

        _statusText = statusGo.GetComponent<Text>();
        _totalText = totalGo.GetComponent<Text>();
        _listContent = contentGo.GetComponent<RectTransform>();

        if (_statusText == null || _totalText == null || _listContent == null)
            return false;

        return true;
    }

    private void WireUiEvents()
    {
        var backGo = GameObject.Find("Canvas/TopBar/Back");
        if (backGo != null)
        {
            var backButton = backGo.GetComponent<Button>();
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() =>
                    SceneManager.LoadScene(StartSceneName, LoadSceneMode.Single));
            }
        }

        string[] keys = { "alltime", "monthly", "weekly", "daily" };
        foreach (var key in keys)
        {
            var go = GameObject.Find($"Canvas/TopBar/PeriodRow/Period_{key}");
            if (go == null) continue;

            var btn = go.GetComponent<Button>();
            if (btn == null) continue;

            btn.onClick.RemoveAllListeners();
            string captured = key;
            btn.onClick.AddListener(() =>
            {
                _period = captured;
                StartCoroutine(LoadAndRender());
            });
        }
    }

    private void EnsureUiFonts()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        var texts = canvas.GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
            if (t == null) continue;
            if (t.font == null) t.font = _font;
        }
    }


    private IEnumerator LoadAndRender()
    {
        _statusText.text = "Loading...";
        _totalText.text = "";

        string url = $"{ScoreApiConfig.ApiBaseUrl}/leaderboard?period={Uri.EscapeDataString(_period)}&offset=1&limit={PageLimit}";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Leaderboard] GET {url}");
#endif

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Leaderboard] Request failed: {req.result} / {req.error}");
#endif
                _statusText.text = $"Error: {req.error}";
                ClearRows();
                yield break;
            }

            LeaderboardResponseDto dto;
            try
            {
                dto = JsonUtility.FromJson<LeaderboardResponseDto>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Leaderboard] Parse failed: {ex.Message}\n{req.downloadHandler.text}");
#endif
                _statusText.text = $"Parse Failed: {ex.Message}";
                ClearRows();
                yield break;
            }

            if (dto.entries == null)
                dto.entries = Array.Empty<LeaderboardEntryDto>();

            _statusText.text = $"{PeriodLabel(_period)} Leaderboard";
            _totalText.text = dto.total > 0 ? $"{dto.total} Entries" : "";

            RenderRows(dto.entries);
        }
    }

    private static string PeriodLabel(string key)
    {
        return key switch
        {
            "monthly" => "Monthly",
            "weekly" => "Weekly",
            "daily" => "Daily",
            _ => "All"
        };
    }

    private void ClearRows()
    {
        for (int i = _listContent.childCount - 1; i >= 0; i--)
            Destroy(_listContent.GetChild(i).gameObject);
    }

    private void RenderRows(LeaderboardEntryDto[] entries)
    {
        ClearRows();

        if (rowPrefab == null)
        {
            Debug.LogError("[Leaderboard] rowPrefab is not assigned!");
            _statusText.text = "System Error: Prefab missing";
            return;
        }

        string myName = UserAccountManagerScript.Instance != null
            ? UserAccountManagerScript.Instance.Nickname
            : PlayerPrefs.GetString("UserAccountNickname", "");

        // Render Header
        var header = Instantiate(rowPrefab, _listContent);
        header.name = "Header";
        SetRowData(header, "#", "Player", "Score", new Color(0.18f, 0.357f, 1f, 0.8f));

        if (entries.Length == 0)
        {
            var empty = Instantiate(rowPrefab, _listContent);
            empty.name = "Empty";
            SetRowData(empty, "-", "No records yet.", "-", new Color(0.1f, 0.12f, 0.2f, 1f));
            return;
        }

        foreach (var e in entries)
        {
            bool mine = !string.IsNullOrEmpty(myName) && string.Equals(e.playerName, myName, StringComparison.Ordinal);
            Color rowBg = mine ? new Color(0.1f, 0.45f, 0.25f, 1f) : new Color(0.12f, 0.14f, 0.24f, 1f);
            var row = Instantiate(rowPrefab, _listContent);
            row.name = $"Row_{e.rank}";
            SetRowData(row, e.rank.ToString(), e.playerName ?? "?", e.score.ToString("N0"), rowBg);
        }
    }

    private void SetRowData(GameObject row, string rank, string player, string score, Color bg)
    {
        var img = row.GetComponent<Image>();
        if (img != null) img.color = bg;

        SetText(row, "Rank", rank);
        SetText(row, "Player", player);
        SetText(row, "Score", score);
    }

    private void SetText(GameObject row, string childName, string value)
    {
        var t = row.transform.Find(childName)?.GetComponent<Text>();
        if (t != null)
        {
            t.text = value;
            if (t.font == null) t.font = _font;
        }
    }
}
