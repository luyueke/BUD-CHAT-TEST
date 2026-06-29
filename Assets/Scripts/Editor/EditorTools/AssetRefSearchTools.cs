using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AssetRefSearchTool
{
    static string[] assetGUIDs;
    static string[] assetPaths;
    static string[] allAssetPaths;
    static Thread thread;

    [MenuItem("Assets/查找图集中图片资源引用", false)]
    static void FindSpriteAssetRefMenu()
    {
        
        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("请先选择任意一个组件，再击此菜单");
            return;
        }
        Debug.LogError("开始查找-----------------------------");
        var  selects =  Selection.gameObjects;
        // string filePath = Application.dataPath + "/../spriteFile.txt";
        // if (File.Exists(filePath))
        // {
        //     File.Delete(filePath);
        // }
        // var sWrite = new StreamWriter(filePath, true);
        for (var i = 0; i < selects.Length; i++)
        {
            var select = selects[i];
            var rawImages = select.GetComponentsInChildren<RawImage>(true);
            for (int j = 0; j < rawImages.Length; j++)
            {
                if (rawImages[j].texture != null)
                {
                    string texPath = AssetDatabase.GetAssetPath(rawImages[j].texture);
                    string rootPath = GetRootPath(select.name,rawImages[j].gameObject);
                    Debug.LogError($"rootPath = {texPath}---texPath={texPath}");
                    // sWrite.Write($"rootPath = {rootPath}---texPath={texPath} \r\n");
                }
            }
            var spriteRenders = select.GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < spriteRenders.Length; j++)
            {
                if (spriteRenders[j].sprite != null)
                {
                    string texPath = AssetDatabase.GetAssetPath(spriteRenders[j].sprite);
                    string rootPath = GetRootPath(select.name,spriteRenders[j].gameObject);
                    Debug.LogError($"rootPath = {rootPath}---texPath={texPath}");
                    // sWrite.Write($"rootPath = {rootPath}---texPath={texPath} \r\n");
                }
            }
            
            var imageRenders = select.GetComponentsInChildren<Image>(true);
            for (int j = 0; j < imageRenders.Length; j++)
            {
                if (imageRenders[j].sprite != null)
                {
                    string texPath = AssetDatabase.GetAssetPath(imageRenders[j].sprite);
                    string rootPath = GetRootPath(select.name,imageRenders[j].gameObject);
                    Debug.LogError($"rootPath = {texPath}---texPath={texPath}");
                    // sWrite.Write($"rootPath = {rootPath}---texPath={texPath} \r\n");
                }
            }
        }
        // sWrite.Flush();
        // sWrite.Dispose();
        // sWrite.Close();
        Debug.LogError("查找完毕-----------------------------");
    }

    private static string GetRootPath(string rootName, GameObject child)
    {
        var tempGo = child.transform;
        string path = "";
        while (tempGo.parent != null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = string.Format("{0}/{1}", tempGo.name, path);
            }
            else
            {
                path = tempGo.name;
            }
            tempGo = tempGo.parent;
        }

        if (string.IsNullOrEmpty(path))
        {
            path = rootName;
        }
        else
        {
            path = string.Format("{0}/{1}", rootName, path);
        }
        return path;
    }



    [MenuItem("Assets/查找资源引用", false)]
    static void FindAssetRefMenu()
    {
        if (Selection.assetGUIDs.Length == 0)
        {
            Debug.Log("请先选择任意一个组件，再击此菜单");
            return;
        }

        assetGUIDs = Selection.assetGUIDs;

        assetPaths = new string[assetGUIDs.Length];

        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
        }

        allAssetPaths = AssetDatabase.GetAllAssetPaths();

        thread = new Thread(new ThreadStart(FindAssetRef));
        thread.Start();
    }

    static void FindAssetRef()
    {
        Debug.Log(string.Format("开始查找引用{0}的资源。", string.Join(",", assetPaths)));
        List<string> logInfo = new List<string>();
        string path;
        string log;
        for (int i = 0; i < allAssetPaths.Length; i++)
        {
            path = allAssetPaths[i];
            if (path.EndsWith(".prefab") || path.EndsWith(".unity"))
            {
                string content = File.ReadAllText(path);
                if (content == null)
                {
                    continue;
                }

                for (int j = 0; j < assetGUIDs.Length; j++)
                {
                    if (content.IndexOf(assetGUIDs[j]) > 0)
                    {
                        log = string.Format("{0} 引用了 {1}", path, assetPaths[j]);
                        logInfo.Add(log);
                    }
                }
            }
        }

        for (int i = 0; i < logInfo.Count; i++)
        {
            Debug.Log(logInfo[i]);
        }

        Debug.Log("选择对象引用数量：" + logInfo.Count);

        Debug.Log("查找完成");
    }
}