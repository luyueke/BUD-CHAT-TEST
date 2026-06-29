using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(GradientImage), false)]
public class GradientImageEditor : ImageEditor
{
    private SerializedProperty _gradientDir;
    private SerializedProperty _colorStart;
    private SerializedProperty _colorEnd;
    private SerializedProperty _flip;

    protected override void OnEnable()
    {
        base.OnEnable();
        _gradientDir = serializedObject.FindProperty("_gradientDir");
        _colorStart  = serializedObject.FindProperty("_colorStart");
        _colorEnd    = serializedObject.FindProperty("_colorEnd");
        _flip        = serializedObject.FindProperty("_flip");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gradient", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_gradientDir);
        EditorGUILayout.PropertyField(_colorStart);
        EditorGUILayout.PropertyField(_colorEnd);
        EditorGUILayout.PropertyField(_flip);

        serializedObject.ApplyModifiedProperties();
    }
}
