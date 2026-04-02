using System.Collections.Generic;
using UnityEngine;

/// 월드 X축 방향으로 타겟 이동 (±)
public enum MenTargetSpawnMoveAxis
{
    PositiveX,
    NegativeX
}

/// <summary>
/// 03MenTarget: 스폰·직선 이동(나감/복귀). 회전·대기는 MenTargetBehavior.
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

    [Header("이동 (월드 직선)")]
    public float moveDistance = 17f;
    public float moveSpeed = 2f;
    [Tooltip("이동 구간 중 멈춰서 회전을 시작할 거리 비율 범위")]
    public float randomStopMin = 0.2f;
    public float randomStopMax = 0.85f;

    private enum MenTargetMovePhase
    {
        MovingOut,
        Rotating,
        MovingBack
    }

    private sealed class MenTargetMoveState
    {
        public MenTargetBehavior Behavior;
        public Transform Transform;
        public Vector3 StartPosition;
        public Vector3 Direction;
        public float Speed;
        public float StopDistance;
        public float Traveled;
        public MenTargetMovePhase Phase;
    }

    private bool _sceneActive;
    private Timer _timer;
    private float _nextSpawnTime;
    private int _aliveCount;
    private readonly List<MenTargetMoveState> _movingTargets = new List<MenTargetMoveState>(8);

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

        TickTargetMovement(Time.deltaTime);

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

    private void TickTargetMovement(float dt)
    {
        for (int i = _movingTargets.Count - 1; i >= 0; i--)
        {
            MenTargetMoveState s = _movingTargets[i];
            if (s.Behavior == null || s.Transform == null)
            {
                _movingTargets.RemoveAt(i);
                continue;
            }

            switch (s.Phase)
            {
                case MenTargetMovePhase.MovingOut:
                    float step = s.Speed * dt;
                    s.Traveled += step;
                    s.Transform.position += s.Direction * step;
                    if (s.Traveled >= s.StopDistance)
                    {
                        s.Phase = MenTargetMovePhase.Rotating;
                        MenTargetBehavior bh = s.Behavior;
                        bh.BeginRotationSequence(() => OnTargetRotationFinished(bh));
                    }

                    break;

                case MenTargetMovePhase.MovingBack:
                    s.Transform.position = Vector3.MoveTowards(s.Transform.position, s.StartPosition, s.Speed * dt);
                    if (Vector3.Distance(s.Transform.position, s.StartPosition) <= 0.05f)
                    {
                        s.Transform.position = s.StartPosition;
                        _movingTargets.RemoveAt(i);
                        Destroy(s.Behavior.gameObject);
                    }

                    break;
            }
        }
    }

    private void OnTargetRotationFinished(MenTargetBehavior behavior)
    {
        for (int i = 0; i < _movingTargets.Count; i++)
        {
            MenTargetMoveState s = _movingTargets[i];
            if (s.Behavior == behavior && s.Phase == MenTargetMovePhase.Rotating)
            {
                s.Phase = MenTargetMovePhase.MovingBack;
                return;
            }
        }
    }

    private void SpawnOne()
    {
        if (!TryPickSpawn(out Transform spawnPoint, out Vector3 moveDir))
            return;
        if (targetPrefab == null)
            return;

        // 스폰 포인트 회전은 이동 방향만 쓰고, 과녘 방향은 프리팹 기본(씬에 직접 배치했을 때와 동일)로 둔다.
        GameObject targetGo = Instantiate(targetPrefab, spawnPoint.position, targetPrefab.transform.rotation);
        _aliveCount++;

        MenTargetBehavior behavior = targetGo.GetComponent<MenTargetBehavior>();
        if (behavior == null)
            behavior = targetGo.AddComponent<MenTargetBehavior>();

        behavior.Configure(this);
        behavior.CaptureInitialRotationForMovement();

        float tLow = Mathf.Min(randomStopMin, randomStopMax);
        float tHigh = Mathf.Max(randomStopMin, randomStopMax);
        float stopFraction = Random.Range(tLow, tHigh);

        var state = new MenTargetMoveState
        {
            Behavior = behavior,
            Transform = targetGo.transform,
            StartPosition = spawnPoint.position,
            Direction = moveDir.normalized,
            Speed = moveSpeed,
            StopDistance = Mathf.Max(0.01f, moveDistance) * stopFraction,
            Traveled = 0f,
            Phase = MenTargetMovePhase.MovingOut
        };
        _movingTargets.Add(state);
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

    public void UnregisterMovementTarget(MenTargetBehavior behavior)
    {
        for (int i = _movingTargets.Count - 1; i >= 0; i--)
        {
            if (_movingTargets[i].Behavior == behavior)
            {
                _movingTargets.RemoveAt(i);
                return;
            }
        }
    }

    public void NotifyMenTargetDestroyed()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }
}
