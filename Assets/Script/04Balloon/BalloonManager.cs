using UnityEngine;

public class BalloonManager : MonoBehaviour
{
    [SerializeField] private BalloonSpawner spawner;
    [SerializeField] private Timer stageTimer;

    private void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<BalloonSpawner>();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (stageTimer == null)
            stageTimer = FindFirstObjectByType<Timer>();

        if (stageTimer != null)
            stageTimer.onTimerStart.AddListener(StartBalloonSpawning);
    }

    private void OnDestroy()
    {
        if (stageTimer != null)
            stageTimer.onTimerStart.RemoveListener(StartBalloonSpawning);
    }

    public void StartBalloonSpawning()
    {
        if (spawner != null)
            spawner.StartSpawning();
    }
}
