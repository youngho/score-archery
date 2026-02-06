using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// AppSceneManager handles scene transitions and UI button assignments for stage selection.
/// </summary>
public class AppSceneManager : MonoBehaviour
{
    private Button balloonSceneBtn;

    private void Awake()
    {
        // Find the BalloonScene button by its hierarchy path in 00StartUI
        GameObject balloonBtnGo = GameObject.Find("StartUiPanel/StageButtonsContainer/BalloonScene");
        if (balloonBtnGo != null)
        {
            balloonSceneBtn = balloonBtnGo.GetComponent<Button>();
            if (balloonSceneBtn == null)
            {
                Debug.LogWarning("[AppSceneManager] BalloonScene GameObject found but Button component is missing.");
            }
        }
        else
        {
            Debug.LogWarning("[AppSceneManager] BalloonScene button not found at path: StartUiPanel/StageButtonsContainer/BalloonScene");
        }
    }

    private void Start()
    {
        if (balloonSceneBtn != null)
        {
            balloonSceneBtn.onClick.AddListener(OnBalloonSceneClick);
        }
    }

    private void OnBalloonSceneClick()
    {
        Debug.Log("[AppSceneManager] Loading 04Balloon scene...");
        SceneManager.LoadScene("04Balloon");
    }

    private void OnDestroy()
    {
        if (balloonSceneBtn != null)
        {
            balloonSceneBtn.onClick.RemoveListener(OnBalloonSceneClick);
        }
    }
}
