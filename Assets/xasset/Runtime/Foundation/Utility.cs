using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace xasset
{
    
  
    public static class Utility
    {
        private class FileMetadata
        {
            public string Hash { get; set; }
            public DateTime LastWriteTime { get; set; }
            public long Size { get; set; }
        }

        private static readonly double[] byteUnits = {1073741824.0, 1048576.0, 1024.0, 1};

        private static readonly string[] byteUnitsNames = {"GB", "MB", "KB", "B"};

        private static readonly MD5 _md5 = MD5.Create(); // 重用MD5实例

        private static readonly Dictionary<string, FileMetadata> _hashData = new Dictionary<string, FileMetadata>(); //性能优化，缓存hash

        public static string FormatBytes(ulong bytes)
        {
            var size = "0 B";
            if (bytes == 0) return size;

            for (var index = 0; index < byteUnits.Length; index++)
            {
                var unit = byteUnits[index];
                if (!(bytes >= unit)) continue;

                size = $"{bytes / unit:##.##} {byteUnitsNames[index]}";
                break;
            }

            return size;
        }

        public static T LoadFromFile<T>(string filename) where T : ScriptableObject
        {
            if (!File.Exists(filename)) return ScriptableObject.CreateInstance<T>();

            var json = File.ReadAllText(filename);
            var asset = ScriptableObject.CreateInstance<T>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, asset);
            }
            catch (Exception e)
            {
                Logger.E(e.Message);
                File.Delete(filename);
            }

            return asset;
        }

        public static T LoadAndDeleteFile<T>(string filename) where T : ScriptableObject
        {
            if (File.Exists(filename)) File.Delete(filename);
            return ScriptableObject.CreateInstance<T>();
        }

        public static T LoadFromJson<T>(string json) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            try
            {
                JsonUtility.FromJsonOverwrite(json, asset);
            }
            catch (Exception e)
            {
                Logger.E(e.Message);
                return null;
            }

            return asset;
        }

        public static void CreateDirectoryIfNecessary(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || Directory.Exists(dir)) return;

            Directory.CreateDirectory(dir);
        }

        public static string ToHash(byte[] data)
        {
            var sb = new StringBuilder(data.Length*2);
            foreach (var t in data) sb.Append(t.ToString("x2"));

            return sb.ToString();
        }

        public static string ComputeHash(byte[] bytes)
        {
            var data = MD5.Create().ComputeHash(bytes);
            return ToHash(data);
        }

  
        public static string GetCachedHash(string filename)
        {
            if (_hashData.TryGetValue(filename, out FileMetadata meta))
            {
                return meta.Hash;
            }
            return string.Empty;
        }


        public static bool HasChanged(string filename)
        {
            if (!File.Exists(filename)) return true;

            var info = new FileInfo(filename);
            if (_hashData.TryGetValue(filename, out var meta))
            {
                return meta.LastWriteTime != info.LastWriteTime ||
                       meta.Size != info.Length;
            }
            return true;
        }

        public static void UpdateMetadata(string filename, string hash)
        {
            var info = new FileInfo(filename);
            _hashData[filename] = new FileMetadata
            {
                Hash = hash,
                LastWriteTime = info.LastWriteTime,
                Size = info.Length
            };
        }
        public static string ComputeHash(string filename)
        {
            if (!File.Exists(filename)) return string.Empty;

            // 检查文件是否变化
            if (!HasChanged(filename)) {
                return GetCachedHash(filename);
            }
         
            string hash;
            using (var stream = File.OpenRead(filename)) 
            {
                hash = ToHash( _md5.ComputeHash(stream)); //BitConverter.ToString(_md5.Hash).Replace("-", "").ToLowerInvariant();
            }

            // 更新缓存
            UpdateMetadata(filename, hash);

            return hash;
            // using (var stream = File.OpenRead(filename))
            // {
            //     return ToHash(MD5.Create().ComputeHash(stream));
            // }
        }

        public static string GetProtocol()
        {
            if (Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.IPhonePlayer) return "file://";

            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
                return "file:///";

            return string.Empty;
        }

        public static Platform GetPlatform()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return Platform.Android;
                case RuntimePlatform.WindowsPlayer:
                    return Platform.Windows;
                case RuntimePlatform.OSXPlayer:
                    return Platform.OSX;
                case RuntimePlatform.IPhonePlayer:
                    return Platform.iOS;
                case RuntimePlatform.WebGLPlayer:
                    return Platform.WebGL;
                case RuntimePlatform.LinuxPlayer:
                    return Platform.Linux;
                default:
                    return Platform.Default;
            }
        }

        public static string ConvertJsonString(string str)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }
    }

 

}