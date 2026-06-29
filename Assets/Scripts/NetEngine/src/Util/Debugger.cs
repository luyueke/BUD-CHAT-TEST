using System;
using System.IO;
using System.Text;
using NetEngine.src.Util.Def;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
using Debug = UnityEngine.Debug;
#else
using Debug = KeyframeEngine.Logging.Debug;
#endif

namespace NetEngine.src.Util
{
    public static class Debugger
    {
        public static bool Enable = false;
        public static bool EnableSocket2 = true;
        public static Action Callback = null;
        public static bool isSaveLog = false;

        public static void LogWithSockId(int id, string format, params object[] args)
        {
            if (!Enable)
                return;
            if (!EnableSocket2 && id == 1) return;
            LogColor("00ff00", format, args);

            Callback?.Invoke();
        }

        public static void Log(string format, params object[] args)
        {
            if (!Enable)
                return;
            LogColor("00ff00", format, args);

            Callback?.Invoke();
        }

        public static void LogError(string format, params object[] args)
        {
            if (!Enable)
                return;
            LogColor("ff0000", format, args);

            Callback?.Invoke();
        }

        public static void LogNormal(string format, params object[] args)
        {
            if (!Enable)
                return;
            LogColor("", format, args);

            Callback?.Invoke();
        }

        static void LogColor(string color, string format, params object[] args)
        {

            string str;
            try
            {
                str = "[" + RequestHeader.Version + "] " + string.Format(format, args);
            }
            catch (Exception)
            {
                str = "[" + RequestHeader.Version + "] " + format;
            }

            if (isSaveLog)
            {
                lock (sb)
                {
                    string outputStr = string.Format("[{0}:{1}:{2}]--{3}", DateTime.Now.Minute, DateTime.Now.Second,
                        DateTime.Now.Millisecond, str);
                    sb.AppendLine(outputStr);
                    Debug.Log($"{outputStr}");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(color))
                {
                    Debug.Log($"{str}");
                } else {
                    Debug.Log($"<color=#{color}>{str}</color>");
                }
            }
        }

        [NonSerialized]
#if UNITY_5_3_OR_NEWER
        private static string saveLogPath = Application.dataPath + "/Debugger.log";
#else
        private static string saveLogPath = "/Debugger.log";
#endif
        private static StringBuilder sb = new StringBuilder();

        public static void SaveLogFile()
        {
            if (!Enable || !isSaveLog)
                return;

            if (sb != null && !string.IsNullOrEmpty(sb.ToString()))
            {
                Debugger.Log("Save Log Finish:" + saveLogPath);
                File.WriteAllText(saveLogPath, sb.ToString());
            }
        }
    }
}