using System;
using System.Diagnostics;
using System.IO;
using Google.Protobuf;
using Pb.Map;

namespace GameData.MapData {
    public static class MapPbDataTool {
        #region Private Tools

        private static void SavePbToFile(string fileName, IMessage message) {
            if (message == null || string.IsNullOrEmpty(fileName)) return;
            using var output = File.Create(fileName);
            message.WriteTo(output);
        }

        private static T ReadPbFile<T>(string filePath, MessageParser parser) where T : IMessage {
            if (string.IsNullOrEmpty(filePath) || parser == null) return default;
            using var input = File.OpenRead(filePath);
            return (T)parser.ParseFrom(input);
        }

        public static byte[] CompressByteToByte(byte[] inputBytes) {
            MemoryStream ms = new MemoryStream();
            Stream stream = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(ms);
            try {
                stream.Write(inputBytes, 0, inputBytes.Length);
            } finally {
                stream.Close();
                ms.Close();
            }

            return ms.ToArray();
        }

        public static byte[] DecompressByteToByte(byte[] inputBytes) {
            MemoryStream ms = new MemoryStream(inputBytes);
            Stream sm = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(ms);
            byte[] data = new byte[sm.Length];
            int count = 0;
            MemoryStream re = new MemoryStream();
            while ((count = sm.Read(data, 0, data.Length)) != 0) {
                re.Write(data, 0, count);
            }

            sm.Close();
            ms.Close();
            return re.ToArray();
        }

        #endregion


        /// <summary>
        /// PB读取
        /// </summary>
        public static PMapData ParseMapPb(string filePath) {
            if (File.Exists(filePath) == false) {
                LoggerUtils.LogError($"ParseMapPb Error, file:{filePath} not found !");
                return null;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            var curMapData = ReadPbFile<PMapData>(filePath, PMapData.Parser);
            LoggerUtils.Log($"{nameof(MapPbDataTool)} ParseMapPb from file cost: {sw.ElapsedMilliseconds}");
            sw.Stop();
            return curMapData;
        }


        /// <summary>
        /// 从Bytes数组解析Pb
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static PMapData ParseMapPb(byte[] data) {
            if (data == null || data.Length <= 0) {
                LoggerUtils.LogError($"ParseMapPb Error, data is empty !");
                return null;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            var curMapData = PMapData.Parser.ParseFrom(data);
            //LoggerUtils.LogError(curMapData);
            LoggerUtils.Log($"{nameof(MapPbDataTool)} ParseMapPb from bytes cost: {sw.ElapsedMilliseconds}");
            sw.Stop();
            return curMapData;
        }

        public static PNodeData ParseNodePb(byte[] data) {
            if (data == null || data.Length <= 0) {
                LoggerUtils.LogError($"ParseNodePb Error, data is empty !");
                return null;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            var curNodeData = PNodeData.Parser.ParseFrom(data);

            LoggerUtils.Log($"{nameof(MapPbDataTool)} ParseNodePb from bytes cost: {sw.ElapsedMilliseconds}");
            sw.Stop();
            return curNodeData;
        }

        public static PUGCItemData ParsePropPb(byte[] data) {
            if (data == null || data.Length <= 0) {
                LoggerUtils.LogError($"ParseUGCItemPb Error, data is empty !");
                return null;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            PUGCItemData curNodeData = null;
            try {
                curNodeData = PUGCItemData.Parser.ParseFrom(data);
            } catch (Exception e) {
                try {
                    var nodeData = ParseNodePb(data);
                    curNodeData = new PUGCItemData() {
                        NodeData = nodeData,
                        UgcmatData = { }
                    };
                } catch (Exception exception) {
                    LoggerUtils.LogError($"ParseUGCItemPb Error, data is error !" + exception);
                }
            }


            sw.Stop();
            return curNodeData;
        }


        /// <summary>
        /// 保存map到pb
        /// </summary>
        public static void SaveMapPb(string filePath, PMapData mapData, Action<string> fileSavedCallback = null) {
            if (mapData == null) {
                LoggerUtils.LogError("SaveMapPb Failed. Data is null");
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            SavePbToFile(filePath, mapData);
            fileSavedCallback?.Invoke(filePath);
            LoggerUtils.Log(
                $"{nameof(MapPbDataTool)} SaveMapPb cost: {sw.ElapsedMilliseconds}， fileSize={mapData.CalculateSize()}");
            sw.Stop();
        }

        public static PNodeData GetNodeData(this PGamePropData pGamePropData, uint id) {
            PNodeData tmpNodeData = null;
            foreach (var pNodeData in pGamePropData.Pref) {
                if (pNodeData.Uid == id) {
                    tmpNodeData = pNodeData;
                    break;
                }

                tmpNodeData = pNodeData.GetNodeData(id);
                if (tmpNodeData != null) {
                    break;
                }
            }

            return tmpNodeData;
        }

        private static PNodeData GetNodeData(this PNodeData pNodeData, uint id) {
            PNodeData tmpNodeData = null;
            if (pNodeData.Prims == null || pNodeData.Prims.Count == 0) {
                return null;
            }

            foreach (var tmpChildNodeData in pNodeData.Prims) {
                if (tmpChildNodeData.Uid != id) continue;
                tmpNodeData = tmpChildNodeData;
                break;
            }

            return tmpNodeData;
        }
    }
}
