using UnityEngine;

/// <summary>
/// 03MenTarget 전용: 타겟이 화살에 맞으면 폭발 이펙트 후 제거하고 전역 ScoreManager에 점수 반영.
/// AppleScoreManager 등 다른 스테이지 매니저와 동일한 싱글톤 패턴.
/// </summary>
[DisallowMultipleComponent]
public class MenTargetScoreManager : MonoBehaviour
{
    private static MenTargetScoreManager _instance;

    public static MenTargetScoreManager Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            _instance = FindFirstObjectByType<MenTargetScoreManager>();
            if (_instance != null)
                return _instance;

            var go = new GameObject("MenTargetScoreManager");
            _instance = go.AddComponent<MenTargetScoreManager>();
            return _instance;
        }
    }

    [Header("Scoring")]
    [Tooltip("타겟 1개 명중당 올릴 점수")]
    public int pointsPerTarget = 1;

    [Header("Visual Effects")]
    public Color explosionColor = Color.white;
    public float explosionDuration = 1f;

    [Header("Sound Effects")]
    public AudioClip breakSound;

    private int _targetsHit;

    public int TargetsHit => _targetsHit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _targetsHit = 0;
    }

    /// <summary>overridePoints가 있으면 해당 점수, 없으면 pointsPerTarget</summary>
    public void AddMenTargetScore(int? overridePoints = null)
    {
        int add = overridePoints ?? pointsPerTarget;
        if (add <= 0)
            return;

        _targetsHit++;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(add);
        else
            Debug.LogWarning("[MenTargetScoreManager] ScoreManager.Instance is null. 점수 UI 갱신을 건너뜁니다.");
    }

    /// <summary>타겟 파괴 + 이펙트 + 점수</summary>
    public void OnTargetHit(GameObject target, Vector3 impactNormal)
    {
        if (target == null)
            return;

        Vector3 pos = target.transform.position;
        AddMenTargetScore();
        CreateExplosion(pos, impactNormal);

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, pos);

        Destroy(target);
        Debug.Log($"[MenTargetScoreManager] Target hit. Total: {_targetsHit}");
    }

    private void CreateExplosion(Vector3 position, Vector3 impactNormal)
    {
        var explosion = new GameObject("MenTargetExplosion");
        explosion.transform.position = position;

        var ps = explosion.AddComponent<ParticleSystem>();
        ps.Stop();

        ParticleSystem.MainModule main = ps.main;
        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.ShapeModule shape = ps.shape;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        main.duration = explosionDuration;
        main.startLifetime = 1f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.loop = false;
        main.startColor = explosionColor;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Mobile/Particles/Alpha Blended");
        if (shader != null)
        {
            renderer.material = new Material(shader);
            renderer.material.color = explosionColor;
        }

        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40, 60) });

        shape.shapeType = ParticleSystemShapeType.Sphere;

        if (impactNormal != Vector3.zero)
            explosion.transform.rotation = Quaternion.LookRotation(impactNormal);

        ps.Play();
        Destroy(explosion, main.duration + main.startLifetime.constantMax);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
