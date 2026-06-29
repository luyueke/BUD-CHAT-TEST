
using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UIAgent;
using UnityEngine;

public class MapDetailPanel : BasePanel<MapDetailPanel>
{
    #region UIComponent
    private GameObject _loadingGo;
    private Transform _trans_Bg;
    private CButton _btn_Back;
    private CButton _btn_TopSelect;
    private RemoteImageBehaviour _rm_MapCover;
    private SuperTextMesh _txt_Title;
    private SuperTextMesh _txt_Desc;
    private UserInfoView _userInfoView;
    private FollowButton _followButton;
    private ReportButton _reportButton;
    private CButton _btn_Play;
    private CButton _createPrivateRoom;
    private DesignCode _designCode ;
    private InteractInfoView _interactInfoView;
    private LikeButton _likeButton;
    private NativeCommentsButton _nativeCommentsButton;
    private FavoritesButton _favoritesButton;
    private CommentButton _commentButton;
    private EnergyCoinButton _energyCoinButton;
    private MapLikePhotoAdpter _adpter;
    private GameObject _noneTips;
    #endregion

    #region Datas
    private MapInfo _curMapInfo;
    private BaseCreator _curCreator;
    private BaseInteractInfo _curInteractInfo;
    private RelationShipInfo _curRelationShipInfo;

    // 相册列表缓存（避免 LazyDataHelper 捕获旧 list，且用于等待 adapter 初始化后再刷新）
    private List<AlbumPhotoInfo> _albumListCache;
    private Coroutine _bindAlbumListCo;
    #endregion


    public override void OnCreate()
    {
        base.OnCreate();
        _loadingGo = GameObjectEx.FindChildByName(this.transform, "LoadingGo").gameObject;
        _trans_Bg = GameObjectEx.FindChildByName(this.transform, "BGTrans");
        _btn_Back = GameObjectEx.FindChildByName(this.transform, "Btn_Back").GetComponent<CButton>();
        _btn_TopSelect = GameObjectEx.FindChildByName(this.transform, "CreateNow").GetComponent<CButton>();
        _rm_MapCover = GameObjectEx.FindChildByName(this.transform, "Rm_MapCover").GetComponent<RemoteImageBehaviour>();
        _txt_Title = GameObjectEx.FindChildByName(this.transform, "Txt_MapTitle").GetComponent<SuperTextMesh>();
        _txt_Desc = GameObjectEx.FindChildByName(this.transform, "Txt_MapDesc").GetComponent<SuperTextMesh>();
        _userInfoView = GameObjectEx.FindChildByName(this.transform, "UserInfoView").GetComponent<UserInfoView>();
        _followButton = GameObjectEx.FindChildByName(this.transform, "FollowButton").GetComponent<FollowButton>();
        _reportButton = GameObjectEx.FindChildByName(this.transform, "ReportButton").GetComponent<ReportButton>();
        _btn_Play = GameObjectEx.FindChildByName(this.transform, "Btn_Play").GetComponent<CButton>();
        _createPrivateRoom = GameObjectEx.FindChildByName(this.transform, "Btn_CreatePrivateRoom").GetComponent<CButton>();

        _interactInfoView = GameObjectEx.FindChildByName(this.transform, "InteractInfoView").GetComponent<InteractInfoView>();
        _likeButton = GameObjectEx.FindChildByName(this.transform, "LikeButton").GetComponent<LikeButton>();
        _likeButton.BindInteractInfoView(_interactInfoView);
        _nativeCommentsButton = GameObjectEx.FindChildByName(this.transform, "NativeCommentsButton").GetComponent<NativeCommentsButton>();
        _favoritesButton = GameObjectEx.FindChildByName(this.transform, "FavoritesButton").GetComponent<FavoritesButton>();
        _favoritesButton.BindInteractInfoView(_interactInfoView);
        _commentButton = GameObjectEx.FindChildByName(this.transform, "CommentButton").GetComponent<CommentButton>();
        _designCode =  GameObjectEx.FindChildByName(this.transform, "DesignCode").GetComponent<DesignCode>();
        _energyCoinButton =  GameObjectEx.FindChildByName(this.transform, "EnergyCoinButton").GetComponent<EnergyCoinButton>();
        _adpter = GameObjectEx.FindChildByName(this.transform, "OSA").GetComponent<MapLikePhotoAdpter>();
        _noneTips = GameObjectEx.FindChildByName(this.transform, "NoneTips").gameObject;
        _btn_Back.onClick.AddListener(() => { CloseSelf();});
        _btn_TopSelect.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel(PanelId.GameStudioPanel);
        });
        InitBG();
    }
    
            
    private void InitBG()
    {
        return;
#if false
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "store_icon4","store_icon5","store_icon6"
        });
        item.gameObject.SetActive(true);
