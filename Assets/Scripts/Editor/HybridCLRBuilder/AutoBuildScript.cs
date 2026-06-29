using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BestHTTP;
using Game.COSXML;
using Game.Editor;
using HybridCLR.Editor.Commands;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using xasset.editor;

#if UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;
#endif
using Debug = UnityEngine.Debug;

namespace AutoBuild
{
    public class AutoBuildScript
    {
        private ProcessStartInfo startInfo;
        public bool needSO = false;
        private HybridCLRBuilderController m_HybridClrBuilderController = new HybridCLRBuilderController();

        public static string PackageType = "";
        public AutoBuildScript()
        {
            startInfo = new ProcessStartInfo("git");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = ".";
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = false;
            startInfo.CreateNoWindow = true;
        }

        //private static void AddMqttSymbol(BuildTarget target)
        //{
        //    var targetGroup = target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;
        //    string packageTypeMqtt = ";UNITY_VERSION_S15_MQTT";
        //    var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
        //    Debug.LogError("##SwitchPackageType1 GetScriptingDefineSymbolsForGroup：" + defineSymbols);
        //    if (!defineSymbols.Contains(packageTypeMqtt))
        //    {
        //        string newSymbols = defineSymbols + packageTypeMqtt;
        //        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbols);
        //        Debug.LogError("##SwitchUnitySetting2 GetScriptingDefineSymbolsForGroup：" + newSymbols);
        //    }
        //}

        public static void SwitchUnitySetting(BuildTarget target,string ver,ref string version)
        {
            if (string.IsNullOrEmpty(ver))
            {
                return;
            }
            var urpSetting = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

            if (urpSetting == null)
            {
                return;
            }

            //var defineSymbols1=  PlayerSettings.GetScriptingDefineSymbolsForGroup(target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS);
            //Debug.LogError("##SwitchUnitySetting GetScriptingDefineSymbolsForGroup："+defineSymbols1);


            switch (ver)
            {
                case "S1":
                    urpSetting.supportsDynamicBatching = false;
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, true);
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = false;
                    }

                    version = "1.0.1";
                    break;
                case "S2":
                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = false;
                    }

                    if (PackageType == "US")
                    {
                        version = "2.3.0";
                    }
                    else
                    {
                        version = "1.0.2";
                    }

                    break;

                case "S3":
                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.3";
                    break;


