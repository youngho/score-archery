using UnityEngine;

/// <summary>
/// 별 개별 동작:
/// - 생성 시 아주 작게 시작
/// - 점점 커지며(스케일↑) 밝아짐(라이트 intensity↑)
/// - 최고 크기를 지나면 다시 작아지며 어두워짐(밝기↓) + 소멸
/// - 화살(ArcheryArrow)에 맞으면 폭발 VFX + 점수 1회 + 즉시 소멸
/// </summary>
[DisallowMultipleComponent]
public class StarBehavior : MonoBehaviour
{
    [Header("Star Life Cycle")]
    [Tooltip("시작 스케일 (점처럼)")]
    public float minScale = 0.05f;

    [Tooltip("최대 스케일")]
    public float maxScale = 0.30f;

    [Tooltip("최대까지 커지는 시간")]
    public float growDuration = 2.5f;

    [Tooltip("커졌다가 작아지는 시간")]
    public float shrinkDuration = 2.5f;

    [Header("Visual Brightness")]
    [Tooltip("별 색(라이트/폭발 파티클에 사용)")]
    public Color starColor = Color.cyan;

    [Tooltip("시작 라이트 intensity")]
    public float minLightIntensity = 0f;

    [Tooltip("최대 라이트 intensity")]
    public float maxLightIntensity = 6f;

    [Header("Scoring")]
    [Tooltip("화살이 맞추면 올릴 점수 (StarScoreManager에서 반영)")]
    public int points = 1;

    [Header("Explosion Effect")]
    public bool useExplosion = true;
    public float explosionDuration = 0.12f;
    public float explosionLifetime = 0.25f;
    public float explosionRadius = 0.06f;
    public int explosionParticleCount = 30;
    public float explosionSpeedMin = 2.0f;
    public float explosionSpeedMax = 6.0f;

    private StarManager _owner;
    private bool _isHit;
    private float _elapsed;
    private Renderer _renderer;
    private Material _material;
    private Light _light;

    public void SetOwner(StarManager owner) => _owner = owner;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _material = _renderer != null ? _renderer.material : null;
        _light = GetComponentInChildren<Light>();

        // InstantiatePrimitive 기준으로, 씬에서 잘못된 초기 스케일이 들어가도 안전하게 고정
        transform.localScale = Vector3.one * minScale;
    }

    private void Start()
    {
        _elapsed = 0f;
        transform.localScale = Vector3.one * minScale;
        ApplyBrightness01(0f);
    }

    private void Update()
    {
        if (_isHit) return;

        _elapsed += Time.deltaTime;

        float total = Mathf.Max(0.0001f, growDuration + shrinkDuration);
        if (_elapsed >= total)
        {
            Destroy(gameObject);
            return;
        }

        float brightness01;
        float scale;

        if (_elapsed <= growDuration)
        {
            float t = growDuration <= 0f ? 1f : (_elapsed / growDuration);
            brightness01 = Mathf.Clamp01(t);
            scale = Mathf.Lerp(minScale, maxScale, t);
        }
        else
        {
            float t = shrinkDuration <= 0f ? 1f : ((_elapsed - growDuration) / shrinkDuration);
            brightness01 = Mathf.Clamp01(1f - t);
            scale = Mathf.Lerp(maxScale, minScale, t);
        }

        transform.localScale = Vector3.one * scale;
        ApplyBrightness01(brightness01);
    }

    private void ApplyBrightness01(float brightness01)
    {
        // 라이트로 "밝아짐" 처리
        if (_light != null)
        {
            float intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, brightness01);
            _light.intensity = intensity;
        }

        // 머티리얼 컬러는 있는 경우에만 간단히 조절
        if (_material != null)
        {
            Color c = starColor;
            c.a = Mathf.Clamp01(brightness01);
            // 일부 셰이더는 alpha가 의미가 없을 수 있어도 안전하게 SetColor만 수행
            _material.color = c * Mathf.Lerp(0.2f, 1.2f, brightness01);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isHit) return;

        // ArcheryArrow에서 Rigidbody 충돌을 멈추기 때문에, 별 쪽도 Collision으로 처리
        ArcheryArrow arrow = collision.collider.GetComponentInParent<ArcheryArrow>();
        if (arrow == null)
        {
            arrow = collision.gameObject.GetComponentInParent<ArcheryArrow>();
        }

        if (arrow == null) return;

        _isHit = true;

        Vector3 impactPoint = transform.position;
        Vector3 impactNormal = Vector3.up;
        if (collision.contacts != null && collision.contacts.Length > 0)
        {
            impactPoint = collision.contacts[0].point;
            impactNormal = collision.contacts[0].normal;
        }

        // 점수 1회 반영
        StarScoreManager.Instance?.AddStarScore(points);

        // VFX
        if (useExplosion)
        {
            CreateStarExplosion(impactPoint, impactNormal);
        }

        // 같은 프레임에서 ArcheryArrow가 SetParent/물리처리를 하므로 즉시 Destroy해도 end-of-frame 처리로 안전
        Destroy(gameObject);
    }

    private void CreateStarExplosion(Vector3 position, Vector3 impactNormal)
    {
        GameObject explosion = new GameObject("StarExplosion");
        explosion.transform.position = position;

        // 파티클 방향을 대략 충돌 노멀 쪽으로 맞춤 (없는 경우 identity)
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
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startColor = starColor;

        // 0초에 Burst로 즉시 터뜨림
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
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
                renderer.material.color = starColor;
            }
        }

        ps.Play();

        float lifetimeMax = explosionLifetime; // main.startLifetime은 float으로 세팅되므로 상한도 동일 취급
        Destroy(explosion, explosionDuration + lifetimeMax + 0.1f);
    }

    private void OnDestroy()
    {
        _owner?.NotifyStarDestroyed(this);
    }
}

