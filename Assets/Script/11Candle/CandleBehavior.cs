using UnityEngine;

/// <summary>
/// 양초 개별 동작:
/// - 화살(ArcheryArrow) 충돌 시 Rigidbody를 켜서 쓰러지게 하고, candleFlame을 끄고 점수 1회 반영
/// - Rigidbody는 candle.prefab에 유니티에서 설정해 둠
/// </summary>
[DisallowMultipleComponent]
public class CandleBehavior : MonoBehaviour
{
    [Header("Flame")]
    [Tooltip("candle.prefab 내부에서 끌 오브젝트 이름")]
    public string flameObjectName = "candleFlame";

    [Header("쓰러짐")]
    [Tooltip("화살 충돌 시 넘어지는 힘 배율 (작을수록 살짝만 쓰러짐)")]
    [Range(0.1f, 1.5f)]
    public float hitImpulseMultiplier = 0.35f;
    [Tooltip("넘어질 때 토크 배율")]
    [Range(0.05f, 1f)]
    public float hitTorqueMultiplier = 0.25f;

    private Rigidbody _rb;
    private GameObject _flame;
    private Vector3 _initialUpWorld;

    private bool _isHit;
    private bool _isScored;

    private void Awake()
    {
        _flame = FindInChildrenByName(gameObject.transform, flameObjectName);
        _initialUpWorld = transform.up;

        _rb = GetComponentInChildren<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit) return;

        var arrow = collision.collider.GetComponentInParent<ArcheryArrow>();
        if (arrow == null) return;

        _isHit = true;

        // Rigidbody 켜서 쓰러지게
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.constraints = RigidbodyConstraints.None;

            Vector3 contactPoint = (collision.contacts != null && collision.contacts.Length > 0)
                ? collision.contacts[0].point
                : transform.position;
            Vector3 impulse = collision.relativeVelocity.sqrMagnitude > 0.0001f
                ? collision.relativeVelocity
                : (collision.impulse.sqrMagnitude > 0.0001f ? collision.impulse : transform.forward);

            Vector3 forceDir = impulse.normalized;
            float forceMag = impulse.magnitude * hitImpulseMultiplier;
            _rb.AddForceAtPosition(forceDir * forceMag, contactPoint, ForceMode.Impulse);

            Vector3 torqueAxis = Vector3.Cross(_initialUpWorld, forceDir);
            if (torqueAxis.sqrMagnitude < 0.0001f) torqueAxis = Random.onUnitSphere;
            _rb.AddTorque(torqueAxis.normalized * (forceMag * hitTorqueMultiplier), ForceMode.Impulse);
        }

        // 화살은 맞은 뒤 바로 멈춤 (튀지 않도록)
        var arrowRb = arrow.GetComponent<Rigidbody>();
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = Vector3.zero;
            arrowRb.angularVelocity = Vector3.zero;
            arrowRb.isKinematic = true;
        }

        // 촛불 꺼짐 + 점수 1회
        ExtinguishAndScoreOnce();
    }

    private void ExtinguishAndScoreOnce()
    {
        if (_isScored) return;
        _isScored = true;

        if (_flame != null)
        {
            _flame.SetActive(false);
        }

        CandleScoreManager.Instance?.AddCandleScore();
    }

    private static GameObject FindInChildrenByName(Transform root, string name)
    {
        if (root == null) return null;

        if (root.name == name) return root.gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindInChildrenByName(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}

