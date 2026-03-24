using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingsUIController : MonoBehaviour
{
    private Button settingsButton;
    private RectTransform settingsMoreRect;
    
    // New references for music and sound
    private Button musicButton;
    private Button soundButton;
    private CanvasGroup settingsMoreCanvasGroup;

    private float animationDuration = 0.5f;
    private bool isExpanded = false;
    private Vector2 originalSize;

    private const string MusicKey = "MusicEnabled";
    private const string SoundKey = "SoundEnabled";

    private void Awake()
    {
        // Find references - paths based on Grep/previous context
        GameObject settingsBtnGo = GameObject.Find("StartUICanvas/StartPanel/Settings/Button_settings");
        if (settingsBtnGo == null) settingsBtnGo = GameObject.Find("Button_settings");
        if (settingsBtnGo != null) settingsButton = settingsBtnGo.GetComponent<Button>();

        GameObject settingsMoreGo = GameObject.Find("StartUICanvas/StartPanel/Settings/Button_settings_more");
        if (settingsMoreGo == null) settingsMoreGo = GameObject.Find("Button_settings_more");

        if (settingsMoreGo != null)
        {
            settingsMoreRect = settingsMoreGo.GetComponent<RectTransform>();
            originalSize = settingsMoreRect.sizeDelta;

            // Ensure CanvasGroup for fading buttons
            settingsMoreCanvasGroup = settingsMoreGo.GetComponent<CanvasGroup>();
            if (settingsMoreCanvasGroup == null) settingsMoreCanvasGroup = settingsMoreGo.AddComponent<CanvasGroup>();
            settingsMoreCanvasGroup.alpha = 0f; // Hidden by default

            // Set pivot to Right Center (1, 0.5) to expand to the left
            Vector2 previousPivot = settingsMoreRect.pivot;
            settingsMoreRect.pivot = new Vector2(1f, 0.5f);
            
            // Adjust position to prevent jumping when pivot changes
            Vector2 size = settingsMoreRect.rect.size;
            Vector2 deltaPivot = settingsMoreRect.pivot - previousPivot;
            Vector2 deltaPosition = new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);
            settingsMoreRect.anchoredPosition += deltaPosition;

            // Add HorizontalLayoutGroup to fix overlapping
            HorizontalLayoutGroup layout = settingsMoreGo.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = settingsMoreGo.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft; // Change to MiddleLeft to push to the left side
            layout.spacing = 15f; // Slightly more spacing
            layout.padding = new RectOffset(20, 80, 5, 5); // Large right padding to avoid the gear icon
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // Find Music and Sound buttons (checking children first)
        if (settingsMoreGo != null)
        {
            musicButton = settingsMoreGo.transform.Find("Button_music")?.GetComponent<Button>();
            soundButton = settingsMoreGo.transform.Find("Button_sound")?.GetComponent<Button>();
        }

        // Fallback search if not children
        if (musicButton == null) musicButton = GameObject.Find("Button_music")?.GetComponent<Button>();
        if (soundButton == null) soundButton = GameObject.Find("Button_sound")?.GetComponent<Button>();

        InitializeButtonStates();
    }

    private void Start()
    {
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
    }

    private void InitializeButtonStates()
    {
        // Load settings, default to 1 (true) if not set
        bool musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        bool soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;

        UpdateButtonVisuals(musicButton, musicEnabled);
        UpdateButtonVisuals(soundButton, soundEnabled);
    }

    private void OnSettingsClicked()
    {
        if (settingsMoreRect == null) return;

        StopAllCoroutines();
        if (!isExpanded)
        {
            StartCoroutine(ExpandSettings(originalSize.x * 4f, 1f));
            isExpanded = true;
        }
        else
        {
            StartCoroutine(ExpandSettings(originalSize.x, 0f));
            isExpanded = false;
        }
    }

    private void ToggleMusic()
    {
        bool current = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        current = !current;
        PlayerPrefs.SetInt(MusicKey, current ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtonVisuals(musicButton, current);
        Debug.Log("[SettingsUIController] Music: " + (current ? "ON" : "OFF"));
    }

    private void ToggleSound()
    {
        bool current = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        current = !current;
        PlayerPrefs.SetInt(SoundKey, current ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtonVisuals(soundButton, current);
        Debug.Log("[SettingsUIController] Sound: " + (current ? "ON" : "OFF"));
    }

    private void UpdateButtonVisuals(Button button, bool enabled)
    {
        if (button == null) return;
        // Use alpha to indicate ON/OFF state (1.0f for ON, 0.4f for OFF)
        var colorBlock = button.colors;
        Color normalColor = colorBlock.normalColor;
        normalColor.a = enabled ? 1f : 0.4f;
        colorBlock.normalColor = normalColor;
        button.colors = colorBlock;

        // Also update child image if exists (to be sure)
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = enabled ? 1f : 0.4f;
            img.color = c;
        }
    }

    private IEnumerator ExpandSettings(float targetWidth, float targetAlpha)
    {
        float startWidth = settingsMoreRect.sizeDelta.x;
        float startAlpha = settingsMoreCanvasGroup != null ? settingsMoreCanvasGroup.alpha : 1f;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float currentWidth = Mathf.SmoothStep(startWidth, targetWidth, t);
            settingsMoreRect.sizeDelta = new Vector2(currentWidth, settingsMoreRect.sizeDelta.y);

            if (settingsMoreCanvasGroup != null)
            {
                settingsMoreCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }
            yield return null;
        }

        settingsMoreRect.sizeDelta = new Vector2(targetWidth, settingsMoreRect.sizeDelta.y);
        if (settingsMoreCanvasGroup != null) settingsMoreCanvasGroup.alpha = targetAlpha;
        
        // Disable interaction when hidden
        if (settingsMoreCanvasGroup != null)
        {
            settingsMoreCanvasGroup.blocksRaycasts = (targetAlpha > 0.5f);
            settingsMoreCanvasGroup.interactable = (targetAlpha > 0.5f);
        }
    }

    private void OnDestroy()
    {
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (musicButton != null) musicButton.onClick.RemoveListener(ToggleMusic);
        if (soundButton != null) soundButton.onClick.RemoveListener(ToggleSound);
    }
}
