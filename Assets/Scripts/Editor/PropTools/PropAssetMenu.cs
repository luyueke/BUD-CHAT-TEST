using common.editor;
using UnityEditor;
using UnityEngine;

namespace prop.editor
{
    public class PropAssetMenu
    {
        [MenuItem(Const.UIPropMenu + "配置",false, 2)]
        static void OpenConfigPath()
        {
            var dataPath = Application.dataPath;
            var s = dataPath.Replace("Assets", "GameConfig") + "/GamePropData.xlsx";
            EditorUtility.RevealInFinder(s);
        }

        [MenuItem(Const.UIPropMenu + "帮助文档", false, 3)]
        static void OpenDoc()
        {
            Application.OpenURL("https://pointone.feishu.cn/docx/K0JYd0iogoK9iTxbRmucScOenLe?from=from_copylink");
        }
    }
}