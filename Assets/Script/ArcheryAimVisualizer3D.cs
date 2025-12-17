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
    public float minScale = 0.5f;

    [Tooltip("최대로 당겼을 때의 화살 크기 배율")]
    public float maxScale = 1.5f;

    [Header("각도 설정")]
    [Tooltip("제스처의 수직 방향을 피치 각도로 사용할지 여부")]
    public bool useGestureAngleForPitch = true;

    [Tooltip("제스처의 수평 방향을 요(yaw) 각도로 사용할지 여부")]
    public bool useGestureAngleForYaw = true;

    [Tooltip("위/아래로 조정 가능한 최대 피치 각도")]
    public float maxPitchAngle = 45f;

    [Tooltip("좌/우로 조정 가능한 최대 요(yaw) 각도")]
    public float maxYawAngle = 45f;

    [Header("디버그")]
    [Tooltip("조준 프리뷰의 생성/갱신 과정을 로그로 출력할지 여부")]
    public bool logDebug = false;

    private ArcheryGestureManager gestureManager;
    private GameObject previewInstance;
    private Transform previewTransform;
    private Vector3 baseLocalScale = Vector3.one;
    /// <summary>
    /// 프리팹 메쉬의 "시각적인 중심"이 로컬 피벗(Transform.position)에서 얼마나 떨어져 있는지 (로컬 좌표계 기준)
    /// </summary>
    private Vector3 previewCenterLocalOffset = Vector3.zero;
    private bool hasPreviewCenterOffset = false;

    private void OnEnable()
    {
        gestureManager = ArcheryGestureManager.Instance;

        if (logDebug)
        {
            Debug.Log("[ArcheryAimVisualizer3D] OnEnable - subscribing gesture events", this); // ARCHERY_DEBUG_LOG
        }

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

        if (logDebug)
        {
            Debug.Log("[ArcheryAimVisualizer3D] OnDisable - unsubscribing gesture events", this); // ARCHERY_DEBUG_LOG
        }

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

        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryAimVisualizer3D] OnDrawStart - startPos={data.startPosition}, baseScale={baseLocalScale}",
                this); // ARCHERY_DEBUG_LOG
        }

        if (arrowSpawnPoint != null)
        {
            // 먼저 회전을 맞춘 뒤, 메쉬 "중심"이 spawnPoint 에 오도록 위치를 보정
            previewTransform.rotation = arrowSpawnPoint.rotation;

            if (hasPreviewCenterOffset)
            {
                previewTransform.position =
                    arrowSpawnPoint.position - previewTransform.rotation * previewCenterLocalOffset;
            }
            else
            {
                previewTransform.position = arrowSpawnPoint.position;
            }
        }
    }

    private void OnDrawing(ArcheryGestureManager.GestureData data)
    {
        if (previewInstance == null || arrowSpawnPoint == null) return;

        Camera cam = Camera.main;

        // 기본 방향
        Vector3 baseDir = arrowSpawnPoint.forward;

        // 발사 방향 계산 (ArcheryShooter와 동일한 방식으로 맞춰줌)
        Vector2 dragDir = (data.startPosition - data.currentPosition).normalized;

        float pitchDeg = 0f;
        float yawDeg = 0f;

        if (useGestureAngleForPitch)
        {
            pitchDeg = Mathf.Clamp(dragDir.y * maxPitchAngle, -maxPitchAngle, maxPitchAngle);
        }

        if (useGestureAngleForYaw)
        {
            yawDeg = Mathf.Clamp(-dragDir.x * maxYawAngle, -maxYawAngle, maxYawAngle);
        }

        Quaternion rot = Quaternion.identity;
        if (cam != null)
        {
            rot = Quaternion.AngleAxis(yawDeg, Vector3.up) *
                  Quaternion.AngleAxis(pitchDeg, cam.transform.right);
        }
        else
        {
            rot = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        }

        Vector3 dir = rot * baseDir;

        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryAimVisualizer3D] OnDrawing - dragDir={dragDir}, pitch={pitchDeg:F1}, yaw={yawDeg:F1}, baseDir={baseDir}, dir={dir}",
                this); // ARCHERY_DEBUG_LOG
        }

        // 회전 먼저 적용
        previewTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 그 다음, 메쉬의 "시각적인 중심"이 spawnPoint 에 위치하도록 보정
        if (hasPreviewCenterOffset)
        {
            previewTransform.position =
                arrowSpawnPoint.position - previewTransform.rotation * previewCenterLocalOffset;
        }
        else
        {
            previewTransform.position = arrowSpawnPoint.position;
        }

        // 이 게임은 항상 최대 힘으로 쏘므로, 프리뷰 화살도 항상 최대 크기로 표시
        float scale = maxScale;
        previewTransform.localScale = baseLocalScale * scale;

        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryAimVisualizer3D] Updated preview - pos={previewTransform.position}, rot={previewTransform.rotation.eulerAngles}, scale={previewTransform.localScale}",
                this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnDrawEnd(ArcheryGestureManager.GestureData data)
    {
        if (logDebug)
        {
            Debug.Log("[ArcheryAimVisualizer3D] OnDrawEnd - hiding preview", this); // ARCHERY_DEBUG_LOG
        }

        HidePreview();
    }

    private void OnRelease(ArcheryGestureManager.GestureData data)
    {
        // 실제 발사는 다른 스크립트(ArcheryShooter)가 담당
        // 여기서는 시 بص주얼만, OnDrawEnd에서 숨김 처리
        if (logDebug)
        {
            Debug.Log("[ArcheryAimVisualizer3D] OnRelease - visual only (no action)", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnAimAdjust(ArcheryGestureManager.GestureData data)
    {
        // 필요하다면 두 손가락 조준 오프셋(data.aimOffset)을 이용해
        // 좌우/상하 미세 조정 비주얼을 추가할 수 있음.
        if (logDebug)
        {
            Debug.Log($"[ArcheryAimVisualizer3D] OnAimAdjust - aimOffset={data.aimOffset}", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnCancel()
    {
        if (logDebug)
        {
            Debug.Log("[ArcheryAimVisualizer3D] OnCancel - hiding preview", this); // ARCHERY_DEBUG_LOG
        }

        HidePreview();
    }
    #endregion

    #region Helpers
    private void EnsurePreviewInstance()
    {
        if (previewInstance != null)
        {
            if (logDebug)
            {
                Debug.Log("[ArcheryAimVisualizer3D] EnsurePreviewInstance - already exists", this); // ARCHERY_DEBUG_LOG
            }
            return;
        }
        if (arrowPreviewPrefab == null)
        {
            Debug.LogWarning("[ArcheryAimVisualizer3D] arrowPreviewPrefab 이 설정되어 있지 않습니다.");
            return;
        }

        previewInstance = Instantiate(arrowPreviewPrefab);
        previewTransform = previewInstance.transform;
        baseLocalScale = previewTransform.localScale;

        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryAimVisualizer3D] EnsurePreviewInstance - instantiated preview, baseScale={baseLocalScale}",
                this); // ARCHERY_DEBUG_LOG
        }

        // 프리팹의 메쉬 중심을 계산해서, 이후에는 "메쉬 중심"이 arrowSpawnPoint를 기준으로
        // 움직이도록 보정한다.
        var renderer = previewInstance.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            // bounds.center 는 월드 좌표이므로, 이를 프리뷰 Transform 로컬 좌표로 변환
            Vector3 worldCenter = renderer.bounds.center;
            Vector3 localCenter = previewTransform.InverseTransformPoint(worldCenter);

            // 로컬 피벗(0,0,0)에서 메쉬 중심까지의 오프셋
            previewCenterLocalOffset = localCenter;
            hasPreviewCenterOffset = (previewCenterLocalOffset != Vector3.zero);

            if (logDebug)
            {
                Debug.Log(
                    $"[ArcheryAimVisualizer3D] Calculated preview center offset - localCenter={localCenter}, hasOffset={hasPreviewCenterOffset}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
        else
        {
            previewCenterLocalOffset = Vector3.zero;
            hasPreviewCenterOffset = false;

            if (logDebug)
            {
                Debug.Log("[ArcheryAimVisualizer3D] EnsurePreviewInstance - no Renderer found on preview", this); // ARCHERY_DEBUG_LOG
            }
        }

        previewInstance.SetActive(false);
    }

    private void HidePreview()
    {
        if (previewInstance != null)
        {
            previewInstance.SetActive(false);

            if (logDebug)
            {
                Debug.Log("[ArcheryAimVisualizer3D] HidePreview - preview disabled", this); // ARCHERY_DEBUG_LOG
            }
        }
    }
    #endregion
}


