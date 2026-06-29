using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Utils;
using UnityEngine;

namespace Game.Utils
{
    public class TextureLocalCache : GlobalInstance<TextureLocalCache>
    {
        private const string extension = ".png";
        private static readonly string CachePath = Path.Combine(SaveGameUtil.CommonFolder, "CachePath");
        private static readonly string CachePathDic = Path.Combine(CachePath, "dic.txt");
#if UNITY_EDITOR
        private static readonly string SavePath = Path.Combine(Environment.CurrentDirectory, CachePath);
#else
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath ,CachePath);
#endif
        private const ulong MaxSize = 1024 * 1024 * 50;

        private Dictionary<string, ulong> dic;
        private List<string> list;

        private ulong size = 0;

        public TextureLocalCache()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            dic = SaveGameUtil.Inst.Load(CachePathDic, new Dictionary<string, ulong>());
            list = dic.Keys.ToList();
            foreach (var s in dic.Values)
            {
                size += s;
            }
        }

        public Texture2D Load(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            Texture2D t = null;
            if (dic.ContainsKey(url))
            {
                try
                {
                    using (FileStream fs = File.Open(Path.Combine(SavePath, url.GetMd5Hash() + extension), FileMode.Open))
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        byte[] bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, (int) fs.Length);
                        t = new Texture2D(0, 0);
                        t.LoadImage(bytes);
                    }
                }
                catch (Exception e)
                {
                    dic.Remove(url);
                    list.Remove(url);
                    SaveGameUtil.Inst.Save(CachePathDic, dic);
                }
            }

            return t;
        }

        public bool Add(string url, Texture2D t)
        {
            if (dic.ContainsKey(url))
            {
                if (list[0] != url)
                {
                    list.Remove(url);
                    list.Add(url);
                }

                return false;
            }
            else
            {
                byte[] bytes = t.EncodeToPNG();
                ulong s = (ulong) bytes.Length;
                MaxDelete(s);
                File.WriteAllBytes(Path.Combine(SavePath, url.GetMd5Hash() + extension), bytes);
                size += s;
                dic.Add(url, s);
                list.Add(url);
                SaveGameUtil.Inst.Save(CachePathDic, dic);
                return true;
            }
        }

        public void MaxDelete(ulong s)
        {
            if (size + s > MaxSize)
            {
                int c = 0;
                ulong ns = size + s, rs = 0;
                while (ns > MaxSize && c < list.Count)
                {
                    var u = list[c];
                    var ts = dic[u];
                    rs += ts;
                    ns -= ts;
                    c++;
                    dic.Remove(u);
                    var f = Path.Combine(SavePath, u.GetMd5Hash() + extension);
                    if (File.Exists(f))
                    {
                        File.Delete(f);
                    }
                }

                for (int i = 0; i < c; i++)
                {
                    var n = list[0];
                    dic.Remove(n);
                    list.RemoveAt(0);
                }

                size -= rs;
            }
        }
    }
}
