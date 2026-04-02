using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 06AppleOneShot 씬 전용: 한 발 발사 후 제스처 잠금. 씬(재)로드 시 Reset()으로 초기화.
/// </summary>
public static class AppleOneShotStageRules
{
    public const string SceneName = "06AppleOneShot";

    public static bool IsAppleOneShotScene => SceneManager.GetActiveScene().name == SceneName;

    /// <summary>이번 씬에서 이미 화살을 한 발 쏜 뒤면 true → 새 드로우 차단.</summary>
    public static bool HasFiredSingleArrow { get; private set; }

    public static void Reset()
    {
        HasFiredSingleArrow = false;
    }

    public static void MarkArrowFiredIfApplicable()
    {
        if (!IsAppleOneShotScene) return;
        HasFiredSingleArrow = true;
        // 사과 명중 여부와 무관: 이후 재발사는 막혀 있으므로 N초 뒤 타이머 종료와 동일하게 마무리
        AppleManager.NotifySingleArrowFiredInThisScene();
    }

    public static bool ShouldBlockBowGestures()
    {
        return IsAppleOneShotScene && HasFiredSingleArrow;
    }
}
