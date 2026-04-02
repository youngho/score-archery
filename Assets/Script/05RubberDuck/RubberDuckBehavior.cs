using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class RubberDuckBehavior : MonoBehaviour
{
    [Header("Water")]
    [Tooltip("Water transform for surface height. If null, finds by name \"Water\" or uses waterLevelY.")]
    public Transform waterTransform;
    public float waterLevelY = -0.3f;

    [Header("Water path (물길)")]
    [Tooltip("물길 경로. 이 Transform의 자식 오브젝트들이 순서대로 웨이포인트가 됩니다. 비어 있으면 Water를 찾고, Water도 없으면 기본 +Z 방향으로 흐릅니다.")]
    public Transform waterPath;
    [Tooltip("물길을 따라 흐르는 속도.")]
    public float waterFlowSpeed = 8f;

    [Header("Drift (random floating)")]
    public float driftSpeed = 1f;
    public float driftDirectionChangeInterval = 2f;
    public float driftRandomness = 0.8f;

    [Header("Bob & Sway (visual)")]
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 2f;
    public float swayAmplitude = 0.2f;
    public float swaySpeed = 1f;
    public float tiltAmplitude = 5f;
    public float tiltSpeed = 1.5f;
    public float yawAmplitude = 10f;

    [Header("Obstacle (WoodBoat, Terrain)")]
    public float obstacleDetectionDist = 1.5f;
    public float turnSpeed = 2f;
    public LayerMask terrainLayer = -1;
    [Tooltip("Bounce force when hitting obstacle (optional).")]
    public float bounceForce = 0.5f;
    public float bounceCooldown = 1f;
    [Tooltip("지형(LowpolyTerrain)과 충돌 시 밀어내는 힘. 이동력보다 커야 오리가 땅에서 벗어납니다.")]
    public float terrainPushForce = 12f;

    [Header("Lifecycle")]
    public float destroyZ = 10f;

    [Header("Hit (화살 명중)")]
    [Tooltip("맞았을 때 재생할 외부 VFX 프리팹(파티클 등). 비워 두면 생략.")]
    public GameObject hitEffectPrefab;

    [Tooltip("맞았을 때 재생할 효과음 (예: RubberDuck.wav)")]
    [SerializeField] private AudioClip hitSound;

    [SerializeField, Range(0f, 1f)]
    private float hitSoundVolume = 1f;

    private Rigidbody _rb;
    private Vector3 _driftDirection;
    private Vector3 _flowDirection;
    private float _nextDirectionChangeTime;
    private float _bobPhase;
    private float _swayPhase;
    private float _lastBounceTime = -999f;
    private float _initialHeadingY;
    private bool _hitByArrow;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.linearDamping = 2f;
        _rb.angularDamping = 2f;

        if (waterTransform == null)
        {
            var w = GameObject.Find("Water");
            if (w != null) waterTransform = w.transform;
        }
        if (waterPath == null)
        {
            var wp = GameObject.Find("WaterPath");
            if (wp != null) waterPath = wp.transform;
        }

        _driftDirection = Random.insideUnitCircle.normalized;
        _driftDirection = new Vector3(_driftDirection.x, 0f, _driftDirection.y);
        _nextDirectionChangeTime = Time.time + Random.Range(driftDirectionChangeInterval * 0.5f, driftDirectionChangeInterval);
        _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        _swayPhase = Random.Range(0f, Mathf.PI * 2f);
        _initialHeadingY = Random.Range(0f, 360f);
    }

    void FixedUpdate()
    {
        float waterY = GetWaterSurfaceY();
        UpdateDriftDirection();
        ApplyDriftAndKeepOnWater(waterY);
    }

    void Update()
    {
        if (transform.position.z > destroyZ)
        {
            Destroy(gameObject);
            return;
        }
        float waterY = GetWaterSurfaceY();
        ApplyBobAndSway(waterY);
    }

    private float GetWaterSurfaceY()
    {
        if (waterTransform != null)
            return waterTransform.position.y;
        return waterLevelY;
    }

    /// <summary>
    /// 물길(waterPath) 웨이포인트에서 오리 위치에 해당하는 흐름 방향을 구합니다.
    /// </summary>
    private void UpdateFlowDirection()
    {
        _flowDirection = GetFlowDirectionFromPath();
    }

    private Vector3 GetFlowDirectionFromPath()
    {
        if (waterPath == null || waterPath.childCount < 2)
        {
            return new Vector3(0f, 0f, 1f);
        }

        Vector3 pos = transform.position;
        Vector2 p = new Vector2(pos.x, pos.z);

        int bestSegment = -1;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < waterPath.childCount - 1; i++)
        {
            Vector3 a = waterPath.GetChild(i).position;
            Vector3 b = waterPath.GetChild(i + 1).position;
            Vector2 a2 = new Vector2(a.x, a.z);
            Vector2 b2 = new Vector2(b.x, b.z);
            Vector2 ab = b2 - a2;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f) continue;

            float t = Vector2.Dot(p - a2, ab) / lenSq;
            Vector2 closest = a2 + Mathf.Clamp01(t) * ab;
            float distSq = (p - closest).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestSegment = i;
            }
        }

        if (bestSegment < 0)
            return new Vector3(0f, 0f, 1f);

        Vector3 from = waterPath.GetChild(bestSegment).position;
        Vector3 to = waterPath.GetChild(bestSegment + 1).position;
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return new Vector3(0f, 0f, 1f);
        return dir.normalized;
    }

    private void UpdateDriftDirection()
    {
        if (Time.time >= _nextDirectionChangeTime)
        {
            _nextDirectionChangeTime = Time.time + Random.Range(driftDirectionChangeInterval * 0.5f, driftDirectionChangeInterval * 1.5f);
            Vector2 r = Random.insideUnitCircle.normalized;
            Vector3 newDir = new Vector3(r.x, 0f, r.y);
            _driftDirection = Vector3.Slerp(_driftDirection, newDir, driftRandomness);
            _driftDirection.y = 0f;
            if (_driftDirection.sqrMagnitude < 0.01f) _driftDirection = new Vector3(1f, 0f, 0f);
            _driftDirection.Normalize();
        }

        AvoidObstacles();
    }

    private void AvoidObstacles()
    {
        Vector3 fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = _driftDirection;

        if (Physics.Raycast(transform.position, fwd, obstacleDetectionDist, terrainLayer))
        {
            _driftDirection = Vector3.Slerp(_driftDirection, -fwd + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f)), turnSpeed * Time.fixedDeltaTime);
            _driftDirection.y = 0f;
            _driftDirection.Normalize();
        }
    }

    private void ApplyDriftAndKeepOnWater(float waterY)
    {
        UpdateFlowDirection();

        Vector3 flow = _flowDirection * waterFlowSpeed;
        Vector3 randomDrift = _driftDirection * driftSpeed;
        Vector3 vel = flow + randomDrift;
        vel.y = 0f;
        _rb.linearVelocity = vel;

        Vector3 pos = transform.position;
        pos.y = waterY;
        transform.position = pos;
    }

    private void ApplyBobAndSway(float waterY)
    {
        _bobPhase += floatSpeed * Time.deltaTime;
        _swayPhase += swaySpeed * Time.deltaTime;

        float bob = Mathf.Sin(_bobPhase) * floatAmplitude;
        float sway = Mathf.Sin(_swayPhase) * swayAmplitude;
        float tilt = Mathf.Sin(_swayPhase * 0.7f) * tiltAmplitude;
        float yaw = Mathf.Sin(_swayPhase * 0.5f) * yawAmplitude;

        Vector3 pos = transform.position;
        pos.y = waterY + bob;
        transform.position = pos;

        Quaternion baseRot = Quaternion.Euler(tilt, _initialHeadingY + yaw, sway);
        transform.rotation = baseRot;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hitByArrow) return;

        var arrow = collision.collider != null
            ? collision.collider.GetComponentInParent<ArcheryArrow>()
            : null;
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();
        if (arrow != null)
        {
            _hitByArrow = true;

            Vector3 impactPoint = transform.position;
            Vector3 impactNormal = Vector3.up;
            if (collision.contactCount > 0)
            {
                var c = collision.GetContact(0);
                impactPoint = c.point;
                impactNormal = c.normal;
            }

            RubberDuckScoreManager.Instance?.AddRubberDuckScore(null);

            if (hitEffectPrefab != null)
            {
                Quaternion vfxRot = impactNormal.sqrMagnitude > 1e-6f
                    ? Quaternion.LookRotation(impactNormal)
                    : Quaternion.identity;
                Instantiate(hitEffectPrefab, impactPoint, vfxRot);
            }

            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, impactPoint, hitSoundVolume);

            Destroy(gameObject);
            return;
        }

        bool isBoat = collision.gameObject.CompareTag("WoodBoat");
        bool isTerrain = collision.gameObject.name.Contains("LowpolyTerrain") || collision.gameObject.CompareTag("Terrain");

        if (collision.gameObject.name.Contains("LowpolyTerrain"))
        {
            Debug.Log($"[RubberDuck] LowpolyTerrain과 충돌: {collision.gameObject.name}");
            PushAwayFromTerrain(collision);
        }

        if ((isBoat || isTerrain) && Time.time - _lastBounceTime >= bounceCooldown && bounceForce > 0f)
        {
            _lastBounceTime = Time.time;
            if (collision.contactCount > 0)
            {
                Vector3 normal = collision.GetContact(0).normal;
                normal.y = 0f;
                if (normal.sqrMagnitude > 0.01f)
                {
                    _rb.AddForce(normal.normalized * bounceForce, ForceMode.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// LowpolyTerrain과 충돌 시 접촉 법선의 반대방향(지형에서 밖으로)으로 오리를 튕겨냅니다.
    /// terrainPushForce + 현재 속도에 비례한 반발을 적용해 땅을 타고 올라가는 걸 막습니다.
    /// </summary>
    private void PushAwayFromTerrain(Collision collision)
    {
        if (collision.contactCount == 0) return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 normal = contact.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.01f) return;
        normal.Normalize();

        float basePush = terrainPushForce > 0f ? terrainPushForce : 12f;
        float velocityIntoTerrain = Mathf.Max(0f, -Vector3.Dot(_rb.linearVelocity, normal));
        float pushStrength = basePush + velocityIntoTerrain * 1.5f;
        _rb.AddForce(normal * pushStrength, ForceMode.Impulse);

        _driftDirection = normal;
        _nextDirectionChangeTime = Time.time + driftDirectionChangeInterval;
    }
}
