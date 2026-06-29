using System;
using System.IO;
using System.Linq;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;


namespace Game.Editor
{
    [Flags]
    public enum Platform : int
    {
        Undefined = 0,

        /// <summary>
        /// Windows 32 位。
        /// </summary>
        Windows = 1 << 0,

        /// <summary>
        /// Windows 64 位。
        /// </summary>
        Windows64 = 1 << 1,

        /// <summary>
        /// macOS。
        /// </summary>
        MacOS = 1 << 2,

        /// <summary>
        /// Linux。
        /// </summary>
        Linux = 1 << 3,

        /// <summary>
        /// iOS。
        /// </summary>
        IOS = 1 << 4,

        /// <summary>
        /// Android。
        /// </summary>
        Android = 1 << 5,

        /// <summary>
        /// Windows Store。
        /// </summary>
        WindowsStore = 1 << 6,

        /// <summary>
        /// WebGL。
        /// </summary>
        WebGL = 1 << 7,
    }
    public class HybridCLRBuilderController
    {
        private const string HotfixDllPath = "Assets/Arts/HybridCLR/Dlls";
        private const string AOTDllPath = "Assets/Arts/HybridCLR/BaseDlls";

        public static readonly string[] AOTDllNames =
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll", // 如果使用了Linq，需要这个
        };


        public string[] PlatformNames { get; }

        public HybridCLRBuilderController()
        {
            PlatformNames = Enum.GetNames(typeof(Platform));
        }

        /// <summary>
        /// 由 UnityGameFramework.Editor.ResourceTools.Platform 得到 BuildTarget。
        /// </summary>
        /// <param name="platformIndex"></param>
        /// <returns>BuildTarget。</returns>
        public BuildTarget GetBuildTarget(int platformIndex)
        {
            Platform platform = (Platform) Enum.Parse(typeof(Platform), PlatformNames[platformIndex]);
            switch (platform)
            {
                case Platform.Windows:
                    return BuildTarget.StandaloneWindows;

                case Platform.Windows64:
                    return BuildTarget.StandaloneWindows64;

                case Platform.MacOS:
                    return BuildTarget.StandaloneOSX;
                case Platform.Linux:
                    return BuildTarget.StandaloneLinux64;

                case Platform.IOS:
                    return BuildTarget.iOS;

                case Platform.Android:
                    return BuildTarget.Android;

                case Platform.WindowsStore:
                    return BuildTarget.WSAPlayer;

                case Platform.WebGL:
                    return BuildTarget.WebGL;

                default:
                    throw new ArgumentException("Platform is invalid.");
            }
        }

        /// <summary>
        /// 将 dll 文件拷贝至项目目录，用于 GameFramework 资源模块的编辑和打包。
        /// </summary>
        /// <param name="buildTarget"></param>
        public void CopyDllAssets(BuildTarget buildTarget)
        {

            if (!Directory.Exists(HotfixDllPath))
            {
                Directory.CreateDirectory(HotfixDllPath);
            }

            if (!Directory.Exists(AOTDllPath))
            {
                Directory.CreateDirectory(AOTDllPath);
            }

            string importSuffix = ".bytes";

            // Copy Hotfix Dll
            var hotfixDllNames =HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions.Select(x => x.name).ToArray();
            foreach (var hotfixDll in hotfixDllNames)
            {
                string oriFileName = Path.Combine(SettingsUtil.GetHotUpdateDllsOutputDirByTarget(buildTarget), hotfixDll+".dll");
                string desFileName = Path.Combine(HotfixDllPath, hotfixDll + importSuffix);
                File.Copy(oriFileName, desFileName, true);
            }

            // Copy AOT Dll
            string aotDllPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(buildTarget);
            foreach (var dllName in AOTDllNames)
            {
                var aotOriFileName = Path.Combine(aotDllPath, dllName);
                if (!File.Exists(aotOriFileName))
                {
                    Debug.LogError($"AOT 补充元数据 dll: {aotOriFileName} 文件不存在。需要构建一次主包后才能生成裁剪后的 AOT dll.");
                    continue;
                }
                var aotDesFileName = Path.Combine(AOTDllPath, dllName + importSuffix);
                File.Copy(aotOriFileName, aotDesFileName, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
