using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Config;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsController;
using Game.Props.PropsManagers;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Author:JayWill
/// Description:属性面板中：开关操作面板
/// </summary>
public class PropertySwitchSubView : MonoBehaviour
{
    private const string TAG = "PropertySwitchSubView";
    private SceneEntity curEntity;

    [SerializeField]
    private Toggle activeToggle;
    [FormerlySerializedAs("switchScrollView")] [SerializeField]
    private GameObject switchScrollView;
    [SerializeField]
    private GameObject ItemPrefab;
    [SerializeField]
    private Transform ItemsContent;
    private Dictionary<int, TabItem> itemScripts = new Dictionary<int, TabItem>();

    public GameGlobalEnum.PropControlType CtrlType;


    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        activeToggle.onValueChanged.AddListener(OnToggleActive);
        switchScrollView.SetActive(false);
    }
    
    
    private void OnToggleActive(bool isOn)
    {
        if (isOn)
        {
            if (itemScripts.Count <= 0)
            {
                TipPanel.ShowToast("请先添加一个开关");
                activeToggle.isOn = false;
                return;
            }
            switchScrollView.SetActive(true);
            
            //如果打开开关时，仅有一个开关，则默认选中
            if (itemScripts.Count == 1)
            {
                var itemNode = itemScripts.FirstOrDefault().Value;
                itemNode.SetIsSelect(true);
            }
        }
        else
        {
            ClearSelect();
            switchScrollView.SetActive(false);
        }

        RefreshLayout();
    }
    
    private void RefreshLayout()
    {
        // 刷新一下布局
        // var contentView = GetComponentInParent<ContentSizeFitter>();
        // if (contentView != null)
        // {
        //     var viewLayout = contentView.GetComponent<RectTransform>();
        //     LayoutRebuilder.ForceRebuildLayoutImmediate(viewLayout);
        // }
        var viewLayout = transform.parent.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewLayout);
    }

    
    private void CreateItem(int id, string text)
    {
        var itemNode = GameObject.Instantiate(ItemPrefab, ItemsContent);
        itemNode.SetActive(true);
        TabItem tabItem = itemNode.GetComponentInChildren<TabItem>(true);
        tabItem.Init();
        tabItem.SetShowName(text);
        tabItem.AddValueChangeCallListener((isOn) =>
        {
            OnItemClick(id,isOn);
        });
        itemScripts.Add(id, tabItem);
    }

    public void SetEntity(SceneEntity entity)
    {
        curEntity = entity;
        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshItemState();

        float alpha = 1;
        if (itemScripts.Count <= 0)
        {
            alpha = 0.5f;
        }
        activeToggle.targetGraphic.color = new Color(1, 1, 1, alpha);
    }

    public void SelectSwitches(bool isSelected)
    {
        activeToggle.isOn = isSelected;
        switchScrollView.SetActive(isSelected);
    }
    
    private void RefreshItemState()
    {
        foreach (var item in itemScripts.Values)
        {
            Destroy(item.gameObject);
        }
        itemScripts.Clear();


        var tempDic = GlobalNodeManager.Inst.Get<SwitchBtnManager>().GetIndexList();
        foreach (var sid in tempDic)
        {
            if (!itemScripts.ContainsKey(sid))
            {
                CreateItem(sid, sid.ToString());
            }
        }
        tempDic.Clear();

        if (!curEntity.HasComp<SwitchCtrComponent>())
        {
            SelectSwitches(false);
            return;
        }
        List<uint> uids = null;
        var switchCtrComp = curEntity.GetComp<SwitchCtrComponent>();
        if (switchCtrComp.SrcTypeDicts.ContainsKey(CtrlType))
        {
            uids = switchCtrComp.SrcTypeDicts[CtrlType];
        }
        
        if (uids == null || uids.Count == 0)
        {
            SelectSwitches(false);
            return;
        }

        LoggerUtils.Log($"{TAG} 当前开关uids:"+uids);

        bool isControlBySwitch = false;
        foreach (var index in itemScripts.Keys)
        {
            if (index != 0)
            {
                uint uid =  GlobalNodeManager.Inst.Get<SwitchBtnManager>().GetUidByIndex(index);
                if (uids.Contains(uid))
                {
                    itemScripts[index].SetIsSelectWithoutCallback(true);
                    isControlBySwitch = true;
                }
            }
        }

        SelectSwitches(isControlBySwitch);
    }

    private void ClearSelect()
    {
        var SwitchBtnManager = GlobalNodeManager.Inst.Get<SwitchBtnManager>();
        foreach (var k in itemScripts.Keys)
        {
            if (k != 0)
            {
                uint sUid = SwitchBtnManager.GetUidByIndex(k);
                uint ctrId = curEntity.GetComp<GameObjectComponent>().Uid;
                SwitchCtrController.Inst.UnbindEntity(curEntity,sUid,CtrlType);
                SwitchBtnManager.RemoveCtrIdFromSwitchComp(ctrId,sUid, CtrlType);
            }
            itemScripts[k].SetIsSelectWithoutCallback(false);
        }
    }

    public void OnItemClick(int sid,bool isOn)
    {
        var SwitchBtnManager = GlobalNodeManager.Inst.Get<SwitchBtnManager>();
       
        if (itemScripts.ContainsKey(sid))
        {
            uint sUid = SwitchBtnManager.GetUidByIndex(sid);
            uint ctrId = curEntity.GetComp<GameObjectComponent>().Uid;
            if (isOn == false)
            {
                //解绑
                SwitchCtrController.Inst.UnbindEntity(curEntity,sUid,CtrlType);
                SwitchBtnManager.RemoveCtrIdFromSwitchComp(ctrId,sUid, CtrlType);
            }
            else
            {
                //绑定
                SwitchCtrController.Inst.BindEntity(curEntity,sUid,CtrlType);
                SwitchBtnManager.AddCtrIdToSwitchComp(ctrId,sUid,CtrlType);
            }
        }
        else
        {
            LoggerUtils.Log("[Click NOT Exist Switch] sid=>" + sid);
        }
    }

}
