//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(GooeyFilter), true)]
	[CanEditMultipleObjects]
	internal class GooeyFilterEditor : FilterBaseEditor	
	{
		private static GUIContent Content_Gooey = new GUIContent("Gooey");
		private static GUIContent Content_Apply = new GUIContent("Apply");

		private SerializedProperty _propSize;
		private SerializedProperty _propBlur;
		private SerializedProperty _propThreshold;
		private SerializedProperty _propThresholdFalloff;
		private SerializedProperty _propStrength;

		private readonly AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Gooey Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-gooey-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/gooey-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/gooey-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/gooey-filter/components/gooey-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/gooey-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								//new AboutButton("Review <b>UIFX - Gooey Filter</b>", "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Forum Thread", ForumBundleUrl),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		void OnEnable()
		{
			_propSize = VerifyFindProperty("_size");
			_propBlur = VerifyFindProperty("_blur");
			_propThreshold = VerifyFindProperty("_threshold");
			_propThresholdFalloff = VerifyFindProperty("_thresholdFalloff");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Gooey, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propSize);
			EditorGUILayout.PropertyField(_propBlur);
			EditorGUILayout.PropertyField(_propThreshold);
			EditorGUILayout.PropertyField(_propThresholdFalloff);
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