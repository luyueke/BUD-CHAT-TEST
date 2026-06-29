//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using UnityEditor; 

namespace ChocDino.UIFX.Editor
{
	internal class FilterBaseEditor : BaseEditor
	{
		protected static readonly string ComponentColorAdjustUrl = "https://www.chocdino.com/products/uifx/bundle/components/color-adjust-filter/";
		protected static readonly string DiscordUrl = "https://discord.gg/wKRzKAHVUE";
		protected static readonly string ForumBundleUrl = "https://forum.unity.com/threads/released-uifx-advanced-effects-for-unity-ui.1549502/";
		protected static readonly string GithubUrl = "https://github.com/Chocolate-Dinosaur/UIFX/issues";
		protected static readonly string SupportEmailUrl = "mailto:support@chocdino.com";
		protected static readonly string AssetStoreBundleReviewUrl = "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews";

		protected static GUIContent Content_R = new GUIContent("R");
		private static GUIContent Content_Debug = new GUIContent("Debug");
		private static GUIContent Content_Strength = new GUIContent("Strength");
		private static GUIContent Content_ConvertToImage = new GUIContent("Convert To Image");
		
		#if !UIFX_FILTER_HIDE_INSPECTOR_PREVIEW

		private string GetFilterShortName()
		{
			return this.target.GetType().Name;
		}

		public override GUIContent GetPreviewTitle()
		{
			return new GUIContent("UIFX - Filters");
		}

		public override bool HasPreviewGUI()
		{
			// We can't support multiple targets because the below logic to only show the last item doesn't work
			if (targets.Length > 1) return false;

			var filter = target as FilterBase;
			if (!filter.isActiveAndEnabled) return false;

			// Only allow the last ENABLED filter in the stack to preview
			var filters = filter.gameObject.GetComponents<FilterBase>();
			FilterBase lastEnabledFilter = null;
			for (int i = 0; i < filters.Length; i++)
			{
				if (filters[i].isActiveAndEnabled && filters[i].IsFilterEnabled())
				{
					lastEnabledFilter = filters[i];
				}
			}
			return (lastEnabledFilter == filter);
		}

		public override string GetInfoString()
		{
			var filter = target as FilterBase;
			return filter.GetDebugString();
		}

		public override void OnPreviewSettings()
		{
			if (GUILayout.Button("Save to PNG"))
			{
				foreach (FilterBase filter in this.targets)
				{
					System.IO.Directory.CreateDirectory(Application.dataPath + "/../Captures/");
					string path = System.IO.Path.GetFullPath(Application.dataPath + string.Format("/../Captures/Image-{0}-{1}.png", filter.gameObject.name, GetFilterShortName()));
					if (filter.SaveToPNG(path))
					{
						Debug.Log("Saving image to: <b>" + path  + "</b>");
					}
					else
					{
						Debug.LogError("Failed to save image to: <b>" + path  + "</b>");
					}
				}
			}
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (Event.current.type == EventType.Repaint)
			{
				var filter = target as FilterBase;
				EditorGUI.DrawTextureTransparent(r, Texture2D.blackTexture, ScaleMode.StretchToFill);
				var texture = filter.ResolveToTexture();
				if (texture)
				{
					GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
				}
			}
		}
		#endif

