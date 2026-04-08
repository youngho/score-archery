using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private RectTransform settingsIconTransform;
    [SerializeField] private RectTransform settingsMoreRect;
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;

    [Header("Toggle Sprites")]
    [SerializeField] private Sprite musicOn;
    [SerializeField] private Sprite musicOff;
    [SerializeField] private Sprite soundOn;
    [SerializeField] private Sprite soundOff;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float expandedWidth = 290f;

    private bool isExpanded = false;
    private float currentWidth = 0f;
    private float targetWidth = 0f;
    private float velocity = 0f; // for SmoothDamp


    private void Awake()
    {
        if (settingsMoreRect != null)
        {
            settingsMoreRect.sizeDelta = new Vector2(0, settingsMoreRect.sizeDelta.y);
            currentWidth = 0f;
            targetWidth = 0f;
        }
        InitializeButtonStates();
    }

    private void InitializeButtonStates()
    {
        UpdateButtonVisuals(musicButton, AudioSettings.IsMusicEnabled, musicOn, musicOff);
        UpdateButtonVisuals(soundButton, AudioSettings.IsSoundEnabled, soundOn, soundOff);
    }

    private void Start()
    {
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
    }

    private void Update()
    {
        if (settingsMoreRect == null) return;
        if (Mathf.Abs(currentWidth - targetWidth) < 0.5f && currentWidth != targetWidth)
        {
            currentWidth = targetWidth;
        }

        if (Mathf.Abs(currentWidth - targetWidth) > 0.1f)
        {
            currentWidth = Mathf.SmoothDamp(currentWidth, targetWidth, ref velocity, animationDuration);
            settingsMoreRect.sizeDelta = new Vector2(currentWidth, settingsMoreRect.sizeDelta.y);

            // Rotate gear icon
            if (settingsIconTransform != null)
            {
                float rotT = Mathf.InverseLerp(0, expandedWidth, currentWidth);
                settingsIconTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, -90f, rotT));
            }
        }
    }

    private void OnSettingsClicked()
    {
        isExpanded = !isExpanded;
        targetWidth = isExpanded ? expandedWidth : 0f;
        velocity = 0f;
    }

    private void ToggleMusic()
    {
        bool current = !AudioSettings.IsMusicEnabled;
        AudioSettings.IsMusicEnabled = current;
        AudioSettings.Save();
        UpdateButtonVisuals(musicButton, current, musicOn, musicOff);
    }

    private void ToggleSound()
    {
        bool current = !AudioSettings.IsSoundEnabled;
        AudioSettings.IsSoundEnabled = current;
        AudioSettings.Save();
        UpdateButtonVisuals(soundButton, current, soundOn, soundOff);
    }

    private void UpdateButtonVisuals(Button button, bool enabled, Sprite onSprite, Sprite offSprite)
    {
        if (button == null) return;
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = enabled ? onSprite : offSprite;
            Color c = img.color;
            c.a = 1f; // Ensure it's not dimmed
            img.color = c;
        }
    }

    private void OnDestroy()
    {
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (musicButton != null) musicButton.onClick.RemoveListener(ToggleMusic);
        if (soundButton != null) soundButton.onClick.RemoveListener(ToggleSound);
    }
}
