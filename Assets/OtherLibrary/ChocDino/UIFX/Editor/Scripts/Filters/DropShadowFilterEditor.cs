//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(DropShadowFilter), true)]
	[CanEditMultipleObjects]
	internal class DropShadowFilterEditor : FilterBaseEditor
	{
		private static GUIContent Content_Shadow = new GUIContent("Shadow");
		private static GUIContent Content_Blur = new GUIContent("Blur");
		private static GUIContent Content_Apply = new GUIContent("Apply");

		private SerializedProperty _propDownsample;
		private SerializedProperty _propBlur;
		private SerializedProperty _propSourceAlpha;
		private SerializedProperty _propHardness;
		private SerializedProperty _propAngle;
		private SerializedProperty _propDistance;
		private SerializedProperty _propColor;
		private SerializedProperty _propMode;
		private SerializedProperty _propStrength;

		private readonly AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Drop Shadow Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-drop-shadow-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/drop-shadow-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/drop-shadow-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/drop-shadow-filter/components/drop-shadow-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/drop-shadow-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Drop Shadow Filter</b>", "https://assetstore.unity.com/packages/slug/272733?aid=1100lSvNe#reviews"),
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
			_propDownsample = VerifyFindProperty("_downSample");
			_propBlur = VerifyFindProperty("_blur");
			_propSourceAlpha = VerifyFindProperty("_sourceAlpha");
			_propHardness = VerifyFindProperty("_hardness");
			_propAngle = VerifyFindProperty("_angle");
			_propDistance = VerifyFindProperty("_distance");
			_propColor = VerifyFindProperty("_color");
			_propMode = VerifyFindProperty("_mode");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Shadow, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			EnumAsToolbar(_propMode);
			EditorGUILayout.PropertyField(_propAngle);
			EditorGUILayout.PropertyField(_propDistance);
			EditorGUILayout.PropertyField(_propColor);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Blur, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propDownsample);
			EditorGUILayout.PropertyField(_propBlur);
			EditorGUILayout.PropertyField(_propHardness);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propSourceAlpha);
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