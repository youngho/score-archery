using UnityEngine;
using UnityEngine.UI;

public class NicknamePopupController : MonoBehaviour
{
    private Button nicknamePopupBtn;
    private GameObject nicknamePopup;
    private Button closeButton;
    private Button confirmButton;

    private void Awake()
    {
        // Find references by path since direct assignment via MCP tool had issues with types
        nicknamePopup = GameObject.Find("StartUICanvas/NicknamePopup");
        
        GameObject openBtnGo = GameObject.Find("StartUICanvas/StartPanel/nicknameBack/nicknamePopupBtn");
        if (openBtnGo != null) nicknamePopupBtn = openBtnGo.GetComponent<Button>();

        GameObject closeBtnGo = GameObject.Find("StartUICanvas/NicknamePopup/Header/CloseButton");
        if (closeBtnGo != null) closeButton = closeBtnGo.GetComponent<Button>();

        GameObject confirmBtnGo = GameObject.Find("StartUICanvas/NicknamePopup/NicknameChangeConfirmBtn");
        if (confirmBtnGo != null) confirmButton = confirmBtnGo.GetComponent<Button>();
    }

    private void Start()
    {
        // Ensure the popup is hidden at startup
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(false);
        }

        // Add listeners to buttons
        if (nicknamePopupBtn != null)
        {
            nicknamePopupBtn.onClick.AddListener(ShowPopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePopup);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(HidePopup);
        }
    }

    public void ShowPopup()
    {
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(true);
        }
    }

    public void HidePopup()
    {
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (nicknamePopupBtn != null) nicknamePopupBtn.onClick.RemoveListener(ShowPopup);
        if (closeButton != null) closeButton.onClick.RemoveListener(HidePopup);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(HidePopup);
    }
}
