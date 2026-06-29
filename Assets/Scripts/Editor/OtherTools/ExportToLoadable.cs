using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public class ExportToLoadable : MonoBehaviour
{
    [MenuItem("Assets/Send To Loadable")]
    public static void SendToLoadable()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("not allowed send To loadable in playing game");
            return;
        }
        var folders = Selection.assetGUIDs;
        if (folders.Length != 1) return;

        var folder = AssetDatabase.GUIDToAssetPath(folders[0]);
        Debug.Log(folder);
        if (!AssetDatabase.IsValidFolder(folder) && folder.Contains("Assets/Arts/"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
            if (asset is GameObject)
            {
                var directory = Path.GetDirectoryName(folder);
                var newPath = folder.Replace("Assets/Arts/", "Assets/Loadable/");
                Debug.Log(newPath + "|" + directory);
                var dic = Path.GetDirectoryName(newPath);
                if (!AssetDatabase.IsValidFolder(dic))
                {
                    Directory.CreateDirectory(dic);
                    AssetDatabase.Refresh();
                }
                GameObject prefabV = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                prefabV.name = asset.name;
                PrefabUtility.SaveAsPrefabAsset(prefabV, newPath);
                GameObject.DestroyImmediate(prefabV);
            }
            else
            {
                var newPath = folder.Replace("Assets/Arts/", "Assets/Loadable/");
                var dic = Path.GetDirectoryName(newPath);
                if (!AssetDatabase.IsValidFolder(dic))
                {
                    Directory.CreateDirectory(dic);
                    AssetDatabase.Refresh();
                }
                AssetDatabase.MoveAsset(folder, newPath);
            }
        }
        
        if (AssetDatabase.IsValidFolder(folder) && folder.Contains("Assets/Arts/Avatar/UGCRolePart"))
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var _prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                var newFolder = path.Replace("Assets/Arts/Avatar/UGCRolePart", "Assets/Loadable/Avatar/UGCRolePart/ModelPrefab");
                var directory = Path.GetDirectoryName(newFolder);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                GameObject prefabV = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
                prefabV.name = _prefab.name;
                PrefabUtility.SaveAsPrefabAsset(prefabV, newFolder);
                GameObject.DestroyImmediate(prefabV);
            }
            return;
        }
        
        if (AssetDatabase.IsValidFolder(folder) && folder.Contains("Assets/Arts/Pet/UGCRolePart"))
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var _prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                var newFolder = path.Replace("Assets/Arts/Pet/UGCRolePart", "Assets/Loadable/Pet/UGCRolePart/ModelPrefab");
                var directory = Path.GetDirectoryName(newFolder);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                GameObject prefabV = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
                prefabV.name = _prefab.name;
                PrefabUtility.SaveAsPrefabAsset(prefabV, newFolder);
                GameObject.DestroyImmediate(prefabV);
            }
            return;
        }
        
        if (AssetDatabase.IsValidFolder(folder) && folder.Contains("Assets/Arts/"))
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new string[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var _prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
                var newFolder = path.Replace("Assets/Arts/", "Assets/Loadable/");
                var directory = Path.GetDirectoryName(newFolder);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                GameObject prefabV = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
                prefabV.name = _prefab.name;
                PrefabUtility.SaveAsPrefabAsset(prefabV, newFolder);
                GameObject.DestroyImmediate(prefabV);
            }

            guids = AssetDatabase.FindAssets("t:spriteatlas", new string[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (spriteAtlas != null)
                {
                    var newPath = path.Replace("Assets/Arts/", "Assets/Loadable/");
                    var dic = Path.GetDirectoryName(newPath);
                    if (!AssetDatabase.IsValidFolder(dic))
                    {
                        Directory.CreateDirectory(dic);
                        AssetDatabase.Refresh();
                    }
                    AssetDatabase.MoveAsset(path, newPath);
                }

            }
        }
        
        
        
    }

    [MenuItem("Assets/Send Prop To Loadable")]
    public static void SendPropToLoadable()
    {
        var folders = Selection.assetGUIDs;
        if (folders.Length != 1) return;
        var folder = AssetDatabase.GUIDToAssetPath(folders[0]);
        if (!AssetDatabase.IsValidFolder(folder) && folder.Contains("Assets/Arts/"))
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folder);
            if (asset is GameObject)
            {
                var newPath = string.Format("Assets/Loadable/Model3D/Editor_Props/AIGames/AIPark/{0}/{1}.prefab", asset.name, asset.name);
                var directory = Path.GetDirectoryName(folder);
                Debug.Log(newPath + "|" + directory);
                var dic = Path.GetDirectoryName(newPath);
                if (!AssetDatabase.IsValidFolder(dic))
                {
                    Directory.CreateDirectory(dic);
                    AssetDatabase.Refresh();
                }
                GameObject prefabV = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                prefabV.name = asset.name;
                PrefabUtility.SaveAsPrefabAsset(prefabV, newPath);
                GameObject.DestroyImmediate(prefabV);
            }

        }
    }


    [MenuItem("Assets/Send Folder To Loadable")]
    public static void SendFolderToLoadable()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("not allowed send To loadable in playing game");
            return;
        }
        var folders = Selection.assetGUIDs;
        if (folders.Length != 1) return;

        var folder = AssetDatabase.GUIDToAssetPath(folders[0]);
        if (AssetDatabase.IsValidFolder(folder) && folder.Contains("Assets/Arts/"))
        {
            var newFolder = folder.Replace("Assets/Arts/", "Assets/Loadable/");
            FileUtil.MoveFileOrDirectory(folder, newFolder);
            AssetDatabase.Refresh();
        }
    }
}
