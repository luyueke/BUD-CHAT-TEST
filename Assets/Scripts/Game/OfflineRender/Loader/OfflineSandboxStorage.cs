using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using xasset;

namespace GameData.OfflineRender {
    public class OfflineSandboxStorage {
        public static string SandboxPath {
            get;
            internal set;
        } = $"{Application.persistentDataPath}/Cache/OfflineRender";

        public static int _refCount {
            get;
            internal set;
        } = 0;

        // UGC缓存大小限制
        public static ulong LimitSize {
            get;
            internal set;
        } = 500 * 1024 * 1024;

        // UGC缓存满了清理后保留的大小
        public static ulong ReduceSize {
            get;
            internal set;
        } = 50 * 1024 * 1024;

        public static ulong CurSize {
            get;
            internal set;
        } = 0;

        public static void Init() {
            if (!Directory.Exists(SandboxPath))
                Directory.CreateDirectory(SandboxPath);
            CurSize = GetDirectorySize(SandboxPath);
        }

        public static void DownLoadFinish(ulong size, string path, byte[] bytes) {
            var request = new SaveCacheRequest(size, path, bytes);
            request.completed += (req) => {
                if (req.result == Request.Result.Success) {
                    CheckSize();
                    CurSize += ((SaveCacheRequest)req)._size;
                }
            };
            request.SendRequest();
        }

        public static void DeleteFile(string filename) {
            try {
                var info = new FileInfo(filename);
                if (info.Exists) {
                    File.Delete(filename);
                    CurSize -= (ulong)info.Length;
                }
            } catch {
            }
        }

        public static string GetSandboxPath(string filename) {
            return $"{SandboxPath}/{filename}";
        }

        public static ulong GetDirectorySize(string path) {
            ulong size = 0;
            DirectoryInfo dir = new DirectoryInfo(path);

            foreach (FileInfo file in dir.GetFiles()) {
                size += (ulong)file.Length;
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories()) {
                size += GetDirectorySize(subDir.FullName);
            }

            return size;
        }

        public static void CheckSize() {
            if (CurSize < LimitSize) return;
            DirectoryInfo dir = new DirectoryInfo(SandboxPath);
            var files = dir.GetFiles();
            var filesList = files.ToList();
            filesList.Sort((a, b) => a.LastWriteTimeUtc > b.LastWriteTimeUtc ? -1 : 1);
            var list = new List<FileInfo>();
            foreach (FileInfo file in dir.GetFiles()) {
                try {
                    File.Delete(file.FullName);
                    CurSize -= (ulong)file.Length;
                    if (CurSize < ReduceSize) return;
                } catch {
                    continue;
                }
            }
        }

        internal static void ClearAllCache() {
            try {
                if (Directory.Exists(SandboxPath))
                    Directory.Delete(SandboxPath, true);
            } catch (Exception E) {
                Debug.LogError(E.Message);
            }
        }
    }
}
