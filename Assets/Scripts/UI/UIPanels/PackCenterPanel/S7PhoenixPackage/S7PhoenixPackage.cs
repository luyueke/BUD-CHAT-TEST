using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Event;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;


public class S7PhoenixPackage : PackBaseView
{
    [SerializeField] private RawImage bg_img;
    [SerializeField] private Transform bg;
    [SerializeField] private Text endTime;
    [SerializeField] private CButton buyBtn;
    [SerializeField] private CButton previewBtn;
    [SerializeField] private PackCommonItem packItem;
    [SerializeField] private GameObject rightArrow;
    [SerializeField] private PackCommonItem packBigItem;
    [SerializeField] private Text taskEndTime;
    [SerializeField] protected GameObject buyNode;
    [SerializeField] protected GameObject taskEndTips;
    [SerializeField] public Transform taskContent;
    [SerializeField] private Text priceText;
    [SerializeField] private Text rewardTips1;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;
    
    private List<PackCommonItem> packItemNodeList = new List<PackCommonItem>();
    private PaidPackageListItem _paidPackageListItem;
    private TaskInfoData _taskInfoData;
    
    private string rewardIconName = "icon_reward";
    private string configPath;
    private string spriteatlasPath;
    internal CharacterWrap characterWrapper;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    private bool isInitAnimation = false;
    private List<string> pgcIds = new List<string>() { "40200420", "11300320", "11000219", "10400429", "10900438" };
    

    private void InitConfig(PaidPackageType paidPackageType)
    {
        //注意海外服需要手动配置该参数（商品id，跟运营拿，海外服需要，国服不需要）
        ProductIdType = ProductIdType.product_budpremiumvaluepack;
        LoadConfig(paidPackageType);
    }

    private void LoadConfig(PaidPackageType paidPackageType)
    {
        PaidPackageType = paidPackageType;
        string packName = PaidPackageType.ToString();
        string configPath = string.Format(PackCenterPanel.ViewBasePath + "{0}/{1}.json",packName, packName);
        var textAsset = Loader.Load<TextAsset>(configPath, gameObject);
        packViewConfig = JsonConvert.DeserializeObject<PackViewConfig>(textAsset.text);
        TaskId = packViewConfig.taskId;//跟后端(谢梓峰)拿
        spriteatlasPath = string.Format(PackCenterPanel.ViewBasePath + "{0}/{1}.spriteatlas",packName, packName);
        if (!string.IsNullOrEmpty(packViewConfig.spriteatlasPath))
        {
            spriteatlasPath = packViewConfig.spriteatlasPath;
        }
    }

    private void Start()
    {
        InitConfig(PaidPackageType.S7PhoenixPackage);
        // InitBg();
        InitClickListener();
        CreateItems(packViewConfig.rewardDataList);
        GetProductInfo();
        RefreshTaskStatus();
        
        InitAvatar();
        Invoke("Delay", 0.1f);
        isInitAnimation = true;
    }
    
