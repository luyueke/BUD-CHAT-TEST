using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorTitleRewardPanel : BasePanel<CreatorTitleRewardPanel>
{
    [SerializeField] private CButton EnterBtn;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private Text RewardTips;
    [SerializeField] private List<TitleRewardItem> ShowInfoItems;

    public override void OnCreate()
    {
        base.OnCreate();
        EnterBtn.onClick.AddListener(CloseSelf);
        ShowBtn.onClick.AddListener(OnShowBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if(args.Length <= 0)
        {
            return;
        }
        List<CreatorBadgeInfoData> dataList = (List<CreatorBadgeInfoData>)args[0];
        for(int i = 0; i < dataList.Count; i++)
        {
            ShowInfoItems[i].gameObject.SetActive(true);
            ShowInfoItems[i].SetData(dataList[i]);
        }
    }
    
    private void OnShowBtnClick()
    {
        Debug.LogError("OnShowBtnClick");
    }
}
