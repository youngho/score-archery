using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameDescriptionPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public Button startButton;
   

    [Header("Game Controllers")]
    public BalloonSpawner spawner;
    public Timer timer;

    private void Start()
    {
        // Find references if not set
        if (popupPanel == null) popupPanel = gameObject;
        
  
        
        if (spawner == null) spawner = FindFirstObjectByType<BalloonSpawner>();
        if (timer == null) timer = FindFirstObjectByType<Timer>();

        // Ensure popup is visible at start
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            
            // Add or get Button component on the panel itself to handle clicks anywhere
            Button panelButton = popupPanel.GetComponent<Button>();
            if (panelButton == null)
            {
                panelButton = popupPanel.AddComponent<Button>();
                // Set transition to None to avoid visual changes when clicking the background
                panelButton.transition = Selectable.Transition.None;
            }
            panelButton.onClick.AddListener(OnStartButtonClicked);
        }


    }

    private void OnStartButtonClicked()
    {
        // Hide popup
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        // Start game components
        if (spawner != null)
        {
            spawner.StartSpawning();
        }

        if (timer != null)
        {
            timer.StartTimer();
        }
    }
}
