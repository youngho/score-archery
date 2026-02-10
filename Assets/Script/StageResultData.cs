using UnityEngine;

/// <summary>
/// 씬 전환 시 99StageResult로 넘길 점수/데이터를 보관.
/// 04Balloon 등에서 씬 로드 직전에 LastScore를 설정하고, 99StageResult에서 읽어서 표시.
/// </summary>
public static class StageResultData
{
    /// <summary>
    /// 결과 씬에 전달할 마지막 점수 (씬 로드 직전에 설정됨)
    /// </summary>
    public static int LastScore { get; set; }
}
