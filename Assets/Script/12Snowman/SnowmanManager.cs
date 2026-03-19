using UnityEngine;

public class SnowmanManager : MonoBehaviour
{
    public GameObject snowmanPrefab;
    public BoxCollider spawnArea;
    public float spawnInterval = 3f;

    private float timer = 0f;

    void Start()
    {
        // Ensure time is running
        Time.timeScale = 1.0f;
        
        Debug.Log($"[SnowmanManager] Spawner initialized. Prefab: {snowmanPrefab?.name}, Area: {spawnArea?.name}, Interval: {spawnInterval}s");
    }

    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnSnowman();
        }
    }

    private void SpawnSnowman()
    {
        if (snowmanPrefab == null || spawnArea == null) return;

        Bounds bounds = spawnArea.bounds;
        Vector3 spawnPos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        Debug.Log($"[SnowmanManager] Spawning Snowman at {spawnPos}");
        GameObject clone = Instantiate(snowmanPrefab, spawnPos, snowmanPrefab.transform.rotation);
        
        if (clone != null)
        {
            clone.SetActive(true);
        }
    }
}
