using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public sealed class RecordTreeViewItem : TreeViewItem
    {
        public readonly Record data;

        public RecordTreeViewItem(Record record, int depth) : base(record.name.GetHashCode(), depth)
        {
            if (record.name.StartsWith("Assets/"))
            {
                displayName = record.name;
                icon = AssetDatabase.GetCachedIcon(displayName) as Texture2D;
            }
            else
            {
                displayName = record.name;
            }

            data = record;
        }
    }

    public class RecordTreeView : TreeView
    {
        private readonly SortOption[] m_SortOptions =
        {
            SortOption.Asset, SortOption.Elapsed, SortOption.Size, SortOption.Load, SortOption.Unload, SortOption.Loads,
            SortOption.Unloads,
            SortOption.Reference
        };

        private readonly List<TreeViewItem> result = new List<TreeViewItem>();

        private List<Record> _records = new List<Record>();

        internal RecordTreeView(TreeViewState state, MultiColumnHeaderState headerState) : base(state,
            new MultiColumnHeader(headerState))
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
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
                    minWidth = 120,
                    width = 240,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Time(MS)/Frames"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Load Scene"),
                    minWidth = 64,
                    width = 128,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Unload Scene"),
                    minWidth = 64,
                    width = 128,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Loads"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Unloads"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("References"),
                    minWidth = 32,
                    width = 50,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = true,
                    autoResize = true
                }
            };
            return retVal;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
                SetSelection(Array.Empty<int>(), TreeViewSelectionOptions.FireSelectionChanged);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = (List<TreeViewItem>) base.BuildRows(root);
            if (!string.IsNullOrEmpty(searchString))
            {
                result.Clear();
                var stack = new Stack<TreeViewItem>();
                foreach (var element in root.children) stack.Push(element);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (current.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        result.Add(current as RecordTreeViewItem);
                    if (current.children == null || current.children.Count <= 0) continue;
                    foreach (var element in current.children)
                        stack.Push(element);
                }

                rows = result;
            }

            SortIfNeeded(root, rows);
            return rows;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var record in _records)
            {
                var item = new RecordTreeViewItem(record, 0);
                foreach (var child in record.children)
                {
                    var get = Recorder.Get(child);
                    if (get == null) continue;
                    item.AddChild(new RecordTreeViewItem(get, item.depth + 1));
                }

                root.AddChild(item);
            }

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var item = (RecordTreeViewItem) args.item;
                if (item?.data == null)
                    using (new EditorGUI.DisabledScope(true))
                    {
                        base.RowGUI(args);
                    }
                else
                    CellGUI(args.GetCellRect(i), (RecordTreeViewItem) args.item, args.GetColumn(i), ref args);
            }
        }

        private static ulong SizeOf(Record loadable)
        {
            return loadable.size;
        }

        private void CellGUI(Rect cellRect, RecordTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case 0:
                    cellRect.xMin += GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                    var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                    if (item.icon != null) GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);

                    var content = item.displayName;
                    DefaultGUI.Label(
                        new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width,
                            cellRect.height),
                        content,
                        args.selected,
                        args.focused);

                    break;
                case 1:
                    if (item.data != null)
                        DefaultGUI.Label(cellRect, $"{item.data.elapsed:F2}/{item.data.frames}", args.selected,
                            args.focused);

                    break;
                case 2:
                    DefaultGUI.Label(cellRect, Utility.FormatBytes(item.data.size), args.selected,
                        args.focused);
                    break;
                case 3:
                    DefaultGUI.Label(cellRect, item.data.loadScene, args.selected, args.focused);
                    break;
                case 4:
                    DefaultGUI.Label(cellRect, item.data.unloadScene, args.selected, args.focused);
                    break;
                case 5:
                    DefaultGUI.Label(cellRect, item.data.loads.ToString(), args.selected, args.focused);
                    break;
                case 6:
                    DefaultGUI.Label(cellRect, item.data.unloads.ToString(), args.selected, args.focused);
                    break;
                case 7:
                    DefaultGUI.Label(cellRect, item.data.refCount.ToString(), args.selected, args.focused);
                    break;
            }
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

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null) return;

            var selectedObjects = new List<Object>();
            foreach (var id in selectedIds)
            {
                var assetItem = FindItem(id, rootItem);
                if (assetItem != null && assetItem.displayName.StartsWith("Assets/"))
                {
                    var o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.displayName);
                    selectedObjects.Add(o);
                    Selection.activeObject = o;
                }
            }

            Selection.objects = selectedObjects.ToArray();
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return true;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            if (DragAndDrop.paths.Length == 0) return false;

            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            return DragAndDropVisualMode.Rejected;
        }

        private void OnSortingChanged(MultiColumnHeader header)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        private void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1) return;

            if (multiColumnHeader.sortedColumnIndex == -1) return;

            SortByColumn();

            rows.Clear();

            foreach (var t in root.children)
            {
                rows.Add(t);
                if (!t.hasChildren || t.children[0] == null || !IsExpanded(t.id)) continue;

                foreach (var child in t.children) rows.Add(child);
            }

            Repaint();
        }

        private void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0) return;

            var assetList = new List<TreeViewItem>();
            foreach (var item in rootItem.children) assetList.Add(item);

            var orderedItems = InitialOrder(assetList, sortedColumns);
            rootItem.children = orderedItems.ToList();
        }

        private IEnumerable<TreeViewItem> InitialOrder(IEnumerable<TreeViewItem> myTypes, int[] columnList)
        {
            var sortOption = m_SortOptions[columnList[0]];
            var ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
                case SortOption.Asset:
                    return myTypes.Order(l => l.displayName, ascending);
                case SortOption.Elapsed:
                    return myTypes.Order(l => ((RecordTreeViewItem) l).data.elapsed, ascending);
                case SortOption.Size:
                    return myTypes.Order(l => SizeOf(((RecordTreeViewItem) l).data), ascending);
                case SortOption.Load:
                    return myTypes.Order(l => ((RecordTreeViewItem) l).data.loadScene, ascending);
                case SortOption.Unload:
                    return myTypes.Order(l => ((RecordTreeViewItem) l).data.unloadScene, ascending);
                case SortOption.Loads:
                    return myTypes.Order(l => ((RecordTreeViewItem) l).data.loads, ascending);
                case SortOption.Unloads:
                    return myTypes.Order(l => ((RecordTreeViewItem) l).data.unloads, ascending);
                case SortOption.Reference:
                    return myTypes.Order(l => ((RecordTreeViewItem) l).data.refCount, ascending);
            }

            return myTypes.Order(l => new FileInfo(l.displayName).Length, ascending);
        }

        public void SetAssets(List<Record> records)
        {
            _records = records;
            Reload();
        }

        private enum SortOption
        {
            Asset,
            Elapsed,
            Size,
            Load,
            Unload,
            Loads,
            Unloads,
            Reference
        }
    }
}