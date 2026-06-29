using System.IO;
using Basic.Utils;
using UnityEngine;

namespace UI.Preview3D.Helper
{
    public static class ThemeSkinSaveHelper
    {
        private static string _localJsonStrPath => Application.dataPath + "/Arts/Avatar/ThemeSkin/CharacterJson/";

        public static void SaveCharacterJsonStr(string jsonContent)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(jsonContent))
            {
                var savePath = _localJsonStrPath + CreateNewFileName();
                File.WriteAllText(savePath, jsonContent);
                LoggerUtils.Log($"保存主题皮肤Avatar数据成功! ==> {savePath}");
            }
#endif
        }

        private static string CreateNewFileName()
        {
            return $"{GameUtils.GetTimeStamp()}.json";
        }
    }
}
