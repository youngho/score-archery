using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 10Star 씬 전용:
/// - StarSpawnPoint(BoxCollider) 범위 안에서 별을 랜덤 생성
/// - 생성된 별은 StarBehavior가 자동으로 커졌다가 사라짐
/// </summary>
[DisallowMultipleComponent]
public class StarManager : MonoBehaviour
{
    [Header("Spawn Range")]
    [Tooltip("별 스폰 범위(BoxCollider bounds 사용)")]
    public BoxCollider StarSpawnPoint;

    [Header("Star Prefab")]
    [Tooltip("생성할 별 프리팹 (예: star.prefab). StarBehavior가 붙어 있어야 함.")]
    public GameObject starPrefab;

    [Header("Spawn Settings")]
    [Tooltip("동시에 존재 가능한 별 최대 개수")]
    public int maxStarsAlive = 10;

    [Tooltip("별 생성 간격 (초)")]
    public float spawnInterval = 0.6f;

    [Tooltip("StarSpawnPoint가 비어있어도 별을 생성할지 여부")]
    public bool logIfMissingSpawnPoint = true;

    [Header("Star Defaults")]
    public int pointsPerStar = 1;
    public Color starColor = Color.cyan;
    public float minStarScale = 0.05f;
    public float maxStarScale = 0.30f;
    public float growDuration = 2.5f;
    public float shrinkDuration = 2.5f;

    private readonly HashSet<StarBehavior> _activeStars = new HashSet<StarBehavior>();
    private Coroutine _spawnRoutine;

    private void Start()
    {
        // 다른 씬에서 우연히 컴포넌트가 붙어도 동작하지 않게 안전장치
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "10Star")
            return;

        EnsureBootstrap();

        if (_spawnRoutine == null)
        {
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private IEnumerator SpawnLoop()
    {
        // 시작 직후는 약간 더 빠르게 생성되도록
        while (true)
        {
            CleanupNulls();

            if (StarSpawnPoint == null || starPrefab == null)
            {
                if (logIfMissingSpawnPoint)
                {
                    if (StarSpawnPoint == null)
                        Debug.LogWarning("[StarManager] StarSpawnPoint is null. Spawn is paused.");
                    if (starPrefab == null)
                        Debug.LogWarning("[StarManager] starPrefab is null. Inspector에서 star.prefab을 할당해주세요.");
                    logIfMissingSpawnPoint = false;
                }

                yield return null;
                continue;
            }

            while (_activeStars.Count < maxStarsAlive)
            {
                SpawnOne();
                yield return null;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        Bounds bounds = StarSpawnPoint.bounds;
        Vector3 pos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        GameObject star = Instantiate(starPrefab, pos, starPrefab.transform.rotation);
        star.name = "StarPoint";

        var behavior = star.GetComponent<StarBehavior>();
        if (behavior == null) behavior = star.AddComponent<StarBehavior>();

        behavior.SetOwner(this);
        behavior.points = pointsPerStar;
        behavior.starColor = starColor;
        behavior.minScale = minStarScale;
        behavior.maxScale = maxStarScale;
        behavior.growDuration = growDuration;
        behavior.shrinkDuration = shrinkDuration;

        _activeStars.Add(behavior);
    }

    public void NotifyStarDestroyed(StarBehavior star)
    {
        if (star == null) return;
        _activeStars.Remove(star);
    }

    private void CleanupNulls()
    {
        if (_activeStars.Count == 0) return;

        // HashSet은 foreach 중 삭제가 불가하므로 List로 복사
        var toRemove = new List<StarBehavior>();
        foreach (var s in _activeStars)
        {
            if (s == null) toRemove.Add(s);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            _activeStars.Remove(toRemove[i]);
        }
    }

    private void EnsureBootstrap()
    {
        // 이미 씬 오브젝트가 있으면 유지
        if (StarSpawnPoint != null) return;

        // 10Star 타겟이 씬에 존재한다면 그 주변을 기본 범위로 잡음
        GameObject target = GameObject.Find("power_star");

        GameObject spawnGO = GameObject.Find("StarSpawnPoint");
        if (spawnGO == null)
        {
            spawnGO = new GameObject("StarSpawnPoint");
        }

        var box = spawnGO.GetComponent<BoxCollider>();
        if (box == null) box = spawnGO.AddComponent<BoxCollider>();

        // 별 생성 범위로만 쓰므로 충돌 간섭 최소화
        box.isTrigger = true;

        if (target != null)
        {
            spawnGO.transform.position = target.transform.position;
        }

        // 너무 작거나 크면 맞추기/시각화가 어려우므로 “적당한” 기본값
        if (box.size == Vector3.zero)
        {
            box.size = new Vector3(2.0f, 2.0f, 2.0f);
        }

        StarSpawnPoint = box;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 씬 로딩 직후 자동으로 StarManager를 구성(사용자가 씬 편집을 안 해도 동작하도록)
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "10Star")
            return;

        // 이미 존재하면 스킵
        var mgr = Object.FindFirstObjectByType<StarManager>();
        if (mgr == null)
        {
            var go = new GameObject("StarManager");
            mgr = go.AddComponent<StarManager>();
        }

        mgr.EnsureBootstrap();

        // 스타가 이미 존재하는 경우를 대비해 spawn coroutine 시작은 Start에서 처리
    }

    private void OnDestroy()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }
}

