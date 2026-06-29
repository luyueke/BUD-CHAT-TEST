using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset
{
    public class Benchmark : MonoBehaviour
    {
        private const int PageSize = 100;
        private readonly List<Record> _assets = new List<Record>();
        private readonly Queue<Record> _queue = new Queue<Record>();
        private readonly List<Record> _selection = new List<Record>();

        private bool _loading;
        private int _page;
        private Vector2 _scrollPosition;
        private string _searchString;

        private void Start()
        {
            _loading = true;
        }

        private void Update()
        {
            if (_queue.Count <= 0) return;
            var item = _queue.Dequeue();
            Load(item);
        }

        private void OnGUI()
        {
            if (_loading)
            {
                GUILayout.Label("加载中...");
                return;
            }

            var str = GUILayout.TextField(_searchString, GUILayout.Width(Screen.width - 8));
            if (str != _searchString)
            {
                _searchString = str;
                UpdateSelection();
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"资源:{_selection.Count}", GUI.skin.label))
                    _selection.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("耗时(毫秒/帧数)", GUI.skin.label))
                    _selection.Sort((a, b) => b.elapsed.CompareTo(a.elapsed));
                if (GUILayout.Button("加载"))
                    foreach (var record in _selection)
                        _queue.Enqueue(record);
            }

            var max = _selection.Count / PageSize;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"页{_page + 1}/{max + 1}", GUILayout.Width(64));
                _page = (int) GUILayout.HorizontalSlider(_page, 0, max);
            }

            using (var s = new GUILayout.ScrollViewScope(_scrollPosition))
            {
                var start = _page * PageSize;
                for (var i = start; i < Mathf.Min(_selection.Count, start + PageSize); i++)
                {
                    var item = _selection[i];
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(item.name);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{item.elapsed:F2}/{item.frames}");
                        if (GUILayout.Button("加载"))
                            _queue.Enqueue(item);
                    }
                }

                _scrollPosition = s.scrollPosition;
            }
        }

        private static void Load(Record item)
        {
            if (item.name.EndsWith(".unity"))
            {
                var request = Scene.LoadAsync(item.name, true);
                item.BeginSample();
                request.WaitForCompletion();
                item.EndSample();
            }
            else
            {
                var request = Asset.LoadAsync(item.name, typeof(Object));
                item.BeginSample();
                request.WaitForCompletion();
                item.EndSample();
            }
        }

        private void UpdateSelection()
        {
            _selection.Clear();
            if (string.IsNullOrEmpty(_searchString))
                foreach (var asset in _assets)
                    _selection.Add(asset);
            else
                foreach (var asset in _assets)
                    if (asset.name.Contains(_searchString))
                        _selection.Add(asset);
        }

        public void Initialized()
        {
            _loading = false;
            foreach (var manifest in Assets.Versions.data)
            foreach (var asset in manifest.manifest.assets)
                if (!asset.auto)
                    _assets.Add(new Record {name = asset.path});
            UpdateSelection();
        }
    }
}