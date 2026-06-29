using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UGCEditInfoPanel : BasePanel<UGCEditInfoPanel>
{
    [SerializeField] private Transform BG;

    [SerializeField] private CButton backBtn;
    [SerializeField] private LoadingButton nextBtn;
    [SerializeField] private Image nextBtnImg;
    [SerializeField] private RemoteImageBehaviour cover;

    [SerializeField] private CButton nameEditBtn;
    [SerializeField] private SuperTextMesh nameText;
    [SerializeField] private GameObject emptyNameObj;
    [SerializeField] private Text nameLimitText;
    private KeyBoardInfo nameKeyBoardInfo;
    private int NameLimitCount = 30;

    [SerializeField] private CButton descriptionEditBtn;
    [SerializeField] private SuperTextMesh descriptionText;
    [SerializeField] private GameObject emptyDescriptionObj;
    [SerializeField] private Text descLimitText;
    private KeyBoardInfo descriptionKeyBoardInfo;
    private int DescLimitCount = 250;

    private UgcBaseInfo _ugcBaseInfo;
    private SkinActionInfo _skinActionInfo;
    private CabinCharacterUgcInfo _cabinCharacterInfo;
    private Action<UgcBaseInfo> _onEditSuccessAct;

    public override void OnCreate()
    {
        base.OnCreate();
        Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/AvatarBg.prefab").Instantiate(BG);
        AddListener();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _ugcBaseInfo = (UgcBaseInfo)args[0];
        _skinActionInfo = (SkinActionInfo)args[1];
        _cabinCharacterInfo = args.Length > 2 ? args[2] as CabinCharacterUgcInfo : null;
        SyncNameLength();
        SyncEditData();
        SetNextEnabled(false);
    }

    private void SyncNameLength()
    {
        if (_ugcBaseInfo is (AnimInfo) || _ugcBaseInfo is (PoseInfo) )
        {
            NameLimitCount = 6;
        }
        else if (_ugcBaseInfo is (AnimMusicInfo))
        {
            NameLimitCount = 5;
        }
    }

    private void AddListener()
    {
        nextBtn.onClick.AddListener(OnNextBtnClick);
        backBtn.onClick.AddListener(CloseSelf);
        nameEditBtn.onClick.AddListener(OnNameEditBtnClick);
        descriptionEditBtn.onClick.AddListener(OnDescriptionEditBtnClick);


        nameKeyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = NameLimitCount,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };

        descriptionKeyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = DescLimitCount,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };
    }

    private void OnDescriptionEditBtnClick() {
        descriptionKeyBoardInfo.defaultText = _ugcBaseInfo.desc;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(descriptionKeyBoardInfo));
    }

    private void OnGetDescFromNative(string desc) {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _ugcBaseInfo.desc = desc;
        SyncEditData();
    }

    private void OnNameEditBtnClick() {
        nameKeyBoardInfo.defaultText = _ugcBaseInfo.name;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(nameKeyBoardInfo));
    }

    private void OnGetNameFormNative(string value) {
        if(value.Length > NameLimitCount)
        {
            TipPanel.ShowToast("字数超出限制");
            return;
        }
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        _ugcBaseInfo.name = value;
        SyncEditData();
    }

    private void SyncEditData() {
        if (string.IsNullOrEmpty(_ugcBaseInfo.name)) {
            emptyNameObj.SetActive(true);
            nameText.gameObject.SetActive(false);
            nameLimitText.text = $"0/{NameLimitCount}";
        } else {
            nameText.text = _ugcBaseInfo.name;
            emptyNameObj.SetActive(false);
            nameText.gameObject.SetActive(true);
            nameLimitText.text = $"{_ugcBaseInfo.name.Length}/{NameLimitCount}";
        }

        if (string.IsNullOrEmpty(_ugcBaseInfo.desc)) {
            emptyDescriptionObj.SetActive(true);
            descriptionText.gameObject.SetActive(false);
            descLimitText.text = $"0/{DescLimitCount}";
        } else {
            descriptionText.text = _ugcBaseInfo.desc;
            emptyDescriptionObj.SetActive(false);
            descriptionText.gameObject.SetActive(true);
            descLimitText.text = $"{_ugcBaseInfo.desc.Length}/{DescLimitCount}";
        }

        if (!string.IsNullOrEmpty(_ugcBaseInfo.cover)) {
            cover.Load(_ugcBaseInfo.cover);
        }
        CheckNextEnable();
    }

    private void CheckNextEnable() {
        bool isEnable = !string.IsNullOrEmpty(_ugcBaseInfo.name);
        isEnable &= !string.IsNullOrEmpty(_ugcBaseInfo.desc);
        isEnable &= !string.IsNullOrEmpty(_ugcBaseInfo.cover);
        SetNextEnabled(isEnable);
    }

    private void SetNextEnabled(bool isEnable)
    {
        var btnColor = isEnable ? "#FFD624" : "#D9D9D9";
        nextBtnImg.color = DataUtil.DeSerializeColorCheckHash(btnColor);
        nextBtn.SetClickAble(isEnable);
    }

    public void SetOnEditSuccessAct(Action<UgcBaseInfo> act)
    {
        _onEditSuccessAct = act;
    }

    private void OnNextBtnClick()
    {
        nextBtn.ShowLoading();

        string updateHeadUrl = "";
        string reqParam = "";
        if (_ugcBaseInfo is SkinInfo)
        {
            var skinReq = new SetSkinInfoReq
            {
                skinInfo = (SkinInfo)_ugcBaseInfo,
                SkinActionInfo = _skinActionInfo,
                setType = (int)SetType.Update
            };
            updateHeadUrl = HttpUrlDefine.SetSkin;
            reqParam = JsonConvert.SerializeObject(skinReq);
        }
        else if (_ugcBaseInfo is PropInfo)
        {
            var propReq = new SetPropInfoReq()
            {
                propInfo = (PropInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };
            updateHeadUrl = HttpUrlDefine.setProp;
            reqParam = JsonConvert.SerializeObject(propReq);
        }
        else if (_ugcBaseInfo is MaterialInfo)
        {
            var matReq = new SetMaterialInfoReq()
            {
                materialInfo = (MaterialInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };
            updateHeadUrl = HttpUrlDefine.SetMaterial;
            reqParam = JsonConvert.SerializeObject(matReq);
        }
        else if (_ugcBaseInfo is MusicScoreInfo)
        {
            var matReq = new SetMusicScoreInfoReq()
            {
                musicScoreInfo= (MusicScoreInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };
            updateHeadUrl = HttpUrlDefine.SetMusicScore;
            reqParam = JsonConvert.SerializeObject(matReq);
        }
        else if (_ugcBaseInfo is ToneInfo)
        {
            EditToneInfoReq req = new EditToneInfoReq()
            {
                musicToneInfo =(ToneInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };

            updateHeadUrl = HttpUrlDefine.SetUGCTone;
            reqParam = JsonConvert.SerializeObject(req);
        } 
        else if (_ugcBaseInfo is AnimInfo)
        {
            SetAnimInfoReq ugcAnimInfoReq = new SetAnimInfoReq()
            {
                animInfo = (AnimInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };

            updateHeadUrl = HttpUrlDefine.setAnim;
            reqParam = JsonConvert.SerializeObject(ugcAnimInfoReq);
        }
        else if (_ugcBaseInfo is PoseInfo)
        {
            SetAnimInfoReq ugcPoseInfoReq = new SetAnimInfoReq()
            {
                poseInfo = (PoseInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };

            updateHeadUrl = HttpUrlDefine.SetPose;
            reqParam = JsonConvert.SerializeObject(ugcPoseInfoReq);
        }
        else if (_ugcBaseInfo is AnimMusicInfo)
        {
            SetUgcAnimMusicReq ugcAnimMusicReq = new SetUgcAnimMusicReq()
            {
                animMusicInfo = (AnimMusicInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };

            updateHeadUrl = HttpUrlDefine.setUgcAnimMusic;
            reqParam = JsonConvert.SerializeObject(ugcAnimMusicReq);
        }
        else if (_ugcBaseInfo is AINpcInfo)
        {
            SetAINpcInfoReq ugcNpcInfoReq = new SetAINpcInfoReq()
            {
                npc = (AINpcInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };

            updateHeadUrl = HttpUrlDefine.NpcSet;
            reqParam = JsonConvert.SerializeObject(ugcNpcInfoReq);
        }
        else if (_ugcBaseInfo is OCTheatreAvatarInfo)
        {
            var actorReq = new SetActorInfoReq()
            {
                actorInfo = (OCTheatreAvatarInfo)_ugcBaseInfo,
                setType = (int)SetType.Update
            };
            updateHeadUrl = HttpUrlDefine.ActorSet;
            reqParam = JsonConvert.SerializeObject(actorReq);
        }
        else if(_ugcBaseInfo is VehicleInfo)
        {
            var req = new UGCVehicleSetRequest((VehicleInfo)_ugcBaseInfo, UGCOperationType.UpdatePublish);
            updateHeadUrl = HttpUrlDefine.SetVehicle;
            reqParam = JsonConvert.SerializeObject(req);
        }
        else if (_cabinCharacterInfo != null)
        {
            _cabinCharacterInfo.name = _ugcBaseInfo.name;
            _cabinCharacterInfo.desc = _ugcBaseInfo.desc;
            var req = new SetCabinCharacterInfoData
            {
                characterInfo = _cabinCharacterInfo,
                setType = SetType.Update
            };
            updateHeadUrl = HttpUrlDefine.CabinCharacterSet;
            reqParam = JsonConvert.SerializeObject(req);
        }

        NetworkManager.Inst.SendHttpRequest(updateHeadUrl, HttpMethod.POST, reqParam, (content) =>
        {
            if(this != null)
                CloseSelf();

            _onEditSuccessAct?.Invoke(_ugcBaseInfo);
        }, (error) =>
        {
            nextBtn.HideLoading();
        });
    }
}
