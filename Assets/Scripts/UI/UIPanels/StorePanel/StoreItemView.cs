using System;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public enum StoreItemStyle
{
    GashaponRecommend,
    GashaponNormal,
    Emote,
    Empty,
    Custom,
}

public class StorePriceInfo
{
    public int price;
    public CurrencyType balanceType;
    public string priceSuffix = "";
}

public class StoreGashaponInfo
{
    public GashaponType gashaponType;
    public string specialCoverName;
    public string specialName;
    public string specialLogo;
}

public class StoreItemViewBaseData
{
    public StoreItemStyle viewStyle;
    public string customViewPath;
    public StoreGashaponInfo gashaponInfo;
    public StorePriceInfo priceInfo;
    public long startTime = 0;
    public long endTime = 0;
    public int specialGroupId = -1;
}

public interface StoreItemViewProtocol
{
    public void SetData(StoreItemViewBaseData data, Action<StoreItemViewBaseData> ClickAction = null);
}

public class StoreItemView: MonoBehaviour, StoreItemViewProtocol
{
    [SerializeField] protected Image Cover;
    [SerializeField] protected Image Logo;
    [SerializeField] protected Text Name;
    [SerializeField] protected PriceDisplayWidget priceWidget;
    [SerializeField] protected CButton ActionBtn;

    [SerializeField] protected CButton MoreActionBtn;
    [SerializeField] protected Text TimeTipText;
    [SerializeField] protected GameObject TimeTipNode;

    protected StoreItemViewBaseData _data;
    protected Action<StoreItemViewBaseData> _clickAction;

    public void SetData(StoreItemViewBaseData data, Action<StoreItemViewBaseData> ClickAction = null)
    {
        _data = data;
        _clickAction = ClickAction;

        ActionBtn?.onClick.RemoveAllListeners();
        ActionBtn?.onClick.AddListener(OnClickAction);
        MoreActionBtn?.onClick.RemoveAllListeners();
        MoreActionBtn?.onClick.AddListener(OnClickAction);
        FillUI(data);
    }

    protected virtual void FillUI(StoreItemViewBaseData data)
    {
        if (data == null)
        {
            return;
        }

        if (data.viewStyle == StoreItemStyle.Emote || data.viewStyle == StoreItemStyle.Empty)
        {
            return;
        }

        var gashaData = data.gashaponInfo;
        if (gashaData == null)
        {
            return;
        }



        var coverName = ((int)data.gashaponInfo.gashaponType).ToString();
        if (!string.IsNullOrEmpty(data.gashaponInfo?.specialCoverName))
        {
            coverName = data.gashaponInfo?.specialCoverName;
        }
        var path = "Assets/Loadable/UI/UIPanel/StorePanelGashaponCover/GashaponCover.spriteatlas";
        Cover.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(path, coverName, gameObject);
        if (TimeTipNode!= null && TimeTipText != null && data.endTime > 0)
        {
            long remaining = data.endTime - DataUtil.GetUtcTimeStamp();
            remaining = Mathf.Max(1, (int)remaining);
            long day = remaining / 24 / 3600;
            long hour = (remaining / 3600 - 24 * day);
            if (day == 0 && hour == 0)
            {
                hour = 1;
            }
            var timeTip = TimeTipText.transform.parent;
            timeTip.gameObject.SetActive(true);

            if (day == 0)
            {
                TimeTipText.SetLocalText("距离售卖结束: {0}小时",hour);
            }
            else
            {
                if (hour == 0)
                {
                    TimeTipText.SetLocalText("距离售卖结束: {0}天",day);
                }
                else
                {
                    TimeTipText.SetLocalText("距离售卖结束: {0}天{1}小时",day,hour);
                }
            }
        }

        if (Logo != null) {
            string spriteName = "gashapon_logo_hallo";
            if (data.gashaponInfo != null && !string.IsNullOrEmpty(data.gashaponInfo.specialLogo)) {
                spriteName = data.gashaponInfo.specialLogo;
            }

            Logo.sprite =  XAssetLoaderMgr.Inst.LoadSpriteInAltas(path, spriteName, gameObject);
            Logo.transform.localEulerAngles = Vector3.zero;
            Logo.SetNativeSize();
        }

        var GashaId = GashaponDataManager.Inst.GetGashaponViewCfg(data.gashaponInfo.gashaponType).GashaId;
        var gashaInfo = GashaponDataManager.Inst.gashaponData(GashaId);
        if (gashaInfo == null)
        {
            return;
        }

        var gashaName = gashaInfo.Name;
        if (!string.IsNullOrEmpty(data.gashaponInfo?.specialName))
        {
            gashaName = data.gashaponInfo?.specialName;
        }
        Name.SetText(gashaName);

        priceWidget?.SetPrice(gashaInfo.CurrencyType, gashaInfo.SinglePrice, "/抽");
    }

    public virtual void OnClickAction()
    {
        if (_data == null)
        {
            return;
        }
        _clickAction?.Invoke(_data);
    }

    public StoreItemViewBaseData GetBindData()
    {
        return _data;
    }
}
