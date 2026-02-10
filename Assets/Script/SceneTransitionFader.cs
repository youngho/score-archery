using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 시 화면이 점점 어두어지는 트랜지션을 제공.
/// </summary>
public class SceneTransitionFader : MonoBehaviour
{
    private static SceneTransitionFader _instance;
    private Canvas _canvas;
    private Image _overlay;

    [Tooltip("페이드 duration (초)")]
    public float fadeDuration = 0.6f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 32767;
        _canvas.overrideSorting = true;
        gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var overlayGo = new GameObject("FadeOverlay");
        overlayGo.transform.SetParent(transform, false);

        _overlay = overlayGo.AddComponent<Image>();
        _overlay.color = new Color(0, 0, 0, 0);
        _overlay.raycastTarget = false;

        var rect = overlayGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 화면을 어둡게 만든 뒤 지정 씬 로드
    /// </summary>
    public static void FadeToBlackAndLoad(int score, string sceneToLoad, string returnSceneName, float duration = 0.6f)
    {
        if (_instance != null)
        {
            _instance.StartCoroutine(_instance.FadeAndLoadCoroutine(score, sceneToLoad, returnSceneName, duration));
            return;
        }

        var go = new GameObject("SceneTransitionFader");
        var fader = go.AddComponent<SceneTransitionFader>();
        fader.fadeDuration = duration;
        fader.StartCoroutine(fader.FadeAndLoadCoroutine(score, sceneToLoad, returnSceneName, duration));
    }

    private IEnumerator FadeAndLoadCoroutine(int score, string sceneToLoad, string returnSceneName, float duration)
    {
        if (_overlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _overlay.color = new Color(0, 0, 0, t);
            yield return null;
        }

        _overlay.color = new Color(0, 0, 0, 1);

        StageResultData.LastScore = score;
        StageResultData.ReturnSceneName = returnSceneName;
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
