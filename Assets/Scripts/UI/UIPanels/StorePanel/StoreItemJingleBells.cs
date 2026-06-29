using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemJingleBells : StoreItemView {

    [SerializeField]
    private GameObject discountMarkObj;

    [SerializeField]
    private  Text countdownText;

    [SerializeField] private GameObject enterObj;

    [SerializeField] private Text originPriceText;
    [SerializeField] private Text priceText;

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
        if (countdownText  != null && data.endTime > 0) {
            countdownText.SetLocalText("圣诞限定：12月20日-1月12日");
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
            if (rsp == null) {
                enterObj.SetActive(true);
                discountMarkObj.SetActive(true);
            } else {
                enterObj.SetActive(true);
                if (rsp.singleDrawPrice == rsp.singleDrawDiscountedPrice) {
                    originPriceText.gameObject.SetActive(false);
                    priceText.SetText(rsp.singleDrawPrice.ToString());
                    priceText.SetPreferredSize();
                    discountMarkObj.SetActive(false);
                } else {
                    originPriceText.gameObject.SetActive(true);
                    originPriceText.SetText(rsp.singleDrawPrice.ToString());
                    originPriceText.SetPreferredSize();
                    priceText.SetText(rsp.singleDrawDiscountedPrice.ToString());
                    priceText.SetPreferredSize();
                    discountMarkObj.SetActive(true);
                }
            }
        }, (_)=>{
            enterObj.SetActive(true);
            discountMarkObj.SetActive(true);
        });

    }
}
