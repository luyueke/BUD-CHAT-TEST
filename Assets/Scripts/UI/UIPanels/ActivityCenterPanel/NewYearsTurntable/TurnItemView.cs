using System;
using System.Collections.Generic;
using ChocDino.UIFX;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class TurnItemView : MonoBehaviour {
    [SerializeField] private Text priceTxt;

    [SerializeField] private Text turnText;

    [SerializeField] private Image bgImg;

    [SerializeField] private GameObject lockObj;
    [SerializeField] private Text tipText;

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private VerticalLayoutGroup layoutGroup;

    [SerializeField] private Text multiText;

    private Color normalColor = Color.black;
    private Color selectedColor = new Color32(201, 53,53,255);

    private int eventId;
    private Action<int> onSelectedCallBack = null;


    private void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnClicked);
    }


    public void SetTurn(int id, TurnInfo turnInfo, Action<int> callBack) {
        eventId = id;
        onSelectedCallBack = callBack;
        turnText.SetLocalText(turnInfo.name);
        priceTxt.SetLocalText("¥{0}",IAPDataManager.Inst.GetPriceInfo(turnInfo.productId).price);
        multiText.SetLocalText(turnInfo.rewardInfos[^1].multiple);

    }


    public void SetStatus(ClaimStatus claimStatus) {
        lockObj.SetActive(false);

        if (claimStatus == ClaimStatus.Lock || claimStatus == ClaimStatus.ErrStatus) {
            lockObj.SetActive(true);
            tipText.SetLocalText("完成上一轮后开始");
        } else if (claimStatus == ClaimStatus.Unlocked) {
            tipText.SetLocalText("当前待开启");
        } else if (claimStatus == ClaimStatus.Claimed) {
            tipText.SetLocalText("已领取");
        }
        tipText.SetPreferredSize();
    }

    public void SetSelected(bool selected) {
        if (selected) {
            layoutGroup.padding = new RectOffset(0, 0, 0, 20);
            layoutGroup.spacing = 10;
            bgImg.sprite = selectedSprite;
            bgImg.SetNativeSize();
            turnText.GetComponent<OutlineFilter>().Color = selectedColor;
            turnText.GetComponent<DropShadowFilter>().Color = selectedColor;
            priceTxt.color = selectedColor;
            priceTxt.GetComponent<OutlineFilter>().enabled = false;
            priceTxt.GetComponent<DropShadowFilter>().enabled = false;
        } else {
            layoutGroup.padding = new RectOffset(0, 0, 0,30);
            layoutGroup.spacing = 28;
            bgImg.sprite = normalSprite;
            bgImg.SetNativeSize();
            turnText.GetComponent<OutlineFilter>().Color = normalColor;
            turnText.GetComponent<DropShadowFilter>().Color = normalColor;
            priceTxt.color = Color.white;
            priceTxt.GetComponent<OutlineFilter>().enabled = true;
            priceTxt.GetComponent<DropShadowFilter>().enabled = true;
        }
    }

    private void OnClicked() {
        onSelectedCallBack?.Invoke(eventId);
    }
}
