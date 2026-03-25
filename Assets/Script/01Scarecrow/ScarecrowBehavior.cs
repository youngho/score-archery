using UnityEngine;

public class ScarecrowBehavior : MonoBehaviour
{
    [Header("허수아비 점수 설정")]
    [SerializeField] private int score = 1;

    public int Score => score;

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
