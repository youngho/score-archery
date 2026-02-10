using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모든 스테이지 종료 시 99StageResult를 모듈처럼 호출하는 공용 서비스.
/// 점수 기록 후 RequestShowResult를 호출하면 결과 화면을 표시하고, 확인 시 00StartUI로 이동.
///
/// 사용 예 (어떤 스테이지에서든):
///   var recorder = GetComponent&lt;StageScoreApiService&gt;() ?? gameObject.AddComponent&lt;StageScoreApiService&gt;();
///   recorder.RecordCurrentStageAndThen(() =&gt;
///       StageResultService.RequestShowResult(ScoreManager.Instance.CurrentScore));
/// </summary>
public static class StageResultService
{
    private const string ResultSceneName = "99StageResult";
    private const string ReturnSceneName = "00StartUI";

    /// <summary>
    /// 결과 화면(99StageResult)을 표시. 점수 기록은 호출측에서 완료한 뒤 호출.
    /// 화면이 어두어지는 트랜지션 후 로드되며, 확인 버튼 클릭 시 00StartUI로 이동.
    /// </summary>
    /// <param name="score">표시할 점수</param>
    public static void RequestShowResult(int score)
    {
        SceneTransitionFader.FadeToBlackAndLoad(score, ResultSceneName, ReturnSceneName, 0.6f);
    }
}
