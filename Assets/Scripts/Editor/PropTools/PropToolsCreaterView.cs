/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-15 17:58:59
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-24 11:37:05
 * @ Description:
 */

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using GameData.Config;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using common.editor;

namespace prop.editor
{
    [Serializable]
    public class EnumListStruct
    {
        public int Id;
        public string Name;
    }

    public class PropToolsCreaterView : BaseToolView
    {
        private string msg;

#region ComponentId
        private bool showComponentMsg = false;
        [BoxGroup("配置")]
        [FoldoutGroup("配置/ComponentId")]
        [VerticalGroup("配置/ComponentId/VG")]
        public int ComponentId;
        [FoldoutGroup("配置/ComponentId")]
        [VerticalGroup("配置/ComponentId/VG")]
        public string ComponentName;
        [FoldoutGroup("配置/ComponentId")]
        [VerticalGroup("配置/ComponentId/VG")]
        [InfoBox("@this.msg", InfoMessageType.Error, "@this.showComponentMsg == true")]
        [Button("Add", ButtonSizes.Medium)]
        void OnAddComponent()
        {
            // Validate
            if (string.IsNullOrEmpty(ComponentName))
            {
                this.showComponentMsg = true;
                msg = "Name不能为空";
                return;
            }

            for (int i = 0; i < ComponentIdList.Count; i++)
            {
                if (ComponentIdList[i].Id == ComponentId)
                {
                    this.showComponentMsg = true;
                    msg = "ID重复";
                    return;
                }
                if (ComponentIdList[i].Name == ComponentName)
                {
                    this.showComponentMsg = true;
                    msg = "Name重复";
                    return;
                }
            }

            ComponentIdList.Add(new EnumListStruct(){Id=ComponentId, Name=ComponentName});
            ComponentIdList.Sort((a, b)=>b.Id - a.Id);
        }

        [FoldoutGroup("配置/ComponentId")]
        [LabelText("ComponentId列表预览")]
        [ListDrawerSettings(HideAddButton = true, NumberOfItemsPerPage = 5)]
        public List<EnumListStruct> ComponentIdList = new List<EnumListStruct>();
#endregion

#region NodeModeType
        private bool showNodeModeTypeMsg = false;
        [FoldoutGroup("配置/NodeModeType")]
        [VerticalGroup("配置/NodeModeType/VG")]
        public int NodeModeTypeId;
        [FoldoutGroup("配置/NodeModeType")]
        [VerticalGroup("配置/NodeModeType/VG")]
        public string NodeModeTypeName;
        [FoldoutGroup("配置/NodeModeType")]
        [VerticalGroup("配置/NodeModeType/VG")]
        [InfoBox("@this.msg", InfoMessageType.Error, "@this.showNodeModeTypeMsg == true")]
        [Button("Add", ButtonSizes.Medium)]
        void OnAddNodeModeType()
        {
            // Validate
            if (string.IsNullOrEmpty(NodeModeTypeName))
            {
                this.showNodeModeTypeMsg = true;
                msg = "Name不能为空";
                return;
            }

            for (int i = 0; i < NodeModeTypeNameList.Count; i++)
            {
                if (NodeModeTypeNameList[i].Id == NodeModeTypeId)
                {
                    this.showNodeModeTypeMsg = true;
                    msg = "ID重复";
                    return;
                }
                if (NodeModeTypeNameList[i].Name == NodeModeTypeName)
                {
                    this.showNodeModeTypeMsg = true;
                    msg = "Name重复";
                    return;
                }
            }

            NodeModeTypeNameList.Add(new EnumListStruct(){Id=NodeModeTypeId, Name=NodeModeTypeName});
            NodeModeTypeNameList.Sort((a, b)=>b.Id - a.Id);
        }

        [FoldoutGroup("配置/NodeModeType")]
        [LabelText("NodeModeType列表预览")]
        [ListDrawerSettings(HideAddButton = true, NumberOfItemsPerPage = 5)]
        public List<EnumListStruct> NodeModeTypeNameList = new List<EnumListStruct>();
#endregion

        [BoxGroup("配置")]
        [Button("导出枚举配置", ButtonSizes.Medium), GUIColor(0, 1, 0)]
        void OnExport()
        {
            new EnumGenerated(Const.ComponentIdFile, "GameData.Config", "NodeComponentId", ComponentIdList).Generate();
            new EnumGenerated(Const.NodeModelTypeFile, "GameData.Config", "NodeModelType", NodeModeTypeNameList).Generate();

            AssetDatabase.Refresh();
        }

        [BoxGroup("创建道具")]
        [ValueDropdown("GetComponentList")]
        [LabelText("ComponentId")]
        public int CompoentIdDropdown;

        [BoxGroup("创建道具")]
        [ValueDropdown("GetNodeModelTypeList")]
        [OnValueChanged("OnNodeModelTypeChange")]
        [LabelText("NodeModelType")]
        public int NodeModelTypeDropdown;

        [BoxGroup("创建道具")]
        public string NewPropName;

        [BoxGroup("创建道具")]
        [Button("创建", ButtonSizes.Medium), GUIColor(0, 1, 0)]
        [PropertySpace(SpaceBefore = 10)]
        void OnCreate()
        {
            var context = new PropGeneratedContext()
            {
                NodeComponentIdName = ComponentIdList[CompoentIdDropdown].Name,
                NodeModelTypeName = NodeModeTypeNameList[NodeModelTypeDropdown].Name,
                Name = NewPropName,
            };

            new PropClassGenerated(context).Generate();
            AssetDatabase.Refresh();
        }

        [BoxGroup("创建道具")]
        [Button("重新生成注册文件", ButtonSizes.Medium), GUIColor(0, 1, 0)]
        void OnCreateRegister()
        {
            var context = new PropGeneratedContext();
            new ManagerRegisterFileGenerated(context).Generate();
			new ComponentRegisterFileGenerated(context).Generate();
            AssetDatabase.Refresh();
        }

        public PropToolsCreaterView(OdinMenuEditorWindow window) : base(window)
		{
		}

        [OnInspectorInit]
        void InitData()
        {
            ComponentIdList.Clear();
            NodeModeTypeNameList.Clear();
            // NodeComponentId
            var componentIdList = Enum.GetValues(typeof(NodeComponentId)).Cast<int>().ToList();
            componentIdList.Sort();
            componentIdList.Reverse();
            componentIdList.ForEach(x => ComponentIdList.Add(new EnumListStruct(){Id=x, Name=((NodeComponentId)x).ToString()}));
            // NodeModelType
            var nodeModelTypeList = Enum.GetValues(typeof(NodeModelType)).Cast<int>().ToList();
            nodeModelTypeList.Sort();
            nodeModelTypeList.Reverse();
            nodeModelTypeList.ForEach(x => NodeModeTypeNameList.Add(new EnumListStruct(){Id=x, Name=((NodeModelType)x).ToString()}));
        }

        IEnumerable GetComponentList()
        {
            return ComponentIdList.Select((x, index) => new ValueDropdownItem(x.Name, index));
        }

        IEnumerable GetNodeModelTypeList()
        {
            return NodeModeTypeNameList.Select((x, index) => new ValueDropdownItem(x.Name, index));
        }

        void OnNodeModelTypeChange()
        {
            NewPropName = NodeModeTypeNameList[NodeModelTypeDropdown].Name;
        }
    }
}