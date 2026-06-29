using AIGame.Base;
using UnityEngine;
using UI.BaseWidgets;
using UI.Base;
using GameData.BaseInfo;
using Game.Base;
using UnityEngine.UI;
using Network.Http;
using Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using GameData.UGCData;
using GameData.Base;
using Com.TheFallenGames.OSA.Util.IO;
using Message;
using Game.Avatar;
using System;

public class AIHospitalUgcMapInfoPanel : BasePanel<AIHospitalUgcMapInfoPanel> 
{
    #region UIComponent
    //[SerializeField] private GameObject _loadingGo;
    [SerializeField] private CButton _backButton;    
    [SerializeField] private CButton _playButton;    
    [SerializeField] private RemoteImageBehaviour _rm_MapCover;
    [SerializeField] private SuperTextMesh _txt_Title;
    [SerializeField] private SuperTextMesh _txt_Desc;
    [SerializeField] private UI.UIWidgets.UserInfoView _userInfoView;
    [SerializeField] private FollowButton _followButton;
    [SerializeField] private ReportButton _reportButton;
    [SerializeField] private InteractInfoView _interactInfoView;
    [SerializeField] private LikeButton _likeButton;
    [SerializeField] private FavoritesButton _favoritesButton;
    //[SerializeField] private CommentButton _commentButton;
    //[SerializeField] private EnergyCoinButton _energyCoinButton;
    [SerializeField] private ShareButton _shareBtn;
    [SerializeField] private AssistMapHeatButton _assistBtn;
    [SerializeField] private Text _mapHeatTxt;
    //[SerializeField] private ScrollView _npcListView;
    [SerializeField] private AIHospitalNpcShowListItem _npcItemPrefab;
    [SerializeField] private CButton Btn_Delete;
    [SerializeField] private CButton Btn_Eit;
    
    [SerializeField] private Toggle tog1;
    [SerializeField] private Toggle tog2;

    [SerializeField] private GameObject _detailTab;
    [SerializeField] private AIMapHeatRankPanel _rankTab;
    [SerializeField] private Image rankImage;

    [SerializeField] private GameObject _avatarParentObj;
    [SerializeField] private AvatarCameraController _clickArea;
    #endregion
    string titleAtlas = "Assets/Loadable/UI/UIPanel/AIHospitalGame/AIHospitalMapInfoPanel/Prefabs/TitleAtlas.spriteatlas";
    /// <summary>
    /// 还需要添加一个npclist 用于展示npc形象以及点击时的具体信息
    /// </summary>

    #region Datas
    private MapInfo _curMapInfo;
    private BaseCreator _curCreator;
    private BaseInteractInfo _curInteractInfo;
    private RelationShipInfo _curRelationShipInfo;
    #endregion


    private Action<string, bool> _updateAction;

