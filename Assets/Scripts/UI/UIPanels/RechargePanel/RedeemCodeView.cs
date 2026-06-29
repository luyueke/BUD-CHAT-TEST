using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class RedeemCodeView : MonoBehaviour
{
    [SerializeField] private CButton rootBtn;
    [SerializeField] private Transform BG;

    private void Start()
    {
        string atlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
        {
            "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
        });
        item.gameObject.SetActive(true);


        rootBtn.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanel<RedeemCodePanel>(PanelId.RedeemCodePanel);
        });
    }

}