#endif
    }

    /// <summary>
    /// 打开地图详情所需要的参数
    /// args[0] ugcId
    /// </summary>
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        string mapId = (string)args[0];
        JObject req = new JObject()
        {
            ["id"] = mapId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
            OnMapInfoSuccess, OnMapInfoFail);
    }

    private void OnMapInfoSuccess(string content)
    {
        _loadingGo.SetActive(false);

        UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
        _curMapInfo = rspData.mapInfo;
        _curCreator = rspData.creator;
        _curInteractInfo = rspData.interactInfo;
        _curRelationShipInfo = rspData.relationShipInfo;
        // 相册列表数据
        BindAlbumList(rspData != null ? rspData.albumList : null);
        InitUI();
    }

    private void BindAlbumList(List<AlbumPhotoInfo> list)
    {
        _albumListCache = list;
        var hasData = list != null && list.Count > 0;
        if (_noneTips != null)
        {
            _noneTips.SetActive(!hasData);
        }

        if (_adpter == null)
        {
            return;
        }

        if (_bindAlbumListCo != null)
        {
            StopCoroutine(_bindAlbumListCo);
            _bindAlbumListCo = null;
        }
        _bindAlbumListCo = StartCoroutine(CoBindAlbumList(hasData ? list.Count : 0));
    }

    private IEnumerator CoBindAlbumList(int count)
    {
        int waitFrames = 10;
        while (waitFrames-- > 0 && (_adpter == null || !_adpter.gameObject.activeInHierarchy))
        {
            yield return null;
        }

        if (_adpter == null) yield break;

        TryInitAdapter(_adpter);

        // DataHelper 的模型创建委托必须使用缓存字段，避免多次刷新时捕获旧 list
        _adpter.Data = new LazyDataHelper<AlbumPhotoInfo>(_adpter, GetAlbumInfo);
        _adpter.OnItemSelected = OnAlbumItemSelected;

        // 若 adapter 仍未完成初始化，则不强行 ResetItems（会导致内部空引用）
        if (!_adpter.IsInitialized)
        {
            yield break;
        }

        _adpter.Data.ResetItems(count);
        _adpter.Refresh();
    }

    private AlbumPhotoInfo GetAlbumInfo(int index)
    {
        var list = _albumListCache;
        if (list == null) return null;
        if (index < 0 || index >= list.Count) return null;
        return list[index];
    }

    private static void TryInitAdapter(MapLikePhotoAdpter adpter)
    {
        if (adpter == null) return;
        if (adpter.IsInitialized) return;
        if (!adpter.gameObject.activeInHierarchy) return;
        adpter.Init();
    }

    private void OnAlbumItemSelected(AlbumPhotoInfo info)
    {
        if (info == null) return;
        // 复用大图预览：AlbumPhotoInfo -> CameraImagePack
        var pack = CameraImgDataUtils.BuildCameraImagePackFromAlbumPhotoInfo(info);
        if (pack == null) return;
        UIManager.Inst.OpenPanel(PanelId.BigPhotoImgPanel, pack);
    }

    private void OnMapInfoFail(string error)
    {
        LoggerUtils.LogError(error);

    }

    private void InitUI()
    {
        _rm_MapCover.Load(_curMapInfo.cover);
        _txt_Title.text = _curMapInfo.name;
        _txt_Desc.text = _curMapInfo.desc;
        AccountUserInfo accountUserInfo = _curCreator;
        _userInfoView.SetData(accountUserInfo);
        _userInfoView.SetUserNickLengthLimit(34);
        _followButton.SetRelation(_curCreator.uid, _curRelationShipInfo);
        _btn_Play.onClick.AddListener(OnBtnPlayClick);
        _createPrivateRoom.onClick.AddListener(OnBtnCreatePrivateRoomClick);
        _interactInfoView.SetData(_curInteractInfo);
        _likeButton.SetData(_curMapInfo.id, _curInteractInfo.liked, _curInteractInfo.likeAmount);
        _energyCoinButton.gameObject.SetActive(true);
        _energyCoinButton.SetData(_curCreator.uid, _curMapInfo.id, _curInteractInfo.rewardAmount);
        _nativeCommentsButton.SetData(_curInteractInfo.commentAmount);
        _favoritesButton.SetData(_curMapInfo.id, _curInteractInfo.collected, _curInteractInfo.collectAmount);
        _commentButton.SetData(_curMapInfo.id);
        _reportButton.SetData(_curMapInfo.id, (int)ErrReportSceneType.UgcMap);
        _designCode.SetCodeInfo(_curMapInfo);
    }

    private void OnBtnPlayClick()
    {
        if (!GameController.IsInHallScene())
        {
            TipPanel.ShowToast("您已经在游戏内，请退出房间后再试");
            return;
        }
        var updateState = (ForceUpdate)_curMapInfo.forceUpdate;
        if (updateState != ForceUpdate.Default)
        {
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
            return;
        }
        
        EnterRoom();
    }

    private void OnBtnCreatePrivateRoomClick()
    {
        GameDataManager.Inst.gameOnlineData.CreatePrivateData();
        EnterRoom();
    }

    private void EnterRoom()
    {
        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(_curMapInfo, _curCreator, LoadingType.Map);
        if (_curMapInfo.gameType == (int)GameType.Normal)
        {
            GameController.StartGuestGame(_curMapInfo,_curCreator,_curInteractInfo);
        }
        else if (_curMapInfo.gameType == (int)GameType.AIGame)
        {
            AIHospitalUtils.Inst.EnterUgcHospitalGame(_curMapInfo.id);
        }
        else
        {
        }
    }
}
