using Network.Tcp.Core;
using UnityEngine;

namespace Network.Tcp
{
    internal class TcpBootstrap : InstMonoBehaviour<TcpBootstrap>
    {

        private TcpNetworkMgr _tcpMgr;
        
        public void Init(TcpNetworkMgr tcpNetworkMgr)
        {
            Debug.Log("TcpBootstrap Init");
            this._tcpMgr = tcpNetworkMgr;
            _tcpMgr?.Init();
            
            TcpLoginManager.Instance.Init();
            TcpFriendListMgr.Instance.Init();
        }

        public void DestroySelf()
        {
            Debug.Log("TcpBootstrap DestroySelf");
        }

        private void Awake()
        {
            GameThreadQueue.Init();
        }

        private void Update()
        {
            GameThreadQueue.Update();
        }

        private void OnDestroy()
        {
            TcpLoginManager.Instance.Destroy();
            TcpFriendListMgr.Instance.Destroy();
            GameThreadQueue.Destroy();
            _tcpMgr?.Destroy();
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                //切换到前台时执行，游戏启动时执行一次
                _tcpMgr?.CheckThreadState();
            }
        }
    }
}