using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using BudUI;

[CustomEditor(typeof(CToggle), true)]
public class CToggleEditor : ToggleEditor
{
    #region SerializedProperties
    // 文本颜色相关
    private SerializedProperty NormalTextColor;
    private SerializedProperty DisableTextColor;
    private SerializedProperty OnTextColor;
    private SerializedProperty OffTextColor;

    // 缩放动画相关
    private SerializedProperty HasScaleAnim;
    private SerializedProperty ScaleOnPressed;
    private SerializedProperty ScaleDurationOnPress;
    private SerializedProperty ScaleDurationOnReleased;
    private SerializedProperty ScaleOnSelected;
    private SerializedProperty ScaleDurationOnSelected;
    private SerializedProperty ScaleDurationOnDeselected;

    // 音效相关
    private SerializedProperty HasSound;
    private SerializedProperty SoundType;
    private SerializedProperty Timing;
    #endregion

    protected override void OnEnable()
    {
        base.OnEnable();

        // 初始化文本颜色属性
        NormalTextColor = serializedObject.FindProperty("NormalTextColor");
        DisableTextColor = serializedObject.FindProperty("DisableTextColor");
        OnTextColor = serializedObject.FindProperty("OnTextColor");
        OffTextColor = serializedObject.FindProperty("OffTextColor");

        // 初始化动画相关属性
        HasScaleAnim = serializedObject.FindProperty("HasScaleAnim");
        ScaleOnPressed = serializedObject.FindProperty("ScaleOnPressed");
        ScaleDurationOnPress = serializedObject.FindProperty("ScaleDurationOnPress");
        ScaleDurationOnReleased = serializedObject.FindProperty("ScaleDurationOnReleased");
        ScaleOnSelected = serializedObject.FindProperty("ScaleOnSelected");
        ScaleDurationOnSelected = serializedObject.FindProperty("ScaleDurationOnSelected");
        ScaleDurationOnDeselected = serializedObject.FindProperty("ScaleDurationOnDeselected");

        // 初始化音效相关属性
        HasSound = serializedObject.FindProperty("HasSound");
        SoundType = serializedObject.FindProperty("SoundType");
        Timing = serializedObject.FindProperty("Timing");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制原有Toggle的属性
        base.OnInspectorGUI();

        EditorGUILayout.Space(10);

        // 绘制文本颜色设置
        EditorGUILayout.LabelField("文本颜色设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(NormalTextColor, new GUIContent("正常颜色"));
        EditorGUILayout.PropertyField(DisableTextColor, new GUIContent("禁用颜色"));
        EditorGUILayout.PropertyField(OnTextColor, new GUIContent("选中颜色"));
        EditorGUILayout.PropertyField(OffTextColor, new GUIContent("未选中颜色"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // 绘制动画设置
        EditorGUILayout.LabelField("动画设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(HasScaleAnim, new GUIContent("启用缩放动画 "));

        if (HasScaleAnim.boolValue)
        {
            EditorGUILayout.PropertyField(ScaleOnPressed, new GUIContent("按下缩放比例"));
            EditorGUILayout.PropertyField(ScaleDurationOnPress, new GUIContent("按下动画时长"));
            EditorGUILayout.PropertyField(ScaleDurationOnReleased, new GUIContent("释放动画时长"));
            EditorGUILayout.PropertyField(ScaleOnSelected, new GUIContent("选中缩放比例"));
            EditorGUILayout.PropertyField(ScaleDurationOnSelected, new GUIContent("选中动画时长"));
            EditorGUILayout.PropertyField(ScaleDurationOnDeselected, new GUIContent("取消选中动画时长"));
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // 绘制音效设置
        EditorGUILayout.LabelField("音效设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(HasSound, new GUIContent("启用音效"));
        if (HasSound.boolValue)
        {
            EditorGUILayout.PropertyField(SoundType, new GUIContent("音效类型"));
            EditorGUILayout.PropertyField(Timing, new GUIContent("播放时机"));
        }
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }
}