using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using GameData.Base;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UIAgent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AINpcPublishPanel : BasePanel<AINpcPublishPanel>
{
    public Transform BG;
    [SerializeField] protected SuperTextMesh nameText;
    [SerializeField] protected CButton nameEditBtn;
    [SerializeField] protected CButton descriptionEditBtn;
    [SerializeField] protected SuperTextMesh descriptionText;
    [SerializeField] protected GameObject emptyNameObj;
    [SerializeField] protected GameObject emptyDescriptionObj;
    [SerializeField] protected Text descLimitText;
    [SerializeField] protected Text nameLimitText;

    private KeyBoardInfo descriptionKeyBoardInfo;
    protected virtual int DescLimitCount => 250;
    protected virtual int NameLimitCount => 25;
    
    [SerializeField] private Toggle[] priceToggles;
    [SerializeField] private CButton priceEditBtn;
    [SerializeField] private Text selfPriceText;

    [SerializeField] private AvatarCameraController cameraController;
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] private  CButton backBtn;
    [SerializeField] private  CButton nextBtn;
    private CharacterWrap characterWrap;
    private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();
    private Dictionary<int, Toggle> gemToggleDic;
    [SerializeField] private GameObject coinText;
    private KeyBoardInfo priceKeyBoardInfo;
    private KeyBoardInfo nameKeyBoardInfo;
    
    private AINpcInfo npcInfo;
    private int Min_Price = 100;
    public override void OnCreate()
    {
        nameEditBtn.onClick.AddListener(OnNameEditBtnClick);
        nameKeyBoardInfo = new KeyBoardInfo {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = NameLimitCount,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };
        
        descriptionEditBtn.onClick.AddListener(OnDescriptionEditBtnClick);
        descriptionKeyBoardInfo = new KeyBoardInfo {
            type = 0,
            placeHolder = "",
            inputMode = 0,
            maxLength = DescLimitCount,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            returnKeyType = (int)ReturnType.Return
        };
        InitBG();
        InitPriceToggleView();
        backBtn.onClick.AddListener(OnBackClick);
        nextBtn.onClick.AddListener(OnNextClick);
    }

    private void InitBG()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3","avatar_icon_4"
        });
        item.gameObject.SetActive(true);
    }
    
    private void OnNextClick()
    {
        SetAINpcInfoReq req = new SetAINpcInfoReq();
        req = new SetAINpcInfoReq()
        {
            npc = npcInfo,
            setType = (int)SetType.Publish
        };
        var req2 = new UGCSetRequest(npcInfo, UGCOperationType.Publish);
        NetworkManager.Inst.SendHttpRequest<DraftListItem>(HttpUrlDefine.NpcSet, HttpMethod.POST, JsonConvert.SerializeObject(req), OnPublishSuccess,
            (rsp) =>
            {
                if (rsp.result == 501) {
                    // 审核失败
                    Action<bool> onAppealCallBack = (isAppeal) => {
                        // 提交申诉，视作发布成功
                        if (isAppeal) {
                            if (npcInfo.auditInfo == null)
                            {
                                npcInfo.auditInfo = new AuditStatus()
                                {
                                    auditResult = 4
                                };
                            }
                            else
                            {
                                npcInfo.auditInfo.auditResult = 4;
                            }
                            MessageHelper.Broadcast(MessageName.OnAINpcStudioPublishedListChange);
                        }
                    };
                    UIAgentManager.Inst.OpenPanel(PanelId.UGCAuditRejectedPanel, WindowId.None, req2, rsp.rmsg, onAppealCallBack);
                } else {
                    HttpErrorCodeHandler errorCodeHandler = new Network.Http.HttpErrorCodeHandler();
                    errorCodeHandler.HandleErrorCodeResult(rsp);
                    LoggerUtils.LogError($"发布失败 [{npcInfo.id}]:" + rsp.rmsg);
                }
            });
    }

    private void OnPublishSuccess(DraftListItem draftListItem)
    {
        CloseSelf();
        MessageHelper.Broadcast(MessageName.OnAINpcStudioPublishedListChange);
    }
    
    private void OnBackClick()
    {
        CloseSelf();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        npcInfo = args[0] as AINpcInfo;
        nameText.text = npcInfo.name;
        nameLimitText.text = $"{npcInfo.name.Length}/{NameLimitCount}";
        
        descriptionText.text = npcInfo.desc;
        descLimitText.text = $"{npcInfo.desc.Length}/{DescLimitCount}";
        emptyDescriptionObj.SetActive(string.IsNullOrEmpty(descriptionText.text));
        emptyNameObj.SetActive(string.IsNullOrEmpty(nameText.text));
        ShowCharacter();
        if (npcInfo.paymentInfo == null)
        {
            priceToggles[0].isOn = true;
        }
        
        SyncEditData();
    }

    private void OnDescriptionEditBtnClick() {
        descriptionKeyBoardInfo.defaultText = npcInfo.desc;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(descriptionKeyBoardInfo));
    }
    
    private void OnNameEditBtnClick() {
        nameKeyBoardInfo.defaultText = npcInfo.name;
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(nameKeyBoardInfo));
    }

    
    private void OnGetDescFromNative(string desc) {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        npcInfo.desc = desc;
        if (string.IsNullOrEmpty(npcInfo.desc)) {
            emptyDescriptionObj.SetActive(true);
            descriptionText.gameObject.SetActive(false);
            descLimitText.text = $"0/{DescLimitCount}";
        } else {
            descriptionText.text = npcInfo.desc;
            emptyDescriptionObj.SetActive(false);
            descriptionText.gameObject.SetActive(true);
            descLimitText.text = $"{npcInfo.desc.Length}/{DescLimitCount}";
        }
        descriptionText.text = npcInfo.desc;

        SyncEditData();
    }
    
    private void OnGetNameFormNative(string name) {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        npcInfo.name = name;
        
        if (string.IsNullOrEmpty(npcInfo.name)) {
            emptyNameObj.SetActive(true);
            nameText.gameObject.SetActive(false);
            nameLimitText.text = $"0/{NameLimitCount}";
        } else {
            nameText.text = npcInfo.name;
            emptyNameObj.SetActive(false);
            nameText.gameObject.SetActive(true);
            nameLimitText.text = $"{npcInfo.name.Length}/{NameLimitCount}";
        }
        nameText.text = npcInfo.name;
        
        SyncEditData();
    }
    
    public void InitPriceToggleView()
    {
        int[] prices = {100, 150, 200, 250, 300};
        priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
        for (var i = 0; i < priceToggles.Length; i++)
        {
            int value = prices[i];
            priceToggleDic.Add(value, priceToggles[i]);
            priceToggles[i].GetComponentInChildren<Text>().SetText(value.ToString());
            priceToggles[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    if (npcInfo.paymentInfo == null)
                    {
                        npcInfo.paymentInfo = new PaymentInfo();
                        npcInfo.paymentInfo.currencyType = CurrencyType.PinkCoin;
                    }
                    npcInfo.paymentInfo.price = value;
                    
                    SyncEditData();
                }
            });
        }
        
        priceKeyBoardInfo = new KeyBoardInfo()
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格"),
            inputMode = 1,
            maxLength = 4,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", Min_Price, 9999),
            defaultText = "",
            returnKeyType = (int)ReturnType.Return
        };
    }
    
    
    private void OnPriceEditBtnClick()
    {
        priceKeyBoardInfo.defaultText = "";
        priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPriceFromNative);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(priceKeyBoardInfo));
    }
    
    private void OnGetPriceFromNative(string price)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (int.TryParse(price, out var value))
        {
            OnPriceToggleClick(value, true);
        }
    }
    
    private void OnPriceToggleClick(int value,bool isCustom = false )
    {
        if (isCustom && (value < Min_Price || value > 9999))
        {
            var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", Min_Price, 9999);
            TipPanel.ShowToast(lengthTips);
            return;
        }

        if (npcInfo.paymentInfo == null)
        {
            npcInfo.paymentInfo = new PaymentInfo();
            npcInfo.paymentInfo.currencyType = CurrencyType.PinkCoin;
        }
        npcInfo.paymentInfo.price = value;
        selfPriceText.text = value.ToString();
        SyncEditData();
    }

    protected void SyncEditData()
    {
        bool isCustom = !priceToggleDic.TryGetValue(npcInfo.paymentInfo.price, out var toggle);
        Toggle findToggle = toggle;

        if (isCustom)
        {
            priceToggles[0].group.SetAllTogglesOff(false);
            priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
            priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
            priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
            priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                npcInfo.paymentInfo.price.ToString();
            priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite =
                PgcUtils.LoadCurrencyIcon(CurrencyType.PinkCoin, gameObject);
        }
        else
        {
            findToggle.SetIsOnWithoutNotify(true);
            priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
            priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
            priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
        }

        selfPriceText.text = $"每个商品你会获得等值的{npcInfo.paymentInfo.price / 2.0}个";
        coinText.SetActive(true);
        CheckNextEnable();
    }

    protected virtual void CheckNextEnable() {
        var isEnable = !string.IsNullOrEmpty(npcInfo.desc) && !string.IsNullOrEmpty(npcInfo.name) && (npcInfo.paymentInfo != null);
        SetNextEnabled(isEnable);
    }
    public void SetNextEnabled(bool value) {
        nextBtn.SetClickAble(value);
    }

    
    public void ShowCharacter()
    {
        var saveCharacterData =  CharacterData.DeserializeObject(npcInfo.npcAvatarJson);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(CharacterRoot, true);
            cameraController.RotateTarget = CharacterRoot;
            cameraController.SetCameraZoom(ViewType.ZoomWholeBody);
        }
    }
    
}
