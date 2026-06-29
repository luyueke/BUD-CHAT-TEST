using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class QualityTreeViewItem : TreeViewItem
{
    public string path;
    public int highSize;
    public int lowSize;

    public QualityTreeViewItem(string asset, int depth) : base(asset.GetHashCode(), depth)
    {
        displayName = asset;
        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(displayName);
    }
}

public class QualityTreeView : TreeView
{
    public QualitySetting data;
    public TextureQualityWindow _window;
    
    internal QualityTreeView(TreeViewState state, MultiColumnHeaderState headerState) : base(state, new MultiColumnHeader(headerState))
    {
        showBorder = true;
        showAlternatingRowBackgrounds = true;
        multiColumnHeader.sortingChanged += OnSortingChanged;
        multiColumnHeader.ResizeToFit();
    }

    private void OnSortingChanged(MultiColumnHeader header)
    {
        //SortIfNeeded(rootItem, GetRows());
    }

    protected override TreeViewItem BuildRoot()
    {
        var root = new TreeViewItem(-1, -1);

        for (int j = 0, C1 = data.specialSettings.Count; j < C1; j++)
        {
            var obj = data.specialSettings[j];
            var objPath = AssetDatabase.GetAssetPath(obj);
            TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(objPath);
            var custom = ti.GetPlatformTextureSettings("Custom");
            ti.GetSourceTextureWidthAndHeight(out int width, out int height);
            var node = new QualityTreeViewItem(objPath, 0)
            {
                path = objPath,
                highSize = width,
                lowSize = custom.overridden == true ? custom.maxTextureSize : 0,
            };
            root.AddChild(node);
        }

        return root;
    }

    internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
    {
        return new MultiColumnHeaderState(GetColumns());
    }

    private static MultiColumnHeaderState.Column[] GetColumns()
    {
        var retVal = new[]
        {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    minWidth = 320,
                    width = 480,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("SourceSize"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("LowSize"),
                    minWidth = 64,
                    width = 96,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true
                }
            };
        return retVal;
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        for (var i = 0; i < args.GetNumVisibleColumns(); ++i) CellGUI(args.GetCellRect(i), (QualityTreeViewItem)args.item, args.GetColumn(i), ref args);
    }

    private void CellGUI(Rect cellRect, QualityTreeViewItem item, int column, ref RowGUIArgs args)
    {
        CenterRectUsingSingleLineHeight(ref cellRect);

        switch (column)
        {
            case 0:
                cellRect.xMin += GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                if (item.icon != null) GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);

                var content = item.displayName.Replace(data.path + "/", "");
                DefaultGUI.Label(
                    new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width,
                        cellRect.height),
                    content,
                    args.selected,
                    args.focused);
                break;
            case 1:
                DefaultGUI.Label(cellRect, item.highSize == 0 ? "-" : item.highSize.ToString(), args.selected, args.focused);
                break;
            case 2:
                DefaultGUI.Label(cellRect, item.lowSize == 0 ? "-" : item.lowSize.ToString(), args.selected, args.focused);
                break;
        }
    }

    protected override void SingleClickedItem(int id)
    {
        base.ContextClickedItem(id);
        var selection = GetSelection();
        var items = Array.ConvertAll(selection.ToArray(), o => FindItem(o, rootItem));
        var o = AssetDatabase.LoadAssetAtPath<Object>(items[0].displayName);
        Selection.activeObject = o;
        //_window.ReloadDependencies(Array.ConvertAll(items, input => input.displayName));
    }

    protected override void DoubleClickedItem(int id)
    {
        var assetItem = FindItem(id, rootItem);
        if (assetItem != null)
        {
            var o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.displayName);
            EditorGUIUtility.PingObject(o);
            Selection.activeObject = o;
        }
    }
}