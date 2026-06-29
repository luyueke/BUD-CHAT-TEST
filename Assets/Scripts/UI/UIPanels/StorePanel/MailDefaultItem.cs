using System;
using ChocDino.UIFX;
using Es;
using GameUI;
using Message;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class MailDefaultItem : MonoBehaviour {
    private Text nameText;
    private GameObject newObj;
    private GameObject reddotObj;
    private GameObject backObj;
    private Image bannerImage;
    private OutlineFilter outlineFilter;

    private Action<GashaponType> onGashaponCallBack;
    private GashaponType gashaponType;
    private string gashaponId;
    private void Awake() {
        nameText = GameObjectEx.FindComponentByName<Text>(transform, "Name");
        newObj = GameObjectEx.FindChildByName(transform, "New").gameObject;
        reddotObj = GameObjectEx.FindChildByName(transform, "reddot").gameObject;
        backObj = GameObjectEx.FindChildByName(transform, "Back").gameObject;
        bannerImage = GetComponent<Image>();
        outlineFilter = GetComponent<OutlineFilter>();
        GetComponent<CButton>().onClick.AddListener(OnSelected);
        MessageHelper.AddListener(MessageName.ReddotNotice, CheckReddot);
    }

    public void SetData(GashaponViewConfig viewConfig, Action<GashaponType> selectCallBack) {
        gashaponType = (GashaponType)viewConfig.ViewId;
        gashaponId = viewConfig.GashaId;
        onGashaponCallBack = selectCallBack;
        var gashaponData = GashaponDataManager.Inst.gashaponData(viewConfig.GashaId);
        nameText.SetText(gashaponData.Name);
        var path = "Assets/Loadable/UI/UIPanel/StorePanelGashaponCover/GashaponCover.spriteatlas";
        bannerImage.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(path, viewConfig.ViewId.ToString(), gameObject);
        newObj.SetActive(BusinessLiveManager.Inst.IsGashaponNew(viewConfig.GashaId));
        backObj.SetActive(BusinessLiveManager.Inst.IsGashaponBack(viewConfig.GashaId));
    }
    public void CheckReddot()
    {
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, (rsp)=> {
            reddotObj.SetActive(false);
            if (rsp.taskList==null || rsp.taskList.Count == 0)
            {
                return;
            }

            foreach(var task in rsp.taskList)
            {
                if(task.rewardStatus == 2)
                {
                    reddotObj.SetActive(true);
                    return;
                }
            }
        
        });

    }
    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.ReddotNotice, CheckReddot);
    }
    public void SetSelected(bool selected) {
        outlineFilter.Color = selected ? new Color32(255, 211, 54, 255) : Color.black;
        outlineFilter.Size = selected ? 4 : 2.5f;
    }

    private void OnSelected() {
        onGashaponCallBack?.Invoke(gashaponType);
    }
}
