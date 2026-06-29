//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	internal struct AboutButton
	{
		public GUIContent title;
		public string url;

		public AboutButton(string title, string url)
		{
			this.title = new GUIContent(title);
			this.url = url;
		}
	}

	internal struct AboutSection
	{
		public GUIContent title;
		public AboutButton[] buttons;

		public AboutSection(string title)
		{
			this.title = new GUIContent(title);
			this.buttons = null;
		}
	}

	/// <summary>
	/// A customisable UI window that displays basic information about the asset/component and has
	/// categories of buttons that link to documentation and support
	/// </summary>
	internal class AboutInfo
	{
		public string iconPath;
		public GUIContent title;
		public AboutSection[] sections;
		private GUIContent icon;

		private bool isExpanded = false;
		private static GUIStyle paddedBoxStyle;
		private static GUIStyle richBoldLabel;
		private static GUIStyle richButtonStyle;

		public AboutInfo(string title, string iconPath)
		{
			this.title = new GUIContent(title);
			this.iconPath = iconPath;
			sections = null;
		}

		internal void OnGUI()
		{
			if (paddedBoxStyle == null)
			{
				paddedBoxStyle = new GUIStyle(GUI.skin.window);
				paddedBoxStyle.padding = new RectOffset(8, 8, 16, 16);
			}
			if (richBoldLabel == null)
			{
				richBoldLabel = new GUIStyle(EditorStyles.boldLabel);
				richBoldLabel.richText = true;
				richBoldLabel.alignment = TextAnchor.UpperCenter;
				richBoldLabel.margin.top = 20;
				richBoldLabel.margin.bottom = 10;
				richBoldLabel.fontSize = 14;
			}
			if (richButtonStyle == null)
			{
				richButtonStyle = new GUIStyle(GUI.skin.button);
				richButtonStyle.richText = true;
				richButtonStyle.stretchWidth = true;
			}
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			isExpanded = GUILayout.Toggle(isExpanded, new GUIContent((isExpanded?"▼":"►") + " About & Help"), GUI.skin.button);
			GUILayout.EndHorizontal();
			
			if (this.icon == null)
			{
				this.icon = new GUIContent(Resources.Load<Texture2D>(this.iconPath));
			}

			if (isExpanded)
			{
				GUILayout.BeginVertical(paddedBoxStyle);

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(this.icon);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label(this.title, EditorStyles.largeLabel);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				foreach (AboutSection section in this.sections)
				{
					GUILayout.Label(section.title, richBoldLabel);
					GUILayout.BeginVertical();
					if (section.buttons != null)
					{
						foreach (AboutButton button in section.buttons)
						{
							if (GUILayout.Button(button.title, richButtonStyle))
							{
								Application.OpenURL(button.url);
							}
						}
					}
					GUILayout.EndVertical();
				}
				
				GUILayout.EndVertical();
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}
		}
	}
}
