using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace xasset.example
{
    public class LoadRawAsset : MonoBehaviour
    {
        public Text content;

        private readonly List<LoadRequest> _requests = new List<LoadRequest>();

        public void LoadAsync()
        {
            var request = RawAsset.LoadAsync("Assets/xasset/Example/Raw/main.lua");
            content.text = $"RawAsset: {request.path}\n{File.ReadAllText(request.path)}";
            _requests.Add(request);
        }

        public void Load()
        {
            var request = RawAsset.Load("Assets/xasset/Example/Raw/main.lua");
            content.text = $"RawAsset: {request.path}\n{File.ReadAllText(request.path)}";
            _requests.Add(request);
        }

        public void Clear()
        {
            content.text = "点击加载后会在这里显示加载的内容。";
            foreach (var request in _requests) request.Release();

            _requests.Clear();
        }
    }
}