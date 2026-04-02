using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 03MenTarget: Y축 90°로 과녘을 보였다가 대기 후 초기 회전으로 복귀만 담당.
/// 직선 이동은 MenTargetManager가 처리.
/// </summary>
[DisallowMultipleComponent]
public class MenTargetBehavior : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("과녘 면이 보이도록 Yaw 회전량(도)")]
    public float faceRotationAngle = 90f;
    public float rotationSpeed = 6f;

    [Header("대기 (회전 시퀀스 사이)")]
    public float waitMinTime = 2f;
    public float waitMaxTime = 4f;

    private Quaternion _initialRotation;
    private bool _initialRotationLocked;
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
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    /// <summary>스폰 직후 MenTargetManager에서 호출</summary>
    public void Configure(MenTargetManager owner)
    {
        _manager = owner;
    }

    /// <summary>
    /// 매니저 스폰 직후 호출. Start보다 먼저 잡아 두어 스폰 포인트 회전과 무관하게
    /// 프리팹 기본 자세를 "초기"로 쓰고, 이동 후 faceRotation만 적용한다.
    /// </summary>
    public void CaptureInitialRotationForMovement()
    {
        _initialRotation = transform.rotation;
        _initialRotationLocked = true;
    }

    private void Start()
    {
        if (!_initialRotationLocked)
            _initialRotation = transform.rotation;
    }

    /// <summary>
    /// 이동이 끝난 뒤 매니저가 호출. 회전→대기→역회전 후 onComplete 실행.
    /// </summary>
    public void BeginRotationSequence(Action onComplete)
    {
        StopAllCoroutines();
        StartCoroutine(RotationSequenceRoutine(onComplete));
    }

    private IEnumerator RotationSequenceRoutine(Action onComplete)
    {
        Quaternion facingRotation = _initialRotation * Quaternion.Euler(0f, faceRotationAngle, 0f);
        while (Quaternion.Angle(transform.rotation, facingRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, facingRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = facingRotation;
        yield return new WaitForSeconds(UnityEngine.Random.Range(waitMinTime, waitMaxTime));

        while (Quaternion.Angle(transform.rotation, _initialRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, _initialRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = _initialRotation;
        onComplete?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit)
            return;

        if (!TryGetArrow(collision, out _))
            return;

        _isHit = true;
        StopAllCoroutines();
        _manager?.UnregisterMovementTarget(this);

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
        _manager?.UnregisterMovementTarget(this);
        _manager?.NotifyMenTargetDestroyed();
        _manager = null;
    }
}
