//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(TrailEffectBase), true)]
	[CanEditMultipleObjects]
	internal class TrailEffectEditor : BaseEditor
	{
		static GUIContent Content_Trail = new GUIContent("Trail");
		static GUIContent Content_Layers = new GUIContent("Layers");
		static GUIContent Content_Gradient = new GUIContent("Gradient");
		static GUIContent Content_Offset = new GUIContent("Offset");
		static GUIContent Content_Scale = new GUIContent("Scale");
		static GUIContent Content_Animation = new GUIContent("Animation");
		static GUIContent Content_OffsetSpeed = new GUIContent("Offset Speed");
		static GUIContent Content_Apply = new GUIContent("Apply");
		static GUIContent Content_Mode = new GUIContent("Vertex Modifier");

		private SerializedProperty _propLayerCount;
		private SerializedProperty _propDampingFront;
		private SerializedProperty _propDampingBack;
		private SerializedProperty _propAlphaCurve;
		private SerializedProperty _propVertexModifierSource;
		private SerializedProperty _propGradient;
		private SerializedProperty _propGradientOffset;
		private SerializedProperty _propGradientScale;
		private SerializedProperty _propGradientOffsetSpeed;
		private SerializedProperty _propShowTrailOnly;
		private SerializedProperty _propBlendMode;
		private SerializedProperty _propStrengthMode;
		private SerializedProperty _propStrength;

		private AboutInfo aboutInfo = 
				new AboutInfo("UIFX - Trail Effect\n© Chocolate Dinosaur Ltd", "uifx-logo-trail-effect")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/trail/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/trail/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/trail/components/trail-effect/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/uifx/trail/API/ChocDino.UIFX/"),
							}
						},
						new AboutSection("Unity Asset Store")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("<color=#ffd700>★★★★☆</color>\r\n<b>PLEASE leave your honest Review / Rating</b>\r\n<color=#ffd700>★★★★☆</color>", "https://assetstore.unity.com/packages/slug/260697?aid=1100lSvNe#reviews"),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", "https://discord.gg/wKRzKAHVUE"),
								new AboutButton("Post to Unity Forum Thread", "https://forum.unity.com/threads/released-uifx-trail-effect.1501220/"),
								new AboutButton("Post Issues to GitHub", "https://github.com/Chocolate-Dinosaur/UIFX/issues"),
								new AboutButton("Email Us", "mailto:support@chocdino.com"),
							}
						}
					}
				};

		void OnEnable()
		{
			_propLayerCount = VerifyFindProperty("_layerCount");
			_propDampingFront = VerifyFindProperty("_dampingFront");
			_propDampingBack = VerifyFindProperty("_dampingBack");
			_propAlphaCurve = VerifyFindProperty("_alphaCurve");
			_propVertexModifierSource = VerifyFindProperty("_vertexModifierSource");
			_propGradient = VerifyFindProperty("_gradient");
			_propGradientOffset = VerifyFindProperty("_gradientOffset");
			_propGradientScale = VerifyFindProperty("_gradientScale");
			_propGradientOffsetSpeed = VerifyFindProperty("_gradientOffsetSpeed");
			_propShowTrailOnly = VerifyFindProperty("_showTrailOnly");
			_propBlendMode = VerifyFindProperty("_blendMode");
			_propStrengthMode = VerifyFindProperty("_strengthMode");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			GUILayout.Label(Content_Trail, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propLayerCount, Content_Layers);
			EditorGUILayout.PropertyField(_propDampingFront);
			EditorGUILayout.PropertyField(_propDampingBack);
			EditorGUILayout.PropertyField(_propAlphaCurve);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(_propVertexModifierSource, Content_Mode);
			if (EditorGUI.EndChangeCheck())
			{
				// Manually adjust the property so that OnChangedVertexModifier() gets called. 
				foreach (TrailEffectBase trail in this.targets)
				{
					trail.VertexModifierSource = (VertexModifierSource)_propVertexModifierSource.enumValueIndex;
				}
			}
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Gradient, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propGradient);
			EditorGUILayout.PropertyField(_propGradientOffset, Content_Offset);
			EditorGUILayout.PropertyField(_propGradientScale, Content_Scale);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Animation, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propGradientOffsetSpeed, Content_OffsetSpeed);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propShowTrailOnly);
			EditorGUILayout.PropertyField(_propBlendMode);
			EditorGUILayout.PropertyField(_propStrengthMode);
			EditorGUILayout.PropertyField(_propStrength);
			EditorGUI.indentLevel--;

			serializedObject.ApplyModifiedProperties();
		}
	}
}