using UnityEngine;
using TMPro;

public class SnowmanScoreManager : MonoBehaviour
{
    public static SnowmanScoreManager Instance { get; private set; }

    [Header("UI Settings")]
    public TextMeshProUGUI snowmanScoreText;

    [Header("Visual Effects")]
    public Color snowColor = Color.white;
    public float explosionDuration = 1.0f;

    [Header("Sound Effects")]
    public AudioClip breakSound;

    private int _snowmenHit = 0;

    public int SnowmenHit => _snowmenHit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void OnSnowmanHit(GameObject snowman, Vector3 impactNormal)
    {
        _snowmenHit++;
        
        // Increase global score via ScoreManager
        if (ScoreManager.Instance != null)
        {
            // Give 10 points for a snowman as they are harder/different from balloons
            ScoreManager.Instance.AddScore(10);
        }

        UpdateUI();
        CreateSnowExplosion(snowman.transform.position, impactNormal);
        
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, snowman.transform.position);
        }

        Destroy(snowman);
        Debug.Log($"[SnowmanScoreManager] Snowman hit! Total: {_snowmenHit}");
    }

    private void CreateSnowExplosion(Vector3 position, Vector3 impactNormal)
    {
        GameObject explosion = new GameObject("SnowExplosion");
        explosion.transform.position = position;

        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        main.duration = explosionDuration;
        main.startLifetime = 1.0f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.loop = false;
        main.startColor = snowColor;

        // Try to find a simple particle shader
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");
        if (shader != null)
        {
            renderer.material = new Material(shader);
            renderer.material.color = snowColor;
        }

        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 40, 60) });

        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        
        // If we have an impact normal, point the explosion towards where the arrow came from
        if (impactNormal != Vector3.zero)
        {
            explosion.transform.rotation = Quaternion.LookRotation(impactNormal);
        }

        ps.Play();
        Destroy(explosion, main.duration + main.startLifetime.constantMax);
    }

    private void UpdateUI()
    {
        if (snowmanScoreText != null)
        {
            snowmanScoreText.text = _snowmenHit.ToString();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
