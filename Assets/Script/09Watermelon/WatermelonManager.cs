using System.Collections;
using UnityEngine;

/// <summary>
/// 09Watermelon 씬 전용:
/// - WatermelonSpawnPoint(BoxCollider) bounds 범위 안에서 수박을 랜덤 생성
/// - 생성된 수박은 Rigidbody 물리로 "공처럼" 튀며 움직임
/// </summary>
[DisallowMultipleComponent]
public class WatermelonManager : MonoBehaviour
{
    [Header("Spawn Range")]
    [Tooltip("수박 스폰 범위(BoxCollider bounds 사용)")]
    public BoxCollider WatermelonSpawnPoint;

    [Header("Watermelon Prefab")]
    [Tooltip("생성할 수박 프리팹. 비어있으면 런타임에 Sphere로 대체 생성합니다.")]
    public GameObject watermelonPrefab;

    [Header("Spawn Settings")]
    [Tooltip("수박 생성 간격 (초)")]
    public float spawnInterval = 0.9f;

    [Header("Movement Direction")]
    [Tooltip("수박이 통통 튀며 이동할 x축 방향 (true: +x, false: -x)")]
    public bool movePositiveX = true;

    [Tooltip("스폰 시 초기 속도(임펄스) 최소/최대")]
    public float initialImpulseMin = 1.5f;
    public float initialImpulseMax = 4.0f;

    [Tooltip("스폰 시 위로 튀게 하는 임펄스 최소/최대")]
    public float initialUpImpulseMin = 1.0f;
    public float initialUpImpulseMax = 3.0f;

    [Header("Watermelon Defaults")]
    [Tooltip("수박 반지름(런타임 Sphere 생성 시)")]
    public float fallbackSphereRadius = 0.18f;

    [Tooltip("탄성(0~1). 1에 가까울수록 공처럼 튐")]
    [Range(0f, 1f)]
    public float bounciness = 0.9f;

    [Tooltip("마찰(0~1). 낮을수록 더 잘 튐")]
    [Range(0f, 1f)]
    public float dynamicFriction = 0.1f;

    [Tooltip("중력 사용 여부")]
    public bool useGravity = true;

    [Tooltip("수박이 너무 멀리 날아가지 않게 최대 속도 제한(0이면 제한 없음)")]
    public float maxSpeed = 12f;

    private Coroutine _spawnRoutine;
    private Coroutine _waitRoutine;

    private void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "09Watermelon")
            return;

        EnsureBootstrap();
        _ = WatermelonScoreManager.Instance;

        if (_waitRoutine == null)
            _waitRoutine = StartCoroutine(WaitForTimerThenSpawn());
    }

    private IEnumerator WaitForTimerThenSpawn()
    {
        // StageTimer(=Timer)가 시작되기 전에는 스폰을 멈춤
        Timer timer = Object.FindFirstObjectByType<Timer>();
        while (timer == null)
        {
            timer = Object.FindFirstObjectByType<Timer>();
            yield return null;
        }

        while (!timer.IsRunning)
            yield return null;

        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (WatermelonSpawnPoint == null)
            {
                yield return null;
                continue;
            }

            SpawnOne();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        Bounds bounds = WatermelonSpawnPoint.bounds;
        Vector3 pos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );

        GameObject go;
        if (watermelonPrefab != null)
        {
            go = Instantiate(watermelonPrefab, pos, watermelonPrefab.transform.rotation);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            float diameter = Mathf.Max(0.01f, fallbackSphereRadius * 2f);
            go.transform.localScale = Vector3.one * diameter;
        }

        go.name = "Watermelon";

        var behavior = go.GetComponent<WatermelonBehavior>();
        if (behavior == null) behavior = go.AddComponent<WatermelonBehavior>();

        behavior.SetOwner(this);
        behavior.useGravity = useGravity;
        behavior.bounciness = bounciness;
        behavior.dynamicFriction = dynamicFriction;
        behavior.maxSpeed = maxSpeed;

        // 초기 임펄스: x축 한 방향 + 위 방향을 섞어서 "통통 튀며 전진"
        Vector3 lateral = movePositiveX ? Vector3.right : Vector3.left;

        float lateralImp = Random.Range(initialImpulseMin, initialImpulseMax);
        float upImp = Random.Range(initialUpImpulseMin, initialUpImpulseMax);
        behavior.initialImpulse = (lateral * lateralImp) + (Vector3.up * upImp);

    }

    public void NotifyWatermelonDestroyed(WatermelonBehavior wm)
    {
        // 더 이상 동시 개수 제한을 두지 않으므로 추적/정리 로직은 비워둠
    }

    private void EnsureBootstrap()
    {
        if (WatermelonSpawnPoint != null) return;

        GameObject spawnGO = GameObject.Find("WatermelonSpawnPoint");
        if (spawnGO == null)
            spawnGO = new GameObject("WatermelonSpawnPoint");

        var box = spawnGO.GetComponent<BoxCollider>();
        if (box == null) box = spawnGO.AddComponent<BoxCollider>();

        box.isTrigger = true;

        // 씬에 수박 모델이 있으면 그 주변을 기본 위치로
        var target = GameObject.Find("watermelon") ?? GameObject.Find("Watermelon");
        if (target != null)
            spawnGO.transform.position = target.transform.position;

        if (box.size == Vector3.zero)
            box.size = new Vector3(3.0f, 1.5f, 3.0f);

        WatermelonSpawnPoint = box;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "09Watermelon")
            return;

        var mgr = Object.FindFirstObjectByType<WatermelonManager>();
        if (mgr == null)
        {
            var go = new GameObject("WatermelonManager");
            mgr = go.AddComponent<WatermelonManager>();
        }

        mgr.EnsureBootstrap();
        _ = WatermelonScoreManager.Instance;
    }

    private void OnDestroy()
    {
        if (_waitRoutine != null)
        {
            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }
}

