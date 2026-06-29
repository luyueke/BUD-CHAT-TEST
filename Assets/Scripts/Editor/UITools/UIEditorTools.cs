using System;
using System.IO;
using UI.Base;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI脚本生成工具
/// 1.导表后，生成PanelId和WindowId枚举类
/// 2.预制体自动生成变体并copy到Loadable目录 -- 已废弃
/// 3.生成对应预制体的Panel类
/// </summary>

namespace editor.UITools
{
    public class UIEditorTools
    {
        private const string UIMenu = "BudTools/UI工具/";

        [MenuItem(UIMenu + "手动生成UI枚举脚本", false, 1)]
        static void ForceGenEnum()
        {
            UIEnumGenerator.GenerateAll();
        }

        [MenuItem(UIMenu + "快速打开UI配置目录", false, 2)]
        static void OpenUIConfigPath()
        {
            var dataPath = Application.dataPath;
            var s = dataPath.Replace("Assets", "GameConfig") + "/UIPanel.xlsx";
            Debug.Log($"OpenUIConfigPath : {s}");
            EditorUtility.RevealInFinder(s);
        }

        [MenuItem(UIMenu + "帮助文档/UI框架说明", false, 3)]
        static void OpenDocUI()
        {
            Application.OpenURL("https://pointone.feishu.cn/docx/RQuad8TFOoShxHxj2GhcpeT0nbn");
        }

        [MenuItem(UIMenu + "帮助文档/UI组件文档", false, 3)]
        static void OpenDocUIWidget()
        {
            Application.OpenURL("https://pointone.feishu.cn/docx/UTzUd6sDyomf51xTpHAczo8An7f");
        }        
        
        [MenuItem(UIMenu + "帮助文档/UI组件Figma链接", false, 3)]
        static void OpenDocUIWidgetFigma()
        {
            Application.OpenURL("https://www.figma.com/file/e1pCgPlRu2EoTUgcKeheFz/BUD%E5%9B%BD%E6%9C%8D%E7%BB%84%E4%BB%B6?type=design&node-id=0-1&mode=design&t=XRxkbqsKrZZw2uQg-0");
        }

        #region base panel script generator
        
        private static string OUPUT_PANEL_SCRIPT_PATH => Application.dataPath + "/Scripts/UI/UIPanels/";
        private static string UI_PANEL_TEMPLATE_SCRIPT => Application.dataPath + "/Scripts/Editor/UITools/UIPanelTemplate.cs";
        
        [MenuItem("Assets/UI工具/自动生成BasePanel脚本并挂载")]
        [MenuItem(UIMenu + "自动生成BasePanel脚本并挂载", false, 1)]
        static void GenerateUIScripts()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("游戏运行时无法生成UI Script");
                return;
            }

            var folders = Selection.assetGUIDs;
            if (folders.Length != 1) return;

            var folder = AssetDatabase.GUIDToAssetPath(folders[0]);
            if (AssetDatabase.IsValidFolder(folder)) return;

            var prefab = Selection.activeGameObject;
            if (prefab == null)
            {
                Debug.LogError("需先选中UI Prefab预制体");
                return;
            }

            // MoveToLoadable();
            GeneratePanelScriptFile();
            AssetDatabase.Refresh();
        }

        private static void GeneratePanelScriptFile()
        {
            var prefab = Selection.activeGameObject;
            if (prefab == null) return;

            var panelScript = prefab.GetComponent<BasePanel>();
            if (panelScript != null)
            {
                Debug.LogError($"{prefab.name} 已经挂载了BasePanel脚本!");
                return;
            }

            var scriptPath = OUPUT_PANEL_SCRIPT_PATH + prefab.name + ".cs";
            if (!File.Exists(scriptPath))
            {
                var tempFile = File.ReadAllText(UI_PANEL_TEMPLATE_SCRIPT);
                var saveStr = tempFile.Replace("{DATE}", DateTime.Now.ToString("yy-MM-dd HH:mm:ss"));
                saveStr = saveStr.Replace("UIPanelTemplate", prefab.name);
                File.WriteAllText(scriptPath, saveStr);
            }

            EditorPrefs.SetBool("GeneratePanelScriptFile", true);
            Debug.Log("GeneratePanelScriptFile success");
            AssetDatabase.ImportAsset(scriptPath, ImportAssetOptions.ForceSynchronousImport);
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }


        [UnityEditor.Callbacks.DidReloadScripts(0)]
        static void OnScriptReload()
        {
            if (EditorPrefs.HasKey("GeneratePanelScriptFile") && EditorPrefs.GetBool("GeneratePanelScriptFile"))
            {
                EditorPrefs.DeleteKey("GeneratePanelScriptFile");

                var prefab = Selection.activeGameObject;
                if (prefab == null)
                {
                    return;
                }

                if (prefab.gameObject.GetComponent<BasePanel>() == null)
                {
                    var guidStrs = Selection.assetGUIDs;
                    var clsName = prefab.name;
                    string path = AssetDatabase.GUIDToAssetPath(guidStrs[0]);
                    var clsType = Type.GetType(clsName + ",GameUI");
                    if (clsType == null)
                    {
                        Debug.LogError($"找不到类:{clsName}");
                        return;
                    }

                    prefab.AddComponent(clsType);
                    PrefabUtility.SavePrefabAsset(prefab);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        #endregion
    }
}