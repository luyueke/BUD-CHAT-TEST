//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

#if UIFX_BETA

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(DoomMeltFilter), true)]
	[CanEditMultipleObjects]
	internal class DoomMeltFilterEditor : FilterBaseEditor
	{
		private static GUIContent Content_Setup = new GUIContent("Setup");
		private static GUIContent Content_Time = new GUIContent("Time");
		private static GUIContent Content_Apply = new GUIContent("Apply");

		private SerializedProperty _propRandomSeed;
		private SerializedProperty _propColumnWidth;
		private SerializedProperty _propStepSize;
		private SerializedProperty _propTime;
		private SerializedProperty _propStrength;

		private readonly AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Doom Melt Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
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
			_propRandomSeed = VerifyFindProperty("_randomSeed");
			_propColumnWidth = VerifyFindProperty("_columnWidth");
			_propStepSize = VerifyFindProperty("_stepSize");
			_propTime = VerifyFindProperty("_time");
			_propStrength  = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Setup, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propRandomSeed);
			EditorGUILayout.PropertyField(_propColumnWidth);
			EditorGUILayout.PropertyField(_propStepSize);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Time, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propTime);
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