using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 씬에서 공통 Settings UI(음악/사운드 토글 + 펼침 애니메이션)를 런타임에 생성/운영합니다.
/// </summary>
public sealed class SettingsOverlayRuntimeUI : MonoBehaviour
{
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float expandedWidth = 375f;

    private bool _isExpanded;
    private float _currentWidth;
    private float _targetWidth;
    private float _velocity;

    private RectTransform _settingsMoreRect;
    private RectTransform _settingsIconTransform;
    private Image _musicButtonImage;
    private Image _soundButtonImage;

    private Sprite _musicOnSprite;
    private Sprite _musicOffSprite;
    private Sprite _soundOnSprite;
    private Sprite _soundOffSprite;
    private Sprite _settingsButtonSprite;
    private Sprite _settingsGearSprite;
    private Sprite _settingsBackgroundSprite;

    private void Awake()
    {
        LoadSprites();
        BuildUI();
        InitializeStates();

        AudioSettings.MusicEnabledChanged += OnMusicEnabledChanged;
        AudioSettings.SoundEnabledChanged += OnSoundEnabledChanged;
    }

    private void OnDestroy()
    {
        AudioSettings.MusicEnabledChanged -= OnMusicEnabledChanged;
        AudioSettings.SoundEnabledChanged -= OnSoundEnabledChanged;
    }

    private void LoadSprites()
    {
        // Resources 경로: Assets/Resources/*
        _musicOnSprite = Resources.Load<Sprite>("musicOn");
        _musicOffSprite = Resources.Load<Sprite>("musicOff");
        _soundOnSprite = Resources.Load<Sprite>("soundOn");
        _soundOffSprite = Resources.Load<Sprite>("soundOff");
        _settingsButtonSprite = Resources.Load<Sprite>("settingButton");
        _settingsGearSprite = Resources.Load<Sprite>("settingGear");
        _settingsBackgroundSprite = Resources.Load<Sprite>("setting");

        // settingOff.png는 현재 UI에서는 사용하지 않지만, 향후 확장 대비로 남겨둘 수 있음.
    }

    private void BuildUI()
    {
        // 00StartUI/스테이지에서 동일 UI 배치값을 사용하기 위해,
        // 아래 컴포넌트들은 StartPanel(기존 씬)의 RectTransform 설정값을 그대로 재현합니다.
        //
        // - gear button: pos (433, -848), size (130, 140)
        // - more panel: anchor right, y=0.5, pos (-108, -848), size y=140, x는 펼침에 따라 변경
        //
        // Canvas 스케일러 참조해도 좌표가 맞는 구조를 가정합니다.

        var root = GetComponent<RectTransform>();
        if (root == null)
            root = gameObject.AddComponent<RectTransform>();

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = Vector2.zero;

        // Background(버튼 영역 뒤)
        if (_settingsBackgroundSprite != null)
        {
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var img = bg.GetComponent<Image>();
            img.sprite = _settingsBackgroundSprite;
            img.raycastTarget = false;
            img.color = new Color(1f, 0.9803922f, 0.9803922f, 0.84313726f);
        }

        // Settings gear button (클릭 영역 + 아이콘 회전 타이밍에만 사용)
        var settingsButtonGo = new GameObject("Button_settings", typeof(RectTransform), typeof(Image), typeof(Button));
        settingsButtonGo.transform.SetParent(root, false);
        var settingsButtonRt = settingsButtonGo.GetComponent<RectTransform>();
        settingsButtonRt.anchorMin = new Vector2(0.5f, 0.5f);
        settingsButtonRt.anchorMax = new Vector2(0.5f, 0.5f);
        settingsButtonRt.pivot = new Vector2(0.5f, 0.5f);
        settingsButtonRt.anchoredPosition = new Vector2(433f, -848f);
        settingsButtonRt.sizeDelta = new Vector2(130f, 140f);

        var settingsButtonImg = settingsButtonGo.GetComponent<Image>();
        settingsButtonImg.sprite = _settingsButtonSprite;
        settingsButtonImg.raycastTarget = true;

        var settingsButton = settingsButtonGo.GetComponent<Button>();
        settingsButton.targetGraphic = settingsButtonImg;
        settingsButton.onClick.AddListener(OnSettingsClicked);

        // Gear icon (기어 이미지 회전)
        var gearIconGo = new GameObject("Image", typeof(RectTransform), typeof(Image));
        gearIconGo.transform.SetParent(settingsButtonGo.transform, false);
        _settingsIconTransform = gearIconGo.GetComponent<RectTransform>();
        _settingsIconTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _settingsIconTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _settingsIconTransform.pivot = new Vector2(0.5f, 0.5f);
        _settingsIconTransform.anchoredPosition = new Vector2(-0.2f, 0.2f);
        _settingsIconTransform.sizeDelta = new Vector2(72f, 72f);
        _settingsIconTransform.localRotation = Quaternion.identity;

        var gearIconImg = gearIconGo.GetComponent<Image>();
        gearIconImg.sprite = _settingsGearSprite;
        gearIconImg.raycastTarget = false;

        // Expandable panel
        var panelGo = new GameObject("Button_settings_more", typeof(RectTransform), typeof(RectMask2D), typeof(HorizontalLayoutGroup));
        panelGo.transform.SetParent(root, false);
        _settingsMoreRect = panelGo.GetComponent<RectTransform>();

        _settingsMoreRect.anchorMin = new Vector2(1f, 0.5f);
        _settingsMoreRect.anchorMax = new Vector2(1f, 0.5f);
        _settingsMoreRect.pivot = new Vector2(1f, 0.5f);
        _settingsMoreRect.anchoredPosition = new Vector2(-108f, -848f);
        _settingsMoreRect.sizeDelta = new Vector2(expandedWidth, 140f);

        var rectMask = panelGo.GetComponent<RectMask2D>();
        rectMask.padding = new Vector4(0, 0, 0, 0);
        rectMask.softness = new Vector2Int(0, 0);

        var layout = panelGo.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 60, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 15f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childScaleWidth = false;
        layout.childScaleHeight = false;
        layout.reverseArrangement = false;

        // Music button
        CreateToggleButton(
            parent: panelGo.transform,
            name: "Button_music",
            sizeDelta: new Vector2(130f, 140f),
            pivot: new Vector2(1f, 0.5f),
            anchoredPosition: Vector2.zero,
            anchorMin: Vector2.zero,
            anchorMax: Vector2.zero,
            onClick: () =>
            {
                AudioSettings.IsMusicEnabled = !AudioSettings.IsMusicEnabled;
                AudioSettings.Save();
            },
            getImage: img => _musicButtonImage = img,
            getSprite: () => AudioSettings.IsMusicEnabled ? _musicOnSprite : _musicOffSprite
        );

        // Sound button
        CreateToggleButton(
            parent: panelGo.transform,
            name: "Button_sound",
            sizeDelta: new Vector2(130f, 140f),
            pivot: new Vector2(1f, 0.5f),
            anchoredPosition: Vector2.zero,
            anchorMin: Vector2.zero,
            anchorMax: Vector2.zero,
            onClick: () =>
            {
                AudioSettings.IsSoundEnabled = !AudioSettings.IsSoundEnabled;
                AudioSettings.Save();
            },
            getImage: img => _soundButtonImage = img,
            getSprite: () => AudioSettings.IsSoundEnabled ? _soundOnSprite : _soundOffSprite
        );
    }

