using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using common.editor;
using GameData.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace prop.editor
{
    public class PropToolsComponentCreaterView : BaseToolView
    {
        private string msg;
        
        private bool showComponentMsg = false;
        [FoldoutGroup("ComponentId")]
        [VerticalGroup("ComponentId/VG")]
        public int ComponentId;
        [FoldoutGroup("ComponentId")]
        [VerticalGroup("ComponentId/VG")]
        public string ComponentName;
        [FoldoutGroup("ComponentId")]
        [VerticalGroup("ComponentId/VG")]
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

        [FoldoutGroup("ComponentId")]
        [LabelText("ComponentId列表预览")]
        [ListDrawerSettings(HideAddButton = true, NumberOfItemsPerPage = 5)]
        public List<EnumListStruct> ComponentIdList = new List<EnumListStruct>();

        [BoxGroup("创建Component")]
        [ValueDropdown("GetComponentList")]
        [OnValueChanged("OnComponentIdChange")]
        [LabelText("ComponentId")]
        public int CompoentIdDropdown;

        [BoxGroup("创建Component")]
        public string ClassName;

        [BoxGroup("创建Component")]
        [Button("创建", ButtonSizes.Medium), GUIColor(0, 1, 0)]
        [PropertySpace(SpaceBefore = 10)]
        void OnCreate()
        {
            var context = new PropGeneratedContext()
            {
                NodeComponentIdName = ComponentIdList[CompoentIdDropdown].Name,
                Name = ClassName,
            };
            context.Name = context.Name.Replace("Component", "");

            // NodeComponentId 文件
            new EnumGenerated(Const.ComponentIdFile, "GameData.Config", "NodeComponentId", ComponentIdList).Generate();

            // Component 文件
            new TemplateGenerated(context)
				.Generate($"{Const.PropLogicComponentPath}/{context.Name}Component.cs", GeneratedTemplate.ComponentTmp);

            // Component注册文件
			new ComponentRegisterFileGenerated(context).Generate();
            AssetDatabase.Refresh();
        }

        public PropToolsComponentCreaterView(OdinMenuEditorWindow window) : base(window)
		{
		}

        [OnInspectorInit]
        void InitData()
        {
            ComponentIdList.Clear();
            var componentIdList = Enum.GetValues(typeof(NodeComponentId)).Cast<int>().ToList();
            componentIdList.Sort();
            componentIdList.Reverse();
            componentIdList.ForEach(x => ComponentIdList.Add(new EnumListStruct(){Id=x, Name=((NodeComponentId)x).ToString()}));
        }

        IEnumerable GetComponentList()
        {
            return ComponentIdList.Select((x, index) => new ValueDropdownItem(x.Name, index));
        }

        void OnComponentIdChange()
        {
            ClassName = ComponentIdList[CompoentIdDropdown].Name;
        }
    }
}