    private void InitAvatar()
    {
        
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
            characterWrapper = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrapper.SetParent(characterRoot, true);
            animationCtrl = characterWrapper.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;

            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }

    }

    private void OnEnable()
    {
        if (isInitAnimation)
        {
            Invoke("Delay", 0.1f);
        }
    }
    
    private void Delay()
    {
        WearPgcClothes(pgcIds);
    }
    private void WearPgcClothes(List<string> pgcIds)
    {
        if (pgcIds == null)
        {
            return;
        }

        for (int i = 0; i < pgcIds.Count; i++)
        {
            var pgcId = pgcIds[i];
            var config = DataTables.GetGameResData(pgcId);
            if (config == null) continue;
            // 因为动作里包含这个手持 所以预览的时候不穿。
            if (pgcId == "11000219") continue;
            switch ((ResourceType)config.ResourceType)
            {
                case ResourceType.Avatar:
                    TryOn(pgcId);
                    break;
                case ResourceType.Emote:
                    PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                    break;
  
            }
        }
    }
    
    private void TryOn(string pgcId)
    {
        var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType),
            pgcId);
        characterWrapper.ChangeColor(classType, config.defaultColor);
        characterWrapper.Move(classType, config.pDef);
        characterWrapper.Rotate(classType, config.rDef);
        characterWrapper.Scale(classType, config.sDef);
        characterWrapper.HVScale(classType, config.vhSDef);
        characterWrapper.SetLeftOrRight(classType, config.leftRightType);

    }
    
    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
    {
        avatarCameraController?.SetEmoteView(pgcId);
        animationCtrl?.ResetEmoteForUICharacter();
        otherAnimationCtrl?.gameObject.SetActive(false);
        otherAnimationCtrl?.ResetEmoteForUICharacter();
        switch (emoteSubType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl?.PlaySingleEmoteForUICharacter(pgcId, null);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl?.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
                break;
                    
        }
    }

    public override void OnServerDataUpdate(PaidPackageListItem packageListItem)
    {
        this._paidPackageListItem = packageListItem;
        buyNode.gameObject.SetActive(packageListItem.isPaid != 1);
        taskEndTips.gameObject.SetActive(packageListItem.isPaid == 1);
        endTime.gameObject.SetActive(packageListItem.isPaid != 1);
        endTime.SetLocalText("距售卖时间结束还有: {0}",packageListItem.endDate);
    }

    private void InitClickListener()
    {
        buyBtn.onClick.AddListener(OnBuyBtnClick);
        previewBtn.onClick.AddListener(OnPreviewBtnClick);
    }

    protected override void OnTaskListUpdate(TaskListRsp taskListRsp)
    {
        taskContent.gameObject.SetActive(true);
        List<TaskInfoData> taskInfoDatas = taskListRsp.list;
        if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
        {
            return;
        }

        TaskInfoData tsTaskInfoData = taskInfoDatas[0];
        if (tsTaskInfoData == null)
        {
            return;
        }

        this._taskInfoData = tsTaskInfoData;

        List<TaskItemData> eventList = tsTaskInfoData.eventList;
        taskEndTime.SetLocalText("距任务结束还有: {0}" , _taskInfoData.endDate);
        for (int i = 0; i < packItemNodeList.Count; i++)
        {
            packItemNodeList[i].SetData(TaskId, eventList[i], taskItemData => { RefreshTaskStatus(); }, () =>
            {
                if (_paidPackageListItem.isPaid != 1)
                {
                    string localProductName = LocalizationManager.Inst.GetLocalizedText(packViewConfig.productName);
                    string tips = LocalizationManager.Inst.GetLocalizedText("购买{0}获取对应礼品哦",localProductName);
                    TipPanel.ShowToast(tips);
                }
            });
        }
    }
    
    protected override void OnBuySuccess(string orderId)
    {
        ShowPackReward();
        buyNode.gameObject.SetActive(false);
        taskEndTips.gameObject.SetActive(true);
        _paidPackageListItem.isPaid = 1;
        ReddotManagerUtils.Inst.RefreshRedDot();
    }
    
    protected void ShowPackReward()
    {
        Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck); 
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, rewardIconName, gameObject),
                RewardAmount = 1,
                rewardName = packViewConfig.productName
            }
        });
    }

    private void CreateItems(List<RewardItem> rewardDataList)
    {
        // taskContent.gameObject.SetActive(false);
        packItemNodeList.Clear();
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }

        for (var i = 0; i < rewardDataList.Count; i++)
        {
            if (string.IsNullOrEmpty(rewardDataList[i].rewardName2))
            {
                var taskItem = Instantiate(packBigItem, taskContent);
                taskItem.SetAtlasPath(spriteatlasPath);
                taskItem.OnInitCreate(rewardDataList[i], true);
                packItemNodeList.Add(taskItem);
            }
            else
            {
                var taskItem = Instantiate(packItem, taskContent);
                taskItem.SetAtlasPath(spriteatlasPath);
                taskItem.OnInitCreate(rewardDataList[i], false);
                packItemNodeList.Add(taskItem);
            }
            if (i != rewardDataList.Count -1)
            {
                Instantiate(rightArrow, taskContent);
            }
        }
    }
    
    private void OnBuyBtnClick()
    {
        if (_paidPackageListItem == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = _paidPackageListItem.productInfo;
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { Purchase(paymentType,productInfo,packViewConfig.productName); });
            return;
        }

        Purchase(ConfirmPaymentPanel.PaymentType.Default,_paidPackageListItem.productInfo,packViewConfig.productName);
    }

    private void OnPreviewBtnClick()
    {
  
        List<PaidPackRewardData> paidPackRewardDatas = new List<PaidPackRewardData>();
        paidPackRewardDatas.Add(new PaidPackRewardData()
        {
            pgcIds = new List<string>(){"11300320", "11000219", "10400429", "10900438"},
            name =  "锦绣菲妮克丝套装",
            isBundle = true,
            bundleId =  "27"
        });
        paidPackRewardDatas.Add(new PaidPackRewardData()
        {
            pgcIds = new List<string>(){"40200420"},
            name =  "折扇轻摇",
            isBundle = false
        });
        var panel = UIManager.Inst.OpenPanel<PaidPackRewardPanel>(PanelId.PaidPackRewardPanel);
        panel.SetEventPreview(paidPackRewardDatas, "", bg_img.texture);
    }
    
}