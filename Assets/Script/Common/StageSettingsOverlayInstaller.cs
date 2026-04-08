using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스테이지(01~12) 씬 로드시, 해당 씬의 Canvas 아래에 Settings 오버레이 UI를 동적으로 생성합니다.
/// </summary>
public static class StageSettingsOverlayInstaller
{
    private static bool _bootstrapped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_bootstrapped) return;
        _bootstrapped = true;

        SceneManager.sceneLoaded += (_, __) => TryInstall(SceneManager.GetActiveScene().name);
        TryInstall(SceneManager.GetActiveScene().name);
    }

    private static void TryInstall(string sceneName)
    {
        if (!IsStageScene(sceneName))
            return;

        // 중복 방지
        // (1) 런타임 UI가 이미 있으면 스킵
        if (Object.FindFirstObjectByType<SettingsOverlayRuntimeUI>() != null)
            return;

        // (2) 프리팹 기반 UI(= SettingsUIController)가 이미 있으면 스킵
        if (Object.FindFirstObjectByType<SettingsUIController>() != null)
            return;

        var canvasGo = GameObject.Find("ArcheryUI") ?? GameObject.Find("ShooterUI");
        if (canvasGo == null)
            return;

        var overlayGo = new GameObject("SettingsOverlayRuntime");
        overlayGo.layer = 5; // UI
        overlayGo.transform.SetParent(canvasGo.transform, false);
        overlayGo.AddComponent<SettingsOverlayRuntimeUI>();
    }

    private static bool IsStageScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName.Length < 2)
            return false;

        if (!char.IsDigit(sceneName[0]) || !char.IsDigit(sceneName[1]))
            return false;

        int num = (sceneName[0] - '0') * 10 + (sceneName[1] - '0');
        return num >= 1 && num <= 12;
    }
}

