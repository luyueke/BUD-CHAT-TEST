using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    // [CreateAssetMenu(fileName = nameof(Wwise), menuName = "xasset/" + nameof(Wwise))]
    // public class Wwise : ScriptableObject
    // {
    //     public int ver;
    //     public AssetPack[] wwisePack = null;
    //     public string buildName = "wwise";

    
        
    //     public string RootFolder => $"Audio/GeneratedSoundBanks/{Settings.Platform}";

    //     public string[] FileUrls
    //     {
    //         get
    //         {
    //             return new[]
    //             {
    //                 $"/../Audio/InstallTimeGeneratedSoundBanks/{Settings.Platform}", $"/../Audio/OnDemandGeneratedSoundBanks/{Settings.Platform}"
    //             };
    //         }
    //     }
    // }

    // public class WwiseBuild
    // {
    //     public static string SavePath = $"{Environment.CurrentDirectory}/{Assets.Bundles}/{Settings.Platform}/".Replace('\\', '/');
    //     public static void Start(Wwise wwise)
    //     {
    //         var versions = Settings.GetDefaultVersions();
    //         var version = versions.Get($"{wwise.buildName}");
    //         var manifest = Utility.LoadFromFile<Manifest>(Settings.GetDataPath($"{version.file}"));
    //         Debug.LogError("SavePath==="+SavePath);
    //         if (Directory.Exists($"{SavePath}/{wwise.buildName}"))
    //         {
    //             FileUtil.DeleteFileOrDirectory($"{SavePath}/{wwise.buildName}/");
    //         }
    //         SplitWwise(wwise);
    //         if (!BuildManifest(wwise, out manifest)) return;
    //         if (wwise.ver > 0)
    //             version.ver = wwise.ver;
    //         else
    //             version.ver++;
    //         var json = JsonUtility.ToJson(manifest);
    //         var bytes = Encoding.UTF8.GetBytes(json);
    //         var hash = Utility.ComputeHash(bytes);
    //         var buildToLower = manifest.build.ToLower();
    //         var file = $"{buildToLower}_{hash}.json";
    //         File.WriteAllText(Settings.GetDataPath(file), json);
    //         // save version
    //         var info = new FileInfo(Settings.GetDataPath(file));
    //         version.build = $"{wwise.buildName}";
    //         version.file = file;
    //         version.size = (ulong)info.Length;
    //         version.hash = hash;
    //         versions.Set(version);
    //         versions.Save(Settings.GetCachePath(Versions.Filename));
    //         var path = Settings.GetDataPath(versions.GetFilename());
    //         versions.Save(path);
    //         Builder.BuildUpdateInfo(versions, Utility.ComputeHash(path), new FileInfo(path).Length);
    //     }

    //     private static void SplitWwise(Wwise wwise)
    //     {
    //         foreach (var tmpFileUrl in wwise.FileUrls)
    //         {
    //             if (Directory.Exists(Application.dataPath + tmpFileUrl))
    //             {
    //                 Directory.Delete(Application.dataPath + tmpFileUrl, true);
    //             }
    //             Directory.CreateDirectory(Application.dataPath + tmpFileUrl);
    //         }
    //         var soundBanksInfo = AssetDatabase.LoadAssetAtPath<SoundBanksInfo>("Assets/Arts/Config/Asset/SoundBanksInfo.asset");
    //         if (File.Exists(soundBanksInfo.path))
    //         {
    //             soundBanksInfo.Parse(File.ReadAllText(soundBanksInfo.path));
    //         }
    //         foreach (var tmpSoundBank in soundBanksInfo.soundBanks)
    //         {
    //             foreach (var tmpFileName in tmpSoundBank.GetFiles())
    //             {
    //                 var tmpFilePath = Path.Combine(wwise.RootFolder, tmpFileName);
    //                 var targetFilePath = Path.Combine(tmpSoundBank.loadState == ResLoadState.InstallTime ? Application.dataPath + wwise.FileUrls[0] : Application.dataPath + wwise.FileUrls[1], tmpFileName);

    //                 if (!File.Exists(targetFilePath))
    //                 {
    //                     File.Copy(tmpFilePath, targetFilePath);
    //                 }
    //             }
    //         }
            
    //     }

    //     private static bool BuildManifest(Wwise wwise, out Manifest manifest)
    //     {
    //         manifest = ScriptableObject.CreateInstance<Manifest>();
    //         manifest.build = $"{wwise.buildName}";
    //         var set = new List<ManifestBundle>();
    //         var aSet = new List<ManifestAsset>();
    //         var installlocation = new List<AssetLocation>();
    //         var onDemandlocation = new List<AssetLocation>();

    //         ulong assetPack = 0;
    //         int count = 0;
            
            
    //         void SetManifest(int pack,FileInfo[] fileInfos,ref  List<AssetLocation> location)
    //         {
    //             for (int i = 0; i < fileInfos.Length; i++)
    //             {
    //                 var fileInfo = fileInfos[i];
    //                 if (fileInfo.Name.Contains(".meta"))
    //                 {
    //                     continue;
    //                 }

    //                 var ext = fileInfo.Extension;
    //                 var hash = Utility.ComputeHash(fileInfo.FullName);
    //                 var toPath = $"{SavePath}/{wwise.buildName}/{fileInfo.Name.Replace(".bnk", "_" + hash + ".bnk")}";

    //                 if (File.Exists(toPath)) continue;
    //                 Utility.CreateDirectoryIfNecessary(toPath);
    //                 File.Copy(fileInfo.FullName, toPath);
    //                 aSet.Add(new ManifestAsset
    //                 {
    //                     bundle = set.Count,
    //                     name = $"{wwise.buildName}/{fileInfo.Name}",
    //                     id = aSet.Count,
    //                     dir = 0,
    //                     auto = false,
    //                 });

    //                 set.Add(new ManifestBundle
    //                 {
    //                     hash = hash,
    //                     name = $"{wwise.buildName}/{fileInfo.Name.Replace(".bnk", "_" + hash + ".bnk")}",
    //                     raw = false,
    //                     size = (ulong) fileInfo.Length,
    //                     pack = pack,
    //                 });
    //                 location.Add(new AssetLocation
    //                 {
    //                     id = count,
    //                     size = (ulong) fileInfo.Length,
    //                 });
    //                 count++;
    //                 assetPack += (ulong) fileInfo.Length;
    //             }
    //         }

    //         var installDir = Directory.CreateDirectory(Application.dataPath +  wwise.FileUrls[0]);
    //         var onDemandDir = Directory.CreateDirectory(Application.dataPath + wwise.FileUrls[1]);
    //         var installInfos = installDir.GetFiles("*.*", SearchOption.AllDirectories);
    //         var onDemandInfos = onDemandDir.GetFiles("*.*", SearchOption.AllDirectories);
            
    //         SetManifest(0,installInfos,ref installlocation);
    //         SetManifest(1,onDemandInfos,ref onDemandlocation);

    //         manifest.assets = aSet.ToArray();
    //         manifest.dirs = new string[1] {""};
    //         manifest.bundles = set.ToArray();

    //         manifest.packs = new xasset.AssetPack[2]
    //         {
    //             new()
    //             {
    //                 name = wwise.wwisePack[0].name,
    //                 file = $"{manifest.build.ToLower()}_{wwise.buildName.ToLower()}_{0}{xasset.AssetPack.Extension}",
    //                 desc = "",
    //                 hash = "",
    //                 size = assetPack,
    //                 packed = false,
    //                 assets = installlocation
    //             },
    //             new()
    //             {
    //                 name = wwise.wwisePack[1].name,
    //                 file = $"{manifest.build.ToLower()}_{wwise.buildName.ToLower()}_{0}{xasset.AssetPack.Extension}",
    //                 desc = "",
    //                 hash = "",
    //                 size = assetPack,
    //                 packed = false,
    //                 assets = onDemandlocation
    //             }
    //         };
    //         return true;
    //     }


        
    // }
}