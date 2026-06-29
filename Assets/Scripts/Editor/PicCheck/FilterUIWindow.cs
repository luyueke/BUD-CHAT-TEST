using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class FilterUIWindow
{
    
    [Serializable]
    public class FilterUIInfo
    {
        [JsonIgnore]
        [PreviewField(Alignment = ObjectFieldAlignment.Center, Height = 64)]
        public Object panel;
            

        public string path;

        public bool isObsolete;
    }
    
    [ReadOnly]
    [LabelText("筛选条件")]
    public string tip = "当前只筛选 Assets/Loadable/Prefabs/UIPanel/ 下名字中带有Panel 的预制体";
    
    

    [Searchable]
    [SerializeField][TableList(ShowIndexLabels = true)]
    [ListDrawerSettings(ShowPaging = true, NumberOfItemsPerPage = 50)]
    public List<FilterUIInfo> uiPrefabs = new List<FilterUIInfo>();
    
    public FilterUIWindow()
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new []{"Assets/Loadable/Prefabs/UIPanel/"});
        foreach (var tmpPrefab in prefabGuids)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(tmpPrefab);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab.name.Contains("Panel"))
            {
                if (uiPrefabs.Find(tmp => tmp.path == prefabPath) == null)
                {
                    uiPrefabs.Add(new FilterUIInfo()
                    {
                        panel = prefab,
                        path = prefabPath
                    });
                }
            }
        }
        
        var infoPath = Path.Combine(Application.dataPath, "..","FilterUIInfo.json");
        if (File.Exists(infoPath))
        {
            var content = File.ReadAllText(infoPath);
            JsonConvert.PopulateObject(content, this);
        }
    }

    public void OnDispose()
    {
        var infoPath = Path.Combine(Application.dataPath, "..","FilterUIInfo.json");
        File.WriteAllText(infoPath, JsonConvert.SerializeObject(this));
    }


}