    public override void OnCreate()
    {
        base.OnCreate();
        InitUI();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        string mapId = (string)args[0];
        JObject req = new JObject()
        {
            ["id"] = mapId,
        };
        //_loadingGo.SetActive(true);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
            OnMapInfoSuccess, OnMapInfoFail);
        _clickArea.RotateTarget = _avatarParentObj.transform;
    }

    private void OnMapInfoSuccess(string content)
    {
        //_loadingGo.SetActive(false);

        UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
        _curMapInfo = rspData.mapInfo;
        _curCreator = rspData.creator;
        _curInteractInfo = rspData.interactInfo;
        _curRelationShipInfo = rspData.relationShipInfo;
        InitUIData();

        InitScrollView();
    }

    private void OnMapInfoFail(string error)
    {
        //_loadingGo.SetActive(false);
        LoggerUtils.LogError(error);
    }

    private void InitUI()
    {
        // 绑定按钮点击事件
        _backButton.onClick.AddListener(OnBackButtonClick);
        _playButton.onClick.AddListener(OnPlayButtonClick);
        tog1.onValueChanged.AddListener((isOn) =>
        {
            OnToggleValueChanged(tog1.gameObject, isOn);
            _detailTab.SetActive(isOn);
        });
        tog2.onValueChanged.AddListener((isOn) =>
        {
            OnToggleValueChanged(tog2.gameObject, isOn);
            _rankTab.gameObject.SetActive(isOn);
            _rankTab.GetData(_curMapInfo.id);
        });
        OnToggleValueChanged(tog1.gameObject,true);
        // 初始化交互按钮
        if (_likeButton != null)
            _likeButton.BindInteractInfoView(_interactInfoView);
        if (_favoritesButton != null)
            _favoritesButton.BindInteractInfoView(_interactInfoView);

    }

    private void InitScrollView()
    {
        if (_curMapInfo == null || _curMapInfo.gameSetting == null || _curMapInfo.gameSetting.aIGameConfig == null) return;
        
        var aiGameConfig = _curMapInfo.gameSetting.aIGameConfig;
        var npcList = aiGameConfig.hospitalNPCs;

        Transform parent = _npcItemPrefab.transform.parent;

        for (int i = 0; i < npcList.Count; i++)
        {
            var npcObj = Instantiate(_npcItemPrefab,parent);
            npcObj.GetComponent<HospitalStudioNpcItem>();
            npcObj.SetData(npcList[i]);
            npcObj.gameObject.SetActive(true);
        }

    }

    private void InitUIData()
    {
        if (_rm_MapCover != null)
            _rm_MapCover.Load(_curMapInfo.cover, true, (param1,success) => 
            {
                if (_shareBtn != null&& success == true)
                {
                    _shareBtn.SetData(_curCreator.uid, _curMapInfo.id, _curInteractInfo.shareAmount);
                    //_shareBtn.SetLocalTex(_rm_MapCover.GetComponent<RawImage>().texture as Texture2D);
                }
            });

        if (_txt_Title != null)
            _txt_Title.text = _curMapInfo.name;

        //排名信息
        if (rankImage != null&& _curMapInfo.mapTitles != null)
        {
            rankImage.gameObject.SetActive(true);
            rankImage.sprite = GetSprite(_curMapInfo.mapTitles.titleId);
        }
        else
        {
            rankImage.gameObject.SetActive(false);
        }
        if (_txt_Desc != null)
            _txt_Desc.text = _curMapInfo.desc;

        // 用户信息
        if (_userInfoView != null)
        {
            _userInfoView.SetData(_curCreator);
            _userInfoView.SetUserNickLengthLimit(34);
        }

        // 关注按钮
        if (_followButton != null)
            _followButton.SetRelation(_curCreator.uid, _curRelationShipInfo);

        // 交互信息
        if (_interactInfoView != null)
            _interactInfoView.SetData(_curInteractInfo);

        // 点赞按钮
        if (_likeButton != null)
            _likeButton.SetData(_curMapInfo.id, _curInteractInfo.liked, _curInteractInfo.likeAmount);

        // 能量币按钮
        //if (_energyCoinButton != null)
        //{
        //    //_energyCoinButton.gameObject.SetActive(true);
        //    _energyCoinButton.SetData(_curCreator.uid, _curMapInfo.id, _curInteractInfo.rewardAmount);
        //}

        // 收藏按钮
        if (_favoritesButton != null)
            _favoritesButton.SetData(_curMapInfo.id, _curInteractInfo.collected, _curInteractInfo.collectAmount);

        // 评论按钮
        //if (_commentButton != null)
        //    _commentButton.SetData(_curMapInfo.id);

        // 举报按钮
        if (_reportButton != null)
            _reportButton.SetData(_curMapInfo.id, (int)ErrReportSceneType.UgcMap);

        if (AccountDataManager.Inst.IsSelf(_curCreator.uid))
        {
            Btn_Delete.gameObject.SetActive(true);
            Btn_Delete.onClick.RemoveAllListeners();
            Btn_Delete.onClick.AddListener(OnBtnDeleteClick);
            Btn_Eit.gameObject.SetActive(true);
            Btn_Eit.onClick.RemoveAllListeners();
            Btn_Eit.onClick.AddListener(OnBtnEditClick);
        }
        if (_assistBtn != null)
        {
            _assistBtn.SetData(_curCreator.uid, _curMapInfo.id, _curInteractInfo.heatAmount);
        }
    }

    private void OnBackButtonClick()
    {
        CloseSelf();
    }

    private void OnPlayButtonClick()
    {
        if (!GameController.IsInHallScene())
        {
            TipPanel.ShowToast("您已经在游戏内，请退出房间后再试");
            return;
        }
        // Debug.LogError(_curMapInfo.id + "");return;

        var updateState = (ForceUpdate)_curMapInfo.forceUpdate;
        if (updateState != ForceUpdate.Default)
        {
            UIManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
            return;
        }

        var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
        p.Init(_curMapInfo, _curCreator, LoadingType.Map);
        AIHospitalUtils.Inst.EnterUgcHospitalGame(_curMapInfo.id);
        
        CloseSelf();
    }

    public void OnToggleValueChanged(GameObject obj,bool isOn)
    {
        //obj.SetActive(isOn);
        var togSwitch = obj.GetComponent<CommonToggleSwitch>();
        togSwitch.SetSelectState(isOn);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // 解绑按钮事件
        if (_backButton != null) 
            _backButton.onClick.RemoveAllListeners();
        if (_playButton != null) 
            _playButton.onClick.RemoveAllListeners();
        if (_updateAction!=null)
        {
            _updateAction = null;
        }
    }
    
    private void OnBtnDeleteClick()
    {
        CommonConfirmPanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("确认删除", "你确定删除该作品吗？", "删除", "取消");
        commonConfirmPanel.SetThemeColor("#68CCBE", "#905CFF", "#68D896");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            var mapReq = new SetMapInfoReq()
            {
                mapInfo = _curMapInfo,
                setType = (int)SetType.Delete
            };
            string deleteHeadUrl = HttpUrlDefine.setMap;
            string reqParam = JsonConvert.SerializeObject(mapReq);

            NetworkManager.Inst.SendHttpRequest(deleteHeadUrl, HttpMethod.POST, reqParam, (content) =>
            {
                if (this != null)
                    CloseSelf();

                MessageHelper.Broadcast(MessageName.OnAssetDelete);
            }, null);
        }, null);
    }

    private void OnBtnEditClick()
    {
        CloseSelf();
        bool bUpdate = true;
        var panel = UIManager.Inst.OpenPanel<AIHospitalUgcPublishPanel>(PanelId.AIHospitalUgcPublishPanel, _curMapInfo, bUpdate);
        panel.SetUpdateMapAction(_updateAction);
    }

    public void SetUpdateAction(Action<string,bool> action)
    {
        _updateAction = null;
        _updateAction = action;
    }
    public Sprite GetSprite(int titleId)
    {
        if (titleId>=989&&titleId<=1000)
        { //mapTitle
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(titleAtlas, titleId.ToString(), rankImage.gameObject);
        }
        else 
        {
            rankImage.gameObject.SetActive(false);
            return null;
        }
    }

}
