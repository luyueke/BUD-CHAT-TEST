using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using AutoBuild;
using Game.COSXML;
using EasySpreadsheet;
using UnityEditor;
using UnityEngine;
using HybridCLR.Editor.Commands;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using xasset.editor;

namespace Game.Editor
{
    public enum UploadEnrEnum
    {
        Master,
        Alpha,
        Prod,
    }

    public enum ABQualityEnum
    {
        High,
        Low,
    }

    public class HybridCLRBuilder : OdinEditorWindow
    {
        private HybridCLRBuilderController m_HybridClrBuilderController = new HybridCLRBuilderController();
        private ResourceOpBuilderController m_ResourceOpController = new ResourceOpBuilderController();
        [ReadOnly]
        [ValueDropdown("DllPlatform")]
        public BuildTarget HotfixPlatform = BuildTarget.Android;

        private static BuildTarget[] DllPlatform = new BuildTarget[]
        {
            BuildTarget.Android, BuildTarget.iOS, BuildTarget.StandaloneWindows64, BuildTarget.StandaloneWindows,
            BuildTarget.tvOS,BuildTarget.StandaloneOSX
        };

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_ANDROID
            HotfixPlatform = BuildTarget.Android;
#elif UNITY_IOS
            HotfixPlatform = BuildTarget.iOS;
#endif
            version = Settings.GetDefaultSettings()?.versionCode;
        }

        [HorizontalGroup("ButtonGroup", Width = 0.8f)]
        [HideLabel]
        [ReadOnly]
        public string Title = "编译hotfix.dll";

        [HorizontalGroup("ButtonGroup")]
        [Button("Compile")]
        public void Compile()
        {
            CompileHotfixDll();
        }

        private void CompileHotfixDll()
         {
             CompileDllCommand.CompileDll(HotfixPlatform);
             m_HybridClrBuilderController.CopyDllAssets(HotfixPlatform);
         }





        [HorizontalGroup("ButtonGroup1", Width = 0.8f)]
        [HideLabel]
        [ReadOnly]
        public string Title1 = "刷新HotfixDll配置文件";

        [HorizontalGroup("ButtonGroup1")]
        [Button("RfOrCreate")]
        public void RfOrCreate()
        {
            HotfixDllEditor.RefreshOrCreatedHotfixDll();
        }





        [HorizontalGroup("ButtonGroup4", Width = 0.8f)]
        [HideLabel]
        [ReadOnly]
        public string Title4 = "当前平台全量打AB";

        [HorizontalGroup("ButtonGroup4")]
        [Button("BuildBundlesAll")]
        public void BuildBundlesAll()
        {
            MenuItems.BuildBundlesAll();
        }


        [HorizontalGroup("ButtonGroup3", Width = 0.6f)]
        [HideLabel]
        [ReadOnly]
        public string Title3 = "上传文件";

        [HorizontalGroup("ButtonGroup3", Width = 0.1f)]
        [HideLabel]
        public UploadEnrEnum UploadEnr = UploadEnrEnum.Master;

        [HorizontalGroup("ButtonGroup3", Width = 0.1f)]
        [HideLabel]
        public ABQualityEnum Quality = ABQualityEnum.High;

        [HorizontalGroup("ButtonGroup3", Width = 0.1f)]
        [HideLabel]
        public string version = "1.0.2";

        [PropertyTooltip("Assets/xasset/Config/Settings.asset中versionCode对应version版本")]
        [HorizontalGroup("ButtonGroup3")]
        [Button("UploadFile")]
        public void UploadFile()
        {
            string platform = "android";
#if UNITY_ANDROID
            platform = "android";
#elif UNITY_IPHONE
            platform = "ios";
#endif
            var enr = UploadEnr.ToString().ToLower();
            AutoBuildScript.UploadAppBundle(enr,version,platform, (ret) =>
            {
                if (ret == 0)
                {
                    EditorApplication.Exit(ret);
                }
            });
        }

        private string CreateRobotMessage(string enr, string platform, string ver, string remoteUrl)
        {
            var str = $"国服热更包打包完成\n构建环境:{enr}\n平台:{platform}\nVersion:{ver}\nRemoteUrl:{remoteUrl}\n打包人:{GitHelper.GetGitUserName()}\n 请重启游戏查看";
            return str;
        }


        [HorizontalGroup("ButtonGroup2", Width = 0.8f)]
        [HideLabel]
        [ReadOnly]
        public string Title2 = "编辑hotfix.dll等资源，并AB打包";

        [HorizontalGroup("ButtonGroup2", Width = 0.1f)]
        [HideLabel]
        [ValueDropdown("bundleEnum")]
        public string ClearBundle = "ClearBundle";
        private static string[] bundleEnum = { "ClearBundle","None"};

