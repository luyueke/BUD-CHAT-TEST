// @Author: YangJie
// @Description:
// @Date:  2023/09/04
// @Modify:

using System;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(CommonFlowButton),false)]
public class CommonFlowButtonEditor: CButtonEditor
{
    
            

    
    private SerializedProperty hollowSp;
    private SerializedProperty solidSp;
    private SerializedProperty btnImage;
    private SerializedProperty flowingImage;


    protected override void OnEnable()
    {
        base.OnEnable();
        hollowSp = serializedObject.FindProperty("hollowSp");
        solidSp = serializedObject.FindProperty("solidSp");
        btnImage = serializedObject.FindProperty("btnImage");
        flowingImage = serializedObject.FindProperty("flowingImage");
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.PropertyField(hollowSp);
        EditorGUILayout.PropertyField(solidSp);
        EditorGUILayout.PropertyField(btnImage);
        EditorGUILayout.PropertyField(flowingImage);
        
        serializedObject.ApplyModifiedProperties();
    }
}