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
    [Tooltip("시작 라이트 intensity")]
    public float minLightIntensity = 0f;

    [Tooltip("최대 라이트 intensity")]
    public float maxLightIntensity = 6f;


    [Header("Explosion Effect")]
    [Tooltip("맞았을 때 터지는 외부 VFX 프리팹 (예: CFXR Hit A)")]
    public GameObject hitEffectPrefab;

    [Tooltip("폭발 효과 사용 여부")]
    public bool useExplosion = true;

    private StarManager _owner;
    private bool _isHit;
    private float _elapsed;
    private Renderer _renderer;
    private Material _material;
    private Light _light;
    private Color _originalColor = Color.white;

    public void SetOwner(StarManager owner) => _owner = owner;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _material = _renderer.material;
            _originalColor = _material.color;
        }
        
        _light = GetComponentInChildren<Light>();
        if (_light != null && _originalColor != Color.white)
        {
            // 라이트 색상도 머티리얼 기본색에 맞춤
            _light.color = _originalColor;
        }

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
            Color c = _originalColor;
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
        StarScoreManager.Instance?.AddStarScore();

        // VFX
        if (useExplosion && hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, impactPoint, Quaternion.LookRotation(impactNormal));
        }

        // 같은 프레임에서 ArcheryArrow가 SetParent/물리처리를 하므로 즉시 Destroy해도 end-of-frame 처리로 안전
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // StarManager no longer tracks active stars
    }
}
