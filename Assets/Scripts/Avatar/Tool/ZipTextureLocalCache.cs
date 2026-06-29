using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Utils;
using UnityEngine;

namespace Game.AvatarTool
{
    public class ZipTextureLocalCache : GlobalInstance<ZipTextureLocalCache>
    {
        public class ZipTexInfo
        {
            public string fileName;
            public ulong size;
        }



        private const string extension = ".png";
        private static readonly string CachePath = Path.Combine(SaveGameUtil.CommonFolder, "UGCCachePath");
#if UNITY_EDITOR
        private static readonly string SavePath = Path.Combine(Environment.CurrentDirectory, CachePath);
        private static readonly string CachePathDic = Path.Combine("../",CachePath, "ugcTex.txt");

#else
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath ,CachePath);
         private static readonly string CachePathDic = Path.Combine(CachePath, "ugcTex.txt");
#endif
        private const ulong MaxSize = 1024 * 1024 * 50;

        private Dictionary<string, List<ZipTexInfo>> dic;
        private List<string> list;

        private ulong size = 0;

        public ZipTextureLocalCache()
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            dic = SaveGameUtil.Inst.Load(CachePathDic,new Dictionary<string, List<ZipTexInfo>>());
            size = GetUGCSize();
            list = dic.Keys.ToList();
        }

        public ulong GetUGCSize()
        {
            ulong temp = 0;
            foreach (var s in dic.Values)
            {
                if (s != null && s.Count > 0)
                {
                    foreach (var z in s)
                    {
                        if (z != null)
                        {
                            temp += z.size;
                        }
                    }

                }
            }
            return temp;
        }


        public Dictionary<string,Texture2D> Load(string url)
        {
            Dictionary<string,Texture2D> texDir = new Dictionary<string,Texture2D>();
            if (dic.ContainsKey(url))
            {

                    var zipInfos = dic[url];
                    var count = zipInfos.Count - 1;
                    string urlMd5 = url.GetMd5Hash();
                    for (var i = count; i >= 0; i--)
                    {
                        if (zipInfos[i] == null)
                        {
                            zipInfos.RemoveAt(i);
                            SaveGameUtil.Inst.Save(CachePathDic, dic);
                            continue;
                        }

                        try
                        {
                            using (FileStream fs = File.Open(Path.Combine(SavePath, zipInfos[i].fileName + extension),
                                       FileMode.Open))
                            {
                                fs.Seek(0, SeekOrigin.Begin);
                                byte[] bytes = new byte[fs.Length];
                                fs.Read(bytes, 0, (int) fs.Length);
                                var tex = new Texture2D(0, 0);
                                tex.LoadImage(bytes);
                                texDir.Add(zipInfos[i].fileName.Replace(urlMd5,""), tex);
                            }
                        }
                        catch (Exception e)
                        {
                            zipInfos.RemoveAt(i);
                            SaveGameUtil.Inst.Save(CachePathDic, dic);
                        }

                    }
            }
            return texDir;
        }

        public void Add(string url, Dictionary<string,Texture2D> texDir)
        {
            List<ZipTexInfo> texInfos = new List<ZipTexInfo>();
            foreach (var keyValue in texDir)
            {
                var t = keyValue.Value;
                byte[] bytes = t.EncodeToPNG();
                ulong s = (ulong) bytes.Length;
                MaxDelete(s);
                File.WriteAllBytes(Path.Combine(SavePath, url.GetMd5Hash() +keyValue.Key+ extension), bytes);
                size += s;
                ZipTexInfo info = new ZipTexInfo();
                info.fileName = url.GetMd5Hash() + keyValue.Key;
                info.size = s;
                texInfos.Add(info);
            }
            dic[url] = texInfos;
            list.Add(url);
            SaveGameUtil.Inst.Save(CachePathDic, dic);
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
                    var texInfos = dic[u];
                    for (int i = 0; i < texInfos.Count; i++)
                    {
                        if (texInfos[i] != null)
                        {
                            rs += texInfos[i].size;
                            ns -= texInfos[i].size;
                        }
                        var f = Path.Combine(SavePath, texInfos[i].fileName + extension);
                        if (File.Exists(f))
                        {
                            File.Delete(f);
                        }
                    }
                    c++;
                    dic.Remove(u);
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
