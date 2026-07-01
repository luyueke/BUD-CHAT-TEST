using System;
using System.Collections.Generic;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using Game.MusicalInstrument;
using Game.Props.PropsManagers;
using Game.PropStore;
using Game.Store;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.PgcData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public enum AssetDetailType
{
    Null = 0,
    Prop = 1,
    Skin = 2,
    Mat = 3,
    Instrument = 4,
    MusicScore = 5,
    UgcBundle = 6,
    MusicTone = 7,
    Pet = 8,
    UgcAnim = 9,
    UgcPose = 10,
    UgcAnimMusic = 11,
    AINpc = 12,
    Vehicle = 13,
    Actor = 14,
    Theatre = 15,
    CabinCharacter = 16,
    CabinTone = 17,
    CharacterBox = 18,
}

public class AssetDetailPanel : BasePanel<AssetDetailPanel>
{
    public SpriteAtlas usedSA;
    public Image typeIcon;
    public Text txt_typeDesc;

    private SuperTextMesh _txtTitle;
    private SuperTextMesh _txtDesc;
    private CButton _btn_Close;
    private UserInfoView _userInfoView;
    private SuperTextMesh _Txt_userNick;
    private FollowButton _followButton;
    private CommentButton _commentButton;
    private LikeButton _likeButton;
    private CButton _characterDetailBtn;
    private EnergyCoinButton _energyCoinButton;
    private ReportButton _reportButton;
    private PurchaseButton _purchaseButton;
    private PreviewButton _previewButton;
    private RemoteImageBehaviour _rmoteCover;
    private CButton _btnEdit;
    private CButton _btnDelete;
    private CButton _btnBuild;
    private CButton _btnVehicle;
    private CText _txt_BuyCount;
    private SkinTicketButton _skinTicketButton;
    private GameObject _goLoading;
    private Transform bgParent;
    private Image titleBg;
    private Image extInfoRoot;
    private Text extInfoText;
    private Text _txt_NamePreview;

    private Image goloadingBg;
    private Image rightBg;
    private Image copyIdBtnImage;
    private GameObject animeTag;

    private AssetDetailType _curDetailType = AssetDetailType.Null;
    private string _curUgcId;

    private UgcBaseInfo _baseInfo;
    private CabinCharacterUgcInfo _cabinCharacterInfo;
    private BaseCreator _creatorInfo;
    private SkinInfo _skinInfo;
    private RelationShipInfo _relationShipInfo;
    private BaseInteractInfo _interactInfo;
    private PaymentInfo _paymentInfo;
    private DesignCode _designCode;
    private DetailRsp _curRspData;
    private Action onBuyUpdate;
    private GameObject tips;

    private Dictionary<AssetDetailType, string> _headUrlDict = new Dictionary<AssetDetailType, string>
    {
        { AssetDetailType.Prop, HttpUrlDefine.propInfo },
        { AssetDetailType.Skin, HttpUrlDefine.GetClothesInfo },
        { AssetDetailType.Mat, HttpUrlDefine.GetMaterialInfo },
        { AssetDetailType.Instrument, HttpUrlDefine.GetClothesInfo },
        { AssetDetailType.MusicScore, HttpUrlDefine.GetMusicScoreInfo },
        { AssetDetailType.UgcBundle, HttpUrlDefine.GetClothesInfo },
        { AssetDetailType.MusicTone, HttpUrlDefine.GetMusicTone },
        { AssetDetailType.UgcPose, HttpUrlDefine.GetPoseInfo },
        { AssetDetailType.UgcAnim, HttpUrlDefine.getAnimInfo },
        { AssetDetailType.UgcAnimMusic, HttpUrlDefine.getUgcAnimMusic },
        { AssetDetailType.AINpc, HttpUrlDefine.NpcInfo },
        { AssetDetailType.Vehicle, HttpUrlDefine.VehicleInfo },
        { AssetDetailType.Actor, HttpUrlDefine.ActorInfo },
        { AssetDetailType.Theatre, HttpUrlDefine.TheatreInfo },
        { AssetDetailType.CabinCharacter, HttpUrlDefine.CabinCharacterInfo },
        { AssetDetailType.CabinTone, HttpUrlDefine.CabinCharacterToneInfo },
        { AssetDetailType.CharacterBox, HttpUrlDefine.CharacterBoxInfoUrl },
    };

    public Action<int> likeNumDidChange;

