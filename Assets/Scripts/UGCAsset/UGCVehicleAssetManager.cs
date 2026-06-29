using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Es;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
//using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using UGCAsset.Draft;
using UnityEngine;

namespace UGCAsset
{
    public class UGCVehicleAssetManager<TK> : GlobalInstance<TK>
        where TK : BaseGlobalInstance, new()
    {
        protected readonly Dictionary<string, VehicleDraftInfo> draftInfos;

        public UGCVehicleAssetManager()
        {
            var infoPath = $"{Application.persistentDataPath}/DraftCache/{typeof(VehicleDraftInfo).Name}_{AccountDataManager.Inst.Uid}.json";
            draftInfos = File.Exists(infoPath)
                ? JsonConvert.DeserializeObject<Dictionary<string, VehicleDraftInfo>>(File.ReadAllText(infoPath))
                : new Dictionary<string, VehicleDraftInfo>();
            MessageHelper.AddListener<VehicleDraftInfo>(DraftMessage.DraftSaveStatus, OnDraftSaveCallBack);
        }

        private void OnDraftSaveCallBack(VehicleDraftInfo draftInfo)
        {
            if (string.IsNullOrEmpty(draftInfo.draftId))
            {
                return;
            }
            if (draftInfo.GetUploadStatus() != UploadStatus.SavedDraft)
                return;
            if (!draftInfos.ContainsKey(draftInfo.draftId))
                return;
            draftInfos.Remove(draftInfo.draftId);
            Save();
        }


        public VehicleDraftInfo GetOrCreateDraftInfo(VehicleInfo info)
        {
            if (!string.IsNullOrEmpty(info.id) && draftInfos.TryGetValue(info.id, out VehicleDraftInfo draftInfo) && draftInfo != null)
            {
                if (draftInfo.draftVersion >= info.draftVersion)
                {
                    draftInfo.vehicleInfo.CopyTo(info);
                    draftInfo.vehicleInfo = info;

                    return draftInfo;
                }

                // 远端草稿版本大于本地，删除本地草稿
                draftInfo = Activator.CreateInstance(typeof(VehicleDraftInfo), info) as VehicleDraftInfo;
                draftInfos[info.id] = draftInfo;
                return null;
            }
            else
            {
                draftInfo = Activator.CreateInstance(typeof(VehicleDraftInfo), info) as VehicleDraftInfo;
                if (!string.IsNullOrEmpty(info.id))
                {
                    draftInfos.Add(info.id, draftInfo);
                }
                return draftInfo;
            }
        }

        public void CreateInServer(VehicleInfo info, Action<byte[]> callBack = null)
        {
            if (string.IsNullOrEmpty(info.cover))
            {
                info.cover = "https://cdn.budapp.cn/UGCVehicleCover/template/1.png";//GetTemplateCover(info.templateId);
            }
            var draftInfo = GetOrCreateDraftInfo(info);
            draftInfo?.CreateDraftToServer((skinInfoRsp, isSuccess) => {
                if (isSuccess)
                {
                    skinInfoRsp.CopyTo(info);
                    callBack?.Invoke(GetTemplateMetaData(skinInfoRsp.templateId));
                }
                else
                {
                    callBack?.Invoke(null);
                }
            });
        }

        public virtual byte[] GetTemplateMetaData(string templateId = null)
        {
            PropTemplate template = null;
            // if (string.IsNullOrEmpty(templateId)) {
            //     template = DataTables.GetPropTemplateList().FirstOrDefault();
            // } else {
            //     template = DataTables.GetPropTemplate(templateId);
            // }
            template = DataTables.GetPropTemplateList().FirstOrDefault();
            var metaDataUrl = template?.MetaDataUrl;
            if (string.IsNullOrEmpty(metaDataUrl)) return null;
            var metaDataRequest = xasset.Asset.Load(metaDataUrl, typeof(TextAsset));
            if (metaDataRequest == null) return null;
            var metaData = metaDataRequest.asset as TextAsset;
            if (metaData == null) return null;
            return metaData.bytes;
        }

        public VehicleDraftInfo GetDraftInfo(VehicleInfo info)
        {
            draftInfos.TryGetValue(info.id, out var draftInfo);
            if (draftInfo != null)
            {
                if (draftInfo.draftVersion > info.draftVersion)
                {
                    draftInfo.vehicleInfo.CopyTo(info);
                    draftInfo.vehicleInfo = info;
                    return draftInfo;
                }
                else
                {
                    // 远端草稿版本大于本地，删除本地草稿
                    draftInfos.Remove(info.id);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }


        public virtual string GetTemplateCover(string templateId = null)
        {
            return null;
        }

        public void SaveDraftInfo(VehicleDraftInfo draftInfo)
        {
            if (!draftInfo.IsValid())
            {
                Debug.LogError("草稿信息无效");
                return;
            }

            if (draftInfos.TryGetValue(draftInfo.draftId, out var tmpMapDraftInfo))
            {
                if (tmpMapDraftInfo.Equals(draftInfo))
                {
                    Save();
                    return;
                }

                Debug.LogError("删除草稿");
                tmpMapDraftInfo.Cancel();
                draftInfos.Remove(draftInfo.draftId);
            }

            draftInfos.Add(draftInfo.draftId, draftInfo);
            Save();
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<VehicleDraftInfo>(DraftMessage.DraftSaveStatus, OnDraftSaveCallBack);
            Save();
        }

        protected virtual void Save()
        {
            if (draftInfos != null)
            {
                var cacheDir = $"{Application.persistentDataPath}/DraftCache";
                var infoPath =
                    $"{Application.persistentDataPath}/DraftCache/{typeof(TK).Name}_{AccountDataManager.Inst.Uid}.json";
                if (!Directory.Exists($"{Application.persistentDataPath}/DraftCache"))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                File.WriteAllText(infoPath, JsonConvert.SerializeObject(draftInfos));
#if UNITY_EDITOR
                Debug.Log(
                    $"UGCAssetManager save draft to:{typeof(TK).Name}_{AccountDataManager.Inst.Uid},  data:{JsonConvert.SerializeObject(draftInfos)}");
#endif
            }
        }


    }
}

