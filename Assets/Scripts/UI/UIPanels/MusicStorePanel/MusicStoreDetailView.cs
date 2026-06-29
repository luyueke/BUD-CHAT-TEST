using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Avatar;
using Game.MusicalInstrument;
using Game.Store;
using GameData.BaseInfo;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Base;
using Pb.Game;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

// 乐谱商城详情视图：从 FittingRoom 的 Ugc+乐谱 流程迁出。
// 展示部分参照 TimbreStoreDetailView（封面/作者/价格/购买/音数）；
// 试听部分参照 FittingRoomPanel.PreviewMusicScore：
//   面板内自建一个预览角色，挂上当前“试听乐器”，循环演奏选中的乐谱；
//   通过 SwitchMI（换乐器）可切换演奏乐器（打开 MusicalInstrumentBagPanel）。
//   与 FittingRoom 一致：选中乐谱即自动试听，没有独立的播放/暂停按钮。
public class MusicStoreDetailView : MonoBehaviour
{
    [Header("商品信息")]
    [SerializeField] private UserInfoView UserInfoView;
    [SerializeField] private CButton Btn_UserHead;
    [SerializeField] private PurchaseButton PurchaseButton;
    [SerializeField] private CText Txt_ItemName;
    [SerializeField] private GameObject toneTypeGameObject;
    [SerializeField] private Text Txt_ToneType;
    [SerializeField] private RemoteImageBehaviour CoverImage;

    [SerializeField] private AccountWidget GemWidget;
    [SerializeField] private AccountWidget PinkWidget;

    [Header("预览角色")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private UIDragUtil dragUtil;
    [SerializeField] private PlayMusicScoreBev playMusicScoreBev; 
    [Header("换乐器")]
    [SerializeField] private SwitchMI switchMI;
    [Header("搜索")]
    [SerializeField] private SearchView SearchView;

    // 是否处于试听状态（用于异步回调里判断是否已被打断）
    private bool IsPlaying = false;

    private RecommendItemData curData;
    public Action<RecommendItemData> DidPayAssetAction;
    public Action<List<RecommendItemData>, Action> SearchResultAction;
    public Action SearchCancelAction;
    private string _searchKey;

    private CharacterWrap characterWrap;
    private PlayerHoldBehaviour playerHoldBehaviour;
    // 当前已加载到预览角色身上的“试听乐器”id；与 FittingRoom 的 previewMusicInstrumentId 同义
    private string previewMusicInstrumentId = "";
    // 乐器是否已异步加载完成（对应 FittingRoom 的 isOk）
    private bool _instrumentReady = false;
    private readonly Dictionary<string, DetailRsp> detailCache = new Dictionary<string, DetailRsp>();

    public void Awake()
    {
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        MessageHelper.AddListener(MessageName.OnTryListenMIChange, OnTryListenMIChange);

        if (GemWidget != null)
        {
            GemWidget.DidClickAction = () => { StopPlayIfNeed(); };
        }
        if (PinkWidget != null)
        {
            PinkWidget.DidClickAction = () => { StopPlayIfNeed(); };
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        MessageHelper.RemoveListener(MessageName.OnTryListenMIChange, OnTryListenMIChange);
        StopPreview();
    }

    #region 搜索（参考原商城 UGCScene 的 OnSearchClick/OnSearchAction 流程）

    public void OpenSearchView()
    {
        if (SearchView == null)
        {
            return;
        }
        SearchView.searchHandle = "搜索设计码/作者ID/乐谱名";
        SearchView.isFittingRoom = true;
        SearchView.gameObject.SetActive(true);
        SearchView.SetSearchAction(OnSearchWord, OnSearchClear, OnSearchCancel);
        SearchView.SetInitSearchAction(OpenSearchView);
        SearchView.Show();
    }

    private void OnSearchWord(string str)
    {
        SearchLogicMgr.Inst.AddSearchHistoryWord(str);
        _searchKey = str;

        var dataHandler = AssetsDataManager.GetData<AvatarUgcSceneHandler>();
        var classType = UniqueType.Get(ResourceType.MusicScore, (int)MusicScoreSubType.GeneralMusicScore);
        Action nextAction = null;
        nextAction = dataHandler.SearchGoodsData(classType, str, (searchKey, isEnd, goodsDatas) =>
        {
            if (this == null || searchKey != _searchKey)
            {
                return;
            }
            SearchView.isSearching = false;
            SearchView.HideSearchDiscoverAndHistoryView();

            var list = goodsDatas == null
                ? new List<RecommendItemData>()
                : goodsDatas
                    .Where(g => g?.Assets != null && g.Assets.Count > 0 && g.Assets[0].UgcInfo != null)
                    .Select(g => g.Assets[0].UgcInfo)
                    .ToList();

            if (isEnd && list.Count == 0)
            {
                TipPanel.ShowToast("没有找到相关内容");
            }
            SearchResultAction?.Invoke(list, nextAction);
        });

        if (!string.IsNullOrEmpty(str) && Regex.IsMatch(str, @"^[0-9A-Z]{7}$"))
        {
            SearchPanel.Search(str, (int)AvatarSubType.All, SearchPanel.SearchType.MusicScore, false);
        }
    }

    private void OnSearchClear()
    {
        _searchKey = null;
        SearchResultAction?.Invoke(new List<RecommendItemData>(), null);
    }

    private void OnSearchCancel()
    {
        _searchKey = null;
        SearchCancelAction?.Invoke();
    }

    #endregion

    public void RefreshUIByData(RecommendItemData data)
    {
        this.curData = data;
        RefreshUIInfo(data);
    }

    private void RefreshUIInfo(RecommendItemData data)
    {
        var msData = data.UgcInfo as MusicScoreInfo;
        if (msData == null)
        {
            return;
        }

        var ugcId = msData?.id;
        var consumed = data?.interactInfo?.consumed ?? 0;
        var propName = data?.UgcInfo?.name;
        var paymentInfo = msData?.paymentInfo;
        AccountUserInfo accountUserInfo = data?.creatorInfo;
        UserInfoView.IsOpenProfilePanel = false;
        UserInfoView.SetData(accountUserInfo);
        Btn_UserHead.onClick.RemoveAllListeners();
        Btn_UserHead.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            StopPlayIfNeed();
            UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.MusicScore, ugcId);
        });
        PurchaseButton.SetData(msData, consumed, paymentInfo);
        // 与列表项一致：consumed 之外再查本地背包，避免已拥有的乐谱在详情页仍显示购买按钮。
        bool isOwned = consumed == 1 || (!string.IsNullOrEmpty(ugcId) && AssetsDataManager.IsOwned(ugcId));
        bool isMyCreation = msData.creator == AccountDataManager.Inst.Uid;
        PurchaseButton.gameObject.SetActive(!isOwned && !isMyCreation);
        Txt_ItemName.text = propName;

