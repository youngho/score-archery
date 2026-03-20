using UnityEngine;

/// 월드 X축 방향으로 타겟 이동 (±)
public enum MenTargetSpawnMoveAxis
{
    PositiveX,
    NegativeX
}

/// <summary>
/// 03MenTarget 씬 전용:
/// - MenTargetSpawnPoint 1~3 중 임의 선택 후 타겟 프리팹 생성
/// - 스폰포인트마다 X축 + 또는 - 방향 지정
/// - Timer(StageTimer)가 실행 중(IsRunning)일 때 spawnInterval마다 스폰
/// </summary>
[DisallowMultipleComponent]
public class MenTargetManager : MonoBehaviour
{
    [Header("MenTargetSpawnPoint 1")]
    public Transform menTargetSpawnPoint1;
    [Tooltip("월드 X축 + 또는 - 방향으로 이동")]
    public MenTargetSpawnMoveAxis menTargetSpawnPoint1MoveAxis = MenTargetSpawnMoveAxis.PositiveX;

    [Header("MenTargetSpawnPoint 2")]
    public Transform menTargetSpawnPoint2;
    public MenTargetSpawnMoveAxis menTargetSpawnPoint2MoveAxis = MenTargetSpawnMoveAxis.PositiveX;

    [Header("MenTargetSpawnPoint 3")]
    public Transform menTargetSpawnPoint3;
    public MenTargetSpawnMoveAxis menTargetSpawnPoint3MoveAxis = MenTargetSpawnMoveAxis.PositiveX;

    [Header("Target Prefab")]
    public GameObject targetPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    [Tooltip("동시 타겟 상한(0이면 제한 없음)")]
    public int maxTargetsAtOnce = 5;

    [Header("Movement (타겟에 전달)")]
    public float moveDistance = 5f;
    public float moveSpeed = 2f;

    private bool _sceneActive;
    private Timer _timer;
    private float _nextSpawnTime;
    private int _aliveCount;

    private void Start()
    {
        _sceneActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "03MenTarget";
        if (!_sceneActive)
            return;

        _ = MenTargetScoreManager.Instance;
        _nextSpawnTime = 0f;
    }

    private void Update()
    {
        if (!_sceneActive)
            return;

        if (_timer == null)
            _timer = Object.FindFirstObjectByType<Timer>();
        if (_timer == null || !_timer.IsRunning)
            return;

        if (maxTargetsAtOnce > 0 && _aliveCount >= maxTargetsAtOnce)
            return;

        if (Time.time < _nextSpawnTime)
            return;

        SpawnOne();
        _nextSpawnTime = Time.time + Mathf.Max(0.01f, spawnInterval);
    }

    private void SpawnOne()
    {
        if (!TryPickSpawn(out Transform spawnPoint, out Vector3 moveDir))
            return;
        if (targetPrefab == null)
            return;

        GameObject target = Instantiate(targetPrefab, spawnPoint.position, spawnPoint.rotation);
        _aliveCount++;

        MenTargetBehavior behavior = target.GetComponent<MenTargetBehavior>();
        if (behavior == null)
            behavior = target.AddComponent<MenTargetBehavior>();

        behavior.Configure(this, moveDistance, moveSpeed, moveDir);
    }

    private bool TryPickSpawn(out Transform spawnPoint, out Vector3 moveDir)
    {
        moveDir = Vector3.right;
        spawnPoint = null;

        int n = (menTargetSpawnPoint1 != null ? 1 : 0)
                + (menTargetSpawnPoint2 != null ? 1 : 0)
                + (menTargetSpawnPoint3 != null ? 1 : 0);
        if (n == 0)
            return false;

        int idx = Random.Range(0, n);
        if (menTargetSpawnPoint1 != null)
        {
            if (idx == 0)
            {
                spawnPoint = menTargetSpawnPoint1;
                moveDir = AxisToWorldDirection(menTargetSpawnPoint1MoveAxis);
                return true;
            }

            idx--;
        }

        if (menTargetSpawnPoint2 != null)
        {
            if (idx == 0)
            {
                spawnPoint = menTargetSpawnPoint2;
                moveDir = AxisToWorldDirection(menTargetSpawnPoint2MoveAxis);
                return true;
            }

            idx--;
        }

        spawnPoint = menTargetSpawnPoint3;
        moveDir = AxisToWorldDirection(menTargetSpawnPoint3MoveAxis);
        return spawnPoint != null;
    }

    private static Vector3 AxisToWorldDirection(MenTargetSpawnMoveAxis axis)
    {
        return axis == MenTargetSpawnMoveAxis.NegativeX ? Vector3.left : Vector3.right;
    }

    public void NotifyMenTargetDestroyed()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }
}
