using System.Collections;
using UnityEngine;

/// <summary>
/// 코코넛 개별 동작:
/// - 스폰 지점에서 서서히 자람(Scale lerp)
/// - 화살(ArcheryArrow)에 맞으면 점수 1회 + 중력/물리 활성화로 "실제처럼" 떨어짐
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class CoconutBehavior : MonoBehaviour
{
    [Header("Grow Settings")]
    [Tooltip("스폰 후 목표 크기까지 자라는 시간(초)")]
    public float growDuration = 1.2f;

    [Tooltip("성장 시작 스케일(0이면 완전 0 스케일)")]
    public float startScale = 0.02f;

    [Tooltip("성장 목표 스케일(프리팹 기본 스케일에 곱해짐)")]
    public float targetScaleMultiplier = 1.0f;

    [Header("Scoring")]
    [Tooltip("맞추면 올릴 점수")]
    public int points = 1;

    [Header("Fall Settings")]
    [Tooltip("맞았을 때 떨어지면서 약간의 회전을 주기 위한 토크 범위")]
    public float randomTorque = 2.0f;

    [Tooltip("맞았을 때 떨어지기 시작할 때 살짝 주는 임펄스(낙하가 너무 정적일 때 보정)")]
    public float extraImpulse = 0.6f;

    private CoconutManager _owner;
    private Rigidbody _rb;
    private Collider _col;
    private bool _isHit;
    private bool _isGrown;
    private Vector3 _baseScale;
    private Coroutine _growRoutine;

    public void SetOwner(CoconutManager owner) => _owner = owner;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        _baseScale = transform.localScale;

        // 스폰 직후는 매달린 상태: 물리 영향 최소화
        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        // 성장 시작
        if (_growRoutine == null)
            _growRoutine = StartCoroutine(GrowRoutine());
    }

    private IEnumerator GrowRoutine()
    {
        _isGrown = false;

        float start = Mathf.Max(0.0001f, startScale);
        Vector3 from = _baseScale * start;
        Vector3 to = _baseScale * Mathf.Max(0.0001f, targetScaleMultiplier);

        transform.localScale = from;

        float t = 0f;
        float dur = Mathf.Max(0.01f, growDuration);
        while (t < dur)
        {
            if (_isHit) yield break;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / dur);
            // 부드럽게 (ease-out)
            float eased = 1f - Mathf.Pow(1f - a, 3f);
            transform.localScale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }

        transform.localScale = to;
        _isGrown = true;
        _growRoutine = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit) return;

        ArcheryArrow arrow = null;
        if (collision.collider != null)
            arrow = collision.collider.GetComponentInParent<ArcheryArrow>();
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();

        if (arrow == null) return;

        HandleHit(arrow, collision.contacts != null && collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isHit) return;

        var arrow = other != null ? other.GetComponentInParent<ArcheryArrow>() : null;
        if (arrow == null) return;

        HandleHit(arrow, other.ClosestPoint(transform.position));
    }

    private void HandleHit(ArcheryArrow arrow, Vector3 hitPoint)
    {
        _isHit = true;

        if (_growRoutine != null)
        {
            StopCoroutine(_growRoutine);
            _growRoutine = null;
        }

        CoconutScoreManager.Instance?.AddCoconutScore(points);

        BeginFall(hitPoint);
    }

    private void BeginFall(Vector3 hitPoint)
    {
        // 떨어지기 시작: 물리 활성화
        _rb.isKinematic = false;
        _rb.useGravity = true;

        // 매달린 상태에서 정지한 물체가 자연스럽게 떨어지도록 약간의 힘/회전
        Vector3 dir = (transform.position - hitPoint);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
        dir.Normalize();

        float impulse = Mathf.Max(0f, extraImpulse);
        if (impulse > 0f)
            _rb.AddForce((Vector3.down + dir * 0.25f) * impulse, ForceMode.Impulse);

        float torque = Mathf.Max(0f, randomTorque);
        if (torque > 0f)
        {
            Vector3 randomAxis = Random.onUnitSphere;
            _rb.AddTorque(randomAxis * torque, ForceMode.Impulse);
        }

        // 충돌이 잘 일어나도록 콜라이더는 트리거가 아니게(혹시 프리팹이 trigger면)
        if (_col != null && _col.isTrigger)
            _col.isTrigger = false;
    }

    private void OnDestroy()
    {
        _owner?.NotifyCoconutDestroyed(this);
    }
}