    private void InitializeStates()
    {
        // 최초엔 펼치지 않음(기존 SettingsUIController Awake 동작과 동일)
        _isExpanded = false;
        _currentWidth = 0f;
        _targetWidth = 0f;
        _velocity = 0f;

        if (_settingsMoreRect != null)
        {
            var size = _settingsMoreRect.sizeDelta;
            size.x = 0f;
            _settingsMoreRect.sizeDelta = size;
        }

        UpdateButtonSprites();
    }

    private void OnSettingsClicked()
    {
        _isExpanded = !_isExpanded;
        _targetWidth = _isExpanded ? expandedWidth : 0f;
        _velocity = 0f;
    }

    private void Update()
    {
        if (_settingsMoreRect == null) return;

        if (Mathf.Abs(_currentWidth - _targetWidth) < 0.5f && _currentWidth != _targetWidth)
            _currentWidth = _targetWidth;

        if (Mathf.Abs(_currentWidth - _targetWidth) > 0.1f)
        {
            _currentWidth = Mathf.SmoothDamp(_currentWidth, _targetWidth, ref _velocity, animationDuration);
            var size = _settingsMoreRect.sizeDelta;
            size.x = _currentWidth;
            _settingsMoreRect.sizeDelta = size;

            if (_settingsIconTransform != null)
            {
                float rotT = Mathf.InverseLerp(0f, expandedWidth, _currentWidth);
                _settingsIconTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -90f, rotT));
            }
        }
    }

    private void OnMusicEnabledChanged(bool enabled)
    {
        if (_musicButtonImage == null) return;
        _musicButtonImage.sprite = enabled ? _musicOnSprite : _musicOffSprite;
    }

    private void OnSoundEnabledChanged(bool enabled)
    {
        if (_soundButtonImage == null) return;
        _soundButtonImage.sprite = enabled ? _soundOnSprite : _soundOffSprite;
    }

    private void UpdateButtonSprites()
    {
        OnMusicEnabledChanged(AudioSettings.IsMusicEnabled);
        OnSoundEnabledChanged(AudioSettings.IsSoundEnabled);
    }

    private void CreateToggleButton(
        Transform parent,
        string name,
        Vector2 sizeDelta,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 anchorMin,
        Vector2 anchorMax,
        System.Action onClick,
        System.Action<Image> getImage,
        System.Func<Sprite> getSprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        var img = go.GetComponent<Image>();
        img.sprite = getSprite();
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        getImage?.Invoke(img);
    }
}

