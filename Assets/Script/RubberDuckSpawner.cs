using UnityEngine;

public class RubberDuckSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public GameObject duckPrefab;
    public float spawnInterval = 2.0f;
    public float spawnRangeX = 4.0f;

    private float _nextSpawnTime;

    void Start()
    {
        _nextSpawnTime = Time.time + spawnInterval;

        if (spawnPoint == null)
        {
            GameObject sp = GameObject.Find("RubberDuckSpawnPoint");
            if (sp != null)
            {
                spawnPoint = sp.transform;
            }
        }
    }

    void Update()
    {
        if (Time.time >= _nextSpawnTime)
        {
            SpawnDuck();
            _nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnDuck()
    {
        if (duckPrefab == null) return;

        Vector3 spawnPos;
        Quaternion spawnRot;

        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        spawnPos = spawnPoint.position + spawnPoint.right * randomX;
        spawnRot = spawnPoint.rotation;
        
        Instantiate(duckPrefab, spawnPos, spawnRot);
    }
}
