using UnityEngine;
using System.Collections;

/// <summary>
/// 99StageResult 씬용. 위/아래 배경(StageResult-Background-up/down)이 중앙으로 모였다가
/// 튕기는 효과 후 만나도록 애니메이션. 이미지 크기는 건드리지 않고, 위치만 애니메이션.
/// </summary>
public class StageResultBackgroundAnimator : MonoBehaviour
{
    [Header("Panels (씬에서 크기 설정해 둔 상태로 연결)")]
    public RectTransform panelUp;
    public RectTransform panelDown;

    [Header("Timing")]
    [Tooltip("중앙으로 접근하는 시간")]
    public float approachDuration = 0.5f;
    [Tooltip("튕겨 나가는 시간")]
    public float bounceOutDuration = 0.1f;
    [Tooltip("다시 만나기까지 시간")]
    public float meetDuration = 0.25f;

    [Header("Layout")]
    [Tooltip("튕길 때 잠시 벌어지는 거리 (픽셀)")]
    public float bounceDistance = 120f;

    [Tooltip("캔버스 기준 세로 범위 (시작 위치 계산용)")]
    public float referenceHeight = 1080f;

    [Header("Sound")]
    [Tooltip("위/아래 배경이 붙을 때 재생할 효과음 (StageResult-Background-close)")]
    public AudioClip closeSound;

    private Vector2 _startPosUp, _startPosDown;
    private Vector2 _meetPosUp, _meetPosDown;
    private Vector2 _bouncePosUp, _bouncePosDown;

    private void Start()
    {
        if (panelUp == null || panelDown == null) return;
        ComputePositions();
        panelUp.anchoredPosition = _startPosUp;
        panelDown.anchoredPosition = _startPosDown;
        StartCoroutine(Animate());
    }

    private void ComputePositions()
    {
        float hUp = panelUp.rect.height;
        float hDown = panelDown.rect.height;
        float halfRef = referenceHeight * 0.5f;
        float margin = 200f;

        _meetPosUp = new Vector2(0, hUp * 0.5f);
        _meetPosDown = new Vector2(0, -hDown * 0.5f);

        _bouncePosUp = new Vector2(0, _meetPosUp.y + bounceDistance);
        _bouncePosDown = new Vector2(0, _meetPosDown.y - bounceDistance);

        _startPosUp = new Vector2(0, halfRef + hUp * 0.5f + margin);
        _startPosDown = new Vector2(0, -halfRef - hDown * 0.5f - margin);
    }

    private IEnumerator Animate()
    {
        // 1. 접근: 위/아래에서 중앙으로
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / approachDuration;
            float s = Mathf.Clamp01(t);
            panelUp.anchoredPosition = Vector2.Lerp(_startPosUp, _meetPosUp, s);
            panelDown.anchoredPosition = Vector2.Lerp(_startPosDown, _meetPosDown, s);
            yield return null;
        }
        panelUp.anchoredPosition = _meetPosUp;
        panelDown.anchoredPosition = _meetPosDown;
        // 위/아래 배경이 붙을 때 효과음 재생
        if (closeSound != null)
            AudioSource.PlayClipAtPoint(closeSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 1f);


        // 2. 튕김: 잠시 벌어졌다가
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / bounceOutDuration;
            float s = Mathf.Clamp01(t);
            panelUp.anchoredPosition = Vector2.Lerp(_meetPosUp, _bouncePosUp, s);
            panelDown.anchoredPosition = Vector2.Lerp(_meetPosDown, _bouncePosDown, s);
            yield return null;
        }
        panelUp.anchoredPosition = _bouncePosUp;
        panelDown.anchoredPosition = _bouncePosDown;

        // 3. 만남: 다시 붙어서 맞닿기
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / meetDuration;
            float s = Mathf.Clamp01(t);
            panelUp.anchoredPosition = Vector2.Lerp(_bouncePosUp, _meetPosUp, s);
            panelDown.anchoredPosition = Vector2.Lerp(_bouncePosDown, _meetPosDown, s);
            yield return null;
        }
        panelUp.anchoredPosition = _meetPosUp;
        panelDown.anchoredPosition = _meetPosDown;
    }
}
