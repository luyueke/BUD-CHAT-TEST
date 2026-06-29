using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AddBaseLayoutTool : Editor
{
    [MenuItem("BudTools/添加BaseLayout")]
    public static void AddBaseLayout()
    {
        GameObject selectedObject = Selection.activeObject as GameObject;

        // 检查是否选择了UI预制体
        if (selectedObject == null)
        {
            Debug.LogWarning("Please open a prefab.");
            return;
        }

        // 检查是否有 RectTransform 组件
        RectTransform rectTransform = selectedObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("The selected prefab does not have a RectTransform component.");
            return;
        }

        // 创建BaseLayout3D节点
        CreateLayout(rectTransform, "BaseLayout3D");
        
        // 创建BaseLayout2D节点
        var baseLayout2D = CreateLayout(rectTransform, "BaseLayout2D");
        baseLayout2D.AddComponent<CanvasGroup>();

        Debug.Log("BaseLayout2D added to the UI prefab.");
    }

    private static GameObject CreateLayout(RectTransform parentRect, string layoutName)
    {
        GameObject baseLayout2D = new GameObject(layoutName);
        RectTransform baseLayoutRectTransform = baseLayout2D.AddComponent<RectTransform>();

        // 将BaseLayout2D设置为预制体的子节点
        baseLayoutRectTransform.SetParent(parentRect, false);

        // 设置为stretch模式
        baseLayoutRectTransform.anchorMin = Vector2.zero;
        baseLayoutRectTransform.anchorMax = Vector2.one;
        baseLayoutRectTransform.sizeDelta = Vector2.zero;

        // 将BaseLayout2D设置为预制体的子节点
        baseLayout2D.transform.SetParent(parentRect.transform, false);

        // 将BaseLayout2D移动到最上方
        baseLayout2D.transform.SetAsFirstSibling();
        return baseLayout2D;
    }
}
