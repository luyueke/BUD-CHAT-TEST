using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIAgent;
using UnityEngine;

public class TestEnterGameUtils :GlobalInstance<TestEnterGameUtils>
{
    //请求地图信息
    public void RequestMapInfo(string mapId,Action<UgcInfoRsp> onSuccess, Action<string> onFail = null)
    {
        JObject req = new JObject
        {
            ["id"] = mapId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
            (content) =>
            {
                UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                if (rspData == null)
                {
                    onFail?.Invoke("data is null");
                }
                else
                {
                    onSuccess?.Invoke(rspData);
                }
            }, (error) =>
            {
                onFail?.Invoke(error);
            });
    }

    //衣服、材质、道具
    // HttpUrlDefine.propInfo
    // HttpUrlDefine.GetClothesInfo
    // HttpUrlDefine.GetMaterialInfo
    private void GetInfo(string headUrl,string ugcId,Action<DetailRsp> onSuccess, Action<string> onFail = null)
    {
        JObject req = new JObject()
        {
            ["id"] = ugcId,
        };
        NetworkManager.Inst.SendHttpRequest(headUrl, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
        {

            DetailRsp rspData = JsonConvert.DeserializeObject<DetailRsp>(content);
            if (rspData == null)
            {
                onFail?.Invoke("data is null");
            }
            else
            {
                onSuccess?.Invoke(rspData);
            }
        }, (error) =>
        {
            onFail?.Invoke(error);
        });
    }

    //进入地图编辑
    public void EnterEditMap(string mapId)
    {
        RequestMapInfo(mapId, OnEnterEditMap);
    }

    public void EnterEditMap(MapInfo mapInfo)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.ContinueEditScene, mapInfo);
    }

    public void EnterGuestMap(string mapId)
    {
        RequestMapInfo(mapId, OnEnterGuestMap);
    }

    public void EnterGuestMap(MapInfo mapInfo)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.GuestScene, mapInfo);
    }

    private void OnEnterEditMap(UgcInfoRsp ugcInfoRsp)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.ContinueEditScene, ugcInfoRsp.mapInfo);
    }

    private void OnEnterGuestMap(UgcInfoRsp ugcInfoRsp)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.GuestScene, ugcInfoRsp.mapInfo);
    }

    //进入3D衣服
    public void EnterEditSkin(string ugcId)
    {
        GetInfo(HttpUrlDefine.GetClothesInfo,ugcId,OnEnterEditSkin);
    }

    public void EnterEditProp(string ugcId)
    {
        GetInfo(HttpUrlDefine.propInfo,ugcId,OnEnterEditProp);
    }

    public void EnterEditPose(string ugcId)
    {
        GetInfo(HttpUrlDefine.GetPoseInfo,ugcId,OnEnterEditPose);
    }
    
    
    public void EnterEditProp(PropInfo propInfo)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.UgcPropContinueEdit, propInfo);
    }

    public void EnterEditSkin(SkinInfo skinInfo) {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.UgcSkinContinueEdit, skinInfo);
    }


    private void OnEnterEditSkin(DetailRsp ugcInfoRsp)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.UgcSkinContinueEdit, ugcInfoRsp.skinInfo);
    }

    private void OnEnterEditProp(DetailRsp ugcInfoRsp)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.UgcPropContinueEdit, ugcInfoRsp.propInfo);
    }
    
    private void OnEnterEditPose(DetailRsp ugcInfoRsp)
    {
        UIAgentManager.Inst.OpenPanel(PanelId.UgcLoadingPanel);
        GameController.StartGame(EnterGameModel.AnimPoseContinueEdit, ugcInfoRsp.poseInfo, true, "AnimatedScene");
    }

    public void OnCreatePose()
    {

    }
}
