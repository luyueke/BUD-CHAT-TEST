using System;
using System.Collections.Generic;
using System.Linq;
using Es;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class NewYearLoginPaidItem : MonoBehaviour {
    public Image Img_Icon;

    /// <summary>
    /// 显示数字
    /// </summary>
    public Text Txt_Title;

    /// <summary>
    /// 显示中文
    /// </summary>
    public Text Txt_Title_CN;

    public GameObject LockMask;
    public GameObject Icon_Received;
    public GameObject Icon_Lock;
    public GameObject Icon_AvailableBg;
    public Button Btn_Item;
    public Animator animator;
    public Image ImgEffect;

    public SeasonPassItemInfo curData;
    public Sprite rewardSp;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
    private Action<SeasonPassItemInfo> ClickAction;

    private void OnEnable() {
        if (curData != null) {
            SetState(curData.BudRewardStatus);
        }
    }

    public void Init(SeasonPassItemInfo data, Action<SeasonPassItemInfo> ClickAction) {
        this.curData = data;
        this.ClickAction = ClickAction;

        var pgcId = data.rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";
        // 这里要用带黑边的图标，所以这样写了
        if (data.BudRewardType == BUDRewardType.RewardPgcResource && pgcId != "40300436") {
            PgcUtils.GetIconSpriteByPgcIdAsync(pgcId, gameObject, sp => {
                rewardSp = sp;
                Img_Icon.sprite = rewardSp;
            });
        } else {
            var iconName = data.IconName();
            if (!string.IsNullOrEmpty(iconName)) {
                rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, this.gameObject);
            }
            if (rewardSp == null) {
                LoggerUtils.LogError("rewardSp is null:" + rewardSp + "," + data.BudRewardType);
            }

            Img_Icon.sprite = rewardSp;
        }




        if (data.BudRewardType == BUDRewardType.RewardPgcResource) {
            Img_Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(253, 253);
        } else {
            Img_Icon.GetComponent<RectTransform>().sizeDelta = new Vector2(211, 211);
        }

        var amount = 0;
        if (data?.rewardInfo?.itemList?.Count > 0) {
            amount = data?.rewardInfo.itemList.First().amount ?? 0;
        }

        Txt_Title.gameObject.SetActive(amount > 0);
        if (amount > 0) {
            Txt_Title.text = "x" + amount.ToString();
            Txt_Title_CN.gameObject.SetActive(false);
        }
    //    Debug.LogError($"NewYearItem Init type={data.BudRewardType},pgcId={pgcId},name={SeasonPassDataManager.Inst.GetPgcName(pgcId)}");
        if (data.BudRewardType == BUDRewardType.RewardPgcResource) {
            if (!string.IsNullOrEmpty(pgcId)) {
                Txt_Title_CN.SetLocalText(SeasonPassDataManager.Inst.GetPgcName(pgcId));
                Txt_Title_CN.gameObject.SetActive(true);
                Txt_Title.gameObject.SetActive(false);
            }
        }

        if (data.BudRewardType == BUDRewardType.RewardAvatarFrame)
        {
            Txt_Title_CN.SetLocalText("心动头像框");
            Txt_Title_CN.gameObject.SetActive(true);
            Txt_Title.gameObject.SetActive(false);
        }

        SetState(data.BudRewardStatus);
        Btn_Item.onClick.RemoveAllListeners();
        Btn_Item.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick() {
        if (curData == null) {
            return;
        }

        this.ClickAction?.Invoke(curData);
    }

    public void SetState(BudRewardStatus state) {
        switch (state) {
            case BudRewardStatus.Lock:
                Icon_Lock.SetActive(true);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                Icon_AvailableBg.SetActive(false);
                animator.enabled = false;
                ImgEffect.color = new Color(255, 255, 255, 0);
                break;
            case BudRewardStatus.Unlocked:
                Icon_Lock.SetActive(false);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                Icon_AvailableBg.SetActive(true);
                animator.enabled = true;
                animator.CrossFade("LoginGiftPanel_prompt", 0.1f);
                break;
            case BudRewardStatus.Claimed:
                Icon_Lock.SetActive(false);
                animator.enabled = false;
                LockMask.SetActive(true);
                Icon_Received.SetActive(true);
                Icon_AvailableBg.SetActive(false);
                ImgEffect.color = new Color(255, 255, 255, 0);
                break;
        }
    }

    public void SyncData(List<SeasonPassItemInfo> infos) {
        if (infos == null)
            return;
        var info = infos.Find(x => x.rewardId == curData.rewardId);
        if (info != null) {
            curData.rewardStatus = info.rewardStatus;
            SetState(curData.BudRewardStatus);
        }
    }

    public void SyncRewardState(string rewardId) {
        if (rewardId == curData.rewardId) {
            curData.rewardStatus = (int)BudRewardStatus.Claimed;
            SetState(curData.BudRewardStatus);
        }
    }
}
