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
            BuildUi();

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

    private void BuildUi()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var rootRt = canvasGo.GetComponent<RectTransform>();

        var bg = CreatePanel(rootRt, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 1f));
        bg.GetComponent<Image>().raycastTarget = false;

        CreateTopBar(rootRt);

        var scrollRt = CreatePanel(rootRt, "Scroll", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(880f, 740f), new Color(0.12f, 0.14f, 0.18f, 0.95f));
        var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        var viewportRt = CreatePanel(scrollRt, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.01f));
        viewportRt.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        viewportRt.GetComponent<Image>().raycastTarget = true;
        scroll.viewport = viewportRt;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportRt, false);
        _listContent = contentGo.AddComponent<RectTransform>();
        _listContent.anchorMin = new Vector2(0f, 1f);
        _listContent.anchorMax = new Vector2(1f, 1f);
        _listContent.pivot = new Vector2(0.5f, 1f);
        _listContent.anchoredPosition = Vector2.zero;
        _listContent.sizeDelta = new Vector2(0f, 0f);
        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(12, 12, 8, 12);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = _listContent;
    }

#if UNITY_EDITOR
    [ContextMenu("Bake UI Into Scene (Creates Hierarchy Objects)")]
    private void BakeUiIntoScene()
    {
        if (TryBindExistingUi())
            return;

        EnsureEventSystem();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
        WireUiEvents();

        var scene = gameObject.scene;
        if (scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }
#endif

    private void CreateTopBar(RectTransform root)
    {
        var bar = CreatePanel(root, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(920f, 120f), new Color(0.15f, 0.17f, 0.22f, 1f));

        CreateButton(bar, "Back", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100f, -34f), new Vector2(160f, 48f), "뒤로", () =>
            SceneManager.LoadScene(StartSceneName, LoadSceneMode.Single));

        _statusText = CreateText(bar, "Status", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(520f, 48f), 24, TextAnchor.MiddleCenter, "불러오는 중…");

        _totalText = CreateText(bar, "Total", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -34f), new Vector2(220f, 48f), 20, TextAnchor.MiddleRight, "");

        var periodRow = new GameObject("PeriodRow");
        periodRow.transform.SetParent(bar, false);
        var prt = periodRow.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 8f);
        prt.sizeDelta = new Vector2(-32f, 44f);
        var h = periodRow.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 10f;
        h.childControlWidth = true;
        h.childForceExpandWidth = true;
        h.padding = new RectOffset(4, 4, 0, 0);

        string[] labels = { "전체", "월간", "주간", "일간" };
        string[] keys = { "alltime", "monthly", "weekly", "daily" };
        for (int i = 0; i < labels.Length; i++)
        {
            string key = keys[i];
            var go = new GameObject($"Period_{key}");
            go.transform.SetParent(periodRow.transform, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.minHeight = 40f;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.5f, 0.75f, 1f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                _period = key;
                StartCoroutine(LoadAndRender());
            });
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tx = textGo.AddComponent<Text>();
            tx.font = _font;
            tx.fontSize = 20;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.text = labels[i];
        }
    }

    private IEnumerator LoadAndRender()
    {
        _statusText.text = "불러오는 중…";
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
                _statusText.text = $"오류: {req.error}";
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
                _statusText.text = $"응답 파싱 실패: {ex.Message}";
                ClearRows();
                yield break;
            }

            if (dto.entries == null)
                dto.entries = Array.Empty<LeaderboardEntryDto>();

            _statusText.text = $"{PeriodLabel(_period)} 리더보드";
            _totalText.text = dto.total > 0 ? $"등록 {dto.total}명" : "";

            RenderRows(dto.entries);
        }
    }

    private static string PeriodLabel(string key)
    {
        return key switch
        {
            "monthly" => "월간",
            "weekly" => "주간",
            "daily" => "일간",
            _ => "전체"
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

        string myName = UserAccountManagerScript.Instance != null
            ? UserAccountManagerScript.Instance.Nickname
            : PlayerPrefs.GetString("UserAccountNickname", "");

        var header = CreateRow(_listContent, "#", "플레이어", "점수", new Color(0.25f, 0.28f, 0.34f, 1f), true);
        header.GetComponent<LayoutElement>().minHeight = 44f;

        if (entries.Length == 0)
        {
            var empty = CreateRow(_listContent, "-", "아직 기록이 없습니다.", "-", new Color(0.18f, 0.2f, 0.24f, 1f), false);
            empty.GetComponent<LayoutElement>().minHeight = 56f;
            return;
        }

        foreach (var e in entries)
        {
            bool mine = !string.IsNullOrEmpty(myName) && string.Equals(e.playerName, myName, StringComparison.Ordinal);
            Color rowBg = mine ? new Color(0.22f, 0.32f, 0.2f, 1f) : new Color(0.18f, 0.2f, 0.24f, 1f);
            var row = CreateRow(_listContent, e.rank.ToString(), e.playerName ?? "?", e.score.ToString("N0"), rowBg, false);
            row.GetComponent<LayoutElement>().minHeight = 48f;
        }
    }

    private GameObject CreateRow(RectTransform parent, string a, string b, string c, Color bg, bool header)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        var rt = row.AddComponent<RectTransform>();
        var img = row.AddComponent<Image>();
        img.color = bg;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.padding = new RectOffset(10, 10, 4, 4);
        hlg.spacing = 8f;
        var le = row.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        float[] weights = { 0.12f, 0.55f, 0.33f };
        AddCell(row.transform, a, weights[0], header ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleCenter);
        AddCell(row.transform, b, weights[1], header ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleLeft);
        AddCell(row.transform, c, weights[2], header ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleRight);
        return row;
    }

    private void AddCell(Transform row, string text, float weight, FontStyle style, TextAnchor align)
    {
        var cell = new GameObject("Cell");
        cell.transform.SetParent(row, false);
        var le = cell.AddComponent<LayoutElement>();
        le.flexibleWidth = weight;
        le.minHeight = 40f;
        var t = cell.AddComponent<Text>();
        t.font = _font;
        t.fontSize = 22;
        t.fontStyle = style;
        t.color = Color.white;
        t.alignment = align;
        t.text = text;
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private Text CreateText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, string msg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.AddComponent<Text>();
        t.font = _font;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = Color.white;
        t.text = msg;
        return t;
    }

    private Button CreateButton(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, string label, Action onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.45f, 0.7f, 1f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tx = textGo.AddComponent<Text>();
        tx.font = _font;
        tx.fontSize = 22;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = Color.white;
        tx.text = label;
        return btn;
    }
}
