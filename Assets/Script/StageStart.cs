using UnityEngine;
using UnityEngine.UI;

public class StageStart : MonoBehaviour
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
        if (popupObject == null) popupObject = gameObject;
        if (clickPanel == null) clickPanel = popupObject;

        Button panelButton = clickPanel.GetComponent<Button>();
        if (panelButton == null)
        {
            panelButton = clickPanel.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
        }

        panelButton.onClick.AddListener(OnDescriptionClicked);
        StageDescriptionPopupAudio.PlayReady();

        if (timer == null)
            Debug.LogError("[StageStart] Timer가 인스펙터에 할당되지 않았습니다.");
    }

    private void OnDescriptionClicked()
    {
        StageDescriptionPopupAudio.PlayGo();

        if (popupObject != null)
            popupObject.SetActive(false);

        if (timer != null)
            timer.StartTimer();
    }
}
