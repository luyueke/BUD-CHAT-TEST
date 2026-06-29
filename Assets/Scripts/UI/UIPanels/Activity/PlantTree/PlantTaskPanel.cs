using System.Collections;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PlantTaskPanel : BasePanel<PlantTaskPanel>
{
    public CButton CloseBtn;

    public Toggle Toggle;

    public Toggle Toggle2;

    public Toggle Toggle3;

    public GameObject Red1;

    public GameObject Red2;

    public GameObject Red3;

    public PlantTaskDailyGroup DailyGroup;

    public PlantTaskRechargeGroup RechargeGroup;

    public PlantTaskGiftGroup GiftGroup;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        Toggle.onValueChanged.AddListener(OnTog1);
        Toggle2.onValueChanged.AddListener(OnTog2);
        Toggle3.onValueChanged.AddListener(OnTog3);
        Red1.gameObject.SetActive(false);
        Red2.gameObject.SetActive(false);
        Red3.gameObject.SetActive(false);
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        Toggle.isOn = true;
    }

    void OnTog1(bool bo) {
        DailyGroup.gameObject.SetActive(bo);
    }
    void OnTog2(bool bo)
    {
        RechargeGroup.gameObject.SetActive(bo);
    }
    void OnTog3(bool bo)
    {
        GiftGroup.gameObject.SetActive(bo);
    }
}