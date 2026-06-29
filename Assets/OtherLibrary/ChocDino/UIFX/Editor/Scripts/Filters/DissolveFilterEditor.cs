//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_BETA

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(DissolveFilter), true)]
	[CanEditMultipleObjects]
	internal class DissolveFilterEditor : FilterBaseEditor
	{
		private static GUIContent Content_Dissolve = new GUIContent("Dissolve");
		private static GUIContent Content_Edge = new GUIContent("Edge");
		private static GUIContent Content_Apply = new GUIContent("Apply");
		private static GUIContent Content_ScaleMode = new GUIContent("Scale Mode");
		private static GUIContent Content_Length = new GUIContent("Length");
		private static GUIContent Content_ColorMode = new GUIContent("Color Mode");
		private static GUIContent Content_Color = new GUIContent("Color");
		private static GUIContent Content_Ramp = new GUIContent("Ramp");
		private static GUIContent Content_Emissive = new GUIContent("Emissive");

		private SerializedProperty _propDissolve;
		private SerializedProperty _propTexture;
		private SerializedProperty _propTextureScaleMode;
		private SerializedProperty _propScale;
		private SerializedProperty _propInvert;
		private SerializedProperty _propEdgeLength;
		private SerializedProperty _propEdgeColorMode;
		private SerializedProperty _propEdgeColor;
		private SerializedProperty _propEdgeTexture;
		private SerializedProperty _propEdgeEmissive;
		private SerializedProperty _propStrength;

		private readonly AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Dissolve Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/dissolve-filter/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Forum Thread", "https://forum.unity.com/threads/coming-soon-uifx-drop-shadow-filter.1550237/"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		void OnEnable()
		{
			_propDissolve = VerifyFindProperty("_dissolve");
			_propTexture = VerifyFindProperty("_texture");
			_propTextureScaleMode = VerifyFindProperty("_textureScaleMode");
			_propScale = VerifyFindProperty("_scale");
			_propInvert = VerifyFindProperty("_invert");
			_propEdgeLength = VerifyFindProperty("_edgeLength");
			_propEdgeColorMode = VerifyFindProperty("_edgeColorMode");
			_propEdgeColor = VerifyFindProperty("_edgeColor");
			_propEdgeTexture = VerifyFindProperty("_edgeTexture");
			_propEdgeEmissive = VerifyFindProperty("_edgeEmissive");
			_propStrength  = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Dissolve, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propDissolve);
			EditorGUILayout.PropertyField(_propTexture);
			EnumAsToolbar(_propTextureScaleMode, Content_ScaleMode);
			EditorGUILayout.PropertyField(_propScale);
			EditorGUILayout.PropertyField(_propInvert);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Edge, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propEdgeLength, Content_Length);
			EnumAsToolbar(_propEdgeColorMode, Content_ColorMode);
			if (_propEdgeColorMode.enumValueIndex == (int)DissolveEdgeColorMode.Color)
			{
				EditorGUILayout.PropertyField(_propEdgeColor, Content_Color);
			}
			else if (_propEdgeColorMode.enumValueIndex == (int)DissolveEdgeColorMode.Ramp)
			{
				EditorGUILayout.PropertyField(_propEdgeTexture, Content_Ramp);
			}
			if (_propEdgeColorMode.enumValueIndex != (int)DissolveEdgeColorMode.None)
			{
				EditorGUILayout.PropertyField(_propEdgeEmissive, Content_Emissive);
			}
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			DrawStrengthProperty(_propStrength);
			EditorGUI.indentLevel--;

			if (OnInspectorGUI_Baking(this.target as FilterBase))
			{
				return;
			}

			FilterBaseEditor.OnInspectorGUI_Debug(this.target as FilterBase);

			serializedObject.ApplyModifiedProperties();
		}
	}
}

#endif