using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using TouchPhaseNew = UnityEngine.InputSystem.TouchPhase;
using TouchEnhanced = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ArcheryGestureManager : MonoBehaviour
{
    #region Singleton
    private static ArcheryGestureManager _instance;

    public static ArcheryGestureManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ArcheryGestureManager>();

                if (_instance == null)
                {
                    var go = new GameObject("ArcheryGestureManager");
                    _instance = go.AddComponent<ArcheryGestureManager>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Events and Enums
    [System.Serializable]
    public class GestureEvent : UnityEvent<GestureData> { }

    public GestureEvent OnDrawStart = new GestureEvent();
    public GestureEvent OnDrawing = new GestureEvent();
    public GestureEvent OnDrawEnd = new GestureEvent();
    public GestureEvent OnRelease = new GestureEvent();
    public GestureEvent OnAimAdjust = new GestureEvent();
    public UnityEvent OnCancel = new UnityEvent();

    public enum GestureState { Idle, Drawing, Aiming, Released }
    #endregion

    #region Inspector Settings
    [Header("Settings")]
    public Transform arrowSpawnPoint;
    public GameObject arrowPrefab;
    public float maxForce = 30f;
    public float maxPitchAngle = 80f;
    public float maxYawAngle = 70f;
    public float minDrawDistance = 50f;
    public float maxDrawDistance = 300f;
    public bool showDebugLog = false;
    public Color drawLineColor = Color.yellow;
    public Color aimLineColor = Color.cyan;
    #endregion

    #region Private Variables
    private Dictionary<int, TouchInfo> activeTouches = new Dictionary<int, TouchInfo>();
    private GestureState currentState = GestureState.Idle;
    private int primaryTouchId = -1;
    private int secondaryTouchId = -1;
    private Vector2 drawStartPosition;
    private Vector2 currentDrawPosition;
    private float drawStartTime;
    private GestureState lastGestureState = GestureState.Idle;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Disable();
            }
        }
    }

    private void Update()
    {
        ProcessInput();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetGesture();
        activeTouches.Clear();
    }
    #endregion

    #region Input Processing
    private void ProcessInput()
    {
        bool hasTouchInput = HandleTouchInput();

        if (!hasTouchInput)
        {
            HandleMouseInput();
        }

        CleanupInactiveTouches();
    }

    private bool HandleTouchInput()
    {
        if (!EnhancedTouchSupport.enabled || Touchscreen.current == null) return false;

        var activeTouchesList = TouchEnhanced.activeTouches;
        foreach (var touch in activeTouchesList)
        {
            ProcessTouch(touch.touchId, touch.screenPosition, touch.phase);
        }
        return activeTouchesList.Count > 0;
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ProcessTouch(0, mousePos, TouchPhaseNew.Began);
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            ProcessTouch(0, mousePos, TouchPhaseNew.Moved);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ProcessTouch(0, mousePos, TouchPhaseNew.Ended);
        }
    }

    private void ProcessTouch(int fingerId, Vector2 position, TouchPhaseNew phase)
    {
        switch (phase)
        {
            case TouchPhaseNew.Began:
                StartTouchGesture(fingerId, position);
                break;
            case TouchPhaseNew.Moved:
            case TouchPhaseNew.Stationary:
                ContinueTouchGesture(fingerId, position);
                break;
            case TouchPhaseNew.Ended:
            case TouchPhaseNew.Canceled:
                EndTouchGesture(fingerId, position);
                break;
        }
    }

    private void CleanupInactiveTouches()
    {
        activeTouches = new Dictionary<int, TouchInfo>(activeTouches);
    }
    #endregion

    #region Gesture Handlers
    private void StartTouchGesture(int fingerId, Vector2 position)
    {
        if (currentState == GestureState.Idle)
        {
            primaryTouchId = fingerId;
            currentState = GestureState.Drawing;
            drawStartPosition = position;
            drawStartTime = Time.time;
        }
    }

    private void ContinueTouchGesture(int fingerId, Vector2 position)
    {
        if (fingerId != primaryTouchId) return;
        GestureData data = CreateGestureData();
        OnDrawing?.Invoke(data);
    }

    private void EndTouchGesture(int fingerId, Vector2 position)
    {
        if (fingerId == primaryTouchId && activeTouches.ContainsKey(fingerId))
        {
            GestureData data = CreateGestureData();
            OnRelease?.Invoke(data);
            ResetGesture();
        }
    }

    private void ResetGesture()
    {
        currentState = GestureState.Idle;
        primaryTouchId = -1;
        secondaryTouchId = -1;
    }
    #endregion

    #region Utility Methods
    private GestureData CreateGestureData()
    {
        return new GestureData();
    }
    private class TouchInfo
    {
        public int FingerId;
        public TouchInfo(int id) { FingerId = id; }
    }
    public class GestureData
    {
    }
    #endregion
}