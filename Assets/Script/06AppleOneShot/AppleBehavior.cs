using UnityEngine;

/// <summary>
/// 사과 개별 동작:
/// - 화살(ArcheryArrow)에 맞으면 폭발 VFX + 점수 1회 + 즉시 파괴
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class AppleBehavior : MonoBehaviour
{
    [Header("Scoring")]
    [Tooltip("맞추면 올릴 점수 (AppleScoreManager에서 override 가능)")]
    public int points = 1;

    [Header("Explosion Effect")]
    public bool useExplosion = true;
    [Tooltip("사과 폭발 색상")]
    public Color explosionColor = new Color(1.0f, 0.25f, 0.25f, 1f);
    [Tooltip("폭발 파티클 시스템 재생 시간")]
    public float explosionDuration = 0.12f;
    [Tooltip("각 파티클 생존 시간")]
    public float explosionLifetime = 0.35f;
    [Tooltip("폭발 반경")]
    public float explosionRadius = 0.18f;
    [Tooltip("폭발 파티클 개수")]
    public int explosionParticleCount = 50;
    [Tooltip("파티클 최소 속도")]
    public float explosionSpeedMin = 2.0f;
    [Tooltip("파티클 최대 속도")]
    public float explosionSpeedMax = 7.0f;

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

        Vector3 impactPoint = transform.position;
        Vector3 impactNormal = Vector3.up;
        if (collision.contacts != null && collision.contacts.Length > 0)
        {
            impactPoint = collision.contacts[0].point;
            impactNormal = collision.contacts[0].normal;
        }

        // 점수 반영
        AppleScoreManager.Instance?.AddAppleScore(points);

        // 폭발 VFX
        if (useExplosion)
        {
            CreateAppleExplosion(impactPoint, impactNormal);
        }

        // 사과 제거
        Destroy(gameObject);
    }

    private void CreateAppleExplosion(Vector3 position, Vector3 impactNormal)
    {
        GameObject explosion = new GameObject("AppleExplosion");
        explosion.transform.position = position;

        if (impactNormal.sqrMagnitude > 0.0001f)
        {
            explosion.transform.rotation = Quaternion.LookRotation(impactNormal);
        }

        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;

        main.duration = explosionDuration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = explosionLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(explosionSpeedMin, explosionSpeedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.startColor = explosionColor;

        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)explosionParticleCount, (short)explosionParticleCount)
        });

        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = explosionRadius;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Particles/Additive");
            if (shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = explosionColor;
            }
        }

        ps.Play();

        Destroy(explosion, explosionDuration + explosionLifetime + 0.1f);
    }

    private void OnDestroy()
    {
        _owner?.NotifyAppleDestroyed(this);
    }
}

