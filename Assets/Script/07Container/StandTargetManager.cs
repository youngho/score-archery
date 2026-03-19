using UnityEngine;

/// <summary>
/// 07Container 씬용 스탠드 표적 매니저.
/// - 씬 내 StandTargetBehavior들을 찾아 공통 링 설정을 적용할 수 있음
/// - 필요 시 향후 스폰/리셋 로직을 확장하기 위한 자리
/// </summary>
[DisallowMultipleComponent]
public class StandTargetManager : MonoBehaviour
{
    [Header("Ring Settings (Global Override)")]
    [Tooltip("true일 경우, 아래 ringSettings를 씬 내 모든 StandTargetBehavior에 강제로 적용")]
    public bool applyGlobalRingSettings = true;

    [Tooltip("안쪽 링부터 바깥쪽 링 순서대로 설정 (Global Override 용)")]
    public StandTargetBehavior.RingScore[] ringSettings;

    private void Start()
    {
        // 07Container 씬에서만 동작하도록 제한
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "07Container")
            return;

        if (applyGlobalRingSettings)
            ApplyRingSettingsToAllTargets();
    }

    private void ApplyRingSettingsToAllTargets()
    {
        if (ringSettings == null || ringSettings.Length == 0)
            return;

        var targets = FindObjectsOfType<StandTargetBehavior>();
        foreach (var t in targets)
        {
            if (t == null) continue;
            t.ringScores = ringSettings;
        }
    }

    // 필요하면 여기서 스탠드 타겟 스폰/리셋 로직을 확장 가능
}

