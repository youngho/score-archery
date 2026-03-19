using UnityEngine;

/// <summary>
/// 수박 개별 동작:
/// - Rigidbody 기반으로 공처럼 튀도록 PhysicMaterial 세팅
/// - 스폰 직후 초기 임펄스를 적용
/// - 화살(ArcheryArrow)에 맞으면 폭발 VFX + 점수 1회 + 즉시 파괴
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WatermelonBehavior : MonoBehaviour
{
    [Header("Physics")]
    public bool useGravity = true;

    [Range(0f, 1f)]
    public float bounciness = 0.9f;

    [Range(0f, 1f)]
    public float dynamicFriction = 0.1f;

    [Tooltip("최대 속도 제한(0이면 제한 없음)")]
    public float maxSpeed = 12f;

    [Tooltip("스폰 시 적용할 초기 임펄스")]
    public Vector3 initialImpulse = Vector3.zero;

    [Header("Scoring")]
    [Tooltip("맞추면 올릴 점수")]
    public int points = 1;

    [Header("Explosion Effect")]
    public bool useExplosion = true;
    public Color explosionColor = new Color(0.35f, 1.0f, 0.45f, 1f);
    public float explosionDuration = 0.12f;
    public float explosionLifetime = 0.35f;
    public float explosionRadius = 0.20f;
    public int explosionParticleCount = 60;
    public float explosionSpeedMin = 2.0f;
    public float explosionSpeedMax = 7.0f;

    private WatermelonManager _owner;
    private Rigidbody _rb;
    private Collider _col;
    private bool _isHit;

    private PhysicsMaterial _runtimeMat;

    public void SetOwner(WatermelonManager owner) => _owner = owner;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        // 안전 기본값
        _rb.useGravity = useGravity;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        EnsureSphereColliderIfPossible();
        ApplyBouncyMaterial();
    }

    private void Start()
    {
        _rb.useGravity = useGravity;

        if (initialImpulse.sqrMagnitude > 0.0001f)
            _rb.AddForce(initialImpulse, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (maxSpeed > 0f && _rb != null)
        {
            float sqr = _rb.linearVelocity.sqrMagnitude;
            float maxSqr = maxSpeed * maxSpeed;
            if (sqr > maxSqr)
                _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void EnsureSphereColliderIfPossible()
    {
        // 프리팹이 MeshCollider일 수 있는데, 물리 튐이 불안정해질 수 있어 가능하면 SphereCollider로 보강
        if (_col == null) return;
        if (_col is SphereCollider) return;

        // 이미 primitive sphere면 SphereCollider가 있을 것
        // MeshCollider(Convex) 등을 강제로 바꾸면 모양이 깨질 수 있어, 여기서는 "없을 때만" 추가/대체는 하지 않음.
    }

    private void ApplyBouncyMaterial()
    {
        if (_col == null) return;

        _runtimeMat = new PhysicsMaterial("WatermelonBouncyMat");
        _runtimeMat.bounciness = Mathf.Clamp01(bounciness);
        _runtimeMat.dynamicFriction = Mathf.Clamp01(dynamicFriction);
        _runtimeMat.staticFriction = Mathf.Clamp01(dynamicFriction);
        _runtimeMat.frictionCombine = PhysicsMaterialCombine.Minimum;
        _runtimeMat.bounceCombine = PhysicsMaterialCombine.Maximum;

        _col.material = _runtimeMat;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit) return;

        var arrow = collision.collider != null
            ? collision.collider.GetComponentInParent<ArcheryArrow>()
            : null;
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();

        if (arrow == null) return;

        _isHit = true;

        Vector3 impactPoint = transform.position;
        Vector3 impactNormal = Vector3.up;
        if (collision.contacts != null && collision.contacts.Length > 0)
        {
            impactPoint = collision.contacts[0].point;
            impactNormal = collision.contacts[0].normal;
        }

        WatermelonScoreManager.Instance?.AddWatermelonScore(points);

        if (useExplosion)
            CreateWatermelonExplosion(impactPoint, impactNormal);

        Destroy(gameObject);
    }

    private void CreateWatermelonExplosion(Vector3 position, Vector3 impactNormal)
    {
        GameObject explosion = new GameObject("WatermelonExplosion");
        explosion.transform.position = position;

        if (impactNormal.sqrMagnitude > 0.0001f)
            explosion.transform.rotation = Quaternion.LookRotation(impactNormal);

        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;

        main.duration = explosionDuration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = explosionLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(explosionSpeedMin, explosionSpeedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.startColor = explosionColor;

        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)explosionParticleCount, (short)explosionParticleCount)
        });

        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = explosionRadius;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Additive");
            if (shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = explosionColor;
            }
        }

        ps.Play();
        Destroy(explosion, explosionDuration + explosionLifetime + 0.1f);
    }

    private void OnDestroy()
    {
        _owner?.NotifyWatermelonDestroyed(this);

        // 런타임으로 만든 PhysicMaterial은 GC/Unity 리소스 정리를 위해 명시적으로 제거
        if (_runtimeMat != null)
        {
            Destroy(_runtimeMat);
            _runtimeMat = null;
        }
    }
}

