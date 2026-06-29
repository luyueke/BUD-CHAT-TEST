using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using Game.Avatar;
using Game.Event;
using GameData;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BreakIcePanel : BasePanel<BreakIcePanel>
{
    public Button buyBtn;
    public Button ruleBtn;
    public Button closeBtn;

    public Button btn_6;
    public Button btn_18;
    public Button btn_30;

    public GameObject leftTop_6;
    public GameObject leftTop_18;
    public GameObject leftTop_30;

    public GameObject title_6;
    public GameObject title_18;
    public GameObject title_30;
    public GameObject bg_6;
    public GameObject bg_18;
    public GameObject bg_30;

    public GameObject image_6;
    public GameObject image_18;
    public GameObject image_30;

    public GameObject TIPS_6;
    public GameObject TIPS_18;
    public GameObject TIPS_30;

    public GameObject txt_zc_6;
    public GameObject txt_zc_18;
    public GameObject txt_zc_30;
    public GameObject txt_BuyBtn_6;
    public GameObject txt_BuyBtn_18;
    public GameObject txt_BuyBtn_30;

    public GameObject ScrollView_6;
    public GameObject ScrollView_18;
    public GameObject ScrollView_30;

    public GameObject bg_30_star;

    public Text txt_res;

    public Text txt_anim_name; 

    [Header("3D角色")]
    public Transform characterRoot;
    public AvatarCameraController avatarCameraController;
    public Camera avatarCamera;
    private string emoteId_6 = "40100510";
    private string emoteId_18 = "41000007";
    private string emoteId_30 = "40100568";

    private CharacterWrap _characterWrap;
    private PlayerAnimationCtrl _animationCtrl;
    private GameObject _selfieNode;

    private const string LeftEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/effect_l";
    private const string RightEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/effect_r";
    private const string SelfieStickPrefabPath = "Assets/Loadable/AnimationsExpress/Feat/selfiestick_effect/selfiestick_effect.prefab";


    string taskId = "NewbieDressUpTask";
    List<TaskItemData> _eventList;
    public List<BreakIceClaimItem> items_6;
    public List<BreakIceClaimItem> items_18;
    public List<BreakIceClaimItem> items_30;
    string rulePath = "Assets/Loadable/UI/UIPanel/BreakIcePanel/Rule.json";
    string price = "6";
    string productId;
    string productName = "新人装扮礼包";

    public override void OnHidden()
    {
        //AkSoundManager.Inst.PlayBGSound();
        base.OnHidden();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        //AkSoundManager.Inst.StopBGSound();
        InitCharacter();
        //ReferenceTask();
        closeBtn.onClick.AddListener(CloseSelf);
        ruleBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
        });

#if UNITY_ANDROID
        productId = "android_budzhuangban6";
#elif UNITY_IOS
        productId = "ios_budzhuangban6";
