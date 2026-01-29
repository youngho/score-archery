using UnityEngine;
using System.Collections.Generic;

public class BalloonBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float upwardSpeed = 2.0f;
    public float horizontalVelocity = 0.0f;
    public float horizontalDamping = 0.5f;
    public float destroyHeight = 10.0f;
    public float driftAmount = 0.5f;
    public float driftSpeed = 1.0f;

    [Header("Separation Settings")]
    public float minSeparationDistance = 3.0f; // 풍선 간 최소 유지 거리
    public float separationForce = 8.0f;       // 점진적 밀어내기 힘
    public float hardPushMultiplier = 2.0f;    // 겹침 시 즉시 밀어내기 배율

    [Header("Sound")]
    [Tooltip("풍선이 터질 때 랜덤으로 재생할 효과음 (예: balloonpop1, 2, 3)")]
    public AudioClip[] popSounds;

    private float _randomDriftOffset;
    private float _balloonRadius;              // 이 풍선의 반지름 (스케일 기반)

    // 활성 풍선 목록 (정적으로 관리)
    private static List<BalloonBehavior> _activeBalloons = new List<BalloonBehavior>();

    void Start()
    {
        _randomDriftOffset = Random.Range(0f, 100f);
        _activeBalloons.Add(this);
        
        // 풍선 반지름 계산 (스케일 기반)
        _balloonRadius = transform.localScale.x * 0.5f;
    }

    void OnDestroy()
    {
        _activeBalloons.Remove(this);
    }

    void Update()
    {
        // 먼저 겹침 해소 (즉시 위치 보정)
        ResolveOverlaps();
        
        // Upward movement
        Vector3 movement = Vector3.up * upwardSpeed * Time.deltaTime;

        // Diagonal movement (inward launch)
        movement.x += horizontalVelocity * Time.deltaTime;
        
        // Dampen horizontal velocity over time
        horizontalVelocity = Mathf.Lerp(horizontalVelocity, 0f, horizontalDamping * Time.deltaTime);

        // Subtle horizontal drift
        float drift = Mathf.Sin(Time.time * driftSpeed + _randomDriftOffset) * driftAmount * Time.deltaTime;
        movement.x += drift;

        // 풍선 간 분리 (점진적 밀어내기)
        Vector3 separationMovement = CalculateSeparation();
        movement += separationMovement * Time.deltaTime;

        transform.Translate(movement, Space.World);

        // Auto-destruction when reaching peak height
        if (transform.position.y > destroyHeight)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 실제로 겹친 풍선들을 즉시 밀어냄 (하드 푸시)
    /// </summary>
    private void ResolveOverlaps()
    {
        foreach (BalloonBehavior other in _activeBalloons)
        {
            if (other == this || other == null) continue;

            Vector3 toThis = transform.position - other.transform.position;
            float distance = toThis.magnitude;
            
            // 두 풍선의 반지름 합 (겹침 기준)
            float combinedRadius = _balloonRadius + other._balloonRadius;
            float minDistance = Mathf.Max(combinedRadius, minSeparationDistance);

            // 실제로 겹치거나 너무 가까운 경우 즉시 밀어냄
            if (distance < minDistance && distance > 0.001f)
            {
                float overlap = minDistance - distance;
                Vector3 pushDirection = toThis.normalized;
                pushDirection.z = 0; // Z축 고정
                
                // 겹침량의 절반만큼 각자 이동 (양쪽 모두 밀어냄)
                Vector3 pushAmount = pushDirection * (overlap * 0.5f * hardPushMultiplier);
                transform.position += pushAmount;
            }
            else if (distance < 0.001f)
            {
                // 완전히 같은 위치인 경우 랜덤 방향으로 밀어냄
                Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
                transform.position += randomDir * minDistance * 0.5f;
            }
        }
    }

    /// <summary>
    /// 다른 풍선들과 겹치지 않도록 밀어내는 벡터 계산 (점진적)
    /// </summary>
    private Vector3 CalculateSeparation()
    {
        Vector3 separationVector = Vector3.zero;

        foreach (BalloonBehavior other in _activeBalloons)
        {
            if (other == this || other == null) continue;

            Vector3 toThis = transform.position - other.transform.position;
            float distance = toThis.magnitude;
            
            // 두 풍선의 반지름 합 기반 분리 반경
            float combinedRadius = _balloonRadius + other._balloonRadius;
            float separationRadius = Mathf.Max(combinedRadius * 1.5f, minSeparationDistance * 1.2f);

            // 분리 반경 내에 있는 풍선만 고려
            if (distance < separationRadius && distance > 0.01f)
            {
                // 거리가 가까울수록 더 강하게 밀어냄 (제곱 비례)
                float normalizedDist = distance / separationRadius;
                float strength = (1.0f - normalizedDist) * (1.0f - normalizedDist);
                separationVector += toThis.normalized * strength * separationForce;
            }
        }

        // Z축은 유지 (카메라 방향으로 밀리지 않도록)
        separationVector.z = 0;

        return separationVector;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Detect if hit by an arrow
        if (collision.gameObject.GetComponent<ArcheryArrow>() != null)
        {
            // Calculate direction from Arrow to Balloon (approximate)
            Vector3 impactDir = Vector3.zero;
            if (collision.contacts.Length > 0)
            {
                impactDir = collision.contacts[0].normal; // Normal is usually pointing OUT of the balloon
                // If we want Arrow Velocity direction, we could get it from the arrow's RB, 
                // but let's use the collision normal to estimate "impact direction".
                // Actually, the user wants fragments "opposite to the arrow flight".
                // Arrow moves A -> B. Fragments should move B -> A.
                // The contact normal on the Balloon surface points OUT towards the arrow.
                // So the normal IS roughly the direction we want the particles to fly (towards the shooter).
                impactDir = collision.contacts[0].normal;
            }
            else
            {
                // Fallback: Balloon Center - Arrow Center
                impactDir = (collision.transform.position - transform.position).normalized; // Points towards arrow
            }
            
            Pop(impactDir);
        }
    }

    private void Pop(Vector3 impactDirection)
    {
        // Increment score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1);
        }

        // --- Visual Effects (VFX) ---
        // 1. Create a temporary GameObject for the explosion
        GameObject explosion = new GameObject("BalloonExplosion");
        explosion.transform.position = transform.position;

        // 2. Add and configure ParticleSystem
        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        
        // STOP the system before configuring main properties that require it to be stopped
        ps.Stop(); 

        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        // Main Module
        main.duration = 1.0f;
        main.startLifetime = 1.0f; // Short duration
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f); // Fast explosion
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); // Small fragments
        main.loop = false; // One-shot
        main.playOnAwake = false; // We will play manually after setup

        // Color - Match the balloon's color
        Renderer balloonRenderer = GetComponent<Renderer>();
        if (balloonRenderer != null)
        {
            main.startColor = balloonRenderer.material.color;
            
            // Set material to Default-Particle (or Additive if available for glow)
            // Trying to find a standard particle material
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");
            if (shader != null)
            {
                Material particleMat = new Material(shader);
                particleMat.color = balloonRenderer.material.color; // tint
                renderer.material = particleMat;
            }
        }

        // Emission Module - Burst!
        emission.rateOverTime = 0; // No continuous emission
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 30, 50) }); // 30-50 particles at start

        // Shape Module - Directional Cone
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f; // Narrow interactive cone
        
        // Orient the explosion opposite to the impact (towards where the arrow came from)
        // impactDirection is Arrow -> Balloon. We want Balloon -> Arrow Origin.
        // So we look in the direction of -impactDirection.
        if (impactDirection != Vector3.zero)
        {
            explosion.transform.rotation = Quaternion.LookRotation(-impactDirection);
        }
        else
        {
            // Fallback: Explode upwards or outwards if direction is unknown
            shape.shapeType = ParticleSystemShapeType.Sphere;
        }

        // 3. Play and Destroy
        ps.Play();
        Destroy(explosion, main.duration + main.startLifetime.constantMax); // Clean up after effect finishes

        // 4. 효과음 재생 (배열에 있으면 랜덤으로 하나 재생)
        if (popSounds != null && popSounds.Length > 0)
        {
            AudioClip clip = popSounds[Random.Range(0, popSounds.Length)];
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, transform.position, 1.5f); // 볼륨 50% 증가 (1.5배)
        }

        // 5. Destroy this balloon
        Destroy(gameObject);
    }
}
