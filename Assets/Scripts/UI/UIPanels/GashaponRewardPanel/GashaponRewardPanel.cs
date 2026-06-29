using System;
using System.Collections.Generic;
using System.Linq;
using Game.Audio;
using Game.Store;
using GameData.Gashapon;
using GameData.Rewards;
using UI.Base;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-16 20:49:46
/// </summary>
public class GashaponRewardPanel : BasePanel<GashaponRewardPanel>
{
    [SerializeField] private GashaponRewardItemView rewardItem;
    
    [SerializeField] private Transform BG;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button bgCloseBtn;
    
    [SerializeField] private GameObject SingleObj;
    [SerializeField] private GameObject TenObj;
    
    [SerializeField] private GashaponRewardTipView tenSTips;
    [SerializeField] private GashaponRewardTipView singleSTips;

    [SerializeField] private Text replaceTips;
    
    /// <summary>
    /// 单抽奖励文案
    /// </summary>
    [SerializeField] private Text rewardText;

    public Action onBackCallBack;
    
    public override void OnCreate()
    {
        base.OnCreate();
        
        closeBtn.onClick.AddListener(OnCloseBtnClick);
        bgCloseBtn.onClick.AddListener(OnCloseBtnClick);
        
        AkSoundManager.Inst.PlayUIEffectSound("Play_UI_GetRewards_A3");
    }

    private void InitBgView(string gashaId)
    {
        if (BG == null)
        {
            LoggerUtils.LogError("[BG] Check GenderSelectPanel Bg Object");
            return;
        }

        var ViewCfg = GashaponDataManager.Inst.GetGashaponView(gashaId);
        if (ViewCfg == null)
        {
            return;
        }
        
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        if (!string.IsNullOrEmpty(ViewCfg.BgPath)) 
        {
            item.InitCustomTextureBg(ViewCfg.BgPath);
        }
        else
        {
            item.InitCustomBgItem(ViewCfg.BgColor, ViewCfg.AtlasPath, ViewCfg.BgSpriteIds);
        }
        item.gameObject.SetActive(true);
    }

    public void OnCloseBtnClick()
    {
        CloseSelf();
        onBackCallBack?.Invoke();
    }

    public void AddBackListener(Action callback)
    {
        onBackCallBack = callback;
    }
    
    public void ShowRewards(string gashaId, GashaponRsp rewardRsp)
    {
        InitBgView(gashaId);
        var rewardList = rewardRsp.rewardList;
        if (rewardList == null || rewardList.Count == 0) return;

        bool isTen = rewardList.Count > 1;
        SingleObj.SetActive(!isTen);
        TenObj.SetActive(isTen);
        
        for (int i = 0; i < rewardList.Count; i++)
        {
            FillCommonRewardItem(rewardList[i], isTen);
        }
        
        if (!isTen)
        {
            var rewardName = rewardList.First().rewardName;
            rewardText.SetLocalText("你获得了 <color=#FFD43E>{0}</color> ！",rewardName);
        }

        bool hasSReward = rewardRsp.wonFirstPrizeFrequency > 0;
        if (hasSReward)
        {
            tenSTips.SetData(rewardRsp.wonFirstPrizeFrequency);
            singleSTips.SetData(rewardRsp.wonFirstPrizeFrequency);
            if (isTen)
            {
                tenSTips.gameObject.SetActive(true);
            }
            else
            {
                singleSTips.gameObject.SetActive(true); 
            }
        }

        if (rewardRsp.replaceRewardList != null && rewardRsp.replaceRewardList.Count > 0)
        {
            replaceTips.text = "重复获得的奖励已经转化为";
            for (int i = 0, C = rewardRsp.replaceRewardList.Count; i < C; i++)
            {
                if (i > 0) replaceTips.text += "和";
                replaceTips.text += $"{rewardRsp.replaceRewardList[i].amount}{PgcUtils.GetRewardName((BUDRewardType)rewardRsp.replaceRewardList[i].rewardType)}";
            }
        }
        else
        {
            replaceTips.text = "";
        }
    }
    
    private void FillCommonRewardItem(RewardInfo data, bool IsTen)
    {
        var content = GameObjectEx.FindChildByName(IsTen ? TenObj : SingleObj, "Content");
        if (content == null)
        {
            return;
        }
        var rewardGo = GameObject.Instantiate(rewardItem, content).GetComponent<GashaponRewardItemView>();
        rewardGo.SetData(data);
        rewardGo.gameObject.SetActive(true);
    }
    
    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
        if (GashaponDataManager.Inst.RewardAc != null)
        {
            GashaponDataManager.Inst.RewardAc?.Invoke();
            GashaponDataManager.Inst.RewardAc = null;
        }
        base.OnDestroy();
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}