using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace EasySpreadsheet
{
	[CustomEditor(typeof(EsSettings))]
	public class EsSettingsInspector : Editor
	{
		private static Texture2D _logo;

		private static Texture2D Logo
		{
			get
			{
				if (_logo == null)
					_logo = Resources.Load<Texture2D>("logo");
				return _logo;
			}
		}
		
		public override void OnInspectorGUI()
		{
			EsGUIStyle.Ensure();

			var settings = target as EsSettings;
			
			GUILayout.BeginHorizontal();
			GUILayout.Box(Logo, EsGUIStyle.Box, GUILayout.Width(58), GUILayout.Height(36));
			EditorGUILayout.HelpBox(@"To modify this, open the settings window.", MessageType.Info);
			GUILayout.EndHorizontal();
			EditorGUILayout.Separator();
			
			var prevGUIState = GUI.enabled;
			GUI.enabled = false;
			base.OnInspectorGUI();
			GUI.enabled = prevGUIState;
			
			EditorGUILayout.Separator();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Edit", GUILayout.Width(100), GUILayout.Height(20)))
				EsMenu.OpenSettingsWindow();
			
			if (GUILayout.Button("Reset", GUILayout.Width(100), GUILayout.Height(20)))
				settings.ResetAll();
			GUILayout.EndHorizontal();
		}
		
		[OnOpenAsset(10)]
		private static bool OnOpenSpreadsheetFile(int instanceId, int line)
		{
			try
			{
				var asset = EditorUtility.InstanceIDToObject(instanceId) as EsSettings;
				if (asset == null)
					return false;
				EsMenu.OpenSettingsWindow();
				return true;
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}

			return false;
		}
		
	}
}