#endif
        buyBtn.onClick.RemoveAllListeners();
        buyBtn.onClick.AddListener(() =>
        {
            OnPayBtnClick();
        });

        btn_6.onClick.AddListener(() => SwitchTab("6"));
        btn_18.onClick.AddListener(() => SwitchTab("18"));
        btn_30.onClick.AddListener(() => SwitchTab("30"));
        SwitchTab("6");
    }

    private void InitCharacter()
    {
        if (characterRoot == null) return;
        var characterData = AccountDataManager.Inst.UserInfo.avatarInfo
            ?? AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (characterData == null) return;

        _characterWrap = AvatarController.Inst.CreateUIAvatar(characterData);
        _characterWrap.SetParent(characterRoot, true);
        _animationCtrl = _characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
        if (avatarCameraController != null)
            avatarCameraController.RotateTarget = characterRoot;

    }

    public void PlayTabEmote(string emoteId)
    {
        if (_animationCtrl == null || string.IsNullOrEmpty(emoteId)) return;

        _animationCtrl.ResetEmoteForUICharacter();
        // 从自拍模式切回时重置 PlayerState，避免 CameraMode 残留
        _animationCtrl.SetPlayerState(PlayerState.Leisure);
        _animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
        if (_selfieNode != null) _selfieNode.SetActive(false);

        var resData = Es.DataTables.GetGameResData(emoteId);
        if (resData == null)
        {
            if (Es.DataTables.GetCameraSelfiePose(emoteId) != null)
            {
                PlaySelfieEmote(emoteId);
            }
            else
            {
                LoggerUtils.LogError($"[BreakIcePanel] PlayTabEmote: emoteId={emoteId} 在 GameResData 和 CameraSelfiePose 中均未找到配置，跳过播放");
            }
            return;
        }

        if (resData.ResourceType == (int)ResourceType.CameraSelfiePose)
        {
            PlaySelfieEmote(emoteId);
            return;
        }

        if (avatarCameraController != null)
        {
            var config = Es.DataTables.GetEmoUIConfig(emoteId);
            if (config != null)
                avatarCameraController.SetEmoteView(emoteId);
        }
        if (resData.ResourceType == (int)ResourceType.Emote)
        {
            PreviewEmote(emoteId, (EmoteSubType)resData.SubType);
        }

        var emoConfig = Es.DataTables.GetEmoUIConfig(emoteId);
        if (txt_anim_name != null && emoConfig != null)
            txt_anim_name.text = emoConfig.name;
    }

    private void PreviewEmote(string emoteId, EmoteSubType emoteSubType)
    {
        try
        {
            
        
            switch (emoteSubType)
            {
                case EmoteSubType.Single:
                case EmoteSubType.SingleLoop:
                    _animationCtrl.PlaySingleEmoteForUICharacter(emoteId, null);
                    break;
                case EmoteSubType.SelfiePose:
                    PlaySelfieEmote(emoteId);
                    break;
            }
        }catch
        {
            Debug.LogError("找不到动作 ");
        }
    }

    private void PlaySelfieEmote(string emoteId)
    {
        var cfg = Es.DataTables.GetCameraSelfiePose(emoteId);
        if (cfg == null) return;

        _animationCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
        _animationCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
        _animationCtrl.OverrideAnimationClip("selfiestick_idle", null);

        if (!string.IsNullOrEmpty(cfg.resourcePath))
        {
            var clipWrapper = Loader.Load<AnimationClip>(cfg.resourcePath + ".anim");
            var clipRes = clipWrapper?.RetainAsset(gameObject);
            if (clipRes != null)
            {
                var clip = AnimationClip.Instantiate(clipRes, gameObject.transform);
                _animationCtrl.OverrideAnimationClip("selfiestick_idle", clip);
            }
        }

        _animationCtrl.SetPlayerState(PlayerState.CameraMode);
        _animationCtrl.SetPlayerAniState(PlayerAniState.Idle);

        var parent = _characterWrap.Avatar.transform.Find(cfg.stickHand == 1 ? RightEffectPath : LeftEffectPath);
        if (parent == null) return;

        if (_selfieNode == null)
        {
            var selfiePrefab = Loader.Load<GameObject>(SelfieStickPrefabPath)?.RetainAsset(gameObject);
            if (selfiePrefab == null) return;
            _selfieNode = GameObject.Instantiate(selfiePrefab, parent);
        }
        else if (_selfieNode.transform.parent != parent)
        {
            _selfieNode.transform.SetParent(parent, false);
        }

        _selfieNode.transform.localPosition = new Vector3(0f, 0f, 0.02f);
        _selfieNode.transform.localScale = Vector3.one;
        _selfieNode.transform.localEulerAngles = cfg.stickRot;
        _selfieNode.SetActive(true);

        var selfieAnimator = _selfieNode.GetComponent<Animator>();
        if (selfieAnimator != null)
            selfieAnimator.SetInteger("BoardState", 2);

        if (txt_anim_name != null)
            txt_anim_name.text = cfg.name;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_characterWrap != null && _characterWrap.Avatar != null)
            Destroy(_characterWrap.Avatar.gameObject);
    }

    void SwitchTab(string tab)
    {
        price = tab;
#if UNITY_ANDROID
        productId = "android_budzhuangban" + tab;
#elif UNITY_IOS
        productId = "ios_budzhuangban" + tab;
#endif
        switch(tab)
        {
             case "6":
                txt_res.text = "甜心蝴蝶结";
                taskId = "NewbieDressUpTask";
                rulePath = "Assets/Loadable/UI/UIPanel/BreakIcePanel/Rule.json";
                PlayTabEmote(emoteId_6);
             break;
             case "18":
                txt_res.text = "甜心蝴蝶结昵称框";
                taskId = "NewbieDressUpTask18";
                rulePath = "Assets/Loadable/UI/UIPanel/BreakIcePanel/Rule18.json";
                PlayTabEmote(emoteId_18);
             break;
             case "30":
                txt_res.text = "甜梦缎带";
                taskId = "NewbieDressUpTask30";
                rulePath = "Assets/Loadable/UI/UIPanel/BreakIcePanel/Rule30.json";
                PlayTabEmote(emoteId_30);
             break;
        }
        ReferenceTask();
        ScrollView_6.SetActive(tab == "6");
        ScrollView_18.SetActive(tab == "18");
        ScrollView_30.SetActive(tab == "30");
        txt_BuyBtn_6.SetActive(tab == "6");
        txt_BuyBtn_18.SetActive(tab == "18");
        txt_BuyBtn_30.SetActive(tab == "30");
        txt_zc_6.SetActive(tab== "6");
        txt_zc_18.SetActive(tab== "18");
        txt_zc_30.SetActive(tab== "30");
        TIPS_6.SetActive(tab == "6");
        TIPS_18.SetActive(tab == "18");
        TIPS_30.SetActive(tab == "30");
        image_6.SetActive(tab == "6");
        image_18.SetActive(tab == "18");
        image_30.SetActive(tab == "30");
        bg_6.SetActive(tab == "6");
        bg_18.SetActive(tab == "18");
        bg_30.SetActive(tab == "30");
        leftTop_6.SetActive(tab == "6");
        leftTop_18.SetActive(tab == "18");
        leftTop_30.SetActive(tab == "30");
        title_6.SetActive(tab == "6");
        title_18.SetActive(tab == "18");
        title_30.SetActive(tab == "30");
        bg_30_star.SetActive(tab == "30");
        GameObjectEx.FindChildByName(btn_6.transform,"img_select")?.gameObject.SetActive(tab == "6");
        GameObjectEx.FindChildByName(btn_18.transform,"img_select")?.gameObject.SetActive(tab == "18");
        GameObjectEx.FindChildByName(btn_30.transform,"img_select")?.gameObject.SetActive(tab == "30");
    }
    public void StopAudio()
    {
        if (_animationCtrl != null)
            AkSoundManager.Inst.StopAll(_animationCtrl.gameObject);
    }

    public void OnPayBtnClick()
    {
        // 检查是否是官方渠道
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            var panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, price);
            panel.SetCallback(paymentType => StartPurchase(paymentType));
        }
        else
        {
            StartPurchase(ConfirmPaymentPanel.PaymentType.Default);
        }
    }

    private void StartPurchase(ConfirmPaymentPanel.PaymentType paymentType)
    {
        ShowPurchaseLoading();

        IAPDataManager.Inst.GetProductOrderId(productId, null, (success, info) =>
        {
            if (!success || string.IsNullOrEmpty(info?.budOrderId))
            {
                HidePurchaseLoading();
                return;
            }
            var channelProductInfo = new ChannelProductInfo
            {
                productId = productId,
                productName = productName,
                productDesc = productName,
                price = price,
                extension = JsonConvert.SerializeObject(info),
                cpOrderId = info.budOrderId,
                paymentType = (int)paymentType
            };

            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        ReferenceTask();
        var response = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (response.resultType == (int)BillingResultType.UserPaySuccess)
        {
            // 购买成功
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var firstBuyKey = "FirstBuyNieBieSixGift" + AccountDataManager.Inst.Uid;
            PlayerPrefs.SetInt(firstBuyKey, 1);
            PlayerPrefs.Save();
            buyBtn.gameObject.SetActive(false);
            // 刷新任务状态
            ReferenceTask();
            HidePurchaseLoading();
        }
        else if (response.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            ReferenceTask();
            // 处理失败...
        }
        ReferenceTask();
    }
    private int taskStatus = 0;
    public void ReferenceTask()
    {
        IAPDataManager.Inst.GetTaskList(taskId, (b, taskInfoResponse) =>
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            if (taskInfoResponse?.list == null || taskInfoResponse.list.Count <= 0) return;

            TaskInfoData tsTaskInfoData = taskInfoResponse.list[0];
            //if (tsTaskInfoData?.eventList == null) return;
            taskStatus = tsTaskInfoData.taskStatus;
            this._eventList = tsTaskInfoData.eventList;
            buyBtn.gameObject.SetActive(tsTaskInfoData.taskStatus==0);
            UpdateView();
        });
        
        
    }

    void UpdateView()
    {
        if(taskId == "NewbieDressUpTask")
        {
            if (_eventList!=null && _eventList.Count > 0) {
                
                for (int i = 0; i < _eventList.Count; i++)
                {
                    items_6[i].SetData(_eventList[i], i + 1, taskId);
                }
            }
            else
            {   
                for (int j = 0; j < items_6.Count; j++)
                {
                    TaskItemData eventt = new TaskItemData
                    {
                        eventStatus = taskStatus == 2 ? (int)EventStatus.Finish : (int)EventStatus.UnClaim
                    };
                    items_6[j].SetData(eventt, j + 1, taskId);
                }
            }
        }
        else if(taskId == "NewbieDressUpTask18")
        {
            if (_eventList!=null && _eventList.Count > 0) {
                
                for (int i = 0; i < _eventList.Count; i++)
                {
                    items_18[i].SetData(_eventList[i], i + 1, taskId);
                }
            }
            else
            {   
                for (int j = 0; j < items_18.Count; j++)
                {
                    TaskItemData eventt = new TaskItemData
                    {
                        eventStatus = taskStatus == 2 ? (int)EventStatus.Finish : (int)EventStatus.UnClaim
                    };
                    items_18[j].SetData(eventt, j + 1, taskId);
                }
            }
        }
        else if(taskId == "NewbieDressUpTask30")
        {
            if (_eventList!=null && _eventList.Count > 0) {
                
                for (int i = 0; i < _eventList.Count; i++)
                {
                    items_30[i].SetData(_eventList[i], i + 1, taskId);
                }
            }
            else
            {   
                for (int j = 0; j < items_30.Count; j++)
                {
                    TaskItemData eventt = new TaskItemData
                    {
                        eventStatus = taskStatus == 2 ? (int)EventStatus.Finish : (int)EventStatus.UnClaim
                    };
                    items_30[j].SetData(eventt, j + 1, taskId);
                }
            }
        }
    }
}