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
    [SerializeField] private float singleCameraSize = 1.4f;
    [SerializeField] private float doubleCameraSize = 2.0f;
    [SerializeField] private float doubleCameraXOffset = -50f;
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


    private readonly List<CharacterWrap> _characters = new();
    private List<(string avatarId, int clothesIndex)> _lastSelections = new();
    private RecommendItemData _emoteData;
    private Action<Vector3, Vector3, float> _onConfirm;
    private Action _onBack;

    // default camera state (captured once in Awake)
    private float _defaultCameraZ;
    private float _defaultCameraY;

    // base values set per PlayEmote call
    private float _baseCameraX;
    private Vector3 _baseCharacterRotation;

    // characterRoot base localPosition (prefab default, no rotation applied)
    private Vector3 _characterRootBasePos;

    // user gesture offsets
    private Vector2 _posOffset;
    private Vector2 _rotOffset;
    private float _scaleOffset;

    // two-finger tracking
    private Vector2 _prevTouch0;
    private Vector2 _prevTouch1;
    private int _prevTouchCount;

    private void Awake()
    {
        confirmBtn?.onClick.AddListener(OnConfirmClick);
        backBtn?.onClick.AddListener(OnBackClick);

        if (avatarCamera != null)
        {
            _defaultCameraZ = avatarCamera.transform.localPosition.z;
            _defaultCameraY = avatarCamera.transform.localPosition.y;
        }

        if (characterRoot != null)
            _characterRootBasePos = characterRoot.localPosition;
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
        Action onBack)
    {
        _emoteData = emoteData;
        _onConfirm = onConfirm;
        _onBack = onBack;
        _posOffset = new Vector2(initialPosition.x, initialPosition.y);
        _rotOffset = new Vector2(initialRotation.x, initialRotation.y);
        _scaleOffset = initialScale;

        bool isPgc = emoteData?.UgcInfo is AnimInfo ai && string.IsNullOrEmpty(ai.metaDataUrl);
        bool isDouble = actorSelections != null && actorSelections.Count > 1;
        _baseCharacterRotation = (isPgc && isDouble) ? new Vector3(0, -90, 0) : Vector3.zero;

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

        bool isOwned = emoteData?.interactInfo?.consumed == 1;
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
                _baseCameraX + _posOffset.x,
                _defaultCameraY + _posOffset.y,
                _defaultCameraZ + _scaleOffset
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

        foreach (var (avatarId, clothesIndex) in actorSelections)
        {
            if (!avatarInfoCache.TryGetValue(avatarId, out var info)) continue;
            if (info.avatarClothes == null || clothesIndex < 0 || clothesIndex >= info.avatarClothes.Count) continue;

            var clothesJson = info.avatarClothes[clothesIndex].clothesJson;
            if (string.IsNullOrEmpty(clothesJson)) continue;

            var characterData = CharacterData.DeserializeObject(clothesJson);
            if (characterData == null) continue;

            var wrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, characterRoot);
            if (wrap == null) continue;

            wrap.Avatar.SetActive(false);
            bool isDouble = actorSelections.Count > 1;
            wrap.Avatar.transform.parent.parent.localPosition = isDouble
                ? new Vector3(-0.5f, -0.5f, 0)
                : new Vector3(0, -0.5f, 0);
            _characters.Add(wrap);
        }

        PlayEmote();
    }

    private void PlayEmote()
    {
        if (_emoteData == null || _characters.Count == 0) return;

        bool isDouble = _characters.Count > 1;

        foreach (var wrap in _characters)
            wrap.Avatar.SetActive(true);

        if (avatarCamera != null)
        {
            avatarCamera.orthographicSize = isDouble ? doubleCameraSize : singleCameraSize;

            float cameraX = isDouble ? doubleCameraXOffset : 0f;

            if (_emoteData.UgcInfo is AnimInfo pgcCheck && string.IsNullOrEmpty(pgcCheck.metaDataUrl))
            {
                var emoConfig = DataTables.GetEmoUIConfig(pgcCheck.id);
                if (emoConfig != null)
                    cameraX += emoConfig.cameraPos.x;
            }

            _baseCameraX = cameraX;
            avatarCamera.transform.localPosition = new Vector3(
                _baseCameraX + _posOffset.x,
                _defaultCameraY + _posOffset.y,
                _defaultCameraZ + _scaleOffset
            );
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
