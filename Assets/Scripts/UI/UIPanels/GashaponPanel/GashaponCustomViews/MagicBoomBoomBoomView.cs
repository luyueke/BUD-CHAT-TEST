using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using GameData.Gashapon;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class MagicBoomBoomBoomView : NewDefaultGashaponView {
    [SerializeField]
    public Text needNumText;
    [SerializeField]
    public Text progressValueText;
    [SerializeField]
    public RectTransform progressValueTrans;
    [SerializeField]
    public CButton bubBtn;
    [SerializeField]
    public Text tipsDoneTxt;
    [SerializeField]
    public Text tipsTxt;
    [SerializeField]
    public GameObject bottom;


    
    [SerializeField] public GameObject tipObj;
    public List<MagicBoomBoomBoomRewardItem> items;
    public List<string> pgcids = new List<string>() {
        "11400059" , //心纸水手套装
        "40300491" , //绘花传意
        "10900043" , //黯影套装
        "10500077" , //星芒流萤特效
        "40300492" , //画笔突袭
        "11000020" , //束带魔杖
        "11400061" , //糖果邦尼套装
    };
    public override void OnCreate(string id)
    {
        base.OnCreate(id); 
    }
    private List<Tuple<int, string>> luckInfos = new List<Tuple<int, string>>() {
        new Tuple<int, string>(20, "双人动作"),
        new Tuple<int, string>(50, "金奖特效"),
        new Tuple<int, string>(80, "紫奖套装"),
        new Tuple<int, string>(140, "金奖套装"),
    };



    protected override void InitUI() {
        base.InitUI();
    }

    protected override void InitBG() {
        for (int i = 0; i < pgcids.Count; i++)
        {
            items[i].InitData(pgcids[i], OnPriviewBtnClick);
        }
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        base.OnGashaponInfoUpdate(infoRsp);

        int curNum = infoRsp.luckyProgressInfo?.start ?? 0;
        curNum %= 140;

        int lastNum = 0;
        int nextNum = 10;

        var nextRewardName = "大奖";

        for (int i = 0; i < luckInfos.Count; i++) {
            var luckInfo = luckInfos.ElementAt(i);
            if (luckInfo.Item1 > curNum) {
                nextNum = luckInfo.Item1;
                nextRewardName = luckInfo.Item2;
                break;
            }
            lastNum = luckInfo.Item1;
        }
        singleText.SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        var fixedLuck = curNum - lastNum;
        needNumText.SetText((infoRsp.luckyProgressInfo.end - infoRsp.luckyProgressInfo.start).ToString());
        needNumText.SetPreferredSize();
        progressValueText.SetLocalText("抽取进度: {0}/{1}", infoRsp.luckyProgressInfo.start, infoRsp.luckyProgressInfo.end);
        progressValueTrans.sizeDelta = new Vector2((infoRsp.luckyProgressInfo.start * 1.0f / (infoRsp.luckyProgressInfo.end)) * 796, 30);
        
        //更新按钮显示、集齐完商品则隐藏
        if(infoRsp.luckyProgressInfo.start == infoRsp.luckyProgressInfo.end)
        {
            twistBtn.gameObject.SetActive(false);
            twist10Btn.gameObject.SetActive(false);
            tipsTxt.gameObject.SetActive(false);
            tipsDoneTxt.gameObject.SetActive(true);
            bottom.SetActive(false);
        }
        else
        {
            twistBtn.gameObject.SetActive(true);
            twist10Btn.gameObject.SetActive(true);
            tipsTxt.gameObject.SetActive(true);
            tipsDoneTxt.gameObject.SetActive(false);
            bottom.SetActive(true);
        }

        //更新按钮价格，设置折扣价格和原价
        if (infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice)
        {
            singleDiscountTag.SetActive(true);
            srcSingleText.gameObject.SetActive(true);
            srcSingleText.SetText(infoRsp.singleDrawPrice.ToString());
        }
        else
        {
            singleDiscountTag.SetActive(false);
            srcSingleText.gameObject.SetActive(false);
        }

        tenText.SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        if (infoRsp.tenDrawDiscountedPrice != infoRsp.tenDrawPrice)
        {
            tenDiscountTag.SetActive(true);
            srcTenText.gameObject.SetActive(true);
            srcTenText.SetText(infoRsp.tenDrawPrice.ToString());
        }
        else
        {
            tenDiscountTag.SetActive(false);
            srcTenText.gameObject.SetActive(false);
        }

        if (infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice)
        {
            float DiscountRate =  (float)infoRsp.singleDrawDiscountedPrice/ (float)infoRsp.singleDrawPrice;
            if(DiscountRate > 0.6f)
            {
                GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时75折");
                GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时75折");
            }else if (DiscountRate > 0.45f)
            {
                GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时5折");
                GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时5折");
            }
            else
            {
                GameObjectEx.FindComponentByName<Text>(twistBtn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时4折");
                GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("限时4折");
            }
        }
        for (int i = 0; i < pgcids.Count; i++)
        {
            items[i].Refresh();
        }
    }
}
