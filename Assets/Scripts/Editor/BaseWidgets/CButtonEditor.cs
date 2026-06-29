using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(CButton),true)]
public class CButtonEditor : ButtonEditor
{
    private SerializedProperty NormalTextColor;
    private SerializedProperty DisableTextColor;
    private SerializedProperty HasScaleAnim;
    private SerializedProperty ScaleOnPressed;
    private SerializedProperty ScaleDurationOnPress;
    private SerializedProperty ScaleDurationOnReleased;
    private SerializedProperty ScaleOnSelected;
    private SerializedProperty ScaleDurationOnSelected;
    private SerializedProperty ScaleDurationOnDeselected;
    
    private SerializedProperty HasSound ;
    private SerializedProperty SoundType;
    private SerializedProperty Timing;
   
    protected override void OnEnable()
    {
        base.OnEnable();
        NormalTextColor = serializedObject.FindProperty("NormalTextColor");
        DisableTextColor = serializedObject.FindProperty("DisableTextColor");
        HasScaleAnim = serializedObject.FindProperty("HasScaleAnim");
        ScaleOnPressed = serializedObject.FindProperty("ScaleOnPressed");
        ScaleDurationOnPress = serializedObject.FindProperty("ScaleDurationOnPress");
        ScaleDurationOnReleased = serializedObject.FindProperty("ScaleDurationOnReleased");
        ScaleOnSelected = serializedObject.FindProperty("ScaleOnSelected");
        ScaleDurationOnSelected = serializedObject.FindProperty("ScaleDurationOnSelected");
        ScaleDurationOnDeselected = serializedObject.FindProperty("ScaleDurationOnDeselected");
        
        HasSound = serializedObject.FindProperty("HasSound");
        SoundType = serializedObject.FindProperty("SoundType");
        Timing = serializedObject.FindProperty("Timing");
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(NormalTextColor);
        EditorGUILayout.PropertyField(DisableTextColor);
        EditorGUILayout.PropertyField(HasScaleAnim);
        EditorGUILayout.PropertyField(ScaleOnPressed);
        EditorGUILayout.PropertyField(ScaleDurationOnPress);
        EditorGUILayout.PropertyField(ScaleDurationOnReleased);
        EditorGUILayout.PropertyField(ScaleOnSelected);
        EditorGUILayout.PropertyField(ScaleDurationOnSelected);
        EditorGUILayout.PropertyField(ScaleDurationOnDeselected);
        EditorGUILayout.PropertyField(HasSound);
        EditorGUILayout.PropertyField(SoundType);
        EditorGUILayout.PropertyField(Timing);

        serializedObject.ApplyModifiedProperties();
    }

    public static void CreateCommonButton(string name,string prefabPath)
    {
        GameObject selectObject = Selection.activeObject as GameObject;
        if (selectObject != null)
        {
            GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (buttonPrefab != null)
            {
                GameObject buttonInstance = PrefabUtility.InstantiatePrefab(buttonPrefab, selectObject.transform) as GameObject;
                buttonInstance.name = name;
                Selection.activeGameObject = buttonInstance;
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

    [MenuItem("GameObject/BudUI/CButton",false, 1)]
    public static void CreateCButton()
    {
        string prefabPath = "Assets/Loadable/UI/BaseWidgets/CButton.prefab";
        CreateCommonButton("CButton",prefabPath);
    }
    
    [MenuItem("GameObject/BudUI/RoundButton",false, 2)]
    public static void CreateRoundButton()
    {
        string prefabPath = "Assets/Loadable/UI/BaseWidgets/RoundButton.prefab";
        CreateCommonButton("RoundButton",prefabPath);
    }
    
       
    [MenuItem("GameObject/BudUI/RectButton",false, 3)]
    public static void CreateRectButton()
    {
        string prefabPath = "Assets/Loadable/UI/BaseWidgets/RectButton.prefab";
        CreateCommonButton("RectButton",prefabPath);
    }
}
