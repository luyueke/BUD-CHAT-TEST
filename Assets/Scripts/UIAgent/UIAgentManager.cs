using GameData.UGCData;
using System;
using UnityEngine;
using xasset;

namespace UIAgent
{
    /// <summary>
    /// 业务反向调用UI程序集，目前支持
    /// 
    /// - 打开UI+传参
    /// - 关闭UI
    /// - 调用UI内部方法
    /// - 打开Toast等公共弹窗
    ///
    /// </summary>
    public class UIAgentManager : GlobalInstance<UIAgentManager>
    {
        public delegate void OpenPanelEvent(PanelId panelId, WindowId windowId, params object[] args);
        public delegate void SwapPanelEvent(PanelId panelId, params object[] args);
        public delegate bool FindPanelEvent(WindowId windowId, PanelId panelId);
        public delegate void ClosePanelEvent(WindowId windowId, PanelId panelId);

        public delegate void CallPanelMethodEvent(PanelId pId, string methodName, params object[] args);
        public delegate void SelectGoMethodEvent(GameObject go);

        public delegate void AssetDetailGetInfo(int headUrl, string ID, Action<DetailRsp> aciton);

        public delegate void BatchGetInfo(string ID, Action<DetailRsp> aciton);

        public event OpenPanelEvent OpenPanelEventHandler;
        public event FindPanelEvent FindPanelEventHandler;
        public event ClosePanelEvent ClosePanelEventHandler;
        public event CallPanelMethodEvent CallPanelMethodEventHandler;
        public event SelectGoMethodEvent SelectGoMethodEventHandler;
        public event SwapPanelEvent SwapPanelEventHandler;
        public event AssetDetailGetInfo AssetDetailGetInfoHandler;
        public event BatchGetInfo BatchGetInfoHandle;
        // OnDownloadRetry 是新版 AOT（>= 1.0.20）才有的字段，用反射访问避免旧包 MissingFieldException
        private static readonly System.Reflection.FieldInfo _onDownloadRetryField =
            typeof(xasset.Downloader).GetField("OnDownloadRetry",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        public override void Initialize()
        {
            base.Initialize();
            if (DeviceInfoManager.Inst.CheckVersion_1_0_20() && _onDownloadRetryField != null)
            {
                var cur = _onDownloadRetryField.GetValue(null) as System.Action<string, int>;
                _onDownloadRetryField.SetValue(null,
                    System.Delegate.Combine(cur, (System.Action<string, int>)OnBundleDownloadRetry));
            }
        }

        public override void Release()
        {
         if (DeviceInfoManager.Inst.CheckVersion_1_0_20() && _onDownloadRetryField != null)
            {
                var cur = _onDownloadRetryField.GetValue(null) as System.Action<string, int>;
                _onDownloadRetryField.SetValue(null,
                    System.Delegate.Remove(cur, (System.Action<string, int>)OnBundleDownloadRetry));
            }
            base.Release();
            OpenPanelEventHandler = null;
            ClosePanelEventHandler = null;
            CallPanelMethodEventHandler = null;
            SelectGoMethodEventHandler = null;
            SwapPanelEventHandler = null;
            AssetDetailGetInfoHandler = null;
            BatchGetInfoHandle = null;
        }

        private float _retryToastLastShown;
        private const float RetryToastCooldown = 10f;

        private void OnBundleDownloadRetry(string url, int retryCount)
        {
            var now = UnityEngine.Time.realtimeSinceStartup;
            if (now - _retryToastLastShown < RetryToastCooldown) return;
            _retryToastLastShown = now;
            var msg = retryCount >= 3 ? "网络连接不稳定，请检查网络" : "网络波动，正在重试...";
            ShowToast(msg);
        }

        #region 外部调用

        public void OpenPanel(PanelId pId, WindowId wId, params object[] args)
        {
            OpenPanelEventHandler?.Invoke(pId, wId, args);
        }

        public void OpenPanel(PanelId pId, params object[] args)
        {
            OpenPanelEventHandler?.Invoke(pId, WindowId.None, args);
        }
        
        public void SwapPanel(PanelId pId, params object[] args)
        {
            SwapPanelEventHandler?.Invoke(pId, WindowId.None, args);
        }

        public void ClosePanel(WindowId wId, PanelId pId)
        {
            ClosePanelEventHandler?.Invoke(wId, pId);
        }
        
        public bool FindPanel(WindowId wId, PanelId pId)
        {
            if (FindPanelEventHandler == null)
            {
                return false;
            }
            return FindPanelEventHandler.Invoke(wId, pId);
        }

        /// <summary>
        /// 调用Panel中方法并传参
        /// 反射调用--禁止高频调用！
        /// </summary>
        public void CallPanelMethod(PanelId pId, string methodName, params object[] args)
        {
            CallPanelMethodEventHandler?.Invoke(pId, methodName, args);
        }

        public void ShowToast(string message)
        {
            OpenPanel(PanelId.TipPanel, message);
        }

        public void ShowCommonConfirmWithTitlePanel(string titleText, string contentText, string confirmText, string cancelText, Action confirmClick,
            Action cancelClick)
        {
            OpenPanel(PanelId.CommonConfirmWithTitlePanel);
            CallPanelMethod(PanelId.CommonConfirmWithTitlePanel, "SetTextAndAction", titleText, contentText, confirmText, cancelText, confirmClick,
                cancelClick);
        }

        public void SelectGo(GameObject go)
        {
            SelectGoMethodEventHandler?.Invoke(go);
        }

        #endregion

        public void AssetGetInfo(int headUrl, string ID, Action<DetailRsp> aciton)
        {
            AssetDetailGetInfoHandler?.Invoke(headUrl, ID, aciton);
        }

        public void GetBatchInfo(string ID, Action<DetailRsp> aciton)
        {
            BatchGetInfoHandle?.Invoke(ID, aciton);
        }
    }
    public struct AIParkConfirmParam
    {
        public string LText;
        public string RText;

        public string Title;
        public string Desc;

        public Action LAction;
        public Action RAction;
    }
}