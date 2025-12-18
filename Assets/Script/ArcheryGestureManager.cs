using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using TouchPhaseNew = UnityEngine.InputSystem.TouchPhase;
using TouchEnhanced = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// 활쏘기 게임을 위한 멀티터치 제스처 매니저
/// 화살 당기기, 발사, 조준 등의 제스처를 처리합니다.
/// </summary>
public class ArcheryGestureManager : MonoBehaviour
{
    #region Singleton
    private static ArcheryGestureManager _instance;

    /// <summary>
    /// 어디서든 접근 가능한 싱글톤 인스턴스.
    /// 씬에 존재하지 않으면 자동으로 GameObject를 생성해서 붙입니다.
    /// (New Scene에서도 바로 사용 가능)
    /// </summary>
    public static ArcheryGestureManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에 이미 존재하는지 먼저 검색
                _instance = FindObjectOfType<ArcheryGestureManager>();

                // 없다면 새 GameObject 생성
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

            if (logDebugEvents)
            {
                Debug.Log("[ArcheryGestureManager] Awake - set as singleton instance", this); // ARCHERY_DEBUG_LOG
            }
        }
        else if (_instance != this)
        {
            // 다른 씬에서 이미 생성된 매니저가 있다면, 중복 객체는 제거
            if (logDebugEvents)
            {
                Debug.Log("[ArcheryGestureManager] Awake - duplicate instance found, destroying this one", this); // ARCHERY_DEBUG_LOG
            }

            Destroy(gameObject);
        }
    }
    #endregion

    #region Events
    [System.Serializable]
    public class GestureEvent : UnityEvent<GestureData> { }

    public GestureEvent OnDrawStart = new GestureEvent();      // 화살 당기기 시작
    public GestureEvent OnDrawing = new GestureEvent();        // 화살 당기는 중
    public GestureEvent OnDrawEnd = new GestureEvent();        // 화살 당기기 종료
    public GestureEvent OnRelease = new GestureEvent();        // 화살 발사
    public GestureEvent OnAimAdjust = new GestureEvent();      // 조준 조정
    public UnityEvent OnCancel = new UnityEvent();             // 제스처 취소
    #endregion

    #region Inspector Settings
    [Header("제스처 설정")]
    [Tooltip("화살을 당기기 시작하는 최소 거리 (픽셀)")]
    public float minDrawDistance = 50f;

    [Tooltip("최대로 당길 수 있는 거리 (픽셀)")]
    public float maxDrawDistance = 300f;

    [Tooltip("조준 조정으로 인식되는 두 번째 손가락 이동 거리")]
    public float aimAdjustThreshold = 30f;

    [Tooltip("제스처가 취소되는 경계 영역 (화면 가장자리)")]
    public float cancelBorderSize = 100f;

    [Header("디버그")]
    public bool showDebugInfo = false;
    [Tooltip("제스처 처리/이벤트 흐름을 Debug.Log로 출력할지 여부")]
    public bool logDebugEvents = false;
    public Color drawLineColor = Color.yellow;
    public Color aimLineColor = Color.cyan;

    [Header("3D 조준 프리뷰 설정")]
    [Tooltip("조준 미리보기 화살이 기준으로 삼을 위치/방향 (보통 활/카메라 앞)")]
    public Transform arrowSpawnPoint;

    [Tooltip("실제 발사할 화살 프리팹 (Rigidbody 필수)")]
    public GameObject arrowPrefab;

    [Tooltip("최대 발사 힘")]
    public float maxForce = 10f;

    [Header("프리뷰 스케일 설정")]
    [Tooltip("제일 약하게 당겼을 때의 화살 크기 배율")]
    public float minScale = 0.5f;

    [Tooltip("최대로 당겼을 때의 화살 크기 배율")]
    public float maxScale = 1.5f;

    [Header("프리뷰 각도 설정")]
    [Tooltip("위/아래로 조정 가능한 최대 각도 (Pitch: 수직 각도)\n" +
             "양수: 위로 향함, 음수: 아래로 향함")]
    public float maxPitchAngle = 89f;

    [Tooltip("좌/우로 조정 가능한 최대 각도 (Yaw: 수평 각도)\n" +
             "양수: 오른쪽으로 향함, 음수: 왼쪽으로 향함")]
    public float maxYawAngle = 70f;

    [Tooltip("조준 프리뷰의 생성/갱신 과정을 로그로 출력할지 여부")]
    public bool logPreviewDebug = true;
    #endregion

    #region Private Variables
    private Dictionary<int, TouchInfo> activeTouches = new Dictionary<int, TouchInfo>();
    private GestureState currentState = GestureState.Idle;
    private Vector2 drawStartPosition;
    private Vector2 currentDrawPosition;
    private int primaryTouchId = -1;
    private int secondaryTouchId = -1;
    private float drawStartTime;

    // 3D 조준 프리뷰 관련
    private GameObject previewInstance;
    private Transform previewTransform;
    private Vector3 baseLocalScale = Vector3.one;
    // 프리팹 메쉬의 "시각적인 중심"이 로컬 피벗(Transform.position)에서 얼마나 떨어져 있는지 (로컬 좌표계 기준)
    private Vector3 previewCenterLocalOffset = Vector3.zero;
    private bool hasPreviewCenterOffset = false;
    // 직전 프레임의 제스처 상태 (디버깅/상태 전이 감지를 위해)
    private GestureState lastGestureState = GestureState.Idle;
    #endregion

    #region Enums
    public enum GestureState
    {
        Idle,           // 대기 중
        Drawing,        // 화살 당기는 중
        Aiming,         // 두 손가락으로 조준 조정 중
        Released        // 화살 발사됨
    }
    #endregion

    #region Data Structures
    [System.Serializable]
    public class GestureData
    {
        public Vector2 startPosition;       // 시작 위치
        public Vector2 currentPosition;     // 현재 위치
        public Vector2 direction;           // 방향 (정규화됨)
        public float distance;              // 당긴 거리
        public float normalizedPower;       // 정규화된 파워 (0-1)
        public float angle;                 // 각도 (도)
        public float pitch;                 // 위/아래 각도 (수직)
        public float yaw;                   // 좌/우 각도 (수평)
        public float duration;              // 제스처 지속 시간
        public Vector2 velocity;            // 속도
        public Vector2 aimOffset;           // 조준 오프셋 (두 손가락 사용 시)

        public GestureData()
        {
            startPosition = Vector2.zero;
            currentPosition = Vector2.zero;
            direction = Vector2.zero;
            distance = 0f;
            normalizedPower = 0f;
            angle = 0f;
            pitch = 0f;
            yaw = 0f;
            duration = 0f;
            velocity = Vector2.zero;
            aimOffset = Vector2.zero;
        }
    }

    private class TouchInfo
    {
        public int fingerId;
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public Vector2 previousPosition;
        public float startTime;
        public bool isMoving;

        public TouchInfo(int id, Vector2 position)
        {
            fingerId = id;
            startPosition = position;
            currentPosition = position;
            previousPosition = position;
            startTime = Time.time;
            isMoving = false;
        }

        public void UpdatePosition(Vector2 newPosition)
        {
            previousPosition = currentPosition;
            currentPosition = newPosition;
            isMoving = Vector2.Distance(previousPosition, currentPosition) > 1f;
        }

        public Vector2 GetVelocity()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime > 0)
            {
                return (currentPosition - previousPosition) / deltaTime;
            }
            return Vector2.zero;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        // 새 Input System EnhancedTouch 활성화
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();

            if (logDebugEvents)
            {
                Debug.Log("[ArcheryGestureManager] OnEnable - EnhancedTouch enabled", this); // ARCHERY_DEBUG_LOG
            }
        }

        // 씬이 바뀔 때 제스처 상태를 초기화하기 위해 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (logDebugEvents)
        {
            Debug.Log("[ArcheryGestureManager] OnEnable - subscribed to sceneLoaded", this); // ARCHERY_DEBUG_LOG
        }

        // 3D 조준 프리뷰 초기화
        EnsurePreviewInstance();
        HidePreview(); // 시작 시에는 항상 숨김

        if (logPreviewDebug)
        {
            Debug.Log("[ArcheryGestureManager] OnEnable - initialized 3D preview system", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void OnDisable()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // 이 매니저가 전역 관리용이라면 비활성화 시 EnhancedTouch도 함께 정리
            if (EnhancedTouchSupport.enabled)
            {
                EnhancedTouchSupport.Disable();

                if (logDebugEvents)
                {
                    Debug.Log("[ArcheryGestureManager] OnDisable - EnhancedTouch disabled", this); // ARCHERY_DEBUG_LOG
                }
            }

            if (logDebugEvents)
            {
                Debug.Log("[ArcheryGestureManager] OnDisable - unsubscribed from sceneLoaded", this); // ARCHERY_DEBUG_LOG
            }

            // 3D 조준 프리뷰 정리
            HidePreview();
        }
    }

    private void Update()
    {
        ProcessInput();
        UpdateGestureState();
        UpdatePreviewByGesture(); // 3D 조준 프리뷰 갱신
    }

    private void OnGUI()
    {
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 로드되면 제스처 상태 및 터치 정보 초기화
        ResetGesture();
        activeTouches.Clear();

        if (logDebugEvents)
        {
            Debug.Log($"[ArcheryGestureManager] OnSceneLoaded - scene='{scene.name}', state reset", this); // ARCHERY_DEBUG_LOG
        }
    }
    #endregion

    #region Input Processing
    /// <summary>
    /// 새 Input System(EnhancedTouch + Mouse)을 이용한 입력 처리
    /// </summary>
    private void ProcessInput()
    {
        bool hasTouchInput = false;

        // 모바일/터치 입력 (EnhancedTouch)
        if (EnhancedTouchSupport.enabled && Touchscreen.current != null)
        {
            var activeTouchesList = TouchEnhanced.activeTouches;
            if (activeTouchesList.Count > 0)
            {
                hasTouchInput = true;

                for (int i = 0; i < activeTouchesList.Count; i++)
                {
                    var touch = activeTouchesList[i];
                    int fingerId = touch.touchId;
                    Vector2 position = touch.screenPosition;
                    TouchPhaseNew phase = touch.phase;

                    ProcessTouch(fingerId, position, phase);
                }
            }
        }

        // 에디터/PC용 마우스 입력 (새 Input System)
        if (!hasTouchInput)
        {
            ProcessMouseInput();
        }

        // 활성 터치 정리
        CleanupInactiveTouches();
    }

    private void ProcessTouch(int fingerId, Vector2 position, TouchPhaseNew phase)
    {
        switch (phase)
        {
            case TouchPhaseNew.Began:
                HandleTouchBegan(fingerId, position);
                break;

            case TouchPhaseNew.Moved:
            case TouchPhaseNew.Stationary:
                HandleTouchMoved(fingerId, position);
                break;

            case TouchPhaseNew.Ended:
            case TouchPhaseNew.Canceled:
                HandleTouchEnded(fingerId, position);
                break;
        }
    }

    private void ProcessMouseInput()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleTouchBegan(0, mousePos);
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            HandleTouchMoved(0, mousePos);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            HandleTouchEnded(0, mousePos);
        }
    }
    #endregion

    #region Touch Handlers
    private void HandleTouchBegan(int fingerId, Vector2 position)
    {
        // 화면 경계 체크
        if (IsNearScreenBorder(position))
            return;

        if (logDebugEvents)
        {
            Debug.Log($"[ArcheryGestureManager] HandleTouchBegan - fingerId={fingerId}, pos={position}, state={currentState}",
                this); // ARCHERY_DEBUG_LOG
        }

        TouchInfo touchInfo = new TouchInfo(fingerId, position);
        activeTouches[fingerId] = touchInfo;

        if (currentState == GestureState.Idle)
        {
            // 첫 번째 터치 - 화살 당기기 시작
            primaryTouchId = fingerId;
            drawStartPosition = position;
            currentDrawPosition = position;
            drawStartTime = Time.time;
            currentState = GestureState.Drawing;

            GestureData data = CreateGestureData();

            if (logDebugEvents)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] Begin Drawing - primaryId={primaryTouchId}, startPos={drawStartPosition}",
                    this); // ARCHERY_DEBUG_LOG
                Debug.Log(
                    $"[ArcheryGestureManager] OnDrawStart Invoke - distance={data.distance:F1}, power={data.normalizedPower:F2}, angle={data.angle:F1}",
                    this); // ARCHERY_DEBUG_LOG
            }

            OnDrawStart?.Invoke(data);
        }
        else if (currentState == GestureState.Drawing && secondaryTouchId == -1)
        {
            // 두 번째 터치 - 조준 조정 모드
            secondaryTouchId = fingerId;
            currentState = GestureState.Aiming;

            if (logDebugEvents)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] Enter Aiming mode - secondaryId={secondaryTouchId}, primaryId={primaryTouchId}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
    }

    private void HandleTouchMoved(int fingerId, Vector2 position)
    {
        if (!activeTouches.ContainsKey(fingerId))
            return;

        activeTouches[fingerId].UpdatePosition(position);

        if (fingerId == primaryTouchId)
        {
            currentDrawPosition = position;

            GestureData data = CreateGestureData();

            // 최소 거리를 당겼는지 확인
            if (data.distance >= minDrawDistance)
            {
                if (logDebugEvents)
                {
                    Debug.Log(
                        $"[ArcheryGestureManager] OnDrawing Invoke - distance={data.distance:F1}, power={data.normalizedPower:F2}, angle={data.angle:F1}",
                        this); // ARCHERY_DEBUG_LOG
                }

                OnDrawing?.Invoke(data);
            }
        }
        else if (fingerId == secondaryTouchId && currentState == GestureState.Aiming)
        {
            GestureData data = CreateGestureData();

            if (logDebugEvents)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] OnAimAdjust Invoke - aimOffset={data.aimOffset}, distance={data.distance:F1}",
                    this); // ARCHERY_DEBUG_LOG
            }

            OnAimAdjust?.Invoke(data);
        }

        // 화면 경계로 이동 시 취소
        if (IsNearScreenBorder(position))
        {
            CancelGesture();
        }
    }

    private void HandleTouchEnded(int fingerId, Vector2 position)
    {
        if (!activeTouches.ContainsKey(fingerId))
            return;

        TouchInfo touchInfo = activeTouches[fingerId];
        touchInfo.UpdatePosition(position);

        if (fingerId == primaryTouchId)
        {
            if (logDebugEvents)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] HandleTouchEnded (primary) - fingerId={fingerId}, pos={position}, state={currentState}",
                    this); // ARCHERY_DEBUG_LOG
            }

            // 주 터치가 끝남 - 화살 발사 또는 취소
            GestureData data = CreateGestureData();

            if (currentState == GestureState.Drawing || currentState == GestureState.Aiming)
            {
                // 발사 조건 확인
                if (data.distance >= minDrawDistance)
                {
                    Vector2 velocity = touchInfo.GetVelocity();
                    data.velocity = velocity;

                    currentState = GestureState.Released;

                    if (logDebugEvents)
                    {
                        Debug.Log(
                            $"[ArcheryGestureManager] OnRelease Invoke - distance={data.distance:F1}, velocity={velocity.magnitude:F1}",
                            this); // ARCHERY_DEBUG_LOG
                    }

                    OnRelease?.Invoke(data);
                    ShootArrow(data);
                }
                else
                {
                    // 충분히 당기지 않음 - 취소
                    CancelGesture();
                }
            }

            OnDrawEnd?.Invoke(data);

            if (logDebugEvents)
            {
                Debug.Log("[ArcheryGestureManager] OnDrawEnd Invoke - gesture finished", this); // ARCHERY_DEBUG_LOG
            }

            ResetGesture();
        }
        else if (fingerId == secondaryTouchId)
        {
            // 두 번째 터치가 끝남 - 다시 드로잉 모드로
            secondaryTouchId = -1;
            if (currentState == GestureState.Aiming)
            {
                currentState = GestureState.Drawing;

                if (logDebugEvents)
                {
                    Debug.Log(
                        "[ArcheryGestureManager] Secondary touch ended - back to Drawing state",
                        this); // ARCHERY_DEBUG_LOG
                }
            }
        }

        activeTouches.Remove(fingerId);
    }
    #endregion

    #region Gesture State Management
    private void UpdateGestureState()
    {
        // 타임아웃 체크 등 추가 상태 관리
        if (currentState == GestureState.Drawing || currentState == GestureState.Aiming)
        {
            // 필요시 타임아웃 체크 로직 추가 가능
            // float duration = Time.time - drawStartTime;
            // if (duration > maxDrawDuration) CancelGesture();
        }
    }

    private void ResetGesture()
    {
        currentState = GestureState.Idle;
        primaryTouchId = -1;
        secondaryTouchId = -1;
        drawStartPosition = Vector2.zero;
        currentDrawPosition = Vector2.zero;

        if (logDebugEvents)
        {
            Debug.Log("[ArcheryGestureManager] ResetGesture - state set to Idle, ids cleared", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void CancelGesture()
    {
        if (logDebugEvents)
        {
            Debug.Log("[ArcheryGestureManager] CancelGesture - invoking OnCancel and clearing touches", this); // ARCHERY_DEBUG_LOG
        }

        OnCancel?.Invoke();
        ResetGesture();
        activeTouches.Clear();
    }
    #endregion

    #region Gesture Data Creation
    private GestureData CreateGestureData()
    {
        GestureData data = new GestureData();
        data.startPosition = drawStartPosition;
        data.currentPosition = currentDrawPosition;

        Vector2 delta = currentDrawPosition - drawStartPosition;
        data.distance = delta.magnitude;
        data.direction = delta.normalized;
        data.normalizedPower = Mathf.Clamp01(data.distance / maxDrawDistance);
        data.angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        data.duration = Time.time - drawStartTime;

        // 피치/요 각도 계산
        // Pitch(수직): 아래로 드래그(y<0) -> 위로 발사(pitch>0)
        data.pitch = Mathf.Clamp(data.direction.y * maxPitchAngle, 0f, maxPitchAngle);

        // 오른쪽으로 드래그하면 왼쪽으로 발사
        // direction.x가 양수(오른쪽으로 드래그) → yawDeg가 음수(왼쪽으로 향함)
        data.yaw = Mathf.Clamp(-data.direction.x * maxYawAngle, -maxYawAngle, maxYawAngle);

        // 조준 오프셋 계산 (두 손가락 사용 시)
        if (secondaryTouchId != -1 && activeTouches.ContainsKey(secondaryTouchId))
        {
            TouchInfo secondaryTouch = activeTouches[secondaryTouchId];
            data.aimOffset = secondaryTouch.currentPosition - secondaryTouch.startPosition;
        }

        if (primaryTouchId != -1 && activeTouches.ContainsKey(primaryTouchId))
        {
            data.velocity = activeTouches[primaryTouchId].GetVelocity();
        }

        return data;
    }
    #endregion

    #region Utility Methods
    private bool IsNearScreenBorder(Vector2 position)
    {
        return position.x < cancelBorderSize ||
               position.x > Screen.width - cancelBorderSize ||
               position.y < cancelBorderSize ||
               position.y > Screen.height - cancelBorderSize;
    }

    private void CleanupInactiveTouches()
    {
        List<int> toRemove = new List<int>();

        foreach (var kvp in activeTouches)
        {
            bool isActive = false;

            // 터치가 실제로 활성 상태인지 확인 (EnhancedTouch)
            if (EnhancedTouchSupport.enabled)
            {
                var activeTouchesList = TouchEnhanced.activeTouches;
                for (int i = 0; i < activeTouchesList.Count; i++)
                {
                    var touch = activeTouchesList[i];
                    if (touch.touchId == kvp.Key)
                    {
                        isActive = true;
                        break;
                    }
                }
            }

            // 마우스 입력 체크
            if (Mouse.current != null && kvp.Key == 0 && Mouse.current.leftButton.isPressed)
            {
                isActive = true;
            }

            if (!isActive && kvp.Key != primaryTouchId && kvp.Key != secondaryTouchId)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            activeTouches.Remove(id);
        }
    }

    public GestureState GetCurrentState()
    {
        return currentState;
    }

    public GestureData GetCurrentGestureData()
    {
        return CreateGestureData();
    }

    private void ShootArrow(GestureData data)
    {
        // 실제 화살 발사 시점
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            Debug.LogWarning("[ArcheryGestureManager] arrowPrefab 또는 arrowSpawnPoint가 설정되어 있지 않습니다.", this); // ARCHERY_DEBUG_LOG
            return;
        }

        float force = maxForce;

        // 발사 기본 방향: spawnPoint가 있으면 그 forward, 없으면 카메라 forward
        Camera cam = Camera.main;
        Vector3 baseDir = arrowSpawnPoint != null
            ? arrowSpawnPoint.forward
            : (cam != null ? cam.transform.forward : Vector3.forward);

        // 월드 기준 회전 구성 (요는 세계 Y축, 피치는 카메라의 오른쪽 축 기준)
        Quaternion rot = Quaternion.identity;
        if (cam != null)
        {
            rot = Quaternion.AngleAxis(data.yaw, Vector3.up) *
                  Quaternion.AngleAxis(data.pitch, cam.transform.right);
        }
        else
        {
            rot = Quaternion.Euler(data.pitch, data.yaw, 0f);
        }

        Vector3 dir = rot * baseDir;

        if (logDebugEvents)
        {
            Debug.Log(
                $"[ArcheryGestureManager] Calculated shot direction - dragDir={data.direction}, pitch={data.pitch:F1}, yaw={data.yaw:F1}, baseDir={baseDir}",
                this); // ARCHERY_DEBUG_LOG
        }

        // 화살 생성 및 초기 회전 설정
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.LookRotation(dir));

        if (logDebugEvents)
        {
            Debug.Log(
                $"[ArcheryGestureManager] Spawned arrow instance '{arrow.name}' at {arrowSpawnPoint.position} with dir={dir}",
                this); // ARCHERY_DEBUG_LOG
        }

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(dir * force, ForceMode.Impulse);

            if (logDebugEvents)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] Applied force to arrow - force={force:F1}, velocity={rb.linearVelocity}, mass={rb.mass}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
        else if (logDebugEvents)
        {
            Debug.Log("[ArcheryGestureManager] Spawned arrow has no Rigidbody component", this); // ARCHERY_DEBUG_LOG
        }
    }
    #endregion

    #region Debug Visualization
    private void DrawDebugInfo()
    {
        // 화면에 디버그 정보 표시
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;

        GUI.Label(new Rect(10, 10, 300, 30), $"State: {currentState}", style);
        GUI.Label(new Rect(10, 40, 300, 30), $"Active Touches: {activeTouches.Count}", style);

        if (currentState != GestureState.Idle)
        {
            GestureData data = CreateGestureData();
            GUI.Label(new Rect(10, 70, 300, 30), $"Distance: {data.distance:F1}px", style);
            GUI.Label(new Rect(10, 100, 300, 30), $"Power: {data.normalizedPower:F2}", style);
            GUI.Label(new Rect(10, 130, 300, 30), $"Angle: {data.angle:F1}°", style);

            // 당기기 선 그리기
            DrawLine(drawStartPosition, currentDrawPosition, drawLineColor);

            // 조준 오프셋 표시
            if (secondaryTouchId != -1)
            {
                GUI.Label(new Rect(10, 160, 300, 30), $"Aim Offset: {data.aimOffset}", style);
            }
        }
    }

    private void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        // GUI 좌표계로 변환 (Y축 반전)
        Vector2 guiStart = new Vector2(start.x, Screen.height - start.y);
        Vector2 guiEnd = new Vector2(end.x, Screen.height - end.y);

        // 간단한 선 그리기
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        Vector2 diff = guiEnd - guiStart;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        float distance = diff.magnitude;

        GUIUtility.RotateAroundPivot(angle, guiStart);
        GUI.DrawTexture(new Rect(guiStart.x, guiStart.y, distance, 3), texture);
        GUIUtility.RotateAroundPivot(-angle, guiStart);
    }
    #endregion

    #region 3D Aim Preview
    /// <summary>
    /// ArcheryGestureManager의 현재 상태를 매 프레임 조회해서
    /// 조준 미리보기 화살의 표시/회전/스케일을 갱신한다.
    /// (이벤트 방식 대신 "폴링 방식"으로 구현해서 버그를 줄임)
    /// </summary>
    private void UpdatePreviewByGesture()
    {
        if (arrowSpawnPoint == null)
        {
            return;
        }

        var state = GetCurrentState();
        if (state != lastGestureState && logPreviewDebug)
        {
            Debug.Log(
                $"[ArcheryGestureManager] GestureState changed {lastGestureState} -> {state}",
                this); // ARCHERY_DEBUG_LOG
        }
        lastGestureState = state;

        // Idle/Released 상태에서는 프리뷰를 숨긴다.
        if (state != GestureState.Drawing &&
            state != GestureState.Aiming)
        {
            HidePreview();
            return;
        }

        // 현재 제스처 데이터 조회
        var data = GetCurrentGestureData();

        // 최소 드로우 거리 미만이면 "그냥 클릭"으로 간주하고 프리뷰를 숨긴다.
        if (data.distance < minDrawDistance)
        {
            HidePreview();
            return;
        }

        // 여기까지 왔으면 실제로 조준 중이므로 프리뷰를 보여줌
        EnsurePreviewInstance();
        if (previewInstance == null) return;

        if (!previewInstance.activeSelf)
        {
            previewInstance.SetActive(true);

            if (logPreviewDebug)
            {
                Debug.Log("[ArcheryGestureManager] Show preview (start drawing)", this); // ARCHERY_DEBUG_LOG
            }
        }

        Camera cam = Camera.main;

        // 기본 방향
        Vector3 baseDir = arrowSpawnPoint.forward;

        // 발사 방향 계산: 드래그 방향의 반대로 발사 (활쏘기처럼 당기는 방향의 반대)
        // 아래로 드래그 → 위로 발사, 오른쪽으로 드래그 → 왼쪽으로 발사
        Vector2 dragVec = (data.currentPosition - data.startPosition);
        Vector2 dragDir = dragVec.sqrMagnitude > 0.0001f ? dragVec.normalized : Vector2.zero;

        // 위로 드래그하면 조준 취소
        // dragDir.y가 양수이면 위로 드래그하는 것
        if (dragDir.y > -0.1f) // 약간의 임계값으로 위로 드래그 감지
        {
            if (logPreviewDebug)
            {
                Debug.Log("[ArcheryGestureManager] Upward drag detected - canceling gesture", this); // ARCHERY_DEBUG_LOG
            }
            CancelGesture();
            HidePreview();
            return;
        }
        float verticalAngleDeg = 0f;
        float horizontalAngleDeg = 0f;

        float rawAngle = dragDir.y * maxPitchAngle;
        verticalAngleDeg = Mathf.Clamp(rawAngle, 0f, maxPitchAngle);
       
        horizontalAngleDeg = Mathf.Clamp(-dragDir.x * maxYawAngle, -maxYawAngle, maxYawAngle);
        

        Quaternion rot;
        if (cam != null)
        {
            // 회전 적용: Yaw (수평) → Pitch (수직)
            rot = Quaternion.AngleAxis(horizontalAngleDeg, Vector3.up) *
                  Quaternion.AngleAxis(verticalAngleDeg, cam.transform.right);
        }
        else
        {
            // 카메라가 없으면 Euler 각도로 설정 (X: Pitch, Y: Yaw, Z: Roll)
            rot = Quaternion.Euler(verticalAngleDeg, horizontalAngleDeg, 0f);
        }

        Vector3 dir = rot * baseDir;

        // 회전 먼저 적용
        previewTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        // 그 다음, 메쉬의 "시각적인 중심"이 spawnPoint 에 위치하도록 보정
        if (hasPreviewCenterOffset)
        {
            previewTransform.position =
                arrowSpawnPoint.position - previewTransform.rotation * previewCenterLocalOffset;
        }
        else
        {
            previewTransform.position = arrowSpawnPoint.position;
        }

        // 드로우 거리(파워)에 따라 스케일 보간
        float t = Mathf.Clamp01(data.normalizedPower);
        float scale = Mathf.Lerp(minScale, maxScale, t);
        previewTransform.localScale = baseLocalScale * scale;

        if (logPreviewDebug)
        {
            Debug.Log(
                $"[ArcheryGestureManager] UpdatePreviewByGesture - state={state}, distance={data.distance:F1}, power={data.normalizedPower:F2}, dragDir={dragDir}, verticalAngle(pitch)={verticalAngleDeg:F1}°, horizontalAngle(yaw)={horizontalAngleDeg:F1}°, pos={previewTransform.position}, scale={previewTransform.localScale}",
                this); // ARCHERY_DEBUG_LOG
        }
    }

    private void EnsurePreviewInstance()
    {
        if (previewInstance != null)
        {
            if (logPreviewDebug)
            {
                Debug.Log("[ArcheryGestureManager] EnsurePreviewInstance - already exists", this); // ARCHERY_DEBUG_LOG
            }
            return;
        }
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[ArcheryGestureManager] arrowPrefab 이 설정되어 있지 않습니다.");
            return;
        }

        previewInstance = Instantiate(arrowPrefab);
        previewTransform = previewInstance.transform;
        baseLocalScale = previewTransform.localScale;

        if (logPreviewDebug)
        {
            Debug.Log(
                $"[ArcheryGestureManager] EnsurePreviewInstance - instantiated preview, baseScale={baseLocalScale}",
                this); // ARCHERY_DEBUG_LOG
        }

        // 프리팹의 메쉬 중심을 계산하여, 메쉬 중심이 arrowSpawnPoint를 기준으로 움직이도록 보정
        var renderer = previewInstance.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            // bounds.center 는 월드 좌표이므로, 이를 프리뷰 Transform 로컬 좌표로 변환
            Vector3 worldCenter = renderer.bounds.center;
            Vector3 localCenter = previewTransform.InverseTransformPoint(worldCenter);

            // 로컬 피벗(0,0,0)에서 메쉬 중심까지의 오프셋
            previewCenterLocalOffset = localCenter;
            hasPreviewCenterOffset = (previewCenterLocalOffset != Vector3.zero);

            if (logPreviewDebug)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] Calculated preview center offset - localCenter={localCenter}, hasOffset={hasPreviewCenterOffset}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
        else
        {
            previewCenterLocalOffset = Vector3.zero;
            hasPreviewCenterOffset = false;

            if (logPreviewDebug)
            {
                Debug.Log("[ArcheryGestureManager] EnsurePreviewInstance - no Renderer found on preview", this); // ARCHERY_DEBUG_LOG
            }
        }

        previewInstance.SetActive(false);
    }

    private void HidePreview()
    {
        if (previewInstance != null)
        {
            previewInstance.SetActive(false);

            if (logPreviewDebug)
            {
                Debug.Log("[ArcheryGestureManager] HidePreview - preview disabled", this); // ARCHERY_DEBUG_LOG
            }
        }
    }
    #endregion
}