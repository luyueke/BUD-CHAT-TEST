using System;
using System.Collections.Generic;

using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;


public class ValentinePackDetailPanel : BasePanel<ValentinePackDetailPanel>
{
    public Button closeBtn;

    public Button buyBtn;
    public Button buyMaskBtn;

    public Sprite bubbleSp;
    public Sprite backgroundSp;
    public List<CButton> items;

    private int currentNum = 1;

    private int gemPrice = 30;
    private string pgcId = "40300437";

    public Action buyAction;


    public override void OnCreate()
    {
        base.OnCreate();

        closeBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });


        buyBtn.onClick.AddListener(() =>
        {
            buyAction?.Invoke();
            CloseSelf();
        });

        for (int i = 0; i < items.Count; i++)
        {
            switch (i)
            {
                case 0:
                    items[i].onClick.RemoveAllListeners();
                    items[i].onClick.AddListener(() =>
                    {
                        UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.PurpleDreamCoin);
                    });
                    break;
                case 1:
                    items[i].onClick.RemoveAllListeners();
                    items[i].onClick.AddListener(() =>
                    {
                        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
                        panel.SetNewyearEventPreview(new List<string>(){pgcId},"流星群双人舞" , "","","情人节限定礼包", null);
                    });
                    break;
                case 2:
                    items[i].onClick.RemoveAllListeners();
                    items[i].onClick.AddListener(() =>
                    {
                        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                        // panel.UpdateUI(bubbleSp, "恋影成双聊天气泡", null, "通过购买情人节限定礼包获得", isAvatarFrame:true);
                    });
                    break;
                case 3:
                    items[i].onClick.RemoveAllListeners();
                    items[i].onClick.AddListener(() =>
                    {
                        var panel = UIManager.Inst.OpenPanel<ValentinePackTipPanel>(PanelId.ValentinePackTipPanel);

                    });
                    break;
            }
        }
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        SetupUI();
    }


    private void SetupUI()
    {
        currentNum = 1; // Reset to 1 when the panel is shown
    }
    
}