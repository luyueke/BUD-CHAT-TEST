using DA_Assets.Constants;
using DA_Assets.FCU;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RemoveComponentTool : EditorWindow
{
    private MonoScript scriptToRemove;

    [MenuItem("Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + "一键移除场景脚本", false, 12)]
    public static void ShowWindow()
    {
        GetWindow<RemoveComponentTool>("一键移除场景脚本");
    }

    void OnGUI()
    {
        GUILayout.Label("一键移除场景中所有物体上的指定脚本", EditorStyles.boldLabel);
        scriptToRemove = (MonoScript)EditorGUILayout.ObjectField("目标脚本", scriptToRemove, typeof(MonoScript), false);

        if (GUILayout.Button("移除所有该脚本"))
        {
            if (scriptToRemove == null)
            {
                EditorUtility.DisplayDialog("错误", "请拖拽一个脚本！", "OK");
                return;
            }
            Type type = scriptToRemove.GetClass();
            if (type == null || !type.IsSubclassOf(typeof(MonoBehaviour)))
            {
                EditorUtility.DisplayDialog("错误", "请选择一个有效的MonoBehaviour脚本！", "OK");
                return;
            }
            RemoveAllComponents(type);
        }
    }

    void RemoveAllComponents(Type type)
    {
        int count = 0;
        var allObjects = GameObject.FindObjectsOfType<GameObject>(true); // 包含隐藏物体
        foreach (var go in allObjects)
        {
            var comp = go.GetComponent(type);
            if (comp != null)
            {
                Undo.DestroyObjectImmediate((Component)comp);
                count++;
            }
        }
        EditorUtility.DisplayDialog("完成", $"已移除 {count} 个组件：{type.Name}", "OK");
    }
}