using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    public UnityEvent onTimerEnd;

    [Range(0, 59)]
    public int seconds;
    
    [Tooltip("If checked, runs the timer on play")]
    public bool startAtRuntime = true;

    [Tooltip("Select what to display")]
    public bool secondsDisplay = true;

    [Space]
    
    public Image dialSlider;

    bool timerRunning = false;
    bool timerPaused = false;
    public double timeRemaining;
    
    [Header("Warning Settings")]
    public Color warningColor = Color.red;
    public AudioClip countDownSound;
    private Color _originalColor;
    private bool _hasPlayedWarningSound = false;
    
    [Header("Timer Settings")]
    [Tooltip("시작 초 (9 → 0 카운트다운)")]
    public int startSeconds = 9;

    [Tooltip("0이 되면 로드할 씬 이름 (상위 메뉴)")]
    public string menuSceneName = "00StartUI";

    [SerializeField] private TextMeshProUGUI _timerText;

    private float _remainingSeconds;

    private void Awake()
    {
        if(!dialSlider)
        if(GetComponent<Image>())
        {
            dialSlider = GetComponent<Image>();
        }
        
        if(dialSlider)
        {
            _originalColor = dialSlider.color;
            dialSlider.fillAmount = 1f;
        }
    }

    void Start()
    {
        _remainingSeconds = startSeconds;
        UpdateDisplay();

        if(startAtRuntime)
        {
            StartTimer();
        }
        else
        {
            float total = ReturnTotalSeconds();
        }
    }

    void Update()
    {
        if (_remainingSeconds <= 0f) return;

        _remainingSeconds -= Time.deltaTime;
        if (_remainingSeconds <= 0f)
        {
            _remainingSeconds = 0f;
            UpdateDisplay();
            RecordScoreAndLoadMenu();
            return;
        }

        UpdateDisplay();

        if(timerRunning)
        {
            CountDown();
            if(dialSlider)
            {
                DialSliderDown();
            }
        }
    }

    private void CountDown()
    {
        /*If you choose to edit this back to 0 for 100% accuracy,
        1 frame at the end of the timer will display maximum numbers as it takes time to switch to the else statement
        which sets the time remaining to 0. This is accurate up to 20 milliseconds or 0.02 of a second.*/  
        if (timeRemaining > 0.02)
        {
            timeRemaining -= Time.deltaTime;
            
            if (timeRemaining <= 5.0)
            {
                if (dialSlider)
                {
                    dialSlider.color = warningColor;
                }
                
                if (!_hasPlayedWarningSound && countDownSound != null)
                {
                    AudioSource.PlayClipAtPoint(countDownSound, Camera.main ? Camera.main.transform.position : transform.position);
                    _hasPlayedWarningSound = true;
                }
            }
            
            DisplayInTextObject();
        }
        else
        {
            //Timer has ended from counting downwards
            timeRemaining = 0;
            timerRunning = false;
            onTimerEnd.Invoke();
            DisplayInTextObject();
        }
    }

    private void DialSliderDown()
    {
        float total = ReturnTotalSeconds();
        if (total > 0)
        {
            float timeRangeClamped = Mathf.InverseLerp(total, 0, (float)timeRemaining);
            dialSlider.fillAmount = Mathf.Lerp(1, 0, timeRangeClamped);
        }
    }

    private void DisplayInTextObject()
    {
    }

    public double GetRemainingSeconds()
    {
        return timeRemaining;
    }

    public void StartTimer()
    {
        if (!timerRunning && !timerPaused)
        {
            ResetTimer();
            timerRunning = true;
        }
    }

    public void StopTimer()
    {
        timerRunning = false;
        ResetTimer();
    }

    private void ResetTimer()
    {
        timerPaused = false;
        timeRemaining = ReturnTotalSeconds();
        DisplayInTextObject();
        if(dialSlider)
        {
            dialSlider.fillAmount = 1f;
            dialSlider.color = _originalColor;
        }
        _hasPlayedWarningSound = false;
    }

    public float ReturnTotalSeconds()
    {
        return seconds;
    }
   
    public string DisplayFormattedTime(double remainingSeconds)
    {
        string convertedNumber;
        float s = Mathf.FloorToInt((float)remainingSeconds % 60);

        if (secondsDisplay)
        {
            convertedNumber = string.Format("{0:00}", s);
        }
        else
        {
            convertedNumber = string.Empty;
        }

        return convertedNumber;
    }

    private void OnValidate()
    {
        timeRemaining = ReturnTotalSeconds();
    }

    /// <summary>
    /// API에 점수 기록 후 메뉴 씬 로드
    /// </summary>
    private void RecordScoreAndLoadMenu()
    {
        var recorder = GetComponent<StageScoreApiService>() ?? gameObject.AddComponent<StageScoreApiService>();
        recorder.RecordCurrentStageAndThen(LoadMenuScene);
    }

    private void UpdateDisplay()
    {
        if (_timerText != null)
            _timerText.text = Mathf.CeilToInt(_remainingSeconds).ToString();
    }

    private void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName)) return;
        SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
    }
}
