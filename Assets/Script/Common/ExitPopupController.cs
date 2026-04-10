using UnityEngine;
using UnityEngine.UI;

public class ExitPopupController : MonoBehaviour
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private void Start()
    {
        if (yesButton == null)
            yesButton = transform.Find("PopupPanel/Button_Yes")?.GetComponent<Button>();
        if (noButton == null)
            noButton = transform.Find("PopupPanel/Button_No")?.GetComponent<Button>();

        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);
    }

    private void OnYesClicked()
    {
        var timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            timer.FinishStageLikeTimerEnd();
        }
        else
        {
            Debug.LogWarning("Timer not found in the scene.");
        }
        Destroy(gameObject);
    }

    private void OnNoClicked()
    {
        Destroy(gameObject);
    }
}
