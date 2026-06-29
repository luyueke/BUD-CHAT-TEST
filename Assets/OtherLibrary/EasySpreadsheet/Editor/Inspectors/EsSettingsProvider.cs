using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasySpreadsheet
{
	public class EsSettingsProvider : SettingsProvider
	{
		private static SerializedObject _serializedObject;

		private SerializedProperty excelFilesPath;
		private SerializedProperty generatedAssetPath;
		private SerializedProperty generatedScriptPath;
		private SerializedProperty generatedEditorScriptPath;
		private SerializedProperty sheetDataPostfix;
		private SerializedProperty rowDataPostfix;
		private SerializedProperty nameSpace;
		private SerializedProperty visualScripting;
		private SerializedProperty writable;
		private SerializedProperty saveTypeData;
		public EsSettingsProvider() : base("Project/Easy Spreadsheet", SettingsScope.Project)
		{
		}


		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			EsSettings.Instance.Save();
			var setting = EsSettings.Instance;
			if (_serializedObject == null)
				_serializedObject = new SerializedObject(setting);
			excelFilesPath = _serializedObject.FindProperty("excelFilesPath");
			generatedAssetPath = _serializedObject.FindProperty("generatedAssetPath");
			generatedScriptPath = _serializedObject.FindProperty("generatedScriptPath");
			generatedEditorScriptPath = _serializedObject.FindProperty("generatedEditorScriptPath");
			sheetDataPostfix = _serializedObject.FindProperty("sheetClassPostfix");
			rowDataPostfix = _serializedObject.FindProperty("rowClassPostfix");
			nameSpace = _serializedObject.FindProperty("nameSpace");
			visualScripting = _serializedObject.FindProperty("visualScripting");
			writable = _serializedObject.FindProperty("writable");
			saveTypeData = _serializedObject.FindProperty("sameTypeDic");
		}

		public override void OnGUI(string searchContext)
		{
			using (CreateSettingsWindowGUIScope())
			{
				var setting = EsSettings.Instance;
				if (_serializedObject == null || !_serializedObject.targetObject)
				{
					_serializedObject = new SerializedObject(setting);
				}

				_serializedObject.Update();
				EditorGUI.BeginChangeCheck();
				
				EditorGUILayout.PropertyField(excelFilesPath);
				EditorGUILayout.PropertyField(generatedScriptPath);
				EditorGUILayout.PropertyField(generatedEditorScriptPath);
				EditorGUILayout.PropertyField(generatedAssetPath);
				EditorGUILayout.PropertyField(sheetDataPostfix);
				EditorGUILayout.PropertyField(rowDataPostfix);
				EditorGUILayout.PropertyField(nameSpace);
				EditorGUILayout.PropertyField(writable);
				EditorGUILayout.PropertyField(visualScripting);
				EditorGUILayout.PropertyField(saveTypeData);
				
				if (EditorGUI.EndChangeCheck())
				{
					_serializedObject.ApplyModifiedProperties();
					EsCaches.Clear();
					EsSettings.Instance.Save();
				}
			}
		}

		private IDisposable CreateSettingsWindowGUIScope()
		{
			var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
			var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
			return Activator.CreateInstance(type) as IDisposable;
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();
			EsSettings.Instance.Save();
			_serializedObject = null;
		}

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			if (EsSettings.Instance)
			{
				var provider = new EsSettingsProvider
				{
					keywords = GetSearchKeywordsFromSerializedObject(_serializedObject = _serializedObject ?? new SerializedObject(EsSettings.Instance))
				};
				return provider;
			}

			return null;
		}
	}
}