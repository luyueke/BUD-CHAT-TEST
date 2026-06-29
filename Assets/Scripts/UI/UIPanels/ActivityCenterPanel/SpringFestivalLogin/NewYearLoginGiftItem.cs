using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearLoginGiftItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private GameObject doneRoot;
    [SerializeField] private GameObject missingRoot;
    [SerializeField] private GameObject tomorrowRoot;
    [SerializeField] private Animator claimAnimator;
    [SerializeField] private Image icon;
    [SerializeField] private Text num;

    private ActivityEventInfo mEventInfo;
    private ActivityRewardInfo mRewardInfo;
    private Action<NewYearLoginGiftItem> onClaimAction;

    public int EventId => mEventInfo.eventId;

    private void Awake()
    {
        button.onClick.AddListener(OnClick);
    }

    public void InitData(ActivityEventInfo eventInfo, ActivityRewardInfo rewardInfo, Action<NewYearLoginGiftItem> action)
    {
        mEventInfo = eventInfo;
        mRewardInfo = rewardInfo;
        onClaimAction = action;

        Refresh();
    }

    public void Refresh()
    {
        lockRoot.SetActive(false);
        missingRoot.SetActive(false);
        doneRoot.SetActive(false);
        tomorrowRoot?.SetActive(false);
        button.gameObject.SetActive(false);
        claimAnimator.Play("None");
        switch((CurrencyType)mRewardInfo.budRewardType)
        {
            case CurrencyType.LuckyCoin:
            case CurrencyType.MagicCoin:
            case CurrencyType.YouYouCoin:
            case CurrencyType.PurpleDreamCoin:
            case CurrencyType.ChristmasCoin:
                string spriteName = PgcUtils.CurrencyIconPath[(CurrencyType)mRewardInfo.budRewardType];
                Sprite rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
                icon.sprite = rewardSp;
                break;
        }
        num.text = "x" + mRewardInfo.rewardNum;
        switch ((ClaimStatus)mEventInfo.eventStatus)
        {
            case ClaimStatus.Expired:
                Missing();
                break;
            case ClaimStatus.Lock:
                Lock();
                break;
            case ClaimStatus.Unlocked:
                Claim();
                break;
            case ClaimStatus.Claimed:
                Done();
                break;
            case ClaimStatus.ErrStatus:
                break;
        }
    }

    public void SetTomorrow()
    {
        lockRoot.SetActive(false);
        tomorrowRoot?.SetActive(true);
    }
    private void Lock()
    {
        lockRoot.SetActive(true);
        return;
        //var cur = TcpTimeSystem.Inst.ServerTime; //服务器时间
        //var dt0 = TimeTools.SecondsToDateTime(cur);
        //dt0 = dt0.AddDays(1); //明天
        //var num1 = TimeTools.DateTimeToSeconds(dt0);

        //DateTime dt = new DateTime(2026, 2, 16); //因为EventID从1开始 ，所以只能从16号开始算，不能算17（初一）
        //var num2 = TimeTools.DateTimeToSeconds(dt.AddDays(EventId ));
        //var num3 = TimeTools.DateTimeToSeconds(dt.AddDays(EventId + 1));

        //dt0 = TimeTools.SecondsToDateTime(num1);
        //dt0 = dt0.AddDays(16);
        //num1 = TimeTools.DateTimeToSeconds(dt0);
        //DateTime dt2 = dt.AddDays(EventId);
        //DateTime dt3 = dt.AddDays(EventId + 1);
        //Debug.LogError("lock dt0=" + dt0.ToString("yyyy-MM-dd HH:mm:ss") + ",,,,,, cur=" + num1);
        //Debug.LogError("lock dt2=" + dt2.ToString("yyyy-MM-dd HH:mm:ss") + ",,,,,, cur=" + num2);
        //Debug.LogError("lock dt3=" + dt3.ToString("yyyy-MM-dd HH:mm:ss") + ",,,,,, cur=" + num3);

        //if (num1 >= num2 && num1 < num3) //明天
        //{
        //    tomorrowRoot?.SetActive(true);
        //}else
        //{
        //    lockRoot.SetActive(true);
        //}
    }

    private void Done()
    {
        doneRoot.SetActive(true);
    }

    private void Claim()
    {
        claimAnimator.Play("Claim");
        button.gameObject.SetActive(true);
    }

    private void Missing()
    {
        missingRoot.SetActive(true);
    }

    private void OnClick()
    {
        if (mEventInfo == null) return;
        if (mEventInfo.eventStatus == (int)ClaimStatus.Unlocked)
        {
            onClaimAction?.Invoke(this);
        }
        else if (mEventInfo.eventStatus == (int)ClaimStatus.Lock)
        {
            TipPanel.ShowToast("时间未到，敬请期待！");
        }
    }

}
