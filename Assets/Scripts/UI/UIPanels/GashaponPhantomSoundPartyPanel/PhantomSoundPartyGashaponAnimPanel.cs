using System;
using System.Collections;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using GameData.Rewards;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;



public class PhantomSoundPartyGashaponAnimPanel : BasePanel<PhantomSoundPartyGashaponAnimPanel>
{
    [SerializeField] private GameObject nextRoundRoot;
    [SerializeField] private GameObject bigRewardRoot;
    [SerializeField] private GameObject playBigRewardSelect;

    [SerializeField] private GameObject lightUpStarRoot;
    [SerializeField] private GameObject animMaskRoot; //动画遮罩
    [SerializeField] private CButton receiveBtn;
    [SerializeField] private CButton receiveBtn1;
    [SerializeField] private Text receiveBtn1Count;
    [SerializeField] private Button backBtn;
    [SerializeField] private Image rewardItemImg;
    [SerializeField] private Text rewardNumText;
    [SerializeField] private Text lightUpStarText;
    [SerializeField] private Image img_yinfu;
    [SerializeField] private Image img_love;
    [SerializeField] private Transform MusicImg;
    [SerializeField] private Transform WaImg;
    [SerializeField] private Transform StarImg;

    [SerializeField] private Button playBigRewardItem0;
    [SerializeField] private Button playBigRewardItem1;
    [SerializeField] private Button playBigRewardItem2;
    private Dictionary<Button, string> playBigRewardBtnDic = new Dictionary<Button, string>();

    private string _lotteryId;
    private string _selectedPgcId = "160100009";
    private bool _isSelectingOptionalBox;

    bool isShowBigReward = false;

    public Action onBackCallBack;
    public Action onReceiveFinish;

    public void SetLotteryId(string lotteryId) => _lotteryId = lotteryId;

    public override void OnCreate()
    {
        base.OnCreate();
        playBigRewardBtnDic = new Dictionary<Button, string>()
        {
            {playBigRewardItem0, "160100009"},
            {playBigRewardItem1, "160100010"},
            {playBigRewardItem2, "160100011"}
        };

        foreach (var kv in playBigRewardBtnDic)
        {
            var btn = kv.Key;
            var pgcId = kv.Value;
            btn.onClick.AddListener(() => OnSelectBigRewardItem(btn, pgcId));
        }

        receiveBtn.onClick.AddListener(OnReceiveBtnClick);
        if (receiveBtn1 != null) receiveBtn1.onClick.AddListener(OnReceive1BtnClick);
        backBtn.onClick.AddListener(OnBackBtnClick);
    }

    private void OnSelectBigRewardItem(Button btn, string pgcId)
    {
        _selectedPgcId = pgcId;
        btn.transform.SetSiblingIndex(3);
        int unselectedIdx = 0;
        foreach (var kv in playBigRewardBtnDic)
        {
            if (kv.Key != btn)
                kv.Key.transform.SetSiblingIndex(unselectedIdx++);
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        onBackCallBack = null;
        onReceiveFinish = null;
        if (args != null && args.Length > 0)
        {
            onBackCallBack = args[0] as Action;
        }
        nextRoundRoot.SetActive(false);
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        if (playBigRewardSelect != null) playBigRewardSelect.SetActive(false);
        animMaskRoot.SetActive(true);
    }

    public void ShowYinfu()
    {
        img_yinfu?.gameObject.SetActive(true);
        img_love?.gameObject.SetActive(false);
    }
    public void ShowLove()
    {
        img_love?.gameObject.SetActive(true);
        img_yinfu?.gameObject.SetActive(false);
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
            if (_isSelectingOptionalBox)
            {
                UIManager.Inst.OpenPanel<PhantomSoundPartySelectPanel>(PanelId.PhantomSoundPartySelectPanel,
                    _selectedPgcId, (Action)RequestSpecialButton);
                return;
            }
            openNewRound();
            isShowBigReward = false;
            return;
        }
        animMaskRoot.SetActive(false);
        nextRoundRoot.SetActive(false);
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        if (playBigRewardSelect != null) playBigRewardSelect.SetActive(false);
        CloseSelf();
    }
    private void OnReceive1BtnClick()
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

