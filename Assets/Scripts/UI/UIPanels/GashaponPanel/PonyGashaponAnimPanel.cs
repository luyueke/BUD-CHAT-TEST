using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using GameData.Rewards;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;



public class PonyGashaponAnimPanel : BasePanel<PonyGashaponAnimPanel>
{
    [SerializeField] private GameObject nextRoundRoot;
    [SerializeField] private GameObject bigRewardRoot;
    [SerializeField] private GameObject lightUpStarRoot;
    [SerializeField] private GameObject animMaskRoot; //动画遮罩
    [SerializeField] private CButton receiveBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Image rewardItemImg;
    [SerializeField] private Text rewardNumText;
    [SerializeField] private Text lightUpStarText;
    [SerializeField] private Image img_yinfu;
    [SerializeField] private Image img_love;
    [SerializeField] private Transform MusicImg;
    [SerializeField] private Transform WaImg;
    [SerializeField] private Transform StarImg;
    [SerializeField] private Transform FishImage;
    [SerializeField] private Image BigRewardBgImg;

    bool isShowBigReward = false;


    public Action onBackCallBack;
    public Action onReceiveFinish; // 领取流程最终关闭后回调(仅需要的面板实例设置，其它为 null)

    public override void OnCreate()
    {
        base.OnCreate();
        receiveBtn.onClick.AddListener(OnReceiveBtnClick);
        backBtn.onClick.AddListener(OnBackBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            onBackCallBack = args[0] as Action;
        }
        nextRoundRoot.SetActive(false);
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        animMaskRoot.SetActive(true);
    }

    public void ShowYinfu()
    {
        img_yinfu.gameObject.SetActive(true);
        img_love.gameObject.SetActive(false);
    }
    public void ShowLove()
    {
        img_love.gameObject.SetActive(true);
        img_yinfu.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


    private void OnBackBtnClick()
    {
        if (isShowBigReward)
        {
            return;
        }
        CloseSelf();
        onBackCallBack?.Invoke();
    }

    private void OnReceiveBtnClick()
    {
        if (isShowBigReward)
        {
            openNewRound();
            isShowBigReward = false;
            return;
        }
        animMaskRoot.SetActive(false);
        nextRoundRoot.SetActive(false);
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        CloseSelf();
        onReceiveFinish?.Invoke();
    }


    /// <summary>
    /// 显示大奖
    /// </summary>
    public void showBigReward(RewardInfo rewardData)
    {
        if (string.IsNullOrEmpty(rewardData.pgcId) || rewardData.pgcId == "0")
        {
            rewardItemImg.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject);
        }
        else
        {
            rewardItemImg.sprite = PgcUtils.GetIconSpriteByPgcId(rewardData.pgcId, gameObject);
        }
        nextRoundRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        bigRewardRoot.SetActive(true);
        isShowBigReward = true;

        if (rewardData.rewardType == (int)BUDRewardType.RewardCrystal || rewardData.rewardType == (int)BUDRewardType.RewardMusicNoteCrystal
            || rewardData.rewardType == (int)BUDRewardType.RewardTypeSockTailTicket || rewardData.rewardType == (int)BUDRewardType.RewardTypeSockYunyunTicket)
        {
            rewardNumText.text = "x" + rewardData.amount.ToString();
        }
        else
        {
            rewardNumText.text = "";
        }
    }

    // 替换惊喜大奖背景图(娃娃机进入时由 WawajiPanel 传入专属背景)
    public void SetBigRewardBg(Sprite sprite)
    {
        if (BigRewardBgImg != null && sprite != null) BigRewardBgImg.sprite = sprite;
    }

    public void SetTypeImage(int type)
    {
        MusicImg.gameObject.SetActive(type <= 0);
        WaImg.gameObject.SetActive(type == 1);
        StarImg.gameObject.SetActive(type == 2);
        if (FishImage != null) FishImage.gameObject.SetActive(type == 3); // 娃娃机(小鱼)
    }

    /// <summary>
    /// 开启新转盘
    /// </summary>
    public void openNewRound()
    {
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        nextRoundRoot.SetActive(true);
    }
    /// <summary>
    /// 点亮星星
    /// </summary>
    public void lightUpStar()
    {
        nextRoundRoot.SetActive(false);
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(true);
    }

    public void setLightUpStarText(string text)
    {
        lightUpStarText.text = text;
    }

}
