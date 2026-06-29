using System;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using UnityEngine;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine.UI;
using GameData.PgcData;
using Es;
using GameData;

public class FlowerCarriageView : NewDefaultGashaponView
{
    private Text _txt_Tip;
    private List<FlowerCarriageRewardItem> _rewardItemList;
    [SerializeField] 
    private GashaponCharacterPreview characterPreview;

    private List<string> _rewardPgcIdList = new List<string>()
    {
        "10400278", "11300191", "11300193", "10900283", "10100046", "11000084", "10400277", "10700097", "11700029", "40900489"
    };
    
    private List<string> _previewAPgcIdList = new List<string>()
    {
        "11300193", "10900283", "10100046", "11000084", "10400277", "10700097", "11700029"
    };
    private List<string> _previewBPgcIdList = new List<string>()
    {
        "10400278", "11300191"
    };

    private bool _isInit;
    protected override void InitUI()
    {
        twistBtn = GameObjectEx.FindChildByName(transform, "TwistBtn").GetComponent<CButton>();
        singleIcon = GameObjectEx.FindChildByName(twistBtn.transform, "icon").GetComponent<Image>();
        singleText = GameObjectEx.FindChildByName(twistBtn.transform, "num").GetComponent<Text>();
        srcSingleText = GameObjectEx.FindChildByName(twistBtn.transform, "srcNum").GetComponent<Text>();
        singleDiscountTag = GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject;
        
        _rewardItemList = GameObjectEx.FindChildByName(this.transform, "RewardList").GetComponentsInChildren<FlowerCarriageRewardItem>().ToList();

        infoBtn = GameObjectEx.FindChildByName(transform, "InfoBtn").GetComponent<CButton>();
        previewBtn = GameObjectEx.FindChildByName(transform, "PreviewBtn").GetComponent<CButton>();

        _txt_Tip = GameObjectEx.FindChildByName(transform, "Txt_Tips").GetComponent<Text>();
        
        twistBtn.onClick.AddListener(OnTwistClick);
        infoBtn.onClick.AddListener(OnInfoClick);
        previewBtn.onClick.AddListener(OnPriviewBtnClick);

        if (characterPreview == null)
        {
            characterPreview = GetComponentInChildren<GashaponCharacterPreview>();
        }
        
        if (characterPreview != null && !characterPreview.isActiveAndEnabled)
        {
            characterPreview.gameObject.SetActive(true);
            characterPreview.InitPreviewPlayer();
            characterPreview.InitPreviewPet();
            characterPreview.StopAllEmoteSound();
        }
        UpdateAltas();
        UpdateWidgetView();
        UpdateBtnUI();

        for (int i = 0; i < _rewardPgcIdList.Count; i++)
        {
            _rewardItemList[i].InitData(_rewardPgcIdList[i], OnPriviewBtnClick);
        }

        DelayShowEmote();
        _isInit = true;
    }

    private void OnEnable()
    {
        if (_isInit)
            DelayShowEmote();
    }

    private void OnDisable()
    {
        characterPreview?.StopAllEmoteSound();
    }

    private void DelayShowEmote()
    {
        if(characterPreview == null)
            return;
        
        characterPreview.StopAllEmoteSound();
        characterPreview.StartPreviewSpecialAnim("40900489", SpecialAnim.FastRun);
        OnWearAvatar(characterPreview.characterWrap, _previewBPgcIdList);
        OnWearAvatar(characterPreview.otherCharacterWrap, _previewAPgcIdList);
    }
    
    internal void OnWearAvatar(CharacterWrap avatarWrapper, List<string> pgcIdList)
    {
        foreach (var pgcId in pgcIdList)
        {
            var config = DataTables.GetAvatarCommonData(pgcId);
            if (config == null) return;
            var classType = UniqueType.GetAvatar(pgcId);
            avatarWrapper.ChangePart(classType, pgcId);
            avatarWrapper.ChangeColor(classType, config.defaultColor);
            avatarWrapper.Move(classType, config.pDef);
            avatarWrapper.Rotate(classType, config.rDef);
            avatarWrapper.Scale(classType, config.sDef);
            avatarWrapper.HVScale(classType, config.vhSDef);
            avatarWrapper.SetLeftOrRight(classType, config.leftRightType);
        }
    }
    
    private void UpdateBtnUI()
    {
        if (gashaponData == null) return;
        var iconSprite = PgcUtils.LoadCurrencyIcon((int)gashaponData.CurrencyType, this.gameObject);
        if (iconSprite != null)
        {
            singleIcon.sprite = iconSprite;
        }

        singleText.SetText(gashaponData.SinglePrice.ToString());
    }
    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        SendGashaponRequestOnce(gashaponData, gashaponInfoRsp.singleDrawDiscountedPrice);
    }

    protected override void OnPriviewBtnClick()
    {
        characterPreview.gameObject.SetActive(false);
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = gashaponData.Name,
            gashaponData = gashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = gashaponData.CurrencyType,
            rulePath = viewCfg.RulePath,
            onBackCallBack = OnPreviewBackAction
        });
        previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }

    private void OnPreviewBackAction()
    {
        characterPreview.gameObject.SetActive(true);
        DelayShowEmote();
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        gashaponInfoRsp = infoRsp;
        RefreshLockState();
        singleText.SetText(infoRsp.singleDrawPrice.ToString());

        if (infoRsp.luckyProgressInfo.start == infoRsp.luckyProgressInfo.end)
        {
            _txt_Tip.SetText("恭喜！你已集齐粉韵花辇奖池所有商品！");
            twistBtn.gameObject.SetActive(false);
        }
        else
        {
            twistBtn.gameObject.SetActive(true);
        }
    }

    private void OnInfoClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var rulePath = viewCfg.RulePath;
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    private void RefreshLockState()
    {
        _rewardItemList.ForEach(x=>x.Refresh());
    }
}
