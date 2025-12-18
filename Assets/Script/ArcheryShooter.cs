using UnityEngine;

/// <summary>
/// ArcheryGestureManager의 제스처 이벤트를 받아 실제로 화살을 발사하는 컴포넌트
/// 새 씬에 빈 오브젝트를 두고 이 스크립트를 붙이면, 제스처만으로 화살 발사가 동작합니다.
/// </summary>
public class ArcheryShooter : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    [Tooltip("화살이 생성될 위치와 방향(보통 활/카메라 앞쪽)")]
    public Transform arrowSpawnPoint;

    [Tooltip("발사할 화살 프리팹 (Rigidbody + ArcheryArrow 포함 권장)")]
    public GameObject arrowPrefab;

    [Tooltip("최대 발사 힘")]
    public float maxForce = 15f;

    [Tooltip("제스처 각도를 위/아래 피치 각도로 사용할지 여부")]
    public bool useGestureAngleForPitch = true;

    [Tooltip("제스처 각도를 좌/우 요(yaw) 각도로 사용할지 여부")]
    public bool useGestureAngleForYaw = true;

    [Tooltip("제스처로 조정 가능한 최대 피치 각도")]
    public float maxPitchAngle = 45f;

    [Tooltip("제스처로 조정 가능한 최대 요(yaw) 각도")]
    public float maxYawAngle = 45f;

    [Header("디버그")]
    public bool logDebug = false;

    private void OnEnable()
    {
        // ArcheryGestureManager 싱글톤을 통해 이벤트 구독
        var mgr = ArcheryGestureManager.Instance;

        if (logDebug)
        {
            Debug.Log("[ArcheryShooter] OnEnable - subscribing gesture events", this); // ARCHERY_DEBUG_LOG
        }

        mgr.OnDrawStart.AddListener(OnDrawStart);
        mgr.OnDrawing.AddListener(OnDrawing);
        mgr.OnDrawEnd.AddListener(OnDrawEnd);
        mgr.OnRelease.AddListener(OnRelease);
        mgr.OnAimAdjust.AddListener(OnAimAdjust);
        mgr.OnCancel.AddListener(OnCancel);
    }

    private void OnDisable()
    {
        // 씬 종료 시 자동으로 제거되지만, 안전하게 이벤트 해제
        var mgr = ArcheryGestureManager.Instance;

        if (logDebug)
        {
            Debug.Log("[ArcheryShooter] OnDisable - unsubscribing gesture events", this); // ARCHERY_DEBUG_LOG
        }

        mgr.OnDrawStart.RemoveListener(OnDrawStart);
        mgr.OnDrawing.RemoveListener(OnDrawing);
        mgr.OnDrawEnd.RemoveListener(OnDrawEnd);
        mgr.OnRelease.RemoveListener(OnRelease);
        mgr.OnAimAdjust.RemoveListener(OnAimAdjust);
        mgr.OnCancel.RemoveListener(OnCancel);
    }

    #region Gesture Event Handlers
    private void OnDrawStart(ArcheryGestureManager.GestureData data)
    {
        if (logDebug)
        {
            Debug.Log($"[ArcheryShooter] OnDrawStart - startPos={data.startPosition}", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnDrawing(ArcheryGestureManager.GestureData data)
    {
        // 필요하면 여기서 조준 UI, 파워 게이지 등을 업데이트
        // 예: data.normalizedPower 사용
        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryShooter] OnDrawing - distance={data.distance:F1}, power={data.normalizedPower:F2}, angle={data.angle:F1}",
                this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnDrawEnd(ArcheryGestureManager.GestureData data)
    {
        // 드로우가 끝났지만 발사가 아닐 수도 있음 (짧게 드래그 등)
        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryShooter] OnDrawEnd - distance={data.distance:F1}, power={data.normalizedPower:F2}, angle={data.angle:F1}",
                this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnAimAdjust(ArcheryGestureManager.GestureData data)
    {
        // 두 손가락으로 조준을 미세 조정하는 용도로 사용할 수 있음
        // data.aimOffset을 이용해 카메라/활 회전 보정 가능
        if (logDebug)
        {
            Debug.Log($"[ArcheryShooter] OnAimAdjust - aimOffset={data.aimOffset}", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnCancel()
    {
        if (logDebug)
        {
            Debug.Log("[ArcheryShooter] OnCancel - gesture canceled", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnRelease(ArcheryGestureManager.GestureData data)
    {
        // 실제 화살 발사 시점
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            Debug.LogWarning("[ArcheryShooter] arrowPrefab 또는 arrowSpawnPoint가 설정되어 있지 않습니다.", this); // ARCHERY_DEBUG_LOG
            return;
        }

        // 사용자의 요청대로 최소 발사 힘 없이, 오직 최대 발사 힘(maxForce)과 각도만으로 거리를 계산(발사)함
        float force = maxForce;

        // 발사 기본 방향: spawnPoint가 있으면 그 forward, 없으면 카메라 forward
        Camera cam = Camera.main;
        Vector3 baseDir = arrowSpawnPoint != null
            ? arrowSpawnPoint.forward
            : (cam != null ? cam.transform.forward : Vector3.forward);

        // 발사 방향 계산: 드래그 방향의 반대로 발사 (활쏘기처럼 당기는 방향의 반대)
        // 아래로 드래그 → 위로 발사, 오른쪽으로 드래그 → 왼쪽으로 발사
        Vector2 dragDir = (data.currentPosition - data.startPosition).normalized;

        // 피치/요 각도 계산
        float pitchDeg = 0f;
        float yawDeg = 0f;

        if (useGestureAngleForPitch)
        {
            // 아래로 드래그하면 위로 발사 (당기는 방향의 반대)
            // dragDir.y가 양수(아래로 드래그) → pitchDeg가 양수(위로 향함)
            // dragDir.y가 음수(위로 드래그) → pitchDeg가 0 (변화 없음)
            float rawAngle = dragDir.y * maxPitchAngle;
            pitchDeg = Mathf.Clamp(rawAngle, 0f, maxPitchAngle); // 위로 드래그는 무시 (최소값 0)
        }

        if (useGestureAngleForYaw)
        {
            // 오른쪽으로 드래그하면 왼쪽으로 발사, 왼쪽으로 드래그하면 오른쪽으로 발사 (당기는 방향의 반대)
            // dragDir.x가 양수(오른쪽으로 드래그) → yawDeg가 음수(왼쪽으로 향함)
            // dragDir.x가 음수(왼쪽으로 드래그) → yawDeg가 양수(오른쪽으로 향함)
            yawDeg = Mathf.Clamp(-dragDir.x * maxYawAngle, -maxYawAngle, maxYawAngle);
        }

        // 월드 기준 회전 구성 (요는 세계 Y축, 피치는 카메라의 오른쪽 축 기준)
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
                $"[ArcheryShooter] Calculated shot direction - dragDir={dragDir}, pitch={pitchDeg:F1}, yaw={yawDeg:F1}, baseDir={baseDir}",
                this); // ARCHERY_DEBUG_LOG
        }

        // 화살 생성 및 초기 회전 설정
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.LookRotation(dir));

        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryShooter] Spawned arrow instance '{arrow.name}' at {arrowSpawnPoint.position} with dir={dir}",
                this); // ARCHERY_DEBUG_LOG
        }

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * force, ForceMode.Impulse);

            if (logDebug)
            {
                Debug.Log(
                    $"[ArcheryShooter] Applied force to arrow - force={force:F1}, velocity={rb.linearVelocity}, mass={rb.mass}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
        else if (logDebug)
        {
            Debug.Log("[ArcheryShooter] Spawned arrow has no Rigidbody component", this); // ARCHERY_DEBUG_LOG
        }

        if (logDebug)
        {
            Debug.Log(
                $"[ArcheryShooter] Shoot complete - gesturePower={data.normalizedPower:F2}, force={force:F1}, dir={dir}, pitch={pitchDeg:F1}, yaw={yawDeg:F1}",
                this); // ARCHERY_DEBUG_LOG
        }
    }
    #endregion
}


