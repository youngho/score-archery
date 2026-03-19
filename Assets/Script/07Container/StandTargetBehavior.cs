using UnityEngine;

/// <summary>
/// 07Container 씬용 스탠드 표적 개별 동작.
/// - 화살(ArcheryArrow)이 충돌한 위치를 기준으로 중심점까지의 거리로 서클링을 판정
/// - StandTargetManager/StandTargetScoreManager 와 연동해 링별로 다른 점수를 부여
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class StandTargetBehavior : MonoBehaviour
{
    [Header("Center / Ring Settings")]
    [Tooltip("서클 중심 기준이 될 트랜스폼 (없으면 이 오브젝트의 Transform 사용)")]
    public Transform centerPoint;

    [System.Serializable]
    public class RingScore
    {
        [Tooltip("에디터용 이름 (예: Inner, Middle, Outer)")]
        public string ringName = "Ring";

        [Tooltip("이 링의 최대 반지름 (centerPoint 기준, 월드 좌표)")]
        public float radius = 0.1f;

        [Tooltip("이 링을 맞췄을 때 획득할 점수")]
        public int score = 1;
    }

    [Tooltip("안쪽 링부터 바깥쪽 링 순서대로 설정")]
    public RingScore[] ringScores;

    [Header("Hit Settings")]
    [Tooltip("한 번 점수 처리 후에는 다시 맞아도 점수를 주지 않음")]
    public bool scoreOnlyOnce = true;

    [Tooltip("화살이 맞았을 때 표적에 붙여서 멈출지 여부")]
    public bool stickArrowOnHit = true;

    private bool _alreadyScored;

    private void Reset()
    {
        // 기본 링 세트 예시
        ringScores = new[]
        {
            new RingScore { ringName = "Inner", radius = 0.05f, score = 10 },
            new RingScore { ringName = "Middle", radius = 0.10f, score = 5 },
            new RingScore { ringName = "Outer", radius = 0.15f, score = 3 },
        };
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 이미 점수 처리한 경우
        if (scoreOnlyOnce && _alreadyScored)
            return;

        var arrow = collision.collider != null
            ? collision.collider.GetComponentInParent<ArcheryArrow>()
            : null;
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();

        if (arrow == null)
            return;

        // 충돌 지점
        Vector3 hitPoint = transform.position;
        if (collision.contacts != null && collision.contacts.Length > 0)
            hitPoint = collision.contacts[0].point;

        int gainedScore = CalculateRingScore(hitPoint, out string ringName);
        if (gainedScore <= 0)
            return;

        if (scoreOnlyOnce)
            _alreadyScored = true;

        // 화살 멈추기/붙이기
        if (stickArrowOnHit)
            StickArrow(arrow, hitPoint);

        // 점수 반영
        StandTargetScoreManager.Instance?.AddStandTargetScore(gainedScore, ringName);
    }

    private int CalculateRingScore(Vector3 hitPoint, out string ringName)
    {
        ringName = null;

        if (ringScores == null || ringScores.Length == 0)
            return 0;

        Transform center = centerPoint != null ? centerPoint : transform;

        // 월드 좌표에서 중심까지의 거리
        float distance = Vector3.Distance(hitPoint, center.position);

        // 안쪽 링부터 검사
        for (int i = 0; i < ringScores.Length; i++)
        {
            var ring = ringScores[i];
            if (ring == null) continue;
            if (ring.radius <= 0f) continue;

            if (distance <= ring.radius)
            {
                ringName = ring.ringName;
                return ring.score;
            }
        }

        // 어떤 링에도 포함되지 않으면 0점
        return 0;
    }

    private void StickArrow(ArcheryArrow arrow, Vector3 hitPoint)
    {
        var arrowRb = arrow.GetComponent<Rigidbody>();
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = Vector3.zero;
            arrowRb.angularVelocity = Vector3.zero;
            arrowRb.isKinematic = true;
        }

        // 살짝 표적에 붙도록 위치/부모 조정
        arrow.transform.position = hitPoint;
        arrow.transform.SetParent(transform, true);
    }
}

