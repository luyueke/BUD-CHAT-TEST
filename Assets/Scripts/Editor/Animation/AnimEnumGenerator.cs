using editor.UITools;
using Es;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace animation.editor
{
    public class AnimEnumGenerator : AssetPostprocessor
    {
        public static string OUPUT_ANIMATION_ID_ENUM_PATH => Application.dataPath + "/Scripts/GameData/AnimConfig/AnimId.cs";

        public const string ANIMATION_CONFIG_PATH = "Assets/Arts/Config/Asset/AnimationConfig.asset";

        // 导入后自动生成枚举脚本
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool isReGen = false;
            foreach (string str in importedAssets)
            {
                // Debug.Log("Reimported Asset: " + str);
                if (str.Contains("AnimationConfig.asset"))
                {
                    isReGen = true;
                    break;
                }
            }

            if (isReGen)
            {
                GenerateAll();
            }
        }

        public static void GenerateAll()
        {
            Debug.Log("开始自动生成Panel/Window枚举");
            GeneratePanelEnumFile();
            Debug.Log("自动生成Panel/Window枚举脚本结束！");
        }

        private static void GeneratePanelEnumFile()
        {
            var animConfigTable = UIEnumGenerator.LoadConfig<AnimationConfigTable>(ANIMATION_CONFIG_PATH);
            if (animConfigTable == null)
            {
                Debug.LogError("AnimationConfigTable is null ,pls check!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            UIEnumGenerator.GenAutoHeader(sb);
            sb.AppendLine("public enum AnimId");
            sb.AppendLine("{");

            int animId = 0;
            foreach (var animConfig in animConfigTable.DataList)
            {
                if (string.IsNullOrEmpty(animConfig.AnimationName) == false)
                {
                    sb.AppendLine($"    {animConfig.AnimationName} = {animId},");
                    animId++;
                }
            }

            sb.AppendLine("}");


            if (File.Exists(OUPUT_ANIMATION_ID_ENUM_PATH))
            {
                File.WriteAllText(OUPUT_ANIMATION_ID_ENUM_PATH, sb.ToString());
                Debug.Log("GeneratePanelEnumFile success");
            }
            else
            {
                Debug.LogError($"{OUPUT_ANIMATION_ID_ENUM_PATH} not exist!!");
            }
        }
    }
}


