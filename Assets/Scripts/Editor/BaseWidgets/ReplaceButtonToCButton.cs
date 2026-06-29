using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ReplaceButtonToCButton : Editor
{
    [MenuItem("BudTools/转换工具/Button转为CButton")]
    static void ReplaceButtonWithCButton()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        Button[] buttons = selectedObject.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            GameObject buttonObject = button.gameObject;
            
            Selectable.Transition originalTransition = button.transition;
            ColorBlock originalColors = button.colors;
            SpriteState originalSpriteState = button.spriteState;
            
            DestroyImmediate(button, true);
            
            CButton cButton = buttonObject.AddComponent<CButton>();
            
            cButton.transition = originalTransition;
            cButton.colors = originalColors;
            cButton.spriteState = originalSpriteState;
        }
        
        
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        //保存
        PrefabUtility.SavePrefabAsset(selectedObject);
    }
}
