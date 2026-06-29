using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class UMIMusicalNoteSkiPanel : BaseGashaponView
{
    [SerializeField] protected string bundleViewBgColor = "#FF9B9B";

    [Header("按钮相关")]
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton SeasonBtn;
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton TwistBtn;
    [SerializeField] private Text singleText;
    [Header("人物展示")]
    [SerializeField] private GameObject playerImageView;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;


    public Text tipsText;

    public List<SnowBoardItem> leftItems;

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        PreviewBtn.onClick.AddListener(OnPriviewBtnClick);
        SeasonBtn.onClick.AddListener(OnSeasonBtnClick);
        InfoBtn.onClick.AddListener(OnInfoClick);
        TwistBtn.onClick.AddListener(OnTwistClick);
    }


    public override void OnShow()
    {
        base.OnShow();

        if (gashaponInfoRsp != null)
        {
            singleText.SetText(gashaponInfoRsp.singleDrawPrice.ToString());
        }
        GashaponDataManager.Inst.RequestGashaponInfo("lottery.musicalPuddingSkateboard", OnGashaponInfoUpdate);
    }

    private void InitItem()
    {
        TwistBtn.gameObject.SetActive(false);
        tipsText.text = "恭喜！你已集齐umi音符滑雪板奖池所有商品！";
        avatarCameraController.RotateTarget = characterRoot;
        for (int i = 0; i < leftItems.Count; i++)
        {
            bool ishave = false;
            leftItems[i].Init(out ishave, OnPriviewBtnClick);

            if (i == 5)
            {
                for (int j = 0; j < gashaponData.RewardList.Count; j++)
                {
                    if(gashaponData.RewardList[j].Id == "93_30" && gashaponInfoRsp.rewardPool != null)
                    {
                        var drawnInfo = gashaponInfoRsp.rewardPool.Find(tmp => tmp.rewardId == gashaponData.RewardList[j].RewardId);
                        if (drawnInfo != null)
                        {
                            leftItems[i].hasImage.gameObject.SetActive(drawnInfo.everDrawn == 1);
                        }
                        else
                        {
                            bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[j]);
                            leftItems[i].hasImage.gameObject.SetActive(isOwned);
                        }
                        break;
                    }
                }
                continue;
            }
            if (!ishave)
            {
                TwistBtn.gameObject.SetActive(true);
                tipsText.text = "每次抽取都不会重复奖励，9次之内必解锁umi音符滑雪板";
            }
        }

        if (gashaponInfoRsp != null)
        {
            singleText.SetText(gashaponInfoRsp.singleDrawPrice.ToString());
        }
    }

    private void OnPriviewBtnClick()
    {
        if(gashaponData == null)
        {
            Debug.LogError("gashaponData is null!");
            return;
        }
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = "UMI音符滑雪板",
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.MiaoCoin,
            rulePath = viewCfg.RulePath,
            gashaponInfoRsp = gashaponInfoRsp
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }

    private void OnSeasonBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.SecUMIMusicalGiftPackPanel);
    }

    private void OnInfoClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var rulePath = viewCfg.RulePath;
        if (string.IsNullOrEmpty(rulePath))
        {
            rulePath = "Assets/Loadable/UI/UIPanel/MusicPuddingPanel/Rule.json";
        }

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null)
        {
            return;
        }

        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.MiaoCoin);
        if (gashaponInfoRsp.singleDrawDiscountedPrice > num)
        {
            int lessNum = gashaponInfoRsp.singleDrawDiscountedPrice - num;
            UIManager.Inst.OpenPanel(PanelId.CatRechargePanel, lessNum);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.MiaoCoin, gashaponInfoRsp.singleDrawPrice))
        {
            var val = gashaponInfoRsp.singleDrawPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.MiaoCoin, val);
            return;
        }

        var gId = "lottery.musicalPuddingSkateboard";
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        if (infoRsp == null)
        {
            return;
        }
        gashaponInfoRsp = infoRsp;
        InitItem();
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        base.OnGashaOnceRsp(gashaponRsp);
        Debug.Log("UMIMusicalNoteSkiPanel.OnGashaOnceRsp: " + gashaponRsp);
        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
        new GashaponTwistAnimParam()
        {
            gashaponId = gashaponData.Id
        });
        panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () => {
            OnGashaTwistAnimComplete(gashaponRsp);
        });
        InitItem();
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
    {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponId, gashaponRsp);

        if (UIManager.Inst.TryFindPanel<CreatorRewardPanel>(WindowId.CreatorWindow, PanelId.CreatorCenterPanel,
                out var creatorRewardPanel) && gashaponData.CurrencyType == CurrencyType.GreenCoin)
        {
            creatorRewardPanel.Refresh();
        }
    }
}
