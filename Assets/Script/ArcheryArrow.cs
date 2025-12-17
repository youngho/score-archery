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

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // 일정 시간 후 자동 제거
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // 화살이 날아가는 방향으로 forward를 자동 정렬
        if (rb.linearVelocity.sqrMagnitude > minVelocitySqrForRotate)
        {
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
    }
}


