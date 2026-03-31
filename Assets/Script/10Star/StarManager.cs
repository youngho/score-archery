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
        // 시작 직후부터 랜덤하게 계속 생성
        while (true)
        {
            if (StarSpawnPoint == null || starPrefab == null)
            {
                yield return null;
                continue;
            }

            SpawnOne();

            // 랜덤 생성 간격 (0.2 ~ 1.0초 사이로 무작위 지연)
            float randomDelay = Random.Range(0.2f, 1.0f);
            yield return new WaitForSeconds(randomDelay);
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

        GameObject star = Instantiate(starPrefab, pos, Random.rotation);
        star.name = "StarPoint";

        var behavior = star.GetComponent<StarBehavior>();
        if (behavior == null) behavior = star.AddComponent<StarBehavior>();

        behavior.SetOwner(this);
    }

    private void EnsureBootstrap()
    {
        // 이미 씬 오브젝트가 있으면 유지
        if (StarSpawnPoint != null) return;

        // 10Star 타켓이 씬에 존재한다면 그 주변을 기본 범위로 잡음
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

