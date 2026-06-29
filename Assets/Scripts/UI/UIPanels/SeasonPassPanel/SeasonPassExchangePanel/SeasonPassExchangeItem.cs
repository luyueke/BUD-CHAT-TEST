using System;
using System.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassExchangeItem : MonoBehaviour
{
    [SerializeField] private Button actionBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private Text priceNum;
    [SerializeField] private Text rewardNumText;
    [SerializeField] private Text limitTxt;
    [SerializeField] private Image giftImg;
    [SerializeField] private HorizontalLayoutGroup group;
    [SerializeField] private Text Name;

    private SeasonExchangeConfig _data;
    private Action<SeasonPassExchangeItem, SeasonExchangeConfig> clickAction;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";

    private void Awake()
    {
        actionBtn?.onClick.AddListener(OnClickItem);
    }

    public void Init(SeasonExchangeConfig info, Action<SeasonPassExchangeItem, SeasonExchangeConfig> action)
    {
        this._data = info;
        clickAction = action;

        limitTxt.text = $"限购次数：{0}/{info.Count}";

        giftImg.gameObject.SetActive(info.Gift);
        if (!info.Gift)
        {
            var tem = info.RewardList.First();
            iconImg.sprite = PgcUtils.LoadRewardIcon(tem.RewardType, gameObject);
            iconImg.transform.localScale = Vector3.one * 0.5f;
            rewardNumText.text = "x" + tem.Count;
            Name.text = "";
        }
        else
        {
            iconImg.sprite = PgcUtils.LoadBundleIcon(info.BundleID, gameObject);
            iconImg.SetNativeSize();
            if (info.BundleID == "199")
            {
                iconImg.transform.localScale = Vector3.one * 0.5f;
            //    iconImg.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);
            }else
            {

                iconImg.transform.localScale = Vector3.one * 1f;
            }
            if (info.BundleID == "210")
            {
                iconImg.transform.localScale = Vector3.one * 0.25f;
            }
            if (info.BundleID == "211")
            {
                iconImg.transform.localScale = Vector3.one * 0.45f;
            }
            rewardNumText.text = "";
            var w = (float)iconImg.rectTransform.sizeDelta.x / iconImg.rectTransform.sizeDelta.y;
            if (w > 0.9f)
            {
                group.spacing = -40;
            }
            else if (w > 0.5f)
            {
                group.spacing = -10;
            }
            Name.text = info.Name;
        }

        priceNum.text = info.Price.ToString();
        //bool isOwned = GashaponUtils.IsOwnedReward(info);
        //priceNum.text = info.Price.ToString();
        //priceObj.SetActive(!isOwned);
        //ownedObj.SetActive(isOwned);
    }

    public void UpdateTimes()
    {
        if (_data != null) {
            limitTxt.text = $"限购次数：{SeasonPassExchangeSystem.Inst.GetTime(_data.ExchangeId)}/{_data.Count}";
        }
    }

    public void InitSprite(SeasonExchangeConfig data)
    {
        //货币类
        //var currencyType = GameUtils.ConvertRewardType((int)data.RewardType);
        //if (data.PgcDatas == null && currencyType != CurrencyType.None)
        //{
        //    PgcUtils.LoadCurrencyIconAsync(currencyType, gameObject, (iconSprite) =>
        //    {
        //        if (this != null && iconImg != null && iconSprite != null)
        //        {
        //            iconImg.sprite = iconSprite;
        //        }
        //    });
        //}
        //else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.Avatar)
        //{//皮肤
        //    if (!string.IsNullOrEmpty(data.BundleId))
        //    {
        //        PgcUtils.LoadBundleIconAsync(data.BundleId, gameObject, (iconSprite) =>
        //        {
        //            if (this != null && iconImg != null && iconSprite != null)
        //            {
        //                iconImg.sprite = iconSprite;
        //            }
        //        });
        //    }
        //    else
        //    {
        //        PgcUtils.LoadAvatarIconAsync(data.Id, gameObject, (iconSprite) =>
        //        {
        //            if (this != null && iconImg != null && iconSprite != null)
        //            {
        //                iconImg.sprite = iconSprite;
        //            }
        //        });
        //    }
        //
        //
        //}
        //else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.PGCPetAvatar)
        //{//Pet皮肤
        //
        //    PgcUtils.LoadPetAvatarIconAsync(data.Id, gameObject, (iconSprite) =>
        //    {
        //        if (this != null && iconImg != null && iconSprite != null)
        //        {
        //            iconImg.sprite = iconSprite;
        //        }
        //    });
        //}
        //else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.Emote)
        //{ //表情
        //    PgcUtils.LoadEmoteIconAsync(data.Id, gameObject, (iconSprite) =>
        //    {
        //        if (this != null && iconImg != null && iconSprite != null)
        //        {
        //            iconImg.sprite = iconSprite;
        //        }
        //    });
        //}
        //else if (data.RewardType == RewardType.RewardAvatarFrame)
        //{ //头像框
        //    UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
        //    {
        //        if (this != null && iconImg != null && iconSprite != null)
        //        {
        //            iconImg.gameObject.SetActive(true);
        //            iconImg.sprite = iconSprite;
        //        }
        //    });
        //}
        //else if (data.RewardType == RewardType.RewardChatBubbles)
        //{ //聊天气泡
        //    UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
        //    {
        //        if (this != null && iconImg != null && iconSprite != null)
        //        {
        //            iconImg.gameObject.SetActive(true);
        //            iconImg.sprite = iconSprite;
        //        }
        //    });
        //}
    }

    public void SetSelectStatus(bool isSelect)
    {

    }

    public SeasonExchangeConfig GetBindData()
    {
        return _data;
    }

    public void OnClickItem()
    {
        //if (SeasonPassExchangeSystem.Inst.GetTime(_data.ExchangeId) >= _data.Count)
        //{
        //    TipPanel.ShowToast("已达上限");
        //    return;
        //}
        clickAction?.Invoke(this, _data);
        SetSelectStatus(true);
    }
}
