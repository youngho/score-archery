using UnityEngine;
using TMPro;

public class NicknameDisplay : MonoBehaviour
{
    private TextMeshProUGUI nicknameText;

    private void Start()
    {
        nicknameText = GetComponent<TextMeshProUGUI>();
        UpdateNickname();
    }

    private void UpdateNickname()
    {
        if (UserAccountManagerScript.Instance != null && nicknameText != null)
        {
            nicknameText.text = UserAccountManagerScript.Instance.Nickname;
        }
        else
        {
            // Fallback if Instance is not yet set (though InitializeOnLoad should have run)
            // Or try to load it manually if needed, but for now let's just log or wait.
            // Since InitializeOnLoad is AfterSceneLoad, it might race with Start. 
            // Better to check in Update or Coroutine if null? 
            // Actually AfterSceneLoad runs after Awake but before Start usually? 
            // Let's rely on Instance for now.
             Debug.LogWarning("UserAccountManagerScript Instance is null or Text component missing.");
        }
    }

    /// <summary>
    /// 닉네임 변경 후 StartPanel 표시를 갱신할 때 호출합니다.
    /// </summary>
    public void RefreshNickname()
    {
        if (nicknameText == null)
            nicknameText = GetComponent<TextMeshProUGUI>();
        UpdateNickname();
    }
}
