using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 07Container 씬용 스탠드 표적 개별 동작.
/// - 화살(ArcheryArrow) 충돌 지점을 <b>과녁 면</b>에 투영한 뒤, 중심까지의 반경으로 링을 판정 (기울어진 표적에 적합)
/// - StandTargetManager / StandTargetScoreManager 와 연동
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class StandTargetBehavior : MonoBehaviour
{
    [Header("Center / Face")]
    [Tooltip("과녁 면 위의 중심(황심). 회전은 면이 놓인 방향과 같아야 함 (로컬 forward가 면의 법선)")]
    public Transform centerPoint;

    [Tooltip("true면 법선을 -centerPoint.forward 로 사용 (모델이 반대로 잡힌 경우)")]
    public bool invertFaceNormal;

    [System.Serializable]
    public class RingScore
    {
        [Tooltip("에디터용 이름")]
        public string ringName = "Ring";

        [Tooltip("이 링 경계까지의 최대 반경 — 과녁 <b>면</b> 위에서 중심까지의 거리(미터). 안쪽 링부터 작은 값으로 나열")]
        public float radius = 0.1f;

        [Tooltip("이 링을 맞췄을 때 점수")]
        public int score = 1;
    }

    [Tooltip("황심(가장 안쪽)부터 바깥 링 순으로 반경 오름차순 정렬")]
    public RingScore[] ringScores;

    [Header("Scene (에디터 기즈모)")]
    [Tooltip("Scene 뷰에 점수 판정과 같은 면·반경으로 링을 원형으로 표시")]
    public bool drawScoreRingsInScene = true;

    [Tooltip("켜면 이 오브젝트를 선택했을 때만 링을 그림")]
    public bool drawRingsOnlyWhenSelected;

    [Tooltip("면 법선 방향으로 짧게 그려서 과녁이 어느 쪽을 바라보는지 확인")]
    public bool drawFaceNormalGizmo = true;

    [Tooltip("각 링 근처에 점수 숫자 표시 (에디터에서만)")]
    public bool showRingScoreLabels = true;

    [Range(12, 96)]
    [Tooltip("원을 몇 각형으로 근사할지")]
    public int gizmoRingSegments = 48;

    [Header("Hit Settings")]
    [Tooltip("한 번 점수 처리 후에는 다시 맞아도 점수를 주지 않음")]
    public bool scoreOnlyOnce = true;

    [Tooltip("화살이 맞았을 때 표적에 붙여서 멈출지 여부")]
    public bool stickArrowOnHit = true;

    private bool _alreadyScored;

    private void Reset()
    {
        ringScores = new[]
        {
            new RingScore { ringName = "10 (황)", radius = 0.05f, score = 10 },
            new RingScore { ringName = "9", radius = 0.11f, score = 9 },
            new RingScore { ringName = "8", radius = 0.16f, score = 8 },
            new RingScore { ringName = "7", radius = 0.22f, score = 7 },
            new RingScore { ringName = "6", radius = 0.28f, score = 6 },
            new RingScore { ringName = "5", radius = 0.33f, score = 5 },
            new RingScore { ringName = "4", radius = 0.38f, score = 4 },
            new RingScore { ringName = "3", radius = 0.44f, score = 3 },
            new RingScore { ringName = "2", radius = 0.5f, score = 2 },
            new RingScore { ringName = "1 (바깥)", radius = 0.56f, score = 1 },
        };
    }

    /// <summary>
    /// 과녁 면 법선(단위 벡터, 화살이 맞는 쪽으로 향함).
    /// </summary>
    private Vector3 GetFaceNormalWorld()
    {
        Transform t = centerPoint != null ? centerPoint : transform;
        Vector3 n = t.forward;
        if (invertFaceNormal)
            n = -n;
        return n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.forward;
    }

    /// <summary>
    /// 월드 좌표 hit를 과녁 면(중심을 지나 법선에 수직인 평면)에 투영한 뒤, 중심까지의 거리.
    /// </summary>
    private float GetRadialDistanceOnFace(Vector3 hitPointWorld, Vector3 centerWorld, Vector3 faceNormal)
    {
        Vector3 toHit = hitPointWorld - centerWorld;
        Vector3 onPlane = Vector3.ProjectOnPlane(toHit, faceNormal);
        return onPlane.magnitude;
    }

    private static void GetPlaneTangentAxes(Vector3 planeNormal, out Vector3 axisU, out Vector3 axisV)
    {
        Vector3 n = planeNormal.normalized;
        axisU = Vector3.Cross(n, Vector3.up);
        if (axisU.sqrMagnitude < 1e-6f)
            axisU = Vector3.Cross(n, Vector3.right);
        axisU.Normalize();
        axisV = Vector3.Cross(n, axisU).normalized;
    }

    private void DrawScoreRingGizmos()
    {
        if (ringScores == null || ringScores.Length == 0)
            return;

        Transform centerTf = centerPoint != null ? centerPoint : transform;
        Vector3 center = centerTf.position;
        Vector3 normal = GetFaceNormalWorld();
        GetPlaneTangentAxes(normal, out Vector3 axisU, out Vector3 axisV);

        if (drawFaceNormalGizmo)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
            Gizmos.DrawLine(center, center + normal * 0.12f);
        }

        Gizmos.matrix = Matrix4x4.identity;
        int segments = Mathf.Clamp(gizmoRingSegments, 12, 96);

        // 바깥부터 그려 안쪽 링 선이 위에 보이도록
        for (int idx = ringScores.Length - 1; idx >= 0; idx--)
        {
            var ring = ringScores[idx];
            if (ring == null || ring.radius <= 0f)
                continue;

            float t = ringScores.Length > 1 ? (float)idx / (ringScores.Length - 1) : 0f;
            var c = Color.Lerp(new Color(1f, 0.82f, 0.15f), new Color(0.35f, 0.55f, 1f), t);
            c.a = 0.88f;
            Gizmos.color = c;

            Vector3 prev = center + axisU * ring.radius;
            for (int s = 1; s <= segments; s++)
            {
                float ang = (float)s / segments * Mathf.PI * 2f;
                Vector3 p = center + (axisU * Mathf.Cos(ang) + axisV * Mathf.Sin(ang)) * ring.radius;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }

#if UNITY_EDITOR
            if (showRingScoreLabels)
            {
                float labelAng = (float)idx / Mathf.Max(1, ringScores.Length) * Mathf.PI * 2f + 0.4f;
                Vector3 labelDir = (axisU * Mathf.Cos(labelAng) + axisV * Mathf.Sin(labelAng)).normalized;
                Vector3 labelAt = center + labelDir * (ring.radius * 0.94f);
                Handles.Label(labelAt, ring.score.ToString(), EditorStyles.boldLabel);
            }
#endif
        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.95f);
        float ch = 0.018f;
        Gizmos.DrawLine(center - axisU * ch, center + axisU * ch);
        Gizmos.DrawLine(center - axisV * ch, center + axisV * ch);
    }

    private void OnDrawGizmos()
    {
        if (!drawScoreRingsInScene || drawRingsOnlyWhenSelected)
            return;
        DrawScoreRingGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawScoreRingsInScene || !drawRingsOnlyWhenSelected)
            return;
        DrawScoreRingGizmos();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (scoreOnlyOnce && _alreadyScored)
            return;

        var arrow = collision.collider != null
            ? collision.collider.GetComponentInParent<ArcheryArrow>()
            : null;
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();

        if (arrow == null)
            return;

        Vector3 hitPoint = transform.position;
        if (collision.contacts != null && collision.contacts.Length > 0)
            hitPoint = collision.contacts[0].point;

        int gainedScore = CalculateRingScore(hitPoint, out string ringName);
        if (gainedScore <= 0)
            return;

        if (scoreOnlyOnce)
            _alreadyScored = true;

        if (stickArrowOnHit)
            StickArrow(arrow, hitPoint);

        StandTargetScoreManager.Instance?.AddStandTargetScore(gainedScore, ringName);
    }

    private int CalculateRingScore(Vector3 hitPoint, out string ringName)
    {
        ringName = null;

        if (ringScores == null || ringScores.Length == 0)
            return 0;

        Transform centerTf = centerPoint != null ? centerPoint : transform;
        Vector3 centerWorld = centerTf.position;
        Vector3 normal = GetFaceNormalWorld();
        float radial = GetRadialDistanceOnFace(hitPoint, centerWorld, normal);

        for (int i = 0; i < ringScores.Length; i++)
        {
            var ring = ringScores[i];
            if (ring == null) continue;
            if (ring.radius <= 0f) continue;

            if (radial <= ring.radius)
            {
                ringName = ring.ringName;
                return ring.score;
            }
        }

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

        arrow.transform.position = hitPoint;
        arrow.transform.SetParent(transform, true);
    }
}
