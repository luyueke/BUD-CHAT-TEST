using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Reflection;
using System;
using UnityEditor.AssetImporters;

public class DecoratorEditor<T> : Editor where T : UnityEngine.Object
{
    protected T _target;
    protected UnityEditor.Editor _nativeEditor;
    private static Type _inspectorEditorType = null;

    public virtual void OnEnable()
    {
        _target = target as T;

        if (_inspectorEditorType == null)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var tagType = assembly.GetType("UnityEditor." + typeof(T).Name + "Inspector")
                    ?? assembly.GetType("UnityEditor." + typeof(T).Name + "Editor");
                if (tagType != null)
                {
                    _inspectorEditorType = tagType;
                    break;
                }
            }
        }

        if (_inspectorEditorType != null)
        {
            _nativeEditor = UnityEditor.Editor.CreateEditor(serializedObject.targetObjects, _inspectorEditorType);
        
        }
        else
        {
            _nativeEditor = UnityEditor.Editor.CreateEditor(serializedObject.targetObjects);
        }
    }

    public virtual void OnDisable()
    {
        _nativeEditor = null;
    }

    public override void OnInspectorGUI()
    {
        if (_nativeEditor)
        {
            _nativeEditor.OnInspectorGUI();
        }
    }
}

#if UNITY_2019_1_OR_NEWER
[CustomEditor(typeof(TextureImporter))]
[CanEditMultipleObjects]
public class TextureImporterCustomEditor : DecoratorEditor<TextureImporter>
{
    [MenuItem("BudTools/AssetsQualitySetting")]
    public static void OpenQualitys()
    {
        EditorWindow.GetWindow<TextureQualityWindow>(false, "Qualitys");
    }

    private static readonly BindingFlags MethodFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;

    private bool _changed = false;
    private MethodInfo _apply = null;
    private MethodInfo _resetValues = null;
    private int c1Size;

    #region Mono Funcs
    public override void OnEnable()
    {
        base.OnEnable();
        _apply = _nativeEditor.GetType().GetMethod("Apply", MethodFlag);
        _resetValues = _nativeEditor.GetType().GetMethod("ResetValues", MethodFlag);
        ResetLowSize();
    }

    private void ResetLowSize()
    {
        var c1 = _target.GetPlatformTextureSettings("Custom");
        c1Size = c1.overridden == false ? 0 : c1.maxTextureSize;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (EditorUtility.IsDirty(target))
        {
            _changed = true;
        }
        if (_target.textureType == TextureImporterType.Default)
        {
            TextureSize s1 = (TextureSize)EditorGUILayout.EnumPopup(new GUIContent("低模尺寸"), (TextureSize)c1Size);
            if (c1Size != (int)s1)
            {
                if ((int)s1 == 0) _target.ClearPlatformTextureSettings("Custom");
                else
                {
                    var s = _target.GetPlatformTextureSettings("Custom");
                    s.maxTextureSize = (int)s1;
                    s.overridden = true;
                    _target.SetPlatformTextureSettings(s);
                }
                c1Size = (int)s1;
            }
        }

        EditorGUI.BeginDisabledGroup(!_changed);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Revert", GUILayout.MaxWidth(50.0f)))
        {
            RevertChanges();
        }
        if (GUILayout.Button("Apply", GUILayout.MaxWidth(50.0f)))
        {
            ApplyChanges();
        }
        GUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        
        var refDependencies = PicCheckWindow.GetRefDependencies(_target.assetPath);
        if (refDependencies != null && refDependencies.Count > 0)
        {
            EditorGUI.BeginDisabledGroup(true);
            foreach (string str in refDependencies)
            {
                GUILayout.Label(str);
            }
            EditorGUI.EndDisabledGroup();
        }



    }

    public override void OnDisable()
    {
        if (_changed)
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Inspector-Unsaved Changes Detected", $"Unapplied import settings for {AssetDatabase.GetAssetPath(target)}", "Save", "Discard"))
            {
                ApplyChanges();
            }
            else
            {
                RevertChanges(true);
            }
        }
        base.OnDisable();
    }
    #endregion

    #region Main Funcs
    protected void ApplyChanges()
    {
        if (_changed)
        {
            _changed = false;
            if (_apply != null)
            {
                _apply.Invoke(_nativeEditor, null);
            }
            foreach (TextureImporter t in targets)
            {
                EditorUtility.SetDirty(t);
                t.SaveAndReimport();
            }
        }
    }
    protected void RevertChanges(bool unselectSelf = false)
    {
        if (_changed)
        {
            _changed = false;
            _resetValues.Invoke(_nativeEditor, null);
        }
        foreach (TextureImporter t in targets)
        {
            EditorUtility.ClearDirty(t);
        }
        ResetLowSize();
        if (unselectSelf)
        {
            Selection.activeObject = null;
        }
    }
    #endregion
}
#endif