		private static void DrawTexture(Texture texture, bool showAlphaBlended)
		{
			if (texture)
			{
				GUILayout.Label(string.Format("{0} {1}x{2}:{3}", texture.name, texture.width, texture.height, texture.updateCount));

				if (showAlphaBlended)
				{
					{
						Rect r = GUILayoutUtility.GetRect(256f, 256f);
						EditorGUI.DrawTextureTransparent(r, Texture2D.blackTexture, ScaleMode.StretchToFill);
						GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
					}
				}
				else
				{
					GUILayout.BeginHorizontal();
					{
						float aspect = (float)texture.width / (float)texture.height;
						Rect r = GUILayoutUtility.GetAspectRect(aspect * 2f, GUILayout.ExpandWidth(true));
						Rect rc = r;
						rc.width /= 2;
						GUI.DrawTexture(rc, texture, ScaleMode.ScaleToFit, false);
						rc.x += rc.width;
						EditorGUI.DrawTextureAlpha(rc, texture, ScaleMode.ScaleToFit);
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Select"))
				{
					UnityEditor.Selection.activeObject = texture;
				}
				if (GUILayout.Button("Save"))
				{
					SaveTexture(texture as RenderTexture);
				}
				GUILayout.EndHorizontal();
			}
		}

		#if UIFX_BETA

		private bool CanBakefiltersToImage(FilterBase filter)
		{
			var graphic = filter.GetComponent<Graphic>();
			if (!graphic) { return false; }

			var canvas = graphic.canvas;
			if (!canvas) { return false; }
			if (canvas.renderMode == RenderMode.WorldSpace) { return false; }

		 	var xform = filter.GetComponent<RectTransform>();
			if (!xform) { return false; }

			if (xform.localRotation != Quaternion.identity) { return false;}
			if (xform.localScale != Vector3.one) { return false;}

			return true;
		}

		private bool BakeFiltersToImage(FilterBase filter)
		{
			Vector2 anchorOffset = Vector2.zero;
			{
				var graphic2 = filter.GetComponent<Graphic>();
			 	var xform2 = graphic2.GetComponent<RectTransform>();
				if (graphic2)
				{
					Rect r = filter.GetLocalRect();
					anchorOffset.x = r.x + r.width * xform2.pivot.x + xform2.anchoredPosition.x;
					anchorOffset.y = r.y + r.height * xform2.pivot.y + xform2.anchoredPosition.y;
				}
			}

			// Bake to new texture asset
			var uniqueFileName = AssetDatabase.GenerateUniqueAssetPath("Assets/Baked-" + filter.gameObject.name + ".png");
			{
				var fullPath = System.IO.Path.GetFullPath(Application.dataPath + "/../" + uniqueFileName);
				AssetDatabase.StartAssetEditing();
				filter.SaveToPNG(fullPath);
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
			}

			// Modify importer settings for new texture asset
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(uniqueFileName);
			if (importer)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.npotScale = TextureImporterNPOTScale.None;
				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;

				TextureImporterSettings settings = new TextureImporterSettings();
				importer.ReadTextureSettings(settings); 
				settings.spriteMeshType = SpriteMeshType.FullRect;
				importer.SetTextureSettings(settings);

				EditorUtility.SetDirty(importer);
				importer.SaveAndReimport();
				AssetDatabase.WriteImportSettingsIfDirty(uniqueFileName);
			}

			// Get the asset
			var textureAsset = (Texture2D)AssetDatabase.LoadAssetAtPath(uniqueFileName, typeof(Texture2D));
			Sprite spriteAsset = null;
			{
				Object[] objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(uniqueFileName);
				if (objs != null)
				{
					foreach (Object obj in objs)
					{
						if (obj.GetType() == typeof(Sprite))
						{
							spriteAsset = obj as Sprite;
							break;
						}
					}
				}
			}

			Component[] components = filter.gameObject.GetComponents<Component>();
			
			int oldGraphicIndex = 0;
			int filterComponentIndex = 0;
			bool isRaycastTarget = false;
			// Remove the previous Graphic
			var graphic = filter.GetComponent<Graphic>();
			if (graphic)
			{
				isRaycastTarget = graphic.raycastTarget;
				oldGraphicIndex = System.Array.IndexOf(components, graphic);
				filterComponentIndex = System.Array.IndexOf(components, filter);
				Undo.DestroyObjectImmediate(graphic);
			}

			// Add RawImage component and assign texture
			var image = Undo.AddComponent<RawImage>(filter.gameObject);
			//image.sprite = spriteAsset;
			image.texture = textureAsset;
			image.raycastTarget = isRaycastTarget;
			//image.preserveAspect = true;
			image.maskable = false;
			float scale = image.canvas.scaleFactor;

			// Move RawImage component to order position of old Graphic
			int newGraphicIndex = (components.Length - 1);
			int moveDelta = Mathf.Abs(oldGraphicIndex - newGraphicIndex);
			for (int i = 0; i < moveDelta; i++)
			{
				if (newGraphicIndex > oldGraphicIndex) { UnityEditorInternal.ComponentUtility.MoveComponentUp(image); }
				else { UnityEditorInternal.ComponentUtility.MoveComponentDown(image); }
			}

			// Change the RectTransform
			var xform = image.rectTransform;
			xform.sizeDelta = new Vector2(textureAsset.width / scale, textureAsset.height / scale);
			xform.anchoredPosition = anchorOffset;

			// Remove all FilterBase and IMeshModifiers that affect this render
			int componentIndex = 0;
			foreach (var component in components)
			{
				if (componentIndex <= filterComponentIndex)
				{
					if (component is FilterBase || component is IMeshModifier)
					{
						Undo.DestroyObjectImmediate(component);
					}
				}
				componentIndex++;
			}

			//Undo.DestroyObjectImmediate(filter);

			return true;
		}

		#endif

		internal bool OnInspectorGUI_Baking(FilterBase filter)
		{
			bool result = false;
			
			#if UIFX_BETA
			EditorGUILayout.Space();
			bool wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled & CanBakefiltersToImage(filter);
			if (GUILayout.Button(Content_ConvertToImage))
			{
				result = BakeFiltersToImage(filter);
			}
			GUI.enabled = wasEnabled;
			#endif

			return result;
		}
		
		internal static void OnInspectorGUI_Debug(FilterBase filter)
		{
#if UIFX_FILTER_DEBUG
			{
				EditorGUILayout.Space();
				GUILayout.Label(Content_Debug, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EditorGUILayout.TextArea(filter.GetDebugString());
				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				Texture[] textures = filter.GetDebugTextures();
				foreach (var texture in textures)
				{
					DrawTexture(texture, false);
					EditorGUILayout.Space();
				}

				if (filter.isActiveAndEnabled)
				{
					DrawTexture(filter.ResolveToTexture(), true);
					if (GUILayout.Button("Save Final"))
					{
						filter.SaveToPNG(Application.dataPath + "/../final.png");
					}
				}
			}
#endif
		}

		internal static void PropertyReset_Slider(SerializedProperty prop, GUIContent label, float min, float max, float resetValue)
		{
			GUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			float newValue = EditorGUILayout.Slider(label, prop.floatValue, min, max);
			if (EditorGUI.EndChangeCheck())
			{
				prop.floatValue = newValue;
			}
			bool wasEnabled = GUI.enabled;
			GUI.enabled = (prop.floatValue != resetValue);
			if (GUILayout.Button(Content_R, GUILayout.ExpandWidth(false)))
			{
				prop.floatValue = resetValue;
			}
			GUI.enabled = wasEnabled;
			GUILayout.EndHorizontal();
		}

		internal static void PropertyReset_Float(SerializedProperty prop, float resetValue)
		{
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(prop);
			bool wasEnabled = GUI.enabled;
			GUI.enabled = (prop.floatValue != resetValue);
			if (GUILayout.Button(Content_R, GUILayout.ExpandWidth(false)))
			{
				prop.floatValue = resetValue;
			}
			GUI.enabled = wasEnabled;
			GUILayout.EndHorizontal();
		}

		internal static bool DrawStrengthProperty(ref float value)
		{
			bool changed = false;
			const float resetValue = 1f;

			GUILayout.BeginHorizontal();
			bool isColored = false;
			if (value < 1f)
			{
				isColored = true;
				if (value > 0f)
				{
					GUI.backgroundColor = Color.yellow;
				}
				else
				{
					GUI.backgroundColor = Color.red;
				}
			}
			EditorGUILayout.PrefixLabel(Content_Strength);
			int indentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			EditorGUI.BeginChangeCheck();
			value = EditorGUILayout.Slider(value, 0f, 1f);
			if (EditorGUI.EndChangeCheck())
			{
				changed = true;
			}
			bool wasEnabled = GUI.enabled;
			GUI.enabled = (value != resetValue);
			if (GUILayout.Button(Content_R, GUILayout.ExpandWidth(false)))
			{
				value = resetValue;
				changed = true;
			}
			GUI.enabled = wasEnabled;

			if (isColored)
			{
				GUI.backgroundColor = Color.white;
			}
			
			EditorGUI.indentLevel = indentLevel;
			GUILayout.EndHorizontal();
			return changed;
		}

		internal static void DrawStrengthProperty(SerializedProperty prop)
		{
			float strength = prop.floatValue;
			if (DrawStrengthProperty(ref strength))
			{
				prop.floatValue = strength;
			}
		}

		protected static void SaveTexture(RenderTexture texture)
		{
			Texture2D rwTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
			TextureUtils.WriteToPNG(texture, rwTexture, Application.dataPath + "/../SavedScreen.png");
			ObjectHelper.Destroy(ref rwTexture);
		}
	}
}