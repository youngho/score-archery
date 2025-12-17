using UnityEngine;

/// <summary>
/// 간단한 화살 동작 스크립트
/// - Rigidbody를 이용해 물리적으로 날아가게 함
/// - 속도 방향으로 화살의 forward를 맞춤
/// - 일정 시간이 지나면 자동 파괴
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ArcheryArrow : MonoBehaviour
{
    [Tooltip("화살이 자동으로 사라지는 시간 (초)")]
    public float lifeTime = 8f;

    [Tooltip("아주 느리게 움직일 때 방향 보정을 멈추기 위한 최소 속도 제곱")]
    public float minVelocitySqrForRotate = 0.05f;

    [Header("디버그")]
    [Tooltip("화살의 생성/비행/충돌 과정을 상세 로그로 출력할지 여부")]
    public bool logDebug = false;

    private Rigidbody rb;
    private bool hasLoggedFirstFlight = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (logDebug)
        {
            Debug.Log($"[ArcheryArrow] Awake - rb assigned={(rb != null)}", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void Start()
    {
        // 일정 시간 후 자동 제거
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }

        if (logDebug)
        {
            Debug.Log($"[ArcheryArrow] Start - lifeTime={lifeTime}", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // 화살이 날아가는 방향으로 forward를 자동 정렬
        if (rb.linearVelocity.sqrMagnitude > minVelocitySqrForRotate)
        {
            if (logDebug && !hasLoggedFirstFlight)
            {
                hasLoggedFirstFlight = true;
                Debug.Log(
                    $"[ArcheryArrow] First flight - velocity={rb.linearVelocity}, speed={rb.linearVelocity.magnitude:F2}",
                    this); // ARCHERY_DEBUG_LOG
            }

            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 간단하게: 충돌 시 물리 멈추고 잠시 후 제거
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 충돌 직후 바로 사라지지 않도록 약간의 시간 후 제거
        Destroy(gameObject, 2f);

        if (logDebug)
        {
            string otherName = collision.collider != null ? collision.collider.name : "Unknown";
            Vector3 contactPoint = collision.contacts != null && collision.contacts.Length > 0
                ? collision.contacts[0].point
                : transform.position;

            Debug.Log(
                $"[ArcheryArrow] OnCollisionEnter - hit={otherName}, contactPoint={contactPoint}, relativeVelocity={collision.relativeVelocity}",
                this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnDestroy()
    {
        if (logDebug)
        {
            Debug.Log("[ArcheryArrow] OnDestroy - arrow destroyed", this); // ARCHERY_DEBUG_LOG
        }
    }
}


