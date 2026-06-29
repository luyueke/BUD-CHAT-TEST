using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Es;
using UI.Manager;

public class SeasonPassPaidItem : MonoBehaviour
{
    public Image Img_Icon;

    public Text Txt_Title;

    public Text Txt_Title_CN;
    public GameObject LockMask;
    public GameObject Icon_Received;
    public GameObject Icon_Lock;
    public GameObject Icon_AvailableBg;
    public Button Btn_Item;
    public Animator animator;

    public GameObject DesBtn;

    public SeasonPassItemInfo curData;
    private Sprite rewardSp;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
    private Action<SeasonPassItemInfo> ClickAction;

    private void OnEnable()
    {
        if (curData != null)
        {
            SetState(curData.BudRewardStatus);
        }
    }

    public void Init(SeasonPassItemInfo data, Action<SeasonPassItemInfo> ClickAction, bool isFirst = false)
    {

        this.curData = data;
        this.ClickAction = ClickAction;
        var iconName = data.IconName();

        if (!string.IsNullOrEmpty(iconName))
        {
            rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, this.gameObject);
        }

        if (rewardSp == null)
        {
            LoggerUtils.LogError("rewardSp is null:" + rewardSp + "," + data.BudRewardType);
        }

        Img_Icon.sprite = rewardSp;

        var amount = 0;
        if (data?.rewardInfo?.itemList?.Count > 0)
        {
            amount = data?.rewardInfo.itemList.First().amount ?? 0;
        }

        DesBtn.gameObject.SetActive(data.IsPgcCloth() || data.BudRewardType == BUDRewardType.RewardSeasonPassRandomPack);

        Txt_Title.gameObject.SetActive(false);

        var pgcId = data.rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";

        //Debug.LogError("SeasonItem init pgcid=" + pgcId);
        //Debug.LogError("SeasonItem init iconName=" + iconName);
        if (amount > 1)
        {
            //Txt_Title.transform.parent.gameObject.SetActive(true);
            //Txt_Title.text = "x" + amount.ToString();
            Txt_Title_CN.text = "x" + amount.ToString();
        }
        else
        {
            //Txt_Title.transform.parent.gameObject.SetActive(false);
            Txt_Title_CN.text = PgcUtils.GetRewardName(data.rewardInfo.itemList.First().BudRewardType);
            if (data.BudRewardType == BUDRewardType.RewardPgcResource)
            {
                if (!string.IsNullOrEmpty(pgcId))
                {
                    GameResData resData = Es.DataTables.GetGameResData(pgcId);
                    if (resData != null)
                    {
                        Txt_Title_CN.gameObject.SetActive(true);
                        Txt_Title.gameObject.SetActive(false);
                        if (resData.ResourceType == (int)GameData.PgcData.ResourceType.Emote)
                        {
                            Txt_Title_CN.SetLocalText(PgcUtils.GetEmoteName(pgcId));
                        }
                        //Txt_Title_CN.fontSize = 24;
                        //Txt_Title_CN.resizeTextMaxSize = 24;
                        //RectTransform rectTransform = Txt_Title_CN.GetComponent<RectTransform>();
                        //Vector2 currentPosition = rectTransform.anchoredPosition;
                        //rectTransform.anchoredPosition = new Vector2(currentPosition.x, 40);
                    }
                }
            }
        }

        if (Txt_Title_CN.text == "")
        {
            Txt_Title_CN.SetLocalText(SeasonPassDataManager.Inst.GetSeasonPassRewardAvatarName(pgcId));
        }
        SetState(data.BudRewardStatus);
        Btn_Item.onClick.RemoveAllListeners();
        Btn_Item.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        if (curData == null)
        {
            return;
        }
        this.ClickAction?.Invoke(curData);
    }

    public void SetState(BudRewardStatus state)
    {
        switch (state)
        {
            case BudRewardStatus.Lock:
                Icon_Lock.SetActive(true);
                LockMask.SetActive(false);
                Icon_Received.SetActive(false);
                Icon_AvailableBg.SetActive(false);
                animator.enabled = false;
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
                break;
        }
    }

    public void SyncData(List<SeasonPassItemInfo> infos)
    {
        if (infos == null)
            return;
        var info = infos.Find(x => x.rewardId == curData.rewardId);
        if (info != null)
        {
            curData.rewardStatus = info.rewardStatus;
            SetState(curData.BudRewardStatus);
        }
    }

    public void SyncRewardState(string rewardId)
    {
        if (rewardId == curData.rewardId)
        {
            curData.rewardStatus = (int)BudRewardStatus.Claimed;
            SetState(curData.BudRewardStatus);
        }
    }
}
