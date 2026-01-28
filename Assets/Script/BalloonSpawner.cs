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

        // 기존 풍선들과 겹치는지 확인
        Vector3 adjustedPos = FindNonOverlappingPosition(spawnPos, isLeft);

        GameObject balloon = Instantiate(balloonPrefab, adjustedPos, Quaternion.identity);
        
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

    /// <summary>
    /// 기존 풍선들과 겹치지 않는 스폰 위치를 찾음
    /// </summary>
    private Vector3 FindNonOverlappingPosition(Vector3 originalPos, bool isLeft)
    {
        float checkRadius = 2.0f;  // 풍선 겹침 체크 반경
        int maxAttempts = 5;       // 최대 시도 횟수
        
        Vector3 bestPos = originalPos;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // 현재 위치에서 다른 풍선과 겹치는지 체크
            Collider[] overlaps = Physics.OverlapSphere(bestPos, checkRadius);
            
            bool hasOverlap = false;
            foreach (Collider col in overlaps)
            {
                if (col.GetComponent<BalloonBehavior>() != null)
                {
                    hasOverlap = true;
                    break;
                }
            }
            
            // 겹치지 않으면 이 위치 사용
            if (!hasOverlap)
            {
                return bestPos;
            }
            
            // 겹치면 위치 조정 (Y축으로 아래로 이동하여 시간차 효과)
            bestPos.y -= checkRadius * 0.8f;
            
            // X축으로도 약간 조정
            float xOffset = Random.Range(0.5f, 1.5f);
            bestPos.x += isLeft ? xOffset : -xOffset;
        }
        
        return bestPos;
    }
}
