using UnityEngine;

public class ScarecrowBehavior : MonoBehaviour
{
    [Header("허수아비 점수 설정")]
    [SerializeField] private int score = 1;

    [Header("허수아비 흔들림 (피벗이 모델 바닥에 있어야 함)")]
    [SerializeField] private bool enableSway = true;
    [SerializeField, Min(0f)] private float swaySpeed = 0.9f;
    [SerializeField, Min(0f)] private float maxTiltDegrees = 4f;
    [Tooltip("앞뒤·좌우 주기를 약간 다르게 해 자연스럽게 만듭니다.")]
    [SerializeField, Min(0.01f)] private float lateralFrequencyFactor = 1.12f;
    [Tooltip("좌우 움직임 위상(라디안).")]
    [SerializeField] private float lateralPhaseRadians = 0.8f;

    public int Score => score;

    private Vector3 _initialLocalEuler;

    private void Awake()
    {
        _initialLocalEuler = transform.localEulerAngles;
    }

    private void LateUpdate()
    {
        if (!enableSway)
            return;

        float t = Time.time * swaySpeed;
        float tiltForwardBack = Mathf.Sin(t) * maxTiltDegrees;
        float tiltLeftRight = Mathf.Sin(t * lateralFrequencyFactor + lateralPhaseRadians) * maxTiltDegrees;

        Vector3 euler = _initialLocalEuler;
        euler.x += tiltForwardBack;
        euler.z += tiltLeftRight;
        transform.localEulerAngles = euler;
    }

    // 화살이 허수아비에 닿았을 때 화살을 멈추고 점수를 주는 역할
    private void OnCollisionEnter(Collision collision)
    {
        TryHandleArrowHit(collision.collider);
    }

    private void TryHandleArrowHit(Collider other)
    {

        // 점수 추가
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(score);
        }
    }
}
