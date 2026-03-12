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
    public Timer timer;

    private void Start()
    {
        // Default to this object if not assigned
        if (popupObject == null) popupObject = gameObject;

        // Default to popupObject if clickPanel not assigned
        if (clickPanel == null) clickPanel = popupObject;

        // Try to find Timer if not assigned
        if (timer == null)
        {
            timer = FindFirstObjectByType<Timer>();
        }

        // Setup the button for the click panel
        Button panelButton = clickPanel.GetComponent<Button>();
        if (panelButton == null)
        {
            panelButton = clickPanel.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
        }

        panelButton.onClick.AddListener(OnDescriptionClicked);
        
        Debug.Log($"[ScarecrowStart] Initialized. HidingTarget: {popupObject.name}, ClickTarget: {clickPanel.name}");
        if (timer == null) Debug.LogWarning("[ScarecrowStart] Timer not found in scene!");
    }

    private void OnDescriptionClicked()
    {
        Debug.Log("[ScarecrowStart] Description Clicked - Starting Game...");

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
