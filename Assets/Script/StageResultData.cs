using UnityEngine;

/// <summary>
/// 씬 전환 시 99StageResult로 넘길 점수/데이터를 보관.
/// StageResultService.RequestShowResult 호출 시 설정되고, 99StageResult에서 읽어서 표시·이동.
/// </summary>
public static class StageResultData
{
    /// <summary>
    /// 결과 씬에 전달할 마지막 점수
    /// </summary>
    public static int LastScore { get; set; }

    /// <summary>
    /// 결과 씬에 전달할 총 발사 화살 수
    /// </summary>
    public static int TotalArrowsShot { get; set; }

    /// <summary>
    /// 결과 씬에 전달할 총 명중 수
    /// </summary>
    public static int TotalHits { get; set; }

    /// <summary>
    /// 결과 확인 후 이동할 씬 이름 (예: 00StartUI, 다음 스테이지 등)
    /// </summary>
    public static string ReturnSceneName { get; set; } = "00StartUI";
}
