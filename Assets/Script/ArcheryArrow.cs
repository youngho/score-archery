using UnityEngine;

/// <summary>
/// 간단한 화살 동작 스크립트
/// - Rigidbody를 이용해 물리적으로 날아가게 함
/// - 속도 방향으로 화살의 forward를 맞춤
/// - 일정 시간이 지나면 자동 파괴
/// - RequireComponent(Rigidbody) 미사용: 사과 박을 때 화살 Rigidbody를 Destroy하므로 엔진이 RB 제거를 막지 않게 함
/// </summary>
public class ArcheryArrow : MonoBehaviour
{
// 화살 총 생명 시간 / 피격 후 생명 시간은 ArcheryGestureManager 에서 주입한다.
// 여기서는 인스펙터에 노출하지 않고, 기본값만 안전하게 잡아둔다.
private float totalLifeTime = 8f;
private float lifeTimeAfterHit = 2f;

    [Tooltip("아주 느리게 움직일 때 방향 보정을 멈추기 위한 최소 속도 제곱")]
    public float minVelocitySqrForRotate = 0.05f;

    [Header("디버그")]
    [Tooltip("화살의 생성/비행/충돌 과정을 상세 로그로 출력할지 여부")]
    public bool logDebug = false;

    [Header("사과 적중")]
    [Tooltip("충돌 직전 화살 운동량(m·v)을 사과에 AddForceAtPosition으로 줄 비율. 엔진 처리와 겹치면 0.4~0.8 정도로 낮춤.")]
    [Min(0f)]
    public float appleMomentumTransferMultiplier = 1f;

    private Rigidbody rb;
    private bool hasLoggedFirstFlight = false;
    /// <summary>사과 등에 박음: RB 제거 후 회전 보정을 멈춤</summary>
    private bool _embeddedWithoutRigidbody;
    /// <summary>직전 FixedUpdate 시점 속도(충돌 콜백에서는 이미 감속된 값일 수 있음)</summary>
    private Vector3 _preCollisionVelocity;
    private Quaternion initialRotation; // 초기 회전 저장 (ShootArrow에서 설정한 회전)
    private bool hasInitialRotation = false; // 초기 회전이 설정되었는지 여부

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("[ArcheryArrow] 비행에는 Rigidbody가 필요합니다. 프리팹에 Rigidbody를 추가하세요.", this);
            enabled = false;
            return;
        }

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
        // 일정 시간 후 자동 제거 (허수아비에 맞으면 CancelInvoke로 취소)
        if (totalLifeTime > 0f)
        {
            Invoke(nameof(DestroyArrow), totalLifeTime);
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
            Debug.Log($"[ArcheryArrow] Start - totalLifeTime={totalLifeTime}, initialRotation={initialRotation.eulerAngles}, showTrail={showTrail}", this); // ARCHERY_DEBUG_LOG
        }
    }

    /// <summary>
    /// ArcheryGestureManager 에서 화살 생성 직후 생명 시간을 설정할 때 사용.
    /// </summary>
    public void ConfigureLifetime(float totalLifetimeSeconds, float afterHitLifetimeSeconds)
    {
        totalLifeTime = totalLifetimeSeconds;
        lifeTimeAfterHit = afterHitLifetimeSeconds;
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
        if (_embeddedWithoutRigidbody) return;

        if (rb != null)
            _preCollisionVelocity = rb.linearVelocity;

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

        AppleBehavior appleBehavior = collision.gameObject.GetComponent<AppleBehavior>()
            ?? collision.gameObject.GetComponentInParent<AppleBehavior>();

        // 사과: 운동량 전달 후 박기 — 엔진만 믿으면 질량 비·수면·밀림에서 안 움직이는 경우가 있음
        if (appleBehavior != null)
        {
            ApplyMomentumToApple(appleBehavior, collision);
            StickToSurface(collision, appleBehavior.transform, removeArrowRigidbody: true);
            CancelInvoke(nameof(DestroyArrow));
            if (logDebug)
            {
                Debug.Log($"[ArcheryArrow] OnCollisionEnter - Hit apple {collision.collider.name}, embedded (no timed destroy).", this);
            }
            return;
        }

        ScarecrowBehavior scarecrow = collision.gameObject.GetComponent<ScarecrowBehavior>()
            ?? collision.gameObject.GetComponentInParent<ScarecrowBehavior>();

        // Not a balloon -> Hard object collision: stop and stick
        if (rb != null)
            StickToSurface(collision, collision.transform, removeArrowRigidbody: false);

        if (scarecrow != null)
        {
            CancelInvoke(nameof(DestroyArrow));
            if (logDebug)
            {
                Debug.Log($"[ArcheryArrow] OnCollisionEnter - Hit scarecrow {collision.collider.name}, arrow kept (no timed destroy).", this);
            }
            return;
        }

        // 충돌 직후 바로 사라지지 않도록 약간의 시간 후 제거
        if (lifeTimeAfterHit > 0f)
        {
            Destroy(gameObject, lifeTimeAfterHit);
        }

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

    /// <summary>
    /// 충돌 직전 화살 속도 기준으로 사과에 충격량(impulse)을 가해 굴러떨어리게 함.
    /// </summary>
    private void ApplyMomentumToApple(AppleBehavior apple, Collision collision)
    {
        if (apple == null || appleMomentumTransferMultiplier <= 0f || rb == null)
            return;

        Rigidbody appleRb = apple.GetComponent<Rigidbody>();
        if (appleRb == null || appleRb.isKinematic)
            return;

        appleRb.WakeUp();

        Vector3 v = _preCollisionVelocity.sqrMagnitude > 1e-6f
            ? _preCollisionVelocity
            : rb.linearVelocity;
        if (v.sqrMagnitude < 1e-6f)
            v = collision.relativeVelocity;

        if (v.sqrMagnitude < 1e-8f)
            return;

        float m = rb.mass;
        Vector3 impulse = v * (m * appleMomentumTransferMultiplier);

        Vector3 point = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;

        appleRb.AddForceAtPosition(impulse, point, ForceMode.Impulse);
    }

    /// <summary>
    /// 충돌 지점에 붙이고, 필요 시 화살 Rigidbody를 제거해 부모(사과) 단일 강체 시뮬에 맞춤.
    /// </summary>
    private void StickToSurface(Collision collision, Transform parent, bool removeArrowRigidbody)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (TryGetComponent<Collider>(out var col)) col.enabled = false;

        if (collision.contacts != null && collision.contacts.Length > 0)
            transform.position = collision.contacts[0].point;

        transform.SetParent(parent, worldPositionStays: true);

        if (removeArrowRigidbody && rb != null)
        {
            // Destroy는 프레임 말에 실행되므로 먼저 플래그로 FixedUpdate 등을 멈춤
            _embeddedWithoutRigidbody = true;
            Destroy(rb);
            rb = null;
        }
    }

    private void DestroyArrow()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (logDebug)
        {
            Debug.Log("[ArcheryArrow] OnDestroy - arrow destroyed", this); // ARCHERY_DEBUG_LOG
        }
    }
}


