using System;
using System.Diagnostics;
using System.IO;
using Google.Protobuf;
using Pb.Theatre;

namespace GameData.TheatreData
{
    public static class TheatrePbDataTool
    {
        #region Private Tools

        private static void SavePbToFile(string fileName, IMessage message)
        {
            if (message == null || string.IsNullOrEmpty(fileName)) return;
            using var output = File.Create(fileName);
            message.WriteTo(output);
        }

        private static T ReadPbFile<T>(string filePath, MessageParser parser) where T : IMessage
        {
            if (string.IsNullOrEmpty(filePath) || parser == null) return default;
            using var input = File.OpenRead(filePath);
            return (T)parser.ParseFrom(input);
        }

        public static byte[] CompressByteToByte(byte[] inputBytes)
        {
            MemoryStream ms = new MemoryStream();
            Stream stream = new ICSharpCode.SharpZipLib.BZip2.BZip2OutputStream(ms);
            try
            {
                stream.Write(inputBytes, 0, inputBytes.Length);
            }
            finally
            {
                stream.Close();
                ms.Close();
            }

            return ms.ToArray();
        }

        public static byte[] DecompressByteToByte(byte[] inputBytes)
        {
            MemoryStream ms = new MemoryStream(inputBytes);
            Stream sm = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(ms);
            byte[] data = new byte[sm.Length];
            int count = 0;
            MemoryStream re = new MemoryStream();
            while ((count = sm.Read(data, 0, data.Length)) != 0)
            {
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
        public static POCTheatreDetailInfo ParseTheatrePb(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                LoggerUtils.LogError($"ParseTheatrePb Error, file:{filePath} not found !");
                return null;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            var curTheatreData = ReadPbFile<POCTheatreDetailInfo>(filePath, POCTheatreDetailInfo.Parser);
            LoggerUtils.Log($"{nameof(TheatrePbDataTool)} ParseTheatrePb from file cost: {sw.ElapsedMilliseconds}");
            sw.Stop();
            return curTheatreData;
        }

        /// <summary>
        /// 从Bytes数组解析Pb
        /// </summary>
        public static POCTheatreDetailInfo ParseTheatrePb(byte[] data)
        {
            if (data == null || data.Length <= 0)
            {
                LoggerUtils.LogError("ParseTheatrePb Error, data is empty !");
                return null;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            var curTheatreData = POCTheatreDetailInfo.Parser.ParseFrom(data);
            LoggerUtils.Log($"{nameof(TheatrePbDataTool)} ParseTheatrePb from bytes cost: {sw.ElapsedMilliseconds}");
            sw.Stop();
            return curTheatreData;
        }

        /// <summary>
        /// 保存theatre到pb
        /// </summary>
        public static void SaveTheatrePb(string filePath, POCTheatreDetailInfo theatreData, Action<string> fileSavedCallback = null)
        {
            if (theatreData == null)
            {
                LoggerUtils.LogError("SaveTheatrePb Failed. Data is null");
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();
            SavePbToFile(filePath, theatreData);
            fileSavedCallback?.Invoke(filePath);
            LoggerUtils.Log($"{nameof(TheatrePbDataTool)} SaveTheatrePb cost: {sw.ElapsedMilliseconds}， fileSize={theatreData.CalculateSize()}");
            sw.Stop();
        }
    }
}
