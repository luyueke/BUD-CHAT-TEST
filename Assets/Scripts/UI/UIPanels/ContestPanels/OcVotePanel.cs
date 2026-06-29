using System;
using Basic.Utils;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Avatar;
using Game.Store;
using Game.Utils;
using GameData;
using GameData.Base;
using GameData.Base.Common;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Game.Pet;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.ProfilePanel;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class OcVotePanel : BasePanel<OcVotePanel>
{
    [SerializeField] internal SpriteAtlas usedSA;
    public Transform _transBG;
    public CButton _btnBack;
    public Image descImage;
    public Text desc;
    public Button VoteButton;
    public Text VoteText;
    public Button ugcHeadRoot;
    public HeadViewWidget HeadViewWidget;
    public FittingRoomAdapter list;
    private ContestInfo mData;
    private ContestCreationInfo mOcData;
    private ContestOCInfoRsp ocInfo;

    public Transform characterRoot;
    public AvatarCameraController avatarCameraController;

    internal PlayerAnimationCtrl animationCtrl;
    internal PetAnimationCtrl petAnimationCtrl;

    private ContestRewardItem firstItem;
    private List<GoodsData> goodsDatas;

    public Action<int> voteNumDidChange;

    public override void OnCreate()
    {
        base.OnCreate();
        _transBG = GameObjectEx.FindChildByName(this.transform, "Trans_BG");
        _btnBack = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _btnBack.onClick.AddListener(CloseSelf);
        VoteButton.gameObject.SetActive(false);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        mData = (ContestInfo)args[0];
        mOcData = (ContestCreationInfo)args[1];

        if (mData.themeColorList != null && mData.themeColorList.Count >= 2)
        {
            ColorUtility.TryParseHtmlString(mData.themeColorList[0], out list.BgColor);
            ColorUtility.TryParseHtmlString(mData.themeColorList[1], out list.SelectedColor);
            descImage.color = list.SelectedColor;
        }

        InitUI();
        StartPreview();

        list.Data = new LazyDataHelper<GoodsData>(list, CreateNewModel);
        list.OnItemSelected = OnItemSelected;
    }

    public bool IsPet
    {
        get
        {
            return mData?.CurrentContestType == BUDContestType.PetOC;
        }
    }

    public GoodsData CreateNewModel(int index)
    {
        var assetsData = goodsDatas[index];
        assetsData.Selected = true;
        return assetsData;
    }

    public override void OnHidden()
    {
        base.OnHidden();
        
        if (animationCtrl != null)
        {
            animationCtrl?.ResetEmoteForUICharacter();
        }
        petAnimationCtrl?.ResetEmoteForUICharacter();
    }

    public void StartPreview()
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ContestOcInfo, HttpMethod.GET, JsonConvert.SerializeObject(new JObject() { ["ocId"] = mOcData.creationId, ["contestId"] = mData.contestId }), (response) =>
        {
            if (this == null)
            {
                return;
            }
            
            ocInfo = JsonConvert.DeserializeObject<ContestOCInfoRsp>(response);
            InitPreviewWarpper(ocInfo);
            
            InitOcList();
            InitVoteButton();
        }, (fail) =>
        {
            //TipPanel.ShowToast("");
        });
    }

    private void InitPreviewWarpper(ContestOCInfoRsp rsp)
    {
        if (rsp == null)
        {
            return;
        }

        var avatarJson = rsp.ocInfo.baseInfo.avatarJson;
        if (IsPet)
        {
            var petWrapper = PetAvatarController.Inst.CreateUIAvatar(PetData.DeserializeObject(avatarJson));
            petWrapper.SetParent(characterRoot, true);
            petAnimationCtrl = petWrapper.Avatar.GetComponentInChildren<PetAnimationCtrl>();
            characterRoot.localScale = Vector3.one * 1.32f;
            characterRoot.localPosition = new Vector3(0, -0.5f, 0);

            avatarCameraController.RotateTarget = characterRoot;   
        }
        else
        {
            var characterWrap = AvatarController.Inst.CreateUIAvatar(CharacterData.DeserializeObject(avatarJson));
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;   
        }
    }
    
    private void InitOcList()
    {
        var dataHandler = AssetsDataManager.GetData<OcContestHandler>();
        if (IsPet)
        {
            dataHandler.UpdatePetData();
        }
        dataHandler.AddDataChange(gameObject, OnDataChange);
        goodsDatas = dataHandler.GetGoodsDataInOc(ocInfo.ocInfo.skinInfos, IsPet);
        list.Data.ResetItems(goodsDatas.Count);
        HeadViewWidget.InitHeadCycle(ocInfo.creator);
        VoteButton.onClick.RemoveAllListeners();
        ugcHeadRoot.onClick.AddListener(() => { UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, ocInfo.creator.uid); });
    }

    private void InitVoteButton()
    {
        VoteButton.gameObject.SetActive(true);
        desc.text = $"{ocInfo.interactInfo.likeAmount}";
        if (mData.status == (int)ContestStatus.NotStart)
        {
            VoteButton.interactable = false;
            VoteText.SetLocalText("未开始");
        }
        else if (mData.status == (int)ContestStatus.InProgress)
        {
            VoteButton.interactable = ocInfo.interactInfo.liked == 0;
            VoteText.SetLocalText(ocInfo.interactInfo.liked == 0 ? "投票" : "已投票");
            VoteButton.onClick.RemoveAllListeners();
            VoteButton.onClick.AddListener(OnVoteClick);
        }
        else
        {
            VoteButton.interactable = false;
            VoteText.SetLocalText("已结束");
        }

    }

    private bool isRequesting;
    private void OnVoteClick()
    {
        if (isRequesting) return;
        var req = new SetTypeReqeust
        {
            id = ocInfo.ocInfo.baseInfo.ocId,
            setType = (int)UGCCommonReq.LikeType.Like
        };
        isRequesting = true;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCLike, HttpMethod.POST, JsonConvert.SerializeObject(req), response =>
        {
            ocInfo.interactInfo.liked = 1;
            ocInfo.interactInfo.likeAmount++;
            InitVoteButton();
            voteNumDidChange?.Invoke(ocInfo.interactInfo.likeAmount);
        }, fail =>
        {
            LoggerUtils.LogError($"大赛点赞Oc失败 [{ocInfo.ocInfo.baseInfo.ocId}]:" + fail);
            isRequesting = false;
        });
    }

    private void OnDataChange(AssetsData[] assets)
    {
        list.Data.ResetItems(goodsDatas.Count);
    }

    private void OnItemSelected(GoodsData data)
    {
        switch (data.GoodsType)
        {
            case GoodsType.SingleUgc:
                int ugcStyle = AssetsDataManager.GetUgcStyle(data);
                UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.Id,ugcStyle);
                break;
            case GoodsType.SinglePgc:
                //UIManager.Inst.SwapPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, data.Id);
                break;
        }
    }

    private void InitUI()
    {
        if (_transBG == null)
        {
            return;
        }

        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/RemoteIconBg.prefab")
            .Instantiate(_transBG);
        var item = itemObj.GetComponent<RemoteIconBgPanel>();
        item.InitCustomBgItem(mData.backgroundColor, mData.backgroundIconUrlList);
        item.gameObject.SetActive(true);
    }
}

public class ContestOCInfoRsp
{
    public AccountUserInfo creator;
    public BaseInteractInfo interactInfo;
    public ContestOcInfo ocInfo;
}

public class ContestOcInfo
{
    public OcInfo baseInfo;
    public List<OcSkinInfo> skinInfos;
}
