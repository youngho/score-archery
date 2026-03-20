using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 03MenTarget: MenTargetSpawnPoint(또는 spawnPoints) 중 임의 위치에서 타겟 프리팹을 생성.
/// Timer(StageTimer)가 실제로 카운트를 시작(IsRunning)한 뒤부터 스폰 루프를 돈다.
/// </summary>
[DisallowMultipleComponent]
public class MenTargetManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject targetPrefab;

    [Tooltip("스폰 위치 배열. 비어 있지 않으면 MenTargetSpawnPoint 1~3 슬롯보다 우선합니다.")]
    public Transform[] spawnPoints;

    [Header("MenTargetSpawnPoint 1~3 (옵션)")]
    public Transform menTargetSpawnPoint1;
    public Transform menTargetSpawnPoint2;
    public Transform menTargetSpawnPoint3;

    public float spawnInterval = 3f;
    public int maxTargetsAtOnce = 5;

    [Header("Movement (타겟에 전달)")]
    public float moveDistance = 5f;
    public float moveSpeed = 2f;
    [Tooltip("체크 해제 시 아래 moveDirection 벡터로 이동")]
    public bool moveAlongSpawnForward = true;
    public Vector3 moveDirection = Vector3.forward;

    [Header("Rail (옵션, 구 RailMovement)")]
    [Tooltip("활성 시 레일을 따라 local X로 왕복 이동 (스폰 포인트 부모에 넣거나 railTransform 지정)")]
    public bool railMovementEnabled;
    [Tooltip("비어 있으면 MenTargetManager가 붙은 오브젝트")]
    public Transform railTransform;
    public float railSpeed = 2f;
    public float railLeftBound = -7f;
    public float railRightBound = 7f;
    public bool railIsMoving = true;

    private bool _spawning;
    private int _activeTargets;
    private Coroutine _bootstrapRoutine;
    private Coroutine _spawnRoutine;
    private Timer _timer;
    private int _railDirection = 1;

    private void Awake()
    {
        ApplyMenTargetSpawnSlotsIfAny();
    }

    private void OnDestroy()
    {
        if (_timer != null)
        {
            _timer.onTimerEnd.RemoveListener(StopSpawning);
            _timer = null;
        }
    }

    private void ApplyMenTargetSpawnSlotsIfAny()
    {
        var list = new List<Transform>(3);
        if (menTargetSpawnPoint1 != null) list.Add(menTargetSpawnPoint1);
        if (menTargetSpawnPoint2 != null) list.Add(menTargetSpawnPoint2);
        if (menTargetSpawnPoint3 != null) list.Add(menTargetSpawnPoint3);
        if (list.Count > 0)
            spawnPoints = list.ToArray();
    }

    private void Start()
    {
        _ = MenTargetScoreManager.Instance;
        if (_bootstrapRoutine == null)
            _bootstrapRoutine = StartCoroutine(WaitForTimerThenSpawn());
    }

    private void Update()
    {
        if (!railMovementEnabled || !railIsMoving)
            return;

        Transform rail = railTransform != null ? railTransform : transform;
        Vector3 pos = rail.localPosition;
        pos.x += _railDirection * railSpeed * Time.deltaTime;

        if (pos.x >= railRightBound)
        {
            pos.x = railRightBound;
            _railDirection = -1;
        }
        else if (pos.x <= railLeftBound)
        {
            pos.x = railLeftBound;
            _railDirection = 1;
        }

        rail.localPosition = pos;
    }

    private IEnumerator WaitForTimerThenSpawn()
    {
        while (_timer == null)
        {
            _timer = Object.FindFirstObjectByType<Timer>();
            yield return null;
        }

        while (!_timer.IsRunning)
            yield return null;

        _timer.onTimerEnd.RemoveListener(StopSpawning);
        _timer.onTimerEnd.AddListener(StopSpawning);

        if (_spawnRoutine == null)
        {
            _spawning = true;
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        _bootstrapRoutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        TrySpawnOne();
        while (_spawning && _timer != null && _timer.IsRunning)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnOne();
        }

        _spawnRoutine = null;
    }

    private void TrySpawnOne()
    {
        if (!_spawning || _timer == null || !_timer.IsRunning)
            return;
        if (targetPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;
        if (maxTargetsAtOnce > 0 && _activeTargets >= maxTargetsAtOnce)
            return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject target = Instantiate(targetPrefab, spawnPoint.position, spawnPoint.rotation);
        _activeTargets++;

        MenTargetBehavior behavior = target.GetComponent<MenTargetBehavior>();
        if (behavior == null)
            behavior = target.AddComponent<MenTargetBehavior>();

        Vector3 dir = moveAlongSpawnForward ? spawnPoint.forward : moveDirection;
        behavior.Configure(this, moveDistance, moveSpeed, dir);
    }

    public void NotifyMenTargetDestroyed()
    {
        _activeTargets = Mathf.Max(0, _activeTargets - 1);
    }

    public void StopSpawning()
    {
        _spawning = false;
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    /// <summary>디버그·외부 연동용</summary>
    public void StartSpawning()
    {
        _spawning = true;
        if (_spawnRoutine == null)
            _spawnRoutine = StartCoroutine(SpawnLoop());
    }
}
