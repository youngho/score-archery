using UnityEngine;
using System.Collections;

/// <summary>
/// 03MenTarget: 스폰 지점에서 일정 방향·거리로 이동(옆면), 임의 지점에서 Y축 90° 돌아 과녘을 보여 준 뒤,
/// 대기 후 -90° 복귀하여 시작 위치로 돌아가 제거됨. 화살(ArcheryArrow)에 맞으면 파괴되며 점수 처리.
/// </summary>
[DisallowMultipleComponent]
public class MenTargetBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveDistance = 5f;
    public float moveSpeed = 2f;
    [Tooltip("Configure() 전 기본값. 스폰 시 스폰 포워드 또는 매니저 값으로 덮어씀")]
    public Vector3 moveDirection = Vector3.right;

    [Header("Rotation Settings")]
    [Tooltip("과녘 면이 보이도록 Yaw 회전량(도)")]
    public float faceRotationAngle = 90f;
    public float rotationSpeed = 6f;

    [Header("Behavior Settings")]
    [Tooltip("이동 구간 중 멈출 위치 비율(랜덤)")]
    public float randomStopMin = 0.2f;
    public float randomStopMax = 0.85f;
    public float waitMinTime = 2f;
    public float waitMaxTime = 4f;

    private Vector3 _startPosition;
    private Quaternion _initialRotation;
    private bool _isHit;
    private MenTargetManager _manager;

    private void Awake()
    {
        EnsurePhysicsForHit();
    }

    private void EnsurePhysicsForHit()
    {
        if (GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.08f);
        }

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// <summary>생성 직후 MenTargetManager에서 호출 (Start 전에 호출됨)</summary>
    public void Configure(MenTargetManager owner, float distance, float speed, Vector3 worldMoveDirection)
    {
        _manager = owner;
        moveDistance = distance;
        moveSpeed = speed;
        moveDirection = worldMoveDirection.normalized;
        if (moveDirection.sqrMagnitude < 0.0001f)
            moveDirection = Vector3.forward;
    }

    private void Start()
    {
        _startPosition = transform.position;
        _initialRotation = transform.rotation;
        StartCoroutine(TargetRoutine());
    }

    private IEnumerator TargetRoutine()
    {
        Vector3 dir = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : transform.forward;

        float stopT = Random.Range(
            Mathf.Min(randomStopMin, randomStopMax),
            Mathf.Max(randomStopMin, randomStopMax));
        float targetStopDistance = moveDistance * stopT;
        float traveled = 0f;

        while (traveled < targetStopDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.position += dir * step;
            traveled += step;
            yield return null;
        }

        Quaternion facingRotation = _initialRotation * Quaternion.Euler(0f, faceRotationAngle, 0f);
        while (Quaternion.Angle(transform.rotation, facingRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, facingRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = facingRotation;
        yield return new WaitForSeconds(Random.Range(waitMinTime, waitMaxTime));

        while (Quaternion.Angle(transform.rotation, _initialRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, _initialRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = _initialRotation;

        while (Vector3.Distance(transform.position, _startPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _startPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = _startPosition;
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit)
            return;

        if (!TryGetArrow(collision, out _))
            return;

        _isHit = true;
        StopAllCoroutines();

        Vector3 impactNormal = Vector3.up;
        if (collision.contactCount > 0)
            impactNormal = collision.GetContact(0).normal;

        if (MenTargetScoreManager.Instance != null)
            MenTargetScoreManager.Instance.OnTargetHit(gameObject, impactNormal);
        else
            Destroy(gameObject);
    }

    private static bool TryGetArrow(Collision collision, out ArcheryArrow arrow)
    {
        arrow = collision.collider != null
            ? collision.collider.GetComponentInParent<ArcheryArrow>()
            : null;
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();
        return arrow != null;
    }

    private void OnDestroy()
    {
        _manager?.NotifyMenTargetDestroyed();
        _manager = null;
    }
}
