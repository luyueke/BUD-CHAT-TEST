using UI.BaseWidgets;
using UnityEditor;

[CustomEditor(typeof(CLongButton), true)]
public class CLongButtonEditor : CButtonEditor
{
    private SerializedProperty LongPressTime;

    protected override void OnEnable()
    {
        base.OnEnable();

        LongPressTime = serializedObject.FindProperty("m_LongPressTime");

    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(LongPressTime);

        serializedObject.ApplyModifiedProperties();
    }
}