        [HorizontalGroup("ButtonGroup2")]
        [Button("BuildAll"),GUIColor(1,0.2f,0)]
        public void BuildAll()
        {
            if (ClearBundle.Equals(bundleEnum[0]))
            {
                MenuItems.ClearBundles();
            }

            Debug.Log("打包开始");
            m_ResourceOpController.StartResourceOpProcedure(UploadEnr, (result) =>
            {
                if (result == false)
                {
                    Debug.LogError("下载Resource Op表格失败");
                    return;
                }

                Debug.Log("下载Resource Op完成");
                EsMenu.GenScripts();
                EsMenu.GenData();

                CompileHotfixDll();
                HotfixDllEditor.RefreshOrCreatedHotfixDll();
                MenuItems.BuildBundlesAll();
            });
        }

        [MenuItem("Game/Build Loading Assetbundle", false, 0)]
        private static void Open()
        {
            // EditorCoroutineRunner.StartEditorCoroutine(AutoBuildScript.SetUpdateVersion("0.0.0","android","master","https://u3d-hotupdate-data-1318932159.cos.ap-beijing.myqcloud.com/AppUpdatePanel/1d1b7ea3a0921faa6024cfba01144d96"));

            // string localUrl = RunPipelineBuild(BuildTarget.Android);
            // string fileName = Path.GetFileNameWithoutExtension(localUrl);
            // var uri = $"AppUpdatePanel/master/android/{fileName}";
            // CosXmlUploadManager.UploadFileOnEditor(uri, localUrl, (url, err) =>
            // {
            //     Debug.LogError("url===" + url);
            //     // UploadImgCallback(url, err, filePath);
            // });

        }

        
        private static AssetImporter ImporterAsset(string path, string assetBundleName) {
            if (!string.IsNullOrEmpty(path)) {
                AssetImporter assetImporter = AssetImporter.GetAtPath(path);
                assetImporter.assetBundleName = assetBundleName;
                return assetImporter;
            }

            return null;
        }



        public static string RunPipelineBuild(BuildTarget target) {
            List<AssetImporter> importers = new List<AssetImporter>();
            string assetbundleName = "AppHotUpdatePanel";
            string loadingPanelPath = $"Assets/AppLanuch/AppHotUpdatePanel/{assetbundleName}.prefab";
            string abOutFolder = Application.dataPath +  $"/../LoadingBundleCache/{target.ToString()}/";
            List<string> assetPaths = new List<string> {loadingPanelPath};
            for (var i = 0; i < assetPaths.Count; i++) {
                var assetImporter = ImporterAsset(assetPaths[i], assetbundleName);
                if (assetImporter != null) {
                    importers.Add(assetImporter);
                }
            }

            foreach (var assetImporter in importers) {
                assetImporter.assetBundleVariant = GetPlatformExt(target);
            }
                
            
            if (Directory.Exists(abOutFolder)) {
                Directory.Delete(abOutFolder,true);
            }
            
            Directory.CreateDirectory(abOutFolder);
            
            BuildAssetBundleOptions abOpt = BuildAssetBundleOptions.ChunkBasedCompression;
            BuildPipeline.BuildAssetBundles(abOutFolder, abOpt, target);

            string oldFile = abOutFolder + assetbundleName + "." + GetPlatformExt(target);
            string newFile = abOutFolder + GetFileMd5(oldFile);
            File.Move(oldFile, newFile);

            foreach (var assetImporter in importers) {
                assetImporter.assetBundleVariant = null;
            }
            
            return newFile;
        }


        private static string GetPlatformExt(BuildTarget target)
        {
            return target == BuildTarget.Android ? string.Intern("a") : string.Intern("i");
        }


