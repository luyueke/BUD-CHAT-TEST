using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.Base;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TheatreEditorEmoteShowcase : MonoBehaviour
{
    [SerializeField] private Camera avatarCamera;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Button buyBtn;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private EventTrigger triggerArea;

    [Header("触控配置")]
    [SerializeField] private float panSensitivity = 0.15f;
    [SerializeField] private float rotateSensitivity = 0.25f;
    [SerializeField] private float scaleSensitivity = 0.08f;
    [SerializeField] private float posXLimit = 150f;
    [SerializeField] private float posYLimit = 100f;
    [SerializeField] private float rotXLimit = 60f;
    [SerializeField] private float rotYLimit = 180f;
    [SerializeField] private float scaleMin = -300f;
    [SerializeField] private float scaleMax = 300f;
    [SerializeField] private float rotationPivotY = 0.5f;

    [Header("Section 预览")]
    [SerializeField] private Text avatarNameText;
    [SerializeField] private RemoteImageBehaviour avatarTypeImage;
    [SerializeField] private RemoteImageBehaviour bgPreviewImage;
    [SerializeField] private GameObject avatarImageGObj;
    [SerializeField] private GameObject avatarNameGObj;
    [SerializeField] private RectTransform dialogueTextArea;

     [Header("快速设置演员镜头")]
     [SerializeField] private GameObject setPosRoot;


    [Serializable]
    public struct CameraPreset
    {
        public float posX;
        public float posY;
        public float scale;
    }

    [Header("镜头预设")]
    [SerializeField] private List<Toggle> toggles; // 预设镜头选择，0-全身，1-半身，2-近景，3-特写
    // cameraPresets 对应 referenceScaleY（默认 Scale=1）时的配置
    [SerializeField] private List<CameraPreset> cameraPresets = new()
    {
        new() { posX = 0, posY = -20, scale =  20 },  // 全身
        new() { posX = 0, posY =  -5, scale = -100 },  // 近距离全身
        new() { posX = 0, posY =  20, scale = -130 },  // 特写
    };
    // cameraPresetsMini 对应 miniScaleY（默认 Scale=0.66）时的配置
    [SerializeField] private List<CameraPreset> cameraPresetsMini = new()
    {
        new() { posX = 0, posY = -18, scale = -60 },   // 全身
        new() { posX = 0, posY = -18, scale = -120 },  // 近距离全身
        new() { posX = 0, posY =   0, scale = -140 },  // 特写
    };
    [SerializeField] private float referenceScaleY = 1f;    // cameraPresets 对应的角色 entityScale.y
    [SerializeField] private float miniScaleY      = 0.66f; // cameraPresetsMini 对应的角色 entityScale.y

    private readonly List<CharacterWrap> _characters = new();
    private List<(string avatarId, int clothesIndex)> _lastSelections = new();
    private RecommendItemData _emoteData;
    private Action<Vector3, Vector3, float> _onConfirm;
    private Action _onBack;

    // default camera localPosition (prefab value, captured once in Awake)
    private Vector3 _cameraDefaultPos;

    // base values set per PlayEmote call
    private Vector3 _baseCameraPos;
    private Vector3 _baseCharacterRotation;
    // 官方(PGC)双人动作时 characterRoot 的 X 位移（与 TheatreGamePanel 保持一致），其它情况为 0
    private float _characterRootX;

    // characterRoot base localPosition (prefab default, no rotation applied)
    private Vector3 _characterRootBasePos;

    // user gesture offsets
    private Vector2 _posOffset;
    private Vector2 _rotOffset;
    private float _scaleOffset;
    private bool _suppressToggleCallback;
    // cameraPresets(scale=1) 和 cameraPresetsMini(scale=0.66) 插值后的运行时预设
    private readonly List<CameraPreset> _effectivePresets = new();

    // two-finger tracking
    private Vector2 _prevTouch0;
    private Vector2 _prevTouch1;
    private int _prevTouchCount;

    private void Awake()
    {
        confirmBtn?.onClick.AddListener(OnConfirmClick);
        backBtn?.onClick.AddListener(OnBackClick);

        if (setPosRoot != null) setPosRoot.SetActive(false);

        if (avatarCamera != null)
            _cameraDefaultPos = avatarCamera.transform.localPosition;

        if (characterRoot != null)
            _characterRootBasePos = characterRoot.localPosition;

        for (int i = 0; i < toggles.Count; i++)
        {
            int idx = i;
            var tog = toggles[idx];
            if (tog == null) continue;
            tog.onValueChanged.RemoveAllListeners();
            tog.onValueChanged.AddListener(isOn => { if (isOn) ApplyPreset(idx); });
        }
    }

    private void UpdateEffectivePresets()
    {
        _effectivePresets.Clear();
        float currentScale = 1f;
        if (_characters.Count > 0 && _characters[0].Avatar != null)
        {
            var bodyCtrl = _characters[0].Avatar.GetComponentInChildren<CustomBodyTypeController>();
            currentScale = bodyCtrl != null ? bodyCtrl.transform.localScale.y : _characters[0].Avatar.transform.lossyScale.y;
        }
        float range = referenceScaleY - miniScaleY;
        float t = range > 0f ? Mathf.Clamp01((referenceScaleY - currentScale) / range) : 0f;
        int count = Mathf.Min(cameraPresets.Count, cameraPresetsMini.Count);
        for (int i = 0; i < count; i++)
        {
            var a = cameraPresets[i];
            var b = cameraPresetsMini[i];
            _effectivePresets.Add(new CameraPreset
            {
                posX  = Mathf.Lerp(a.posX,  b.posX,  t),
                posY  = Mathf.Lerp(a.posY,  b.posY,  t),
                scale = Mathf.Lerp(a.scale, b.scale, t),
            });
        }
    }

    private void ApplyPreset(int index)
    {
        if (_suppressToggleCallback || index < 0 || index >= _effectivePresets.Count) return;
        var p = _effectivePresets[index];
        _posOffset = new Vector2(p.posX, p.posY);
        _scaleOffset = p.scale;
        ApplyTransform();
    }

    private void SyncToggleSelection()
    {
        _suppressToggleCallback = true;
        for (int i = 0; i < toggles.Count && i < _effectivePresets.Count; i++)
        {
            if (toggles[i] == null) continue;
            var p = _effectivePresets[i];
            toggles[i].isOn = Mathf.Approximately(_posOffset.x, p.posX)
                           && Mathf.Approximately(_posOffset.y, p.posY)
                           && Mathf.Approximately(_scaleOffset, p.scale);
        }
        _suppressToggleCallback = false;
    }

    private void OnDisable()
    {
        foreach (var wrap in _characters)
        {
            if (wrap?.Avatar == null) continue;
            wrap.Avatar.GetComponent<AnimIKController>()?.StopAnim();
            AkSoundEngine.StopAll(wrap.Avatar);
            wrap.Avatar.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        CleanupCharacters();
    }

    public void Show(
        RecommendItemData emoteData,
        List<(string avatarId, int clothesIndex)> actorSelections,
        Dictionary<string, OCTheatreAvatarInfo> avatarInfoCache,
        Vector3 initialPosition,
        Vector3 initialRotation,
        float initialScale,
        Action<Vector3, Vector3, float> onConfirm,
        Action<Action> onBuyRequest,
        Action onBack,
        bool showSetPos = false)
    {
        _emoteData = emoteData;
        _onConfirm = onConfirm;
        _onBack = onBack;

        // setPosRoot（快速设置演员镜头）只在从 btn_setPos 打开时显示，其它情况隐藏
        if (setPosRoot != null) setPosRoot.SetActive(showSetPos);
        _posOffset = new Vector2(initialPosition.x, initialPosition.y);
        _rotOffset = new Vector2(initialRotation.x, initialRotation.y);
        _scaleOffset = initialScale;
        SyncToggleSelection();

        bool isPgc = emoteData?.UgcInfo is AnimInfo ai && string.IsNullOrEmpty(ai.metaDataUrl);
        bool isDouble = actorSelections != null && actorSelections.Count > 1;
        _baseCharacterRotation = TheatreEmoteCameraLayout.GetBaseCharacterRotation(isPgc, isDouble);

        if (SelectionsChanged(actorSelections))
        {
            CleanupCharacters();
            _lastSelections = new List<(string, int)>(actorSelections);
            LoadAndPlay(actorSelections, avatarInfoCache);
        }
        else
        {
            PlayEmote();
        }

        // emoteData 为空表示 idle 预览（段落未配置动作），没有购买概念，直接当作已拥有显示确认
        bool isOwned = emoteData == null || emoteData.interactInfo?.consumed == 1;
        SetOwnershipUI(isOwned, onBuyRequest);
    }

    public void SetSectionInfo(string avatarName, string avatarTypeUrl, string bgUrl, bool isNarrator = false)
    {
        if (avatarImageGObj != null) avatarImageGObj.SetActive(!isNarrator);
        if (avatarNameGObj != null) avatarNameGObj.SetActive(!isNarrator);

        if (!isNarrator)
        {
            if (avatarNameText != null) avatarNameText.text = avatarName;
            if (avatarTypeImage != null)
            {
                bool hasExpr = !string.IsNullOrEmpty(avatarTypeUrl);
                avatarTypeImage.gameObject.SetActive(hasExpr);
                if (hasExpr) avatarTypeImage.Load(avatarTypeUrl);
            }
        }

        if (bgPreviewImage != null)
        {
            bool hasBg = !string.IsNullOrEmpty(bgUrl);
            bgPreviewImage.gameObject.SetActive(hasBg);
            if (hasBg) bgPreviewImage.Load(bgUrl);
        }
    }

    private void Update()
    {
        if (triggerArea == null) return;
        int count = Input.touchCount;

        if (count == 1 && _prevTouchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                _rotOffset.x = Mathf.Clamp(_rotOffset.x - touch.deltaPosition.y * rotateSensitivity, -rotXLimit, rotXLimit);
                _rotOffset.y = Mathf.Clamp(_rotOffset.y - touch.deltaPosition.x * rotateSensitivity, -rotYLimit, rotYLimit);
                ApplyTransform();
            }
        }
        else if (count == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            if (_prevTouchCount != 2)
            {
                _prevTouch0 = t0.position;
                _prevTouch1 = t1.position;
            }
            else
            {
                var curCentroid = (t0.position + t1.position) * 0.5f;
                var prevCentroid = (_prevTouch0 + _prevTouch1) * 0.5f;
                var panDelta = curCentroid - prevCentroid;
                var pinchDelta = Vector2.Distance(t0.position, t1.position)
                               - Vector2.Distance(_prevTouch0, _prevTouch1);

                _prevTouch0 = t0.position;
                _prevTouch1 = t1.position;

                _posOffset.x = Mathf.Clamp(_posOffset.x + panDelta.x * panSensitivity, -posXLimit, posXLimit);
                _posOffset.y = Mathf.Clamp(_posOffset.y - panDelta.y * panSensitivity, -posYLimit, posYLimit);
                _scaleOffset = Mathf.Clamp(_scaleOffset - pinchDelta * scaleSensitivity, scaleMin, scaleMax);
                ApplyTransform();
            }
        }

        _prevTouchCount = count;
    }

    private void ApplyTransform()
    {
        if (avatarCamera != null)
        {
            avatarCamera.transform.localPosition = new Vector3(
                _baseCameraPos.x + _posOffset.x,
                _baseCameraPos.y + _posOffset.y,
                _baseCameraPos.z + _scaleOffset
            );
        }
        if (characterRoot != null)
        {
            float totalX = _baseCharacterRotation.x + _rotOffset.x;
            float totalY = _baseCharacterRotation.y + _rotOffset.y;
            characterRoot.localEulerAngles = new Vector3(totalX, totalY, _baseCharacterRotation.z);

            characterRoot.localPosition = _characterRootBasePos;
        }
    }

    private bool SelectionsChanged(List<(string avatarId, int clothesIndex)> newSelections)
    {
        if (newSelections == null || _lastSelections.Count != newSelections.Count) return true;
        for (int i = 0; i < _lastSelections.Count; i++)
            if (_lastSelections[i] != newSelections[i]) return true;
        return false;
    }

    private void SetOwnershipUI(bool isOwned, Action<Action> onBuyRequest)
    {
        buyBtn?.gameObject.SetActive(!isOwned);
        confirmBtn?.gameObject.SetActive(isOwned);

        if (!isOwned)
        {
            buyBtn?.onClick.RemoveAllListeners();
            buyBtn?.onClick.AddListener(() =>
            {
                onBuyRequest?.Invoke(() =>
                {
                    if (_emoteData != null)
                    {
                        _emoteData.interactInfo ??= new BaseInteractInfo();
                        _emoteData.interactInfo.consumed = 1;
                    }
                    buyBtn?.gameObject.SetActive(false);
                    confirmBtn?.gameObject.SetActive(true);
                    OnConfirmClick();
                });
            });
        }
    }

    private void LoadAndPlay(
        List<(string avatarId, int clothesIndex)> actorSelections,
        Dictionary<string, OCTheatreAvatarInfo> avatarInfoCache)
    {
        if (actorSelections == null || characterRoot == null) return;

        // 等所有角色模型加载完再显示，避免半成品/换装过程的闪屏
        int pending = 0;
        bool allCreated = false;
        void OnCharacterLoaded()
        {
            pending--;
            if (allCreated && pending <= 0) PlayEmote();
        }

        foreach (var (avatarId, clothesIndex) in actorSelections)
        {
            if (!avatarInfoCache.TryGetValue(avatarId, out var info)) continue;
            if (info.avatarClothes == null || clothesIndex < 0 || clothesIndex >= info.avatarClothes.Count) continue;

            var clothesJson = info.avatarClothes[clothesIndex].clothesJson;
            if (string.IsNullOrEmpty(clothesJson)) continue;

            var characterData = CharacterData.DeserializeObject(clothesJson);
            if (characterData == null) continue;

            pending++;
            var wrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, characterRoot, callback: OnCharacterLoaded);
            if (wrap == null) { pending--; continue; }

            wrap.Avatar.SetActive(false);
            bool isDouble = actorSelections.Count > 1;
            wrap.Avatar.transform.parent.parent.localPosition = isDouble
                ? new Vector3(-0.5f, -0.5f, 0)
                : new Vector3(0, -0.5f, 0);
            _characters.Add(wrap);
        }

        allCreated = true;
        // 所有回调已同步触发完（或没有需要加载的角色）时立即播放
        if (pending <= 0) PlayEmote();
    }

    private void PlayEmote()
    {
        if (_characters.Count == 0) return;

        bool isDouble = _characters.Count > 1;

        foreach (var wrap in _characters)
            wrap.Avatar.SetActive(true);

        UpdateEffectivePresets();
        SyncToggleSelection();

        if (avatarCamera != null)
        {
            bool isPgcEmote = _emoteData?.UgcInfo is AnimInfo ai && string.IsNullOrEmpty(ai.metaDataUrl);
            float pgcConfigX = 0f;
            if (isPgcEmote)
            {
                var emoConfig = DataTables.GetEmoUIConfig(((AnimInfo)_emoteData.UgcInfo).id);
                if (emoConfig != null) pgcConfigX = emoConfig.cameraPos.x;
            }

            _baseCameraPos = TheatreEmoteCameraLayout.GetBaseCameraPosition(
                _cameraDefaultPos, isPgcEmote, isDouble, pgcConfigX);
            ApplyTransform();
        }

        // 段落未配置动作：展示 idle（与 TheatreGamePanel 角色无动作时一致），不播放任何 emote
        if (_emoteData == null)
        {
            foreach (var wrap in _characters)
                wrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>()?.ResetEmoteAnimation();
            return;
        }

        var ikA = _characters[0].Avatar.GetComponent<AnimIKController>();
        var ikB = isDouble ? _characters[1].Avatar.GetComponent<AnimIKController>() : null;

        if (_emoteData.UgcInfo is AnimInfo animInfo)
        {
            if (!string.IsNullOrEmpty(animInfo.metaDataUrl))
            {
                if (ikA == null) return;
                ikA.Play(animInfo, ikB, onceCallback: () =>
                {
                    if (gameObject.activeInHierarchy) PlayEmote();
                });
            }
            else
            {
                var ctrlA = _characters[0].Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                if (ctrlA == null) return;
                if (animInfo.animType == 3 && isDouble)
                {
                    var charAPos = _characters[0].Avatar.transform.localPosition;
                    _characters[1].Avatar.transform.localPosition = charAPos + new Vector3(0, 0, 0.65f);
                    var ctrlB = _characters[1].Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
                    ctrlA.PlayDoubleEmoteForUICharacter(animInfo.id, ctrlB,
                        OnCompleteDoubleEmote: () => { if (gameObject.activeInHierarchy) PlayEmote(); },
                        loopNeedFinish: () => !gameObject.activeInHierarchy);
                }
                else
                {
                    ctrlA.PlaySingleEmoteForUICharacter(animInfo.id,
                        OnCompleteSingleEmote: () => { if (gameObject.activeInHierarchy) PlayEmote(); },
                        loopNeedFinish: () => !gameObject.activeInHierarchy);
                }
            }
        }
        else if (_emoteData.UgcInfo is PoseInfo poseInfo)
        {
            if (ikA == null) return;
            ikA.Pose(poseInfo, ikB);
        }
    }

    private void OnConfirmClick()
    {
        _onConfirm?.Invoke(
            new Vector3(_posOffset.x, _posOffset.y, 0f),
            new Vector3(_rotOffset.x, _rotOffset.y, 0f),
            _scaleOffset
        );
    }

    private void OnBackClick()
    {
        _onBack?.Invoke();
    }

    private void CleanupCharacters()
    {
        foreach (var wrap in _characters)
        {
            if (wrap?.Avatar != null)
                Destroy(wrap.Avatar);
        }
        _characters.Clear();
        _lastSelections.Clear();
    }
}
