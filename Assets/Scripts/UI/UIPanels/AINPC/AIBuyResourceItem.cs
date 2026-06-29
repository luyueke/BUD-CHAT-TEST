using System;
using UnityEngine;
using UnityEngine.UI;
public class AIBuyResourceItem: MonoBehaviour
{
    [SerializeField] internal Text ocNumTitle;
    [SerializeField] internal Text ocNum;
    [SerializeField] internal Text gemNum;
    [SerializeField] internal GameObject discountRoot;
    [SerializeField] internal Text discountNum;
    [SerializeField] internal Button clickButton;
    [SerializeField] internal Image iconImage;
    [SerializeField] private AIResourcePayData payData;
    public Action<AIResourcePayData> OnBuyOcAction;

    private void Awake()
    {
        clickButton.onClick.AddListener(OnBuyOcClick);
    }

    public void SetData(AIResourcePayData data, AIResType aiResType)
    {
        payData = data;
        string strFormat = aiResType == AIResType.AIChat ? "{0}句对话" : "{0}局游玩卡";
        ocNumTitle.SetLocalText(strFormat,data.amount);
        ocNum.text = $"x {data.amount}";
        gemNum.text = $"{data.gem}";

        if (data.discount == 0)
        {
            discountRoot.gameObject.SetActive(false);
        }
        else
        {
            discountRoot.gameObject.SetActive(true);
            discountNum.SetLocalText("{0}折",100 - data.discount);
        }
        var spriteName =aiResType ==  AIResType.AIChat ? "chat" : "game";
        var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, spriteName, gameObject);
        iconImage.sprite = sprite;
    }

    public void DisableButton()
    {
        clickButton.enabled = false;
    }

    private void OnBuyOcClick()
    {
        OnBuyOcAction?.Invoke(payData);
    }
}