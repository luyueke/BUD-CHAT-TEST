using System;
using Basic.Utils;
using Game.Store;
using GameData.PgcData;
using Newtonsoft.Json;
using Product;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponExchangeItem : MonoBehaviour
{
    [SerializeField] private Color selectColor;
    [SerializeField] private Color normalColor;
    [SerializeField] private Image bgImage;
    [SerializeField] private Button actionBtn;
    [SerializeField] private Image iconImg;
    [SerializeField] private Text priceNum;
    [SerializeField] private Image priceIcon;
    [SerializeField] private Image colorBg;
    [SerializeField] private GameObject priceObj;
    [SerializeField] private GameObject ownedObj;
    [SerializeField] private Text rewardNumText;

    private GashaponExchangeData _data;
    private Action<GashaponExchangeItem,GashaponExchangeData> clickAction;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";

    private void Awake()
    {
        actionBtn?.onClick.AddListener(OnClickItem);
    }
    
    public void Init(GashaponExchangeData info, Action<GashaponExchangeItem,GashaponExchangeData> action)
    {
        this._data = info;
        clickAction = action;
        
        InitSprite(info);
        string pgcId = "";
        if (GashaponUtils.HasPGCData(info))
        {
            pgcId = info.PgcDatas[0].Id;
        }

        if (!string.IsNullOrEmpty(pgcId))
        {
            rewardNumText.gameObject.SetActive(false);
        } else {
            if (info.Num > 1) {
                rewardNumText.gameObject.SetActive(true);
                rewardNumText.SetText($"x {info.Num}");
            } else {
                rewardNumText.gameObject.SetActive(false);
            }
        }
        
        bool isOwned = GashaponUtils.IsOwnedReward(info);
        priceNum.text = info.Price.ToString();
        priceObj.SetActive(!isOwned);
        ownedObj.SetActive(isOwned);
        
        string priceIconName = PgcUtils.GetScopeCurrencyIconName(info.CurrencyType);
        if (string.IsNullOrEmpty(priceIconName) && PgcUtils.CurrencyIconPath.ContainsKey(info.CurrencyType))
            priceIconName = PgcUtils.CurrencyIconPath[info.CurrencyType];
        if (!string.IsNullOrEmpty(priceIconName))
        {
            priceIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, priceIconName, gameObject);
        }
        
        
    }

    public void SetItemColor(Color bgColor)
    {
        colorBg.color = bgColor;
    }

    public void SetItemColor(string bgColor)
    {
        var color = DataUtil.DeSerializeColorCheckHash(bgColor);
        colorBg.color = color;
    }

    public void InitSprite(GashaponExchangeData data)
     {
        // 诊断：兑换项缺少 Id 时大概率无法定位图标资源
        if (string.IsNullOrEmpty(data.Id))
        {
            Debug.LogWarning($"[GashaponExchangeItem] 兑换项缺少 Id，RewardType={data.RewardType} BundleId={data.BundleId} HasPGC={GashaponUtils.HasPGCData(data)}");
        }

        //货币类
        var currencyType = GameUtils.ConvertRewardType((int)data.RewardType);
        if (data.PgcDatas == null && currencyType != CurrencyType.None)
        {
            PgcUtils.LoadCurrencyIconAsync(currencyType,gameObject, (iconSprite) =>
            {
                if (this != null && iconImg!= null && iconSprite != null)
                {
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "Currency", iconSprite);
                }
            });
        }
        else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.Avatar)
        {//皮肤
            if (!string.IsNullOrEmpty(data.BundleId))
            {
                PgcUtils.LoadBundleIconAsync(data.BundleId,gameObject, (iconSprite) =>
                {
                    if (this != null && iconImg != null && iconSprite != null)
                    {
                        iconImg.sprite = iconSprite;
                    }
                    else
                    {
                        LogIconMissing(data, "Avatar(Bundle)", iconSprite);
                    }
                });
            }
            else
            {
                PgcUtils.LoadAvatarIconAsync(data.Id,gameObject, (iconSprite) =>
                {
                    if (this != null && iconImg != null && iconSprite != null)
                    {
                        iconImg.sprite = iconSprite;
                    }
                    else
                    {
                        LogIconMissing(data, "Avatar", iconSprite);
                    }
                });
            }


        }  else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.PGCPetAvatar)
        {//Pet皮肤

            PgcUtils.LoadPetAvatarIconAsync(data.Id,gameObject, (iconSprite) =>
            {
                if (this != null && iconImg != null && iconSprite != null)
                {
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "PetAvatar", iconSprite);
                }
            });
        }
        else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.Emote)
        { //表情
            PgcUtils.LoadEmoteIconAsync(data.Id,gameObject, (iconSprite) =>
            {
                if (this != null && iconImg != null && iconSprite != null)
                {
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "Emote", iconSprite);
                }
            });
        }
        else if (data.RewardType == RewardType.RewardAvatarFrame)
        { //头像框
            UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImg != null && iconSprite != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "AvatarFrame", iconSprite);
                }
            });
        }
        else if (data.RewardType == RewardType.RewardChatBubbles)
        { //聊天气泡
            UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImg != null && iconSprite != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "ChatBubbles", iconSprite);
                }
            });
        }
        else if (GashaponUtils.HasPGCData(data) && data.PgcDatas[0].ResourceType == ResourceType.Vehicle)
        {//载具

            var iconSprite = PgcUtils.GetIconSpriteByPgcId(data.PgcDatas[0].Id, gameObject);
            iconImg.gameObject.SetActive(true);
            iconImg.sprite = iconSprite;
            if (iconSprite == null)
            {
                LogIconMissing(data, "Vehicle", iconSprite);
            }
        }
        else if((int)data.RewardType == (int)BUDRewardType.RewardTypeTitle)//称号
        {
            UserUIWidgetManager.Inst.GetTitleImgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImg != null && iconSprite != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "Title", iconSprite);
                }
            });
        }
        else if((int)data.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame)//昵称框
        {
            UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(data.Id, gameObject, (iconSprite) =>
            {
                if (this != null && iconImg != null && iconSprite != null)
                {
                    iconImg.gameObject.SetActive(true);
                    iconImg.sprite = iconSprite;
                }
                else
                {
                    LogIconMissing(data, "NicknameFrame", iconSprite);
                }
            });
        }
        else
        {
            // 没有命中任何分支：RewardType/ResourceType 未被处理，图标必然为空
            var resType = GashaponUtils.HasPGCData(data) ? data.PgcDatas[0].ResourceType.ToString() : "None";
            Debug.LogWarning($"[GashaponExchangeItem] 兑换项无匹配图标分支，Id={data.Id} RewardType={data.RewardType} ResourceType={resType} BundleId={data.BundleId}");
        }
    }

    // 图标加载失败时打印诊断信息，便于定位空白兑换项
    private void LogIconMissing(GashaponExchangeData data, string branch, Sprite iconSprite)
    {
        if (this == null || iconImg == null)
        {
            return;
        }
        string reason = iconSprite == null ? "iconSprite 为 null" : "iconImg 已销毁";
        Debug.LogWarning($"[GashaponExchangeItem] 图标加载失败({branch})：{reason}，Id={data.Id} RewardType={data.RewardType} BundleId={data.BundleId}");
    }

    public void SetSelectStatus(bool isSelect)
    {
        bgImage.color = isSelect ? selectColor : normalColor;
    }
    
    public GashaponExchangeData GetBindData()
    {
        return _data;
    }

    public void OnClickItem()
    {
        clickAction?.Invoke(this,_data);
        SetSelectStatus(true);
    }

    public void SetOwnedUI(bool value)
    {
        priceObj.SetActive(!value);
        ownedObj.SetActive(value);
    }
}
