using System;
using System.Collections.Generic;
using System.IO;
using Game.Audio;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Pb.Theatre;
using UI.Base;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorPanel : BasePanel<TheatreEditorPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private Text EditTitle;
    [SerializeField] private TheatreEditorSetting setting;
    [SerializeField] private TheatreEditorLeftMenu leftMenu;
    [SerializeField] private TheatreEditorMainList mainList;
    [SerializeField] private TheatreEditorBGSelector bgSelector;

    [SerializeField] private TheatreEditorMainEdit mainEdit;
    [SerializeField] private TheatreEditorSectionEdit sectionEdit;
    [SerializeField] private TheatreEditorAvatarEdit avatarEdit;
    [SerializeField] private TheatreEditorAvatarExpressEdit avatarExpressEdit;
    [SerializeField] private TheatreEditorEmoteEdit emoteEdit;
    [SerializeField] private TheatreEditorOptionEdit optionEdit;
    [SerializeField] private TheatreEditorMusicEdit musicEdit;
    [SerializeField] private TheatreEditorAvatarSelector avatarSelector;
    [SerializeField] private TheatreEditorEmoteShowcase emoteShowcase;
    [SerializeField] private TheatreClose closePanel;
    [SerializeField] private Button EditBackBtn;
    [SerializeField] private TheatreEditorSearchBar searchBar;
    [SerializeField] private GameObject editTitleGObj;
    [SerializeField] private TheatreEditorBusinessView buyView;
    [SerializeField] private TheatreEditorSearchSection searchSection;
    [SerializeField] private TheatreEditorJumpWindow jumpWindow;


    private const string TAG = "Theater";

    private TheatreEditorDataCenter dataCenter;
    private OCTheatreDraftInfo _draftInfo;
    private EditorPageType currentPage = EditorPageType.Main;
    private BudTimer _autoSaveTimer;
    private bool _suppressToggleNav;

    public TheatreEditorDataCenter DataCenter => dataCenter;

    public override void OnCreate()
    {
        InitBG();
    }

    public override void OnShow(params object[] args)
    {
        AkSoundManager.Inst.StopBGSound();
        dataCenter = new TheatreEditorDataCenter();
        dataCenter.Init();

        OCTheatreInfo theatreInfo = null;
        if (args != null && args.Length > 0)
            theatreInfo = args[0] as OCTheatreInfo;
        theatreInfo ??= new OCTheatreInfo();

        dataCenter.InitLoad(theatreInfo);
        _draftInfo = new OCTheatreDraftInfo(dataCenter.TheatreInfo);

        leftMenu?.OnShow(dataCenter);
        mainList?.OnShow(dataCenter);

        if (sectionEdit != null) sectionEdit.Panel = this;
        if (optionEdit != null) optionEdit.Panel = this;
        if (avatarExpressEdit != null) avatarExpressEdit.Panel = this;
        if (musicEdit != null) musicEdit.Panel = this;
        if (emoteEdit != null) emoteEdit.Panel = this;

        if (leftMenu != null)
        {
            leftMenu.OnEditorToggleChanged = OnEditorToggleChanged;
            leftMenu.OnAvatarToggleChanged = OnAvatarToggleChanged;
            leftMenu.OnCreateClicked = OnCreateSectionClicked;
            leftMenu.OnDeleteClicked = OnDeleteSectionClicked;
        }

        mainList?.SetOnSectionSelected(OnSectionSelected);
        mainList?.SetOnJumpWindowRequested(OnJumpWindowRequested);
        mainList?.SetOnPlayClicked(ShowPlay);
        dataCenter.OnSectionCreated += OnSectionCreatedHandler;
        dataCenter.OnDataChanged += OnDataCenterChanged;
        dataCenter.OnDataChanged += ScheduleAutoSave;
        dataCenter.OnDialogueAdded += AutoSaveDraft;

        searchSection?.Init(dataCenter, mainList, OnSectionSelected);

        closePanel?.Init(OnSaveAndClose, OnCloseConfirmed);
        setting?.OnInit(OnSettingExit, OnSettingSave);

        EditBackBtn?.onClick.RemoveAllListeners();
        EditBackBtn?.onClick.AddListener(OnEditBackClicked);

        ShowPage(EditorPageType.Main);

        avatarEdit?.PreloadOwned(dataCenter, () =>
        {
            if (dataCenter?.SectionList == null) return;
            foreach (var section in dataCenter.SectionList)
                mainList?.RefreshSectionItem(section);
            if (currentPage == EditorPageType.Section)
                sectionEdit?.OnShow(dataCenter);
        });

        GameTimeUtils.Inst.StartCollect(TAG);
    }

    private void InitBG()
    {
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#ffffff00", atlasPath, new List<string>()
        {
            "theatre_editor_icon1", "theatre_editor_icon2","theatre_editor_icon3", "theatre_editor_icon4"
        });
        item.gameObject.SetActive(true);
    }

    private void OnEditorToggleChanged(bool isOn)
    {
        if (!isOn || _suppressToggleNav) return;
        ShowPage(EditorPageType.Main);
    }

    private void OnAvatarToggleChanged(bool isOn)
    {
        if (isOn && !_suppressToggleNav) ShowPage(EditorPageType.Avatar);
    }

    private void OnCreateSectionClicked() => dataCenter.AddNewSection();

    private void OnDeleteSectionClicked()
    {
        if (dataCenter.currentSelected != null)
            dataCenter.RemoveSection(dataCenter.currentSelected);
    }

    private void OnSectionSelected(POCTheatreSection section)
    {
        dataCenter.SelectSection(section);
        _suppressToggleNav = true;
        leftMenu?.SetEditorToggleOff();
        leftMenu?.SetAvatarToggleOff();
        _suppressToggleNav = false;
        ShowPage(EditorPageType.Section);
        sectionEdit?.OnShow(dataCenter);
    }

    private void OnDataCenterChanged()
    {
        if (currentPage == EditorPageType.Section && dataCenter?.currentSelected != null)
            sectionEdit?.OnShow(dataCenter);
    }

    private void ScheduleAutoSave()
    {
        TimerManager.Inst.Stop(_autoSaveTimer);
        _autoSaveTimer = TimerManager.Inst.RunOnce("TheatreEditorAutoSave", 3f, AutoSaveDraft);
    }

    private void AutoSaveDraft()
    {
        if (dataCenter == null || string.IsNullOrEmpty(_draftInfo?.draftId)) return;

        dataCenter.UpdateTheatreInfoCounts();
        _draftInfo.SetMetaData(dataCenter.SerializeToBytes());

        var coverPath = dataCenter.TheatreInfo?.cover ?? "";
        if (!string.IsNullOrEmpty(coverPath) && !coverPath.StartsWith("file://"))
            _draftInfo.SyncCoverUrl(coverPath);

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);
        _draftInfo.editTime += curEditTime;
        _draftInfo.UploadAndSave();
    }

    private void OnJumpWindowRequested(int jumpIndex)
    {
        var targetSection = dataCenter?.SectionList?.Find(s => s.SectionIndex == jumpIndex);
        if (targetSection == null)
        {
            TipPanel.ShowToast("跳转目标不存在");
            return;
        }
        jumpWindow?.Show(targetSection, () =>
        {
            mainList?.ScrollToSection(targetSection);
            OnSectionSelected(targetSection);
        });
    }

    private void OnSectionCreatedHandler(POCTheatreSection section)
    {
        OnSectionSelected(section);
        AutoSaveDraft();
    }

    public void ShowAvatarExpressEdit(POCTheatreSection section)
    {
        ShowPage(EditorPageType.AvatarExpress);
        avatarExpressEdit?.OnShow(section);
    }

    public void ShowOptionEdit(POCTheatreSection section)
    {
        ShowPage(EditorPageType.Option);
        optionEdit?.OnShow(section);
    }

    public void ShowOptionEditWithIndex(POCTheatreSection section, int optionIndex)
    {
        ShowPage(EditorPageType.Option);
        optionEdit?.OnShow(section);
        optionEdit?.SetCurrentOptionIndex(optionIndex);
    }

    public void ShowEmoteEdit(POCTheatreSection section)
    {
        ShowPage(EditorPageType.Emote);
        emoteEdit?.OnShow(dataCenter);
    }

    public void ShowMusicEdit(POCTheatreSection section)
    {
        ShowPage(EditorPageType.Music);
        musicEdit?.OnShow(dataCenter);
    }

    public void SetupSearchBar(Action<string> onSearch)
    {
        searchBar?.Init(onSearch);
    }

    public void ShowBuyView(AnimMusicInfo info, Action onBuy)
    {
        buyView?.Show(info, onBuy);
    }

    public void ShowBuyView(string name, int price, Action onBuy)
    {
        buyView?.Show(name, price, onBuy);
    }

    public void ShowBuyViewForEmote(string name, int price, Action onBuy,
        string coverUrl = null, string itemId = null,
        CurrencyType currencyType = CurrencyType.PinkCoin)
    {
        buyView?.ShowForEmote(name, price, onBuy, currencyType: currencyType,
            coverUrl: coverUrl, itemId: itemId);
    }

    public void ShowAvatarSelector(
        TheatreEditorDataCenter dc,
        Game.Store.RecommendItemData emoteData,
        bool isSingle,
        Action<List<(string avatarId, int clothesIndex)>> onConfirm)
    {
        avatarEdit?.PreloadAvatarData(dc, () =>
        {
            ShowPage(EditorPageType.AvatarSelector);
            avatarSelector?.Show(
                dc.SelectedAvatarIds,
                dc.AvatarInfoCache,
                isSingle,
                onConfirm,
                onBack: BackToEmoteEdit);
        });
    }

    public void ShowEmoteShowcase(
        TheatreEditorDataCenter dc,
        Game.Store.RecommendItemData emoteData,
        List<(string avatarId, int clothesIndex)> actorSelections,
        Vector3 initialPosition,
        Vector3 initialRotation,
        float initialScale,
        Action<Vector3, Vector3, float> onConfirm,
        bool showSetPos = false)
    {
        ShowPage(EditorPageType.EmoteShowcase);

        var section = dc.currentSelected;
        // Use the actor being previewed (actorSelections) rather than section's original actor,
        // so the name is correct even when the section has no actor or the user picked a different one.
        string selectedId = actorSelections?.Count > 0 ? actorSelections[0].avatarId : section?.AvatarId;
        var avatarOc = !string.IsNullOrEmpty(selectedId) ? dc.AllAvatars.Find(a => a.PlayerId == selectedId) : null;
        string avatarName = avatarOc?.AvatarName ?? "";
        if (string.IsNullOrEmpty(avatarName) && !string.IsNullOrEmpty(selectedId)
            && dc.AvatarInfoCache.TryGetValue(selectedId, out var nameInfo))
            avatarName = nameInfo.name ?? "";
        var bgUrl = (section != null && section.BackgroundUrl > 0 && section.BackgroundUrl <= dc.AllBackgrounds.Count)
            ? dc.AllBackgrounds[section.BackgroundUrl - 1]
            : "";
        string avatarTypeUrl = "";
        if (!string.IsNullOrEmpty(selectedId) && dc.AvatarInfoCache.TryGetValue(selectedId, out var exprInfo) && exprInfo.expressions != null)
        {
            string avatarType = section?.AvatarType ?? "";
            int sep = avatarType.IndexOf('|');
            string targetName = sep >= 0 ? avatarType.Substring(0, sep) : avatarType;
            int targetMType = (sep >= 0 && int.TryParse(avatarType.Substring(sep + 1), out int m)) ? m : -1;

            foreach (var expr in exprInfo.expressions)
            {
                if (expr.expressionName == targetName
                    && (targetMType < 0 || expr.mType == targetMType)
                    && !string.IsNullOrEmpty(expr.expressionURL))
                { avatarTypeUrl = expr.expressionURL; break; }
            }
            if (string.IsNullOrEmpty(avatarTypeUrl) && exprInfo.expressions.Count > 0)
                avatarTypeUrl = exprInfo.expressions[0].expressionURL ?? "";
        }
        bool isNarratorSection = string.IsNullOrEmpty(selectedId)
            || selectedId == TheatreEditorDataCenter.NarratorAvatarId;
        emoteShowcase?.SetSectionInfo(avatarName, avatarTypeUrl, bgUrl, isNarratorSection);

        var paymentInfo = GetEmotePaymentInfo(emoteData);
        emoteShowcase?.Show(
            emoteData,
            actorSelections,
            dc.AvatarInfoCache,
            initialPosition,
            initialRotation,
            initialScale,
            onConfirm,
            onBuyRequest: onBought =>
            {
                var coverUrl     = emoteData?.UgcInfo?.cover ?? "";
                var ugcId        = emoteData?.ugcId ?? "";
                bool isPgc       = string.IsNullOrEmpty(coverUrl) && !string.IsNullOrEmpty(ugcId);
                var currencyType = paymentInfo?.currencyType ?? CurrencyType.PinkCoin;
                ShowBuyViewForEmote(
                    emoteData?.UgcInfo?.name ?? "",
                    paymentInfo?.price ?? 0,
                    onBought,
                    coverUrl:     isPgc ? null : coverUrl,
                    itemId:       ugcId,
                    currencyType: currencyType);
            },
            onBack: () =>
            {
                if (showSetPos)
                {
                    // 从 btn_setPos 进入：返回段落编辑，不走演员选择
                    BackToSectionEdit();
                    return;
                }
                ShowAvatarSelector(dc, emoteData, actorSelections.Count == 1,
                    newSelections => ShowEmoteShowcase(dc, emoteData, newSelections,
                        initialPosition, initialRotation, initialScale, onConfirm));
            },
            showSetPos: showSetPos);
    }

    public void BackToEmoteEdit()
    {
        ShowPage(EditorPageType.Emote);
    }

    private static GameData.Base.PaymentInfo GetEmotePaymentInfo(Game.Store.RecommendItemData data)
    {
        if (data?.UgcInfo is GameData.BaseInfo.AnimInfo anim) return anim.paymentInfo;
        if (data?.UgcInfo is GameData.BaseInfo.PoseInfo pose) return pose.paymentInfo;
        return null;
    }

    public void ShowBGSelector(Action<int> onSelected)
    {
        ShowPage(EditorPageType.BGSelector);
        bgSelector?.Show(dataCenter, bgIndex =>
        {
            onSelected?.Invoke(bgIndex);
            BackToSectionEdit();
        });
    }

    private void OnEditBackClicked()
    {
        switch (currentPage)
        {
            case EditorPageType.BGSelector:
            case EditorPageType.Option:
            case EditorPageType.AvatarExpress:
            case EditorPageType.Music:
            case EditorPageType.Emote:
                BackToSectionEdit();
                break;
        }
    }

    public void BackToSectionEdit()
    {
        ShowPage(EditorPageType.Section);
        sectionEdit?.OnShow(dataCenter);
    }

    public void NotifyMusicSelected(string audioName)
    {
        BackToSectionEdit();
    }

    public void RefreshSectionItemDisplay(POCTheatreSection section)
    {
        mainList?.RefreshSectionItem(section);
    }

    public void ShowAvatarEdit()
    {
        _suppressToggleNav = true;
        leftMenu?.SetEditorToggleOff();
        leftMenu?.SetAvatarToggleOn();
        _suppressToggleNav = false;
        ShowPage(EditorPageType.Avatar);
    }

    public void BackToMainEdit()
    {
        ShowPage(EditorPageType.Main);
    }

    private void ShowPlay(POCTheatreSection startSection)
    {
        if (dataCenter == null || startSection == null) return;
        var preview = new OCTheatreInfo();
        preview.id = "";
        preview.name = dataCenter.TheatreInfo?.name ?? "";
        preview.cover = dataCenter.TheatreInfo?.cover ?? "";
        preview.LoadDetailInfo(dataCenter.SerializeToBytes());
        // Sync editor avatar cache so TheatreGamePanel.GetAvatarDisplayInfo can resolve names
        foreach (var kv in dataCenter.AvatarInfoCache)
            if (!string.IsNullOrEmpty(kv.Key))
                OCTheatreDataManager.Inst.ocCharacterInfoDict[kv.Key] = kv.Value;
        UIManager.Inst.OpenPanel(PanelId.TheatreGamePanel, preview, (int)GameData.TheatreEnterType.Editor, startSection.SectionIndex);
    }

    private void OnSettingExit()
    {
        closePanel?.gameObject.SetActive(true);
    }

    private void OnSaveAndClose()
    {
        if (!TryValidateSave()) return;

        dataCenter.UpdateTheatreInfoCounts();
        _draftInfo.SetMetaData(dataCenter.SerializeToBytes());

        var coverPath = dataCenter.TheatreInfo?.cover ?? "";
        if (coverPath.StartsWith("file://"))
        {
            var localPath = coverPath.Replace("file://", "");
            if (File.Exists(localPath))
                _draftInfo.SetCover(File.ReadAllBytes(localPath));
            if (dataCenter.TheatreInfo != null)
                dataCenter.TheatreInfo.cover = "";
        }
        else if (!string.IsNullOrEmpty(coverPath))
        {
            _draftInfo.SyncCoverUrl(coverPath);
        }

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);
        _draftInfo.editTime += curEditTime;

        if (string.IsNullOrEmpty(_draftInfo.draftId))
        {
            _draftInfo.Upload((_, uploadOk) =>
            {
                if (!uploadOk) { TipPanel.ShowToast("上传失败，请重试"); return; }
                _draftInfo.CreateDraftToServer((info, ok) =>
                {
                    if (!ok || info == null) { TipPanel.ShowToast("保存失败，请重试"); return; }
                    _draftInfo.draftId = info.id ?? "";
                    if (dataCenter.TheatreInfo != null)
                        dataCenter.TheatreInfo.id = info.id ?? "";
                    OnCloseConfirmed();
                });
            });
        }
        else
        {
            _draftInfo.UploadAndSave((_, success) =>
            {
                if (!success) { TipPanel.ShowToast("上传失败，请重试"); return; }
                OnCloseConfirmed();
            });
        }
    }

    private void OnCloseConfirmed()
    {
        MessageHelper.Broadcast(MessageName.OnTheatreStudioDraftListChange);
        CloseSelf();
    }

    private bool TryValidateSave()
    {
        if (string.IsNullOrEmpty(dataCenter?.TheatreInfo?.name))
        {
            TipPanel.ShowToast("请先输入剧场名称");
            return false;
        }

        if (string.IsNullOrEmpty(dataCenter?.TheatreInfo?.cover))
        {
            TipPanel.ShowToast("请先上传剧场封面图");
            return false;
        }

        if (dataCenter == null || dataCenter.SectionList.Count == 0)
        {
            TipPanel.ShowToast("请至少添加一个场景");
            return false;
        }

        int totalDialogues = 0;
        foreach (var s in dataCenter.SectionList)
            totalDialogues += dataCenter.GetNormalDialogueCount(s);
        if (totalDialogues == 0)
        {
            TipPanel.ShowToast("请至少添加一句对话");
            return false;
        }

        if (dataCenter.SelectedAvatarIds.Count == 0)
        {
            TipPanel.ShowToast("请至少选择一名参演演员");
            return false;
        }

        return true;
    }

    private void OnSettingSave()
    {
        if (!TryValidateSave()) return;

        dataCenter.UpdateTheatreInfoCounts();
        _draftInfo.SetMetaData(dataCenter.SerializeToBytes());

        // Editor cover picker returns a local file:// path — register it for COS upload.
        var coverPath = dataCenter.TheatreInfo?.cover ?? "";
        if (coverPath.StartsWith("file://"))
        {
            var localPath = coverPath.Replace("file://", "");
            if (File.Exists(localPath))
                _draftInfo.SetCover(File.ReadAllBytes(localPath));
            if (dataCenter.TheatreInfo != null)
                dataCenter.TheatreInfo.cover = "";
        }
        else if (!string.IsNullOrEmpty(coverPath))
        {
            _draftInfo.SyncCoverUrl(coverPath);
        }

        int curEditTime = GameTimeUtils.Inst.RestartCollect(TAG);
        _draftInfo.editTime += curEditTime;

        if (string.IsNullOrEmpty(_draftInfo.draftId))
        {
            // Upload assets to COS first so CreateDraftToServer sends CDN URLs.
            _draftInfo.Upload((_, uploadOk) =>
            {
                if (!uploadOk) { TipPanel.ShowToast("上传失败，请重试"); return; }
                _draftInfo.CreateDraftToServer((info, ok) =>
                {
                    if (!ok || info == null) { TipPanel.ShowToast("保存失败，请重试"); return; }
                    _draftInfo.draftId = info.id ?? "";
                    if (dataCenter.TheatreInfo != null)
                        dataCenter.TheatreInfo.id = info.id ?? "";
                    TipPanel.ShowToast("保存成功");
                    MessageHelper.Broadcast(MessageName.OnTheatreStudioDraftListChange);
                });
            });
        }
        else
        {
            _draftInfo.UploadAndSave((_, success) =>
            {
                TipPanel.ShowToast(success ? "保存成功" : "上传失败，请重试");
                if (success) MessageHelper.Broadcast(MessageName.OnTheatreStudioDraftListChange);
            });
        }
    }

    private void ShowPage(EditorPageType page)
    {
        currentPage = page;
        mainEdit?.gameObject.SetActive(page == EditorPageType.Main);
        sectionEdit?.gameObject.SetActive(page == EditorPageType.Section);
        avatarEdit?.gameObject.SetActive(page == EditorPageType.Avatar);
        avatarExpressEdit?.gameObject.SetActive(page == EditorPageType.AvatarExpress);
        emoteEdit?.gameObject.SetActive(page == EditorPageType.Emote
            || page == EditorPageType.AvatarSelector || page == EditorPageType.EmoteShowcase);
        optionEdit?.gameObject.SetActive(page == EditorPageType.Option);
        bgSelector?.gameObject.SetActive(page == EditorPageType.BGSelector);
        musicEdit?.gameObject.SetActive(page == EditorPageType.Music);
        avatarSelector?.gameObject.SetActive(page == EditorPageType.AvatarSelector);
        emoteShowcase?.gameObject.SetActive(page == EditorPageType.EmoteShowcase);

        bool showSearch = page == EditorPageType.Music || page == EditorPageType.Emote;
        searchBar?.gameObject.SetActive(showSearch);
        editTitleGObj?.SetActive(!showSearch);

        bool showBack = page == EditorPageType.Option || page == EditorPageType.AvatarExpress
                        || page == EditorPageType.BGSelector || page == EditorPageType.Music
                        || page == EditorPageType.Emote;
        if (EditBackBtn != null) EditBackBtn.gameObject.SetActive(showBack);

        if (page == EditorPageType.Main)
        {
            mainEdit?.OnShow(dataCenter);
            dataCenter?.SelectSection(null);
        }
        if (page == EditorPageType.Avatar) avatarEdit?.OnShow(dataCenter);

        if (EditTitle != null)
            EditTitle.text = page switch
            {
                EditorPageType.Main          => "管理剧场",
                EditorPageType.Section       => "对话编辑",
                EditorPageType.Option        => "选项编辑",
                EditorPageType.Emote         => "动画编辑",
                EditorPageType.AvatarExpress => "角色立绘",
                EditorPageType.Avatar        => "演员选择",
                EditorPageType.BGSelector    => "背景选择",
                EditorPageType.Music         => "音效选择",
                EditorPageType.AvatarSelector => "选择演员",
                EditorPageType.EmoteShowcase  => "动作预览",
                _                            => EditTitle.text,
            };
    }

    public override void OnHidden()
    {
        base.OnHidden();
        TimerManager.Inst.Stop(_autoSaveTimer);
        AkSoundManager.Inst.PlayBGSound();
        dataCenter?.Dispose();
        dataCenter = null;
    }

    private enum EditorPageType
    {
        Main, Section, Avatar, AvatarExpress, Emote, Option, BGSelector, Music,
        AvatarSelector, EmoteShowcase,
    }
}
