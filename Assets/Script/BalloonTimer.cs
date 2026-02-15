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
    
    [Tooltip("If checked, runs the timer on play")]
    public bool startAtRuntime = true;

    [Space]
    
    public Image dialSlider;

    bool timerRunning = false;
    bool timerPaused = false;
    
    [Header("Warning Settings")]
    public Color warningColor = Color.red;
    public AudioClip countDownSound;
    private Color _originalColor;
    private bool _hasPlayedWarningSound = false;
    
    [Header("Timer Settings")]
    [Tooltip("시작 초 (9 → 0 카운트다운)")]
    public int startSeconds = 9;

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
        ResetTimer();

        if(startAtRuntime)
        {
            StartTimer();
        }
    }

    void Update()
    {
        if (!timerRunning) return;
        if (_remainingSeconds <= 0f) return;

        _remainingSeconds -= Time.deltaTime;
        if (_remainingSeconds <= 0f)
        {
            _remainingSeconds = 0f;
            HandleTimerEnd();
            return;
        }

        UpdateVisuals();
    }

    public double GetRemainingSeconds()
    {
        return _remainingSeconds;
    }

    public void StartTimer()
    {
        if (!timerRunning && !timerPaused)
        {
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
        _remainingSeconds = startSeconds;
        UpdateVisuals();
        if(dialSlider)
        {
            dialSlider.fillAmount = 1f;
            dialSlider.color = _originalColor;
        }
        _hasPlayedWarningSound = false;
    }
    
    private void UpdateVisuals()
    {
        UpdateDisplay();
        UpdateDialAndWarning();
    }

    private void UpdateDialAndWarning()
    {
        if (!dialSlider) return;

        // 경고 색 및 사운드
        if (_remainingSeconds <= 5.0f)
        {
            dialSlider.color = warningColor;

            if (!_hasPlayedWarningSound && countDownSound != null)
            {
                AudioSource.PlayClipAtPoint(countDownSound, Camera.main ? Camera.main.transform.position : transform.position);
                _hasPlayedWarningSound = true;
            }
        }

        // 채우기 양 (0~1)
        if (startSeconds > 0)
        {
            float timeRangeClamped = Mathf.InverseLerp(startSeconds, 0, _remainingSeconds);
            dialSlider.fillAmount = Mathf.Lerp(1, 0, timeRangeClamped);
        }
    }

    private void HandleTimerEnd()
    {
        UpdateVisuals();
        timerRunning = false;
        onTimerEnd.Invoke();
        RecordScoreAndLoadMenu();
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
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int arrows = ScoreManager.Instance != null ? ScoreManager.Instance.TotalArrowsShot : 0;
        int hits = ScoreManager.Instance != null ? ScoreManager.Instance.TotalHits : 0;
        
        StageResultService.RequestShowResult(score, arrows, hits);
    }
}
