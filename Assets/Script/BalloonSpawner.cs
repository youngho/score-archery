using UnityEngine;

public class BalloonSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject balloonPrefab;
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 2.0f;
    public float spawnDepth = 10f; // Distance from camera

    [Header("Balloon Settings")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 3.5f;
    public float minHorizontalSpeed = 1.0f;
    public float maxHorizontalSpeed = 4.0f;
    public float horizontalDamping = 0.8f;
    public float destroyHeight = 8.0f;

    private float _nextSpawnTime;
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (Time.time >= _nextSpawnTime)
        {
            SpawnBalloon();
            ScheduleNextSpawn();
        }
    }

    void ScheduleNextSpawn()
    {
        _nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void SpawnBalloon()
    {
        if (balloonPrefab == null) return;

        // Randomly choose left or right side
        bool isLeft = Random.value > 0.5f;
        
        // Viewport coordinates: (0,0) is bottom-left, (1,0) is bottom-right
        Vector3 viewportPos = isLeft ? new Vector3(0.05f, -0.1f, spawnDepth) : new Vector3(0.95f, -0.1f, spawnDepth);
        
        Vector3 spawnPos = _mainCamera.ViewportToWorldPoint(viewportPos);

        // Check for overlap before spawning
        if (Physics.CheckSphere(spawnPos, 1.0f)) // 1.0f is approx radius of balloon
        {
            // If overlapping, try slightly offsetting or skip
            // Let's try one alternate position
             spawnPos += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
             
             // Check again, if still overlapping, maybe skip this frame or just let physics resolve it (it will pop apart)
             // Physics push-out is better than nothing.
        }

        GameObject balloon = Instantiate(balloonPrefab, spawnPos, Quaternion.identity);
        
        // Randomize visual properties
        float scale = Random.Range(1.6f, 2.4f);
        balloon.transform.localScale = Vector3.one * scale;
        
        Renderer renderer = balloon.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(Random.value, Random.value, Random.value, 1f);
        }

        // Configure movement behavior
        BalloonBehavior behavior = balloon.GetComponent<BalloonBehavior>();
        if (behavior == null)
        {
            behavior = balloon.AddComponent<BalloonBehavior>();
        }
        
        behavior.upwardSpeed = Random.Range(minSpeed, maxSpeed);
        
        // Set horizontal velocity towards center (0.5 in viewport space)
        float horizontalSpeed = Random.Range(minHorizontalSpeed, maxHorizontalSpeed);
        behavior.horizontalVelocity = isLeft ? horizontalSpeed : -horizontalSpeed;
        behavior.horizontalDamping = horizontalDamping;

        behavior.destroyHeight = destroyHeight;
    }
}
