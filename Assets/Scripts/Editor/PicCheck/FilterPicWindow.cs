
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
[Serializable]
public class FilterPicWindow
{
    
    [Button("刷新资源")]
    public void RefreshAsset()
    {
        var texturesGuids = AssetDatabase.FindAssets("t:Texture", checkFolders.ToArray());
        bigTextureInfos.Clear();
        foreach (var texturesGuid in texturesGuids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(texturesGuid);
            if (!assetPath.EndsWith(".png") && !assetPath.EndsWith(".jpg") && !assetPath.EndsWith(".jpeg"))
            {
                Debug.Log("当前仅处理PNG、jpg、jpeg格式的图片:" + assetPath);
                continue;
            }
            bool isContains = false;
            foreach (var tmp in ignorePaths)
            {
                if (assetPath.Contains(tmp))
                {
                    isContains = true;
                    break;
                }
            }
            if (isContains)
            {
                continue;
            }
            var fileInfo = new FileInfo(assetPath);

            var textureImport = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            GetTextureOriginalSize(textureImport, out var originWidth, out var originHeight);
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
            if (originHeight <= 512 && originWidth <= 512)
            {
                continue;
            }
            
            var textureInfo = new BigTextureInfo()
            {
                name = fileInfo.Name,
                path = assetPath,
                originStorageSize = (int)fileInfo.Length,
                unityStorageSize = 0,
                unityWidth = texture.width,
                unityHeight = texture.height,
                originWidth = originWidth,
                originHeight = originHeight,
                texture = null,
            };
            textureInfo.unitySize = EditorUtility.FormatBytes(textureInfo.unityStorageSize);
            textureInfo.originSize = EditorUtility.FormatBytes(textureInfo.originStorageSize);
            bigTextureInfos.Add(textureInfo);
        }

        bigTextureInfos.Sort((a, b) => (b.unityWidth*b.unityHeight).CompareTo(a.unityWidth*a.unityHeight));
  
    }
    [JsonIgnore]
    [Searchable]
    [SerializeField]
    [TableList(ShowIndexLabels = true)]
    [ListDrawerSettings(Expanded = false)]
    public List<BigTextureInfo> bigTextureInfos = new List<BigTextureInfo>();
    
    
    public List<string> ignorePaths;
    public List<string> checkFolders;
    
    public FilterPicWindow(PicCheckWindow window)
    {
        var infoConfigPath = Path.Combine(Application.dataPath, "..","FilterPic.json");
        if (File.Exists(infoConfigPath))
        {
            var content = File.ReadAllText(infoConfigPath);
            JsonConvert.PopulateObject(content, this);
        }
        
        if (ignorePaths == null)
        {
            ignorePaths = new List<string>();
        }

        if (checkFolders == null)
        {
            checkFolders = new List<string>();
        }
        
        var infoPath = Path.Combine(Application.dataPath, "..","bigTextureInfos.json");
        if (File.Exists(infoPath))
        {
            var bigFileContent = File.ReadAllText(infoPath);
            bigTextureInfos = JsonConvert.DeserializeObject<List<BigTextureInfo>>(bigFileContent);    
        }
        else
        {
            RefreshAsset();
        }
    }
    
    private void GetTextureOriginalSize(TextureImporter ti, out int width, out int height)
    {
        if (ti == null)
        {
            width = 0;
            height = 0;
            return;
        }

        object[] args = new object[2] { 0, 0 };
        MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
        mi.Invoke(ti, args);

        width = (int)args[0];
        height = (int)args[1];
    }
    
    public void OnDispose()
    {
        var infoConfigPath = Path.Combine(Application.dataPath, "..","FilterPic.json");
        File.WriteAllText(infoConfigPath, JsonConvert.SerializeObject(this));
        
        var infoPath = Path.Combine(Application.dataPath, "..","bigTextureInfos.json");
        File.WriteAllText(infoPath, JsonConvert.SerializeObject(bigTextureInfos));
    }



}