                case "S10":
                    urpSetting.supportsDynamicBatching = true;
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                        PlayerSettings.SetGraphicsAPIs(target,
                            new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.12";
                    break;
                case "S11":
                    urpSetting.supportsDynamicBatching = true;
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                        PlayerSettings.SetGraphicsAPIs(target,
                            new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.13";
                    break;
                case "P1":
                    urpSetting.supportsDynamicBatching = true;
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                        PlayerSettings.SetGraphicsAPIs(target,
                            new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.15";
                    break;
                case "P2":
                    urpSetting.supportsDynamicBatching = true;
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                        PlayerSettings.SetGraphicsAPIs(target,
                            new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.16";
                    break;
                case "P5":
                    //AddMqttSymbol(target);
                    urpSetting.supportsDynamicBatching = true;
                   
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                        PlayerSettings.SetGraphicsAPIs(target,
                            new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.19";
                    break;
                case "P6":
                    urpSetting.supportsDynamicBatching = true;
                    if (target == BuildTarget.Android)
                    {
                        PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                        PlayerSettings.SetGraphicsAPIs(target,
                            new GraphicsDeviceType[] { GraphicsDeviceType.OpenGLES3 });
                    }

                    if (target == BuildTarget.iOS)
                    {
                        PlayerSettings.allowedAutorotateToLandscapeRight = true;
                    }

                    version = "1.0.20";
                    break;
            }
        }

        public static void SwitchPackageType(BuildTarget target,string packageType)
        {
            if(string.IsNullOrEmpty(packageType) || packageType != "US")return;
            string packageTypeUS = ";PACKAGE_TYPE_US";
            var targetGroup = target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;
            var defineSymbols=  PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            Debug.LogError("##SwitchPackageType GetScriptingDefineSymbolsForGroup："+defineSymbols);
            if (!defineSymbols.Contains(packageTypeUS))
            {
                string newSymbols = defineSymbols + packageTypeUS;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newSymbols);
                Debug.LogError("##SwitchPackageType SetScriptingDefineSymbolsForGroup："+newSymbols);
                // AssetDatabase.Refresh();
                // Debug.LogError("##强制重新编辑");
            }
            
            if (targetGroup == BuildTargetGroup.Android)
            {
                string usPackageName = "com.pointone.buddyglobal";
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, usPackageName);
                Debug.LogError("##修改包名："+PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
            }
        }

        public static void ShellUploadAll()
        {
            AssetDatabase.Refresh();
            String[] arguments = Environment.GetCommandLineArgs();
            var argstr = GetShellArgument(arguments, "AutoBuild.AutoBuildScript.ShellUploadAll");
            arguments = argstr.Split(' ');
            string enr = GetShellArgument(arguments, "-enr");
            string platform = GetShellArgument(arguments, "-buildTarget");
            string ver = GetShellArgument(arguments, "-version").Trim();
            string packageType = GetShellArgument(arguments, "-packageType").Trim();
            PackageType = packageType;
            CosXmlUploadManager.PackageType = packageType;
            BuildTarget target = platform.Equals("android") ? BuildTarget.Android : BuildTarget.iOS;
            Debug.LogError($"HotUpdate/{enr}/{platform}----");
            string version = Settings.GetDefaultSettings()?.versionCode.Trim();
            SwitchUnitySetting(target,ver,ref version);
            SwitchPackageType(target,packageType);




            PrebuildCommand.GenerateAll();
            CompileDllCommand.CompileDll(target);
            new HybridCLRBuilderController().CopyDllAssets(target);
            HotfixDllEditor.RefreshOrCreatedHotfixDll();
            MenuItems.BuildBundlesAll();
            UploadAppBundle(enr,version,platform, (ret) =>
            {
                if (ret == 0)
                {
                    EditorApplication.Exit(ret);
                }
            });
        }
        
        public static void UploadAppBundle(string enr,string version, string platform,Action<int> callback)
        {
            UploadPanelBundle(enr,platform, (url) =>
            {
                UploadHotBundle(enr, version, platform, url, callback);
            });
        }


        private static string GetUploadBaseUrl()
        {
            string baseUrl = "https://cdn-hotupdate.budapp.cn";
            if (PackageType == "US")
            {
                baseUrl = "https://cdn-hotupdate-global.joinbudapp.com";
                Debug.LogError("##海外服上传地址："+baseUrl);
            }
            return baseUrl;
        }

        private static void UploadPanelBundle(string enr,string platform,Action<string> success)
        {
            BuildTarget target = platform.Equals("android") ? BuildTarget.Android : BuildTarget.iOS;
            string localUrl = HybridCLRBuilder.RunPipelineBuild(target);
            string fileName = Path.GetFileNameWithoutExtension(localUrl);
            var uri = $"AppUpdatePanel/{enr}/{platform}/{fileName}";
            CosXmlUploadManager.UploadFileOnEditor(uri, localUrl, (url, err) =>
            {
                if (string.IsNullOrEmpty(err))
                {
                    Uri uri = new Uri(url);
                    string newUrl = GetUploadBaseUrl()+ uri.PathAndQuery;
                    Debug.LogError("url===" + url);
                    Debug.LogError("newUrl===" + newUrl);
                    success?.Invoke(newUrl);
                }
                else
                {
                    Debug.Log("UploadDirectoryToHotUpdate UI err:" + err);
                }
            });
        }

        private static void UploadHotBundle(string enr,string version, string platform,string panelUrl,Action<int> callback)
        {
            Debug.LogError($"HotUpdate/{enr}/{platform}/{version}");
            CosXmlUploadManager.UploadDirectoryToHotUpdate($"HotUpdate/{enr}/{platform}/{version}",
                $"Bundles/{platform}", (remotePath, err) =>
                {
                    if (string.IsNullOrEmpty(err))
                    {
                        Debug.Log("path:" + remotePath);
                        EditorCoroutineRunner.StartEditorCoroutine(SetUpdateVersion(version, platform.ToLower(),
                            enr.ToLower(),panelUrl,
                            (ret)=>
                            {
                                if (ret == 0)
                                {
                                    // LarkRobotMessageHelper.SendTextMessage(LarkRobotDefine.SUPER_CODER_3000, CreateRobotMessage(enr, platform, version, remotePath));
                                }
                                string log = ret == 0 ? "UploadAppBundle Success" : "UploadAppBundle Failed";
                                Debug.LogError(log);
                                callback?.Invoke(ret);
                            }));
                    }
                    else
                    {
                        Debug.Log("UploadDirectoryToHotUpdate err:" + err);
                        callback?.Invoke(-1);
                    }
                }, (progress) => { Debug.LogError($"progress==={progress}"); });
        }
        
        
        private class PanelDataRequest
        {
            public string panelURL;
        }

        private static string GetRecordUrl(string env)
        {
            string url = string.Empty;
            if (env == "prod")
            {
                url = "https://api.budapp.cn/resourceOp/hotUpdate/record";
            }
            else
            {
                url = "https://api-test.budapp.cn/resourceOp/hotUpdate/record";
            }

            if (PackageType == "US")
            {
                if (env == "prod")
                {
                    url = "https://global.joinbudapp.com/resourceOp/hotUpdate/record";
                }
                else
                {
                    url = "https://global.joinbudapp.com/resourceOp/hotUpdate/record";
                }
                Debug.LogError("##海外GetRecordUrl地址："+url);
            }

           
           

            return url;
        }

        public static IEnumerator SetUpdateVersion(string version, string mobile, string env,string pUrl,Action<int> callback = null)
        {
            string url = GetRecordUrl(env);
            HTTPRequest request = new HTTPRequest(new Uri(url), HTTPMethods.Post);
            request.SetHeader("Content-Type", "application/json");
            request.SetHeader("environment", env);
            request.SetHeader("platform", "U3D");
            request.SetHeader("mobile", mobile);
            request.SetHeader("version", version ?? "0.1.0");

            var arg = new PanelDataRequest()
            {
                panelURL = pUrl
            };
            request.RawData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arg));
            yield return request.Send();
            if (request.Exception != null)
            {
                Debug.Log("请求异常" + request.Exception.Message);
                callback?.Invoke(-1);
                yield break;
            }

            if (request.Response == null)
            {
                Debug.Log("请求异常 request.Response is null");
                callback?.Invoke(-1);
                yield break;
            }
            if (request.Response.IsSuccess)
            {
                JObject success = JObject.Parse(request.Response.DataAsText);
                if (success["result"].Value<int>() == 0)
                {
                    Debug.Log($"设置版本号成功{request.Response.DataAsText}");
                    callback?.Invoke(0);
                }
                else
                {
                    Debug.LogError($"设置版本号失败{request.Response.DataAsText}");
                    callback?.Invoke(-1);
                }
            }
            else
            {
                Debug.LogError($"设置版本号失败{request.Response.Context}");
                callback?.Invoke(-1);
            }
        }


