using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 11Candle 씬 전용:
/// - Step_A 위에 양초를 10개 랜덤 생성
/// </summary>
[DisallowMultipleComponent]
public class CandleManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Step_A 위에 생성할 양초 개수")]
    public int candleCount = 10;

    [Tooltip("스폰 랜덤 범위에서 가장자리 제외를 위한 마진")]
    public float spawnMargin = 0.05f;

    [Tooltip("생성할 양초 프리팹 에셋 (Assets/11Candle/candle.prefab 을 할당)")]
    public GameObject candlePrefabAsset;

    [Tooltip("Step_A 오브젝트 이름")]
    public string stepAName = "Step_A";


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


        if (candlePrefabAsset == null)
        {
            Debug.LogError("[CandleManager] candlePrefabAsset 이 비어있습니다. Assets/11Candle/candle.prefab 을 할당해주세요.");
            return;
        }

        for (int i = 0; i < candleCount; i++)
        {
            Vector3 spawnPos = GetRandomPointOnBounds(bounds, stepA.transform);
            Quaternion spawnRot = GetUprightRotation(stepA.transform);

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

    }


    private Vector3 GetRandomPointOnBounds(Bounds bounds, Transform stepA)
    {
        float xMin = bounds.min.x + spawnMargin;
        float xMax = bounds.max.x - spawnMargin;
        float zMin = bounds.min.z + spawnMargin;
        float zMax = bounds.max.z - spawnMargin;

        // 마진이 너무 커서 범위가 역전되는 경우 방지
        if (xMin > xMax) { xMin = xMax = bounds.center.x; }
        if (zMin > zMax) { zMin = zMax = bounds.center.z; }

        float x = Random.Range(xMin, xMax);
        float z = Random.Range(zMin, zMax);

        // 계단 형태의 오브젝트 표면을 찾기 위해 위에서 아래로 Raycast
        // 바운드 최상단보다 5m 위에서 아래로 쏨
        float rayStartHeight = bounds.max.y + 5f;
        Vector3 rayOrigin = new Vector3(x, rayStartHeight, z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f))
        {
            // 실제 충돌 지점의 좌표를 반환
            return hit.point;
        }

        // Raycast가 실패한 경우(콜라이더가 없는 지점 등) 기존 방식대로 바운드 상단 사용
        return new Vector3(x, bounds.max.y, z);
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

