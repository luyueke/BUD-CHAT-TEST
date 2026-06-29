using DA_Assets.Constants;
using DA_Assets.FCU;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class RemoveUIMaterialTool : EditorWindow
{
    [MenuItem("Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + "一键移除场景UI材质", false, 12)]
    public static void ShowWindow()
    {
        GetWindow<RemoveUIMaterialTool>("一键移除场景UI材质");
    }

    void OnGUI()
    {
        GUILayout.Label("一键移除场景中所有UI上的Material", EditorStyles.boldLabel);

        if (GUILayout.Button("移除所有UI上的Material"))
        {
            RemoveAllUIMaterials();
        }
    }

    void RemoveAllUIMaterials()
    {
        int count = 0;
        // 处理所有Graphic（Image、RawImage、Text等）
        var graphics = GameObject.FindObjectsOfType<Graphic>(true);
        foreach (var g in graphics)
        {
            if (g.material != null)
            {
                Undo.RecordObject(g, "Remove UI Material");
                g.material = null;
                count++;
            }
        }

        // 处理TextMeshProUGUI（如果有TextMeshPro包）
#if TMP_PRESENT
        var tmps = GameObject.FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var t in tmps)
        {
            if (t.fontMaterial != null)
            {
                Undo.RecordObject(t, "Remove TMP Material");
                t.fontMaterial = null;
                count++;
            }
        }
#endif

        EditorUtility.DisplayDialog("完成", $"已移除 {count} 个UI组件上的Material。", "OK");
    }
}