    public override void OnCreate()
    {
        base.OnCreate();
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        _txtTitle = GameObjectEx.FindChildByName(this.transform, "Txt_TopTitle").GetComponent<SuperTextMesh>();
        _txtDesc = GameObjectEx.FindChildByName(this.transform, "Txt_Desc").GetComponent<SuperTextMesh>();
        _btn_Close = GameObjectEx.FindChildByName(this.transform, "Btn_Close").GetComponent<CButton>();
        _userInfoView = GameObjectEx.FindChildByName(this.transform, "UserInfoView").GetComponent<UserInfoView>();
        _Txt_userNick = GameObjectEx.FindChildByName(_userInfoView.transform, "NickName").GetComponent<SuperTextMesh>();
        _followButton = GameObjectEx.FindChildByName(this.transform, "FollowButton").GetComponent<FollowButton>();
        _commentButton = GameObjectEx.FindChildByName(this.transform, "CommentButton").GetComponent<CommentButton>();
        _purchaseButton = GameObjectEx.FindChildByName(this.transform, "PurchaseButton").GetComponent<PurchaseButton>();
        _likeButton = GameObjectEx.FindChildByName(this.transform, "LikeButton").GetComponent<LikeButton>();
        _characterDetailBtn = GameObjectEx.FindChildByName(this.transform, "CharacterDetailBtn").GetComponent<CButton>();
        _reportButton = GameObjectEx.FindChildByName(this.transform, "ReportButton").GetComponent<ReportButton>();
        _previewButton = GameObjectEx.FindChildByName(this.transform, "PreviewButton").GetComponent<PreviewButton>();
        _rmoteCover = GameObjectEx.FindChildByName(this.transform, "Remote_Cover").GetComponent<RemoteImageBehaviour>();
        _btnEdit = GameObjectEx.FindChildByName(this.transform, "Btn_Edit").GetComponent<CButton>();
        _btnDelete = GameObjectEx.FindChildByName(this.transform, "Btn_Delete").GetComponent<CButton>();
        _btnBuild = GameObjectEx.FindChildByName(this.transform, "BuildButton").GetComponent<CButton>();
        _btnVehicle = GameObjectEx.FindChildByName(this.transform, "PlayVehicleBtn").GetComponent<CButton>();
        _skinTicketButton = GameObjectEx.FindChildByName(this.transform, "SkinTicketButton").GetComponent<SkinTicketButton>();
        _txt_BuyCount = GameObjectEx.FindChildByName(this.transform, "Txt_BuyCount").GetComponent<CText>();
        _goLoading = GameObjectEx.FindChildByName(this.transform, "LoadingGo").gameObject;
        goloadingBg = _goLoading.transform.Find("Image1").GetComponent<Image>();
        _designCode = GameObjectEx.FindChildByName(this.transform, "DesignCode").GetComponent<DesignCode>();
        copyIdBtnImage = _designCode.transform.Find("CopyIdBtn").GetComponent<Image>();
        bgParent = GameObjectEx.FindChildByName(this.transform, "GemBG").GetComponent<Transform>();
        titleBg = GameObjectEx.FindChildByName(this.transform, "TopBarPanel").GetComponent<Image>();
        animeTag = titleBg.transform.Find("GameObject/AnimeTag").gameObject;
        rightBg = GameObjectEx.FindChildByName(this.transform, "RightBarBG").GetComponent<Image>();

        extInfoRoot = GameObjectEx.FindChildByName(this.transform, "ExtInfo").GetComponent<Image>();
        extInfoText = GameObjectEx.FindChildByName(this.transform, "ExtInfo/Text").GetComponent<Text>();
        _energyCoinButton = GameObjectEx.FindChildByName(this.transform, "EnergyCoinButton").GetComponent<EnergyCoinButton>();
        _txt_NamePreview = GameObjectEx.FindChildByName(this.transform, "NamePreview/Txt_NamePreview").GetComponent<Text>();
        tips = GameObjectEx.FindChildByName(this.transform, "Tip").gameObject;
        _btn_Close.onClick.AddListener(() =>
        {
            if (_curDetailType == AssetDetailType.MusicTone || _curDetailType == AssetDetailType.UgcAnimMusic)
            {
                _previewButton?.PauseTonePlay();
            }
            CloseSelf();
        });
        _btnEdit.onClick.AddListener(OnBtnEditClick);
        _btnDelete.onClick.AddListener(OnBtnDeleteClick);


        _likeButton.likeNumChange = i =>
        {
            likeNumDidChange?.Invoke(i);
            if (_curDetailType == AssetDetailType.Actor)
            {
                MessageHelper.Broadcast(MessageName.OnActorStudioPublishedListChange);
            }
            else if (_curDetailType == AssetDetailType.CabinCharacter)
            {
                MessageHelper.Broadcast<int>(MessageName.OnCabinCharacterLikeChange, i);
            }
        };
      
        _btnVehicle.onClick.AddListener(() =>
        {
            // 当前 OperationView 选中的商品就是 target；UGC载具的 VehicleInfo 在 AssetsData.UgcInfo.vehicleInfo 上
            if(_baseInfo is VehicleInfo vehicleInfo)
            {
                if (!GameController.IsInHallScene())
                {
                    TipPanel.ShowToast("在社区地图中无法进入试驾驶状态，请退出社区地图后重试");
                    return;
                }
                if (vehicleInfo == null)
                {
                    Debug.LogError("[OperationView] 当前未获取到选中载具的 VehicleInfo");
                    return;
                }
                string tempSpriteatlasPath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(tempSpriteatlasPath, "UGCVehicle_1", gameObject);
                var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
                p.Init(new VehicleInfo()
                {
                    name = vehicleInfo.name
                }, null, LoadingType.Vehicle, s: sprite);

                // 进图试玩载具：不打开 UGCVehicleEditPanel，直接打开 VehiclePlayPanel
                GameController.StartVehicleActionGame(EnterGameModel.UgcVehicleTryPlay, vehicleInfo);
            }

        });
        _btnVehicle.gameObject.SetActive(false);
        _characterDetailBtn.gameObject.SetActive(false);
        _characterDetailBtn.onClick.AddListener(OnCharacterDetailBtnClick);
    }

    

