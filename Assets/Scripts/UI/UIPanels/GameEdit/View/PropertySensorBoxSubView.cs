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
/// Description:属性面板中：感应盒操作面板
/// </summary>
public class PropertySensorBoxSubView : MonoBehaviour
{
    private const string TAG = "PropertySensorBoxSubView";
    private SceneEntity curEntity;

    [SerializeField]
    private Toggle activeToggle;
    [FormerlySerializedAs("sensorBoxScrollView")] [SerializeField]
    private GameObject sensorBoxScrollView;
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
        sensorBoxScrollView.SetActive(false);
    }
    
    
    private void OnToggleActive(bool isOn)
    {
        if (isOn)
        {
            if (itemScripts.Count <= 0)
            {
                TipPanel.ShowToast("请先添加一个感应盒");
                activeToggle.isOn = false;
                return;
            }
            sensorBoxScrollView.SetActive(true);
            //如果打开感应盒时，仅有一个感应盒，则默认选中
            if (itemScripts.Count == 1)
            {
                var itemNode = itemScripts.FirstOrDefault().Value;
                itemNode.SetIsSelect(true);
            }
        }
        else
        {
            ClearSelect();
            sensorBoxScrollView.SetActive(false);
        }

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        // 刷新一下布局
        var contentView = GetComponentInParent<ContentSizeFitter>();
        if (contentView != null)
        {
            var viewLayout = contentView.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewLayout);
        }
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

    public void SelectItem(bool isSelected)
    {
        activeToggle.isOn = isSelected;
        sensorBoxScrollView.SetActive(isSelected);
    }
    
    private void RefreshItemState()
    {
        foreach (var item in itemScripts.Values)
        {
            Destroy(item.gameObject);
        }
        itemScripts.Clear();


        var tempDic = GlobalNodeManager.Inst.Get<SensorBoxManager>().GetIndexList();
        foreach (var sid in tempDic)
        {
            if (!itemScripts.ContainsKey(sid))
            {
                CreateItem(sid, sid.ToString());
            }
        }
        tempDic.Clear();

        if (!curEntity.HasComp<SensorCtrComponent>())
        {
            SelectItem(false);
            return;
        }
        List<uint> uids = null;
        var ctrComp = curEntity.GetComp<SensorCtrComponent>();
        if (ctrComp.SrcTypeDicts.ContainsKey(CtrlType))
        {
            uids = ctrComp.SrcTypeDicts[CtrlType];
        }
        
        if (uids == null || uids.Count == 0)
        {
            SelectItem(false);
            return;
        }

        LoggerUtils.Log($"{TAG} 当前感应盒uids:"+uids);

        bool isControlBySensorBox = false;
        foreach (var index in itemScripts.Keys)
        {
            if (index != 0)
            {
                uint uid =  GlobalNodeManager.Inst.Get<SensorBoxManager>().GetUidByIndex(index);
                if (uids.Contains(uid))
                {
                    itemScripts[index].SetIsSelectWithoutCallback(true);
                    isControlBySensorBox = true;
                }
            }
        }

        SelectItem(isControlBySensorBox);
    }

    private void ClearSelect()
    {
        var SensorBoxManager = GlobalNodeManager.Inst.Get<SensorBoxManager>();
        foreach (var k in itemScripts.Keys)
        {
            if (k != 0)
            {
                uint sUid = SensorBoxManager.GetUidByIndex(k);
                uint ctrId = curEntity.GetComp<GameObjectComponent>().Uid;
                SensorCtrController.Inst.UnbindEntity(curEntity,sUid,CtrlType);
                SensorBoxManager.RemoveCtrIdFromSensorComp(ctrId,sUid, CtrlType);
            }
            itemScripts[k].SetIsSelectWithoutCallback(false);
        }
    }

    public void OnItemClick(int sid,bool isOn)
    {
       var SensorBoxManager = GlobalNodeManager.Inst.Get<SensorBoxManager>();
       
        if (itemScripts.ContainsKey(sid))
        {
            uint sUid = SensorBoxManager.GetUidByIndex(sid);
            uint ctrId = curEntity.GetComp<GameObjectComponent>().Uid;
            if (isOn == false)
            {
                //解绑
                SensorCtrController.Inst.UnbindEntity(curEntity,sUid,CtrlType);
                SensorBoxManager.RemoveCtrIdFromSensorComp(ctrId,sUid, CtrlType);
            }
            else
            {
                //绑定
                SensorCtrController.Inst.BindEntity(curEntity,sUid,CtrlType);
                SensorBoxManager.AddCtrIdToSensorComp(ctrId,sUid,CtrlType);
            }
        }
        else
        {
            LoggerUtils.Log("[Click Not Exist SensorBox] sid=>" + sid);
        }
    }

}
