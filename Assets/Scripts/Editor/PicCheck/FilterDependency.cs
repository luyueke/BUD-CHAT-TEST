using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class FilterDependencyWindow
{

    [OnValueChanged("OnRefreshObj")]
    [PreviewField(Alignment = ObjectFieldAlignment.Left, Height = 64)]
    public Object obj;

    [ReadOnly]
    public string objName;

    [ListDrawerSettings(Expanded = true)]
    public List<Object> reference;


    public void OnRefreshObj()
    {
        reference.Clear();
        if (obj == null)
        {
            return;
        }
        var path = AssetDatabase.GetAssetPath(obj);
        objName = obj.name;
        if (PicCheckWindow.textureRefDependencies.TryGetValue(path, out var parentAssets))
        {
            foreach (var tmpAsset in parentAssets)
            {
                reference.Add(AssetDatabase.LoadMainAssetAtPath(tmpAsset));
            }
    
        }
    }
}