    private void SetUgcStyle(int ugcStyle)
    {
        var shaderStyle = (UgcShaderStyle)ugcStyle;
        bool isNormal = shaderStyle == UgcShaderStyle.Normal;
        titleBg.color = isNormal
            ? DataUtil.DeSerializeColorCheckHash("AD57FF")
            : DataUtil.DeSerializeColorCheckHash("4CDDBC");

        var bgColor = isNormal
            ? DataUtil.DeSerializeColorCheckHash("DAD0FF")
            : DataUtil.DeSerializeColorCheckHash("C0F5E9");
        goloadingBg.color = bgColor;
        rightBg.color = bgColor;
        _designCode.GetComponent<Image>().color = bgColor;
        copyIdBtnImage.color = isNormal
            ? DataUtil.DeSerializeColorCheckHash("925BFF")
            : DataUtil.DeSerializeColorCheckHash("4CDDBC");

        var textColor = isNormal
            ? DataUtil.DeSerializeColorCheckHash("905CFF")
            : DataUtil.DeSerializeColorCheckHash("4CDDBC");
        _designCode.codeName.color = textColor;
        _designCode.codeText.color = textColor;
        animeTag.SetActive(!isNormal);
    }


    public void InitBg(Transform bgParent, string bgColor, List<string> iconNames)
    {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgParent);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        item.InitCustomBgItem(bgColor, atlasPath, iconNames);
        item.gameObject.SetActive(true);
    }

    /// <summary>
    /// 所需要的参数
    /// args[0] AssetDetailType展示类型
    /// args[1] ugcId
    /// </summary>
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length >= 2)
        {
            _curDetailType = (AssetDetailType)args[0];
            _curUgcId = (string)args[1];
            string headUrl = HttpUrlDefine.propInfo;
            _headUrlDict.TryGetValue(_curDetailType, out headUrl);
            GetInfo(headUrl);
        }

        if (args.Length >= 3)
        {
            int ugcStyle = (int)args[2];
            SetUgcStyle(ugcStyle);
        }
 
    }

    public override void OnHidden()
    {
        base.OnHidden();
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
    }

    private void GetInfo(string headUrl)
    {
        JObject req = new JObject()
        {
            ["id"] = _curUgcId,
        };
        NetworkManager.Inst.SendHttpRequest(headUrl, HttpMethod.GET, JsonConvert.SerializeObject(req), OnGetInfoSuccess, OnGetInfoFail);
    }

    private void OnGetInfoSuccess(string content)
    {
        if (this.gameObject == null || string.IsNullOrEmpty(content))
        {
            return;
        }
        _btnVehicle.gameObject.SetActive(false);
        _characterDetailBtn.gameObject.SetActive(false);

        if (_curDetailType == AssetDetailType.CabinCharacter)
        {
            typeIcon.gameObject.SetActive(false);
            _previewButton.gameObject.SetActive(false);
            _energyCoinButton.gameObject.SetActive(true);
            _characterDetailBtn.gameObject.SetActive(true);

            var cabinRsp = JsonConvert.DeserializeObject<CabinCharacterDetailData>(content);

            if (cabinRsp?.characterInfo == null)
                return;

            _cabinCharacterInfo = cabinRsp.characterInfo;
            _creatorInfo = cabinRsp.creator;
            _interactInfo = cabinRsp.interactInfo;
            _relationShipInfo = cabinRsp.relationShipInfo;
            _paymentInfo = cabinRsp.characterInfo.paymentInfo;
            _skinInfo = new SkinInfo { subType = 0 };
            _baseInfo = _cabinCharacterInfo;
            _rmoteCover.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(cabinRsp.characterInfo.cover))
                _rmoteCover.Load(cabinRsp.characterInfo.cover);

            _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.Character);

            InitData();
            _purchaseButton.gameObject.SetActive(false);
            return;
        }

        if (_curDetailType == AssetDetailType.CabinTone)
        {
            typeIcon.gameObject.SetActive(false);
            _previewButton.gameObject.SetActive(false);
            _energyCoinButton.gameObject.SetActive(true);

            var cabinToneRsp = JsonConvert.DeserializeObject<CabinCharacterToneDetailData>(content);

            if (cabinToneRsp?.characterToneInfo == null)
                return;

            _creatorInfo = cabinToneRsp.creator;
            _interactInfo = cabinToneRsp.interactInfo;
            _relationShipInfo = cabinToneRsp.relationShipInfo;
            _paymentInfo = cabinToneRsp.characterToneInfo.paymentInfo;
            _skinInfo = new SkinInfo { subType = 0 };
            // CabinToneInfo 继承自 UgcBaseInfo，可直接赋给 _baseInfo
            _baseInfo = cabinToneRsp.characterToneInfo;
            _rmoteCover.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(cabinToneRsp.characterToneInfo.cover))
                _rmoteCover.Load(cabinToneRsp.characterToneInfo.cover);

            _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcMusicTone);

            InitData();
            return;
        }

        if (_curDetailType == AssetDetailType.CharacterBox)
        {
            typeIcon.gameObject.SetActive(false);
            _previewButton.gameObject.SetActive(true);
            _energyCoinButton.gameObject.SetActive(true);

            var boxRsp = JsonConvert.DeserializeObject<CharacterBoxDetailData>(content);
            if (boxRsp?.characterBoxInfo == null) return;
            _baseInfo = boxRsp.characterBoxInfo;
            _creatorInfo = boxRsp.creator;
            _interactInfo = boxRsp.interactInfo;
            _relationShipInfo = boxRsp.relationShipInfo;
            _paymentInfo = boxRsp.characterBoxInfo.paymentInfo;
            _skinInfo = new SkinInfo { subType = 0 };
            _previewButton.SetData(_curDetailType, _baseInfo, boxRsp.characterBoxInfo);
            _rmoteCover.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(boxRsp.characterBoxInfo.cover))
                _rmoteCover.Load(boxRsp.characterBoxInfo.cover);

            _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UGCSence);

            InitData();
            return;
        }

        _curRspData = JsonConvert.DeserializeObject<DetailRsp>(content);
        _creatorInfo = _curRspData.creator;
        _interactInfo = _curRspData.interactInfo;
        _relationShipInfo = _curRspData.relationShipInfo;
        _skinInfo = _curRspData.skinInfo;
        typeIcon.gameObject.SetActive(false);
        _energyCoinButton.gameObject.SetActive(true);

        if (_curDetailType == AssetDetailType.Skin)
        {
            if (_curRspData.skinInfo.subType == (int)AvatarSubType.MusicalInstrument)
            {
                _curDetailType = AssetDetailType.Instrument;
            }
            else if (_curRspData.skinInfo.subType == (int)AvatarSubType.Bundle)
            {
                _curDetailType = AssetDetailType.UgcBundle;
            }
        }

        switch (_curDetailType)
        {
            case AssetDetailType.Skin:
                _baseInfo = _curRspData.skinInfo;
                _previewButton.SetData(_curDetailType, _curRspData.skinInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcSkin);
                _paymentInfo = _curRspData.skinInfo.paymentInfo;
                typeIcon.gameObject.SetActive(true);
                if (_curRspData.skinInfo.isPrivateOrder == 1)
                {
                    typeIcon.sprite = usedSA.GetSprite("UgcPrivateIcon");
                }
                else
                {
                    switch ((SkinType)_curRspData.skinInfo.skinType)
                    {
                        case SkinType.Pet:
                            typeIcon.sprite = usedSA.GetSprite("Pet" + $"{(AvatarSubType)_curRspData.skinInfo.subType}");
                            break;

                        case SkinType.Avatar:
                        default:
                            typeIcon.sprite = usedSA.GetSprite($"{(AvatarSubType)_curRspData.skinInfo.subType}");
                            break;
                    }
                }
                SetUgcStyle(_curRspData.skinInfo.ugcStyle);
                break;
            case AssetDetailType.Prop:
                _baseInfo = _curRspData.propInfo;
                _previewButton.SetData(_curDetailType, _curRspData.propInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcProp);
                _paymentInfo = _curRspData.propInfo.paymentInfo;
                SetUgcStyle(_curRspData.propInfo.ugcStyle);
                break;
            case AssetDetailType.Mat:
                _baseInfo = _curRspData.materialInfo;
                _previewButton.SetData(_curDetailType, _curRspData.materialInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcMaterial);
                _paymentInfo = _curRspData.materialInfo.paymentInfo;
                SetUgcStyle(_curRspData.materialInfo.ugcStyle);
                break;
            case AssetDetailType.Instrument:
                _baseInfo = _curRspData.skinInfo;
                _previewButton.SetData(_curDetailType, _curRspData.skinInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcSkin);
                _paymentInfo = _curRspData.skinInfo.paymentInfo;
                typeIcon.gameObject.SetActive(true);
                typeIcon.sprite = usedSA.GetSprite($"{(AvatarSubType)_curRspData.skinInfo.subType}");
                SetUgcStyle(_curRspData.skinInfo.ugcStyle);
                break;
            case AssetDetailType.MusicScore:
                _baseInfo = _curRspData.musicScoreInfo;
                _previewButton.SetData(_curDetailType, _curRspData.musicScoreInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcMusicScore);
                _paymentInfo = _curRspData.musicScoreInfo.paymentInfo;
                break;
            case AssetDetailType.UgcBundle:
                _baseInfo = _curRspData.skinInfo;
                _previewButton.SetData(_curDetailType, _curRspData.skinInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcSkin);
                _paymentInfo = new PaymentInfo() { price = _curRspData.skinInfo.paymentInfo.price, currencyType = _curRspData.skinInfo.paymentInfo.currencyType };
                _interactInfo.consumed = 1;
                foreach (var sub in _curRspData.skinInfo.bundleItems)
                {
                    var subSkin = JsonConvert.DeserializeObject<SkinInfo>(sub);
                    if (AssetsDataManager.IsOwned(subSkin.id)) _paymentInfo.price -= subSkin.paymentInfo.price;
                    else _interactInfo.consumed = 0;
                }
                if (_paymentInfo.price != _curRspData.skinInfo.paymentInfo.price)
                {
                    _purchaseButton.ShowOriginPrice(_curRspData.skinInfo.paymentInfo);
                }

                typeIcon.gameObject.SetActive(true);
                typeIcon.sprite = usedSA.GetSprite($"{(AvatarSubType)_curRspData.skinInfo.subType}");
                SetUgcStyle(_curRspData.skinInfo.ugcStyle);
                break;
            case AssetDetailType.MusicTone:
                _baseInfo = _curRspData.musicToneInfo;
                _previewButton.SetData(_curDetailType, _curRspData.musicToneInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcMusicTone);
                _paymentInfo = _curRspData.musicToneInfo.paymentInfo;
                extInfoRoot.gameObject.SetActive(true);
                extInfoText.text = _curRspData.musicToneInfo.toneType == (int)ToneType.Fifteen ? "15音" : "22音";
                break;
            case AssetDetailType.UgcPose:
                _baseInfo = _curRspData.poseInfo;
                _previewButton.SetData(_curDetailType, _curRspData.poseInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcPose);
                _paymentInfo = _curRspData.poseInfo.paymentInfo;
                typeIcon.gameObject.SetActive(true);
                _skinInfo = new SkinInfo
                {
                    subType = 1008
                };
                switch ((EmoteSubType)_curRspData.poseInfo.poseType)
                {
                    case EmoteSubType.Single:
                    case EmoteSubType.Double:
                        typeIcon.sprite = usedSA.GetSprite("UgcPose");
                        break;

                    case EmoteSubType.PetSingle:
                    case EmoteSubType.PetWithPlayer:
                        typeIcon.sprite = usedSA.GetSprite("UgcPose_Pet");
                        break;
                }
                break;
            case AssetDetailType.UgcAnim:
                _baseInfo = _curRspData.animInfo;
                _previewButton.SetData(_curDetailType, _curRspData.animInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcAnimation);
                _paymentInfo = _curRspData.animInfo.paymentInfo;
                typeIcon.gameObject.SetActive(true);
                _skinInfo = new SkinInfo
                {
                    subType = 1007
                };
                switch ((EmoteSubType)_curRspData.animInfo.animType)
                {
                    case EmoteSubType.Single:
                    case EmoteSubType.Double:
                        typeIcon.sprite = usedSA.GetSprite("UgcAnim");
                        break;

                    case EmoteSubType.PetSingle:
                    case EmoteSubType.PetWithPlayer:
                        typeIcon.sprite = usedSA.GetSprite("UgcAnim_Pet");
                        break;
                }

                var desc = _curRspData.animInfo.loop == 1 ? "循环动作" : "非循环动作";
                txt_typeDesc.text = desc;
                break;
            case AssetDetailType.UgcAnimMusic:
                _baseInfo = _curRspData.animMusicInfo;
                _previewButton.SetData(_curDetailType, _curRspData.animMusicInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcAnimationMusic);
                _paymentInfo = _curRspData.animMusicInfo.paymentInfo;
                extInfoRoot.gameObject.SetActive(false);
                _rmoteCover.gameObject.SetActive(false);
                _txt_NamePreview.transform.parent.gameObject.SetActive(true);
                _txt_NamePreview.SetText(_baseInfo.name);
                break;
            case AssetDetailType.AINpc:
                _baseInfo = _curRspData.npc;
                _previewButton.SetData(_curDetailType, _curRspData.npc);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.AINpc);
                _paymentInfo = _curRspData.npc.paymentInfo;
                break;
            case AssetDetailType.Actor:
                _baseInfo = _curRspData.actorInfo;
                _previewButton.SetData(_curDetailType, _curRspData.actorInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.Actor);
                _paymentInfo = _curRspData.actorInfo.paymentInfo;
                //_purchaseButton.ShowOriginPrice(_curRspData.actorInfo.paymentInfo);
                _skinInfo = new SkinInfo { subType = 0 };
                _rmoteCover.gameObject.SetActive(true);
                var firstClothesUrl = _curRspData.actorInfo.avatarClothes?.Count > 0
                    ? _curRspData.actorInfo.avatarClothes[0].clothesURL
                    : null;
                if (!string.IsNullOrEmpty(firstClothesUrl))
                    _rmoteCover.Load(firstClothesUrl);
                break;
            case AssetDetailType.Vehicle:
                _baseInfo = _curRspData.vehicleInfo;
                _previewButton.SetData(_curDetailType, _curRspData.vehicleInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.UgcVehicle);
                _paymentInfo = _curRspData.vehicleInfo.paymentInfo;
                typeIcon.gameObject.SetActive(true);
                _skinInfo = new SkinInfo
                {
                    subType = 1011
                };
                var vehicleType = _curRspData.vehicleInfo.vehicleType == (int)VehicleType.Single ? "单人载具" : "双人载具";
                txt_typeDesc.text = vehicleType;
                typeIcon.sprite = usedSA.GetSprite("Vehicle");
                _btnVehicle.gameObject.SetActive(true);
                break;
            case AssetDetailType.Theatre:
                _baseInfo = _curRspData.theaterInfo;
                _previewButton.SetData(_curDetailType, _curRspData.theaterInfo);
                _reportButton.SetData(_curUgcId, (int)ErrReportSceneType.Theater);
                _paymentInfo = _curRspData.theaterInfo.paymentInfo;
                _skinInfo = new SkinInfo { subType = 1015 };
                _rmoteCover.GetComponent<RectTransform>().sizeDelta = new Vector2(512, 288);
                break;
        }

        InitData();
    }

    private void OnGetInfoFail(string error)
    {
        LoggerUtils.LogError(error);
    }

    private void InitData()
    {
        // 防御性检查：若 _baseInfo 为 null（服务端返回数据缺失或未覆盖的新类型），直接中止，避免 NullReferenceException
        if (_baseInfo == null)
        {
            LoggerUtils.LogError($"[AssetDetailPanel] InitData: _baseInfo 为 null，detailType={_curDetailType}，跳过初始化");
            return;
        }

        AccountUserInfo accountUserInfo = _creatorInfo;
        var uid = _creatorInfo?.uid;
        var cover = _baseInfo?.cover;

        bool isSelf = uid == AccountDataManager.Inst.Uid;
        _btnEdit.gameObject.SetActive(isSelf);
        _btnDelete.gameObject.SetActive(isSelf);
        if (isSelf && _curDetailType == AssetDetailType.Actor) _purchaseButton.gameObject.SetActive(false);

        _txtTitle.text = _baseInfo.name;
        _txtDesc.text = _baseInfo.desc;
        _userInfoView.SetData(accountUserInfo);
        _Txt_userNick.text = GameUtils.SubStringByBytes(accountUserInfo.nickname, 22);
        _followButton.SetRelation(uid, _relationShipInfo);
        _commentButton.SetData(_curUgcId);
        if(_paymentInfo != null)
        {
            _purchaseButton.SetData(_baseInfo, _interactInfo?.consumed, _paymentInfo, _skinInfo);
        }
        _purchaseButton.SetBuyUpdate(onBuyUpdate);
        if (_paymentInfo != null && _paymentInfo.price != 0)
        {
            if (_paymentInfo.currencyType == CurrencyType.PinkCoin)
            {
                if (AnniversaryMonthCardMgr.Inst.IsAnyMonthCardActive() || _skinInfo?.subType == 1015) //剧本跳过月卡
                {
                    if (_skinInfo == null || !ChekcUseTicket(_skinInfo.subType, _paymentInfo.price))
                    {
                        var nowPrice = (_paymentInfo.price * AnniversaryMonthCardMgr.Inst.GetDiscountRate());
                        nowPrice = Mathf.Round(nowPrice * 10f) / 10f;
                        if (Mathf.Approximately(nowPrice, Mathf.Floor(nowPrice)))
                        {
                            nowPrice = Mathf.Floor(nowPrice);
                        }
                        _purchaseButton.ShowOriginAndNowPrice(_paymentInfo.price, nowPrice);
                    }
                }
            }
        }

        if (_interactInfo != null)
        {
            _txt_BuyCount.text = GameUtils.ToBudCommonNumString(_interactInfo.consumeAmount);
            var txtRtComp = _txt_BuyCount.GetComponent<RectTransform>();
            if (txtRtComp)
                LayoutRebuilder.ForceRebuildLayoutImmediate(txtRtComp);
        }

        var likeUgcId = (_curDetailType == AssetDetailType.CabinCharacter
                         && !string.IsNullOrEmpty(_cabinCharacterInfo?.targetUgcId))
            ? _cabinCharacterInfo.targetUgcId
            : _curUgcId;
        //Debug.LogError(_cabinCharacterInfo.targetUgcId);
        _likeButton.SetData(likeUgcId, _interactInfo?.liked ?? 0, _interactInfo?.likeAmount ?? 0);
        if(_interactInfo != null)
            _energyCoinButton.SetData(uid, _curUgcId, _interactInfo.rewardAmount);
        _reportButton.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(cover))
        {
            _rmoteCover.Load(cover);
        }
        _designCode.SetCodeInfo(_baseInfo, _paymentInfo);
        RefreshBuildButton();

        _goLoading.SetActive(false);
        tips.SetActive(_curDetailType != AssetDetailType.Actor && _curDetailType != AssetDetailType.Theatre);

        if (_paymentInfo?.currencyType == CurrencyType.Gem)
        {
            bgParent.gameObject.SetActive(true);
            InitBg(bgParent, "#AC8FEF", new List<string>() { "detail_bg_1", "detail_bg_2", "detail_bg_3" });
            titleBg.color = DataUtil.DeSerializeColorCheckHash("#8250EE");
        }
    }

    public bool ChekcUseTicket(int subType, int price)
    {
        switch (subType)
        {
            case 0:
            case 1008://姿势和乐谱不能用卷
                return false;
            case 1007: //动作卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0
                    || price > 200) return false;
                return true;
            case 24: //乐器卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                    || price > 200) return false;
                return true;
            case 1011: //载具卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                    || price > 200) return false;
                return true;
            case 1015: //载具卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                    || price >350) return false;
                return true;
            default: //其余都算皮肤卷
                if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                    || price > 60) return false;
                return true;
        }
    }

    private void RefreshBuildButton()
    {
        if (UIManager.Inst.TryFindPanel(PanelId.PropStorePanel, out PropStorePanel propStorePanel))
        {
            return;
        }
        if (_curDetailType == AssetDetailType.Prop && GameController.GetCurrentGameMode() == GameMode.Edit)
        {
            _btnBuild.onClick.RemoveAllListeners();
            _btnBuild.onClick.AddListener(() =>
            {
                GlobalNodeManager.Inst.Get<PropManager>().Create(_curRspData.propInfo, behaviour =>
                {
                    if (behaviour != null)
                    {
                        UI.Manager.InputHandlerManager.Inst.SelectEntity(behaviour.entity);
                    }
                    CloseSelf();
                });
            });

            //已经拥有
            if (_interactInfo.consumed == 1)
            {
                _purchaseButton.gameObject.SetActive(false);
                _btnBuild.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 购买刷新列表
    /// </summary>
    /// <param name="buyUpdate"></param>
    public void SetBuyUpdate(Action buyUpdate)
    {
        this.onBuyUpdate = buyUpdate;
    }


    private void OnCharacterDetailBtnClick()
    {
        CabinRolesNetManager.Inst.OpenPanelWithFreshData(_cabinCharacterInfo.id, isFromShop: true);
    }

    private void OnBtnEditClick()
    {
        var editPanel = UIManager.Inst.OpenPanel<UGCEditInfoPanel>(PanelId.UGCEditInfoPanel, _baseInfo, _curRspData?.skinActionInfo, _cabinCharacterInfo);
        editPanel.SetOnEditSuccessAct((info) =>
        {
            this._baseInfo = info;
            InitData();
        });
    }

    private void OnBtnDeleteClick()
    {
        CommonConfirmWithTitlePanel commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmWithTitlePanel>(PanelId.CommonConfirmWithTitlePanel);
        commonConfirmPanel.SetLocalText("确认删除", "你确定删除该作品吗？", "删除", "取消");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            string deleteHeadUrl = "";
            string reqParam = "";
            switch (_curDetailType)
            {
                case AssetDetailType.Skin:
                    var skinReq = new SetSkinInfoReq
                    {
                        skinInfo = _curRspData.skinInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.SetSkin;
                    reqParam = JsonConvert.SerializeObject(skinReq);
                    break;
                case AssetDetailType.Prop:
                    var propReq = new SetPropInfoReq()
                    {
                        propInfo = _curRspData.propInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.setProp;
                    reqParam = JsonConvert.SerializeObject(propReq);
                    break;
                case AssetDetailType.Mat:
                    var matReq = new SetMaterialInfoReq()
                    {
                        materialInfo = _curRspData.materialInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.SetMaterial;
                    reqParam = JsonConvert.SerializeObject(matReq);
                    break;
                case AssetDetailType.Instrument:
                    var instrumentReq = new SetSkinInfoReq()
                    {
                        skinInfo = _curRspData.skinInfo,
                        SkinActionInfo = _curRspData.skinActionInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.SetSkin;
                    reqParam = JsonConvert.SerializeObject(instrumentReq);
                    break;
                case AssetDetailType.MusicScore:
                    var musicScoreReq = new SetMusicScoreInfoReq()
                    {
                        musicScoreInfo = _curRspData.musicScoreInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.SetMusicScore;
                    reqParam = JsonConvert.SerializeObject(musicScoreReq);
                    break;
                case AssetDetailType.UgcBundle:
                    var ugcBundleReq = new SetSkinInfoReq
                    {
                        skinInfo = _curRspData.skinInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.SetSkin;
                    reqParam = JsonConvert.SerializeObject(ugcBundleReq);
                    break;
                case AssetDetailType.MusicTone:
                    EditToneInfoReq muiscToneReq = new EditToneInfoReq()
                    {
                        musicToneInfo = _curRspData.musicToneInfo,
                        setType = (int)SetType.Delete
                    };

                    deleteHeadUrl = HttpUrlDefine.SetUGCTone;
                    reqParam = JsonConvert.SerializeObject(muiscToneReq);
                    break;

                case AssetDetailType.UgcAnim:
                    SetAnimInfoReq ugcAnimInfoReq = new SetAnimInfoReq()
                    {
                        animInfo = _curRspData.animInfo,
                        setType = (int)SetType.Delete
                    };

                    deleteHeadUrl = HttpUrlDefine.setAnim;
                    reqParam = JsonConvert.SerializeObject(ugcAnimInfoReq);
                    break;

                case AssetDetailType.UgcPose:
                    SetAnimInfoReq ugcPoseInfoReq = new SetAnimInfoReq()
                    {
                        poseInfo = _curRspData.poseInfo,
                        setType = (int)SetType.Delete
                    };

                    deleteHeadUrl = HttpUrlDefine.SetPose;
                    reqParam = JsonConvert.SerializeObject(ugcPoseInfoReq);
                    break;

                case AssetDetailType.UgcAnimMusic:
                    SetUgcAnimMusicReq ugcAnimMusicReq = new SetUgcAnimMusicReq()
                    {
                        animMusicInfo = _curRspData.animMusicInfo,
                        setType = (int)SetType.Delete
                    };

                    deleteHeadUrl = HttpUrlDefine.setUgcAnimMusic;
                    reqParam = JsonConvert.SerializeObject(ugcAnimMusicReq);
                    break;

                case AssetDetailType.AINpc:
                    SetAINpcInfoReq aiNpcInfoRep = new SetAINpcInfoReq()
                    {
                        npc = _curRspData.npc,
                        setType = (int)SetType.Delete
                    };

                    deleteHeadUrl = HttpUrlDefine.NpcSet;
                    reqParam = JsonConvert.SerializeObject(aiNpcInfoRep);
                    break;
                case AssetDetailType.Actor:
                    SetActorInfoReq setActorInfoReq = new SetActorInfoReq()
                    {
                        actorInfo = _curRspData.actorInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.ActorSet;
                    reqParam = JsonConvert.SerializeObject(setActorInfoReq);
                    break;
                case AssetDetailType.Vehicle:
                    SetVehicleReq setVehicleReq = new SetVehicleReq()
                    {
                        vehicleInfo = _curRspData.vehicleInfo,
                        setType = (int)SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.SetVehicle;
                    reqParam = JsonConvert.SerializeObject(setVehicleReq);
                    break;
                case AssetDetailType.CharacterBox:
                    CharacterBoxSetRequestData boxSetRequestData = new CharacterBoxSetRequestData()
                    {
                        characterBoxInfo = (CharacterBoxInfo)_baseInfo,
                        setType = SetType.Delete
                    };
                    deleteHeadUrl = HttpUrlDefine.CharacterBoxSet;
                    reqParam = JsonConvert.SerializeObject(boxSetRequestData);
                    break;
            }

            NetworkManager.Inst.SendHttpRequest(deleteHeadUrl, HttpMethod.POST, reqParam, (content) =>
            {
                if (this != null)
                    CloseSelf();

                MessageHelper.Broadcast(MessageName.OnAssetDelete);
            }, null);
        }, null);
    }

    private void OnBuyUgcItemSuccess(string ugcId)
    {
        if (ugcId == _curUgcId)
        {
            _interactInfo.consumed = 1;
            _purchaseButton.SetData(_baseInfo, _interactInfo.consumed, _paymentInfo, _skinInfo);
            RefreshBuildButton();
        }
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        this.gameObject.SetActive(true);
    }

    public override void OnWindowBeCovered(bool isCover)
    {
        base.OnWindowBeCovered(isCover);
        this.gameObject.SetActive(false);
    }
}
