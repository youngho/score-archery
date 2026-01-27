using UnityEngine;

public class BalloonBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float upwardSpeed = 2.0f;
    public float horizontalVelocity = 0.0f;
    public float horizontalDamping = 0.5f;
    public float destroyHeight = 10.0f;
    public float driftAmount = 0.5f;
    public float driftSpeed = 1.0f;

    private float _randomDriftOffset;

    void Start()
    {
        _randomDriftOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Upward movement
        Vector3 movement = Vector3.up * upwardSpeed * Time.deltaTime;

        // Diagonal movement (inward launch)
        movement.x += horizontalVelocity * Time.deltaTime;
        
        // Dampen horizontal velocity over time
        horizontalVelocity = Mathf.Lerp(horizontalVelocity, 0f, horizontalDamping * Time.deltaTime);

        // Subtle horizontal drift
        float drift = Mathf.Sin(Time.time * driftSpeed + _randomDriftOffset) * driftAmount * Time.deltaTime;
        movement.x += drift;

        transform.Translate(movement, Space.World);

        // Auto-destruction when reaching peak height
        if (transform.position.y > destroyHeight)
        {
            Destroy(gameObject);
        }
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
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        // Main Module
        main.startLifetime = 1.0f; // Short duration
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f); // Fast explosion
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); // Small fragments
        main.duration = 1.0f;
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

        // 4. Destroy this balloon
        Destroy(gameObject);
        
        // Optional: Play sound effect here
    }
}
