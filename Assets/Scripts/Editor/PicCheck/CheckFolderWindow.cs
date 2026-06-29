// @Author: YangJie
// @Description:
// @Date:  2023/12/20
// @Modify:

using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

public class CheckFolderWindow
{
    
    [ListDrawerSettings()]
    public List<string> checkFolder;

    public List<string> ignorePaths;
    
    public CheckFolderWindow(PicCheckWindow window)
    {
        var infoPath = Path.Combine(Application.dataPath, "..","CheckFolderInfos.json");
        if (File.Exists(infoPath))
        {
            var content = File.ReadAllText(infoPath);
            JsonConvert.PopulateObject(content, this);
        }
    }

    public void AddIgnore(string path)
    {
        if (!ignorePaths.Contains(path))
        {
            ignorePaths.Add(path);
        }
    }

    public void OnDispose()
    {
        var infoPath = Path.Combine(Application.dataPath, "..","CheckFolderInfos.json");
        File.WriteAllText(infoPath, JsonConvert.SerializeObject(this));
    }
    
}