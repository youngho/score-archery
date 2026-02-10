using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;


public class Timer : MonoBehaviour
{
    public UnityEvent onTimerEnd;

    [Range(0, 23)]
    public int hours;
    [Range(0, 59)]
    public int minutes;
    [Range(0, 59)]
    public int seconds;
    
    [Tooltip("If checked, runs the timer on play")]
    public bool startAtRuntime = true;

    [Tooltip("Select what to display")]
    public bool hoursDisplay = false;
    public bool minutesDisplay = true;
    public bool secondsDisplay = true;

    [Space]
    
    public Text standardText;
    public TextMeshProUGUI textMeshProText;
    public Slider standardSlider;
    public Image dialSlider;

    bool timerRunning = false;
    bool timerPaused = false;
    public double timeRemaining;
    
    [Header("Warning Settings")]
    public Color warningColor = Color.red;
    public AudioClip countDownSound;
    private Color _originalColor;
    private bool _hasPlayedWarningSound = false;
    

    private void Awake()
    {
        if(!standardText)
        if(GetComponent<Text>())
        {
            standardText = GetComponent<Text>();
        }
        if(!textMeshProText)
        if(GetComponent<TextMeshProUGUI>())
        {
            textMeshProText = GetComponent<TextMeshProUGUI>();
        }
        if(!standardSlider)
        if(GetComponent<Slider>())
        {
            standardSlider = GetComponent<Slider>();
        }
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

        if(standardSlider)
        {
            standardSlider.maxValue = ReturnTotalSeconds();
            standardSlider.value = standardSlider.maxValue;
        }
    }

    void Start()
    {
        if(startAtRuntime)
        {
            StartTimer();
        }
        else
        {
            float total = ReturnTotalSeconds();
            if(standardText)
            {
                standardText.text = DisplayFormattedTime(total);
            }
            if(textMeshProText)
            {
                textMeshProText.text = DisplayFormattedTime(total);
            }
        }
    }

    void Update()
    {
        if(timerRunning)
        {
            CountDown();
            if(standardSlider)
            {
                StandardSliderDown();
            }
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

    private void StandardSliderDown()
    {
        if(standardSlider.value > standardSlider.minValue)
        {
            standardSlider.value -= Time.deltaTime;
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
        if (standardText)
        {
            standardText.text = DisplayFormattedTime(timeRemaining);
        }
        if (textMeshProText)
        {
            textMeshProText.text = DisplayFormattedTime(timeRemaining);
        }
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

        if(standardSlider)
        {
            standardSlider.maxValue = (float)timeRemaining;
            standardSlider.value = standardSlider.maxValue;
        }
        if(dialSlider)
        {
            dialSlider.fillAmount = 1f;
            dialSlider.color = _originalColor;
        }
        _hasPlayedWarningSound = false;
    }

    public float ReturnTotalSeconds()
    {
        float totalTimeSet;
        totalTimeSet = hours * 60 * 60;
        totalTimeSet += minutes * 60;
        totalTimeSet += seconds;
        return totalTimeSet;
    }
   
    public string DisplayFormattedTime(double remainingSeconds)
    {
        string convertedNumber;
        float h, m, s;
        RemainingSecondsToHHMMSSMMM(remainingSeconds, out h, out m, out s);

        string HoursFormat()
        {
            if (hoursDisplay)
            {
                string hoursFormatted = string.Format("{0:00}", h);
                if (minutesDisplay || secondsDisplay)
                    hoursFormatted += ":";
                return hoursFormatted;
            }
            return null;
        }
        string MinutesFormat()
        {
            if (minutesDisplay)
            {
                string minutesFormatted = string.Format("{0:00}", m);
                if (secondsDisplay)
                    minutesFormatted += ":";
                return minutesFormatted;
            }
            return null;
        }
        string SecondsFormat()
        {
            if (secondsDisplay)
            {
                return string.Format("{0:00}", s);
            }
            return null;
        }
        
        convertedNumber = HoursFormat() + MinutesFormat() + SecondsFormat();
        return convertedNumber;
    }

    private static void RemainingSecondsToHHMMSSMMM(double totalSeconds, out float hours, out float minutes, out float seconds)
    {
        hours = Mathf.FloorToInt((float)totalSeconds / 3600);
        minutes = Mathf.FloorToInt(((float)totalSeconds % 3600) / 60);
        seconds = Mathf.FloorToInt((float)totalSeconds % 60);
    }

    private void OnValidate()
    {
        timeRemaining = ReturnTotalSeconds();
    }
}
