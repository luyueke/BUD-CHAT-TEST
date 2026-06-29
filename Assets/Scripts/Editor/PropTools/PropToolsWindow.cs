using common.editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace prop.editor
{
    public class PropToolsWindow : OdinMenuEditorWindow
    {
        [MenuItem(Const.UIPropMenu + "主界面", false, 1)]
        private static void ShowWindow() {
            // PropsToolHelper.Clear();
            var window = GetWindow<PropToolsWindow>();
            window.titleContent = new GUIContent("道具工具");
            window.Show();
        }

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree(supportsMultiSelect: false);
            tree.Add("创建道具", new PropToolsCreaterView(this), EditorIcons.House);
            tree.Add("Component管理", new PropToolsComponentCreaterView(this), EditorIcons.House);
            return tree;
		}
	}
}