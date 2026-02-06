using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NicknamePopupController : MonoBehaviour
{
    private Button nicknamePopupBtn;
    private GameObject nicknamePopup;
    private Button closeButton;
    private Button confirmButton;
    
    // New references
    private TMP_InputField nicknameInputField;
    private GameObject confirmSpinner;

    private void Awake()
    {
        // Find references by path
        nicknamePopup = GameObject.Find("StartUICanvas/NicknamePopup");
        
        GameObject openBtnGo = GameObject.Find("StartUICanvas/StartPanel/nicknameBack/nicknamePopupBtn");
        if (openBtnGo != null) nicknamePopupBtn = openBtnGo.GetComponent<Button>();

        GameObject closeBtnGo = GameObject.Find("StartUICanvas/NicknamePopup/Header/CloseButton");
        if (closeBtnGo != null) closeButton = closeBtnGo.GetComponent<Button>();

        GameObject confirmBtnGo = GameObject.Find("StartUICanvas/NicknamePopup/NicknameChangeConfirmBtn");
        if (confirmBtnGo != null) confirmButton = confirmBtnGo.GetComponent<Button>();

        // Find InputField and Spinner
        GameObject inputFieldGo = GameObject.Find("StartUICanvas/NicknamePopup/nicknameChangeTxt");
        if (inputFieldGo != null) nicknameInputField = inputFieldGo.GetComponent<TMP_InputField>();

        confirmSpinner = GameObject.Find("StartUICanvas/NicknamePopup/ConfirmSpinner");
    }

    private void Start()
    {
        // Ensure the popup and spinner are hidden at startup
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(false);
        }

        if (confirmSpinner != null)
        {
            confirmSpinner.SetActive(false);
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
            confirmButton.onClick.AddListener(OnConfirmNicknameChange);
        }
    }

    public void ShowPopup()
    {
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(true);
            
            // Focus and set initial value if needed
            if (nicknameInputField != null)
            {
                nicknameInputField.text = UserAccountManagerScript.Instance.Nickname;
            }
        }
    }

    public void HidePopup()
    {
        if (nicknamePopup != null)
        {
            nicknamePopup.SetActive(false);
        }
        
        if (confirmSpinner != null)
        {
            confirmSpinner.SetActive(false);
        }
    }

    private void OnConfirmNicknameChange()
    {
        if (nicknameInputField == null || string.IsNullOrEmpty(nicknameInputField.text))
        {
            Debug.LogWarning("[NicknamePopupController] New nickname is empty.");
            return;
        }

        string newNickname = nicknameInputField.text;

        // Reset error message if using input field for display or clear previous error
        if (nicknameInputField != null) 
        {
            // Reset to normal style if changed
            nicknameInputField.textComponent.color = Color.white; 
        }

        // Show spinner
        if (confirmSpinner != null) confirmSpinner.SetActive(true);

        // Call UserAccountManagerScript
        StartCoroutine(UserAccountManagerScript.Instance.ChangeNickname(newNickname, (success, message) => {
            // Hide spinner on response
            if (confirmSpinner != null) confirmSpinner.SetActive(false);

            if (success)
            {
                Debug.Log($"[NicknamePopupController] Nickname changed to: {message}");
                HidePopup();
            }
            else
            {
                Debug.LogError($"[NicknamePopupController] Failed to change nickname: {message}");
                
                // Display "Server Error" on failure or timeout
                if (nicknameInputField != null)
                {
                    nicknameInputField.text = "Server Error";
                    nicknameInputField.textComponent.color = Color.red;
                }
            }
        }));
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (nicknamePopupBtn != null) nicknamePopupBtn.onClick.RemoveListener(ShowPopup);
        if (closeButton != null) closeButton.onClick.RemoveListener(HidePopup);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmNicknameChange);
    }
}
