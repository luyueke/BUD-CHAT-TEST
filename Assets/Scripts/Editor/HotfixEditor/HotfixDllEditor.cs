using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class HotfixDllEditor:Editor
{
    public static void RefreshOrCreatedHotfixDll()
    {
        return;
        string assetPath = "Assets/Arts/Config";
        //检查保存路径
        if (!Directory.Exists(assetPath))
            Directory.CreateDirectory(assetPath);
        string fullPath = assetPath + "/" + "HotfixDll.json";
        var hotfixDllNames = SortByDes();
        hotfixDllNames.Remove("HotDllDownload");
        // CheckDependence(hotfixDllNames);
        string content = JsonConvert.SerializeObject(hotfixDllNames);
        File.WriteAllText(fullPath,content);
        AssetDatabase.Refresh();
    }

    [System.Serializable]
    private class AsmdefData
    {
        public string[] references;
    }
    public static List<string>  SortByDes()
    {
        var hotfixDlls =HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions
            .Select(x => x.name).ToList();
        var defs = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions.ToList();
        int count = hotfixDlls.Count;
        for (int i = 0; i < count - 1; i++)
        {
            int mixIndex = i;
            var assemb = defs.Find(x => x.name.Equals(hotfixDlls[i]));
            for (int j = i + 1; j < count; j++)
            {

                var deps = GetDependencies(assemb.ToString());
                if (deps.Contains(hotfixDlls[j]))
                {
                    mixIndex = j;
                }
            }

            (hotfixDlls[mixIndex], hotfixDlls[i]) = (hotfixDlls[i], hotfixDlls[mixIndex]);
        }

        return hotfixDlls;
    }

    private static List<string> GetDependencies(string content)
    {
        
        AsmdefData data = JsonUtility.FromJson<AsmdefData>(content);
        List<string> deps = new List<string>();
        if (data != null && data.references != null)
        {
            foreach (var reference in data.references)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(reference.Replace("GUID:", ""));
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                if (asset != null)
                {
                    deps.Add(asset.name);
                }
            }
        }

        return deps;
    }

    private static void CheckDependence(List<string> sortDlls)
    {
        var defs = HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions.ToList();
        for (int i = 0; i < sortDlls.Count -1; i++)
        {
            var dllName = sortDlls[i];
            var assemb = defs.Find(x => x.name.Equals(dllName));
            if (assemb != null)
            {
                var deps = GetDependencies(assemb.ToString());
                for (int j = i + 1; j < sortDlls.Count - 1; j++)
                {
                    if (deps.Contains(sortDlls[j]))
                    {
                        Debug.LogError($"{dllName}.dll and {sortDlls[j]}.dll are related dependencies");
                    }
                }
            }
        }
    }


}