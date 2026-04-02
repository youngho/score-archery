using UnityEngine;

/// <summary>
/// 사과 개별 동작:
/// - 화살(ArcheryArrow)에 맞으면 점수 1회 (오브젝트 유지, 관통은 ArcheryArrow가 박힘·RB 제거 처리)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class AppleBehavior : MonoBehaviour
{
    [Header("Scoring")]
    [Tooltip("맞추면 올릴 점수 (AppleScoreManager에서 override 가능)")]
    public int points = 1;

    private AppleManager _owner;
    private bool _isHit;

    public void SetOwner(AppleManager owner) => _owner = owner;

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit) return;

        var arrow = collision.collider != null
            ? collision.collider.GetComponentInParent<ArcheryArrow>()
            : null;
        if (arrow == null && collision.gameObject != null)
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();

        if (arrow == null) return;

        _isHit = true;

        AppleScoreManager.Instance?.AddAppleScore(points);
        _owner?.NotifyAppleHit(this);
    }

    private void OnDestroy()
    {
        _owner?.NotifyAppleDestroyed(this);
    }
}
