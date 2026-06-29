using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace xasset.editor
{
    [CustomEditor(typeof(Group))]
    public class GroupEditor : Editor, IDependenciesEditor
    {
        [SerializeField] private TreeViewState treeViewState;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderState;
        [SerializeField] private TreeViewState dependenciesTreeViewState;

        private BundleMode _bundleMode;
        private SearchField _searchField;
        private DependenciesTreeView dependenciesTreeView;

        private Group selected;
        private GroupAssetTreeView treeView;

        public void ReloadDependencies(params string[] assetPaths)
        {
            if (dependenciesTreeView == null) return;

            dependenciesTreeView.assetPaths.Clear();
            dependenciesTreeView.assetPaths.AddRange(assetPaths);
            dependenciesTreeView.Reload();
            dependenciesTreeView.ExpandAll();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (dependenciesTreeViewState == null) dependenciesTreeViewState = new TreeViewState();
            if (dependenciesTreeView == null) dependenciesTreeView = new DependenciesTreeView(dependenciesTreeViewState);
            if (treeViewState == null)
            {
                treeViewState = new TreeViewState();
                var headerState =
                    GroupAssetTreeView.CreateDefaultMultiColumnHeaderState(); // multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);

                multiColumnHeaderState = headerState;
            }

            selected = target as Group;
            if (selected == null) return;

            //if (treeView == null)
            //{
            //    treeView = new GroupAssetTreeView(treeViewState, multiColumnHeaderState, this);
            //    Reload(selected);
            //}

            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
            }

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64)))
                {
                    if (treeView == null)
                    {
                        treeView = new GroupAssetTreeView(treeViewState, multiColumnHeaderState, this);
                    }
                    Reload(selected);
                }
                treeView.searchString = _searchField.OnToolbarGUI(treeView.searchString);
            }

            treeView.OnGUI(EditorGUILayout.GetControlRect(GUILayout.MinHeight(256), GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)));
            dependenciesTreeView.OnGUI(EditorGUILayout.GetControlRect(GUILayout.MinHeight(256),
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true), GUILayout.Height(256)));

            if (_bundleMode != selected.bundleMode) treeView?.Repaint();
            _bundleMode = selected.bundleMode;
        }

        private void Reload(Group group)
        {
            if (treeView == null) return;
            var assets = CollectAssets.Collect(group);
            treeView.assets.Clear();
            treeView.assets.AddRange(assets);
            treeView.Reload();

            var builds = Builder.FindAssets<Build>();
            foreach (var build in builds)
            foreach (var item in build.parameters.groups)
            {
                if (group != item) continue;
                group.build = build.name;
                return;
            }
        }
    }
}