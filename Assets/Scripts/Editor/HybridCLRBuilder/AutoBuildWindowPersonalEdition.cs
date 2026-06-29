using System;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace AutoBuild
{
    public class AutoBuildWindowPersonalEdition : EditorWindow
    {
        [MenuItem("Game/AutoBuildWindow 个人版", false, 0)]
        static void Open()
        {
            EditorWindow.GetWindow<AutoBuildWindowPersonalEdition>("Build Project");
        }

        private string version = string.Empty;
        private string[] gitBranchArray = new[] {"空"};
        private string gitBranchName = string.Empty;
        private int gitBranchIndex = -1;
        private string projectSavePath = string.Empty;

        private string[] androidBranchArray = new[] {"空"};
        private string androidBranchName = string.Empty;
        private int androidBranchIndex = -1;
        private string androidPath = string.Empty;

        private string[] iosBranchArray = new[] {"空"};
        private string iosBranchName = string.Empty;
        private int iosBranchIndex = -1;
        private string iosPath = string.Empty;

        private bool moveToProject = true;
        private bool PushToGit = false;
        private bool resetProject = false;
        private bool includeAllAssets = true;

        private Thread gitThread;
        private Thread ASgitThread;
        private Thread XCgitThread;

        private const string buildPathPPKey = "buildPath";
        private const string iosPathPPKey = "iosPathPPKey";
        private const string androidPathPPKey = "androidPathPPKey";

        private ProcessStartInfo startInfo;
        private AutoBuildScript autoBuildScript;

        private void OnEnable()
        {
            projectSavePath = PlayerPrefs.GetString(buildPathPPKey, projectSavePath);
            androidPath = PlayerPrefs.GetString(androidPathPPKey, androidPath);
            iosPath = PlayerPrefs.GetString(iosPathPPKey, iosPath);
            autoBuildScript = new AutoBuildScript();
            GetGitInfo();
            GetIOSGitInfo();
            GetAndroidGitInfo();
        }

        private void GetGitInfo()
        {
            if (gitThread != null && this.gitThread.IsAlive) return;

            gitThread = new Thread(() =>
            {
                gitBranchName = autoBuildScript.GetNowBranch();
                gitBranchArray = autoBuildScript.GetBranchArray();
                gitBranchIndex = Array.IndexOf(gitBranchArray, gitBranchName);
            });
            gitThread.Start();
        }

        private void GetAndroidGitInfo()
        {
            if (string.IsNullOrEmpty(androidPath)) return;
            if (ASgitThread != null && this.ASgitThread.IsAlive) return;

            ASgitThread = new Thread(() =>
            {
                androidBranchName = autoBuildScript.GetNowBranch(androidPath);
                androidBranchArray = autoBuildScript.GetBranchArray(androidPath);
                androidBranchIndex = Array.IndexOf(androidBranchArray, androidBranchName);
            });
            ASgitThread.Start();
        }

        private void GetIOSGitInfo()
        {
            if (string.IsNullOrEmpty(iosPath)) return;
            if (XCgitThread != null && this.XCgitThread.IsAlive) return;

            XCgitThread = new Thread(() =>
            {
                iosBranchName = autoBuildScript.GetNowBranch(iosPath);
                iosBranchArray = autoBuildScript.GetBranchArray(iosPath);
                iosBranchIndex = Array.IndexOf(iosBranchArray, iosBranchName);
            });
            XCgitThread.Start();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("刷新git"))
            {
                if (gitThread != null && gitThread.IsAlive) gitThread.Abort();
                if (ASgitThread != null && ASgitThread.IsAlive) ASgitThread.Abort();
                if (XCgitThread != null && XCgitThread.IsAlive) XCgitThread.Abort();

                GetGitInfo();
                GetAndroidGitInfo();
                GetIOSGitInfo();
            }

            GUILayout.Space(10);
            GUILayout.Label("unity当前分支：" + gitBranchName);
            GUILayout.Space(10);
            if (GUILayout.Button("选择保存路径"))
            {
                projectSavePath = EditorUtility.OpenFolderPanel("选择保存路径", "", "");
                PlayerPrefs.SetString(buildPathPPKey, projectSavePath);
                PlayerPrefs.Save();
            }

            string defultPath = Application.dataPath+ "/../../ExportProject/";
            projectSavePath = string.IsNullOrEmpty(projectSavePath) ? defultPath : projectSavePath;
            GUILayout.Label(string.IsNullOrEmpty(projectSavePath) ? "路径：" : "路径：" + projectSavePath);
            GUILayout.Space(10);

            StringBuilder pname = new StringBuilder();
            pname.Append(gitBranchName.Replace("/", "_")).Append('_');
            if (!string.IsNullOrEmpty(version))
            {
                pname.Append(version).Append('_');
            }

            GUILayout.Label("工程名： " + "(XC/AS)_" + pname + DateTime.Now.ToString("yy-MM-dd.HH:mm:ss"));
            
            moveToProject = GUILayout.Toggle(moveToProject, "覆盖至目标工程");
            GUILayout.Space(10);

            if (GUILayout.Button("选择Android工程路径"))
            {
                androidPath = EditorUtility.OpenFolderPanel("选择Android工程路径", "", "");
                PlayerPrefs.SetString(androidPathPPKey, androidPath);
                PlayerPrefs.Save();
                GetAndroidGitInfo();
            }

            GUILayout.Space(10);
            GUILayout.Label("Android工程路径: " + androidPath);
            if (androidBranchIndex >= 0)
            {
                GUILayout.Label("Android工程git分支: " + androidBranchName);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("选择IOS工程路径"))
            {
                iosPath = EditorUtility.OpenFolderPanel("选择IOS工程路径", "", "");
                PlayerPrefs.SetString(iosPathPPKey, iosPath);
                PlayerPrefs.Save();
                GetIOSGitInfo();
            }

            GUILayout.Space(10);
            GUILayout.Label("IOS工程路径: " + iosPath);
            if (iosBranchIndex >= 0)
            {
                GUILayout.Label("IOS工程git分支: " + iosBranchName);
            }

            GUILayout.Space(10);

            includeAllAssets = GUILayout.Toggle(includeAllAssets, "AB资源全部打入包内");

            GUILayout.Space(10);
            if (GUILayout.Button("出Android"))
            {
                BuildAndroid();
            }

            if (GUILayout.Button("出IOS"))
            {
                BuildIOS();
            }
            
            if (GUILayout.Button("我都要"))
            {
                BuildAndroid();
                BuildIOS();
            }
        }

        void BuildAndroid()
        {
            if (!Directory.Exists(projectSavePath))
            {
                Directory.CreateDirectory(projectSavePath);
            }

            if (includeAllAssets)
            {
                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
                if (!defineSymbols.Contains("LOCAL_BUILD"))
                {
                    string newSymbols = defineSymbols + ";LOCAL_BUILD";
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, newSymbols);
                }
            }
            var settings = xasset.editor.Settings.GetDefaultSettings();
            settings.playerAssetsSplitMode = includeAllAssets ? xasset.editor.PlayerAssetsSplitMode.IncludeAllAssets : xasset.editor.PlayerAssetsSplitMode.SplitByAssetPacksWithInstallTime;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            var path = autoBuildScript.Build(BuildTarget.Android, gitBranchName, version, projectSavePath);
            if (!string.IsNullOrEmpty(androidPath))
            {
                if (moveToProject)
                {
                    autoBuildScript.AndroidMoveToProject(path, androidPath);
                }

                ShowUnityDataSize(path);
            }

            Debug.LogWarning("BuildAndroid成功");
        }
        
        void BuildIOS()
        {
            if (!Directory.Exists(projectSavePath))
            {
                Directory.CreateDirectory(projectSavePath);
            }
            
            var path = autoBuildScript.Build(BuildTarget.iOS, gitBranchName, version, projectSavePath);
            Debug.LogError("path="+path);
            autoBuildScript.EditXcode(path);
            // if (!string.IsNullOrEmpty(iosPath))
            // {
            //     if (moveToProject)
            //     {
            //         autoBuildScript.IOSMoveToProject(path, iosPath);
            //     }
            // }
            Debug.LogWarning("BuildIOS成功");
        }
        


        void ShowUnityDataSize(string androidPath)
        {
            string dataRealPath = "unityLibrary\\src\\main\\assets\\bin\\Data\\data.unity3d";
            string dataFullPath = Path.Combine(androidPath, dataRealPath);

            FileInfo fileInfo = new FileInfo(dataFullPath);

            if (fileInfo.Exists)
            {
                Debug.LogError($"data.unity3d file size : {(fileInfo.Length / 1024f / 1024f).ToString("f1")}MB");
            }
        }

        private void OnDisable()
        {
            if (gitThread != null && gitThread.IsAlive) gitThread.Abort();
            if (ASgitThread != null && ASgitThread.IsAlive) ASgitThread.Abort();
            if (XCgitThread != null && XCgitThread.IsAlive) XCgitThread.Abort();
        }
    }
}