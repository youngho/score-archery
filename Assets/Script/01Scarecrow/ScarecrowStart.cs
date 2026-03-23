using UnityEngine;
using UnityEngine.UI;

public class ScarecrowStart : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The entire popup object that should be hidden")]
    public GameObject popupObject;
    
    [Tooltip("The panel inside the popup that intercepts clicks")]
    public GameObject clickPanel;

    [Header("Game References")]
    [Tooltip("인스펙터에서 명시적으로 Timer 오브젝트(컴포넌트)를 지정하세요.")]
    [SerializeField] private Timer timer;

    private void Start()
    {
        // Default to this object if not assigned
        if (popupObject == null) popupObject = gameObject;

        // Default to popupObject if clickPanel not assigned
        if (clickPanel == null) clickPanel = popupObject;

        // Setup the button for the click panel
        Button panelButton = clickPanel.GetComponent<Button>();
        if (panelButton == null)
        {
            panelButton = clickPanel.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
        }

        panelButton.onClick.AddListener(OnDescriptionClicked);

        StageDescriptionPopupAudio.PlayReady();

        Debug.Log($"[ScarecrowStart] Initialized. HidingTarget: {popupObject.name}, ClickTarget: {clickPanel.name}");
        if (timer == null) Debug.LogError("[ScarecrowStart] Timer가 인스펙터에 할당되지 않았습니다. (자동 탐색은 하지 않습니다)");
    }

    private void OnDescriptionClicked()
    {
        Debug.Log("[ScarecrowStart] Description Clicked - Starting Game...");

        StageDescriptionPopupAudio.PlayGo();

        // Hide the entire popup
        if (popupObject != null)
        {
            popupObject.SetActive(false);
        }

        // Start game components
        if (timer != null)
        {
            timer.StartTimer();
        }
    }
}
