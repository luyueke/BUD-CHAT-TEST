using System.Collections.Generic;
using GameData.Base;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;


public class RemovedItemsPanel : BasePanel<RemovedItemsPanel>
{
    public CButton closeBtn;
    public CButton okBtn;
    public RemovedPropItem removedPropItem;
    public Transform taskContent;
    private List<RemovedPropItem> remveRemovedPropItems = new List<RemovedPropItem>();
    private List<UgcBaseInfo> _ugcBaseInfos;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(() => { CloseSelf(); });
        okBtn.onClick.AddListener(() => CloseSelf());
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _ugcBaseInfos = (List<UgcBaseInfo>)args[0];
        SetData(_ugcBaseInfos);
    }

    private void SetData(List<UgcBaseInfo> list)
    {
        remveRemovedPropItems.Clear();
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var baseinfo in list)
        {
            var taskItem = Instantiate(removedPropItem, taskContent);
            taskItem.OnInitCreate(baseinfo);
            remveRemovedPropItems.Add(taskItem);
        }
    }
}