using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingsUIController : MonoBehaviour
{
    private Button settingsButton;
    private RectTransform settingsMoreRect;
    
    private float animationDuration = 0.5f;
    private bool isExpanded = false;
    private Vector2 originalSize;

    private void Awake()
    {
        // Find references - paths based on Grep/previous context
        GameObject settingsBtnGo = GameObject.Find("StartUICanvas/StartPanel/Settings/Button_settings");
        if (settingsBtnGo == null) 
        {
            // Fallback search if path is different
            settingsBtnGo = GameObject.Find("Button_settings");
        }

        if (settingsBtnGo != null)
        {
            settingsButton = settingsBtnGo.GetComponent<Button>();
        }

        GameObject settingsMoreGo = GameObject.Find("StartUICanvas/StartPanel/Settings/Button_settings_more");
        if (settingsMoreGo == null)
        {
            settingsMoreGo = GameObject.Find("Button_settings_more");
        }

        if (settingsMoreGo != null)
        {
            settingsMoreRect = settingsMoreGo.GetComponent<RectTransform>();
            originalSize = settingsMoreRect.sizeDelta;

            // Set pivot to Right Center (1, 0.5) to expand to the left
            Vector2 previousPivot = settingsMoreRect.pivot;
            settingsMoreRect.pivot = new Vector2(1f, 0.5f);
            
            // Adjust position to prevent jumping when pivot changes
            Vector2 size = settingsMoreRect.rect.size;
            Vector2 deltaPivot = settingsMoreRect.pivot - previousPivot;
            Vector2 deltaPosition = new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);
            settingsMoreRect.anchoredPosition += deltaPosition;
        }
    }

    private void Start()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
    }

    private void OnSettingsClicked()
    {
        if (settingsMoreRect == null) return;

        StopAllCoroutines();
        if (!isExpanded)
        {
            StartCoroutine(ExpandSettings(originalSize.x * 4f));
            isExpanded = true;
        }
        else
        {
            StartCoroutine(ExpandSettings(originalSize.x));
            isExpanded = false;
        }
    }

    private IEnumerator ExpandSettings(float targetWidth)
    {
        float startWidth = settingsMoreRect.sizeDelta.x;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            // Use SmoothStep for a nice slide feeling
            float currentWidth = Mathf.SmoothStep(startWidth, targetWidth, t);
            settingsMoreRect.sizeDelta = new Vector2(currentWidth, settingsMoreRect.sizeDelta.y);
            yield return null;
        }

        settingsMoreRect.sizeDelta = new Vector2(targetWidth, settingsMoreRect.sizeDelta.y);
    }

    private void OnDestroy()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }
    }
}
