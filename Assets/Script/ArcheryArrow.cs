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
    private Quaternion initialRotation; // 초기 회전 저장 (ShootArrow에서 설정한 회전)
    private bool hasInitialRotation = false; // 초기 회전이 설정되었는지 여부

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (logDebug)
        {
            Debug.Log($"[ArcheryArrow] Awake - rb assigned={(rb != null)}", this); // ARCHERY_DEBUG_LOG
        }
    }

    [Header("Trail Effect")]
    [Tooltip("화살 비행 시 궤적(Trail) 표시 여부")]
    public bool showTrail = true;
    [Tooltip("궤적 색상")]
    public Color trailColor = Color.yellow;
    [Tooltip("궤적 유지 시간")]
    public float trailTime = 0.5f;
    [Tooltip("궤적 시작 두께")]
    public float trailStartWidth = 0.05f;
    [Tooltip("궤적 끝 두께")]
    public float trailEndWidth = 0.0f;
    [Tooltip("사용할 궤적 머티리얼 (없으면 기본 Additive 쉐이더 사용)")]
    public Material trailMaterial;

    private void Start()
    {
        // 일정 시간 후 자동 제거
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }

        // Start에서 초기 회전 저장 (ShootArrow에서 설정한 회전을 보존)
        // ShootArrow에서 이미 올바른 회전을 설정했으므로, 이를 초기 회전으로 저장
        initialRotation = transform.rotation;
        hasInitialRotation = true;

        if (showTrail)
        {
            SetupTrail();
        }

        if (logDebug)
        {
            Debug.Log($"[ArcheryArrow] Start - lifeTime={lifeTime}, initialRotation={initialRotation.eulerAngles}, showTrail={showTrail}", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void SetupTrail()
    {
        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr == null)
        {
            tr = gameObject.AddComponent<TrailRenderer>();
        }

        tr.time = trailTime;
        tr.startWidth = trailStartWidth;
        tr.endWidth = trailEndWidth;
        tr.minVertexDistance = 0.1f; // 부드러운 곡선을 위해

        // 머티리얼 설정
        if (trailMaterial != null)
        {
            tr.material = trailMaterial;
        }
        else
        {
            // 기본 머티리얼 생성 (Additive 쉐이더 사용으로 빛나는 효과)
            Shader shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Additive"); // Fallback
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive"); // Double Fallback
            
            if (shader != null)
            {
                tr.material = new Material(shader);
            }
        }

        // 그라디언트 설정 (투명하게 사라지도록)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(trailColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        tr.colorGradient = gradient;

        // 그림자 끄기
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // 화살이 날아가는 방향으로 forward를 자동 정렬
        // ShootArrow에서 설정한 초기 회전을 기준으로 속도 방향에 맞춰 회전
        if (rb.linearVelocity.sqrMagnitude > minVelocitySqrForRotate)
        {
            if (logDebug && !hasLoggedFirstFlight)
            {
                hasLoggedFirstFlight = true;
                Debug.Log(
                    $"[ArcheryArrow] First flight - velocity={rb.linearVelocity}, speed={rb.linearVelocity.magnitude:F2}, currentRotation={transform.rotation.eulerAngles}, initialRotation={(hasInitialRotation ? initialRotation.eulerAngles.ToString() : "not set")}",
                    this); // ARCHERY_DEBUG_LOG
            }

            // 속도 방향으로 회전
            // ShootArrow에서 이미 올바른 방향으로 회전을 설정했으므로,
            // 속도 방향에 맞춰 회전하면 자연스럽게 화살이 날아가는 방향을 향함
            Vector3 velocityDir = rb.linearVelocity.normalized;
            transform.rotation = Quaternion.LookRotation(velocityDir);
        }
        else if (hasInitialRotation && !hasLoggedFirstFlight)
        {
            // 속도가 아직 충분하지 않으면 초기 회전 유지
            // ShootArrow에서 설정한 회전을 보존
            transform.rotation = initialRotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit a balloon
        BalloonBehavior balloon = collision.gameObject.GetComponent<BalloonBehavior>();
        if (balloon != null)
        {
            // If hit a balloon, don't stop the arrow.
            // The balloon will handle its own popping and destruction.
            if (logDebug)
            {
                Debug.Log($"[ArcheryArrow] OnCollisionEnter - Hit balloon {collision.collider.name}, passing through.", this);
            }
            return;
        }

        // Not a balloon -> Hard object collision: stop and stick
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Stick to the hit object
            transform.SetParent(collision.transform);
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


