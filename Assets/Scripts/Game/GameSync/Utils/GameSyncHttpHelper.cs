using System;
using GameData.Base;
using GameData.GameSync;
using GameData.Manager;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UIAgent;

namespace Game.GameSync
{
    public class GameSyncHttpHelper:GlobalInstance<GameSyncHttpHelper>
    {
        HttpRequestHandle<GetServerInfoByCodeResp> GetServerInfoByCodeHandle;
        public GameSyncHttpHelper()
        {
            GetServerInfoByCodeHandle = new HttpRequestHandle<GetServerInfoByCodeResp>(HttpUrlDefine.GetServerInfoByCode, HttpMethod.POST);
        }

        public HttpSender<GetServerInfoByCodeResp> GetServerInfoByCode(string roomCode)
        {
            GetServerInfoByCodeReq gameServerReq = new GetServerInfoByCodeReq()
            {
                roomCode = roomCode,
            };
            
            return GetServerInfoByCodeHandle.Send(gameServerReq);
        }

        private Action<UgcInfoRsp> _onEnterRoomSuccess;
        private Action _onEnterRoomFail;
        private HttpSender<GetServerInfoByCodeResp> _enterRoomByRoomCodeHttpSender;
        public void EnterRoomByRoomCode(string roomCode, Action<UgcInfoRsp> onSuccess, Action onFail)
        {
            _enterRoomByRoomCodeHttpSender?.Cancel();
            _onEnterRoomSuccess = onSuccess;
            _onEnterRoomFail = onFail;
            
            _enterRoomByRoomCodeHttpSender = GetServerInfoByCode(roomCode);
            _enterRoomByRoomCodeHttpSender.OnSuccess = OnGetServerInfoByCodeSuccess;
            _enterRoomByRoomCodeHttpSender.OnFail = OnGetServerInfoByCodeFail;
        }
        
        private void OnGetServerInfoByCodeSuccess(GetServerInfoByCodeResp rsp)
        {
            if (rsp == null)
            {
                _onEnterRoomFail?.Invoke();
            }
        
            GameDataManager.Inst.gameOnlineData.CreateJoinCodeData(rsp);
            JObject req = new JObject
            {
                ["id"] = rsp.mapId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), OnMapInfoSuccess, OnMapInfoFail);
        }
        
        private void OnGetServerInfoByCodeFail(HttpResponseFailDataStruct failRsp)
        {
            _onEnterRoomFail?.Invoke();
        }
        
        private void OnMapInfoSuccess(string content)
        {
            UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
            if (rspData == null)
            {
                _onEnterRoomFail?.Invoke();
            }
            else
            {
                var updateState = (ForceUpdate)rspData?.mapInfo?.forceUpdate;
                if (updateState != ForceUpdate.Default)
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.UpdateTipsPanel, updateState);
                    _onEnterRoomFail?.Invoke();
                    return;
                }
                _onEnterRoomSuccess?.Invoke(rspData);
            }
        }

        private void OnMapInfoFail(string error)
        {
            LoggerUtils.LogError("EnterRoomCodePanel OnMapInfoFail error = " , error);
            _onEnterRoomFail?.Invoke();
        }
    }
}