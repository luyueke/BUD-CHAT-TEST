using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class NightFairyTaleView : NewDefaultGashaponView {
    [SerializeField]
    public Text needNumText;
    [SerializeField]
    public Text progressValueText;
    [SerializeField]
    public RectTransform progressValueTrans;
    [SerializeField]
    public Text rewardText;

    private CButton seasonBtn;

    [SerializeField] public GameObject tipObj;

    private List<Tuple<int, string>> luckInfos = new List<Tuple<int, string>>() {
        new Tuple<int, string>(20, "双人动作"),
        new Tuple<int, string>(50, "金奖特效"),
        new Tuple<int, string>(80, "紫奖套装"),
        new Tuple<int, string>(140, "金奖套装"),
    };


    private void OnSeasonBtnClick() {
        UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, RechargeId.LimitedRechargeGiftPack , 3);
    }

    protected override void InitUI() {
        base.InitUI();
        seasonBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SeasonBtn");
        seasonBtn.onClick.AddListener(OnSeasonBtnClick);
    }

    protected override void InitBG() {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgRootNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomTextureBg("Assets/Loadable/UI/UIPanel/NightFairyTale/StarryNightFairyTale.png");
        item.gameObject.SetActive(true);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        base.OnGashaponInfoUpdate(infoRsp);



        int curNum = infoRsp.luckyProgressInfo?.start ?? 0;
        curNum %= 140;

        int lastNum = 0;
        int nextNum = 20;

        var nextRewardName = "双人动作";

        for (int i = 0; i < luckInfos.Count; i++) {
            var luckInfo = luckInfos.ElementAt(i);
            if (luckInfo.Item1 > curNum) {
                nextNum = luckInfo.Item1;
                nextRewardName = luckInfo.Item2;
                break;
            }
            lastNum = luckInfo.Item1;
        }



        var fixedLuck = curNum - lastNum;
        needNumText.SetText((nextNum - curNum).ToString());
        needNumText.SetPreferredSize();
        rewardText.SetLocalText($"次必得{nextRewardName}");
        rewardText.SetPreferredSize();
        progressValueText.SetLocalText("抽取进度: {0}/{1}", fixedLuck, (nextNum - lastNum));
        progressValueTrans.sizeDelta = new Vector2((fixedLuck*1.0f / (nextNum - lastNum)) * 796, 30);
        if (curNum == 0) {
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("首次五折");
        } else {
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "TagDiscount/Text (Legacy)").SetLocalText("九折优惠");
        }

    }
}
