using System;
using UnityEditor;
using UnityEngine;

namespace EasySpreadsheet
{
	internal class EsAboutWindow : EditorWindow
	{
		private Texture2D logo;
		
		private void Awake()
		{
			logo = Resources.Load<Texture2D>("logo");
		}

		private void OnGUI()
		{
			EsGUIStyle.Ensure();
			
			GUILayout.Space(10);
			GUILayout.Box(logo, EsGUIStyle.Box, GUILayout.Width(200), GUILayout.Height(124));
			GUILayout.Label("EasySpreadsheet", EsGUIStyle.largeLabel);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label("Version " + EsConstant.Version, EsGUIStyle.label);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label("(c) 2018-2023 Locke. All rights reserved.", EsGUIStyle.label);
			GUILayout.EndHorizontal();
			
			GUILayout.Space(10);
			
			GUILayout.Label("Support", EsGUIStyle.boldLabel);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			if (GUILayout.Button("Asset Store", EsGUIStyle.link))
				Application.OpenURL("http://u3d.as/WsS");
			GUILayout.EndHorizontal();
			
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			if (GUILayout.Button("locke.indienova.com", EsGUIStyle.link))
				Application.OpenURL("https://locke.indienova.com/");
			GUILayout.EndHorizontal();
			
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			if (GUILayout.Button("Email 1534921818@qq.com", EsGUIStyle.link))
				Application.OpenURL("mailto:1534921818@qq.com");
			GUILayout.EndHorizontal();
		}
		
	}
}