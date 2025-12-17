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

    [Header("발사 파라미터")]
    [Tooltip("최소 발사 힘")]
    public float minForce = 5f;

    [Tooltip("최대 발사 힘")]
    public float maxForce = 25f;

    [Tooltip("제스처 각도를 위/아래 피치 각도로 사용할지 여부")]
    public bool useGestureAngleForPitch = true;

    [Tooltip("제스처로 조정 가능한 최대 피치 각도")]
    public float maxPitchAngle = 45f;

    [Header("디버그")]
    public bool logDebug = true;

    private void OnEnable()
    {
        // ArcheryGestureManager 싱글톤을 통해 이벤트 구독
        var mgr = ArcheryGestureManager.Instance;
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
            Debug.Log($"[ArcheryShooter] Draw Start at {data.startPosition}");
        }
    }

    private void OnDrawing(ArcheryGestureManager.GestureData data)
    {
        // 필요하면 여기서 조준 UI, 파워 게이지 등을 업데이트
        // 예: data.normalizedPower 사용
    }

    private void OnDrawEnd(ArcheryGestureManager.GestureData data)
    {
        // 드로우가 끝났지만 발사가 아닐 수도 있음 (짧게 드래그 등)
    }

    private void OnAimAdjust(ArcheryGestureManager.GestureData data)
    {
        // 두 손가락으로 조준을 미세 조정하는 용도로 사용할 수 있음
        // data.aimOffset을 이용해 카메라/활 회전 보정 가능
    }

    private void OnCancel()
    {
        if (logDebug)
        {
            Debug.Log("[ArcheryShooter] Gesture Canceled");
        }
    }

    private void OnRelease(ArcheryGestureManager.GestureData data)
    {
        // 실제 화살 발사 시점
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            Debug.LogWarning("[ArcheryShooter] arrowPrefab 또는 arrowSpawnPoint가 설정되어 있지 않습니다.");
            return;
        }

        // 제스처 파워를 0~1로 받아 실제 힘으로 변환
        float t = Mathf.Clamp01(data.normalizedPower);
        float force = Mathf.Lerp(minForce, maxForce, t);

        // 발사 방향: 기본은 spawnPoint의 forward
        Vector3 dir = arrowSpawnPoint.forward;

        if (useGestureAngleForPitch)
        {
            // 제스처 각도(data.angle)는 화면 상의 2D 각도이므로,
            // 세로 방향(Y)을 위아래 피치로만 사용하는 단순 매핑
            float pitch = Mathf.Clamp(-data.direction.y * maxPitchAngle, -maxPitchAngle, maxPitchAngle);
            dir = Quaternion.Euler(pitch, 0f, 0f) * dir;
        }

        // 화살 생성 및 초기 회전 설정
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.LookRotation(dir));

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * force, ForceMode.Impulse);
        }

        if (logDebug)
        {
            Debug.Log($"[ArcheryShooter] Shoot. power={t:F2}, force={force:F1}, dir={dir}");
        }
    }
    #endregion
}


