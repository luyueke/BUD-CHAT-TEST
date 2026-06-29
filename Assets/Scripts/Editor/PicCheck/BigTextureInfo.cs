using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


[Serializable]
public class BigTextureInfo
{
    [JsonIgnore] [PreviewField(Alignment = ObjectFieldAlignment.Left, Height = 64)] [ReadOnly]
    public Texture2D texture;

    [VerticalGroup("Properties")] [ReadOnly]
    public string name;


    [VerticalGroup("Properties")] [ReadOnly]
    public string path;

    [VerticalGroup("Origin")] [ReadOnly] public int originWidth;

    [VerticalGroup("Origin")] [ReadOnly] public int originHeight;
    [JsonIgnore] [HideInInspector] public int originStorageSize;

    [VerticalGroup("Origin")] [ReadOnly] public string originSize;

    [VerticalGroup("Unity")] [ReadOnly] public int unityWidth;

    [VerticalGroup("Unity")] [ReadOnly] public int unityHeight;

    [JsonIgnore] [HideInInspector] [ReadOnly]
    public int unityStorageSize;

    [VerticalGroup("Unity")] [ReadOnly] public string unitySize;


    [VerticalGroup("Reference")]
    [Button("FindReference")]
    public void FindReference()
    {
        reference.Clear();
        if (PicCheckWindow.textureRefDependencies.TryGetValue(path, out var parentAssets))
        {
            foreach (var tmpAsset in parentAssets)
            {
                if (tmpAsset.EndsWith(".prefab"))
                {
                    reference.Add(AssetDatabase.LoadMainAssetAtPath(tmpAsset));
                }
            }
        }
    }

    [VerticalGroup("Reference")] [JsonIgnore] [ReadOnly] [ListDrawerSettings(Expanded = true)]
    public List<Object> reference;


    [OnInspectorInit]
    private void LoadTexture()
    {
        if (texture == null)
        {
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }

    [Button("添加到忽略列表")]
    public void AddIgnorePath()
    {
        var window = EditorWindow.GetWindow<PicCheckWindow>();
        window.checkFolderWindow.ignorePaths.Add(path);
        window.bigTextureInfos.Remove(this);
    }


    [Button("删除资源")]
    public void DeleteAsset()
    {
        var window = EditorWindow.GetWindow<PicCheckWindow>();
        AssetDatabase.DeleteAsset(path);
        window.bigTextureInfos.Remove(this);
    }


    [HideInInspector] public bool isManual;

    [HideInInspector] public string manualComment;

    [JsonIgnore] [HideInInspector] public bool isDelete;
}