    private void RequestSpecialButton()
    {
        if (string.IsNullOrEmpty(_selectedPgcId)) return;
// #if UNITY_EDITOR
//         OnSpecialButtonSuccess();
//         return;
// #endif
        JObject req = new JObject()
        {
            ["lotteryId"] = _lotteryId,
            ["buttonType"] = (int)GashaponSpecialButton.PgcOptionalBox,
            ["pgcId"] = _selectedPgcId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GashaponSpecialButton, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            onReceive: _ => OnSpecialButtonSuccess(),
            onFail: err =>
            {
                LoggerUtils.LogError("幻音派对大奖选择失败: " + err);
                TipPanel.ShowToast(err);
            });
    }

    private void OnSpecialButtonSuccess()
    {
        _isSelectingOptionalBox = false;
        isShowBigReward = false;
        if (playBigRewardSelect != null) playBigRewardSelect.SetActive(false);
        openNewRound();
    }

    /// <summary>
    /// 主面板检测到未完成的3选1时直接显示选择界面
    /// </summary>
    public void ShowPlayBigRewardSelect()
    {
        nextRoundRoot.SetActive(false);
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        animMaskRoot.SetActive(false);
        _isSelectingOptionalBox = true;
        isShowBigReward = true;
        if (playBigRewardSelect != null)
        {
            playBigRewardSelect.SetActive(true);
            OnSelectBigRewardItem(playBigRewardItem0, "160100009");
        }
    }


    /// <summary>
    /// 显示大奖
    /// </summary>
    public void showBigReward(RewardInfo rewardData)
    {
        if (rewardData == null)
        {
            LoggerUtils.LogError("PhantomSoundPartyGashaponAnimPanel.showBigReward: rewardData is null");
            return;
        }
        if (string.IsNullOrEmpty(rewardData.pgcId) || rewardData.pgcId == "0")
        {
            rewardItemImg.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject);
        }
        else
        {
            rewardItemImg.sprite = PgcUtils.GetIconSpriteByPgcId(rewardData.pgcId, gameObject);
        }
        receiveBtn1Count.text = "x" + rewardData.amount.ToString();
        nextRoundRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        isShowBigReward = true;

        if (rewardData.rewardType == (int)BUDRewardType.RewardPgcOptionalBox)
        {
            _isSelectingOptionalBox = true;
            bigRewardRoot.SetActive(false);
            if (playBigRewardSelect != null) playBigRewardSelect.SetActive(true);
        }
        else
        {
            if (playBigRewardSelect != null) playBigRewardSelect.SetActive(false);
            bigRewardRoot.SetActive(true);

            if (rewardData.rewardType == (int)BUDRewardType.RewardCrystal || rewardData.rewardType == (int)BUDRewardType.RewardMusicNoteCrystal
                || rewardData.rewardType == (int)BUDRewardType.RewardTypeSockTailTicket || rewardData.rewardType == (int)BUDRewardType.RewardTypeSockYunyunTicket)
            {
                if(rewardNumText)
                    rewardNumText.text = "x" + rewardData.amount.ToString();
            }
            else
            {
                if(rewardNumText)
                    rewardNumText.text = "";
            }
        }
    }

    public void SetTypeImage(int type)
    {
        MusicImg.gameObject.SetActive(type <= 0);
        WaImg.gameObject.SetActive(type == 1);
        StarImg.gameObject.SetActive(type == 2);
    }

    /// <summary>
    /// 开启新转盘
    /// </summary>
    public void openNewRound()
    {
        bigRewardRoot.SetActive(false);
        lightUpStarRoot.SetActive(false);
        nextRoundRoot.SetActive(true);
        animMaskRoot.SetActive(true);
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
