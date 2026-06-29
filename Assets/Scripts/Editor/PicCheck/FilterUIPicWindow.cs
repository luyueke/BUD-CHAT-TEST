using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


[Serializable]
public class FilterUIPicWindow
{
      
      [Serializable]
      public class FilterPicInfo
      {
            [PreviewField(Alignment = ObjectFieldAlignment.Left, Height = 64)]
            public Object pic;
            
            [ListDrawerSettings(Expanded = true)]
            public List<Object> dependencies;

            public string path;

            [Button("删除无用资源")]
            private void DeleteAsset()
            {
                  AssetDatabase.DeleteAsset(path);
            }

            [OnInspectorInit]
            private void OnInspectorInit()
            {
                  if (pic == null && !string.IsNullOrEmpty(path))
                  {
                        pic = AssetDatabase.LoadMainAssetAtPath(path);
                  }
            }

            [OnInspectorDispose]
            private void OnInspectorDispose()
            {
                  pic = null;
            }
      }

      [JsonIgnore]
      [ReadOnly]
      [LabelText("筛选条件")]
      public string tip = "当前只筛选 UITexture/UIPanel 下未有引用关系的图片";
      
      
      public List<string> filterFolders;
      public List<string> ignorePaths;
      
      [JsonIgnore]
      [Searchable]
      [TableList(ShowIndexLabels = true)]
      [ListDrawerSettings(Expanded = true)]
      public List<FilterPicInfo> filterPicInfos = new List<FilterPicInfo>();



      [JsonIgnore]
      private Dictionary<string, List<string>> reverseDependencies;
      
      public FilterUIPicWindow()
      {
            this.reverseDependencies = PicCheckWindow.textureRefDependencies;
            var infoPath = Path.Combine(Application.dataPath, "..","FilterUIPic.json");
            if (File.Exists(infoPath))
            {
                  var content = File.ReadAllText(infoPath);
                  JsonConvert.PopulateObject(content, this);
            }
      }

      [Button("刷新资源")]
      public void RefreshAsset()
      {
            filterPicInfos.Clear();
            if (filterFolders == null)
            {
                  filterFolders = new List<string>();
            }
            var assetGuids = AssetDatabase.FindAssets("t:Texture", filterFolders.ToArray());
            foreach (var assetGuid in assetGuids)
            {
                  var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                  if (ignorePaths.Any(tmp => assetPath.Contains(tmp)))
                  {
                        continue;
                  }
                  reverseDependencies.TryGetValue(assetPath, out var parentAssets);
                  var realParentAssets = new List<string>();
                  realParentAssets.AddRange(
                        parentAssets?.Where(tmp => tmp.EndsWith(".prefab") || tmp.EndsWith(".mat")) ?? Array.Empty<string>());
                  if (realParentAssets.Count == 0)
                  {
                        var filterPicInfo = new FilterPicInfo()
                        {
                              dependencies = realParentAssets.Select(AssetDatabase.LoadAssetAtPath<Object>).ToList(),
                              path = assetPath
                        };
                        filterPicInfos.Add(filterPicInfo);
                  }
            }
      }

      [Button("清理资源")]
      public void DeleteAssets()
      {
            foreach (var filterPic in filterPicInfos.ToArray())
            {
                  AssetDatabase.DeleteAsset(filterPic.path);
            }
            filterPicInfos.Clear();
      }


      public void OnDispose()
      {
            var infoPath = Path.Combine(Application.dataPath, "..","FilterUIPic.json");
            File.WriteAllText(infoPath, JsonConvert.SerializeObject(this));
      }
}