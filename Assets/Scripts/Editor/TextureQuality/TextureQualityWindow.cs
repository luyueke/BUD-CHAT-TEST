using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using xasset;
using xasset.editor;

public class TextureQualityWindow : EditorWindow
{
    private const int k_SearchHeight = 20;
    [SerializeField] private MultiColumnHeaderState multiColumnHeaderState;
    [SerializeField] private TreeViewState treeViewState;
    public bool reloadSelectedNow;

    private readonly GUIContent folderPath = new GUIContent("模块");
    private readonly GUIContent highSizeTitle = new GUIContent("HighSize");
    private readonly GUIContent lowSizeTitle = new GUIContent("LowSize");

    private TextureQualityData _qualitys;

    private bool reloadAssets;
    private bool reloadNow = true;

    private int selected;

    private QualityTreeView treeView;
    private VerticalSplitter verticalSplitter;

    private void OnEnable()
    {
        reloadNow = true;
    }

    private void OnGUI()
    {
        if (reloadNow)
        {
            Reload();
            reloadNow = false;
            reloadSelectedNow = true;
        }
        if (reloadSelectedNow && _qualitys.qualitySettings.Count > 0)
        {
            var setting = _qualitys.qualitySettings[selected];
            setting.specialSettings.Clear();
            var assets = AssetDatabase.FindAssets("t:Texture", new string[] { setting.path });
            if (assets != null && assets.Length > 0)
            {
                for (int i = 0, L = assets.Length; i < L; i++)
                {
                    var importerPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                    var importer = AssetImporter.GetAtPath(importerPath);
                    if (importer is TextureImporter)
                    {
                        var textureImporter = importer as TextureImporter;
                        if (textureImporter.textureType == TextureImporterType.Default)
                        {
                            setting.specialSettings.Add(AssetDatabase.LoadAssetAtPath<Texture>(importerPath));
                        }
                    }
                }
            }
            reloadAssets = true;
        }

        if (reloadAssets)
            if (treeView != null)
            {
                treeView.data = _qualitys.qualitySettings[selected];
                treeView.Reload();
                reloadAssets = false;
            }


        if (treeView == null)
        {
            if (treeViewState == null) treeViewState = new TreeViewState();

            var headerState = QualityTreeView.CreateDefaultMultiColumnHeaderState();
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);

            multiColumnHeaderState = headerState;
            treeView = new QualityTreeView(treeViewState, multiColumnHeaderState);
            treeView.data = _qualitys.qualitySettings[selected];
            treeView.Reload();
        }

        if (verticalSplitter == null) verticalSplitter = new VerticalSplitter();

        var rect = new Rect(0, 0, position.width, position.height);
        DrawTree(rect);
        DrawToolbar(new Rect(rect.xMin, rect.yMin, rect.width, k_SearchHeight));
    }


    private void Reload()
    {
        _qualitys = AssetDatabase.LoadAssetAtPath<TextureQualityData>("Assets/Scripts/Editor/TextureQuality/Quality.asset");
        if (_qualitys == null)
        {
            _qualitys = ScriptableObject.CreateInstance<TextureQualityData>();
            AssetDatabase.CreateAsset(_qualitys, $"Assets/Scripts/Editor/TextureQuality/Quality.asset");
            AssetDatabase.Refresh();
        }
    }


    private void DrawManifest()
    {
        if (_qualitys.qualitySettings.Count == 0) return;
        folderPath.text = _qualitys.qualitySettings[selected].path;
        var rect = GUILayoutUtility.GetRect(folderPath, EditorStyles.toolbarDropDown);
        if (EditorGUI.DropdownButton(rect, folderPath, FocusType.Keyboard, EditorStyles.toolbarDropDown))
        {
            var menu = new GenericMenu();
            menu.allowDuplicateNames = false;
            for (var index = 0; index < _qualitys.qualitySettings.Count; index++)
            {
                var version = _qualitys.qualitySettings[index];
                menu.AddItem(new GUIContent(version.path.Replace("Assets/", "")), selected == index,
                    data =>
                    {
                        selected = (int)data;
                        reloadSelectedNow = true;
                    }, index);
            }

            menu.DropDown(rect);
        }

        _qualitys.qualitySettings[selected].highSize = (TextureSize)EditorGUILayout.EnumPopup(highSizeTitle, _qualitys.qualitySettings[selected].highSize);
        _qualitys.qualitySettings[selected].lowSize = (TextureSize)EditorGUILayout.EnumPopup(lowSizeTitle, _qualitys.qualitySettings[selected].lowSize);
    }

    public void Save()
    {
        EditorUtility.SetDirty(_qualitys);
        AssetDatabase.SaveAssetIfDirty(_qualitys);
        AssetDatabase.Refresh();
    }

    public void Refresh()
    {
        reloadSelectedNow = true;
    }

    private void DrawToolbar(Rect toolbarPos)
    {
        GUILayout.BeginArea(new Rect(0, 0, toolbarPos.width, k_SearchHeight * 2));
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            DrawManifest();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) Refresh();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) Save();
            GUILayout.FlexibleSpace();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawTree(Rect rect)
    {
        const int toolbarHeight = k_SearchHeight + 4;
        var treeRect = new Rect(
            rect.xMin,
            rect.yMin + toolbarHeight,
            rect.width,
            verticalSplitter.rect.y - toolbarHeight);

        treeView.OnGUI(treeRect);
        verticalSplitter.OnGUI(rect);
        if (verticalSplitter.resizing) Repaint();
    }
}
