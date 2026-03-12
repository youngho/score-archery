using UnityEngine;

public class StrawEffigy : MonoBehaviour
{
    [Header("허수아비 점수 설정")]
    [SerializeField] private int score = 10;

    public int Score => score;

    // 화살이 허수아비에 닿았을 때 화살을 멈추고 점수를 주는 역할
    private void OnCollisionEnter(Collision collision)
    {
        TryHandleArrowHit(collision.collider, collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandleArrowHit(other, null);
    }

    private void TryHandleArrowHit(Collider other, Collision collision)
    {
        // 화살(ArcheryArrow)인지 확인
        ArcheryArrow arrow = other.GetComponentInParent<ArcheryArrow>();
        if (arrow == null) return;

        // 화살의 Rigidbody / Collider 가져오기
        Rigidbody arrowRb = arrow.GetComponent<Rigidbody>();
        Collider arrowCol = arrow.GetComponent<Collider>();

        // 충돌 지점 기준으로 위치/회전 정렬 (Collision 정보가 있을 때만)
        if (collision != null && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            arrow.transform.position = contact.point;
            arrow.transform.rotation = Quaternion.LookRotation(-contact.normal, Vector3.up);
        }

        // 화살 물리 멈추기
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = Vector3.zero;
            arrowRb.angularVelocity = Vector3.zero;
            arrowRb.isKinematic = true;
        }

        // 더 이상 다른 것과 충돌하지 않도록 콜라이더 비활성화
        if (arrowCol != null)
        {
            arrowCol.enabled = false;
        }

        // 허수아비에 붙이기
        arrow.transform.SetParent(transform);

        // 점수 추가
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(score);
        }
    }
}
