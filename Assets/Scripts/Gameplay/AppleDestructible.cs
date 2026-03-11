#nullable enable
using UnityEngine;

public sealed class AppleDestructible : MonoBehaviour
{
    [Header("Swap Prefab")]
    [SerializeField] private GameObject destroyedPrefab;

    [Header("Hit Filter")]
    [SerializeField] private string arrowTag = "Arrow";
    [SerializeField] private float minImpactSpeed = 2.0f;

    [Header("Explosion")]
    [SerializeField] private float explosionForce = 6f;
    [SerializeField] private float explosionRadius = 1.2f;
    [SerializeField] private float upModifier = 0.2f;

    [Header("Cleanup")]
    [SerializeField] private float destroyedLifetime = 3f;

    private bool _broken;

    private void OnCollisionEnter(Collision collision)
    {
        if (_broken) return;

        var other = collision.collider;
        if (!string.IsNullOrEmpty(arrowTag) && !other.CompareTag(arrowTag))
            return;

        var speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed)
            return;

        var hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        var hitDir = collision.relativeVelocity.sqrMagnitude > 0.0001f ? collision.relativeVelocity.normalized : (other.transform.position - transform.position).normalized;

        Break(hitPoint, hitDir);
    }

    public void Break(Vector3 hitPoint, Vector3 hitDir)
    {
        if (_broken) return;
        _broken = true;

        if (!destroyedPrefab)
        {
            Destroy(gameObject);
            return;
        }

        var go = Instantiate(destroyedPrefab, transform.position, transform.rotation);

        foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
        {
            rb.AddExplosionForce(explosionForce, hitPoint, explosionRadius, upModifier, ForceMode.Impulse);
            rb.AddForce(hitDir * (explosionForce * 0.3f), ForceMode.Impulse);
        }

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Play(true);
        }

        Destroy(go, destroyedLifetime);
        Destroy(gameObject);
    }
}
