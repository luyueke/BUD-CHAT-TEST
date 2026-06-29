//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(PixelateFilter), true)]
	[CanEditMultipleObjects]
	internal class PixelateFilterEditor : FilterBaseEditor
	{
		private static GUIContent Content_Pixelate = new GUIContent("Pixelate");
		private static GUIContent Content_Apply = new GUIContent("Apply");

		private SerializedProperty _propSize;
		private SerializedProperty _propStrength;

		private readonly AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Pixelate Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/bundle/components/doom-melt-filter/"),
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
			_propSize = VerifyFindProperty("_size");
			_propStrength  = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Pixelate, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propSize);
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