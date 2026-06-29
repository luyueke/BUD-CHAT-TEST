using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

public class LocalizationImporter : Editor
{
    [MenuItem("BudTools/多语言导入Excel")]
    public static void ImportLocalizationFromExcel()
    {
        string excelPath = EditorUtility.OpenFilePanel("选择Excel文件", "", "xlsx");
        if (string.IsNullOrEmpty(excelPath))
        {
            Debug.LogWarning("没有选择文件");
            return;
        }
        if (!File.Exists(excelPath))
        {
            Debug.LogError("Excel 文件不存在: " + excelPath);
            return;
        }

        Dictionary<string, Dictionary<string, string>> languageData = new Dictionary<string, Dictionary<string, string>>();

        using (ExcelPackage package = new ExcelPackage(new FileInfo(excelPath)))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets["Sheet1"]; // 选择第一个工作表

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            if (rowCount < 2)
            {
                Debug.LogError("Excel 文件格式不正确");
                return;
            }

            // 解析表头
            Dictionary<string, int> languageIndices = new Dictionary<string, int>();
            for (int col = 2; col <= colCount; col++) // 从第二列开始
            {
                string header = worksheet.Cells[1, col].Text;
                languageIndices[header] = col;
            }

            // 读取数据
            for (int row = 2; row <= rowCount; row++) // 从第二行开始
            {
                string key = worksheet.Cells[row, 1].Text;

                foreach (var language in languageIndices)
                {
                    if (!languageData.ContainsKey(language.Key))
                    {
                        languageData[language.Key] = new Dictionary<string, string>();
                    }

                    if (languageData[language.Key].ContainsKey(key)) {
                        LoggerUtils.LogError("已经存在相同Key:" + key + " 语言:" + language.Key);
                        continue;
                    }
                    string value = worksheet.Cells[row, language.Value].Text;
                    languageData[language.Key][key] = value;
                }
            }
        }

        // 创建并保存Localization资产
        foreach (var lang in languageData)
        {
            CreateAssetForLanguage(lang.Key, lang.Value);
        }
    }

    private static void CreateAssetForLanguage(string language, Dictionary<string, string> data)
    {
        var localizationData = ScriptableObject.CreateInstance<LocalizationKV>();
        localizationData.list = new List<LangKV>();

        foreach (var entry in data)
        {
            localizationData.list.Add(new LangKV { key = entry.Key, value = entry.Value });
        }

        string assetPath = $"Assets/Arts/Config/Localization/Lang-{language}.asset";

        // 删除已有的 asset 文件
        if (File.Exists(assetPath))
        {
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.Refresh();
        }

        AssetDatabase.CreateAsset(localizationData, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"生成翻译文件：Lang-{language}   路径: {assetPath}");
    }
}
