using Pb.Base;
using Basic.Utils;
using NetEngine.src.Net;
using NetEngine.src.Ping;
using NetEngine.src.Sender;
using NetEngine.src.Util;
using NetEngine.src.Util.Def;

namespace NetEngine.src
{
    public static class Core
    {
        public static SocketClient Socket1 { get; set; } = null;

        public static SocketClient Socket2 { get; set; } = null;

        public static Pinger Pinger1 { get; set; } = null;

        public static Pinger Pinger2 { get; set; } = null;

        public static User.User User { get; set; } = null;

        public static Room.Room Room { get; set; } = null;

        public static FrameSender FrameSender { get; set; } = null;
        // public static KeyFrameSender KeyFrameSender { get; set; } = null;

        // public static BattleGame.BattleGame BattleGame { get; set; } = null;

        private static void InitModules()
        {
            Core.User = new User.User(Sdk.BstCallbacks);
            // Core.Matcher = new Matcher.Matcher(Sdk.BstCallbacks);
            Core.Room = new Room.Room(Sdk.BstCallbacks);
            // Core.BattleGame = new BattleGame.BattleGame(Sdk.BstCallbacks);

            Core.FrameSender = new FrameSender(Sdk.BstCallbacks);
            // Core.KeyFrameSender = new KeyFrameSender(Sdk.BstCallbacks);

            Core.Socket1 = new SocketClient(0, false, Config.Url + ":" + Port.TcpRelayPort);
            Core.Socket2 = new SocketClient(1, false, Config.Url + ":" + Port.TcpRelayPort2);
            // Core.Socket3 = new Socket(2, false, Config.KeyFrameUrl+":"+Port.TcpRelayPort3);

            Core.Pinger1 = new Pinger(Sdk.BstCallbacks, 0, "");
            Core.Pinger2 = new Pinger(Sdk.BstCallbacks, 1, "");
            // Core.Pinger3 = new Pinger(Sdk.BstCallbacks, 2, "");

            var route1 = new BaseNetUtil[3] { User, Room, Pinger1 };
            var route2 = new BaseNetUtil[2] { FrameSender.NetUtil2, Pinger2 };
            // var route2 = new BaseNetUtil[3] { FrameSender.NetUtil1, FrameSender.NetUtil2, Pinger2 };
            // var route3 = new BaseNetUtil[2] { KeyFrameSender.NetUtil, Pinger3 };
            // var route2 = new BaseNetUtil[2] { FrameSender.NetUtil1, FrameSender.NetUtil2 };

            foreach (var request in route1)
            {
                request.BindSocket(Core.Socket1);
            }

            foreach (var request in route2)
            {
                request.BindSocket(Core.Socket2);
            }

            // foreach (var request in route3)
            // {
            //     request.BindSocket(Core.Socket3);
            // }

            Util.Pb.Init();
            Sdk.UpdateSdk();
        }

        private static void UnInitModules()
        {
            Socket1?.DestroySocketTask();
            Socket2?.DestroySocketTask();
            // Socket3?.DestroySocketTask();
            var route = new BaseNetUtil[5] { User, Room, FrameSender.NetUtil2, Pinger1, Pinger2 };
            // var route = new BaseNetUtil[7] { User, Room, Sender, FrameSender.NetUtil1, FrameSender.NetUtil2, Pinger1, Pinger2 };
            // var route = new BaseNetUtil[10]
            //             {
            //                 User, Room, Sender, Matcher, FrameSender.NetUtil1, FrameSender.NetUtil2, Pinger1,
            //                 Pinger2, KeyFrameSender.NetUtil, Pinger3
            //             };
            foreach (var request in route)
            {
                request?.UnbindSocket();
            }
        }

        public static void CloseConnnect(ConnectionType socketType)
        {
            SocketClient waiCloseSocket = GetSocket(socketType);
            Pinger pinger = GetPinger(socketType);
            waiCloseSocket?.CloseSocketTask();
            pinger?.Stop();
        }

        public static void OpenConnnect(ConnectionType socketType)
        {
            SocketClient waiOpenSocket = GetSocket(socketType);
            Pinger pinger = GetPinger(socketType);
            waiOpenSocket.ConnectSocketTask($"open socket socketType={socketType}");
        }
        private static SocketClient GetSocket(ConnectionType socketType)
        {
            if (socketType == ConnectionType.Frame)
            {
                return Socket2;
            }

            // if (socketType==ConnectionType.KeyFrame)
            // {
            //     return Socket3;
            // }

            return null;
        }

        private static Pinger GetPinger(ConnectionType socketType)
        {
            if (socketType == ConnectionType.Frame)
            {
                return Pinger2;
            }

            // if (socketType==ConnectionType.KeyFrame)
            // {
            //     return Pinger3;
            // }

            return null;
        }
        public static void InitSdk()
        {
            if (!SdkStatus.IsUnInit()) return;
            // 正在初始化
            SdkStatus.SetStatus(SdkStatus.StatusType.Initing);

            Core.InitModules();
            BaseNetUtil.StopQueueLoop();
            BaseNetUtil.StartQueueLoop();

            // 设置 Socket 链接地址
            Socket1.Url = Config.Url;
            // loginEvent += onSocketConnect;
            ListenSocketConnect();

            Socket1.ConnectSocketTask("init Sdk");
        }

