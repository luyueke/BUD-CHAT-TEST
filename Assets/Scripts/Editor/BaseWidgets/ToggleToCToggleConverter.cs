using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BudUI;

public class ReplaceToggleToCToggle : Editor
{
    [MenuItem("BudTools/转换工具/Toggle转为CToggle")]
    static void ReplaceToggleWithCToggle()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("未选中任何物体");
            return;
        }

        Toggle[] toggles = selectedObject.GetComponentsInChildren<Toggle>(true);
        int convertCount = 0;

        foreach (Toggle toggle in toggles)
        {
            // 跳过已经是CToggle的组件
            if (toggle is CToggle)
                continue;

            GameObject toggleObject = toggle.gameObject;

            // 保存原Toggle的属性
            bool originalIsOn = toggle.isOn;
            bool originalInteractable = toggle.interactable;
            ToggleGroup originalGroup = toggle.group;
            Toggle.ToggleEvent originalOnValueChanged = toggle.onValueChanged;
            Selectable.Transition originalTransition = toggle.transition;
            ColorBlock originalColors = toggle.colors;
            SpriteState originalSpriteState = toggle.spriteState;
            AnimationTriggers originalAnimationTriggers = toggle.animationTriggers;
            Graphic originalTargetGraphic = toggle.targetGraphic;
            Graphic originalGraphic = toggle.graphic;

            // 记录Undo
            Undo.RegisterCompleteObjectUndo(toggleObject, "Replace Toggle with CToggle");

            // 删除原Toggle组件
            DestroyImmediate(toggle, true);

            // 添加CToggle组件
            CToggle cToggle = toggleObject.AddComponent<CToggle>();

            // 还原原有属性
            cToggle.isOn = originalIsOn;
            cToggle.interactable = originalInteractable;
            cToggle.group = originalGroup;
            cToggle.onValueChanged = originalOnValueChanged;
            cToggle.transition = originalTransition;
            cToggle.colors = originalColors;
            cToggle.spriteState = originalSpriteState;
            cToggle.animationTriggers = originalAnimationTriggers;
            cToggle.targetGraphic = originalTargetGraphic;
            cToggle.graphic = originalGraphic;

            // 设置CToggle的默认属性
            cToggle.HasScaleAnim = true;
            cToggle.HasSound = true;
            cToggle.ScaleOnPressed = 0.8f;
            cToggle.ScaleDurationOnPress = 0.3f;
            cToggle.ScaleDurationOnReleased = 0.3f;

            convertCount++;
        }

        // 如果是场景物体，保存场景
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // 如果是预制体，保存预制体
        if (PrefabUtility.IsPartOfAnyPrefab(selectedObject))
        {
            PrefabUtility.SavePrefabAsset(selectedObject);
        }

        Debug.Log($"成功将 {convertCount} 个Toggle转换为CToggle");
    }

    // 验证菜单项是否可用
    [MenuItem("BudTools/转换工具/Toggle转为CToggle", true)]
    static bool ValidateReplaceToggle()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
            return false;

        // 检查是否有可转换的Toggle
        Toggle[] toggles = selectedObject.GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            if (!(toggle is CToggle))
                return true;
        }

        return false;
    }
}