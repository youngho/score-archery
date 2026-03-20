using UnityEngine;

/// <summary>
/// 03MenTarget 씬 전용:
/// - MenTargetSpawnPoint 1~3 중 임의 선택 후 타겟 프리팹 생성
/// - Timer(StageTimer)가 실행 중(IsRunning)일 때 spawnInterval마다 스폰
/// - 타겟 이동·회전은 MenTargetBehavior
/// </summary>
[DisallowMultipleComponent]
public class MenTargetManager : MonoBehaviour
{
    [Header("MenTargetSpawnPoint 1~3")]
    public Transform menTargetSpawnPoint1;
    public Transform menTargetSpawnPoint2;
    public Transform menTargetSpawnPoint3;

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
        Transform spawnPoint = ChooseSpawnPoint();
        if (spawnPoint == null || targetPrefab == null)
            return;

        GameObject target = Instantiate(targetPrefab, spawnPoint.position, spawnPoint.rotation);
        _aliveCount++;

        MenTargetBehavior behavior = target.GetComponent<MenTargetBehavior>();
        if (behavior == null)
            behavior = target.AddComponent<MenTargetBehavior>();

        behavior.Configure(this, moveDistance, moveSpeed, spawnPoint.forward);
    }

    private Transform ChooseSpawnPoint()
    {
        int n = (menTargetSpawnPoint1 != null ? 1 : 0)
                + (menTargetSpawnPoint2 != null ? 1 : 0)
                + (menTargetSpawnPoint3 != null ? 1 : 0);
        if (n == 0)
            return null;

        int idx = Random.Range(0, n);
        if (menTargetSpawnPoint1 != null)
        {
            if (idx == 0) return menTargetSpawnPoint1;
            idx--;
        }

        if (menTargetSpawnPoint2 != null)
        {
            if (idx == 0) return menTargetSpawnPoint2;
            idx--;
        }

        return menTargetSpawnPoint3;
    }

    public void NotifyMenTargetDestroyed()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }
}
