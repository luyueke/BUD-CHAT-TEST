
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using xasset;

public static class HotDllDownload
{
   private static List<string> OrderedAssemblyNames;
   private static Assembly entryAssembly = null;
   
   public static void LoadHotDll()
   {
      //todo: 需要考虑dll加载顺序
      var hotfixDll = Asset.Load("Assets/Arts/Config/HotfixDll.json",typeof(TextAsset));
      var content = (hotfixDll.asset as TextAsset)?.text;
      OrderedAssemblyNames = JsonConvert.DeserializeObject<List<string>>(content);
      foreach (var dll in OrderedAssemblyNames)
      {
         Debug.Log($"Bud load dll :{dll}");
         var hotReq = Asset.Load($"Assets/Arts/HybridCLR/Dlls/{dll}.bytes", typeof(TextAsset));
         OnLoadAssetSuccess(hotReq.asset);
      }
      if (entryAssembly != null)
      {
         StartHotfix(entryAssembly);
      }
      else
      {
         Debug.LogError($"Bud load hot fix dll failed !!");
      }
   }
   
   private static void OnLoadAssetSuccess(object asset)
   {
      TextAsset dll = (TextAsset) asset;
      Assembly hotfixAssembly = Assembly.Load(dll.bytes);
      var assName = hotfixAssembly.GetName().Name;
      Debug.Log($"Bud Load hotfix dll {assName} OK.");
      SetEntryAssembly(assName, hotfixAssembly);
   }


   private static void StartHotfix(Assembly hotfixAssembly)
   {
      Debug.Log($"Bud StartHotfix");
      var hotfixEntry = hotfixAssembly.GetType("HotfixEntry");
      var start = hotfixEntry.GetMethod("Start");
      start?.Invoke(null, null);
   }

   private static void SetEntryAssembly(string assName, Assembly ass)
   {
      var targetName = OrderedAssemblyNames[^1];
      if (targetName.Contains(assName))
      {
         entryAssembly = ass;
      }
   }
}
