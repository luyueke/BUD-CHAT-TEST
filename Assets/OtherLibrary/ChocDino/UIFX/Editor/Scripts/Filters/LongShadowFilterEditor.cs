//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(LongShadowFilter), true)]
	[CanEditMultipleObjects]
	internal class LongShadowFilterEditor : FilterBaseEditor
	{
		private static GUIContent Content_LongShadow = new GUIContent("Long Shadow");
		private static GUIContent Content_Distance = new GUIContent("Distance");
		private static GUIContent Content_Apply = new GUIContent("Apply");

		private SerializedProperty _propMethod;
		private SerializedProperty _propAngle;
		private SerializedProperty _propDistance;
		private SerializedProperty _propStepSize;
		private SerializedProperty _propColorSource;
		private SerializedProperty _propColor1;
		private SerializedProperty _propUseBackColor;
		private SerializedProperty _propColor2;
		private SerializedProperty _propPivot;
		private SerializedProperty _propSourceAlpha;
		private SerializedProperty _propCompositeMode;
		private SerializedProperty _propStrength;

		private readonly AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Long Shadow Filter\n© Chocolate Dinosaur Ltd", "uifx-logo-long-shadow-filter")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/long-shadow-filter/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/long-shadow-filter/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/long-shadow-filter/components/long-shadow-filter/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/long-shadow-filter/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Long Shadow Filter</b>", "https://assetstore.unity.com/packages/slug/276742?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", AssetStoreBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Forum Thread", "https://forum.unity.com/threads/coming-soon-uifx-long-shadow-filter.1551566/"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		void OnEnable()
		{
			_propMethod = VerifyFindProperty("_method");
			_propSourceAlpha = VerifyFindProperty("_sourceAlpha");
			_propAngle = VerifyFindProperty("_angle");
			_propDistance = VerifyFindProperty("_distance");
			_propStepSize = VerifyFindProperty("_stepSize");
			_propColor1 = VerifyFindProperty("_colorFront");
			_propUseBackColor = VerifyFindProperty("_useBackColor");
			_propColor2 = VerifyFindProperty("_colorBack");
			_propPivot = VerifyFindProperty("_pivot");
			_propCompositeMode = VerifyFindProperty("_compositeMode");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Distance, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propMethod);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_LongShadow, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			EditorGUILayout.PropertyField(_propAngle);
			EditorGUILayout.PropertyField(_propDistance);
			if (_propMethod.enumValueIndex == (int)LongShadowMethod.Normal)
			{
				EditorGUILayout.PropertyField(_propStepSize);
			}

			EditorGUILayout.PropertyField(_propColor1);
			EditorGUILayout.PropertyField(_propUseBackColor);
			if (_propUseBackColor.boolValue)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_propColor2);
				EditorGUI.indentLevel--;
			}
			EditorGUI.indentLevel--;

			EditorGUILayout.PropertyField(_propPivot);

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propSourceAlpha);
			EnumAsToolbar(_propCompositeMode);
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