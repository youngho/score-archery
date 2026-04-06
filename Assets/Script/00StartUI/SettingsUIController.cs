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

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float expandedWidth = 290f;

    private bool isExpanded = false;
    private float currentWidth = 0f;
    private float targetWidth = 0f;
    private float velocity = 0f; // for SmoothDamp

    private const string MusicKey = "MusicEnabled";
    private const string SoundKey = "SoundEnabled";

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
        bool musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        bool soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;

        UpdateButtonVisuals(musicButton, musicEnabled);
        UpdateButtonVisuals(soundButton, soundEnabled);
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
        bool current = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        current = !current;
        PlayerPrefs.SetInt(MusicKey, current ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtonVisuals(musicButton, current);
    }

    private void ToggleSound()
    {
        bool current = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        current = !current;
        PlayerPrefs.SetInt(SoundKey, current ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtonVisuals(soundButton, current);
    }

    private void UpdateButtonVisuals(Button button, bool enabled)
    {
        if (button == null) return;
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = enabled ? 1f : 0.4f;
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
