using System.Collections;
using UnityEngine;

/// <summary>
/// Coconut 씬 전용:
/// - CoconutSpawnPoint 2곳(BoxCollider bounds) 중 임의 선택
/// - 선택된 bounds 범위 안에서 코코넛을 랜덤 생성
/// - StageTimer(=Timer)가 시작되면 스폰 시작
/// - 코코넛은 스폰 지점에서 서서히 자라며, 화살에 맞으면 실제처럼 떨어짐(CoconutBehavior)
/// </summary>
[DisallowMultipleComponent]
public class CoconutManager : MonoBehaviour
{
    [Header("Spawn Centers (2)")]
    [Tooltip("야자나무 기둥/꼭대기 등, 코코넛이 맺힐 중심 위치 A")]
    public Transform CoconutSpawnCenterA;

    [Tooltip("야자나무 기둥/꼭대기 등, 코코넛이 맺힐 중심 위치 B")]
    public Transform CoconutSpawnCenterB;

    [Header("Ring Settings")]
    [Tooltip("나무 기둥(스폰포인트 중심)으로부터 코코넛이 시작되는 최소 반지름")]
    public float innerRadius = 0.5f;

    [Tooltip("나무 기둥(스폰포인트 중심)으로부터 코코넛이 생성되는 최대 반지름")]
    public float outerRadius = 1.5f;

    [Header("Coconut Prefab")]
    [Tooltip("생성할 코코넛 프리팹(없으면 런타임에 Sphere로 대체 생성)")]
    public GameObject coconutPrefab;

    [Header("Spawn Settings")]
    [Tooltip("코코넛 생성 간격(초)")]
    public float spawnInterval = 1.2f;

    [Tooltip("동시에 존재 가능한 코코넛 최대 개수(0이면 제한 없음)")]
    public int maxCoconutsAlive = 0;

    [Header("Grow Defaults")]
    public float growDuration = 1.2f;
    public float startScale = 0.02f;
    public float targetScaleMultiplier = 1.0f;

    [Header("Score Defaults")]
    public int pointsPerCoconut = 1;

    private Coroutine _waitRoutine;
    private Coroutine _spawnRoutine;
    private int _aliveCount;

    private void Start()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "09Coconut" && sceneName != "08Coconut")
            return;

        EnsureBootstrap();
        _ = CoconutScoreManager.Instance;

        if (_waitRoutine == null)
            _waitRoutine = StartCoroutine(WaitForTimerThenSpawn());
    }

    private IEnumerator WaitForTimerThenSpawn()
    {
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
            if (CoconutSpawnCenterA == null && CoconutSpawnCenterB == null)
            {
                yield return null;
                continue;
            }

            if (maxCoconutsAlive > 0 && _aliveCount >= maxCoconutsAlive)
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
        Transform chosen = ChooseSpawnCenter();
        if (chosen == null) return;

        Vector3 pos = GetRandomPointInRing(chosen);

        GameObject go;
        if (coconutPrefab != null)
        {
            go = Instantiate(coconutPrefab, pos, coconutPrefab.transform.rotation);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 0.25f;
        }

        go.name = "Coconut";

        // 코코넛은 매달린 상태 -> 기본은 물리 비활성(Behavior에서 처리)
        var behavior = go.GetComponent<CoconutBehavior>();
        if (behavior == null) behavior = go.AddComponent<CoconutBehavior>();

        behavior.SetOwner(this);
        behavior.growDuration = Mathf.Max(0.01f, growDuration);
        behavior.startScale = Mathf.Max(0.0001f, startScale);
        behavior.targetScaleMultiplier = Mathf.Max(0.0001f, targetScaleMultiplier);
        behavior.points = pointsPerCoconut;

        // Rigidbody/Collider 보장
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        var col = go.GetComponent<Collider>();
        if (col == null) col = go.AddComponent<SphereCollider>();
        col.isTrigger = false;

        _aliveCount++;
    }

    private Transform ChooseSpawnCenter()
    {
        if (CoconutSpawnCenterA != null && CoconutSpawnCenterB != null)
            return Random.value < 0.5f ? CoconutSpawnCenterA : CoconutSpawnCenterB;
        return CoconutSpawnCenterA != null ? CoconutSpawnCenterA : CoconutSpawnCenterB;
    }

    private Vector3 GetRandomPointInRing(Transform centerTransform)
    {
        // 링 반지름 보정 (XZ 평면 기준)
        float rMin = Mathf.Max(0f, innerRadius);
        float rMax = Mathf.Max(rMin + 0.001f, outerRadius);

        // 균일 분포: r^2 를 선형 보간 후 sqrt
        float u = Random.value;
        float v = Random.value;
        float rSquared = Mathf.Lerp(rMin * rMin, rMax * rMax, u);
        float r = Mathf.Sqrt(rSquared);
        float theta = v * Mathf.PI * 2f;

        // XZ 평면에만 퍼지도록: (cos, 0, sin)
        Vector3 center = centerTransform.position;
        float dx = Mathf.Cos(theta) * r;
        float dz = Mathf.Sin(theta) * r;

        return new Vector3(center.x + dx, center.y, center.z + dz);
    }

    public void NotifyCoconutDestroyed(CoconutBehavior coconut)
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }

    private void EnsureBootstrap()
    {
        if (CoconutSpawnCenterA != null && CoconutSpawnCenterB != null) return;

        // 기존 씬 오브젝트가 있다면 우선 사용
        var aGo = GameObject.Find("CoconutSpawnPointA") ?? GameObject.Find("CoconutSpawnPoint1") ?? GameObject.Find("CoconutSpawnPoint");
        var bGo = GameObject.Find("CoconutSpawnPointB") ?? GameObject.Find("CoconutSpawnPoint2");

        if (aGo == null)
            aGo = new GameObject("CoconutSpawnPointA");
        if (bGo == null)
            bGo = new GameObject("CoconutSpawnPointB");

        // 씬에 코코넛 모델이 있으면 그 주변을 기본 위치로 잡기
        var target = GameObject.Find("coconut") ?? GameObject.Find("Coconut") ?? GameObject.Find("coconut2") ?? GameObject.Find("coconut3");
        if (target != null)
        {
            if (aGo.transform.position == Vector3.zero) aGo.transform.position = target.transform.position + new Vector3(-1.2f, 0.8f, 0f);
            if (bGo.transform.position == Vector3.zero) bGo.transform.position = target.transform.position + new Vector3(1.2f, 0.8f, 0f);
        }

        CoconutSpawnCenterA = aGo.transform;
        CoconutSpawnCenterB = bGo.transform;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "09Coconut" && sceneName != "08Coconut")
            return;

        var mgr = Object.FindFirstObjectByType<CoconutManager>();
        if (mgr == null)
        {
            var go = new GameObject("CoconutManager");
            mgr = go.AddComponent<CoconutManager>();
        }

        mgr.EnsureBootstrap();
        _ = CoconutScoreManager.Instance;
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

