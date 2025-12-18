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

            if (showDebugLog)
            {
                Debug.Log("[ArcheryGestureManager] Awake - set as singleton instance", this); // ARCHERY_DEBUG_LOG
            }
        }
        else if (_instance != this)
        {
            // 다른 씬에서 이미 생성된 매니저가 있다면, 중복 객체는 제거
            if (showDebugLog)
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

    [Header("3D 조준 프리뷰 설정")]
    [Tooltip("조준 미리보기 화살이 기준으로 삼을 위치/방향 (보통 활/카메라 앞)")]
    public Transform arrowSpawnPoint;

    [Tooltip("실제 발사할 화살 프리팹 (Rigidbody 필수)")]
    public GameObject arrowPrefab;

    [Header("화살 Aim 설정")]
    [Tooltip("최대 발사 힘")]
    public float maxForce = 30f;

    [Tooltip("위/아래로 조정 가능한 최대 각도 (Pitch: 수직 각도)\n" +
             "양수: 위로 향함, 음수: 아래로 향함")]
    public float maxPitchAngle = 80f;

    [Tooltip("좌/우로 조정 가능한 최대 각도 (Yaw: 수평 각도)\n" +
             "양수: 오른쪽으로 향함, 음수: 왼쪽으로 향함")]
    public float maxYawAngle = 70f;

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
    [Tooltip("제스처 처리 및 조준 프리뷰 관련 로그를 출력할지 여부")]
    public bool showDebugLog = false;
    public Color drawLineColor = Color.yellow;
    public Color aimLineColor = Color.cyan;

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
    // 프리팹 메쉬의 "시각적인 중심"이 로컬 피벗(Transform.position)에서 얼마나 떨어져 있는지 (로컬 좌표계 기준)
    private Vector3 previewCenterLocalOffset = Vector3.zero;
    private bool hasPreviewCenterOffset = false;
    // 직전 프레임의 제스처 상태 (디버깅/상태 전이 감지를 위해)
    private GestureState lastGestureState = GestureState.Idle;
    
    // 궤적 시각화 관련
    private GameObject trajectoryLineObject;
    private LineRenderer trajectoryLineRenderer;
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

            if (showDebugLog)
            {
                Debug.Log("[ArcheryGestureManager] OnEnable - EnhancedTouch enabled", this); // ARCHERY_DEBUG_LOG
            }
        }

        // 씬이 바뀔 때 제스처 상태를 초기화하기 위해 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (showDebugLog)
        {
            Debug.Log("[ArcheryGestureManager] OnEnable - subscribed to sceneLoaded", this); // ARCHERY_DEBUG_LOG
        }

        // 3D 조준 프리뷰 초기화
        EnsurePreviewInstance();
        HidePreview(); // 시작 시에는 항상 숨김

        if (showDebugLog)
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

                if (showDebugLog)
                {
                    Debug.Log("[ArcheryGestureManager] OnDisable - EnhancedTouch disabled", this); // ARCHERY_DEBUG_LOG
                }
            }

            if (showDebugLog)
            {
                Debug.Log("[ArcheryGestureManager] OnDisable - unsubscribed from sceneLoaded", this); // ARCHERY_DEBUG_LOG
            }

        // 3D 조준 프리뷰 정리
        HidePreview();
        HideTrajectory();
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
        if (showDebugLog)
        {
            DrawDebugInfo();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 로드되면 제스처 상태 및 터치 정보 초기화
        ResetGesture();
        activeTouches.Clear();

        if (showDebugLog)
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

        if (showDebugLog)
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

            if (showDebugLog)
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

            if (showDebugLog)
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
                if (showDebugLog)
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

            if (showDebugLog)
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
            if (showDebugLog)
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

                    if (showDebugLog)
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

            if (showDebugLog)
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

                if (showDebugLog)
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

        if (showDebugLog)
        {
            Debug.Log("[ArcheryGestureManager] ResetGesture - state set to Idle, ids cleared", this); // ARCHERY_DEBUG_LOG
        }
    }

    private void CancelGesture()
    {
        if (showDebugLog)
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
        // 드래그 거리에 비례하여 각도 조정
        float normalizedDistance = Mathf.Clamp01(data.distance / maxDrawDistance);
        
        // Pitch(수직): 아래로 드래그(y<0) -> 위로 발사(pitch>0), 위로 드래그(y>0) -> 아래로 발사(pitch<0)
        // 드래그 방향과 거리에 따라 각도 조정
        float rawPitch = data.direction.y * maxPitchAngle * normalizedDistance;
        data.pitch = Mathf.Clamp(rawPitch, -maxPitchAngle, maxPitchAngle);

        // 오른쪽으로 드래그하면 왼쪽으로 발사
        // direction.x가 양수(오른쪽으로 드래그) → yawDeg가 음수(왼쪽으로 향함)
        // 드래그 거리에 비례하여 각도 조정
        data.yaw = Mathf.Clamp(-data.direction.x * maxYawAngle * normalizedDistance, -maxYawAngle, maxYawAngle);

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

    /// <summary>
    /// 사용자의 제스처 데이터를 기반으로 화살을 실제로 발사하는 함수
    /// 
    /// 이 함수는 다음 단계를 수행합니다:
    /// 1. 필수 컴포넌트 검증 (arrowPrefab, arrowSpawnPoint)
    /// 2. 화면 드래그 정보를 3D 공간의 발사 방향으로 변환
    /// 3. 화살 프리팹을 인스턴스화하고 올바른 회전과 위치 설정
    /// 4. Rigidbody에 물리 힘을 적용하여 화살을 발사
    /// 
    /// 주의: 이 함수는 OnRelease 이벤트에서 호출되며, 
    /// 사용자가 화면에서 손가락을 떼는 순간 실행됩니다.
    /// </summary>
    /// <param name="data">사용자의 제스처 데이터 (드래그 시작점, 현재점, 거리, 방향 등)</param>
    private void ShootArrow(GestureData data)
    {
        // ============================================
        // 1단계: 필수 컴포넌트 검증
        // ============================================
        // arrowPrefab: 발사할 화살 프리팹 (Inspector에서 설정)
        // arrowSpawnPoint: 화살이 생성될 위치와 기본 방향을 가진 Transform
        // 둘 중 하나라도 없으면 화살을 발사할 수 없으므로 경고 후 종료
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            Debug.LogWarning("[ArcheryGestureManager] arrowPrefab 또는 arrowSpawnPoint가 설정되어 있지 않습니다.", this); // ARCHERY_DEBUG_LOG
            return;
        }

        // ============================================
        // 2단계: 발사 힘 설정
        // ============================================
        // maxForce: Inspector에서 설정한 최대 발사 힘 (기본값: 30)
        // 현재는 드래그 거리와 관계없이 항상 최대 힘을 사용
        // 향후 normalizedPower를 활용하여 거리에 비례한 힘을 적용할 수 있음
        float force = maxForce;

        // ============================================
        // 3단계: 발사 기본 방향 결정
        // ============================================
        // 화살이 발사될 기본 방향을 결정합니다.
        // 우선순위:
        //   1. arrowSpawnPoint.forward (가장 정확한 방향)
        //   2. 카메라의 forward (arrowSpawnPoint가 없을 경우)
        //   3. Vector3.forward (Z축 양수 방향, 최후의 수단)
        // 
        // 이 기본 방향에 사용자의 드래그 제스처에 따른 회전이 추가로 적용됩니다.
        Camera cam = Camera.main;
        Vector3 baseDir = arrowSpawnPoint != null
            ? arrowSpawnPoint.forward
            : (cam != null ? cam.transform.forward : Vector3.forward);

        // ============================================
        // 4단계: 화면 드래그 정보를 3D 각도로 변환
        // ============================================
        // 사용자가 화면에서 드래그한 정보를 3D 공간의 발사 각도로 변환합니다.
        // 이 과정은 프리뷰 화살과 동일한 로직을 사용하여 일관성을 유지합니다.
        
        // 4-1. 드래그 벡터 계산
        // data.currentPosition: 사용자가 손가락을 떼는 시점의 화면 좌표
        // data.startPosition: 사용자가 드래그를 시작한 화면 좌표
        // dragVec: 화면 좌표계에서의 드래그 벡터 (픽셀 단위)
        Vector2 dragVec = (data.currentPosition - data.startPosition);
        
        // 드래그 방향을 정규화 (단위 벡터로 변환)
        // sqrMagnitude > 0.0001f: 매우 작은 이동은 무시 (0으로 나누기 방지 및 노이즈 제거)
        Vector2 dragDir = dragVec.sqrMagnitude > 0.0001f ? dragVec.normalized : Vector2.zero;
        float dragDistance = dragVec.magnitude; // 드래그한 총 거리 (픽셀)
        
        // 4-2. 드래그 거리를 정규화 (0.0 ~ 1.0 범위로 변환)
        // maxDrawDistance: Inspector에서 설정한 최대 드래그 거리 (기본값: 300px)
        // normalizedDistance: 드래그 거리를 최대 거리에 대한 비율로 변환
        // 예: dragDistance=150px, maxDrawDistance=300px → normalizedDistance=0.5
        // 이 값은 각도 계산에 사용되어, 짧게 당기면 작은 각도, 길게 당기면 큰 각도가 됩니다.
        float normalizedDistance = Mathf.Clamp01(dragDistance / maxDrawDistance);
        
        // 4-3. 수직 각도 (Pitch) 계산
        // dragDir.y 값의 의미:
        //   - 음수: 아래로 드래그 (화면 좌표계에서 Y축은 위가 양수, 아래가 음수)
        //   - 양수: 위로 드래그
        // 
        // rawVerticalAngle 계산:
        //   - dragDir.y가 음수(아래로) → rawVerticalAngle이 음수 → 위로 발사
        //   - dragDir.y가 양수(위로) → rawVerticalAngle이 양수 → 아래로 발사
        // 
        // normalizedDistance를 곱하여 드래그 거리에 비례한 각도 계산
        // maxPitchAngle: Inspector에서 설정한 최대 수직 각도 (기본값: 80도)
        float rawVerticalAngle = dragDir.y * maxPitchAngle * normalizedDistance;
        float verticalAngleDeg = Mathf.Clamp(rawVerticalAngle, -maxPitchAngle, maxPitchAngle);
       
        // 4-4. 수평 각도 (Yaw) 계산
        // dragDir.x 값의 의미:
        //   - 양수: 오른쪽으로 드래그
        //   - 음수: 왼쪽으로 드래그
        // 
        // horizontalAngleDeg 계산:
        //   - dragDir.x가 양수(오른쪽) → horizontalAngleDeg가 음수 → 왼쪽으로 발사
        //   - dragDir.x가 음수(왼쪽) → horizontalAngleDeg가 양수 → 오른쪽으로 발사
        // 
        // 부호가 반대인 이유: 활쏘기처럼 당기는 방향의 반대로 발사하는 것이 직관적
        // maxYawAngle: Inspector에서 설정한 최대 수평 각도 (기본값: 70도)
        float horizontalAngleDeg = Mathf.Clamp(-dragDir.x * maxYawAngle * normalizedDistance, -maxYawAngle, maxYawAngle);

        // ============================================
        // 5단계: 각도를 Quaternion 회전으로 변환
        // ============================================
        // 계산한 각도(verticalAngleDeg, horizontalAngleDeg)를 Quaternion으로 변환하여
        // 3D 공간에서의 회전을 표현합니다.
        Quaternion rot;
        if (cam != null)
        {
            // 카메라가 있는 경우: 카메라의 right 벡터를 기준으로 Pitch 회전
            // 회전 적용 순서: Yaw (수평) → Pitch (수직)
            // 
            // Quaternion.AngleAxis(각도, 축):
            //   - horizontalAngleDeg: 수평 회전 각도 (Yaw, 좌우)
            //   - Vector3.up: 수직 회전 축 (세계 Y축 기준, 좌우 회전)
            //   - verticalAngleDeg: 수직 회전 각도 (Pitch, 위아래)
            //   - cam.transform.right: 수평 회전 축 (카메라의 오른쪽 방향 기준, 위아래 회전)
            //
            // 두 회전을 곱하면: 먼저 Yaw 회전이 적용되고, 그 다음 Pitch 회전이 적용됩니다.
            // 이 순서는 활쏘기 게임에서 직관적인 조준을 위해 중요합니다.
            rot = Quaternion.AngleAxis(horizontalAngleDeg, Vector3.up) *
                  Quaternion.AngleAxis(verticalAngleDeg, cam.transform.right);
        }
        else
        {
            // 카메라가 없는 경우: Euler 각도로 직접 변환
            // Euler(x, y, z): X축(Pitch), Y축(Yaw), Z축(Roll) 순서로 회전
            // 카메라가 없을 때는 간단하게 Euler 각도로 변환
            rot = Quaternion.Euler(verticalAngleDeg, horizontalAngleDeg, 0f);
        }

        // ============================================
        // 6단계: 최종 발사 방향 계산
        // ============================================
        // baseDir (arrowSpawnPoint.forward 또는 카메라 forward)에
        // 계산한 회전(rot)을 적용하여 최종 발사 방향(dir)을 얻습니다.
        // 이 방향이 화살이 날아갈 3D 공간의 방향 벡터입니다.
        Vector3 dir = rot * baseDir;

        // 디버그 로그: 계산된 발사 방향 정보 출력
        if (showDebugLog)
        {
            Debug.Log(
                $"[ArcheryGestureManager] Calculated shot direction - dragDir={dragDir}, verticalAngle(pitch)={verticalAngleDeg:F1}°, horizontalAngle(yaw)={horizontalAngleDeg:F1}°, baseDir={baseDir}",
                this); // ARCHERY_DEBUG_LOG
        }

        // ============================================
        // 7단계: 화살 프리팹 인스턴스화 및 회전 설정
        // ============================================
        // 화살 프리팹을 씬에 생성하고, 계산한 발사 방향으로 회전을 설정합니다.
        // 
        // Quaternion.LookRotation(방향, 위쪽):
        //   - dir: 화살이 바라볼 방향 (forward 방향)
        //   - Vector3.up: 위쪽 방향 (roll 회전 방지)
        //
        // 이 회전은 화살의 Transform.forward가 dir 방향을 향하도록 설정합니다.
        // 프리팹의 초기 회전(90도 X축)은 프리팹 자체에 저장되어 있으므로,
        // LookRotation으로 계산한 회전이 프리팹의 로컬 회전에 추가로 적용됩니다.
        Quaternion arrowRotation = Quaternion.LookRotation(dir, Vector3.up);
        
        // 화살 인스턴스 생성: arrowSpawnPoint 위치에 arrowRotation 회전으로 생성
        // Instantiate의 세 번째 매개변수(회전)가 화살의 Transform.rotation에 직접 설정됨
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowRotation);
        
        // 디버그 로그: 화살 회전 정보 출력
        if (showDebugLog)
        {
            Debug.Log($"[ArcheryGestureManager] Arrow rotation set - rotation={arrowRotation.eulerAngles}, dir={dir}", this);
        }

        // 디버그 로그: 화살 생성 위치 정보 출력
        if (showDebugLog)
        {
            Debug.Log(
                $"[ArcheryGestureManager] Spawned arrow instance '{arrow.name}' at {arrowSpawnPoint.position} with dir={dir}",
                this); // ARCHERY_DEBUG_LOG
        }

        // ============================================
        // 8단계: 물리 힘 적용하여 화살 발사
        // ============================================
        // Rigidbody 컴포넌트를 찾아 물리 힘을 적용하여 화살을 실제로 날아가게 합니다.
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 화살의 초기 속도를 0으로 설정 (이전 상태의 영향을 제거)
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // 발사 방향(dir)으로 힘을 적용
            // ForceMode.Impulse: 즉시 속도를 변경하는 방식 (순간적인 힘)
            // dir * force: 방향 벡터에 힘의 크기를 곱하여 최종 힘 벡터 생성
            // 이 힘은 화살의 질량에 따라 가속도로 변환되어 화살을 날아가게 합니다.
            rb.AddForce(dir * force, ForceMode.Impulse);

            // 디버그 로그: 적용된 힘과 결과 속도 정보 출력
            if (showDebugLog)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] Applied force to arrow - force={force:F1}, velocity={rb.linearVelocity}, mass={rb.mass}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
        else if (showDebugLog)
        {
            // Rigidbody가 없으면 물리 시뮬레이션이 작동하지 않으므로 경고
            Debug.Log("[ArcheryGestureManager] Spawned arrow has no Rigidbody component", this); // ARCHERY_DEBUG_LOG
        }
        
        // 함수 종료: 화살이 성공적으로 발사되었습니다.
        // 이후 화살의 비행은 ArcheryArrow 스크립트와 Unity의 물리 엔진이 처리합니다.
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
        if (state != lastGestureState && showDebugLog)
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
            HideTrajectory();
            return;
        }

        // 현재 제스처 데이터 조회
        var data = GetCurrentGestureData();

        // 최소 드로우 거리 미만이면 "그냥 클릭"으로 간주하고 프리뷰를 숨긴다.
        if (data.distance < minDrawDistance)
        {
            HidePreview();
            HideTrajectory();
            return;
        }

        // 여기까지 왔으면 실제로 조준 중이므로 프리뷰를 보여줌
        EnsurePreviewInstance();
        if (previewInstance == null) return;

        if (!previewInstance.activeSelf)
        {
            previewInstance.SetActive(true);

            if (showDebugLog)
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
        float dragDistance = dragVec.magnitude;

        float verticalAngleDeg = 0f;
        float horizontalAngleDeg = 0f;

        // 드래그 방향에 따라 각도 조정
        // 아래로 드래그(dragDir.y < 0) → 각도 증가 (위로 발사)
        // 위로 드래그(dragDir.y > 0) → 각도 감소 (아래로 발사)
        // 드래그 거리에 비례하여 각도 조정
        float normalizedDistance = Mathf.Clamp01(dragDistance / maxDrawDistance);
        
        // 수직 각도: 드래그 방향과 거리에 따라 조정
        // dragDir.y가 음수(아래로)면 양수 각도, 양수(위로)면 음수 각도
        float rawVerticalAngle = dragDir.y * maxPitchAngle * normalizedDistance;
        verticalAngleDeg = Mathf.Clamp(rawVerticalAngle, -maxPitchAngle, maxPitchAngle);
       
        // 수평 각도: 좌우 드래그에 따라 조정
        horizontalAngleDeg = Mathf.Clamp(-dragDir.x * maxYawAngle * normalizedDistance, -maxYawAngle, maxYawAngle);
        

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

        if (showDebugLog)
        {
            Debug.Log(
                $"[ArcheryGestureManager] UpdatePreviewByGesture - state={state}, distance={data.distance:F1}, power={data.normalizedPower:F2}, dragDir={dragDir}, verticalAngle(pitch)={verticalAngleDeg:F1}°, horizontalAngle(yaw)={horizontalAngleDeg:F1}°, pos={previewTransform.position}",
                this); // ARCHERY_DEBUG_LOG
        }

        // 화살 궤적 시각화 (게임에서 항상 표시)
        if (arrowPrefab != null)
        {
            Rigidbody rb = arrowPrefab.GetComponent<Rigidbody>();
            float mass = (rb != null) ? rb.mass : 1.0f;
            // 힘은 거리에 비례하지 않고 maxForce라고 가정 (ShootArrow와 동일하게)
            // 만약 ShootArrow에서 normalizedPower를 쓴다면 여기도 맞춰야 함.
            // 현재 ShootArrow 코드는 "float force = maxForce;" 로 고정힘을 사용 중임.
            Vector3 v0 = dir * (maxForce / mass);
            DrawTrajectory(arrowSpawnPoint.position, v0);
        }
    }

    /// <summary>
    /// 조준 프리뷰 화살 인스턴스가 존재하는지 확인하고, 없으면 생성하는 함수
    /// 
    /// [사용 목적]
    /// 이 함수는 "Lazy Initialization" 패턴을 사용하여 프리뷰 화살을 필요할 때만 생성합니다.
    /// 화살 프리팹을 미리 생성해두고 재사용함으로써 성능을 최적화합니다.
    /// 
    /// [호출 시점]
    /// 1. OnEnable(): 매니저가 활성화될 때 초기화를 위해 호출
    ///    - 게임 시작 시 또는 씬 로드 시 프리뷰 시스템 준비
    /// 
    /// 2. UpdatePreviewByGesture(): 사용자가 화살을 당기기 시작할 때 호출
    ///    - 실제로 조준 프리뷰가 필요할 때 인스턴스가 존재하는지 확인
    ///    - 없으면 생성하고, 있으면 재사용
    /// 
    /// [주요 기능]
    /// 1. 프리뷰 인스턴스 존재 여부 확인 (이미 있으면 재생성하지 않음)
    /// 2. arrowPrefab을 복제하여 previewInstance 생성
    /// 3. 화살의 메쉬 중심 오프셋 계산 (회전 중심 보정을 위해)
    /// 4. 초기 상태는 비활성화 (SetActive(false))
    /// 
    /// [중심 오프셋 계산의 중요성]
    /// 화살 프리팹의 피벗(pivot)이 화살의 끝에 있을 수 있어서,
    /// 회전 시 끝을 기준으로 회전하게 됩니다. 이를 방지하기 위해
    /// 메쉬의 시각적 중심을 계산하여, 나중에 위치를 보정할 때 사용합니다.
    /// </summary>
    private void EnsurePreviewInstance()
    {
        // ============================================
        // 1단계: 이미 프리뷰 인스턴스가 존재하는지 확인
        // ============================================
        // previewInstance는 클래스 멤버 변수로, 한 번 생성되면 계속 재사용됩니다.
        // 이미 존재하면 새로 생성하지 않고 바로 반환 (성능 최적화)
        if (previewInstance != null)
        {
            if (showDebugLog)
            {
                Debug.Log("[ArcheryGestureManager] EnsurePreviewInstance - already exists", this); // ARCHERY_DEBUG_LOG
            }
            return; // 이미 존재하므로 함수 종료
        }
        
        // ============================================
        // 2단계: arrowPrefab이 설정되어 있는지 확인
        // ============================================
        // arrowPrefab이 없으면 프리뷰를 생성할 수 없으므로 경고 후 종료
        // 이는 Inspector에서 arrowPrefab을 설정하지 않았을 때 발생합니다.
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[ArcheryGestureManager] arrowPrefab 이 설정되어 있지 않습니다.");
            return;
        }

        // ============================================
        // 3단계: 프리뷰 화살 인스턴스 생성
        // ============================================
        // arrowPrefab을 복제하여 previewInstance를 생성합니다.
        // Instantiate(arrowPrefab): 위치와 회전을 지정하지 않으면 (0,0,0)에 생성
        // 나중에 UpdatePreviewByGesture()에서 위치와 회전을 매 프레임 업데이트합니다.
        previewInstance = Instantiate(arrowPrefab);
        previewTransform = previewInstance.transform; // Transform 참조 저장 (성능 최적화)
        
        if (showDebugLog)
        {
            Debug.Log(
                $"[ArcheryGestureManager] EnsurePreviewInstance - instantiated preview",
                this); // ARCHERY_DEBUG_LOG
        }

        // ============================================
        // 4단계: 화살의 메쉬 중심 오프셋 계산
        // ============================================
        // 화살이 회전할 때 중심을 기준으로 회전하도록 하기 위해,
        // 프리팹의 피벗(Transform.position)에서 메쉬의 시각적 중심까지의 오프셋을 계산합니다.
        // 
        // 이 오프셋은 나중에 UpdatePreviewByGesture()에서 사용되어,
        // 화살의 중심이 arrowSpawnPoint에 위치하도록 위치를 보정합니다.
        var renderer = previewInstance.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            // Renderer.bounds.center: 메쉬의 시각적 중심 (월드 좌표)
            Vector3 worldCenter = renderer.bounds.center;
            
            // InverseTransformPoint: 월드 좌표를 로컬 좌표로 변환
            // previewTransform의 로컬 좌표계에서 메쉬 중심의 위치를 얻습니다.
            Vector3 localCenter = previewTransform.InverseTransformPoint(worldCenter);

            // 로컬 피벗(0,0,0, 즉 Transform.position)에서 메쉬 중심까지의 오프셋
            // 이 값은 나중에 위치 보정에 사용됩니다.
            previewCenterLocalOffset = localCenter;
            hasPreviewCenterOffset = (previewCenterLocalOffset != Vector3.zero);

            if (showDebugLog)
            {
                Debug.Log(
                    $"[ArcheryGestureManager] Calculated preview center offset - localCenter={localCenter}, hasOffset={hasPreviewCenterOffset}",
                    this); // ARCHERY_DEBUG_LOG
            }
        }
        else
        {
            // Renderer가 없으면 중심 오프셋을 계산할 수 없음
            // 이 경우 오프셋 없이 사용 (피벗이 이미 중심에 있다고 가정)
            previewCenterLocalOffset = Vector3.zero;
            hasPreviewCenterOffset = false;

            if (showDebugLog)
            {
                Debug.Log("[ArcheryGestureManager] EnsurePreviewInstance - no Renderer found on preview", this); // ARCHERY_DEBUG_LOG
            }
        }

        // ============================================
        // 5단계: 초기 상태는 비활성화
        // ============================================
        // 프리뷰 화살은 사용자가 화살을 당기기 시작할 때만 보여야 하므로,
        // 생성 직후에는 비활성화 상태로 둡니다.
        // UpdatePreviewByGesture()에서 필요할 때 SetActive(true)로 활성화합니다.
        previewInstance.SetActive(false);
        
        // 함수 종료: 프리뷰 인스턴스가 준비되었습니다.
        // 이제 UpdatePreviewByGesture()에서 이 인스턴스를 사용하여
        // 사용자의 조준 방향에 따라 위치와 회전을 업데이트할 수 있습니다.
    }

    /// <summary>
    /// 화살의 예상 궤적을 계산하고 LineRenderer로 시각화하는 함수
    /// 
    /// [사용 목적]
    /// 사용자가 화살을 당기고 조준하는 동안, 실제로 발사했을 때 화살이 날아갈 경로를
    /// 미리 보여줍니다. 이를 통해 사용자가 조준을 더 정확하게 할 수 있습니다.
    /// 
    /// [호출 시점]
    /// UpdatePreviewByGesture()에서 매 프레임 호출됩니다.
    /// 사용자가 화살을 당기는 동안 실시간으로 궤적이 업데이트됩니다.
    /// 
    /// [물리 시뮬레이션]
    /// 이 함수는 물리 엔진의 중력과 초기 속도를 사용하여 포물선 운동을 계산합니다.
    /// 실제 화살 발사와 동일한 물리 법칙을 적용하여 정확한 예측을 제공합니다.
    /// 
    /// [수식 설명]
    /// 포물선 운동 공식: p(t) = p0 + v0*t + 0.5*g*t^2
    ///   - p0: 초기 위치 (startPos)
    ///   - v0: 초기 속도 (velocity)
    ///   - g: 중력 가속도 (Physics.gravity)
    ///   - t: 경과 시간
    /// </summary>
    /// <param name="startPos">화살 발사 시작 위치 (arrowSpawnPoint.position)</param>
    /// <param name="velocity">화살의 초기 속도 벡터 (방향 * (힘 / 질량))</param>
    private void DrawTrajectory(Vector3 startPos, Vector3 velocity)
    {
        // ============================================
        // 1단계: LineRenderer 준비
        // ============================================
        // 궤적을 그리기 위한 LineRenderer 컴포넌트가 필요합니다.
        // 없으면 EnsureTrajectoryLineRenderer()에서 생성합니다.
        // EnsureTrajectoryLineRenderer() 내부에서도 null 체크를 하므로,
        // 여기서는 한 번만 호출하고 결과를 확인합니다.
        EnsureTrajectoryLineRenderer();

        // 생성에 실패했거나 여전히 null이면 궤적을 그릴 수 없으므로 함수 종료
        if (trajectoryLineRenderer == null)
        {
            return;
        }

        // ============================================
        // 2단계: 궤적 계산 파라미터 설정
        // ============================================
        // timeStep: 시간 간격 (초 단위)
        //   - 작을수록 더 정밀한 궤적이지만 계산 비용이 증가
        //   - 0.05초 간격으로 계산하면 1초에 20개의 포인트 생성
        float timeStep = 0.05f;
        
        // maxTime: 궤적을 계산할 최대 시간 (초 단위)
        //   - 2초 동안의 궤적을 계산 (대부분의 화살이 이 시간 내에 착지)
        float maxTime = 2.0f;
        
        // gravity: Unity의 물리 엔진에서 설정된 중력 가속도
        //   - 기본값: (0, -9.81, 0) - Y축 아래 방향
        //   - 실제 화살 발사 시 사용되는 중력과 동일하게 설정
        Vector3 gravity = Physics.gravity;
        
        // ============================================
        // 3단계: 궤적 포인트 계산
        // ============================================
        // 시간에 따라 화살의 위치를 계산하여 포인트 리스트를 생성합니다.
        List<Vector3> trajectoryPoints = new List<Vector3>();
        trajectoryPoints.Add(startPos); // 시작점 추가

        // 시간이 0부터 maxTime까지 timeStep 간격으로 증가하면서 위치 계산
        for (float t = timeStep; t <= maxTime; t += timeStep)
        {
            // 포물선 운동 공식 적용
            // p(t) = p0 + v0*t + 0.5*g*t^2
            //   - startPos: 초기 위치 (p0)
            //   - velocity * t: 초기 속도로 이동한 거리 (v0*t)
            //   - 0.5f * gravity * t * t: 중력으로 인한 낙하 거리 (0.5*g*t^2)
            Vector3 pos = startPos + velocity * t + 0.5f * gravity * t * t;
            trajectoryPoints.Add(pos);
            
            // ============================================
            // 4단계: 지면 충돌 감지 (조기 종료 최적화)
            // ============================================
            // 화살이 지면에 닿으면 더 이상 계산할 필요가 없으므로 루프를 종료합니다.
            // 
            // 조건 설명:
            //   - pos.y < startPos.y - 0.1f: 현재 위치가 시작 위치보다 0.1m 이상 아래
            //   - velocity.y < 0: 속도의 Y 성분이 음수 (아래로 떨어지는 중)
            // 
            // 두 조건을 모두 만족하면 화살이 지면에 충돌했다고 판단하여 계산 중단
            // 이는 불필요한 계산을 줄여 성능을 최적화합니다.
            if (pos.y < startPos.y - 0.1f && velocity.y < 0)
            {
                break;
            }
        }

        // ============================================
        // 5단계: LineRenderer에 포인트 설정
        // ============================================
        // 계산한 궤적 포인트들을 LineRenderer에 설정하여 시각화합니다.
        
        // positionCount: LineRenderer가 그릴 선분의 포인트 개수 설정
        // 계산한 포인트 개수만큼 설정
        trajectoryLineRenderer.positionCount = trajectoryPoints.Count;
        
        // 각 포인트의 위치를 LineRenderer에 설정
        // LineRenderer는 이 포인트들을 순서대로 연결하여 선을 그립니다.
        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            trajectoryLineRenderer.SetPosition(i, trajectoryPoints[i]);
        }

        // ============================================
        // 6단계: 궤적 표시 활성화
        // ============================================
        // 궤적을 그릴 GameObject를 활성화하여 화면에 표시합니다.
        // trajectoryLineObject는 EnsureTrajectoryLineRenderer()에서 생성되며,
        // LineRenderer 컴포넌트를 포함하고 있습니다.
        if (trajectoryLineObject != null)
        {
            trajectoryLineObject.SetActive(true);
        }

        // ============================================
        // 7단계: 디버그 로그 출력
        // ============================================
        // 디버그 모드가 활성화되어 있으면 궤적 계산 정보를 출력합니다.
        // 개발 중 궤적이 올바르게 계산되는지 확인하는 데 유용합니다.
        if (showDebugLog)
        {
            Debug.Log($"[ArcheryGestureManager] DrawTrajectory - startPos: {startPos}, velocity: {velocity}, points: {trajectoryPoints.Count}", this);
        }
        
        // 함수 종료: 궤적이 성공적으로 계산되고 화면에 표시되었습니다.
        // 사용자가 조준을 조정하면 다음 프레임에 다시 호출되어 업데이트됩니다.
    }

    private void EnsureTrajectoryLineRenderer()
    {
        if (trajectoryLineRenderer != null)
        {
            return;
        }

        // 궤적을 표시할 GameObject 생성
        trajectoryLineObject = new GameObject("TrajectoryLine");
        trajectoryLineObject.transform.SetParent(transform);
        
        // LineRenderer 컴포넌트 추가
        trajectoryLineRenderer = trajectoryLineObject.AddComponent<LineRenderer>();
        
        // LineRenderer 설정
        trajectoryLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLineRenderer.startColor = Color.red;
        trajectoryLineRenderer.endColor = Color.red;
        trajectoryLineRenderer.startWidth = 0.05f;
        trajectoryLineRenderer.endWidth = 0.02f;
        trajectoryLineRenderer.useWorldSpace = true;
        trajectoryLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trajectoryLineRenderer.receiveShadows = false;

        // 초기에는 숨김
        trajectoryLineObject.SetActive(false);

        if (showDebugLog)
        {
            Debug.Log("[ArcheryGestureManager] EnsureTrajectoryLineRenderer - created LineRenderer", this);
        }
    }

    private void HideTrajectory()
    {
        if (trajectoryLineObject != null)
        {
            trajectoryLineObject.SetActive(false);
        }
    }

    private void HidePreview()
    {
        if (previewInstance != null)
        {
            previewInstance.SetActive(false);

            if (showDebugLog)
            {
                Debug.Log("[ArcheryGestureManager] HidePreview - preview disabled", this); // ARCHERY_DEBUG_LOG
            }
        }
    }
    #endregion
}