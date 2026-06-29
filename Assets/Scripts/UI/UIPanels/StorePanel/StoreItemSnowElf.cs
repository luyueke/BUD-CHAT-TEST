using System.Collections;
using System.Collections.Generic;
using UI.UIPanels.GashaponPanel;
using UnityEngine;

public class StoreItemSnowElf : StoreItemView
{
    protected override void FillUI(StoreItemViewBaseData data) {
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
                TimeTipText.SetLocalText("下架倒计时: {0}小时",hour);
            }
            else
            {
                if (hour == 0)
                {
                    TimeTipText.SetLocalText("下架倒计时: {0}天",day);
                }
                else
                {
                    TimeTipText.SetLocalText("下架倒计时: {0}天{1}小时",day,hour);
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
        GashaponDataManager.Inst.RequestGashaponInfo(GashaId, rsp => {
            priceWidget?.SetPrice(gashaInfo.CurrencyType, rsp.singleDrawPrice, "/抽");
        }, (_)=>{

        });
    }

    public override void OnClickAction()
    {
        if (_data == null)
        {
            return;
        }

        UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.ElfFrost.ToString());



    }
}