        // private static IEnumerator SetUpdateVersion(JObject jo)
        // {
        //     var url = "https://api-test.pointone.tech/configuration/hotUpdate";
        //
        //     Dictionary<string, string> postHeader = new Dictionary<string, string>();
        //     postHeader.Add("Content-Type", "application/json");
        //     postHeader.Add("environment", "master");
        //     postHeader.Add("device", "");
        //     postHeader.Add("platform", "U3D");
        //     postHeader.Add("version", jo["version"].Value<string>()??"0.0.1");
        //
        //     byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jo));
        //
        //     WWW www = new WWW(url, data, postHeader);
        //     yield return www;
        //     if (www.error == null)
        //     {
        //         JObject success = JObject.Parse(www.text);
        //         if (success["result"].Value<int>() == 0)
        //         {
        //             Debug.Log($"设置版本号成功{www.text}");
        //         }
        //         else
        //         {
        //             Debug.LogError($"设置版本号失败{www.text}");
        //         }
        //     }
        //     else
        //     {
        //
        //         Debug.LogError($"设置版本号失败{www.error}");
        //     }
        // }
        
        [MenuItem("BudTools/测试上传IOS")]
        public static void TestUploadIOS()
        {
            string enr = "us_master";
            var version = Settings.GetDefaultSettings()?.versionCode.Trim();
            string platform = "ios";
            UploadAppBundle(enr,version,platform, (ret) =>
            {
                LoggerUtils.LogError("####上传结果："+ret);
            });
        }

