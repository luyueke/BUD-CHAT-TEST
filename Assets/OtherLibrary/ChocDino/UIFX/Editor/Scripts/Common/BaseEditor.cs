//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	public class BaseEditor : UnityEditor.Editor
	{
		protected void EnumAsToolbar(SerializedProperty prop, GUIContent displayName = null)
		{
			if (displayName == null)
			{
				displayName = new GUIContent(prop.displayName);
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(displayName);
			Rect rect = EditorGUILayout.GetControlRect();
			EditorGUILayout.EndHorizontal();

			displayName = EditorGUI.BeginProperty(rect, displayName, prop);

			EditorGUI.BeginChangeCheck();

			int newIndex = GUI.Toolbar(rect, prop.enumValueIndex, prop.enumNames);

			// Only assign the value back if it was actually changed by the user.
			// Otherwise a single value will be assigned to all objects when multi-object editing,
			// even when the user didn't touch the control.
			if (EditorGUI.EndChangeCheck())
			{
				prop.enumValueIndex = newIndex;
			}
			EditorGUI.EndProperty();
		}

		protected void TextureScaleOffset(SerializedProperty propTexture, SerializedProperty propScale, SerializedProperty propOffset, GUIContent displayName)
		{
			EditorGUILayout.PropertyField(propTexture);
			//EditorGUILayout.PropertyField(_propTextureOffset, Content_Offset);
			//EditorGUILayout.PropertyField(_propTextureScale, Content_Scale);

			Rect rect = EditorGUILayout.GetControlRect(true, 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing), EditorStyles.layerMaskField);
			EditorGUI.BeginChangeCheck();
			Vector4 scaleOffset = new Vector4(propScale.vector2Value.x, propScale.vector2Value.y, propOffset.vector2Value.x, propOffset.vector2Value.y);
			EditorGUI.indentLevel++;
			scaleOffset = MaterialEditor.TextureScaleOffsetProperty(rect, scaleOffset, false);
			EditorGUI.indentLevel--;
			if (EditorGUI.EndChangeCheck())
			{
				propScale.vector2Value = new Vector2(scaleOffset.x, scaleOffset.y);
				propOffset.vector2Value = new Vector2(scaleOffset.z, scaleOffset.w);
			}
		}
		
		protected SerializedProperty VerifyFindProperty(string propertyPath)
		{
			SerializedProperty result = serializedObject.FindProperty(propertyPath);
			Debug.Assert(result != null);
			if (result == null)
			{
				Debug.LogError("Failed to find property '" + propertyPath + "' in object '" + serializedObject.ToString()+ "'");
			}
			return result;
		}

		internal static SerializedProperty VerifyFindPropertyRelative(SerializedProperty property, string relativePropertyPath)
		{
			SerializedProperty result = null;
			Debug.Assert(property != null);
			if (property == null)
			{
				Debug.LogError("Property is null while finding relative property '"+ relativePropertyPath + "'");
			}
			else
			{
				result = property.FindPropertyRelative(relativePropertyPath);
				Debug.Assert(result != null);
				if (result == null)
				{
					Debug.LogError("Failed to find relative property '" + relativePropertyPath + "' in property '" + property.name + "'");
				}
			}
			return result;
		}
	}
}
