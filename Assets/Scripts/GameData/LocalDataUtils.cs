using System;
using System.Collections.Generic;
using System.IO;
using Basic.Extensions;
using Basic.Utils;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using UnityEngine;

namespace GameData
{
    [Serializable]
    public class LocalInfo
    {
        public Dictionary<string, PlayerLocalInfo> PlayerLocalInfoDic;
    }
    [Serializable]
    public class PlayerLocalInfo
    {
        public List<string> CustomizeColor = new List<string>();//自定义颜色
        public List<string> UgcColorDatas = new List<string>();//Ugcy衣服颜色数据集合
    }

    public class LocalDataUtils : GlobalInstance<LocalDataUtils>
    {
        static string dataDir => Application.persistentDataPath + "/U3D/";
        static string localInfo = "localInfo.json";

        LocalInfo globalLocalInfo;

        public LocalDataUtils()
        {
            globalLocalInfo = GetLocalInfo();
        }

        public void SetCustomizeColorInfo(List<string> list)
        {
            // TODO : SelfUID
            var testSelfId = "1557008334052933632";
            var playInfo = GetPlayerInfoById(testSelfId, globalLocalInfo);
            playInfo.CustomizeColor = list;
            globalLocalInfo.PlayerLocalInfoDic[testSelfId] = playInfo;
        }

        public List<string> GetCustomizeColorInfo()
        {
            // TODO : SelfUID
            var testSelfId = "1557008334052933632";
            if (globalLocalInfo.PlayerLocalInfoDic != null && globalLocalInfo.PlayerLocalInfoDic.ContainsKey(testSelfId))
            {
                return globalLocalInfo.PlayerLocalInfoDic[testSelfId].CustomizeColor;
            }
            return new List<string>();
        }

        public void NotifySaveLocal()
        {
            SaveLocalInfo(globalLocalInfo);
        }

        public string SaveImgRes(byte[] img)
        {
            string fileName = GetResImgName();
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            fileName = dataDir + fileName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            FileStream stream = new FileStream(fileName, FileMode.Create);
            stream.Write(img, 0, img.Length);
            stream.Flush();
            stream.Close();
            return fileName;
        }
        
        public string SaveTempImgRes(string userId,byte[] img)
        {
            string fileName = GetResImgName(userId);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            fileName = dataDir + fileName;
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            FileStream stream = new FileStream(fileName, FileMode.Create);
            stream.Write(img, 0, img.Length);
            stream.Flush();
            stream.Close();
            return fileName;
        }
        

        public Texture2D LoadImgRes(string path, int w, int h)
        {
            var imgByte = File.ReadAllBytes(path);

            Texture2D img = new Texture2D(w, h);
            img.LoadImage(imgByte);

            return img;
        }

        private PlayerLocalInfo GetPlayerInfoById(string id,LocalInfo info)
        {
            if (info.PlayerLocalInfoDic == null)
            {
                info.PlayerLocalInfoDic = new Dictionary<string, PlayerLocalInfo>();
            }
            PlayerLocalInfo playInfo;
            if (info.PlayerLocalInfoDic.ContainsKey(id))
            {
                playInfo = info.PlayerLocalInfoDic[id];
            }
            else
            {
                playInfo = new PlayerLocalInfo();
            }
            return playInfo;
        }

        private void SaveLocalInfo(LocalInfo info)
        {
            string filePath = dataDir + localInfo;
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            string json = JsonConvert.SerializeObject(info);
            File.WriteAllText(filePath, json);
        }

        public LocalInfo GetLocalInfo()
        {
            string filePath = dataDir + localInfo;
            LocalInfo info = null;
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            if (File.Exists(filePath))
            {
                string jsonStr = File.ReadAllText(filePath);
                if(!string.IsNullOrEmpty(jsonStr))
                {
                    info = JsonConvert.DeserializeObject<LocalInfo>(jsonStr);
                }
            }

            if (info == null)
            {
                info = new LocalInfo();

            }
            return info;
        }
        public void SetUgcClothsColors(string id ,List<Color> colors)
        {
            if (globalLocalInfo.PlayerLocalInfoDic == null)
            {
                globalLocalInfo.PlayerLocalInfoDic = new Dictionary<string, PlayerLocalInfo>();
            }
            if (!globalLocalInfo.PlayerLocalInfoDic.ContainsKey(id))
            {
                globalLocalInfo.PlayerLocalInfoDic.Add(id,new PlayerLocalInfo());
            }
            var sList = new List<string>();
            for (int i = 0; i < colors.Count; i++)
            {
                sList.Add(FormatUtils.ColorToString(colors[i]));
            }
            globalLocalInfo.PlayerLocalInfoDic[id].UgcColorDatas = sList;
        }
        /// <summary>
        /// 保存临时文件到本地沙盒
        /// </summary>
        /// <param name="data">文件数据</param>
        /// <param name="fileName">文件名</param>
        /// <returns>返回本地全路径</returns>
        public static string SaveDataToSandbox(byte[] data, string fileName)
        {
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            var fullPath = dataDir + fileName;
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            LoggerUtils.Log("dataDir + fileName=====" + fullPath);
            FileStream stream = new FileStream(fullPath, FileMode.Create);
            stream.Write(data, 0, data.Length);
            LoggerUtils.Log("dataDir + json.Length=====" + data.Length);

            stream.Flush();
            stream.Close();
            return fullPath;
        }

        private string GetResImgName()
        {
            // TODO : SelfUID
            var testSelfId = "1557008334052933632";
            return testSelfId + "_" + GameUtils.GetTimeStamp() + ".png";
        }
        
        private string GetResImgName(string userId)
        {
            return userId + "_" + GameUtils.GetTimeStamp() + ".png";
        }

        
        public static void SaveZipDataToSandbox(Dictionary<string, byte[]> data, string fullPath)
        {

            var tmpDir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(tmpDir) && !string.IsNullOrEmpty(tmpDir))
            {
                Directory.CreateDirectory(tmpDir);
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            var zipFile = File.Create(fullPath);
            var zipStream = new ZipOutputStream(zipFile);
            zipStream.SetLevel(6);
            foreach (var dataKeyValuePair in data)
            {
                zipStream.PutNextEntry(new ZipEntry(dataKeyValuePair.Key));
                zipStream.Write(dataKeyValuePair.Value);
            }

            zipStream.PutNextEntry(new ZipEntry("fileList.json"));
            zipStream.Write( System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data.Keys)));

            zipStream.Finish();
            zipStream.Close();
            zipFile.Close();

        }

    }
}