        public static void ShellBulidAll()
        {
            var autoBuildScript = new AutoBuildScript();
            string path, savePath, branchName, androidPath, androidBranch, iosPath, iosBranch, commitMsg, buildTarget = null, buildAB;
            String[] arguments = Environment.GetCommandLineArgs();
            path = GetShellArgument(arguments, "-projectPath");
            var argstr = GetShellArgument(arguments, "AutoBuild.AutoBuildScript.ShellBulidAll");
            arguments = argstr.Split(' ');
            savePath = GetShellArgument(arguments, "-savePath");
            branchName = GetShellArgument(arguments, "-branchName");
            buildTarget = GetShellArgument(arguments, "-buildTarget");
            buildTarget = string.IsNullOrEmpty(buildTarget) ? "all" : buildTarget;
            
            string ver = GetShellArgument(arguments, "-version").Trim();
            string packageType = GetShellArgument(arguments, "-packageType").Trim();
            PackageType = packageType;
            CosXmlUploadManager.PackageType = packageType;
            BuildTarget target = buildTarget.Equals("android") ? BuildTarget.Android : BuildTarget.iOS;

            string version = Settings.GetDefaultSettings()?.versionCode.Trim();
            SwitchUnitySetting(target,ver,ref version);
            SwitchPackageType(target,packageType);
            
            // if (string.IsNullOrEmpty(branchName))
            // {
            //     branchName = autoBuildScript.GetNowBranch(path);
            // }
            // else
            // {
            //     autoBuildScript.SwitchBranch(branchName, path, true);
            //     AssetDatabase.Refresh();
            // }

            string projectPath = "";
            if (buildTarget == "android" || buildTarget == "all")
            {
                androidPath = GetShellArgument(arguments, "-androidPath");
                // androidBranch = GetShellArgument(arguments, "-androidBranch");
                // if (string.IsNullOrEmpty(androidBranch))
                // {
                //     androidBranch = autoBuildScript.GetNowBranch(androidPath);
                // }
                // else
                // {
                //     autoBuildScript.SwitchBranch(androidBranch, androidPath, true);
                // }

                bool.TryParse(GetShellArgument(arguments, "-needSO"), out autoBuildScript.needSO);

                projectPath = autoBuildScript.Build(BuildTarget.Android, branchName, string.Empty, savePath);
                if (string.IsNullOrEmpty(projectPath))
                {
                    Debug.LogError("Android Path Null");
                    EditorApplication.Exit(-1);
                }
                autoBuildScript.AndroidMoveAllToProject(projectPath, androidPath);

                string enr = "master";
                if (branchName.Contains("prod"))
                {
                    enr = "prod";
                }
                
                Debug.LogError("###当前enr:"+enr);
                UploadAppBundle(enr,version,"android", (ret) =>
                {
                    if (ret == 0)
                    {
                        EditorApplication.Exit(ret);
                    }
                });

            }

            if (buildTarget == "ios" || buildTarget == "all")
            {
                iosPath = GetShellArgument(arguments, "-iosPath");
                // iosBranch = GetShellArgument(arguments, "-iosBranch");
                // if (string.IsNullOrEmpty(iosBranch))
                // {
                //     iosBranch = autoBuildScript.GetNowBranch(iosPath);
                // }
                // else
                // {
                //     autoBuildScript.SwitchBranch(iosBranch, iosPath, true);
                // }

                projectPath = autoBuildScript.Build(BuildTarget.iOS, branchName, string.Empty, savePath);
                if (string.IsNullOrEmpty(projectPath))
                {
                    Debug.LogError("IOS Path Null");

                    EditorApplication.Exit(-1);
                }
                autoBuildScript.EditXcode(projectPath);
                autoBuildScript.CopyFramework(projectPath ,iosPath);
                autoBuildScript.IOSMoveToProject(projectPath, iosPath);
                string enr = "master";
                if (branchName.Contains("prod"))
                {
                    enr = "prod";
                }
                Debug.LogError("###当前enr:"+enr);
                
                UploadAppBundle(enr,version,"ios", (ret) =>
                {
                    Debug.LogError("####IOS 上传回调："+ret);
                    EditorApplication.Exit(ret);
                });
            }
        }

