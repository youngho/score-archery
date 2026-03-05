using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// AppSceneManager handles scene transitions and UI button assignments for stage selection.
/// </summary>
public class AppSceneManager : MonoBehaviour
{
    private Button balloonSceneBtn;
    private Button rubberDuckBtn;

    private void Awake()
    {
        // Find the BalloonScene button by its hierarchy path in 00StartUI
        GameObject balloonBtnGo = GameObject.Find("StartUiPanel/StageButtonsContainer/BalloonScene");
        GameObject rubberDuckBtnGo = GameObject.Find("StartUiPanel/StageButtonsContainer/RubberDuckScene");
        
        if (balloonBtnGo != null)
        {
            balloonSceneBtn = balloonBtnGo.GetComponent<Button>();
         
        }
        if (rubberDuckBtnGo != null)
        {
            rubberDuckBtn = rubberDuckBtnGo.GetComponent<Button>();
        }
    }

    private void Start()
    {
        if (balloonSceneBtn != null)
        {
            balloonSceneBtn.onClick.AddListener(OnBalloonSceneClick);
        }
        if (rubberDuckBtn != null)
        {
            rubberDuckBtn.onClick.AddListener(OnRubberDuckSceneClick);
     
        }
    }

    private void OnBalloonSceneClick()
    {
        SceneManager.LoadScene("04Balloon");
    }

    private void OnRubberDuckSceneClick()
    {
        SceneManager.LoadScene("05RubberDuck");
    }


    private void OnDestroy()
    {
        if (balloonSceneBtn != null)
        {
            balloonSceneBtn.onClick.RemoveListener(OnBalloonSceneClick);
        }
    }
}
