using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BUD.EditorUtils
{
    public class BaseWidgetsUtils : Editor
    {
        
        [MenuItem("GameObject/BudUI/TabView",false, 5)]
        public static void CreateTabView()
        {
            string name = "TabView";
            string prefabPath = "Assets/Loadable/UI/BaseWidgets/TabView.prefab";
            GameObject selectObject = Selection.activeObject as GameObject;
            if (selectObject != null)
            {
                GameObject viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (viewPrefab != null)
                {
                    GameObject viewInstance = PrefabUtility.InstantiatePrefab(viewPrefab, selectObject.transform) as GameObject;
                    PrefabUtility.UnpackPrefabInstance(viewInstance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    viewInstance.name = name;
                    Selection.activeGameObject = viewInstance;
                }
                else
                {
                    Debug.LogError($"{name}.prefab not found at:{prefabPath}.");
                }
            }
            else
            {
                Debug.LogError($"Select a GameObject in the hierarchy to create the {name}.prefab instance.");
            }
        }
    }
}