        UserInfoView.gameObject.SetActive(true);
        Txt_ItemName.gameObject.SetActive(true);

        var coverPath = msData.cover;
        if (!string.IsNullOrEmpty(coverPath))
        {
            CoverImage.Load(coverPath);
        }

        if (toneTypeGameObject != null)
        {
            toneTypeGameObject.SetActive(true);
        }
        if (Txt_ToneType != null)
        {
            Txt_ToneType.text = msData.toneType == (int)ToneType.Fifteen ? "15音" : "22音";
        }

        // 选中乐谱即自动试听（与 FittingRoom 一致，无独立播放按钮）
        PlayCurScore();
    }

    private void OnBuyUgcItemSuccess(string ugcId)
    {
        if (curData == null)
        {
            return;
        }

        if (ugcId != curData.UgcInfo.id)
        {
            return;
        }

        if (curData.interactInfo != null)
        {
            curData.interactInfo.consumed = 1;
            RefreshUIInfo(curData);
        }
        AccountDataManager.Inst.BalanceInfo.Refresh();
        DidPayAssetAction?.Invoke(curData);
    }

    public void StopPlayIfNeed()
    {
        StopPreview();
    }

    #region 预览角色 + 乐谱试听

    // 试听当前选中的乐谱：乐器没变只重播乐谱，乐器变了才重新加载乐器。
    private void PlayCurScore()
    {
        var msData = curData?.UgcInfo as MusicScoreInfo;
        if (msData == null)
        {
            return;
        }
        IsPlaying = true;
        // 先停上一首乐谱（乐器是否重载由 PreviewInstrumentThenPlay 决定）
        if (playMusicScoreBev != null)
        {
            playMusicScoreBev.StopPLay();
        }
        PreviewInstrumentThenPlay(msData);
    }

    private void EnsureCharacter()
    {
        if (characterWrap != null)
        {
            return;
        }
        var avatarJson = AccountDataManager.Inst.UserInfo.avatarJson;
        CharacterData tempAvatarInfo = CharacterData.DeserializeObject(avatarJson).Clone();
        characterWrap = AvatarController.Inst.CreateUIAvatar(tempAvatarInfo);
        characterWrap.SetParent(characterRoot, true);
        if (dragUtil != null)
        {
            dragUtil.RotateTarget = characterWrap.Avatar.transform;
        }
        playerHoldBehaviour = characterWrap.Avatar.GetComponentInChildren<PlayerHoldBehaviour>();
    }

    // 把当前“试听乐器”加载到预览角色身上（ChangePart/ChangeUGCPart 为异步换装，
    // 必须在加载完成回调里才能预览乐器并演奏乐谱，否则乐器 mesh 未就绪会动作错位）。
    private void PreviewInstrumentThenPlay(MusicScoreInfo msInfo)
    {
        EnsureCharacter();
        if (characterWrap == null || playerHoldBehaviour == null || playMusicScoreBev == null)
        {
            return;
        }

        var isPgc = AccountDataManager.Inst.TryListenMIData.isPgc;
        var id = AccountDataManager.Inst.TryListenMIData.id;

        if (switchMI != null)
        {
            switchMI.gameObject.SetActive(true);
        }

        // 乐器没变且已加载好：直接播放乐谱，不重载乐器
        if (previewMusicInstrumentId == id && _instrumentReady)
        {
            PlayScoreNow(msInfo);
            return;
        }

        previewMusicInstrumentId = id;
        _instrumentReady = false;

        if (isPgc)
        {
            characterWrap.ChangePart(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), id, () =>
            {
                if (this == null || !IsPlaying || previewMusicInstrumentId != id)
                {
                    return;
                }
                playerHoldBehaviour.PreviewPGCInstrument(id);
                if (switchMI != null)
                {
                    switchMI.SetTarget(id, "");
                }
                _instrumentReady = true;
                PlayScoreNow(msInfo);
            });
            return;
        }

        GetMIDetailInfo(id, (rspData) =>
        {
            if (this == null || !IsPlaying || previewMusicInstrumentId != id)
            {
                return;
            }
            characterWrap.ChangeUGCPart(rspData.skinInfo, () =>
            {
                if (this == null || !IsPlaying || previewMusicInstrumentId != id)
                {
                    return;
                }
                playerHoldBehaviour.PreviewUGCInstrument(rspData.skinActionInfo.instrumentInfo);
                if (switchMI != null)
                {
                    switchMI.SetTarget(id, rspData.skinInfo.cover);
                }
                _instrumentReady = true;
                PlayScoreNow(msInfo);
            });
        },
        () =>
        {
            if (this == null || !IsPlaying)
            {
                return;
            }
            // UGC 乐器拉取失败，回退默认乐器（话筒）
            AccountDataManager.Inst.TryListenMIData = new();
            if (!AccountDataManager.Inst.TryListenMIData.isPgc)
            {
                if (switchMI != null)
                {
                    switchMI.gameObject.SetActive(true);
                }
                return;
            }
            previewMusicInstrumentId = "";
            PreviewInstrumentThenPlay(msInfo);
        });
    }

    // 乐器已就绪，循环演奏乐谱
    private void PlayScoreNow(MusicScoreInfo msInfo)
    {
        if (playMusicScoreBev == null || playerHoldBehaviour == null)
        {
            return;
        }
        MusicalInstrumentManager.Inst.CheckInstrumentCanPlayMusicScore(playerHoldBehaviour.curToneInfo, msInfo, () =>
        {
            TipPanel.ShowToast("这个乐谱是22音，你的乐器是15音，听起来可能会少音哦");
        });
        MusicalInstrumentManager.Inst.CheckInstrumentIsUgcToneAndShowToast(playerHoldBehaviour.curToneInfo);
        playMusicScoreBev.ChangePlayType(PlayMusicScoreBev.PlayType.Loop);
        playMusicScoreBev.StartPLay(msInfo, OnPlaySingleSyllable);
    }

    private void GetMIDetailInfo(string id, Action<DetailRsp> suc, Action fail)
    {
        if (detailCache.ContainsKey(id))
        {
            suc?.Invoke(detailCache[id]);
            return;
        }
        JObject req = new JObject()
        {
            ["idList"] = id,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {
            if (this == null || previewMusicInstrumentId != id || playerHoldBehaviour == null)
            {
                return;
            }
            BatchDetailRsp rspData = JsonConvert.DeserializeObject<BatchDetailRsp>(content);
            if (rspData.skinList == null || rspData.skinList.Count == 0 || rspData.skinList[0].skinActionInfo == null)
            {
                fail?.Invoke();
                return;
            }
            detailCache[id] = rspData.skinList[0];
            suc?.Invoke(detailCache[id]);
        },
        (msg) =>
        {
            fail?.Invoke();
        });
    }

    private void OnPlaySingleSyllable(List<SyllablePlayData> playData)
    {
        if (playerHoldBehaviour != null)
        {
            playerHoldBehaviour.PlayMusicSyllable(playData);
        }
    }

    private void StopPreview()
    {
        IsPlaying = false;
        previewMusicInstrumentId = "";
        _instrumentReady = false;
        if (playMusicScoreBev != null)
        {
            playMusicScoreBev.StopPLay();
        }
        if (playerHoldBehaviour != null)
        {
            playerHoldBehaviour.StopPreviewInstrument();
        }
        if (switchMI != null)
        {
            switchMI.gameObject.SetActive(false);
        }
    }

    // 在 MusicalInstrumentBagPanel 里换了乐器后回调：重新加载乐器并继续演奏当前乐谱。
    private void OnTryListenMIChange()
    {
        if (!IsPlaying)
        {
            return;
        }
        // 乐器 id 已变，PreviewInstrumentThenPlay 会检测到并重载
        PlayCurScore();
    }

    #endregion
}
