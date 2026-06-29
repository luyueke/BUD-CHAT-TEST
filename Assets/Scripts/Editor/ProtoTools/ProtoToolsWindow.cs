using System.Collections;
using common.editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace proto.editor
{
    public class ProtoToolsWindow : OdinMenuEditorWindow
    {
        [MenuItem(Const.UIPBMenu, false, 3)]
        private static void ShowWindow() {
            var window = GetWindow<ProtoToolsWindow>();
            window.titleContent = new GUIContent("PB工具");
            window.minSize = new Vector2(880, 810);
            window.Show();
        }

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree(supportsMultiSelect: false);
            tree.Add("PB转Json", new ProtoToolsViewerView(this), EditorIcons.House);
            return tree;
		}
	}
}