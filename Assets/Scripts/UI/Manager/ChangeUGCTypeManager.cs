using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using UnityEngine;
using System;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;

public struct ChangeUGCTypeConfig
{
    public string Name;
    public string Cover;
    public string TemplateId;
    public AssetDetailType AssetType;
}

public class ChangeUGCTypeManager : GlobalInstance<ChangeUGCTypeManager>
{
    public void ChangeType(DraftListItem srcUgcInfo,ChangeUGCTypeConfig targetConfig,Action<bool>callback = null)
    {
        BaseUgcInfoReq reqInfo = null;
        string setUrl = GetSetUrl(targetConfig);
        if (targetConfig.AssetType == AssetDetailType.Skin)
        {
            var newInfo = ConvertToSkinInfo(srcUgcInfo,targetConfig);
            newInfo.skinType = (int)SkinType.Avatar;
            var skinReq = new SetSkinInfoReq
            {
                skinInfo = newInfo,
                setType = (int)SetType.Create
            };
            reqInfo = skinReq;
        }
        else if(targetConfig.AssetType == AssetDetailType.Instrument)
        {
            var newInfo = ConvertToSkinInfo(srcUgcInfo,targetConfig);
            newInfo.skinType = (int)SkinType.Avatar;
            var skinReq = new SetSkinInfoReq
            {
                skinInfo = newInfo,
                SkinActionInfo = MusicalInstrumentUtils.GetDefaultSkinActionInfo(),
                setType = (int)SetType.Create
            };
            reqInfo = skinReq;
        }
        else if(targetConfig.AssetType == AssetDetailType.Prop)
        {
            var newInfo = CovertToPropInfo(srcUgcInfo,targetConfig);
            var propReq = new SetPropInfoReq()
            {
                propInfo = newInfo,
                setType = (int)SetType.Create
            };
            reqInfo = propReq;
        }
        if (targetConfig.AssetType == AssetDetailType.Pet)
        {
            var newInfo = ConvertToSkinInfo(srcUgcInfo,targetConfig);
            newInfo.skinType = (int)SkinType.Pet;
            var skinReq = new SetSkinInfoReq
            {
                skinInfo = newInfo,
                setType = (int)SetType.Create
            };
            reqInfo = skinReq;
        }

        if (reqInfo == null)
        {
            TipPanel.ShowToast("目标类型还不支持转换："+targetConfig.AssetType);
            return;
        }

        var reqParam = JsonConvert.SerializeObject(reqInfo);

        NetworkManager.Inst.SendHttpRequest<DraftListItem>(setUrl, HttpMethod.POST, reqParam, draftListItem => {
            LoggerUtils.Log($"转换类型成功:");
            // MessageHelper.Broadcast(DraftMessage.DraftSaveStatus, newBaseInfo);
            callback?.Invoke(true);
        }, fail => {
            LoggerUtils.LogError($"转换类型失败:" + fail);
            callback?.Invoke(false);
        });
    }

    public SkinInfo ConvertToSkinInfo(DraftListItem srcUgcInfo,ChangeUGCTypeConfig targetConfig)
    {
        var baseInfo = GameStudioUtils.GetBaseInfo(srcUgcInfo);
        SkinInfo cloneInfo = baseInfo.ToSkinInfo();
        cloneInfo.id = "";
        cloneInfo.draftVersion = 0;
        cloneInfo.templateId = targetConfig.TemplateId;
        cloneInfo.subType = GetAvatarSubTypeByTempleteId(targetConfig.TemplateId);
        cloneInfo.isProp = true;

        //复制顶点数相关数据
        if (srcUgcInfo.propInfo != null && srcUgcInfo.propInfo.detailInfo != null)
        {
            cloneInfo.skinDetailInfo = SkinDetailInfo.FromDetailInfo(srcUgcInfo.propInfo.detailInfo);
        }

        //清除锚点相关数据
        if (cloneInfo.skinDetailInfo != null)
        {
            cloneInfo.skinDetailInfo.anchor = Vector3.zero;
            cloneInfo.skinDetailInfo.scale = Vector3.one;
            cloneInfo.skinDetailInfo.size = Vector3.one;
            cloneInfo.skinDetailInfo.pDef = Vector3.zero;
            cloneInfo.skinDetailInfo.rDef = Vector3.zero;
            cloneInfo.skinDetailInfo.sDef = Vector3.one;
        }

        return cloneInfo;
    }

    public PropInfo CovertToPropInfo(DraftListItem srcUgcInfo,ChangeUGCTypeConfig targetConfig)
    {
        var baseInfo = GameStudioUtils.GetBaseInfo(srcUgcInfo);
        PropInfo cloneInfo = baseInfo.ToPropInfo();
        cloneInfo.id = "";
        cloneInfo.draftVersion = 0;
        cloneInfo.templateId = targetConfig.TemplateId;

        //复制顶点数相关数据
        if (srcUgcInfo.skinInfo != null && srcUgcInfo.skinInfo.skinDetailInfo != null)
        {
            cloneInfo.detailInfo = DetailInfo.FromSkinDetailInfo(srcUgcInfo.skinInfo.skinDetailInfo);
        }

        //清除锚点相关数据
        if (cloneInfo.detailInfo != null)
        {
            cloneInfo.detailInfo.anchor = Vector3.zero;
            cloneInfo.detailInfo.scale = Vector3.one;
            cloneInfo.detailInfo.size = Vector3.one;
        }
        return cloneInfo;
    }


    public int GetAvatarSubTypeByTempleteId(string templeteId)
    {
        string subTypeStr = templeteId.Substring(1, 2);
        if (int.TryParse(subTypeStr, out int intResult))
        {
            return intResult;
        }
        return 0;
    }


    public string GetSetUrl(ChangeUGCTypeConfig config)
    {
        string setUrl = "";
        switch (config.AssetType)
        {
            case AssetDetailType.Prop:
                setUrl = HttpUrlDefine.setProp;
                break;
            case AssetDetailType.Skin:
                setUrl = HttpUrlDefine.SetSkin;
                break;
            case AssetDetailType.Instrument:
                setUrl = HttpUrlDefine.SetSkin;
                break;
            case AssetDetailType.Pet:
                setUrl = HttpUrlDefine.SetSkin;
                break;
        }

        return setUrl;
    }
}
