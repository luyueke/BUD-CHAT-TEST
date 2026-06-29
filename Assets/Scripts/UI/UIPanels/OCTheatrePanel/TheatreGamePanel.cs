using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Config;
using GameData.UGCData;
using System.Collections;
using System.Collections.Generic;
using GameData.Manager;
using Com.TheFallenGames.OSA.Util.IO;
using xasset;
using Newtonsoft.Json;
using System.Linq;
using DG.Tweening;
using Game.Audio;
using Game.Base;
using Game.MapSetting;
using GameSync.Manager;
using Game.Store;
using GameData.PgcData;
using Es;
using Message;
using Network;
using Network.Http;
using EventTracking;
using UI.Manager;
using UI.UIPanels.FittingRoom;

public class TheatreGamePanel : BasePanel<TheatreGamePanel>{
    private const float TheatreDataProgress = 0.75f;
    private const float ChangeBackgroundTime = 0.85f; //背景切换时间

    [Header("背景")]
    [SerializeField] private RemoteImageBehaviour bgImage; //当前背景
    [SerializeField] private RemoteImageBehaviour bgImage2; //等待背景

    [Header("按钮")]
    [SerializeField] private GameObject settingGroup;
    [SerializeField] private Button globalBtn; //页面中的背景按钮，触发下一句话的
    [SerializeField] private Button settingBtn;
    [SerializeField] private Button avatarSettingBtn;
    [SerializeField] private Button avatarCardBtn;
    [SerializeField] private Button bgSoundBtn; 
    [SerializeField] private Image bgSoundBtnImage; 
    [SerializeField] private Button closeBtn;
    [SerializeField] private Sprite openSound;
    [SerializeField] private Sprite closeSound;


    [Header("加载")]
    [SerializeField] private TheatreLoadingBoard loadingBoard;