        private static string GetShellArgument(String[] arguments, string key)
        {
            int index = Array.IndexOf(arguments, key);
            if (index != -1)
            {
                return index + 1 > arguments.Length ? string.Empty : arguments[index + 1];
            }

            return string.Empty;
        }

        private void CopyDirectory(string sourcePath, string destPath, string rename = "")
        {
            string floderName = Path.GetFileName(string.IsNullOrEmpty(rename) ? sourcePath : rename);
            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(destPath, floderName));
            string[] files = Directory.GetFileSystemEntries(sourcePath);

            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    CopyDirectory(file, di.FullName);
                }
                else
                {
                    File.Copy(file, Path.Combine(di.FullName, Path.GetFileName(file)), true);
                }
            }
        }

        private static void MoveDirectory(string sourcePath, string destPath)
        {
            if (!Directory.Exists(sourcePath)) return;
            string[] files = Directory.GetFileSystemEntries(sourcePath);


            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    MoveDirectory(file, file.Replace(sourcePath, destPath));
                }
                else
                {
                    Debug.Log(file + "--" + file.Replace(sourcePath, destPath));
                    var directory = Path.GetDirectoryName(file.Replace(sourcePath, destPath));
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    FileUtil.CopyFileOrDirectory(file, file.Replace(sourcePath, destPath));
                }
            }

            Directory.Delete(sourcePath, true);
        }

        public bool GitAddCommitPushAll(string workingDirectory, string commitMsg)
        {
            bool success = false;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardError = true;
            Process process = new Process();
            process.StartInfo = startInfo;
            startInfo.Arguments = "add -A .";
            process.Start();
            process.WaitForExit();
            startInfo.Arguments = "commit -m " + "\"" + commitMsg + "\"";
            process.Start();
            process.WaitForExit();
            startInfo.Arguments = "push";
            process.Start();
            process.WaitForExit();
            success = process.StandardError.ReadToEnd().Length <= 0;
            process.Close();
            startInfo.RedirectStandardError = false;
            return success;
        }

        public void GitResetHead(string workingDirectory, int step = 1, bool hard = true)
        {
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.RedirectStandardOutput = false;
            Process process = new Process();
            process.StartInfo = startInfo;
            startInfo.Arguments = "git reset head~" + step + (hard ? " --hard" : "");
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        public void GitAddTagAndPush(string workingDirectory, string tag)
        {
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.RedirectStandardOutput = false;
            Process process = new Process();
            process.StartInfo = startInfo;
            startInfo.Arguments = "tag " + tag;
            process.Start();
            process.WaitForExit();
            startInfo.Arguments = "push origin " + tag;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        public void AndroidMoveToProject(string source, string dest)
        {
            string path = Path.Combine(dest, "module_unity", "src", "main");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string libPath = Path.Combine(dest, "module_unity", "libs");
            if (!Directory.Exists(libPath))
            {
                Directory.CreateDirectory(libPath);
            }

            var main = Path.Combine("unityLibrary", "src", "main");
            var destMain = Path.Combine("module_unity", "src", "main");
            var uc = "unity-classes.jar";
            File.Copy(Path.Combine(source, "unityLibrary", "libs", uc),
                Path.Combine(libPath, uc), true);

            var assets = "assets";
            MoveDirectory(Path.Combine(source, main, assets, "Bundles1"), Path.Combine(source, main, assets, "Bundles"));
            MoveDirectory(Path.Combine(source, main, assets, "Bundles2"), Path.Combine(source, main, assets, "Bundles"));
            var destAssets = Path.Combine(dest, destMain, assets);
            if (Directory.Exists(destAssets)) Directory.Delete(destAssets, true);
            CopyDirectory(Path.Combine(source, main, assets), Path.Combine(dest, destMain));
            var Il2CppOutputProject = "Il2CppOutputProject";
            var destDir = Path.Combine(dest, destMain, Il2CppOutputProject);
            if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
            CopyDirectory(Path.Combine(source, main, Il2CppOutputProject), Path.Combine(dest, destMain));
            var jniStaticLibs = "jniStaticLibs";
            destDir = Path.Combine(dest, destMain, jniStaticLibs);
            if (Directory.Exists(destDir)) Directory.Delete(destDir, true);
            CopyDirectory(Path.Combine(source, main, jniStaticLibs), Path.Combine(dest, destMain));
            var jniLibs = "jniLibs";
            CopyDirectory(Path.Combine(source, main, jniLibs), Path.Combine(dest, destMain));
        }

        public void AndroidMoveAllToProject(string source, string dest)
        {
            DirectoryInfo pathInfo = new DirectoryInfo(dest);
            var assets = Path.Combine(dest, "unityLibrary", "src", "main", "assets");
            if (Directory.Exists(assets)) Directory.Delete(assets, true);
            var il2cpp = Path.Combine(dest, "unityLibrary", "src", "main", "Il2CppOutputProject", "Source", "il2cppOutput");
            if (Directory.Exists(il2cpp)) Directory.Delete(il2cpp, true);
            CopyDirectory(source, pathInfo.Parent.FullName, Path.GetFileName(dest));
            if (needSO) CopyDirectory(Path.Combine("Temp", "StagingArea", "symbols"), source);
        }

        public void AndroidZipToProject(string source, string dest)
        {
            FastZip zip = new FastZip();
            zip.CreateZip(Path.Combine(dest, "ASproject.zip"),
                source, true, "");
        }

        public void IOSMoveToProject(string source, string dest)
        {
            // if (Directory.Exists(dest))
            // {
            //     Directory.Delete(dest,true);
            // }
            // FileUtil.CopyFileOrDirectory(source,dest);
            DirectoryInfo pathInfo = new DirectoryInfo(dest);
            var raw = Path.Combine(dest, "Data", "Raw");
            if (Directory.Exists(raw)) Directory.Delete(raw, true);
            CopyDirectory(source, pathInfo.Parent.FullName, Path.GetFileName(dest));
        }

        public string GetNowBranch(string workingDirectory = ".")
        {
            var startInfo = new ProcessStartInfo("git");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.CreateNoWindow = true;

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.RedirectStandardOutput = true;
            startInfo.Arguments = "rev-parse --abbrev-ref HEAD";
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            char[] linecharsToTrim = {'\r', '\n'};
            var name = process.StandardOutput.ReadToEnd().Trim(linecharsToTrim);
            process.Close();
            return name;
        }

        public string[] GetBranchArray(string workingDirectory = ".")
        {
            var startInfo = new ProcessStartInfo("git");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.CreateNoWindow = true;

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.RedirectStandardOutput = true;
            startInfo.Arguments = "branch -a";
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            string branchnames = process.StandardOutput.ReadToEnd();
            process.Close();
            var branchArray = branchnames.Split(Environment.NewLine.ToCharArray());
            char[] charsToTrim = {'*', ' '};
            for (int i = 0; i < branchArray.Length; i++)
            {
                var str = branchArray[i];
                branchArray[i] = str.TrimStart(charsToTrim);
            }

            return branchArray;
        }

        public void SwitchBranch(string name, string workingDirectory = ".", bool needPull = false)
        {
            StringBuilder cmd = new StringBuilder();
            cmd.Append("checkout ");
            if (name.StartsWith("remotes/"))
            {
                name = name.Replace("remotes/", "");
                cmd.Append("-f -b ").Append(name);
            }
            else
            {
                cmd.Append("-f ").Append(name);
            }

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.RedirectStandardOutput = false;
            startInfo.Arguments = cmd.ToString();
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            if (needPull)
            {
                startInfo.Arguments = "pull --all";
                process.Start();
                process.WaitForExit();
            }

            process.Close();
        }

        public void CopyFramework(string org, string des)
        {
//#if UNITY_EDITOR_OSX
//            var xcworkDir = Path.Combine(org, "Unity-iPhone.xcodeproj", "project.xcworkspace");
//            var desXcworkPath = Path.Combine(des, "Unity-iPhone.xcodeproj", "project.xcworkspace");
//            if (Directory.Exists(desXcworkPath))
//            {
//                Directory.Delete(desXcworkPath,true);
//                Directory.CreateDirectory(desXcworkPath);
//            }
//            FileUtil.CopyFileOrDirectory(xcworkDir, desXcworkPath);

//            var tempDic = Path.Combine(org, "Unity-iPhone.xcodeproj", "xcuserdata");
//            var desDir = Path.Combine(des, "Unity-iPhone.xcodeproj", "xcuserdata");
//            if (Directory.Exists(desDir))
//            {
//                Directory.Delete(desDir, true);
//                Directory.CreateDirectory(desDir);
//            }

//            FileUtil.CopyFileOrDirectory(tempDic, desDir);
//#endif
        }

        public void EditXcode(string path)
        {
#if UNITY_EDITOR_OSX
            var ppath = Path.Combine(path, "Unity-iPhone.xcodeproj", "project.pbxproj");

            FileStream file = new FileStream(ppath, FileMode.Open);
            StreamReader sr = new StreamReader(file);
            var str = sr.ReadToEnd();
            str = str.Replace(@"/* UnitySendToIosCallProxy.h */; };",
                @"/* UnitySendToIosCallProxy.h */; settings = {ATTRIBUTES = (Public, ); }; };");
            
            str = str.Replace(@"/* NativeShare-Bridging-Header.h */; };",
                @"/* NativeShare-Bridging-Header.h */; settings = {ATTRIBUTES = (Public, ); }; };");

            str = str.Replace(@"GCC_OPTIMIZATION_LEVEL = 0",
                @"GCC_OPTIMIZATION_LEVEL = s");

            sr.Close();
            file.Close();

            PBXProject project = new PBXProject();
            project.ReadFromString(str);
            var frameTarget = project.GetUnityFrameworkTargetGuid();
            var resourceTarget = project.GetResourcesBuildPhaseByTarget(frameTarget);
            var resGUID = project.FindFileGuidByProjectPath("Data");
            project.AddFileToBuildSection(frameTarget, resourceTarget, resGUID);
            string target = project.TargetGuidByName("UnityFramework");
            project.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            File.WriteAllText(ppath, project.WriteToString());
#endif
        }


        private void CompileHotfixDll(BuildTarget target)
        {
            PrebuildCommand.GenerateAll();
            CompileDllCommand.CompileDll(target);
            m_HybridClrBuilderController.CopyDllAssets(target);
        }

        public string Build(BuildTarget target, string gitBranch, string version, string path)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(target);
            }

            CompileHotfixDll(target);
            HotfixDllEditor.RefreshOrCreatedHotfixDll();
            MenuItems.BuildBundlesAll();
            AssetDatabase.Refresh();

            PlayerSettings.defaultInterfaceOrientation = target == BuildTarget.Android ? UIOrientation.LandscapeLeft : UIOrientation.AutoRotation;
            var bTarget = target == BuildTarget.Android?BuildTargetGroup.Android:BuildTargetGroup.iOS;
            var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(bTarget);
            Debug.LogError("##Build GetScriptingDefineSymbolsForGroup："+defineSymbols);
            if (!defineSymbols.Contains(";UNITY_STANDARD_BUILD"))
            {
                string newSymbols = defineSymbols.Replace(";UNITY_STANDARD_BUILD","");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(bTarget, newSymbols);
            }

            List<string> levels = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                levels.Add(scene.path);
            }

            StringBuilder pname = new StringBuilder();
            pname.Append(gitBranch.Replace("/", "_")).Append('_');
            if (!string.IsNullOrEmpty(version))
            {
                pname.Append(version).Append('_');
            }

            var name = (BuildTarget.iOS == target ? "XC_" : "AS_") + pname + DateTime.Now.ToString("yy-MM-dd;HH.mm.ss");
            //path = new DirectoryInfo(folderPath).FullName;
            path = Path.Combine(path, name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            PlayerSettings.SetPropertyInt("ScriptingBackend", (int) ScriptingImplementation.IL2CPP, target);
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.show = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

    
            BuildOptions ops = BuildOptions.None;
            if(target == BuildTarget.Android)
            {
                ops |= BuildOptions.CompressWithLz4;
                if (gitBranch.Contains("master"))
                {
                    ops |= BuildOptions.Development;
                    ops |= BuildOptions.EnableDeepProfilingSupport;
                }              
            }
     
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = levels.ToArray(),
                locationPathName = path,
                target = target,
                options = ops,
            };
            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);


            if (buildResult.summary.result == BuildResult.Failed)
            {
                path = "";
            }
            return path;
        }

        public string HMACSHA256AndBase64(string key)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            byte[] messageBytes = new byte[] { };
            using (var hmacSHA256 = new HMACSHA256(keyByte))
            {
                byte[] hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashMessage);
            }
        }


        #region 先导出子模块git哈希值

        private const string masterCmd = "rev-parse origin/master";
        private const string alphaCmd = "rev-parse origin/alpha";
        private const string prodCmd = "rev-parse origin/prod";
        private const string submodulePath ="Assets/OtherLibrary/BudEngine";
        public void FillAllRepositoryGitHash(VersionInfo verInfo)
        {
            VersionInfo info;
            string trunk = $"{@Directory.GetCurrentDirectory()}";
            string submodule = $"{@Directory.GetCurrentDirectory()}/{submodulePath}/";
            FillRepositoryGitHash(verInfo.trunk,trunk);
            FillRepositoryGitHash(verInfo.submodules,submodule);
        }

        public void FillRepositoryGitHash(VersionObj verObj,string workDir)
        {
            verObj.master= Run(masterCmd,workDir);
            verObj.alpha=Run(alphaCmd,workDir);
            verObj.prod= Run(prodCmd,workDir);
        }
        public string Run(string arg,string workDir)
        {
            string ret = "";
            var info = new ProcessStartInfo("git",arg)
                       {
                           CreateNoWindow = true,
                           RedirectStandardOutput = true,
                           UseShellExecute = false,
                           WorkingDirectory = workDir,
                       };
            var process = new Process
                          {
                              StartInfo = info,
                          };
            process.Start();
            ret=process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return ret;
        }

        #endregion
    }
}
