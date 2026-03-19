using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 11Candle 씬 전용:
/// - Step_A 위에 CandleSpawnPoint를 10개 랜덤 생성
/// - 해당 스폰포인트 위에 양초를 생성
/// </summary>
[DisallowMultipleComponent]
public class CandleManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Step_A 위에 생성할 양초 개수")]
    public int candleCount = 10;

    [Tooltip("스폰 Y 오프셋 (Step_A 표면 위로 살짝 띄움)")]
    public float spawnYOffset = 0.02f;

    [Tooltip("스폰 랜덤 범위에서 가장자리 제외를 위한 마진")]
    public float spawnMargin = 0.08f;

    [Tooltip("생성할 양초 프리팹 에셋 (Assets/11Candle/candle.prefab 을 할당)")]
    public GameObject candlePrefabAsset;

    [Tooltip("Step_A 오브젝트 이름")]
    public string stepAName = "Step_A";

    [Header("CandleSpawnPoint")]
    [Tooltip("생성할 스폰 포인트 오브젝트 이름 prefix")]
    public string spawnPointPrefix = "CandleSpawnPoint";

    [Tooltip("스폰포인트를 먼저 만들고 그 위치를 이용해 양초를 생성")]
    public bool createSpawnPoints = true;

    private readonly List<Transform> _spawnPoints = new List<Transform>();

    private void Start()
    {
        // 11Candle 씬에서만 동작 (오브젝트 기반으로도 확인)
        if (!Is11CandleScene())
            return;

        SpawnCandleRoutine();
    }

    private bool Is11CandleScene()
    {
        // 씬 이름이 정확히 "11Candle"일 때만 실행
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "11Candle";
    }

    private void SpawnCandleRoutine()
    {
        var stepA = GameObject.Find(stepAName);
        if (stepA == null)
        {
            Debug.LogError("[CandleManager] Step_A 오브젝트를 찾지 못했습니다.");
            return;
        }

        // 이전 플레이에서 남은 양초/스폰포인트가 있으면 정리
        ClearPreviouslySpawned();

        var bounds = GetCombinedBounds(stepA);
        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            Debug.LogError("[CandleManager] Step_A 바운드를 계산하지 못했습니다.");
            return;
        }

        CreateSpawnPointsIfNeeded(bounds, stepA.transform);

        if (candlePrefabAsset == null)
        {
            Debug.LogError("[CandleManager] candlePrefabAsset 이 비어있습니다. Assets/11Candle/candle.prefab 을 할당해주세요.");
            return;
        }

        for (int i = 0; i < candleCount; i++)
        {
            Vector3 spawnPos;
            Quaternion spawnRot = GetUprightRotation(stepA.transform);

            if (_spawnPoints.Count > i)
            {
                spawnPos = _spawnPoints[i].position;
            }
            else
            {
                spawnPos = GetRandomPointOnBounds(bounds, stepA.transform);
            }

            // 약간 랜덤 yaw (시각적 다양성)
            spawnRot = spawnRot * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var candle = Instantiate(candlePrefabAsset, spawnPos, spawnRot);
            candle.name = $"Candle_{i}";

            // CandleBehavior이 없으면 붙임
            var beh = candle.GetComponent<CandleBehavior>();
            if (beh == null)
                beh = candle.AddComponent<CandleBehavior>();
        }
    }

    private void ClearPreviouslySpawned()
    {
        // CandleBehavior가 붙어있던 양초 제거
        var candles = FindObjectsOfType<CandleBehavior>();
        for (int i = candles.Length - 1; i >= 0; i--)
        {
            if (candles[i] != null)
                Destroy(candles[i].gameObject);
        }

        // 이전 런에서 만든 스폰 포인트 제거(이름 prefix로만 정리)
        var candidates = GameObject.FindObjectsOfType<Transform>();
        foreach (var t in candidates)
        {
            if (t != null && t.name != null && t.name.StartsWith(spawnPointPrefix))
            {
                Destroy(t.gameObject);
            }
        }
    }

    private void CreateSpawnPointsIfNeeded(Bounds bounds, Transform stepA)
    {
        _spawnPoints.Clear();

        if (!createSpawnPoints)
            return;

        for (int i = 0; i < candleCount; i++)
        {
            Vector3 pos = GetRandomPointOnBounds(bounds, stepA);

            var go = new GameObject($"{spawnPointPrefix}_{i}");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.SetParent(transform, true);

            _spawnPoints.Add(go.transform);
        }
    }

    private Vector3 GetRandomPointOnBounds(Bounds bounds, Transform stepA)
    {
        float xMin = bounds.min.x + spawnMargin;
        float xMax = bounds.max.x - spawnMargin;
        float zMin = bounds.min.z + spawnMargin;
        float zMax = bounds.max.z - spawnMargin;

        float x = Random.Range(xMin, xMax);
        float z = Random.Range(zMin, zMax);

        // Step_A 표면 위
        float y = bounds.max.y + spawnYOffset;

        // Step_A가 회전되어 있을 수 있으니 약간 더 안정적으로: stepA up 기준으로 살짝 조정
        // (현재 bounds는 world AABB이므로 완벽하진 않지만 Step_A가 평평하다는 가정에서는 충분)
        return new Vector3(x, y, z);
    }

    private static Quaternion GetUprightRotation(Transform stepA)
    {
        Vector3 up = stepA.up;
        Vector3 forward = stepA.forward;
        // up에 수직으로 forward를 투영해서 look rotation의 up/forward 일관성을 맞춤
        forward = Vector3.ProjectOnPlane(forward, up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        return Quaternion.LookRotation(forward, up);
    }

    private static Bounds GetCombinedBounds(GameObject root)
    {
        bool hasAny = false;
        Bounds combined = new Bounds(root.transform.position, Vector3.zero);

        // Collider가 있으면 그 바운드를 우선
        var cols = root.GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
            foreach (var c in cols)
            {
                if (!hasAny)
                {
                    combined = c.bounds;
                    hasAny = true;
                }
                else
                {
                    combined.Encapsulate(c.bounds);
                }
            }

            return combined;
        }

        // 없으면 renderer 기준
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            foreach (var r in rends)
            {
                if (!hasAny)
                {
                    combined = r.bounds;
                    hasAny = true;
                }
                else
                {
                    combined.Encapsulate(r.bounds);
                }
            }
        }

        return combined;
    }

    // 씬에 CandleManager 컴포넌트가 없어도 동작하도록 런타임 부트스트랩
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeScene != "11Candle")
            return;

        var existing = Object.FindFirstObjectByType<CandleManager>();
        if (existing == null)
        {
            var go = new GameObject("CandleManager");
            go.AddComponent<CandleManager>();
        }

        // CandleScoreManager도 필요 시 생성(정말 필요할 때 ScoreManager를 참조)
        _ = CandleScoreManager.Instance;
    }
}

