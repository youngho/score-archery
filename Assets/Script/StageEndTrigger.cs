using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지 종료 시 점수를 API에 기록하고 다음 씬으로 이동.
/// BalloonTimer 외의 스테이지에서도 사용 가능.
/// </summary>
public class StageEndTrigger : MonoBehaviour
{
    [Tooltip("스테이지 종료 시 이동할 씬 이름 (예: 00StartUI)")]
    public string nextSceneName = "00StartUI";

    [Tooltip("월드 번호 (1부터, 씬에서 파싱 불가 시 사용)")]
    public int worldNumber = 1;

    [Tooltip("스테이지 번호 (1부터, 0이면 현재 씬 이름에서 파싱)")]
    public int stageNumberOverride = 0;

    private bool _hasTriggered;

    /// <summary>
    /// 외부에서 스테이지 종료 시 호출 (예: 목표 달성, 타임업 등)
    /// </summary>
    public void TriggerStageEnd()
    {
        if (_hasTriggered) return;
        _hasTriggered = true;

        var recorder = GetComponent<StageScoreApiService>() ?? gameObject.AddComponent<StageScoreApiService>();
        int w = worldNumber;
        int s = stageNumberOverride > 0 ? stageNumberOverride : 1;
        if (stageNumberOverride <= 0 && StageScoreApiService.TryParseStageFromScene(SceneManager.GetActiveScene().name, out _, out int parsedStage))
            s = parsedStage;

        long score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        StartCoroutine(RecordAndLoadCoroutine(recorder, w, s, score));
    }

    private System.Collections.IEnumerator RecordAndLoadCoroutine(StageScoreApiService recorder, int worldNum, int stageNum, long score)
    {
        bool done = false;
        yield return recorder.RecordScoreAsync(worldNum, stageNum, score, 0, 0, (_, _) => { done = true; });
        while (!done) yield return null;

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
}
