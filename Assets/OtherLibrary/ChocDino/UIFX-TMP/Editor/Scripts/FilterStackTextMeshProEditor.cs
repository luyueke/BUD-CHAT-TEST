//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//
#if UIFX_TMPRO

// In Unity 2020.2 reorderable lists became default for arrays and lists
#if UNITY_2020_2_OR_NEWER
	#define UNITY_REORDERABLE_LISTS
#endif
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using ChocDino.UIFX;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(FilterStackTextMeshPro), true)]
	[CanEditMultipleObjects]
	internal class FilterStackTextMeshProEditor : BaseEditor
	{
		#if !UNITY_REORDERABLE_LISTS
		private ReorderableList list;

		static GUIContent Content_Filters = new GUIContent("Filters");

		private SerializedProperty _propApplyToSprites;
		private SerializedProperty _propRelativeToTransformScale;
		private SerializedProperty _propRelativeFontSize;
		#endif

		static GUIContent Content_Add = new GUIContent("Add");

		private SerializedProperty _propFilters;

		private List<System.Type> _filterTypes;
		private GUIContent[] _filterTypesNames;
		private int _selectedTypeIndex;

		void OnEnable()
		{
			_propFilters = VerifyFindProperty("_filters");

			_filterTypes = new List<System.Type>(FindSubClassesOf<FilterBase>());
			if (_filterTypes != null && _filterTypes.Count > 0)
			{
				_filterTypesNames = new GUIContent[_filterTypes.Count];
				for (int i = 0; i < _filterTypes.Count; i++)
				{
					_filterTypesNames[i] = new GUIContent(GetDisplayNameForComponentType(_filterTypes[i]));
				}
			}

			#if !UNITY_REORDERABLE_LISTS
			_propApplyToSprites = VerifyFindProperty("_applyToSprites");
			_propRelativeToTransformScale = VerifyFindProperty("_relativeToTransformScale");
			_propRelativeFontSize = VerifyFindProperty("_relativeFontSize");
			list = new ReorderableList(serializedObject, _propFilters, true, false, true, true)
			{
				drawHeaderCallback = DrawListHeader,
				drawElementCallback = DrawListElement
			};
			#endif
		}

		void OnDisable()
		{
			_filterTypes = null;
			_filterTypesNames = null;
			_selectedTypeIndex = 0;
		}

		#if !UNITY_REORDERABLE_LISTS
		private void DrawListHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, Content_Filters);
		}

		private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			SerializedProperty prop = list.serializedProperty.GetArrayElementAtIndex(index);
	
			// By default, the array dropdown is offset to the left which intersects with
			// the drag handle, so we can either indent the array property or inset the rect.
			//EditorGUI.indentLevel++;
	
			// Take note of the last argument, since this is an array,
			// we want to draw it with all of its children.
			EditorGUI.PropertyField(rect, prop, new GUIContent("Filter " + index), includeChildren: false);
			//EditorGUI.indentLevel--;
		}
		#endif

		public override void OnInspectorGUI()
		{
			#if UNITY_REORDERABLE_LISTS
			{
				base.DrawDefaultInspector();
			}
			#else
			{
				serializedObject.Update();
				EditorGUILayout.PropertyField(_propApplyToSprites);
				EditorGUILayout.PropertyField(_propRelativeToTransformScale);
				EditorGUILayout.PropertyField(_propRelativeFontSize);
				list.DoLayoutList();
				serializedObject.ApplyModifiedProperties();
			}
			#endif

			serializedObject.Update();

			if (_filterTypesNames != null)
			{
				EditorGUILayout.BeginHorizontal();
				_selectedTypeIndex = EditorGUILayout.Popup(_selectedTypeIndex, _filterTypesNames);
				if (GUILayout.Button(Content_Add))
				{
					foreach (var obj in this.targets)
					{
						var filterStack = obj as FilterStackTextMeshPro;
						var component = ObjectFactory.AddComponent(filterStack.gameObject, _filterTypes[_selectedTypeIndex]);
						
						// Find the last null child, or add a new one
						int indexToInsert = -1;
						if (_propFilters.arraySize > 0)
						{
							var lastChildProp = _propFilters.GetArrayElementAtIndex(_propFilters.arraySize - 1);
							if (lastChildProp.objectReferenceValue == null)
							{
								indexToInsert = _propFilters.arraySize - 1;
							}
						}
						if (indexToInsert < 0)
						{
							_propFilters.InsertArrayElementAtIndex(_propFilters.arraySize);
							indexToInsert = _propFilters.arraySize - 1;
						}

						_propFilters.GetArrayElementAtIndex(indexToInsert).objectReferenceValue = component;
					}
				}
				EditorGUILayout.EndHorizontal();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private static IEnumerable<System.Type> FindSubClassesOf<TBaseType>()
		{
			var baseType = typeof(TBaseType);
			var assembly = baseType.Assembly;

			return assembly.GetTypes().Where(t => t.IsSubclassOf(baseType));
		}

		private static string GetDisplayNameForComponentType(System.Type type)
		{
			string title = null;
			
			// Use the AddComponentMenu attribute to get the nice name for the component
			var attr = type.GetCustomAttributes(typeof(AddComponentMenu), false).FirstOrDefault() as AddComponentMenu;
			if (attr != null)
			{
				title = attr.componentMenu?.Trim();
				if (!string.IsNullOrEmpty(title))
				{
					var lastPathCharIndex = title.LastIndexOf('/');
					if (lastPathCharIndex >= 0)
					{
						if (lastPathCharIndex < title.Length - 1)
						{
							title = title.Substring(lastPathCharIndex + 1);
						}
					}
				}
			}
			// Fallback to just use the type name
			if (string.IsNullOrEmpty(title))
			{
				title = type.Name;
			}
			return title;
		}
	}
}
#endif