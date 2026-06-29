using System;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
public class ActionGetMoreItem : MonoBehaviour
{
    public Button getMoreBtn;
    public Action onGetMoreAction; //获取更多事件


    void Awake()
    {
        getMoreBtn.onClick.AddListener(OnGetMoreBtnClick);
    }

    public void Init()
    {
        // iconImage.sprite = data.skinPack.cover;
    }

    void OnGetMoreBtnClick()
    {
        var fittingRoomPanel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        fittingRoomPanel.JumpTo(MainTabs.Tab.Ugc, GameData.PgcData.UniqueType.Get(GameData.PgcData.ResourceType.UgcPose, (int)GameData.PgcData.UgcAnimSubType.PeopleAll));
        onGetMoreAction?.Invoke();
    }


}
