using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class PlantRankRewardPanel : BasePanel<PlantRankRewardPanel>
{
    public Button Close;
    public override void OnCreate()
    {
        base.OnCreate();
        Close.onClick.AddListener(CloseSelf);
    }







}