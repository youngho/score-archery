using UnityEngine;

public class LeafSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Target Leaf Prefab to be spawned")]
    public GameObject leafPrefab;

    [Tooltip("The central transform representing the spawn point")]
    public Transform spawnPoint;

    [Tooltip("Radius around the spawn point to randomly spawn leaves")]
    public float spawnRadius = 5.0f;

    [Tooltip("Time interval between spawns (seconds)")]
    public float spawnInterval = 5.0f;

    [Header("Leaf Movement Settings")]
    public float leafFallSpeed = 1.8f;
    public float leafSwaySpeed = 1.0f;
    [Tooltip("How far the leaf sways on the X axis")]
    public float leafSwayDistanceX = 0.1f;
    [Tooltip("How far the leaf sways on the Z axis")]
    public float leafSwayDistanceZ = 3.0f;
    [Tooltip("If the leaf falls below this local Y position relative to start position, it will be destroyed.")]
    public float leafFallingDistanceLimit = 20f;

    private float _spawnTimer;

    private void Update()
    {
        if (leafPrefab == null || spawnPoint == null) return;

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= spawnInterval)
        {
            SpawnLeaf();
            _spawnTimer = 0f;
        }
    }

    private void SpawnLeaf()
    {
        // Calculate a random position within the radius on the X and Z axes
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPosition = spawnPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Spawn Leaf
        GameObject leafInstance = Instantiate(leafPrefab, randomPosition, spawnPoint.rotation);

        // Add FallingLeaf script if not present to handle falling and swaying logic
        FallingLeaf fallingLeaf = leafInstance.GetComponent<FallingLeaf>();
        if (fallingLeaf == null)
        {
            fallingLeaf = leafInstance.AddComponent<FallingLeaf>();
        }

        // Apply settings from spawner to the instantiated leaf
        fallingLeaf.fallSpeed = leafFallSpeed;
        fallingLeaf.swaySpeed = leafSwaySpeed;
        fallingLeaf.swayDistanceX = leafSwayDistanceX;
        fallingLeaf.swayDistanceZ = leafSwayDistanceZ;
        fallingLeaf.fallingDistanceLimit = leafFallingDistanceLimit;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a wire sphere in the Scene View to visualize the spawn radius
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
        }
    }
}

public class FallingLeaf : MonoBehaviour
{
    // Settings are now controlled and assigned by LeafSpawner
    [HideInInspector] public float fallSpeed;
    [HideInInspector] public float swaySpeed;
    [HideInInspector] public float swayDistanceX;
    [HideInInspector] public float swayDistanceZ;
    [HideInInspector] public float fallingDistanceLimit;

    private float _randomTimeOffsetX;
    private float _randomTimeOffsetZ;
    private Vector3 _startPosition;
    private float _currentY;

    private void Start()
    {
        _startPosition = transform.position;
        _currentY = _startPosition.y;
        
        // Use different offsets for X and Z to create a more natural, swirling motion
        _randomTimeOffsetX = Random.Range(0f, 100f);
        _randomTimeOffsetZ = Random.Range(0f, 100f);

        // Add some random variation to make it look natural
        fallSpeed *= Random.Range(0.8f, 1.2f);
        swaySpeed *= Random.Range(0.8f, 1.2f);
        swayDistanceX *= Random.Range(0.8f, 1.2f);
        swayDistanceZ *= Random.Range(0.8f, 1.2f);
    }

    private void Update()
    {
        // 1. Calculate falling (downwards)
        _currentY -= fallSpeed * Time.deltaTime;

        // 2. Calculate swaying (left/right and forward/backward)
        float swayOffsetX = Mathf.Sin((Time.time + _randomTimeOffsetX) * swaySpeed) * swayDistanceX;
        float swayOffsetZ = Mathf.Cos((Time.time + _randomTimeOffsetZ) * swaySpeed) * swayDistanceZ;

        // Apply new position (sway on X and Z axes, fall on Y axis)
        transform.position = new Vector3(_startPosition.x + swayOffsetX, _currentY, _startPosition.z + swayOffsetZ);

        // 3. Combine rotation effect while swaying
        // Adds a realistic falling leaf angle
        float angleZ = Mathf.Sin((Time.time + _randomTimeOffsetX) * swaySpeed) * 30f; 
        float angleX = Mathf.Cos((Time.time + _randomTimeOffsetZ) * swaySpeed) * 30f; 
        transform.localRotation = Quaternion.Euler(angleX, 0, angleZ);

        // Destroy self when it falls beyond the prescribed block distance
        if (_startPosition.y - _currentY > fallingDistanceLimit)
        {
            Destroy(gameObject);
        }
    }
}
