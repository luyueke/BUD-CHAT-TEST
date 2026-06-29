using System.Collections.Generic;
using System.IO;
using System.Text;
using Es;
using Newtonsoft.Json;
using OfficeOpenXml;
using Product;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using xasset;

public class LocalizationCheckTool : EditorWindow
{
    public static Dictionary<string, string> LangDic = new Dictionary<string, string>();

    [MenuItem("BudTools/多语言检测并导出Excel")]

    static public string CheckChineseCharacters()
    {
        LangDic.Clear();

        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Loadable", "Assets/Arts/UIPanel" });

        foreach (var prefabPath in prefabPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabPath);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                CheckTextComponents(prefab.GetComponentsInChildren<Text>(true));
                CheckSuperTextMesh(prefab.GetComponentsInChildren<SuperTextMesh>(true));
            }
        }

        CheckAllScript(); //检测脚本中动态设置的本地化key
        CheckAllAssetConfig();

        // string enPath = WriteToJson();
        string enPath = WriteToExcel();
        return enPath;
    }

    static string WriteToJson()
    {
      // var json = Utility.ConvertJsonString(JsonConvert.SerializeObject(LangDic));
        var json = Utility.ConvertJsonString(JsonConvert.SerializeObject(LangDic.Keys));
        Debug.Log(json);
        string enPath = "Assets/Loadable/Localization/locale-en.json";
        if (File.Exists(enPath))
        {
            File.Delete(enPath);
        }
        File.WriteAllText(enPath, json);
        return enPath;
    }
    
    public static string WriteToExcel()
    {
        // Excel 文件路径
        string excelPath = "Assets/Loadable/Localization/ExportToOnline.xlsx";
        
        // 检查文件是否存在，如果存在则删除
        if (File.Exists(excelPath))
        {
            File.Delete(excelPath);
        }

        // 确保文件夹存在
        Directory.CreateDirectory(Path.GetDirectoryName(excelPath));

        // 创建新的 Excel 文件
        using (ExcelPackage package = new ExcelPackage(new FileInfo(excelPath)))
        {
            // 添加新的工作表
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Sheet1");

            // 设置表头
            worksheet.Cells[1, 1].Value = "Key";
            worksheet.Cells[1, 2].Value = "en"; // 可以根据需要修改语言

            int row = 2; // 从第二行开始写数据
            foreach (var entry in LangDic)
            {
                // 写入数据
                worksheet.Cells[row, 1].Value = entry.Key;
                worksheet.Cells[row, 2].Value = ""; // 这里填充对应的数据，若有的话
                row++;
            }

            // 保存 Excel 文件
            package.Save();
        }

        return excelPath;
    }
    
    static void CheckAllScript()
    {
        string[] scriptPaths = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts" });
        foreach (var scriptPath in scriptPaths)
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GUIDToAssetPath(scriptPath));
            string content = File.ReadAllText(path).Trim();
            CheckScript(content, "TipPanel.ShowToast", 0);
            CheckScript(content, "UIAgentManager.Inst.ShowToast", 0);
        }
    }

    static void CheckAllAssetConfig()
    {
        CheckEmoteUIConfig();
        CheckAudioConfig();
        CheckPropConfig();
        CheckInstrumentConfig();
        CheckClothConfig();
    }
    
    /// <summary>
    /// 表情配置表
    /// </summary>
    static void CheckEmoteUIConfig()
    {
        var EmoUIConfigTable = AssetDatabase.LoadAssetAtPath<EmoUIConfigTable>("Assets/Arts/Config/Asset/EmoUIConfig.asset");
        foreach (var data in EmoUIConfigTable.DataList)
        {
            AddToDict(data.name);
        }
    }

    /// <summary>
    /// 编辑器背景音乐和环境音配置表
    /// </summary>
    static void CheckAudioConfig()
    {
        var GameBgAudioData = AssetDatabase.LoadAssetAtPath<GameBgAudioDataTable>("Assets/Arts/Config/Asset/GameBgAudioData.asset");
        foreach (var data in GameBgAudioData.DataList)
        {
            AddToDict(data.Name);
        }

        var GameNoiseAudioData = AssetDatabase.LoadAssetAtPath<GameNoiseAudioDataTable>("Assets/Arts/Config/Asset/GameNoiseAudioData.asset");
        foreach (var data in GameNoiseAudioData.DataList)
        {
            AddToDict(data.Name);
        }
    }
    
    /// <summary>
    ///编辑器设置 + 道具 配置表
    /// </summary>
    static void CheckPropConfig()
    {
        var dataTable = AssetDatabase.LoadAssetAtPath<GameEnvSettingDataTable>("Assets/Arts/Config/Asset/GameEnvSettingData.asset");
        foreach (var data in dataTable.DataList)
        {
            AddToDict(data.Name);
        }
        
        var propDataTable = AssetDatabase.LoadAssetAtPath<GamePropDataTable>("Assets/Arts/Config/Asset/GamePropData.asset");
        foreach (var data in propDataTable.DataList)
        {
            AddToDict(data.ShowName);
        }
    }

    /// <summary>
    /// 音色配置表
    /// </summary>
    static void CheckInstrumentConfig()
    {
        var dataTable = AssetDatabase.LoadAssetAtPath<InstrumentToneConfigTable>("Assets/Arts/Config/Asset/InstrumentToneConfig.asset");
        foreach (var data in dataTable.DataList)
        {
            AddToDict(data.toneName);
        }
    }
    
    /// <summary>
    /// 衣服相关配置
    /// </summary>
    static void CheckClothConfig()
    {
        var skinOrderConfig = AssetDatabase.LoadAssetAtPath<SkinOrderConfigTable>("Assets/Arts/Config/Asset/SkinOrderConfig.asset");
        foreach (var data in skinOrderConfig.DataList)
        {
            AddToDict(data.name);
        }

        var ugcPartData = AssetDatabase.LoadAssetAtPath<UgcPartDataTable>("Assets/Arts/Config/Asset/UgcPartData.asset");
        foreach (var data in ugcPartData.DataList)
        {
            AddToDict(data.partName);
        }
    }

    static void CheckProductConfig()
    {
        string prodectStr = System.IO.File.ReadAllText("Assets/Arts/Config/StoreConfig/product_test.json");
        var storeData = JsonConvert.DeserializeObject<StoreData>(prodectStr);
        foreach (var data in storeData.ProductList)
        {
            foreach (var assetData in data.AssetDataList)
            {
                AddToDict(assetData.Name);
            }
        }
    }

    static int GetEndIndexOfKeyParam(int startIndex, string content)
    {
        string subStr = content.Substring(startIndex);
        if (subStr.Trim().StartsWith("\""))
        {
            int index = subStr.IndexOf('\"');
            do
            {
                index = subStr.IndexOf('\"', index + 1);
                if (subStr[index - 1] != '\\') return startIndex + index + 1;
            } while (index > 0);
        }
        return -1;
    }

    static string RegularMatchReplace(string orig)
    {
        return orig.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\'", "\'").Replace("\\\"", "\"");
    }

    static void CheckScript(string content, string methodName, int paramIndex)
    {
        //遍历脚本内容
        int startIndex = 0;
        do
        {
            startIndex = content.IndexOf($"{methodName}(", startIndex);
            if (startIndex > -1)
            {
                startIndex = content.IndexOf(paramIndex == 0 ? '(' : ',', startIndex) + 1;
                int endIndex = GetEndIndexOfKeyParam(startIndex, content);
                if (endIndex - startIndex < 0) continue;
                string tagetStr = content.Substring(startIndex, endIndex - startIndex).Trim();
                //提取key
                int sIndex = tagetStr.IndexOf("\"") + 1;
                int eIndex = tagetStr.LastIndexOf("\"");
                if (sIndex > 0 && eIndex > 0 && sIndex != eIndex && !tagetStr.Contains("$\""))
                {
                    string key = tagetStr.Substring(sIndex, eIndex - sIndex);
                    key = RegularMatchReplace(key);
                    AddToDict(key);
                }
            }
        }
        while (startIndex > -1);
    }
    
    static bool TryParseNumber(string input)
    {
        float floatResult;
        int intValue;
        if (float.TryParse(input, out floatResult))
        {
            return true;
        }
        else if (int.TryParse(input, out intValue))
        {
            return true;
        }

        return false;
    }
    
    static bool ContainsChineseCharacters(string input)
    {
        foreach (char c in input)
        {
            if (IsChineseCharacter(c))
            {
                return true;
            }
        }
        return false;
    }

    static bool IsChineseCharacter(char c)
    {
        // 在这里添加判断字符是否为中文的逻辑
        return (c >= '\u4e00' && c <= '\u9fff');
    }

    static void AddToDict(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }
    
        // 去掉前后空格
        key = key.Trim();

        // 如果是数字或不包含中文字符，直接跳过
        if (TryParseNumber(key) || !ContainsChineseCharacters(key))
        {
            return;
        }

        LangDic[key] = key;
    }


    static void CheckTextComponents(Text[] textComponents)
    {
        foreach (var textComponent in textComponents)
        {
            AddToDict(textComponent.text);
        }
    }

    static void CheckSuperTextMesh(SuperTextMesh[] textComponents)
    {
        foreach (var textComponent in textComponents)
        {
            AddToDict(textComponent.text);
        }
    }
    
}
