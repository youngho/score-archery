using UnityEngine;

/// <summary>
/// ArcheryGestureManager의 제스처 데이터를 이용해
/// 2D 선 대신 3D 화살 모양으로 조준 상태를 보여주는 시 بص주얼라이저.
/// </summary>
public class ArcheryAimVisualizer3D : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    [Tooltip("조준 미리보기 화살이 기준으로 삼을 위치/방향 (보통 활/카메라 앞)")]
    public Transform arrowSpawnPoint;

    [Tooltip("조준 미리보기에 사용할 3D 화살 프리팹 (Rigidbody 없어도 됨)")]
    public GameObject arrowPreviewPrefab;

    [Header("스케일 설정")]
    [Tooltip("제일 약하게 당겼을 때의 화살 크기 배율")]
    public float minScale = 20.5f;

    [Tooltip("최대로 당겼을 때의 화살 크기 배율")]
    public float maxScale = 200.5f;

    [Header("각도 설정")]
    [Tooltip("제스처의 수직 방향을 피치 각도로 사용할지 여부")]
    public bool useGestureAngleForPitch = true;

    [Tooltip("위/아래로 조정 가능한 최대 피치 각도")]
    public float maxPitchAngle = 45f;

    private ArcheryGestureManager gestureManager;
    private GameObject previewInstance;
    private Transform previewTransform;
    private Vector3 baseLocalScale = Vector3.one;

    private void OnEnable()
    {
        gestureManager = ArcheryGestureManager.Instance;

        gestureManager.OnDrawStart.AddListener(OnDrawStart);
        gestureManager.OnDrawing.AddListener(OnDrawing);
        gestureManager.OnDrawEnd.AddListener(OnDrawEnd);
        gestureManager.OnRelease.AddListener(OnRelease);
        gestureManager.OnAimAdjust.AddListener(OnAimAdjust);
        gestureManager.OnCancel.AddListener(OnCancel);
    }

    private void OnDisable()
    {
        if (gestureManager == null) return;

        gestureManager.OnDrawStart.RemoveListener(OnDrawStart);
        gestureManager.OnDrawing.RemoveListener(OnDrawing);
        gestureManager.OnDrawEnd.RemoveListener(OnDrawEnd);
        gestureManager.OnRelease.RemoveListener(OnRelease);
        gestureManager.OnAimAdjust.RemoveListener(OnAimAdjust);
        gestureManager.OnCancel.RemoveListener(OnCancel);
    }

    #region Event Handlers
    private void OnDrawStart(ArcheryGestureManager.GestureData data)
    {
        EnsurePreviewInstance();
        if (previewInstance == null) return;

        previewInstance.SetActive(true);

        if (arrowSpawnPoint != null)
        {
            previewTransform.position = arrowSpawnPoint.position;
            previewTransform.rotation = arrowSpawnPoint.rotation;
        }
    }

    private void OnDrawing(ArcheryGestureManager.GestureData data)
    {
        if (previewInstance == null || arrowSpawnPoint == null) return;

        // 발사 방향 계산 (ArcheryShooter와 동일한 방식으로 맞춰줌)
        Vector3 dir = arrowSpawnPoint.forward;

        if (useGestureAngleForPitch)
        {
            float pitch = Mathf.Clamp(-data.direction.y * maxPitchAngle, -maxPitchAngle, maxPitchAngle);
            dir = Quaternion.Euler(pitch, 0f, 0f) * dir;
        }

        previewTransform.position = arrowSpawnPoint.position;
        previewTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 파워에 따라 크기 조절
        float t = Mathf.Clamp01(data.normalizedPower);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        previewTransform.localScale = baseLocalScale * scale;
    }

    private void OnDrawEnd(ArcheryGestureManager.GestureData data)
    {
        HidePreview();
    }

    private void OnRelease(ArcheryGestureManager.GestureData data)
    {
        // 실제 발사는 다른 스크립트(ArcheryShooter)가 담당
        // 여기서는 시 بص주얼만, OnDrawEnd에서 숨김 처리
    }

    private void OnAimAdjust(ArcheryGestureManager.GestureData data)
    {
        // 필요하다면 두 손가락 조준 오프셋(data.aimOffset)을 이용해
        // 좌우/상하 미세 조정 비주얼을 추가할 수 있음.
    }

    private void OnCancel()
    {
        HidePreview();
    }
    #endregion

    #region Helpers
    private void EnsurePreviewInstance()
    {
        if (previewInstance != null) return;
        if (arrowPreviewPrefab == null)
        {
            Debug.LogWarning("[ArcheryAimVisualizer3D] arrowPreviewPrefab 이 설정되어 있지 않습니다.");
            return;
        }

        previewInstance = Instantiate(arrowPreviewPrefab);
        previewTransform = previewInstance.transform;
        baseLocalScale = previewTransform.localScale;
        previewInstance.SetActive(false);
    }

    private void HidePreview()
    {
        if (previewInstance != null)
        {
            previewInstance.SetActive(false);
        }
    }
    #endregion
}


