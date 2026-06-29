using System;
using GameData.BaseInfo;
using UnityEngine;

namespace Es {
    public partial class GameBgAudioData {

        [SerializeField]
        private bool _isUGC;
        public bool IsUGC => _isUGC;

        [SerializeField] private string _ugcMusicUrl;
        public string UgcMusicUrl => _ugcMusicUrl;

        [SerializeField] private int _loudness;

        public int Loudness => _loudness;


        public void SetUGCUrl(string url, int loudness) {
            _Id = "";
            _ugcMusicUrl = url;
            _State = "";
            _loudness = loudness;
            _isUGC = true;

            if (string.IsNullOrEmpty(url)) {
                _Name = "从本地资源提取";
            } else {
                Uri uri = new Uri(url);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
                _Name = fileName;
            }

        }

    }
}