    [Header("角色")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Camera avatarCamera;
    [SerializeField] private GameObject characterEmoteView;
    [SerializeField] private GameObject characterEmoteWindow;
    [SerializeField] private Button shrinkBtn;
    [SerializeField] private Button replayBtn;

    [Header("播放")]
    [SerializeField] private TheatreGamePlayBoard playBoard;

    [Header("系统")]
    [SerializeField] private TheatreClose theatreClose;
    [SerializeField] private TheatreSetting theatreSetting;
    // [SerializeField] private TheatreTtsPlayer ttsPlayer;    // TTS disabled
    // [SerializeField] private TheatreTTSEffector ttsEffector; // TTS disabled
    [SerializeField] private GameObject endPanel;
    [SerializeField] private Button endBtn;
    [SerializeField] private Text endText;
    [SerializeField] private Button retryBtn;
    [SerializeField] private GameObject buyBtnObject;
    [SerializeField] private Button buyButton;
    [SerializeField] private Text buyPriceText;
    [SerializeField] private Image priceCurrencyIcon;


    private const string ActorLineupKey = "OCTheatreActorLineup_";

    private bool isInit = false;
    private bool isOpenBGSound = true;
    private Vector3 _defaultCameraLocalPos;
    private OCTDetailInfoRuntime _runtimeInfo;
    private bool isShrunk = false;

    //剧本数据
    private TheatreEnterType enterType = TheatreEnterType.None;
    private int _editorStartSectionIndex = 1;
    private bool _isTryPlay = false;
    private int _tryDialogueCount = 0;
    private List<TheatreActorSaveItem> _roomPlayerSaveItems;
    private OCTheatreGameController controller;
    private OCTheatreInfo theatreInfo;
    //加载数据相关
    private Coroutine loadingCoroutine;
    private int loadingVersion;
    //游玩数据
    private string currentBgURL; //当前背景url
    private Coroutine changeBackgroundCoroutine;
    private RawImage currentBackgroundImage;
    private RawImage nextBackgroundImage;
    private bool isChangingBackground;
    private bool completeBackgroundImmediately;
    private bool isDialogueEnd = false;
    private bool isOptioning = false;
    private OCTheatreSection currentSection;
    private OCTheatreDialogue currentDialogue;
    private int nextSectionIndex = 0;
    private GameObject musicRoot;
    private MessageHandler _lineupChangedHandler;



    public override void OnCreate()
    {
        base.OnCreate();
        _lineupChangedHandler = () => StartCoroutine(RefreshActorLineupAsync());
        MessageHelper.AddListener(MessageName.OnTheatreActorLineupChanged, _lineupChangedHandler);
        if (avatarCamera != null)
            _defaultCameraLocalPos = avatarCamera.transform.localPosition;
        globalBtn.onClick.AddListener(OnGlobalBtnClick);
        settingBtn.onClick.AddListener(OnSettingBtnClick);
        avatarSettingBtn.onClick.AddListener(OnAvatarSettingBtnClick);
        avatarCardBtn.onClick.AddListener(OnAvatarCardBtnClick);
        bgSoundBtn.onClick.AddListener(OnBgSoundBtnClick);
        closeBtn.onClick.AddListener(OnCloseBtnClick);
        shrinkBtn.onClick.AddListener(OnShrinkBtnClick);
        replayBtn.onClick.AddListener(OnReplayBtnClick);

        characterEmoteView.SetActive(false);
        playBoard.gameObject.SetActive(false);
        playBoard.Init(OnOptionClick);
        playBoard.onDialogueCompleted = () => controller?.NotifyDialogueTextComplete();
        playBoard.gameObject.SetActive(false);
        settingGroup.gameObject.SetActive(false);

        currentBackgroundImage = GetBackgroundRawImage(bgImage);
        nextBackgroundImage = GetBackgroundRawImage(bgImage2);
        SetRawImageAlpha(currentBackgroundImage, 1f);
        SetRawImageAlpha(nextBackgroundImage, 0f);
        if (nextBackgroundImage != null)
        {
            nextBackgroundImage.rectTransform.SetAsFirstSibling();
        }
        theatreClose.Init(ConfirmClose);
        theatreSetting.Init(OnTypingChange, OnBGSoundChange);
        // theatreSetting.SetTtsPlayer(ttsPlayer);    // TTS disabled
        // ttsEffector?.SetTtsPlayer(ttsPlayer);      // TTS disabled
        theatreSetting.gameObject.SetActive(false);
        theatreClose.gameObject.SetActive(false);
        globalBtn.interactable = false;
        endPanel.gameObject.SetActive(false);
        endBtn.onClick.AddListener(ConfirmClose);
        retryBtn.onClick.AddListener(OnRetryBtnClick);
        buyButton.onClick.AddListener(OnBuyBtnClick);
        if(musicRoot == null){
            musicRoot = new GameObject("MusicRoot");
            musicRoot.transform.SetParent(transform);
            musicRoot.transform.localPosition = Vector3.zero;
            musicRoot.transform.localScale = Vector3.one;
            musicRoot.transform.localRotation = Quaternion.identity;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        loadingVersion++;
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        if (changeBackgroundCoroutine != null)
        {
            StopCoroutine(changeBackgroundCoroutine);
            changeBackgroundCoroutine = null;
        }
        MessageHelper.RemoveListener(MessageName.OnTheatreActorLineupChanged, _lineupChangedHandler);
        theatreSetting.OnRelease();
        controller?.Release();
        controller = null;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        isInit = false;
        enterType = TheatreEnterType.None;
        _editorStartSectionIndex = 1;
        if (args.Length > 1 && args[1] is int enterTypeArg)
            enterType = (TheatreEnterType)enterTypeArg;
        if (args.Length > 2 && args[2] is int startSection)
            _editorStartSectionIndex = startSection;
        _isTryPlay = false;
        _tryDialogueCount = 0;
        _roomPlayerSaveItems = null;
        if (args.Length > 3 && args[3] is List<TheatreActorSaveItem> roomItems)
            _roomPlayerSaveItems = roomItems;
        if (args.Length > 0 && args[0] is OCTheatreInfo info)
        {
            theatreInfo = info;
            if (enterType != TheatreEnterType.Share && enterType != TheatreEnterType.Room && enterType != TheatreEnterType.Editor)
            {
                bool isCreator = !string.IsNullOrEmpty(theatreInfo.creator)
                                 && theatreInfo.creator == AccountDataManager.Inst.Uid;
                bool isOwned = !string.IsNullOrEmpty(theatreInfo.id)
                               && AssetsDataManager.IsOwned(theatreInfo.id);
                _isTryPlay = !isCreator && !isOwned;
            }
            Init();
        }
        AkSoundManager.Inst.StopBGSound();
        if (GameController.IsGame())
            AkSoundManager.Inst.StopGameMusic();
        if (_isTryPlay)
        {
            LoadEvent.ReportTask(187, 1);  //试玩剧场
        }
        if(enterType == TheatreEnterType.Scene)
        {
            LoadEvent.ReportTask(188, 1);  //场景游玩剧场
        }
    }

    private void Init(){
        if(theatreInfo == null){
            LoggerUtils.LogError($"TheatreInfo is null, enterType: {enterType}");
            return;
        }
        if (characterRoot != null)
            characterRoot.localEulerAngles = Vector3.zero;
        controller = new OCTheatreGameController(theatreInfo.id, characterRoot, musicRoot);
        controller.DisableSave = _isTryPlay;
        controller.RegisterEvents(
            OnSectionChange,
            OnDialogueTrigger,
            OnEmoteTrigger,
            OnSoundTrigger,
            OnEmoteEnd,
            OnEnd
        );
        theatreSetting.SetMusicRoot(musicRoot);

        isInit = true;
        _tryDialogueCount = 0;
        bgImage.Load(theatreInfo.cover);
        bgImage2.Load(theatreInfo.cover);
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        loadingCoroutine = StartCoroutine(StartLoading(++loadingVersion));

    }

    private IEnumerator StartLoading(int version){
        loadingBoard?.Show("加载中剧场数据中...");
        loadingBoard?.ResetProgress(0f);
        yield return null;
        if (version != loadingVersion) yield break;

        OCTDetailInfoRuntime detailInfo = null;
        if (OCTheatreDataManager.Inst.TryGetCachedTheatreInfo(theatreInfo, out var cachedInfo))
        {
            detailInfo = cachedInfo;
            if (loadingBoard != null)
            {
                loadingBoard.TargetProgress = TheatreDataProgress;
                yield return loadingBoard.SmoothToTarget(() => version == loadingVersion);
            }
        }
        else if (theatreInfo?.detailInfoPb != null)
        {
            // theatreInfo 已携带 PB 数据，直接使用，无需 xasset 加载。
            detailInfo = OCTheatreDataManager.Inst.RegisterFromExistingPb(theatreInfo);
            if (detailInfo == null)
            {
                OnLoadingFailed($"RegisterFromExistingPb failed, theatreID: {theatreInfo?.id}");
                yield break;
            }
            if (loadingBoard != null)
            {
                loadingBoard.TargetProgress = TheatreDataProgress;
                yield return loadingBoard.SmoothToTarget(() => version == loadingVersion);
            }
        }
        else
        {
            var request = OCTheatreDataManager.Inst.LoadTheatreInfoRemoteRequest(theatreInfo);
            if (request == null)
            {
                OnLoadingFailed($"LoadTheatreInfoRequest failed, theatreID: {theatreInfo?.id}");
                yield break;
            }

            while (!request.isDone)
            {
                if (version != loadingVersion)
                {
                    request.Release();
                    yield break;
                }

                if (loadingBoard != null)
                {
                    loadingBoard.TargetProgress = request.progress * TheatreDataProgress;
                    loadingBoard.TickProgress();
                }
                yield return null;
            }

            if (request.result != xasset.Request.Result.Success || request.asset == null || request.asset.Length == 0)
            {
                var errMsg = string.IsNullOrEmpty(request.error) ? "request failed" : request.error;
                request.Release();
                OnLoadingFailed($"LoadTheatreInfo failed, theatreID: {theatreInfo.id}, error:{errMsg}");
                yield break;
            }

            detailInfo = OCTheatreDataManager.Inst.CacheLoadedTheatreInfo(theatreInfo, request.asset);
            request.Release();
            if (detailInfo == null)
            {
                OnLoadingFailed($"Parse theatre data failed, theatreID: {theatreInfo.id}");
                yield break;
            }

            if (loadingBoard != null)
            {
                loadingBoard.TargetProgress = TheatreDataProgress;
                yield return loadingBoard.SmoothToTarget(() => version == loadingVersion);
            }
        }

        controller.StoreOriginalActors(detailInfo.allAvatars);
        ApplyActorLineupIfExists(detailInfo);
        loadingBoard?.SetText("加载中角色数据中...");
        if (detailInfo.allAvatars != null && detailInfo.allAvatars.Count > 0)
            yield return StartCoroutine(FetchMissingActorInfoAsync(detailInfo.allAvatars));
        ClampAvatarClothesIndex(detailInfo.allAvatars);
        if (version != loadingVersion) yield break;

        bool characterLoaded = false;
        yield return StartCoroutine(OCTheatreDataManager.Inst.LoadCharacterInfoAsync(
            controller,
            detailInfo,
            progress =>
            {
                if (loadingBoard != null)
                {
                    loadingBoard.TargetProgress = TheatreDataProgress + progress * (1f - TheatreDataProgress);
                }
            },
            isLoaded => { characterLoaded = isLoaded; }));

        if (version != loadingVersion) yield break;

        if (!characterLoaded)
        {
            OnLoadingFailed($"LoadCharacterInfo failed, theatreID: {theatreInfo.id}");
            yield break;
        }

        PreloadEmoteClothesIndexes(detailInfo);

        // TTS disabled
        // if (ttsPlayer != null && TtsReflection.IsAvailable && !ttsPlayer.IsReady)
        // {
        //     loadingBoard?.SetText("加载语音合成...");
        //     float ttsWait = 0f;
        //     while (!ttsPlayer.IsReady && ttsWait < 10f && version == loadingVersion)
        //     {
        //         ttsWait += Time.deltaTime;
        //         yield return null;
        //     }
        // }
        if (version != loadingVersion) yield break;

        if (loadingBoard != null)
        {
            loadingBoard.TargetProgress = 1f;
            yield return loadingBoard.SmoothToTarget(() => version == loadingVersion);
            loadingBoard.Hide();
        }
        loadingCoroutine = null;
        globalBtn.interactable = true;

        settingGroup.gameObject.SetActive(true);
        avatarSettingBtn.gameObject.SetActive(enterType != TheatreEnterType.Room);

        //加载剧本数据到controller中
        _runtimeInfo = detailInfo;
        controller.LoadTheatreInfo(detailInfo);
        if (enterType == TheatreEnterType.Editor || enterType == TheatreEnterType.Room)
        {
            controller.LoadTheatreSave(_editorStartSectionIndex, new List<int>());
        }
        else if (!_isTryPlay && !OCTheatreDataManager.Inst.IsFirstTime(theatreInfo.id))
        {
            var savedProgress = OCTheatreDataManager.Inst.GetProgress(theatreInfo.id);
            var savedPaths = OCTheatreDataManager.Inst.GetPaths(theatreInfo.id);
            controller.LoadTheatreSave(savedProgress, savedPaths);
            var savedBgURL = OCTheatreDataManager.Inst.GetBackground(theatreInfo.id);
            if (!string.IsNullOrEmpty(savedBgURL))
                yield return StartCoroutine(ChangeBackground(savedBgURL));
        }
        controller.StartGame();
    }

    private void OnLoadingFailed(string error)
    {
        //LoggerUtils.LogError(error);
        Debug.LogError(error); //上报
        loadingBoard?.Hide();
        loadingCoroutine = null;
        TipPanel.ShowToast("加载失败，请重试");
        RestoreSceneOrHallBGM();
        CloseSelf();
    }

    private void OnGlobalBtnClick()
    {
        if(isOptioning){
            return;
        }

        if (isChangingBackground)
        {
            completeBackgroundImmediately = true;
            return;
        }

        if(isDialogueEnd){
            if (_isTryPlay && _tryDialogueCount >= 10)
            {
                if (playBoard.CanJumpNext())
                    OnEnd();
                return;
            }
            if (playBoard.CanJumpNext() && !controller.CheckEndGame()){
                controller.GoToNextSection(nextSectionIndex);
            }
            return;
        }

        if(playBoard.gameObject.activeSelf && playBoard.CanJumpNext()){
            globalBtn.interactable = false; //防止连续点击
            controller.NextDialogue(out isDialogueEnd);
            if (_isTryPlay && _tryDialogueCount >= 10)
                isDialogueEnd = true;
            return;
        }
    }

    private void OnOptionClick(int index){
        if (_isTryPlay && _tryDialogueCount >= 10) { OnEnd(); return; }
        if(currentDialogue.options == null || index >= currentDialogue.options.Count){
            LoggerUtils.LogError($"选项索引错误, index: {index}");
            return;
        }
        nextSectionIndex = currentDialogue.options[index].jumpIndex;
        isOptioning = false;
        isDialogueEnd = true;
        controller.GoToNextSection(nextSectionIndex);
        globalBtn.interactable = true;
    }

    private void OnSettingBtnClick()
    {
        theatreSetting.gameObject.SetActive(true);
    }

    private void OnAvatarSettingBtnClick()
    {
        if (enterType == TheatreEnterType.Room && _roomPlayerSaveItems != null)
            UIManager.Inst.OpenPanel(PanelId.TheatreGameSetPanel, _roomPlayerSaveItems);
        else
            UIManager.Inst.OpenPanel(PanelId.TheatreGameSetPanel);
    }

    private void OnAvatarCardBtnClick()
    {
        // Room 模式传入剧本原始演员列表，ActorCardInfoPanel 会直接使用而不走 PlayerPrefs
        var avatarList = enterType == TheatreEnterType.Room ? theatreInfo?.avatarList : null;
        UIManager.Inst.OpenPanel(PanelId.ActorCardInfoPanel, avatarList, true);
    }

    private void OnBgSoundBtnClick()
    {
        isOpenBGSound = !isOpenBGSound;
        bgSoundBtnImage.sprite = isOpenBGSound ? openSound : closeSound;
        theatreSetting.SetBGVolume(!isOpenBGSound);
    }

    private void OnTypingChange(bool isOn){
        playBoard.SetTypingEnabled(isOn);
    }

    private void OnBGSoundChange(bool isOn)
    {
        isOpenBGSound = isOn;
        bgSoundBtnImage.sprite = isOpenBGSound ? openSound : closeSound;
    }

    private void OnCloseBtnClick()
    {
        theatreClose.gameObject.SetActive(true);
    }

    private Tweener shrinkTween;
    private void OnShrinkBtnClick()
    {
        isShrunk = !isShrunk;
        if(shrinkTween != null){
            shrinkTween.Kill();
            shrinkTween = null;
        }
        //0.4秒缩放动画，线性变化
        shrinkTween = characterEmoteWindow.transform.DOScale(isShrunk ? 0f : 1f, 0.2f).SetEase(Ease.Linear);
    }

    private void OnReplayBtnClick()
    {
        controller.PlayCurrentEmoteAgain();
    }

    private void OnRetryBtnClick()
    {
        if (theatreInfo == null) return;

        // 清除本局存档进度
        OCTheatreDataManager.Inst.ClearProgress(theatreInfo.id);

        // 停掉旧 controller 的 BGM，清理旧 Avatar，重新建 controller
        controller?.Release();
        controller?.StopBgMusic();
        for (int i = characterRoot.childCount - 1; i >= 0; i--)
            GameObject.Destroy(characterRoot.GetChild(i).gameObject);
        avatarCamera?.transform.DOLocalMove(_defaultCameraLocalPos, 0.5f);
        controller = new OCTheatreGameController(theatreInfo.id, characterRoot, musicRoot);
        controller.DisableSave = _isTryPlay;
        controller.RegisterEvents(
            OnSectionChange,
            OnDialogueTrigger,
            OnEmoteTrigger,
            OnSoundTrigger,
            OnEmoteEnd,
            OnEnd
        );

        // 停止背景切换协程
        if (changeBackgroundCoroutine != null)
        {
            StopCoroutine(changeBackgroundCoroutine);
            changeBackgroundCoroutine = null;
        }
        isChangingBackground = false;
        completeBackgroundImmediately = false;

        // 重置 UI 状态
        _tryDialogueCount = 0;
        endPanel.gameObject.SetActive(false);
        endBtn.interactable = true;
        globalBtn.interactable = true;
        characterEmoteView.SetActive(false);
        playBoard.gameObject.SetActive(false);
        currentBgURL = string.Empty;
        isDialogueEnd = false;
        isOptioning = false;

        // 直接从缓存拿 detailInfo（Editor 模式 id="" 时走 PB fallback）
        OCTDetailInfoRuntime detailInfo = null;
        if (!OCTheatreDataManager.Inst.TryGetCachedTheatreInfo(theatreInfo, out detailInfo)
            && theatreInfo?.detailInfoPb != null)
        {
            detailInfo = OCTheatreDataManager.Inst.RegisterFromExistingPb(theatreInfo);
        }

        if (detailInfo != null)
        {
            controller.StoreOriginalActors(detailInfo.allAvatars);
            ApplyActorLineupIfExists(detailInfo);
            ClampAvatarClothesIndex(detailInfo.allAvatars);
            controller.LoadTheatreInfo(detailInfo);
            OCTheatreDataManager.Inst.LoadCharacterInfo(controller, detailInfo, _ => { });
            PreloadEmoteClothesIndexes(detailInfo);
            controller.StartGame();
        }
    }

    #region 游戏事件回调

    private void OnSectionChange(OCTheatreGamePack<OCTheatreSection> pack){
        if (pack.data == null) return;

        isDialogueEnd = false;

        if (string.IsNullOrEmpty(pack.data.backgroundURL) || currentBgURL == pack.data.backgroundURL)
        {
            SetSectionData(pack.data);
        }else{
            if (changeBackgroundCoroutine != null)
            {
                StopCoroutine(changeBackgroundCoroutine);
            }
            currentSection = pack.data;
            changeBackgroundCoroutine = StartCoroutine(ChangeBackground(pack.data.backgroundURL));
        }


    }

    private void SetSectionData(OCTheatreSection section){
        if (!string.IsNullOrEmpty(section.backgroundURL))
            currentBgURL = section.backgroundURL;
        currentSection = section;
        isOptioning = false;
        characterEmoteView.SetActive(false);
        avatarCamera?.transform.DOLocalMove(_defaultCameraLocalPos, 0.5f);
        if(section.dialogues.Count > 0){
            playBoard.gameObject.SetActive(true);
            bool isSectionNarrator = string.IsNullOrEmpty(section.avatarID) || section.avatarID == "1";
            playBoard.SetAvatarVisible(!isSectionNarrator);
            if (!isSectionNarrator)
            {
                var (avatarName, avatarUrl) = GetAvatarDisplayInfo(section.avatarID, section.avatarType);
                playBoard.SetAvatar(avatarName, avatarUrl, 0);
            }
            playBoard.PlayEnterAnimation();
            currentDialogue = section.dialogues[0];
            playBoard.SetDialogue(section.dialogues[0].text);
            // ttsPlayer?.Speak(section.dialogues[0].text); // TTS disabled
            isDialogueEnd = section.dialogues.Count == 1 && section.dialogues[0].type == 1;
            nextSectionIndex = section.nextIndex;
            if(section.dialogues.Count == 1 && section.dialogues[0].type == 1 && section.nextIndex == 0){
                isDialogueEnd = true;
                isOptioning = false;
            }else if(section.dialogues[0].type == 2 && section.dialogues[0].options.Count > 0){
                isOptioning = true;
                isDialogueEnd = false;
                playBoard.SetOptions(section.dialogues[0].options.Select(option => option.text).ToList());
            }
            // 试玩计数：每个 section 的第一句也算入限制
            if (_isTryPlay)
            {
                _tryDialogueCount++;
                if (_tryDialogueCount >= 10)
                {
                    isDialogueEnd = true;
                    // 不强制 isOptioning=false：选项句让玩家完成第10次选择，OnOptionClick 在 count>=10 时调 OnEnd()
                }
            }
        }
    }

    private void OnDialogueTrigger(OCTheatreGamePack<OCTheatreDialogue> pack){
        playBoard.gameObject.SetActive(true);
        currentDialogue = pack.data;
        playBoard.SetDialogue(pack.data.text);
        // ttsPlayer?.Speak(pack.data.text); // TTS disabled
        globalBtn.interactable = true;
        if(pack.data.type == 1 && currentSection.nextIndex == 0){
            isDialogueEnd = true;
            isOptioning = false;
        }else if(pack.data.type == 2 && pack.data.options.Count > 0){
            isOptioning = true;
            isDialogueEnd = false;
            playBoard.SetOptions(pack.data.options.Select(option => option.text).ToList());
        }
        // Trial limit applied last so it can override the type=2 branch above.
        if (_isTryPlay)
        {
            _tryDialogueCount++;
            if (_tryDialogueCount >= 10)
            {
                isDialogueEnd = true;
                // 不强制 isOptioning=false：选项句让玩家完成第10次选择，OnOptionClick 在 count>=10 时调 OnEnd()
            }
        }
    }

    private void OnEmoteTrigger(OCTheatreGamePack<OCTheatreEmote> pack){
        characterEmoteView.SetActive(true);
        isShrunk = false;
        if(shrinkTween != null){
            shrinkTween.Kill();
            shrinkTween = null;
        }
        characterEmoteWindow.transform.localScale = Vector3.one;

        if (pack.data != null)
        {
            var emote = pack.data;
            bool isPgc = int.TryParse(emote.emoteID, out _);
            bool isDouble = emote.emotePlayers != null && emote.emotePlayers.Count > 1;

            if (avatarCamera != null)
            {
                if (isPgc)
                {
                    var uiConfig = DataTables.GetEmoUIConfig(emote.emoteID);
                    float targetX = (uiConfig != null ? uiConfig.cameraPos.x : 0f) + emote.customPosition.x;
                    var targetPos = new Vector3(
                        targetX,
                        _defaultCameraLocalPos.y + emote.customPosition.y,
                        _defaultCameraLocalPos.z + emote.scale);
                    avatarCamera.transform.DOLocalMove(targetPos, 0.5f);
                }
                else
                {
                    avatarCamera.transform.DOKill();
                    avatarCamera.transform.localPosition = new Vector3(
                        (isDouble ? -25f : 0f) + emote.customPosition.x,
                        _defaultCameraLocalPos.y + emote.customPosition.y,
                        _defaultCameraLocalPos.z + emote.scale);
                }
            }

            if (characterRoot != null)
            {
                if (isPgc)
                {
                    var baseRot = isDouble ? new Vector3(0, -90, 0) : Vector3.zero;
                    characterRoot.DOKill();
                    characterRoot.localEulerAngles = new Vector3(
                        baseRot.x + emote.customRotation.x,
                        baseRot.y + emote.customRotation.y,
                        0f);
                    characterRoot.DOLocalMoveX(isDouble ? 50f : 0f, 0.5f);
                }
                else
                {
                    characterRoot.DOKill();
                    characterRoot.localEulerAngles = new Vector3(emote.customRotation.x, emote.customRotation.y, 0f);
                    characterRoot.localPosition = new Vector3(0f, characterRoot.localPosition.y, characterRoot.localPosition.z);
                }
            }
        }
    }

    private void OnSoundTrigger(OCTheatreGamePack<OCTheatreAudio> pack){
        
    }

    private void OnEmoteEnd()
    {
        //characterEmoteView.SetActive(false);
    }

    private void OnEnd()
    {
        endPanel.gameObject.SetActive(true);
        endBtn.interactable = true;
        globalBtn.interactable = false;
        characterEmoteView.gameObject.SetActive(false);
        playBoard.gameObject.SetActive(false);
        if (endText != null)
            endText.text = _isTryPlay ? "试玩结束" : "剧  终";
        if (!_isTryPlay)
        {
            EventTracking.LoadEvent.ReportTask(189, 1);
        }

        var showBuy = _isTryPlay && theatreInfo?.paymentInfo != null && theatreInfo.paymentInfo.price > 0;
        if (buyBtnObject != null)
            buyBtnObject.SetActive(showBuy);
        if (showBuy)
        {
            if (buyPriceText != null)
                buyPriceText.text = CalcDisplayPrice(theatreInfo.paymentInfo).ToString();
            if (priceCurrencyIcon != null)
                priceCurrencyIcon.sprite = PgcUtils.LoadCurrencyIcon(theatreInfo.paymentInfo.currencyType, gameObject);
        }
    }

    private void OnBuyBtnClick()
    {
        if (theatreInfo?.paymentInfo == null) return;
        var currencyType = theatreInfo.paymentInfo.currencyType;
        var price = CalcDisplayPrice(theatreInfo.paymentInfo);

        var confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        confirmPanel.SetText("购买确认", $"确认花费 {price} 购买该剧本吗？", "确认购买", "取消");
        confirmPanel.SetOnClickAction(() =>
        {
            AssetsDataManager.BuyUgc(theatreInfo.id, currencyType, price, (success, reason, needNum) =>
            {
                if (!success)
                {
                    if (reason.Equals("余额不足"))
                    {
                        switch (currencyType)
                        {
                            case CurrencyType.Coin:
                                var p1 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                p1.SetData(CurrencyType.Coin, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.Badge:
                                var p2 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                p2.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                                break;
                            case CurrencyType.PinkCoin:
                                if (ExchangeCoinPanel.JudgePinkCoin(needNum))
                                {
                                    var p3 = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                                    p3.SetData(CurrencyType.PinkCoin, CurrencyType.Gem, needNum);
                                }
                                break;
                            case CurrencyType.Gem:
                                UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                                break;
                        }
                    }
                }
                else
                {
                    var successPanel = UIManager.Inst.OpenPanel<BuySuccessTipPanel>(PanelId.BuySuccessTipPanel);
                    successPanel?.InitData(theatreInfo, "购买成功！");
                    MessageHelper.Broadcast(MessageName.OnBuyUgcItemSuccess, theatreInfo.id);
                    if (buyBtnObject != null)
                        buyBtnObject.SetActive(false);
                }
            });
        });
    }

    #endregion

    private void ApplyRoomPlayersToRuntime(OCTDetailInfoRuntime detailInfo)
    {
        if (_roomPlayerSaveItems == null || _roomPlayerSaveItems.Count == 0) return;
        if (detailInfo.allAvatars == null || detailInfo.allAvatars.Count == 0) return;

        // Build override map using siblingIndex as the direct slot index into allAvatars.
        // Positional mapping after filtering is wrong when some slots are unassigned.
        var overrideMap = new Dictionary<string, (string newPlayerId, string newName, string newURL, int newClothesIndex)>();
        foreach (var saved in _roomPlayerSaveItems)
        {
            if (string.IsNullOrEmpty(saved.playerId)) continue;
            int idx = saved.siblingIndex;
            if (idx < 0 || idx >= detailInfo.allAvatars.Count) continue;
            var original = detailInfo.allAvatars[idx];
            if (saved.playerId == original.playerId) continue;
            if (!overrideMap.ContainsKey(original.playerId))
                overrideMap[original.playerId] = (saved.playerId, saved.avatarName, saved.avatarURL, saved.clothesIndex >= 0 ? saved.clothesIndex : 0);
        }
        if (overrideMap.Count == 0) return;

        foreach (var avatar in detailInfo.allAvatars)
        {
            if (overrideMap.TryGetValue(avatar.playerId, out var rep))
            {
                avatar.playerId = rep.newPlayerId;
                avatar.avatarName = rep.newName;
                avatar.avatarURL = rep.newURL;
                avatar.clothesIndex = rep.newClothesIndex;
            }
        }

        if (detailInfo.sections == null) return;
        foreach (var section in detailInfo.sections.Values)
        {
            if (!string.IsNullOrEmpty(section.avatarID) && overrideMap.TryGetValue(section.avatarID, out var sRep))
            {
                section.avatarID = sRep.newPlayerId;
                section.avatarType = "常态";
            }
            if (section.emote?.emotePlayers == null) continue;
            foreach (var player in section.emote.emotePlayers)
            {
                if (overrideMap.TryGetValue(player.playerId, out var eRep))
                {
                    player.playerId = eRep.newPlayerId;
                    player.clothesIndex = eRep.newClothesIndex;
                }
            }
        }
    }

    private void InjectRoomPlayerAvatarsToDict()
    {
        if (_roomPlayerSaveItems == null) return;
        var inRoomPlayers = TheatreGameManager.Inst.InRoomPlayers;
        foreach (var saved in _roomPlayerSaveItems)
        {
            if (string.IsNullOrEmpty(saved.playerId)) continue;
            // Only inject real room-player UIDs. Original OC actor IDs (unswapped slots) must
            // NOT be blocked here — FetchMissingActorInfoAsync will fetch their full actor info
            // from the /ugc/actor/info API (clothesJson + expressions). Injecting a stub entry
            // for them would prevent that fetch and leave their 3D models unloaded.
            if (inRoomPlayers == null || !inRoomPlayers.Contains(saved.playerId)) continue;
            if (OCTheatreDataManager.Inst.ocCharacterInfoDict.ContainsKey(saved.playerId)) continue;

            // Local player is not in PlayerInfosManager.PlayerInfos (only remote players are).
            // Use the account UserInfo for self; fall back to protobuf PlayerInfo for others.
            string avatarJson;
            if (saved.playerId == AccountDataManager.Inst.Uid)
                avatarJson = AccountDataManager.Inst.UserInfo?.avatarJson;
            else
            {
                var playerInfo = ClientManager.Inst.PlayerInfosManager.GetPlayerInfoById(saved.playerId);
                avatarJson = playerInfo?.AvatarJson; // Pb.Base.PlayerInfo.AvatarJson, populated on OnPlayerEnter
            }

            // Always insert an entry so FetchMissingActorInfoAsync skips the /ugc/actor/info
            // API call (room player UIDs are not OC actors — the call would always 404).
            //
            // Populate name + expressions so GetAvatarDisplayInfo returns the player's nickname
            // and portrait correctly. Populate avatarClothes for 3D rendering when available.
            var clothesList = string.IsNullOrEmpty(avatarJson)
                ? new List<OTCAvatarClothes>()
                : new List<OTCAvatarClothes>
                {
                    new OTCAvatarClothes { clothesIndex = 0, clothesJson = avatarJson, isDef = 1 }
                };

            var expressionsList = string.IsNullOrEmpty(saved.avatarURL)
                ? null
                : new List<OCTAvatarExpression>
                {
                    new OCTAvatarExpression { expressionURL = saved.avatarURL }
                };

            OCTheatreDataManager.Inst.ocCharacterInfoDict[saved.playerId] = new OCTheatreAvatarInfo
            {
                id = saved.playerId,
                name = saved.avatarName,
                expressions = expressionsList,
                avatarClothes = clothesList
            };
        }
    }

    private void ApplyActorLineupIfExists(OCTDetailInfoRuntime detailInfo)
    {
        if (detailInfo == null || string.IsNullOrEmpty(theatreInfo?.id)) return;

        if (enterType == TheatreEnterType.Room)
        {
            ApplyRoomPlayersToRuntime(detailInfo);
            InjectRoomPlayerAvatarsToDict();
            return;
        }

        string json = PlayerPrefs.GetString(ActorLineupKey + theatreInfo.id, string.Empty);
        if (string.IsNullOrEmpty(json)) return;

        TheatreGameSetPanelSaveData savedData;
        try { savedData = JsonConvert.DeserializeObject<TheatreGameSetPanelSaveData>(json); }
        catch { return; }

        if (savedData?.currentPanelActors == null || savedData.currentPanelActors.Count == 0) return;
        if (detailInfo.allAvatars == null || detailInfo.allAvatars.Count == 0) return;

        var savedActors = savedData.currentPanelActors
            .Where(a => !string.IsNullOrEmpty(a.playerId))
            .OrderBy(a => a.siblingIndex)
            .ToList();

        int count = Mathf.Min(savedActors.Count, detailInfo.allAvatars.Count);

        // 构建 originalPlayerId → replacement 的映射
        var overrideMap = new Dictionary<string, (string newPlayerId, string newName, string newURL, int newClothesIndex)>();
        for (int i = 0; i < count; i++)
        {
            var original = detailInfo.allAvatars[i];
            var saved = savedActors[i];
            if (string.IsNullOrEmpty(saved.playerId)) continue;
            if (saved.playerId == original.playerId)
            {
                if (saved.clothesIndex >= 0 && saved.clothesIndex != original.clothesIndex)
                    original.clothesIndex = saved.clothesIndex;
                continue;
            }
            if (!overrideMap.ContainsKey(original.playerId))
            {
                int clothesIdx = saved.clothesIndex >= 0 ? saved.clothesIndex : 0;
                overrideMap[original.playerId] = (saved.playerId, saved.avatarName, saved.avatarURL, clothesIdx);
            }
        }

        if (overrideMap.Count == 0) return;

        // 更新 allAvatars（FetchMissing 和 LoadCharacter 均依赖此列表）
        for (int i = 0; i < count; i++)
        {
            var avatar = detailInfo.allAvatars[i];
            if (overrideMap.TryGetValue(avatar.playerId, out var rep))
            {
                avatar.playerId = rep.newPlayerId;
                avatar.avatarName = rep.newName;
                avatar.avatarURL = rep.newURL;
                avatar.clothesIndex = rep.newClothesIndex;
            }
        }

        // 更新所有 section 的 avatarID 及表情演者
        if (detailInfo.sections == null) return;
        foreach (var section in detailInfo.sections.Values)
        {
            if (!string.IsNullOrEmpty(section.avatarID) && overrideMap.TryGetValue(section.avatarID, out var sRep))
            {
                section.avatarID = sRep.newPlayerId;
                section.avatarType = "常态";
            }

            if (section.emote?.emotePlayers == null) continue;
            foreach (var player in section.emote.emotePlayers)
            {
                if (overrideMap.TryGetValue(player.playerId, out var eRep))
                {
                    player.playerId = eRep.newPlayerId;
                    player.clothesIndex = eRep.newClothesIndex;
                }
            }
        }
    }

    private void PreloadEmoteClothesIndexes(OCTDetailInfoRuntime runtime)
    {
        if (runtime?.sections == null || controller == null) return;
        var dict = OCTheatreDataManager.Inst.ocCharacterInfoDict;
        foreach (var section in runtime.sections.Values)
        {
            if (section.emote?.emotePlayers == null) continue;
            foreach (var player in section.emote.emotePlayers)
            {
                if (string.IsNullOrEmpty(player.playerId)) continue;
                if (!dict.TryGetValue(player.playerId, out var info)) continue;
                if (info.avatarClothes == null) continue;
                var emoteClothes = info.avatarClothes.Find(c => c.clothesIndex == player.clothesIndex);
                if (emoteClothes == null) continue;
                controller.LoadCharacter(player.playerId, player.clothesIndex, emoteClothes.clothesJson);
            }
        }
    }

    private void ClampAvatarClothesIndex(List<OCTheatreAvatarOc> avatars)
    {
        if (avatars == null) return;
        foreach (var avatar in avatars)
        {
            if (!OCTheatreDataManager.Inst.ocCharacterInfoDict.TryGetValue(avatar.playerId, out var info)) continue;
            if (info.avatarClothes == null || info.avatarClothes.Count == 0) continue;
            // Validate that the stored clothesIndex actually exists in the list.
            // Clamping by count is wrong when clothesIndex values are non-sequential.
            if (!info.avatarClothes.Any(c => c.clothesIndex == avatar.clothesIndex))
            {
                var fallback = info.avatarClothes.Find(c => c.isDef == 1) ?? info.avatarClothes[0];
                avatar.clothesIndex = fallback.clothesIndex;
            }
        }
    }

    private IEnumerator RefreshActorLineupAsync()
    {
        OCTDetailInfoRuntime freshRuntime = null;
        if (theatreInfo?.detailInfoPb != null)
            freshRuntime = theatreInfo.GetDetailInfoRuntime();
        if (freshRuntime == null)
            OCTheatreDataManager.Inst.TryGetCachedTheatreInfo(theatreInfo, out freshRuntime);
        if (freshRuntime == null) yield break;

        ApplyActorLineupIfExists(freshRuntime);
        if (freshRuntime.allAvatars != null && freshRuntime.allAvatars.Count > 0)
            yield return StartCoroutine(FetchMissingActorInfoAsync(freshRuntime.allAvatars));

        ClampAvatarClothesIndex(freshRuntime.allAvatars);
        OCTheatreDataManager.Inst.LoadCharacterInfo(controller, freshRuntime, _ => { });
        PreloadEmoteClothesIndexes(freshRuntime);
        _runtimeInfo = freshRuntime;
        controller?.LoadTheatreInfo(freshRuntime);

        if (currentSection != null &&
            freshRuntime.sections.TryGetValue(currentSection.sectionIndex, out var updatedSection))
        {
            currentSection = updatedSection;
            bool isUpdatedNarrator = string.IsNullOrEmpty(updatedSection.avatarID) || updatedSection.avatarID == "1";
            playBoard.SetAvatarVisible(!isUpdatedNarrator);
            if (!isUpdatedNarrator)
            {
                var (avatarName, avatarUrl) = GetAvatarDisplayInfo(updatedSection.avatarID, updatedSection.avatarType);
                playBoard.SetAvatar(avatarName, avatarUrl, 0);
            }
        }
    }

    private IEnumerator FetchMissingActorInfoAsync(List<OCTheatreAvatarOc> avatars)
    {
        int pending = 0;
        foreach (var avatar in avatars)
        {
            if (string.IsNullOrEmpty(avatar.playerId)) continue;
            if (OCTheatreDataManager.Inst.ocCharacterInfoDict.ContainsKey(avatar.playerId)) continue;
            pending++;
            var id = avatar.playerId;
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.ActorInfo, HttpMethod.GET,
                JsonConvert.SerializeObject(new { id }),
                content =>
                {
                    var rsp = JsonConvert.DeserializeObject<DetailRsp>(content);
                    if (rsp?.actorInfo != null)
                        OCTheatreDataManager.Inst.ocCharacterInfoDict[id] = rsp.actorInfo;
                    pending--;
                },
                _ => pending--);
        }

        float elapsed = 0f;
        while (pending > 0 && elapsed < 8f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ChangeBackground(string targetBgUrl)
    {
        isChangingBackground = true;
        completeBackgroundImmediately = false;
        characterEmoteView.SetActive(false);
        playBoard.gameObject.SetActive(false);

        if (string.IsNullOrEmpty(targetBgUrl))
        {
            currentBgURL = targetBgUrl;
            EndBackgroundChange();
            yield break;
        }

        var wrapper = Loader.LoadRemoteImageAsync(targetBgUrl);
        if (wrapper == null || wrapper.request == null)
        {
            LoggerUtils.LogError($"Load background failed, url={targetBgUrl}");
            EndBackgroundChange();
            yield break;
        }

        while (!wrapper.request.isDone)
        {
            if (completeBackgroundImmediately)
            {
                wrapper.request.WaitForCompletion();
                break;
            }
            yield return null;
        }

        if (wrapper.request.result != Request.Result.Success)
        {
            LoggerUtils.LogError($"Load background failed, url={targetBgUrl}, error={wrapper.request.error}");
            EndBackgroundChange();
            yield break;
        }

        var texture = wrapper.RetainAsset(gameObject);
        if (texture == null)
        {
            LoggerUtils.LogError($"Load background texture null, url={targetBgUrl}");
            EndBackgroundChange();
            yield break;
        }

        if (string.IsNullOrEmpty(currentBgURL) || currentBackgroundImage.texture == null)
        {
            currentBackgroundImage.texture = texture;
            currentBgURL = targetBgUrl;
            if (enterType != TheatreEnterType.Room && !_isTryPlay)
                OCTheatreDataManager.Inst.SaveBackground(theatreInfo.id, targetBgUrl);
            SetRawImageAlpha(currentBackgroundImage, 1f);
            SetRawImageAlpha(nextBackgroundImage, 0f);
            EndBackgroundChange();
            if (currentSection != null) SetSectionData(currentSection);
            yield break;
        }

        nextBackgroundImage.texture = texture;
        SetRawImageAlpha(nextBackgroundImage, 0f);
        int currentBackgroundIndex = currentBackgroundImage.rectTransform.GetSiblingIndex();
        nextBackgroundImage.rectTransform.SetSiblingIndex(currentBackgroundIndex);  

        float elapsed = 0f;
        while (elapsed < ChangeBackgroundTime)
        {
            if (completeBackgroundImmediately)
            {
                elapsed = ChangeBackgroundTime;
                break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / ChangeBackgroundTime);
            SetRawImageAlpha(nextBackgroundImage, progress);
            yield return null;
        }

        SetRawImageAlpha(nextBackgroundImage, 1f);
        SetRawImageAlpha(currentBackgroundImage, 0f);
        currentBgURL = targetBgUrl;
        if (enterType != TheatreEnterType.Room && !_isTryPlay)
            OCTheatreDataManager.Inst.SaveBackground(theatreInfo.id, targetBgUrl);

        var previousCurrent = currentBackgroundImage;
        currentBackgroundImage = nextBackgroundImage;
        nextBackgroundImage = previousCurrent;
        SetRawImageAlpha(nextBackgroundImage, 0f);
        nextBackgroundImage.rectTransform.SetAsFirstSibling();

        EndBackgroundChange();
        yield return new WaitForSeconds(0.5f);
        if (currentSection != null) SetSectionData(currentSection); //重新设置当前段落数据
    }

    private void EndBackgroundChange()
    {
        isChangingBackground = false;
        completeBackgroundImmediately = false;
        changeBackgroundCoroutine = null;
    }

    private static void SetRawImageAlpha(RawImage rawImage, float alpha)
    {
        if (rawImage == null) return;
        var color = rawImage.color;
        color.a = Mathf.Clamp01(alpha);
        rawImage.color = color;
    }

    private static RawImage GetBackgroundRawImage(RemoteImageBehaviour imageBehaviour)
    {
        if (imageBehaviour == null) return null;
        return imageBehaviour.RawImage != null ? imageBehaviour.RawImage : imageBehaviour.GetComponent<RawImage>();
    }

    private (string, string) GetAvatarDisplayInfo(string avatarID, string avatarType)
    {
        if (OCTheatreDataManager.Inst.ocCharacterInfoDict.TryGetValue(avatarID, out var avatarInfo))
        {
            string url = string.Empty;
            if (avatarInfo.expressions != null)
            {
                int sep = (avatarType ?? "").IndexOf('|');
                string targetName = sep >= 0 ? avatarType.Substring(0, sep) : avatarType ?? "";
                int targetMType = (sep >= 0 && int.TryParse(avatarType.Substring(sep + 1), out int m)) ? m : -1;

                foreach (var expr in avatarInfo.expressions)
                {
                    if (expr.expressionName == targetName
                        && (targetMType < 0 || expr.mType == targetMType)
                        && !string.IsNullOrEmpty(expr.expressionURL))
                    { url = expr.expressionURL; break; }
                }
                if (string.IsNullOrEmpty(url) && avatarInfo.expressions.Count > 0)
                    url = avatarInfo.expressions[0].expressionURL ?? string.Empty;
            }
            return (avatarInfo.name, url);
        }
        var runtimeAvatar = _runtimeInfo?.allAvatars?.Find(a => a.playerId == avatarID);
        if (runtimeAvatar != null)
            return (runtimeAvatar.avatarName, runtimeAvatar.avatarURL);
        return (avatarID, string.Empty);
    }

    private void ConfirmClose()
    {
        // ttsPlayer?.StopSpeaking(); // TTS disabled
        CloseSelf();
        RestoreSceneOrHallBGM();
    }

    private void RestoreSceneOrHallBGM()
    {
        if (GameController.IsGame() && BgMusicManager.HasInstance)
            BgMusicManager.Inst.PlayAllMusic();
        else if (!GameController.IsGame() && enterType != TheatreEnterType.Editor)
            AkSoundManager.Inst.PlayBGSound();
    }

    private static int CalcDisplayPrice(PaymentInfo paymentInfo)
    {
        var price = paymentInfo.price;
        if (paymentInfo.currencyType == CurrencyType.PinkCoin && AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive())
            price = (int)Mathf.Ceil(price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
        return price;
    }

}