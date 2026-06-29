using UnityEngine;
using UI.UIPanels.GameEdit;
using UnityEditor;

namespace debugtools.editor
{
    [CustomEditor(typeof(BasePropertyEditSubView), true)]
    [CanEditMultipleObjects]
    public class BasePropertyEditSubViewEditor : Editor
    {
        bool isExpand;
        SerializedProperty isExpandSerializedProperty;
        BasePropertyEditSubView subView;

        private void OnEnable()
        {
            subView = this.target as BasePropertyEditSubView;
            isExpandSerializedProperty = serializedObject.FindProperty("isExpand");
            isExpand = isExpandSerializedProperty.boolValue;
            subView.SetIsExpand(isExpand);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (isExpand != isExpandSerializedProperty.boolValue)
            {
                isExpand = isExpandSerializedProperty.boolValue;
                subView.SetIsExpand(isExpand);
            }
        }
    }
}