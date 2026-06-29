using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System;

namespace Game.Database
{
    /// <summary>
    /// 背包数据读写本地文件类
    /// </summary>
    public class BagPbFileIO
    {
        public static string CurUserId = "Temp";
        private static readonly string path = $"{Application.persistentDataPath}/AvatarDatabase";

        public static string GetPbFilePath(int filename)
        {
            return $"{path}/{CurUserId}/{filename}";
        }

        public static string GetPbCookiePath(int filename)
        {
            return $"{path}/{CurUserId}/{filename}.json";
        }

        public static string GetPbEditPath(int filename)
        {
            return $"{path}/{CurUserId}/{filename}_edit.json";
        }

        public static byte[] GetPdFile(int key)
        {
            byte[] fileText = null;
            try
            {
                var path = GetPbFilePath(key);
                if (!File.Exists(path)) return fileText;
                fileText = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                LoggerUtils.LogError("[AvatarDataBase] pb文件读取失败" + ex.Message + ex.StackTrace);
            }

            return fileText;
        }

        public static PbCookie GetPdCookie(int key)
        {
            PbCookie pbCookie = null;
            try
            {
                var path = GetPbCookiePath(key);
                if (!File.Exists(path)) return pbCookie;
                string fileText = File.ReadAllText(path);
                pbCookie = JsonConvert.DeserializeObject<PbCookie>(fileText);
            }
            catch (Exception ex)
            {
                LoggerUtils.LogError("[AvatarDataBase] pb文件读取失败" + ex.Message + ex.StackTrace);
            }

            return pbCookie;
        }

        public static bool CheckPbCookieIsOld(int key, int cookie, string md5 = "")
        {
            var pbCookie = GetPdCookie(key);
            if (pbCookie == null || !pbCookie.cookie.Equals(cookie)) return true;

            if (!string.IsNullOrEmpty(md5) && !pbCookie.md5.Equals(md5)) return true;

            return false;
        }

        public static PbCookie SetPbFile(int key, int cookie, byte[] bytes)
        {
            if (bytes == null) return null;

            var md5 = ComputeHash(bytes);

            try
            {
                CreateDirectoryIfNecessary(GetPbFilePath(key));
                File.WriteAllBytes(GetPbFilePath(key), bytes);
                var cookiePb = new PbCookie() { cookie = cookie, md5 = md5 };
                File.WriteAllText(GetPbCookiePath(key), JsonConvert.SerializeObject(cookiePb));
#if UNITY_EDITOR
                File.WriteAllText(GetPbEditPath(key), JsonConvert.SerializeObject(Network.Message.UserBackpack.Parser.ParseFrom(bytes)));
#endif
                return cookiePb;
            }
            catch(Exception ex)
            {
                LoggerUtils.LogError("[AvatarDataBase] pb文件写入失败" + ex.Message + ex.StackTrace);
                return null;
            }
        }

        public static void CreateDirectoryIfNecessary(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || Directory.Exists(dir)) return;

            Directory.CreateDirectory(dir);
        }

        public static string ToHash(byte[] data)
        {
            var sb = new StringBuilder();
            foreach (var t in data) sb.Append(t.ToString("x2"));
            return sb.ToString();
        }

        public static string ComputeHash(byte[] bytes)
        {
            var data = MD5.Create().ComputeHash(bytes);
            return ToHash(data);
        }
    }

    public class PbCookie
    {
        public int cookie;
        public string md5;
    }
}


