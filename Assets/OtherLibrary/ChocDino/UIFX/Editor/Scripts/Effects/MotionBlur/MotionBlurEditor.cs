//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(MotionBlurSimple))]
	[CanEditMultipleObjects]
	internal class MotionBlurSimpleEditor : MotionBlurEditor
	{
		private SerializedProperty _propMode;
		private SerializedProperty _propSampleCount;
		private SerializedProperty _propBlendStrength;
		private SerializedProperty _propFrameRateIndependent;
		private SerializedProperty _propStrength;

		void OnEnable()
		{
			_propMode = VerifyFindProperty("_mode");
			_propSampleCount = VerifyFindProperty("_sampleCount");
			_propBlendStrength = VerifyFindProperty("_blendStrength");
			_propFrameRateIndependent = VerifyFindProperty("_frameRateIndependent");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			EditorGUILayout.PropertyField(_propMode);
			EditorGUILayout.PropertyField(_propSampleCount);
			EditorGUILayout.PropertyField(_propBlendStrength);
			EditorGUILayout.PropertyField(_propFrameRateIndependent);
			EditorGUILayout.PropertyField(_propStrength);
			
			serializedObject.ApplyModifiedProperties();
		}
	}

	[CustomEditor(typeof(MotionBlurReal))]
	[CanEditMultipleObjects]
	internal class MotionBlurRealEditor : MotionBlurEditor
	{
		private SerializedProperty _propMode;
		private SerializedProperty _propSampleCount;
		private SerializedProperty _propFrameRateIndependent;
		private SerializedProperty _propStrength;
		private SerializedProperty _propShaderAdd;
		private SerializedProperty _propShaderResolve;

		void OnEnable()
		{
			_propMode = VerifyFindProperty("_mode");
			_propSampleCount = VerifyFindProperty("_sampleCount");
			_propFrameRateIndependent = VerifyFindProperty("_frameRateIndependent");
			_propStrength = VerifyFindProperty("_strength");
			_propShaderAdd = VerifyFindProperty("_shaderAdd");
			_propShaderResolve = VerifyFindProperty("_shaderResolve");
		}

		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			EditorGUILayout.PropertyField(_propMode);
			EditorGUILayout.PropertyField(_propSampleCount);
			EditorGUILayout.PropertyField(_propFrameRateIndependent);
			EditorGUILayout.PropertyField(_propStrength);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_propShaderAdd);
			EditorGUILayout.PropertyField(_propShaderResolve);

			serializedObject.ApplyModifiedProperties();
		}
	}

	internal abstract class MotionBlurEditor : BaseEditor
	{
		protected AboutInfo aboutInfo
				= new AboutInfo("UIFX - Motion Blur\n© Chocolate Dinosaur Ltd", "uifx-logo-motion-blur")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Documentation")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("User Guide", "https://www.chocdino.com/products/uifx/motion-blur/about/"),
								new AboutButton("Scripting Guide", "https://www.chocdino.com/products/uifx/motion-blur/scripting/"),
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/motion-blur/components/motion-blur-real/"),
								new AboutButton("API Reference", "https://www.chocdino.com/products/unity-assets/"),
							}
						},
						new AboutSection("Unity Asset Store")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("<color=#ffd700>★★★★☆</color>\r\n<b>PLEASE leave your honest Review / Rating</b>\r\n<color=#ffd700>★★★★☆</color>", "https://assetstore.unity.com/packages/slug/260687?aid=1100lSvNe#reviews"),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", "https://discord.gg/wKRzKAHVUE"),
								new AboutButton("Post to Unity Forum Thread", "https://forum.unity.com/threads/released-uifx-motion-blur.1501217/"),
								new AboutButton("Post Issues to GitHub", "https://github.com/Chocolate-Dinosaur/UIFX/issues"),
								new AboutButton("Email Us", "mailto:support@chocdino.com"),
							}
						}
					}
				};
		
		public override void OnInspectorGUI()
		{
			aboutInfo.OnGUI();

			serializedObject.Update();

			DrawDefaultInspector();

			serializedObject.ApplyModifiedProperties();
		}
	}
}