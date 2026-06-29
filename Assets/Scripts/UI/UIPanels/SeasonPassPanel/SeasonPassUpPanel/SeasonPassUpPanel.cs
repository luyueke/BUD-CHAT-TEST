using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassUpPanel : BasePanel<SeasonPassUpPanel>
{
    public Button CloseBtn;

    public Button GoBtn;

    public Button BuyBtn;

    public Text ContentTxt;

    public Text DesTxt;

    int offset;
    public override void OnCreate()
    {
        base.OnCreate();

        CloseBtn.onClick.AddListener(CloseSelf);

        GoBtn.onClick.AddListener(OnGoBtn);

        BuyBtn.onClick.AddListener(OnBuyBtn);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);


        var data = SeasonPassDataManager.Inst.GetCurSeasonData();
        offset = 25 - data.progressInfo.currentTier;
        ContentTxt.text = $"当前通行证等级<color=#FFD441>{data.progressInfo.currentTier}级</color>，再升<color=#FFD441>{offset}级</color>可达25级";
        
        DesTxt.text = $"立即购买<color=#9640E9>{offset}级</color>通行证等级，预计需要<color=#9640E9>{offset * 50}钻石</color>";
    }

    void OnGoBtn() 
    { 
        CloseSelf();
        UIManager.Inst.ClosePanel(PanelId.SeasonPassExchangePanel);
    }

    void OnBuyBtn()
    {
        CloseSelf();
        var panel = UIManager.Inst.OpenPanel<SeasonPassBuyLevelPanel>(PanelId.SeasonPassBuyLevelPanel);
        panel.UpdateLevel(offset);
    }
}