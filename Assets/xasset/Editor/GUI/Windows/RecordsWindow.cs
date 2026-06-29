using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    public class RecordsWindow : EditorWindow
    {
        [SerializeField] private MultiColumnHeaderState m_MultiColumnHeaderState;
        [SerializeField] private TreeViewState m_TreeViewState;

        private readonly GUIContent export = new GUIContent("Export to...", "Export recorded assets to split config");
        private readonly Dictionary<int, List<Record>> frameWithRequests = new Dictionary<int, List<Record>>();

        private Mode _mode = Mode.Current;

        private SearchField _searchField;

        private bool recording = true;

        private List<Record> records = new List<Record>();

        private int current { get; set; }

        private int frame { get; set; }

        private RecordTreeView m_TreeView { get; set; }

        private void Update()
        {
            if (recording && Application.isPlaying) TakeASample();
        }

        private void OnGUI()
        {
            if (m_TreeView == null)
            {
                m_TreeViewState = new TreeViewState();
                var headerState =
                    RecordTreeView.CreateDefaultMultiColumnHeaderState(); // multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);

                m_MultiColumnHeaderState = headerState;
                m_TreeView = new RecordTreeView(m_TreeViewState, headerState);
                m_TreeView.SetAssets(records);
            }

            if (_searchField == null)
            {
                _searchField = new SearchField();
                _searchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                recording = GUILayout.Toggle(recording, "Record", EditorStyles.toolbarButton);
                EditorGUI.BeginChangeCheck();
                _mode = (Mode) EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarDropDown, GUILayout.Width(64));
                if (EditorGUI.EndChangeCheck()) TakeASample();

                m_TreeView.searchString = _searchField.OnToolbarGUI(m_TreeView.searchString);

                GUILayout.FlexibleSpace();
                DrawExport();
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                {
                    frame = 0;
                    records.Clear();
                    ReloadFrameData();
                }

                GUILayout.Label("Frame:", EditorStyles.miniLabel, GUILayout.Width(48));
                if (GUILayout.Button("<", EditorStyles.toolbarButton))
                {
                    frame = Mathf.Max(0, frame - 1);
                    ReloadFrameData();
                    recording = false;
                }

                if (GUILayout.Button("Current", EditorStyles.toolbarButton))
                {
                    TakeASample();
                    recording = false;
                }

                if (GUILayout.Button(">", EditorStyles.toolbarButton))
                {
                    frame = Mathf.Min(frame + 1, Time.frameCount);
                    ReloadFrameData();
                    recording = false;
                }

                EditorGUI.BeginChangeCheck();
                frame = EditorGUILayout.IntSlider(frame, 0, current);
                if (EditorGUI.EndChangeCheck())
                {
                    recording = false;
                    ReloadFrameData();
                }
            }

            var treeRect = GUILayoutUtility.GetLastRect();
            m_TreeView.OnGUI(new Rect(0, treeRect.yMax, position.width, position.height - treeRect.yMax));
        }


        private void DrawExport()
        {
            var rect = GUILayoutUtility.GetRect(export, EditorStyles.toolbarDropDown);
            if (!EditorGUI.DropdownButton(rect, export, FocusType.Keyboard,
                    EditorStyles.toolbarDropDown))
                return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("csv"), false, data =>
            {
                var path = EditorUtility.SaveFilePanel("Save", "",
                    "records_" + DateTime.Now.ToFileTime(), "csv");
                if (string.IsNullOrEmpty(path)) return;

                var sb = new StringBuilder();
                sb.AppendLine("Path, Elapsed, Frames, Load Scene, Unload Scene, Reference Count");
                foreach (var loadable in records)
                    sb.AppendLine(
                        $"{loadable.name},{loadable.elapsed},{loadable.frames},{loadable.loadScene},{loadable.unloadScene},{loadable.refCount}");

                File.WriteAllText(path, sb.ToString());
                EditorUtility.OpenWithDefaultApp(path);
                ShowNotification(new GUIContent(path + "保存成功！"));
            }, null);

            var configs = Builder.FindAssets<AssetPack>();
            foreach (var config in configs)
                menu.AddItem(new GUIContent($"SplitConfig/{config.name}"), false,
                    data =>
                    {
                        var set = new HashSet<Object>(config.assets);
                        foreach (var item in records)
                        {
                            if (!File.Exists(item.name)) continue;
                            set.Add(AssetDatabase.LoadAssetAtPath<Object>(item.name));
                        }

                        config.assets = set.ToArray();
                        Selection.activeObject = config;
                        EditorUtility.SetDirty(config);
                        EditorUtility.FocusProjectWindow();
                        AssetDatabase.SaveAssets();
                    }, null);

            menu.DropDown(rect);
        }


        private void ReloadFrameData()
        {
            if (m_TreeView != null)
                m_TreeView.SetAssets(frameWithRequests.TryGetValue(frame, out var value) ? value : new List<Record>());
        }

        private void TakeASample()
        {
            current = frame = Time.frameCount;
            records = new List<Record>();

            switch (_mode)
            {
                case Mode.Current:
                    records.AddRange(Recorder.Current);
                    break;
                case Mode.Loads:
                    records.AddRange(Recorder.Loads);
                    break;
                case Mode.Unloads:
                    records.AddRange(Recorder.Unloads);
                    break;
            }

            frameWithRequests[frame] = records;
            ReloadFrameData();
        }

        private enum Mode
        {
            Current,
            Loads,
            Unloads
        }
    }
}