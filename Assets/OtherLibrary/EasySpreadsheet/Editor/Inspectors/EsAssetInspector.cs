using UnityEngine;
using UnityEditor;

namespace EasySpreadsheet
{
	public abstract class EsAssetInspector : Editor
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
			
			var dataCollection = target as EsRowDataTable;
			if (dataCollection != null)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Box(Logo, EsGUIStyle.Box, GUILayout.Width(58), GUILayout.Height(36));
				EditorGUILayout.HelpBox(@"This file is generated from " + dataCollection.SpreadsheetFileName, MessageType.Info);
				GUILayout.EndHorizontal();

				var prevGUIState = GUI.enabled;
				GUI.enabled = false;
				base.OnInspectorGUI();
				GUI.enabled = prevGUIState;
			}
			else
			{
				base.OnInspectorGUI();
			}
		}
	}
}