        private static string GetFileMd5(string filePath)
        {
            string fileMd5 = string.Empty;
            try
            {
                using (FileStream fs = File.OpenRead(filePath))
                {
                    MD5 md5 = MD5.Create();
                    byte[] fileMd5Bytes = md5.ComputeHash(fs); // 计算FileStream 对象的哈希值
                    fileMd5 = System.BitConverter.ToString(fileMd5Bytes).Replace("-", "").ToLower();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }

            return fileMd5;
        }

        [MenuItem("Game/HybridCLR Builder", false, 0)]
         private static void OpenHybridCLR()
         {
             GetWindow<HybridCLRBuilder>().Show();
         }

//         private int m_HotfixPlatformIndex;
//
//         private int HotfixPlatformIndex
//         {
//             get
//             {
// #if UNITY_ANDROID
//                 return 6;
// #elif UNITY_IOS
//                 return 5;
// #else
//                 return 0;
// #endif
//             }
//             set { m_HotfixPlatformIndex = value; }
//         }
//
//         [MenuItem("Game/HybridCLR Builder", false, 0)]
//         private static void Open()
//         {
//             HybridCLRBuilder window = GetWindow<HybridCLRBuilder>("HybridCLR Builder", true);
//             window.minSize = new Vector2(800f, 300f);
//         }
//
//         private void OnEnable()
//         {
//             m_HybridClrBuilderController = new HybridCLRBuilderController();
//             m_HotfixPlatformIndex = HotfixPlatformIndex;
//         }
//
//         private void OnGUI()
//         {
//             // Builder
//             GUILayout.Space(5f);
//             EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
//             EditorGUILayout.BeginVertical("box");
//             {
//                 int hotfixPlatformIndex = EditorGUILayout.Popup("选择hotfix平台。", m_HotfixPlatformIndex, m_HybridClrBuilderController.PlatformNames);
//                 if (hotfixPlatformIndex != m_HotfixPlatformIndex)
//                 {
//                     HotfixPlatformIndex = hotfixPlatformIndex;
//                 }
//                 GUIItem("编译hotfix.dll。", "Compile", CompileHotfixDll);
//                 GUIItem("刷新HotfixDll配置文件","RfOrCreate",HotfixDllEditor.RefreshOrCreatedHotfixDll);
//                 GUIResourcesTool();
//             }
//             EditorGUILayout.EndVertical();
//         }
//
//         private void GUIItem(string content, string button, Action onClick)
//         {
//             EditorGUILayout.BeginHorizontal();
//             {
//                 EditorGUILayout.LabelField(content);
//                 if (GUILayout.Button(button, GUILayout.Width(100)))
//                 {
//                     onClick?.Invoke();
//                 }
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//
//         private void GUIResourcesTool()
//         {
//             EditorGUILayout.BeginHorizontal();
//             {
//                 EditorGUILayout.LabelField("编辑hotfix.dll等资源，并AB打包。");
//
//                 if (GUILayout.Button("Build", GUILayout.Width(100)))
//                 {
//                     CompileHotfixDll();
//                     HotfixDllEditor.RefreshOrCreatedHotfixDll();
//                     Builder.BuildBundles(Selection.GetFiltered<Build>(SelectionMode.DeepAssets));
//                 }
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//
//
    }

    public static class EditorCoroutineRunner
    {
        private class EditorCoroutine : IEnumerator
        {
            private Stack<IEnumerator> executionStack;

            public EditorCoroutine(IEnumerator iterator)
            {
                this.executionStack = new Stack<IEnumerator>();
                this.executionStack.Push(iterator);
            }

            public bool MoveNext()
            {
                IEnumerator i = this.executionStack.Peek();

                if (i.MoveNext())
                {
                    object result = i.Current;
                    if (result != null && result is IEnumerator)
                    {
                        this.executionStack.Push((IEnumerator)result);
                    }

                    return true;
                }
                else
                {
                    if (this.executionStack.Count > 1)
                    {
                        this.executionStack.Pop();
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                throw new System.NotSupportedException("This Operation Is Not Supported.");
            }

            public object Current
            {
                get { return this.executionStack.Peek().Current; }
            }

            public bool Find(IEnumerator iterator)
            {
                return this.executionStack.Contains(iterator);
            }
        }

        private static List<EditorCoroutine> editorCoroutineList;
        private static List<IEnumerator> buffer;

        public static void StartEditorCoroutine(IEnumerator iterator)
        {
            if (editorCoroutineList == null)
            {
                // test
                editorCoroutineList = new List<EditorCoroutine>();
            }
            if (buffer == null)
            {
                buffer = new List<IEnumerator>();
            }
            if (editorCoroutineList.Count == 0)
            {
                EditorApplication.update += Update;
            }
            // add iterator to buffer first
            buffer.Add(iterator);
        }

        private static bool Find(IEnumerator iterator)
        {
            // If this iterator is already added
            // Then ignore it this time
            foreach (EditorCoroutine editorCoroutine in editorCoroutineList)
            {
                if (editorCoroutine.Find(iterator))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Update()
        {
            // EditorCoroutine execution may append new iterators to buffer
            // Therefore we should run EditorCoroutine first
            editorCoroutineList.RemoveAll
            (
                coroutine => { return coroutine.MoveNext() == false; }
            );

            // If we have iterators in buffer
            if (buffer.Count > 0)
            {
                foreach (IEnumerator iterator in buffer)
                {
                    // If this iterators not exists
                    if (!Find(iterator))
                    {
                        // Added this as new EditorCoroutine
                        editorCoroutineList.Add(new EditorCoroutine(iterator));
                    }
                }

                // Clear buffer
                buffer.Clear();
            }

            // If we have no running EditorCoroutine
            // Stop calling update anymore
            if (editorCoroutineList.Count == 0)
            {
                EditorApplication.update -= Update;
            }
        }
    }
}


