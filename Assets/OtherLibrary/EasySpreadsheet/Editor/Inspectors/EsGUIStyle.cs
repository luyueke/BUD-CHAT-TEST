using System;
using UnityEditor;
using UnityEngine;

namespace EasySpreadsheet
{
	public static class EsGUIStyle
	{
		private static GUISkin skin;
		public static GUIStyle label { get; private set; }
		public static GUIStyle boldLabel { get; private set; }
		public static GUIStyle smallLabel { get; private set; }
		public static GUIStyle largeLabel { get; private set; }
		public static GUIStyle textField { get; private set; }
		public static GUIStyle textFieldPath { get; private set; }
		public static GUIStyle textFieldCell { get; private set; }
		public static GUIStyle textArea { get; private set; }
		public static GUIStyle toggle { get; private set; }
		public static GUIStyle button { get; private set; }
		public static GUIStyle helpBox { get; private set; }
		public static GUIStyle link { get; private set; }
		public static GUIStyle foldout { get; private set; }
		public static GUIStyle red { get; private set; }
		public static GUIStyle yellow { get; private set; }
		public static GUIStyle Box { get; private set; }

		public static void Ensure()
		{
			if (skin != null)
				return;

			const int fontSizeSmall = 11;
			const int fontSizeNormal = 11;
			const int fontSizeInput = 11;
			const int fontSizeLarge = 20;

			skin = Resources.Load<GUISkin>("EasySpreadsheetGUISkin");
			link = skin.FindStyle("Link");
			red = skin.FindStyle("red");
			yellow = skin.FindStyle("yellow");
			Box = skin.FindStyle("Box");


			label = new GUIStyle(EditorStyles.label)
			{
				fontSize = fontSizeNormal,
				wordWrap = true,
				richText = true
			};

			boldLabel = new GUIStyle(EditorStyles.label)
			{
				fontStyle = FontStyle.Bold,
				fontSize = fontSizeNormal
			};

			smallLabel = new GUIStyle(EditorStyles.label)
			{
				fontSize = fontSizeSmall,
				wordWrap = true,
				richText = true
			};

			largeLabel = new GUIStyle(EditorStyles.largeLabel)
			{
				fontStyle = FontStyle.Bold,
				fontSize = fontSizeLarge
			};

			textField = new GUIStyle(EditorStyles.textField) {fontSize = fontSizeInput};
			textFieldPath = new GUIStyle(EditorStyles.textField) {fontSize = fontSizeNormal};
			textFieldCell = new GUIStyle(EditorStyles.textField)
			{
				fontSize = fontSizeNormal,
				margin = new RectOffset(1, 1, 1, 1)
			};
			textArea = new GUIStyle(EditorStyles.textArea) {fontSize = fontSizeInput};
			helpBox = new GUIStyle(EditorStyles.helpBox) {fontSize = fontSizeSmall};
			toggle = new GUIStyle(EditorStyles.toggle) {fontSize = fontSizeNormal};
			button = new GUIStyle(EditorStyles.miniButton)
			{
				fontStyle = FontStyle.Bold,
				fontSize = fontSizeInput
			};
			foldout = new GUIStyle(EditorStyles.foldout)
			{
				fontStyle = FontStyle.Bold,
				fontSize = fontSizeInput,
				fixedWidth = 400,
				fixedHeight = 20
			};
		}

	}
}