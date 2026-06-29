using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace xasset.editor
{
    public static class MenuItems
    {
        [MenuItem("xasset/About xasset", false, 1)]
        public static void OpenAbout()
        {
            Application.OpenURL("https://xasset.cc");
        }

        [MenuItem("xasset/Check for Updates", false, 1)]
        public static void CheckForUpdates()
        {
            Application.OpenURL("https://xasset.cc/docs/next/change-log");
        }

        [MenuItem("xasset/Edit Mode", false, 50)]
        public static void SwitchSimulationMode()
        {
            var settings = Settings.GetDefaultSettings();
            settings.EditMode = !settings.EditMode;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("xasset/Edit Mode", true, 50)]
        public static bool RefreshSimulationMode()
        {
            var settings = Settings.GetDefaultSettings();
            Menu.SetChecked("xasset/Edit Mode", settings.EditMode);
            return true;
        }

        [MenuItem("xasset/Open/Settings", false, 100)]
        public static void PingSettings()
        {
            Selection.activeObject = Settings.GetDefaultSettings();
            EditorGUIUtility.PingObject(Selection.activeObject);
            EditorUtility.FocusProjectWindow();
        }

        [MenuItem("xasset/Open/Startup Scene", false, 100)]
        public static void OpenStartupScene()
        {
            EditorSceneManager.OpenScene("Assets/xasset/Example/Startup.unity");
        }

        [MenuItem("xasset/Open/Download Data Path", false, 100)]
        public static void OpenDownloadBundles()
        {
            Debug.LogError(Assets.DownloadDataPath);
            EditorUtility.OpenWithDefaultApp(Assets.DownloadDataPath);
        }
        
        [MenuItem("xasset/当前平台全量打AB", false, 100)]
        public static void BuildBundlesAll()
        {
            Builder.Quality = 0;
            Builder.PreprocessBuildBundles = TextureQuality.PreprocessBuildBundles;
            AssetDatabase.Refresh();
            Builder.BuildBundles(Builder.FindAssets<Build>());
			//BuildWwise();
            BuildPlayerAssets.ClearTempFile();
            BuildPlayerAssets.StartNew();
            ClearHistory();
        }

        [MenuItem("xasset/当前平台全量打AB低模", false, 100)]
        public static void BuildBundlesAllLow()
        {
            Builder.Quality = 1;
            Builder.PreprocessBuildBundles = TextureQuality.PreprocessBuildBundles;
            AssetDatabase.Refresh();
            Builder.BuildBundles(Builder.FindAssets<Build>());
            //BuildWwise();
            BuildPlayerAssets.ClearTempFile();
            BuildPlayerAssets.StartNew();
            ClearHistory();
        }

        // private static void BuildWwise()
        // {
        //     string[] sdirs = {"Assets/xasset/Config/Builds"};
        //     var asstIds = AssetDatabase.FindAssets("t:Wwise", sdirs);
        //     for (int i = 0; i < asstIds.Length; i++)
        //     {
        //         string path = AssetDatabase.GUIDToAssetPath(asstIds[i]);
        //         var wwiseAsset = AssetDatabase.LoadAssetAtPath<Wwise>(path);
        //         if (wwiseAsset) WwiseBuild.Start(wwiseAsset);
        //     }
        // }
        
        [MenuItem("xasset/Build Bundles with Last Build", false, 100)]
        public static void BuildBundlesWithLastBuild()
        {
            Builder.BuildBundlesWithLastBuild(Selection.GetFiltered<Build>(SelectionMode.DeepAssets));
        }

        [MenuItem("xasset/Build Player", false, 100)]
        public static void BuildPlayer()
        {
            Builder.BuildPlayer();
        }

        [MenuItem("xasset/Build Player Assets", false, 100)]
        public static void BuildPlayerAssetsWithSelection()
        {

            BuildPlayerAssets.ClearTempFile();
            BuildPlayerAssets.StartNew();
            BuildPlayerAssets.Copy();
        }

        [MenuItem("xasset/Build Update Info", false, 100)]
        public static void BuildUpdateInfo()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select", Settings.PlatformDataPath,
                new[] { "versions", "json" });
            if (string.IsNullOrEmpty(path)) return;

            var versions = Utility.LoadFromFile<Versions>(path);
            var file = new FileInfo(path);
            var hash = Utility.ComputeHash(path);
            Builder.BuildUpdateInfo(versions, hash, file.Length);
        }

        [MenuItem("xasset/Print Changes with Selection", false, 150)]
        public static void PrintChangesFromSelection()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Select", Settings.PlatformDataPath,
                new[] { "versions", "json" });
            if (string.IsNullOrEmpty(path)) return;
            var versions = Utility.LoadFromFile<Versions>(path);
            Builder.PrintChanges(versions);
        }

        [MenuItem("xasset/Clear Download", false, 200)]
        public static void ClearDownload()
        {
            var directory = Application.persistentDataPath;
            Debug.LogError("directory==="+directory);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

     

        [MenuItem("xasset/Clear History", false, 200)]
        public static void ClearHistory()
        {
            Builder.ClearHistory();
        }

        [MenuItem("Window/xasset/Builds")]
        public static void OpenBuilds()
        {
            EditorWindow.GetWindow<BuildsWindow>(false, "Builds");
        }

        [MenuItem("Window/xasset/Manifests")]
        public static void OpenManifests()
        {
            EditorWindow.GetWindow<ManifestsWindow>(false, "Manifests");
        }

        [MenuItem("Window/xasset/Records")]
        public static void OpenRecords()
        {
            EditorWindow.GetWindow<RecordsWindow>(false, "Records");
        }

        [MenuItem("xasset/Resize File")]
        public static void ResizeFile()
        {
            var path = EditorUtility.OpenFilePanel("Select", Application.persistentDataPath, "");
            if (string.IsNullOrEmpty(path)) return;
            var stream = File.OpenWrite(path);
            stream.SetLength(1024 * 1024 * 4);
            stream.Flush();
            stream.Close();
        }
        
        [MenuItem("xasset/Clear Bundles")]
        public static void ClearBundles()
        {
            var directory = Settings.PlatformDataPath;
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            var directory1 = Settings.PlatformCachePath;
            if (Directory.Exists(directory1))
                Directory.Delete(directory1, true);
        }
        
        [MenuItem("Assets/To Json")]
        public static void ToJson()
        {
            var activeObject = Selection.activeObject;
            var json = Utility.ConvertJsonString(JsonUtility.ToJson(activeObject));
            var path = AssetDatabase.GetAssetPath(activeObject);
            var ext = Path.GetExtension(path);
            File.WriteAllText(path.Replace(ext, ".json"), json);

            AssetDatabase.Refresh();
        }
        
        [MenuItem("xasset/删除低于当前大版本的启动资源", false, 100)]
        public static void ClearLaunchResouce()
        {
            var versionStr = Settings.GetDefaultSettings().versionCode;
            
            string[] words = versionStr.Split('.');
            if (words.Length < 3)
            {
                Debug.LogError("app version format must same 1.0.0");
                return;
            }
            
            var appVersion = int.Parse(words[0] + words[1]);
            string folderPath = "Assets/Resources/Texture/LoadingBg";

            //获取指定路径下面的所有资源文件  
            if (!Directory.Exists(folderPath))
            {
                return;
            }
            DirectoryInfo direction = new DirectoryInfo(folderPath);
            FileInfo[] files = direction.GetFiles("*");//只查找本文件夹下
            // FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);//查找本文件夹和所有子文件夹
            for (int i = 0; i < files.Length; i++)
            {
                //忽略关联文件
                if (files[i].Name.EndsWith(".meta")|| files[i].Name.EndsWith(".mesh"))
                {
                    continue;
                }

                if (files[i].Name == "loading_bg_01.jpg") // default continue
                {
                    continue;
                }
                
                Debug.Log("文件名:" + files[i].Name);
                Debug.Log("文件绝对路径:" + files[i].FullName);
                Debug.Log("文件所在目录:" + files[i].DirectoryName);

                string currentFolder = files[i].Name.Replace(".", string.Empty); 
                Debug.Log("版本号:" + currentFolder);
                var cFolderVersion = int.Parse(currentFolder);
                if (cFolderVersion < appVersion)
                {
                    // delete
                    
                }
                
                
            }

        }
        
    }
}