using System.Collections;
using System.IO;
using Es;
using Game.COSXML;
using Game.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Networking;

public class OtherTools
{
    [MenuItem("BudTools/打开Proto目录")]
    public static void OpenPbFolder()
    {
        EditorUtility.RevealInFinder($"{Application.dataPath.Replace("/Assets", "")}/OtherTools/Protocol/");
    }

    [MenuItem("BudTools/ClearPlayerPrefs")]
    public static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

    [MenuItem("BudTools/上传模版封面")]
    public static void UploadTemplateCover() {
        EditorCoroutineRunner.StartEditorCoroutine(UploadTemplateCoverEnumerator());
    }

    // private static Enumerat

    private static bool IsExist(string remotePath) {
        if (remotePath.StartsWith("http://") || remotePath.StartsWith("https://")) {
            var request = UnityWebRequest.Head(remotePath);
            request.SendWebRequest();
            while (!request.isDone) {

            }
            return request.result == UnityWebRequest.Result.Success;
        }
        return File.Exists(remotePath);
    }

    private static IEnumerator UploadTemplateCoverEnumerator() {
        yield return null;
        Es.DataTables _dataTables = new Es.DataTables();
        _dataTables.Load(new EsDataLoader());
        var clothesTemplateList = DataTables.GetClothesTemplateList();
        EditorUtility.DisplayProgressBar("上传模版封面", "正在上传模版封面", 0);
        for (int i = 0; i < clothesTemplateList.Count; i++) {
            EditorUtility.DisplayProgressBar("上传模版封面", $"正在上传衣服模版封面 {i} / {clothesTemplateList.Count}", i*1.0f / clothesTemplateList.Count);
            var clothesTemplate = clothesTemplateList[i];
            var localCover = $"Assets/Arts/Avatar/UGCRolePart/UGCAvatarIcon/{clothesTemplate.Cover}.png";
            if (!File.Exists(localCover)) {
                LoggerUtils.LogError("localCover not exist:" + localCover);
                yield return null;
                continue;
            }
            var remoteCover = CosXmlUploadManager.GetBusinessRootUrl() + "/" + clothesTemplate.RemoteCover;
            if (!IsExist(remoteCover)) {
                LoggerUtils.LogError("update cover:" + remoteCover);
                CosXmlUploadManager.UploadFile(remoteCover, localCover);
            }
            yield return null;
        }
        var petTemplateList = DataTables.GetPetClothesTemplateList();
        for (int i = 0; i < petTemplateList.Count; i++) {
            EditorUtility.DisplayProgressBar("上传模版封面", $"正在宠物衣服模版封面 {i} / {petTemplateList.Count}", i*1.0f / petTemplateList.Count);
            var clothesTemplate = petTemplateList[i];
            var localCover = $"Assets/Arts/Pet/UGCRolePart/UGCAvatarIcon/{clothesTemplate.Cover}.png";
            if (!File.Exists(localCover)) {
                LoggerUtils.LogError("localCover not exist:" + localCover);
                yield return null;
                continue;
            }
            var remoteCover = CosXmlUploadManager.GetBusinessRootUrl() + "/" + clothesTemplate.RemoteCover;
            if (!IsExist(remoteCover)) {
                LoggerUtils.LogError("update cover:" + localCover);
                CosXmlUploadManager.UploadFile(clothesTemplate.RemoteCover, localCover, (url, err) => {
                    if (string.IsNullOrEmpty(err)) {
                        LoggerUtils.LogError("上传成功:" + url);
                    } else {
                        LoggerUtils.LogError("上传失败:" + err);
                    }
                });
            }
            yield return null;
        }

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("BudTools/安全打包全部SpriteAtlas")]
    public static void SafePackAllSpriteAtlases()
    {
        // 清空选择并强制重建 Inspector，防止 TextureImporterInspector 在资源刷新/域重新加载时崩溃
        Selection.objects = System.Array.Empty<UnityEngine.Object>();
        if (ActiveEditorTracker.sharedTracker != null)
        {
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
        Debug.Log("[SafePack] SpriteAtlas 已清空选择并完成打包。进入 Play 模式前请勿重新选中纹理资源。");
    }
}

/// <summary>
/// 在所有会触发 Domain Reload 的场景前，自动清空 Inspector 选择并强制重建 EditorTracker，
/// 防止 TextureImporterInspector 在域重新加载时崩溃（SIGSEGV at CreateOrReloadInspectorCopy）。
/// 仅清除 Selection 不够：Unity 会在域重新加载前序列化现有 Editor 实例，
/// 必须调用 ForceRebuild() 强制销毁 TextureImporterInspector 实例，使其不再被序列化。
/// 覆盖场景：
///   1. 进入 Play 模式（ExitingEditMode）
///   2. 脚本编译/资产变更触发的自动刷新（beforeAssemblyReload）
/// </summary>
[InitializeOnLoad]
public static class SafePlayModeEnterHelper
{
    static SafePlayModeEnterHelper()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload += ClearInspector;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            ClearInspector();
        }
    }

    static void ClearInspector()
    {
        // ForceRebuild 是延迟执行的：Domain Reload 备份在其执行前就已完成，
        // TextureImporterInspector 仍会被序列化进备份，还原后 OnEnable 崩溃。
        // 改用 DestroyImmediate 立即销毁所有 AssetImporterEditor（含 TextureImporterInspector），
        // 确保备份时该对象已不存在。
        var assetImporterEditors = Resources.FindObjectsOfTypeAll<AssetImporterEditor>();
        foreach (var editor in assetImporterEditors)
        {
            if (editor != null)
            {
                Object.DestroyImmediate(editor);
            }
        }

        Selection.objects = System.Array.Empty<UnityEngine.Object>();

        if (ActiveEditorTracker.sharedTracker != null)
        {
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }
    }
}
