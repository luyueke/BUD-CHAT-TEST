using System;
using System.IO;
using Basic.Utils;
using GameData.BaseInfo;

namespace UGCAsset.Draft
{
    /// <summary>
    /// 盒子场景草稿信息，负责将封面图和元数据 JSON 上传至 COS。
    /// 继承 BaseDraftInfo 以复用 COS 上传、RefreshUploadInfo 等通用逻辑；
    /// 覆盖 EditDraftToServer 使其跳过旧版皮肤接口（/ugc/skin/set）调用，
    /// 由 UGCBoxSceneEditorPanel 在 UploadAndSave 回调中通过
    /// CabinBoxSceneNetManager.SetCharacterBox 完成实际的服务端写入。
    /// </summary>
    public class BoxSceneDraftInfo : BaseDraftInfo<BoxSceneDraftInfo, BoxSceneInfo>
    {
        public BoxSceneDraftInfo() { }

        /// <summary>
        /// 以指定的盒子场景元数据构造草稿。
        /// 基类构造函数会自动从 boxSceneInfo.cover / metaDataUrl 初始化远端 URL 跟踪项。
        /// </summary>
        /// <param name="boxSceneInfo">当前编辑的盒子场景元数据</param>
        public BoxSceneDraftInfo(BoxSceneInfo boxSceneInfo) : base(boxSceneInfo) { }

        /// <summary>
        /// 将设计数据 JSON 写入本地缓存文件并注册为待上传项。
        /// </summary>
        /// <param name="metaData">经 JsonConvert.SerializeObject 序列化的 UGCClothesData JSON 字符串</param>
        public void SetMetaData(string metaData)
        {
            draftVersion++;
            updateTime = GameUtils.GetTimeStamp();
            isSaveDraft = false;
            var localPath = Path.Combine(GetDraftCacheFolder(), GetDraftFileName() + ".json");
            File.WriteAllText(localPath, metaData);
            SetUploadLocalInfo(MetaDataKey, localPath, MetaDataRemoteFolder);
        }

        /// <summary>
        /// 覆盖服务端写入步骤：盒子场景不使用皮肤接口（/ugc/skin/set），
        /// 直接以成功状态触发回调，由外部调用方通过
        /// CabinBoxSceneNetManager.SetCharacterBox 完成盒子场景的服务端写入。
        /// </summary>
        public override void EditDraftToServer(Action<BoxSceneDraftInfo, bool> callBack = null)
        {
            isSaveDraft = true;
            callBack?.Invoke(this, true);
        }
    }
}
