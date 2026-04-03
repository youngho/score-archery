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
            // Unity는 isKinematic Rigidbody에 대해 angularVelocity 설정을 지원하지 않습니다.
            // 따라서 velocity 0 설정을 kinematic 전환보다 먼저 합니다.
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
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
            _rb.constraints = RigidbodyConstraints.None;

            // 물리 엔진이 이미 계산한 충격량(Impulse)을 그대로 전달하여 자연스럽게 밀려나게 함
            _rb.AddForce(collision.impulse, ForceMode.Impulse);
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

