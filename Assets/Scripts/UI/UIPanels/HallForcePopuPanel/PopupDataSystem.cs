using System;
using Network;
using Network.Http;
using Newtonsoft.Json;
using View.UI.PopupPanelSystem.Base.Core;
using View.UI.PopupPanelSystem.Data;

namespace View.UI.PopupPanelSystem.Base.SubSystem
{
    public class PopupDataSystem : BasePopupSystem
    {
        public PopupRspData PopupData;
        private bool _isRequesting = false;

        public PopupDataSystem(PopupPanelManager context) : base(context)
        {
        }

        public override void Release()
        {
            PopupData = null;
            _isRequesting = false;
        }

        #region 冷启动

        public void RequestDataOnCodeStart(Action<PopupRspData> callback)
        {
            if (_isRequesting) return;
            _isRequesting = true;
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.LobbyPopupList,
                HttpMethod.GET,
                "",
                onReceive: arg0 =>
                {
                    PopupRspData responseData = JsonConvert.DeserializeObject<PopupRspData>(arg0);
                    PopupData = responseData;
                    callback.Invoke(responseData);
                    _isRequesting = false;
                }, onFail: arg0 =>
                {
                    _isRequesting = false;
                });
            
        }

        #endregion

        #region 暖启动

        public void RequestDataOnWarmStart(Action<PopupRspData> callback)
        {
            if (_isRequesting) return;
            _isRequesting = true;
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.LobbyPopupList,
                HttpMethod.GET,
                "",
                onReceive: arg0 =>
                {
                    PopupRspData responseData = JsonConvert.DeserializeObject<PopupRspData>(arg0);
                    PopupData = responseData;
                    _isRequesting = false;
                    callback.Invoke(responseData);
                }, onFail: arg0 =>
                {
                    _isRequesting = false;
                });
            
        }

        #endregion

        #region 任务数据

        private void RequestTaskData(Action<PopupRspData> callback)
        {

        }

        #endregion
    }
}