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
                _instance = FindFirstObjectByType<ArcheryGestureManager>();

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

            // AudioSource 초기화
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound

            LogDebug("[ArcheryGestureManager] Awake - set as singleton instance, AudioSource initialized");
        }
        else if (_instance != this)
        {
            // 다른 씬에서 이미 생성된 매니저가 있다면, 중복 객체는 제거
            LogDebug("[ArcheryGestureManager] Awake - duplicate instance found, destroying this one");

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

    [Header("Audio Settings")]
    [Tooltip("활을 당기기 시작할 때 재생할 효과음")]
    public AudioClip bowDrawStartSound;

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

    // Audio
    private AudioSource audioSource;

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
            LogDebug("[ArcheryGestureManager] OnEnable - EnhancedTouch enabled");
        }

        // 씬이 바뀔 때 제스처 상태를 초기화하기 위해 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        LogDebug("[ArcheryGestureManager] OnEnable - subscribed to sceneLoaded");

        // 3D 조준 프리뷰 초기화
        EnsurePreviewInstance();
        HidePreview(); // 시작 시에는 항상 숨김
        LogDebug("[ArcheryGestureManager] OnEnable - initialized 3D preview system");
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
                LogDebug("[ArcheryGestureManager] OnDisable - EnhancedTouch disabled");
            }

            LogDebug("[ArcheryGestureManager] OnDisable - unsubscribed from sceneLoaded");

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

        LogDebug($"[ArcheryGestureManager] OnSceneLoaded - scene='{scene.name}', state reset");
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

        LogDebug($"[ArcheryGestureManager] HandleTouchBegan - fingerId={fingerId}, pos={position}, state={currentState}");

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

            LogDebug($"[ArcheryGestureManager] Begin Drawing - primaryId={primaryTouchId}, startPos={drawStartPosition}");
            LogDebug($"[ArcheryGestureManager] OnDrawStart Invoke - distance={data.distance:F1}, power={data.normalizedPower:F2}, angle={data.angle:F1}");

            // 활 당기기 시작 효과음 재생
            PlayBowDrawStartSound();

            OnDrawStart?.Invoke(data);
        }
        else if (currentState == GestureState.Drawing && secondaryTouchId == -1)
        {
            // 두 번째 터치 - 조준 조정 모드
            secondaryTouchId = fingerId;
            currentState = GestureState.Aiming;

            LogDebug($"[ArcheryGestureManager] Enter Aiming mode - secondaryId={secondaryTouchId}, primaryId={primaryTouchId}");
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
                LogDebug($"[ArcheryGestureManager] OnDrawing Invoke - distance={data.distance:F1}, power={data.normalizedPower:F2}, angle={data.angle:F1}");

                OnDrawing?.Invoke(data);
            }
        }
        else if (fingerId == secondaryTouchId && currentState == GestureState.Aiming)
        {
            GestureData data = CreateGestureData();

            LogDebug($"[ArcheryGestureManager] OnAimAdjust Invoke - aimOffset={data.aimOffset}, distance={data.distance:F1}");

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
            LogDebug($"[ArcheryGestureManager] HandleTouchEnded (primary) - fingerId={fingerId}, pos={position}, state={currentState}");

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

                    LogDebug($"[ArcheryGestureManager] OnRelease Invoke - distance={data.distance:F1}, velocity={velocity.magnitude:F1}");

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

            LogDebug("[ArcheryGestureManager] OnDrawEnd Invoke - gesture finished");

            ResetGesture();
        }
        else if (fingerId == secondaryTouchId)
        {
            // 두 번째 터치가 끝남 - 다시 드로잉 모드로
            secondaryTouchId = -1;
            if (currentState == GestureState.Aiming)
            {
                currentState = GestureState.Drawing;

                LogDebug("[ArcheryGestureManager] Secondary touch ended - back to Drawing state");
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

        LogDebug("[ArcheryGestureManager] ResetGesture - state set to Idle, ids cleared"); // ARCHERY_DEBUG_LOG
    }

    private void CancelGesture()
    {
        LogDebug("[ArcheryGestureManager] CancelGesture - invoking OnCancel and clearing touches");

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
        if (state != lastGestureState)
        {
            LogDebug($"[ArcheryGestureManager] GestureState changed {lastGestureState} -> {state}");
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
            LogDebug("[ArcheryGestureManager] Show preview (start drawing)");
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

        LogDebug($"[ArcheryGestureManager] UpdatePreviewByGesture - state={state}, distance={data.distance:F1}, power={data.normalizedPower:F2}, dragDir={dragDir}, verticalAngle(pitch)={verticalAngleDeg:F1}°, horizontalAngle(yaw)={horizontalAngleDeg:F1}°, pos={previewTransform.position}");

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
    /// </summary>
    private void EnsurePreviewInstance()
    {
        if (previewInstance != null)
        {
            LogDebug("[ArcheryGestureManager] EnsurePreviewInstance - already exists");
            return; // 이미 존재하므로 함수 종료
        }
        
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[ArcheryGestureManager] arrowPrefab 이 설정되어 있지 않습니다.");
            return;
        }

        previewInstance = Instantiate(arrowPrefab);
        previewTransform = previewInstance.transform; // Transform 참조 저장 (성능 최적화)
        
        LogDebug($"[ArcheryGestureManager] EnsurePreviewInstance - instantiated preview");

        var renderer = previewInstance.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Vector3 worldCenter = renderer.bounds.center;
            Vector3 localCenter = previewTransform.InverseTransformPoint(worldCenter);
            previewCenterLocalOffset = localCenter;
            hasPreviewCenterOffset = (previewCenterLocalOffset != Vector3.zero);

            LogDebug($"[ArcheryGestureManager] Calculated preview center offset - localCenter={localCenter}, hasOffset={hasPreviewCenterOffset}");
        }
        else
        {
            previewCenterLocalOffset = Vector3.zero;
            hasPreviewCenterOffset = false;

            LogDebug("[ArcheryGestureManager] EnsurePreviewInstance - no Renderer found on preview");
        }

        previewInstance.SetActive(false);
    }

    /// <summary>
    /// 화살의 예상 궤적을 계산하고 LineRenderer로 시각화하는 함수
    /// </summary>
    private void DrawTrajectory(Vector3 startPos, Vector3 velocity)
    {
        EnsureTrajectoryLineRenderer();

        if (trajectoryLineRenderer == null)
        {
            return;
        }

        float timeStep = 0.05f;
        float maxTime = 2.0f;
        Vector3 gravity = Physics.gravity;
        
        List<Vector3> trajectoryPoints = new List<Vector3>();
        trajectoryPoints.Add(startPos); // 시작점 추가

        for (float t = timeStep; t <= maxTime; t += timeStep)
        {
            Vector3 pos = startPos + velocity * t + 0.5f * gravity * t * t;
            trajectoryPoints.Add(pos);
            
            if (pos.y < startPos.y - 0.1f && velocity.y < 0)
            {
                break;
            }
        }

        trajectoryLineRenderer.positionCount = trajectoryPoints.Count;
        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            trajectoryLineRenderer.SetPosition(i, trajectoryPoints[i]);
        }

        if (trajectoryLineObject != null)
        {
            trajectoryLineObject.SetActive(true);
        }

        LogDebug($"[ArcheryGestureManager] DrawTrajectory - startPos: {startPos}, velocity: {velocity}, points: {trajectoryPoints.Count}");
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

        LogDebug("[ArcheryGestureManager] EnsureTrajectoryLineRenderer - created LineRenderer");
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
            LogDebug("[ArcheryGestureManager] HidePreview - preview disabled");
        }
    }
    #endregion

    #region Audio
    /// <summary>
    /// 활 당기기 시작 효과음을 재생합니다.
    /// </summary>
    private void PlayBowDrawStartSound()
    {
        if (audioSource != null && bowDrawStartSound != null)
        {
            audioSource.PlayOneShot(bowDrawStartSound);
            LogDebug("[ArcheryGestureManager] Playing bow draw start sound");
        }
        else
        {
            if (audioSource == null)
            {
                LogDebug("[ArcheryGestureManager] AudioSource is null, cannot play sound");
            }
            if (bowDrawStartSound == null)
            {
                LogDebug("[ArcheryGestureManager] bowDrawStartSound is null, cannot play sound");
            }
        }
    }
    #endregion

    #region Shooter
    /// <summary>
    /// 사용자의 제스처 데이터를 기반으로 화살을 실제로 발사하는 함수
    /// </summary>
    private void ShootArrow(GestureData data)
    {
        // 1단계: 필수 컴포넌트 검증
        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            Debug.LogWarning("[ArcheryGestureManager] arrowPrefab 또는 arrowSpawnPoint가 설정되어 있지 않습니다.", this); // ARCHERY_DEBUG_LOG
            return;
        }

        // 2단계: 발사 힘 설정
        float force = maxForce;

        // 3단계: 발사 기본 방향 결정
        Camera cam = Camera.main;
        Vector3 baseDir = arrowSpawnPoint != null
            ? arrowSpawnPoint.forward
            : (cam != null ? cam.transform.forward : Vector3.forward);

        // 4단계: 화면 드래그 정보를 3D 각도로 변환
        Vector2 dragVec = (data.currentPosition - data.startPosition);
        Vector2 dragDir = dragVec.sqrMagnitude > 0.0001f ? dragVec.normalized : Vector2.zero;
        float dragDistance = dragVec.magnitude;
        
        float normalizedDistance = Mathf.Clamp01(dragDistance / maxDrawDistance);
        
        float rawVerticalAngle = dragDir.y * maxPitchAngle * normalizedDistance;
        float verticalAngleDeg = Mathf.Clamp(rawVerticalAngle, -maxPitchAngle, maxPitchAngle);
       
        float horizontalAngleDeg = Mathf.Clamp(-dragDir.x * maxYawAngle * normalizedDistance, -maxYawAngle, maxYawAngle);

        // 5단계: 각도를 Quaternion 회전으로 변환
        Quaternion rot;
        if (cam != null)
        {
            rot = Quaternion.AngleAxis(horizontalAngleDeg, Vector3.up) *
                  Quaternion.AngleAxis(verticalAngleDeg, cam.transform.right);
        }
        else
        {
            rot = Quaternion.Euler(verticalAngleDeg, horizontalAngleDeg, 0f);
        }

        // 6단계: 최종 발사 방향 계산
        Vector3 dir = rot * baseDir;

        LogDebug($"[ArcheryGestureManager] Calculated shot direction - dragDir={dragDir}, verticalAngle(pitch)={verticalAngleDeg:F1}°, horizontalAngle(yaw)={horizontalAngleDeg:F1}°, baseDir={baseDir}");

        // 7단계: 화살 프리팹 인스턴스화 및 회전 설정
        Quaternion arrowRotation = Quaternion.LookRotation(dir, Vector3.up);
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, arrowRotation);
        
        LogDebug($"[ArcheryGestureManager] Arrow rotation set - rotation={arrowRotation.eulerAngles}, dir={dir}");
        LogDebug($"[ArcheryGestureManager] Spawned arrow instance '{arrow.name}' at {arrowSpawnPoint.position} with dir={dir}");

        // 8단계: 물리 힘 적용하여 화살 발사
        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            rb.AddForce(dir * force, ForceMode.Impulse);

            LogDebug($"[ArcheryGestureManager] Applied force to arrow - force={force:F1}, velocity={rb.linearVelocity}, mass={rb.mass}");
        }
        else
        {
            LogDebug("[ArcheryGestureManager] Spawned arrow has no Rigidbody component");
        }
    }
    #endregion

    #region Debug
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

            // 조준 오프셋 표시
            if (secondaryTouchId != -1)
            {
                GUI.Label(new Rect(10, 160, 300, 30), $"Aim Offset: {data.aimOffset}", style);
            }
        }
    }

    private void LogDebug(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message, this);
        }
    }
    #endregion
}