        public static void UnInitSdk()
        {
            if (SdkStatus.IsUnInit())
            {
                return;
            }

            Pinger1.Stop();
            Pinger2.Stop();
            // Pinger3.Stop();

            BaseNetUtil.StopQueueLoop();
            Sdk.Instance.ClearResponse();

            Core.UnInitModules();

            SdkStatus.SetStatus(SdkStatus.StatusType.Uninit);
            UserStatus.SetStatus(UserStatus.StatusType.Logout);
            Sdk.Uninit();
        }

        private static void ListenSocketConnect()
        {
            // 联网
            Socket1.OnEvent("connect", (SocketEvent socketEvent) =>
            {
                // 联网时自动Login
                //if (!UserStatus.IsStatus(UserStatus.StatusType.Logining))
                //{
                //    UserUtil.Login(null);
                //}
                Debugger.Log("socket1 on connect");
                SdkUtil.UnityLog("socket1 on connect");
                UserStatus.SetStatus(UserStatus.StatusType.Login);
                ulong serverTime = 0;
                ResponseEvent eveInit;
                var initRsp = new InitRsp(serverTime);
                eveInit = new ResponseEvent(ErrCode.EcOk, "", initRsp);
                Core.SdkInitCallback(true, eveInit);
                if (string.IsNullOrEmpty(Socket1.Url)) return;
                MainThreadDispatcher.Enqueue(() =>
                {
                    var eve = new ResponseEvent(ErrCode.EcOk) { Data = Socket1.Id };
                    Sdk.BstCallbacks.OnNetwork("connect", eve);
                    Pinger1.Ping(null);
                });
            });
            Socket2.OnEvent("connect", (SocketEvent socketEvent) =>
            {
                // check login 成功后发送业务数据
                //FrameSender.CheckLogin(null, "connect " + Socket2.IsSocketStatus("connect"));

                // Debugger.Log("socket2 on connect:"+Socket2.Url);
                Debugger.Log("socket2 on connect");
                SdkUtil.UnityLog("socket2 on connect");
                CheckLoginStatus.SetStatus(CheckLoginStatus.StatusType.Checked);
                if (!string.IsNullOrEmpty(Socket2.Url))
                {
                    var eve = new ResponseEvent(ErrCode.EcOk) { Data = Socket2.Id };
                    Sdk.BstCallbacks.OnNetwork("connect", eve);
                }
                MainThreadDispatcher.Enqueue(() =>
                {
                    Pinger2.Ping(null);
                });
            });


            // 断网
            Socket1.OnEvent("connectClose", (SocketEvent socketEvent) =>
            {
                Debugger.Log("socket1 on connect close");
                SdkUtil.UnityLog("socket1 on connect close");
                // 初始化失败
                SdkInitCallback(false, new ResponseEvent(ErrorCode.EcSdkSocketClose));
                if (!SdkStatus.IsInited())
                {
                    return;
                }

                // 断网时自动 Logout
                UserStatus.SetStatus(UserStatus.StatusType.Logout);
                if (string.IsNullOrEmpty(Socket1.Url)) return;
                // var eve = new ResponseEvent(ErrCode.EcSdkSocketClose, "Socket 断开", null, null);
                var eve = new ResponseEvent(ErrorCode.EcSdkSocketClose, null, null);
                Sdk.BstCallbacks.OnNetwork("connectClose", eve);
                Pinger1.Stop();
            });
            Socket2.OnEvent("connectClose", (SocketEvent socketEvent) =>
            {
                Debugger.Log("socket2 on connect close");
                SdkUtil.UnityLog("socket2 on connect close");
                if (!SdkStatus.IsInited())
                {
                    return;
                }

                CheckLoginStatus.SetStatus(CheckLoginStatus.StatusType.Offline);
                if (!string.IsNullOrEmpty(Socket2.Url))
                {
                    // var eve = new ResponseEvent(ErrCode.EcSdkSocketClose, "Socket 断开", null, null);
                    var eve = new ResponseEvent(ErrorCode.EcSdkSocketClose, null, null);
                    Sdk.BstCallbacks.OnNetwork("connectClose", eve);
                }

                ;
                Pinger2.Stop();
            });


            // socket 错误
            Socket1.OnEvent("connectError", (SocketEvent socketEvent) =>
            {
                Debugger.Log("socket1 connectError");
                SdkUtil.UnityLog("socket1 connectError");
                // 初始化失败
                SdkInitCallback(false, new ResponseEvent(ErrorCode.EcSdkSocketError));
                if (!SdkStatus.IsInited()) return;
                if (string.IsNullOrEmpty(Socket1.Url)) return;
                // var eve = new ResponseEvent(ErrCode.EcSdkSocketError, "Socket 错误", null, null);
                var eve = new ResponseEvent(ErrorCode.EcSdkSocketError, null, null);
                Sdk.BstCallbacks.OnNetwork("connectError", eve);
            });
            Socket2.OnEvent("connectError", (SocketEvent socketEvent) =>
            {
                Debugger.Log("socket2 connectError");
                SdkUtil.UnityLog("socket2 connectError");
                if (!SdkStatus.IsInited()) return;
                if (string.IsNullOrEmpty(Socket2.Url)) return;
                // var eve = new ResponseEvent(ErrCode.EcSdkSocketError, "Socket 错误", null, null);
                var eve = new ResponseEvent(ErrorCode.EcSdkSocketError, null, null);
                Sdk.BstCallbacks.OnNetwork("connectError", eve);
            });
            // Socket3.OnEvent("connectError", (SocketEvent socketEvent) =>
            // {
            //     Debugger.Log("socket3 connectError");
            //     SdkUtil.UnityLog("socket3 connectError");
            //     if (!SdkStatus.IsInited()) return;
            //     if (string.IsNullOrEmpty(Socket3.Url)) return;
            //     var eve = new ResponseEvent(ErrCode.EcSdkSocketError, "Socket 错误", null, null);
            //     Sdk.BstCallbacks.OnNetwork("connectError",eve);
            // });

            // 需要自动登录
            Socket1.OnEvent("autoAuth", (SocketEvent socketEvent) =>
            {
                if (!SdkStatus.IsInited()) return;

                var isLogout = UserStatus.IsStatus(UserStatus.StatusType.Logout);
                if (!string.IsNullOrEmpty(Socket1.Url) && isLogout)
                {
                    //UserUtil.Login(null);
                }

                ;
            });
            Socket2.OnEvent("autoAuth", (SocketEvent socketEvent) =>
            {
                if (!SdkStatus.IsInited()) return;
                if (string.IsNullOrEmpty(Socket2.Url)) return;

                // Debugger.Log("auto auth check 1");
                // 检查是否需要重登录
                //if (UserStatus.IsStatus(UserStatus.StatusType.Logout)) UserUtil.Login(null);

                // 检查是否需要 checkLogin
                var info = FrameSender.RoomInfo ?? new RoomInfoRsp { };
                // Debugger.Log("auto auth check 2: {0}", CheckLoginStatus.GetRouteId() != info.RouteId);

                if (CheckLoginStatus.IsOffline())
                {
                    //FrameSender.CheckLogin((ResponseEvent eve) =>
                    //{
                    //    if (eve.Code == ErrCode.EcOk)
                    //    {
                    //        Pinger2.Ping(null);
                    //    }
                    //}, "autoAuth");
                }
            });

            // 心跳发包
            Socket1.OnEvent("pingSend", (SocketEvent socketEvent) =>
            {
                if (!SdkStatus.IsInited()) return;

                if (!string.IsNullOrEmpty(Socket1.Url))
                {
                    var eve = new ResponseEvent(ErrCode.EcOk) { Data = Socket1.Id };
                    Sdk.BstCallbacks.OnNetwork("pingSend", eve);
                }
            });


            // 心跳回包正常
            Socket1.OnEvent("pongResposne", (SocketEvent socketEvent) =>
            {
                if (!SdkStatus.IsInited()) return;

                if (!string.IsNullOrEmpty(Socket1.Url))
                {
                    var eve = new ResponseEvent(ErrCode.EcOk) { Data = Socket1.Id };
                    Sdk.BstCallbacks.OnNetwork("pongResposne", eve);
                }
            });

            // 心跳回包超时
            Socket1.OnEvent("pongTimeout", (SocketEvent socketEvent) =>
            {
                if (!SdkStatus.IsInited()) return;

                if (!string.IsNullOrEmpty(Socket1.Url))
                {
                    var eve = new ResponseEvent(ErrorCode.EcSdkTimeOut) { Data = Socket1.Id };
                    Sdk.BstCallbacks.OnNetwork("pongTimeout", eve);
                }
            });
        }


        // 初始化回调函数
        public static void SdkInitCallback(bool success, ResponseEvent eve)
        {
            // 修改Sdk状:
            if (!SdkStatus.IsIniting()) return;
            // 初始化成功
            if (success) SdkStatus.SetStatus(SdkStatus.StatusType.Inited);

            //  初始化失败
            if (!success) SdkStatus.SetStatus(SdkStatus.StatusType.Uninit);

            // 回调
            var code = SdkStatus.IsInited() ? ErrorCode.Ok : ErrorCode.EcSdkUninit;
            if (!success && eve != null && eve.Code != ErrCode.EcOk)
            {
                code = eve.Code;
            }

            // 错误信息
            var msg = SdkStatus.IsInited() ? "初始化成功" : "初始化失败";

            // 服务器时间戳
            var initRsp = (InitRsp)eve?.Data ?? null;
            ulong serverTime = initRsp?.ServerTime ?? 0;

            var e = new ResponseEvent(code, null, new InitRsp(serverTime));

            Sdk.Instance.InitRsp(e);
            if (!SdkStatus.IsInited()) Sdk.Uninit();
        }
    }
}
