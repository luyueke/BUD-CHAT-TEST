using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class AddLocalizationTool
{
    [MenuItem("BudTools/添加LocalizationComponent")]
    public static void AddLocalizationComponents()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject != null)
        {
            AddLocalizationComponentsInChildren(selectedObject);
            EditorUtility.SetDirty(selectedObject);
            if (PrefabUtility.IsPartOfPrefabInstance(selectedObject))
            {
                PrefabUtility.SavePrefabAsset(selectedObject);
            }

            AssetDatabase.SaveAssets();
            LoggerUtils.Log("成功添加LocalizationComponent");
        }
        else
        {
            LoggerUtils.LogError("请选择一个节点");
        }
    }

    private static void AddLocalizationComponentsInChildren(GameObject parent)
    {
        Text[] textComponents = parent.GetComponentsInChildren<Text>(true);
        foreach (Text textComponent in textComponents)
        {
            SetTextProperties(textComponent); // 设置 Text 属性
            AddLocalizationComp(textComponent.transform, textComponent.text);
        }

        SuperTextMesh[] superTMComponents = parent.GetComponentsInChildren<SuperTextMesh>(true);
        foreach (SuperTextMesh superTM in superTMComponents)
        {
            AddLocalizationComp(superTM.transform, superTM.text);
        }
    }

    private static void SetTextProperties(Text textComponent)
    {
        if (!textComponent.resizeTextForBestFit)
        {
            textComponent.resizeTextForBestFit = true; // 勾选 BestFit
        }

        int maxSize = textComponent.fontSize;
        textComponent.resizeTextMaxSize = maxSize; // 设置 MaxSize
        textComponent.resizeTextMinSize = Mathf.Max(Mathf.CeilToInt(maxSize / 4f), 2); // 设置 MinSize，确保不小于 2
    }

    private static void AddLocalizationComp(Transform child, string content)
    {
        if (child.GetComponent<LocalizationComponent>() == null)
        {
            LocalizationComponent localizationComponent = child.gameObject.AddComponent<LocalizationComponent>();
            localizationComponent.localizationKey = content.Trim();
        }
    }
}
