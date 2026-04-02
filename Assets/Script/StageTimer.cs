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
    public UnityEvent onTimerStart;
    
    [Tooltip("If checked, runs the timer on play")]
    public bool startAtRuntime = true;

    [Space]
    
    public Image dialSlider;

    bool timerRunning = false;
    bool timerPaused = false;

    public bool IsRunning => timerRunning;
    
    [Header("Warning Settings")]
    public Color warningColor = Color.red;
    public AudioClip countDownSound;
    private Color _originalColor;
    private bool _hasPlayedWarningSound = false;
    
    [Header("Timer Settings")]
    [Tooltip("시작 초 (9 → 0 카운트다운)")]
    public int startSeconds = 9;

    [Tooltip("startSeconds가 0(무제한)일 때 숫자 대신 표시할 문자 (예: ∞, 무제한)")]
    [SerializeField] private string _unlimitedTimeDisplay = "\u221E";

    [SerializeField] private TextMeshProUGUI _timerText;

    private float _remainingSeconds;
    private bool _stageCompletionHandled;

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
        bool startingNow = !timerRunning && !timerPaused;
        if (startingNow)
        {
            timerRunning = true;
        }
        onTimerStart.Invoke();
        if (startingNow)
            StageRandomBGMController.NotifyStageGameplayStarted();
    }

    public void StopTimer()
    {
        timerRunning = false;
        ResetTimer();
    }

    private void ResetTimer()
    {
        timerPaused = false;
        _stageCompletionHandled = false;
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

        // 경고 색 및 사운드 (실제 카운트다운이 있을 때만 — startSeconds=0 무제한 씬에서는 남은 시간이 0이라 경고가 켜지지 않게)
        if (startSeconds > 0 && _remainingSeconds > 0f && _remainingSeconds <= 5.0f)
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
        else
        {
            // 무제한(startSeconds=0): 플레이 중엔 다이얼 유지, 스테이지 완료 시에만 비움(남은 시간은 항상 0)
            dialSlider.fillAmount = _stageCompletionHandled ? 0f : 1f;
        }
    }

    private void HandleTimerEnd()
    {
        CompleteStageFromTimer();
    }

    /// <summary>카운트다운만 멈춤 (AppleOneShot 등에서 사과 명중 후 이중 종료 방지).</summary>
    public void PauseCountdown()
    {
        if (_stageCompletionHandled) return;
        timerRunning = false;
        timerPaused = true;
    }

    /// <summary>시간이 0이 된 것과 동일하게 스테이지 종료(API 기록, 결과 화면).</summary>
    public void FinishStageLikeTimerEnd()
    {
        CompleteStageFromTimer();
    }

    private void CompleteStageFromTimer()
    {
        if (_stageCompletionHandled) return;
        _stageCompletionHandled = true;

        _remainingSeconds = 0f;
        timerPaused = false;
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
        if (_timerText == null) return;

        if (startSeconds <= 0)
            _timerText.text = string.IsNullOrEmpty(_unlimitedTimeDisplay) ? "\u221E" : _unlimitedTimeDisplay;
        else
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
