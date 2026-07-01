using Basic.Utils;
using Game.BLE;
using Game.BudBox;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Author:
/// Desc: BOX BudBox 设备管理器（全局单例）。
///       负责拉取账号绑定的设备列表、维护 MQTT 连接、
///       缓存当前 Box 的角色/音量/亮度数据，并通过 MQTT 将控制指令下发至硬件。
/// Date: 26-04-09
/// </summary>
public class CabinBoxManager : GlobalInstance<CabinBoxManager>
{
    /// <summary>测试模式开关：true 时跳过硬件回包，直接在本地更新 _budBoxDic 数据</summary>
    public const bool ISTEST = true;

    /// <summary>下行指令 Topic 模板，{0} 填设备 ID</summary>
    public const string TopicSet = "device/{0}/command/set";
    /// <summary>设备主动上报 Topic 模板，{0} 填设备 ID</summary>
    public const string TopicReported = "device/{0}/state/reported";
    /// <summary>设备主动上报 Topic 模板，{0} 填设备 ID</summary>
    public const string TopicReported2App = "device/{0}/state/reported2App";
    /// <summary>设备二维码 Retain Topic 模板，{0} 填设备 ID；硬件以 retain=true 发布，软件订阅后立即获取</summary>
    public const string TopicQrCode = "device/{0}/state/qrcode";

    /// <summary>硬件客户端已连接事件 Topic</summary>
    public const string TopicEventClientConnected = "$events/client_connected";
    /// <summary>硬件客户端已断开事件 Topic</summary>
    public const string TopicEventClientDisconnected = "$events/client_disconnected";

    private const string BrokerHost = "mqtt-829rbd2k-bj-public.mqtt.tencenttdmq.com"; // MQTT Broker 地址
    private const int BrokerPort = 1883;                                               // MQTT Broker 端口
    private const string MqttUsername = "box_client";                                  // MQTT 连接用户名
    private const string MqttPassword = "skdcd62dccf020c030";                         // MQTT 连接密码
    private const string MqttInstanceId = "mqtt-829rbd2k";                            // 腾讯 TDMQ 实例 ID，用于拼接 ClientId

    /// <summary>账号下所有已绑定 Box 的数据，key 为 deviceId</summary>
    private Dictionary<string, CabinBudBoxData> _budBoxDic = new Dictionary<string, CabinBudBoxData>();
    /// <summary>当前已选中并连接的 Box 数据</summary>
    private CabinBudBoxData _budBoxData;

    private object _mqttClient;
    // Cached reflection metadata — populated in CacheReflectionMetadata() after client creation
    private PropertyInfo _isConnectedProp;
    private MethodInfo _connectAsyncMethod;
    private MethodInfo _disconnectAsyncMethod;
    private MethodInfo _publishAsyncMethod;
    private MethodInfo _subscribeAsyncMethod;
    private MethodInfo _unsubscribeAsyncMethod;
    private MethodInfo _appMsgReceivedAddMethod;
    private MethodInfo _appMsgReceivedRemoveMethod;
    private MethodInfo _disconnectedAddMethod;
    private MethodInfo _disconnectedRemoveMethod;
    private object _atLeastOnceQos;
    private object _appMsgReceivedDelegate;
    private object _disconnectedDelegate;
    private string _mqttClientId;  // 当前客户端 ID，格式：{InstanceId}@app-{uid}
    private bool _reconnecting;    // 是否正在等待重连，防止并发触发多次重连
    private bool _mqttStopped;     // 是否主动停止 MQTT（面板关闭时置 true），阻止自动重连
    private string _subscribedBoxDeviceId; // 当前已订阅 Topic 的设备 ID，空表示未订阅
    /// <summary>MQTT 未连接时暂存的待跳转设备数据，连接成功后自动执行 ConnectToBox</summary>
    private CabinBudBoxData _pendingBoxData;
    /// <summary>key = deviceId，value = 设备二维码 JSON 字符串，由 sync_baseMsg 上报时写入</summary>
    private readonly Dictionary<string, string> _deviceQrCodeDic = new Dictionary<string, string>();
    /// <summary>key = deviceId，value = 扫码得到的原始 qrcode 字符串；MQTT 未连接时暂存，SubscribeBox 时补发 retain</summary>
    private readonly Dictionary<string, string> _pendingQrCodeRetainDic = new Dictionary<string, string>();
    /// <summary>key = deviceId，value = 心跳定时器句柄；每4秒发送一次探测包</summary>
    private readonly Dictionary<string, BudTimer> _heartbeatTimers = new Dictionary<string, BudTimer>();
    private const float HeartbeatIntervalSec = 4f; // 心跳发送间隔（秒）
    /// <summary>key = deviceId，value = 离线超时定时器句柄；6秒内无任何消息则判定离线</summary>
    private readonly Dictionary<string, BudTimer> _offlineTimers = new Dictionary<string, BudTimer>();
    private const float OfflineTimeoutSec = 6f; // 离线判定超时阈值（秒）
    public bool IsConnected =>
        _mqttClient != null && _isConnectedProp != null &&
        (bool)(_isConnectedProp.GetValue(_mqttClient) ?? false);

    #region MQTT 连接

    /// <summary>
    /// 连接到云端 MQTT Broker，面板打开时调用。
    /// 已连接时直接跳过；连接在后台线程执行，不阻塞主线程。
    /// </summary>
    public void ConnectMqttBroker()
    {
        LoggerUtils.Log("[CabinBoxManager] ConnectMqttBroker — 调用");
        bool versionOk = false;
        try { versionOk = CheckMqttVersion(); }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] ConnectMqttBroker — CheckMqttVersion 异常: {ex.GetType().Name} {ex.Message}");
            return;
        }
        LoggerUtils.Log($"[CabinBoxManager] ConnectMqttBroker — versionOk={versionOk} IsConnected={IsConnected}");
        if (!versionOk) return;
        if (IsConnected) return;

        _mqttStopped = false;
        _mqttClientId = $"{MqttInstanceId}@app-{AccountDataManager.Inst.UserInfo.uid}1";
        // 必须在主线程提前初始化，后台线程无法调用 Unity API
        MainThreadDispatcher.Init();
        ConnectInternal();
    }

    /// <summary>
    /// 在后台线程池中执行实际的 MQTT 连接：创建客户端、注册事件、发起 Connect。
    /// 连接结果（成功或失败）均通过 <see cref="MainThreadDispatcher"/> 切回主线程处理。
    /// </summary>
    private void ConnectInternal()
    {
        Task.Run(async () =>
        {
            try
            {
                CleanupMqttClient();

                var factoryType = Type.GetType("MQTTnet.MqttFactory, MQTTnet");
                if (factoryType == null) throw new InvalidOperationException("MQTTnet assembly not found");
                var factory = Activator.CreateInstance(factoryType);
                var createClientMethod = factoryType.GetMethod("CreateMqttClient", Type.EmptyTypes)
                                      ?? factoryType.GetMethod("CreateMqttClient");
                _mqttClient = createClientMethod.Invoke(factory, null);
                LoggerUtils.Log($"[CabinBoxManager] _mqttClient 已创建: {_mqttClient != null}, type={_mqttClient?.GetType().Name}");

                CacheReflectionMetadata();

                var appMsgDelegateType = _appMsgReceivedAddMethod?.GetParameters().Length > 0
                    ? _appMsgReceivedAddMethod.GetParameters()[0].ParameterType : null;
                var disconnDelegateType = _disconnectedAddMethod?.GetParameters().Length > 0
                    ? _disconnectedAddMethod.GetParameters()[0].ParameterType : null;
                _appMsgReceivedDelegate = CreateMqttEventDelegate(appMsgDelegateType, OnMqttMessageReceivedProxy);
                _disconnectedDelegate = CreateMqttEventDelegate(disconnDelegateType, OnMqttConnectionClosedProxy);
                if (_appMsgReceivedDelegate != null && _appMsgReceivedAddMethod != null)
                    _appMsgReceivedAddMethod.Invoke(_mqttClient, new object[] { _appMsgReceivedDelegate });
                else
                    LoggerUtils.LogError("[CabinBoxManager] ApplicationMessageReceivedAsync 事件委托创建失败，将无法接收消息");
                if (_disconnectedDelegate != null && _disconnectedAddMethod != null)
                    _disconnectedAddMethod.Invoke(_mqttClient, new object[] { _disconnectedDelegate });

                var optBuilderType = Type.GetType("MQTTnet.Client.MqttClientOptionsBuilder, MQTTnet");
                var optBuilder = Activator.CreateInstance(optBuilderType);
                ReflInvoke(optBuilder, "WithTcpServer", BrokerHost, BrokerPort);
                ReflInvoke(optBuilder, "WithClientId", _mqttClientId);
                ReflInvoke(optBuilder, "WithCredentials", MqttUsername, MqttPassword);

                var protoType = Type.GetType("MQTTnet.Formatter.MqttProtocolVersion, MQTTnet");
                if (protoType != null)
                    ReflInvoke(optBuilder, "WithProtocolVersion", Enum.Parse(protoType, "V500"));

                ReflInvoke(optBuilder, "WithCleanSession", true);
                ReflInvoke(optBuilder, "WithKeepAlivePeriod", TimeSpan.FromSeconds(20));
                var options = ReflInvoke(optBuilder, "Build");

                var connectTask = (Task)_connectAsyncMethod.Invoke(_mqttClient,
                    BuildAsyncArgs(_connectAsyncMethod, options));
                await connectTask;

                var result = connectTask.GetType().GetProperty("Result")?.GetValue(connectTask);
                var resultCodeStr = result?.GetType().GetProperty("ResultCode")?.GetValue(result)?.ToString() ?? string.Empty;

                if (resultCodeStr == "Success" || resultCodeStr == "0")
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        LoggerUtils.Log("[CabinBoxManager] MQTT Broker 连接成功");
                        PublishAppToken();
                        SubscribeEventTopics();
                        SubscribeBox(); //订阅当前设备
                        MessageHelper.Broadcast(MessageName.OnLinkMqttBrokerFinish);
                    });
                }
                else
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        LoggerUtils.LogError($"[CabinBoxManager] MQTT Broker 连接被拒绝，错误码: {resultCodeStr}");
                        TryReconnect();
                    });
                }
            }
            catch (Exception e)
            {
                var rootEx = e;
                while (rootEx.InnerException != null) rootEx = rootEx.InnerException;
                var rootMsg = $"[{rootEx.GetType().Name}] {rootEx.Message}";
                MainThreadDispatcher.Enqueue(() =>
                {
                    LoggerUtils.LogError($"[CabinBoxManager] MQTT Broker 连接失败: {rootMsg}");
                    TryReconnect();
                });
            }
        });
    }

    private Task OnMqttConnectionClosedProxy(object e)
    {
        string reason = "Unknown";
        string exMsg = string.Empty;
        try
        {
            var reasonVal = e?.GetType().GetProperty("Reason")?.GetValue(e);
            if (reasonVal != null) reason = reasonVal.ToString();
            var exVal = e?.GetType().GetProperty("Exception")?.GetValue(e) as Exception;
            if (exVal != null) exMsg = $" ex=[{exVal.GetType().Name}] {exVal.Message}";
        }
        catch { }
        MainThreadDispatcher.Enqueue(() =>
        {
            LoggerUtils.Log($"[CabinBoxManager] MQTT 连接断开 reason={reason}{exMsg}");
            // SessionTakenOver 表示本端新连接已将旧 session 踢下线，无需重连
            if (reason == "SessionTakenOver") return;
            TryReconnect();
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// 5 秒后尝试重连。若已在重连中或主动停止则跳过，防止重复重连。
    /// </summary>
    private void TryReconnect()
    {
        if (_reconnecting || _mqttStopped) return;
        _reconnecting = true;
        LoggerUtils.Log("[CabinBoxManager] MQTT 5秒后重连...");
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(5000);
            if (_mqttStopped) { _reconnecting = false; return; }
            MainThreadDispatcher.Enqueue(() =>
            {
                _reconnecting = false;
                ConnectInternal();
            });
        });
    }

    /// <summary>断开当前 MQTT 连接，并取消自动重连</summary>
    public void DisconnectMqtt()
    {
        _mqttStopped = true;
        _reconnecting = false;
        // 清除待跳转的暂存数据，防止断开后仍触发界面跳转
        _pendingBoxData = null;
        MessageHelper.RemoveListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnectedForPendingBox);
        ClearBudBoxData();
        CancelAllHeartbeatTimers();
        UnsubscribeEventTopics();
        CleanupMqttClient();
    }

    /// <summary>清理旧 MqttClient 的事件注册并断开连接，防止重连时事件重复注册</summary>
    private void CleanupMqttClient()
    {
        if (_mqttClient == null) return;
        try
        {
            if (_appMsgReceivedRemoveMethod != null && _appMsgReceivedDelegate != null)
                _appMsgReceivedRemoveMethod.Invoke(_mqttClient, new object[] { _appMsgReceivedDelegate });
            if (_disconnectedRemoveMethod != null && _disconnectedDelegate != null)
                _disconnectedRemoveMethod.Invoke(_mqttClient, new object[] { _disconnectedDelegate });

            if (_isConnectedProp != null && (bool)(_isConnectedProp.GetValue(_mqttClient) ?? false) &&
                _disconnectAsyncMethod != null)
            {
                Task.Run(async () =>
                {
                    try { await (Task)_disconnectAsyncMethod.Invoke(_mqttClient, BuildAsyncArgs(_disconnectAsyncMethod)); }
                    catch { }
                }).Wait(3000);
            }

            if (_mqttClient is IDisposable disposable)
                disposable.Dispose();
        }
        catch { }

        LoggerUtils.Log($"[CabinBoxManager] CleanupMqttClient: 清空 _mqttClient（原值非空={_mqttClient != null}）");
        _mqttClient = null;
        _subscribedBoxDeviceId = null; // 连接已断，订阅状态归零，重连后需重新订阅
        LoggerUtils.Log($"[CabinBoxManager] CleanupMqttClient: 清空 _isConnectedProp（原值非空={_isConnectedProp != null}）");
        _isConnectedProp = null;
        _connectAsyncMethod = null;
        _disconnectAsyncMethod = null;
        _publishAsyncMethod = null;
        _subscribeAsyncMethod = null;
        _unsubscribeAsyncMethod = null;
        _appMsgReceivedAddMethod = null;
        _appMsgReceivedRemoveMethod = null;
        _disconnectedAddMethod = null;
        _disconnectedRemoveMethod = null;
        _atLeastOnceQos = null;
        _appMsgReceivedDelegate = null;
        _disconnectedDelegate = null;
    }

    #endregion

    #region Topic 订阅

    /// <summary>连接成功后订阅当前选中设备，无设备时跳过</summary>
    private void SubscribeBox()
    {
        if (_budBoxData == null || string.IsNullOrEmpty(_budBoxData.deviceId)) return;
        SubscribeBox(_budBoxData.deviceId);
    }

    /// <summary>订阅指定设备的上报 Topic，QoS = AT_LEAST_ONCE；已订阅同一设备时跳过</summary>
    private void SubscribeBox(string deviceId)
    {
        if (!IsConnected) return;
        if (string.IsNullOrEmpty(deviceId)) return;
        if (_subscribedBoxDeviceId == deviceId) return;
        _subscribedBoxDeviceId = deviceId;
        string topic_reported = string.Format(TopicReported, deviceId);
        string topic_reported2App = string.Format(TopicReported2App, deviceId);
        string topic_qrcode = string.Format(TopicQrCode, deviceId);
        Task.Run(() => MqttSubscribeAsync(topic_reported, topic_reported2App, topic_qrcode));
        LoggerUtils.Log($"[CabinBoxManager] 已订阅 Topic: {topic_reported}");
        LoggerUtils.Log($"[CabinBoxManager] 已订阅 Topic: {topic_reported2App}");
        LoggerUtils.Log($"[CabinBoxManager] 已订阅 Topic: {topic_qrcode}");

        // 若扫码时 MQTT 未连接，此处补发 retain
        if (_pendingQrCodeRetainDic.TryGetValue(deviceId, out var pendingQr))
        {
            _pendingQrCodeRetainDic.Remove(deviceId);
            Task.Run(() => MqttPublishRetainAsync(topic_qrcode, pendingQr));
            LoggerUtils.Log($"[CabinBoxManager] 扫码二维码 retain 补发成功: deviceId={deviceId} qrcode={pendingQr}");
        }
    }

    /// <summary>退订指定设备的上报 Topic</summary>
    private void UnsubscribeBox(string deviceId)
    {
        if (!IsConnected) return;
        _subscribedBoxDeviceId = null;
        string topic_reported = string.Format(TopicReported, deviceId);
        string topic_reported2App = string.Format(TopicReported2App, deviceId);
        string topic_qrcode = string.Format(TopicQrCode, deviceId);
        Task.Run(() => MqttUnsubscribeAsync(topic_reported, topic_reported2App, topic_qrcode));
        LoggerUtils.Log($"[CabinBoxManager] 已退订 Topic: {topic_reported}");
        LoggerUtils.Log($"[CabinBoxManager] 已退订 Topic: {topic_reported2App}");
        LoggerUtils.Log($"[CabinBoxManager] 已退订 Topic: {topic_qrcode}");
    }

    /// <summary>订阅硬件上/下线事件 Topic，MQTT 连接成功后调用</summary>
    private void SubscribeEventTopics()
    {
        if (!IsConnected) return;
        Task.Run(() => MqttSubscribeAsync(TopicEventClientConnected, TopicEventClientDisconnected));
        LoggerUtils.Log("[CabinBoxManager] 已订阅设备上/下线事件 Topics");
    }

    /// <summary>退订硬件上/下线事件 Topic，断开连接前调用</summary>
    private void UnsubscribeEventTopics()
    {
        if (!IsConnected) return;
        Task.Run(() => MqttUnsubscribeAsync(TopicEventClientConnected, TopicEventClientDisconnected));
        LoggerUtils.Log("[CabinBoxManager] 已退订设备上/下线事件 Topics");
    }

    #endregion

    #region http协议

    /// <summary>
    /// 拉取账号下所有已绑定的 BudBox 完整数据。
    /// 第一阶段：请求 /box/deviceList 获取设备 ID 列表；
    /// 第二阶段：对每台设备请求 /box/state 获取完整状态；
    /// 所有请求完成后统一赋值 _budBoxDic，并触发 OnBudBoxListInit 广播和 action 回调。
    /// </summary>
    public void InitBudBoxDic(Action<bool> action = null)
    {
        LoggerUtils.Log("[CabinBoxManager] 请求设备列表...");
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CabinBudBoxList, HttpMethod.GET, string.Empty,
            (rspStr) =>
            {
                _budBoxDic.Clear();
                var boxDataList = JsonConvert.DeserializeObject<BudBoxDeviceList>(rspStr);

#if UNITY_EDITOR
                if (CabinTools.isLiuHe)
                {
                    if (boxDataList == null)
                    {
                        boxDataList = new BudBoxDeviceList();
                    }
                    if (boxDataList.deviceList == null)
                    {
                        boxDataList.deviceList = new List<BudBoxDeviceData>();
                    }
                    if (boxDataList.deviceList.Count == 0)
                    {
                        boxDataList.deviceList.Add(new BudBoxDeviceData()
                        {
                            deviceId = "BUD-8D676CF0"
                        });
                    }
                    else
                    {
                        boxDataList.deviceList[0].deviceId = "BUD-8D676CF0";
                    }
                }
#endif

                // 设备列表为空时直接触发回调
                if (boxDataList == null || boxDataList.deviceList == null || boxDataList.deviceList?.Count == 0)
                {
                    LoggerUtils.Log("[CabinBoxManager] 设备列表为空，初始化完成");
                    MessageHelper.Broadcast(MessageName.OnBudBoxListInit);
                    action?.Invoke(true);
                    return;
                }

                // 过滤出有效 deviceId 的设备
                var validDevices = new List<BudBoxDeviceData>();
                foreach (var item in boxDataList.deviceList)
                {
                    if (!string.IsNullOrEmpty(item.deviceId))
                    {
                        validDevices.Add(item);
                    }
                }

                if (validDevices.Count == 0)
                {
                    LoggerUtils.Log("[CabinBoxManager] 无有效 deviceId，初始化完成");
                    MessageHelper.Broadcast(MessageName.OnBudBoxListInit);
                    action?.Invoke(true);
                    return;
                }

                // 用计数器追踪并行状态请求的完成情况
                int pendingCount = validDevices.Count;

                foreach (var device in validDevices)
                {
                    var capturedDevice = device;
                    var req = new JObject();
                    req.Add("deviceId", capturedDevice.deviceId);

                    NetworkManager.Inst.SendHttpRequest<CabinBudBoxData>(
                        HttpUrlDefine.CabinBudBoxState,
                        HttpMethod.GET,
                        req.ToString(),
                        (stateData) =>
                        {
                            if (stateData != null && !string.IsNullOrEmpty(stateData.deviceId))
                            {
                                _budBoxDic[stateData.deviceId] = stateData;
                            }

                            pendingCount--;

                            if (pendingCount <= 0)
                            {
                                LoggerUtils.Log($"[CabinBoxManager] 设备状态拉取完成，共 {_budBoxDic.Count} 个设备");
                                MessageHelper.Broadcast(MessageName.OnBudBoxListInit);
                                action?.Invoke(true);
                            }
                        },
                        (fail) =>
                        {
                            LoggerUtils.LogError($"[CabinBoxManager] 设备 {capturedDevice.deviceId} 状态拉取失败: {fail}");

                            pendingCount--;

                            if (pendingCount <= 0)
                            {
                                LoggerUtils.Log($"[CabinBoxManager] 设备状态拉取完成（部分失败），共 {_budBoxDic.Count} 个设备");
                                MessageHelper.Broadcast(MessageName.OnBudBoxListInit);
                                action?.Invoke(true);
                            }
                        }
                    );
                }
            },
            (fail) =>
            {
                action?.Invoke(false);
                LoggerUtils.LogError($"[CabinBoxManager] 设备列表拉取失败: {fail}，尝试连接 MQTT");
                CabinBoxManager.Inst.ConnectMqttBroker();
            }
        );
    }

    public int GetBudBoxCount()
    {
        return _budBoxDic.Count;
    }

    /// <summary>
    /// 通过 HTTP GET /configuration/hotUpdate/cabin 接口获取最新版本，
    /// 结果写入对应设备的 latestBaseVersion。
    /// deviceId 为 null 时使用当前选中设备。
    /// </summary>
    public void RequestLatestBaseVersion(string basePackVersion, string deviceId = null, string productType = null)
    {
        var resolvedId = deviceId ?? _budBoxData?.deviceId;
        if (string.IsNullOrEmpty(resolvedId))
        {
            return;
        }


        VersionHeader VersionHeader = new VersionHeader()
        {
            version = basePackVersion,
        };

        var req = new JObject();
        req.Add("productType", productType);
        // requestHeader.
        NetworkManager.Inst.SendHttpRequest<CabinBoxHotUpdateData>(
            HttpUrlDefine.CabinBoxHotUpdate,
            HttpMethod.GET,
            req.ToString(),
            (data) =>
            {
                if (data == null || string.IsNullOrEmpty(data.appVersion))
                {
                    return;
                }
                string currentVersion = string.Empty;
                if (_budBoxDic.TryGetValue(resolvedId, out var boxData))
                {
                    boxData.deviceState.latestBasePackVersion = data.appVersion;
                    boxData.deviceState.latestBaseDest = data.appInfo;
                    boxData.deviceState.appDownloadUrl = data.appDownloadUrl ?? string.Empty;
                    boxData.deviceState.latestFirmwareVersion = data.systemVersion ?? string.Empty;
                    boxData.deviceState.firmwareDownloadUrl = data.systemDownloadUrl ?? string.Empty;
                    boxData.deviceState.productDetail = data.productDetail ?? string.Empty;
                    boxData.deviceState.hotUpdateVersion = data.hotUpdateVersion ?? string.Empty;
                    boxData.deviceState.downloadURL = data.downloadURL ?? string.Empty;
                    boxData.deviceState.hotUpdateDest = data.comment ?? string.Empty;
                    currentVersion = boxData.deviceState.currentBasePackVersion;
                }
                if (_budBoxData != null && _budBoxData.deviceId.Equals(resolvedId))
                {
                    _budBoxData.deviceState.latestBasePackVersion = data.appVersion;
                    _budBoxData.deviceState.appDownloadUrl = data.appDownloadUrl ?? string.Empty;
                    _budBoxData.deviceState.latestBaseDest = data.appInfo;

                    _budBoxData.deviceState.latestFirmwareVersion = data.systemVersion ?? string.Empty;
                    _budBoxData.deviceState.firmwareDownloadUrl = data.systemDownloadUrl ?? string.Empty;
                    _budBoxData.deviceState.productDetail = data.productDetail ?? string.Empty;
                    _budBoxData.deviceState.zipInfo = data.zipInfo ?? string.Empty;

                    _budBoxData.deviceState.hotUpdateVersion = data.hotUpdateVersion ?? string.Empty;
                    _budBoxData.deviceState.downloadURL = data.downloadURL ?? string.Empty;
                    _budBoxData.deviceState.hotUpdateDest = data.comment ?? string.Empty;
                }
                LoggerUtils.Log($"[CabinBoxManager] 设备 {resolvedId} 最新底包版本: {data.appVersion}, 下载地址: {data.appDownloadUrl}");
            },
            (fail) =>
            {
                LoggerUtils.LogError($"[CabinBoxManager] 获取底包最新版本失败: {fail}");
            }, sHeader: VersionHeader
        );
    }

    /// <summary>
    /// 比较两个语义版本号，返回 latest 是否严格高于 current。
    /// 格式支持 "x.y.z" 或 "x.y.z.w"；解析失败时返回 false。
    /// </summary>
    private bool IsNewerVersion(string latest, string current)
    {
        if (string.IsNullOrEmpty(latest) || string.IsNullOrEmpty(current))
        {
            return false;
        }
        if (System.Version.TryParse(latest, out var latestVer) &&
            System.Version.TryParse(current, out var currentVer))
        {
            return latestVer > currentVer;
        }
        return false;
    }

    /// <summary>更新指定 BudBox 设备</summary>
    public void UpdateBudBox(string deviceId, string deviceName, Action<bool> callback = null)
    {
        LoggerUtils.Log($"[CabinBoxManager] 更新 Box 名称: deviceId={deviceId}, deviceName={deviceName}");
        var req = new JObject
        {
            ["deviceId"] = deviceId,
            ["deviceName"] = deviceName,
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CabinBoxUpdate, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                if (string.IsNullOrEmpty(rspStr))
                {
                    LoggerUtils.LogError("[CabinBoxManager] 更新 Box 响应为空");
                    callback?.Invoke(false);
                    return;
                }

                var item = JsonConvert.DeserializeObject<CabinBudBoxData>(rspStr);
                if (item == null)
                {
                    LoggerUtils.LogError("[CabinBoxManager] 更新 Box 响应解析失败");
                    callback?.Invoke(false);
                    return;
                }

                if (_budBoxData != null && _budBoxData.deviceId.Equals(item.deviceId))
                {
                    _budBoxData.deviceName = item.deviceName;
                }
                if (_budBoxDic.TryGetValue(item.deviceId, out var data))
                {
                    data.deviceName = item.deviceName;
                }
                LoggerUtils.Log($"[CabinBoxManager] 更新 Box 成功: deviceId={item.deviceId}, deviceName={item.deviceName}");
                callback?.Invoke(true);
                MessageHelper.Broadcast(MessageName.OnBudBoxNameChange, deviceId);
            },
            errRsp =>
            {
                LoggerUtils.LogError($"[CabinBoxManager] 更新 Box 失败: {errRsp}");
                callback?.Invoke(false);
            }
        );
    }

    /// <summary>解绑指定 BudBox 设备</summary>
    public void UnbindBudBox(string deviceId, Action<bool> callback = null)
    {
        LoggerUtils.Log($"[CabinBoxManager] 解绑 Box: deviceId={deviceId}");
        var req = new JObject
        {
            ["deviceId"] = deviceId,
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CabinBoxUnbind, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {
                LoggerUtils.Log($"[CabinBoxManager] 解绑 Box 成功: deviceId={deviceId}");
                // 从本地字典中移除已解绑设备，保持缓存与服务器状态一致
                _budBoxDic.Remove(deviceId);
                // 若解绑的是当前选中设备，清空选中状态
                if (_budBoxData != null && _budBoxData.deviceId == deviceId)
                {
                    ClearBudBoxData();
                }
                // 广播列表变更，触发 IncubationCabinBoxMain 立即刷新
                MessageHelper.Broadcast(MessageName.OnBudBoxListInit);
                callback?.Invoke(true);
            },
            errRsp =>
            {
                LoggerUtils.LogError($"[CabinBoxManager] 解绑 Box 失败: {errRsp}");
                callback?.Invoke(false);
            }
        );
    }


    /// <summary>绑定指定 BudBox 设备</summary>
    public void BindCabinBox(string deviceId, Action<bool> callback = null)
    {
        LoggerUtils.Log($"[CabinBoxManager] 绑定 Box: deviceId={deviceId}");
        var req = new JObject
        {
            ["deviceId"] = deviceId,
        };
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.CabinBoxBind, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            rspStr =>
            {

                var dict = new Dictionary<string, object>();
                dict.Add("box_device_id", deviceId);
                AnalyticsManager.Inst.UserSet(dict);  //上报用户属性 BOX的设备ID

                LoggerUtils.Log($"[CabinBoxManager] 绑定 Box 成功: deviceId={deviceId}");
                callback?.Invoke(true);
            },
            errRsp =>
            {
                LoggerUtils.LogError($"[CabinBoxManager] 绑定 Box 失败: {errRsp}");
                callback?.Invoke(false);
            }
        );
    }



    /// <summary>
    /// 获取角色详情
    /// </summary>
    private void GetCabinCharacterInfo(string cabinCharacterId, Action<bool, CabinCharacterUgcInfo> callback = null)
    {
        LoggerUtils.Log($"[CabinBoxManager] 请求角色详情: cabinCharacterId={cabinCharacterId}");
        var req = new JObject()
        {
            ["id"] = cabinCharacterId,
        };
        NetworkManager.Inst.SendHttpRequest<CabinCharacterDetailData>(HttpUrlDefine.CabinCharacterInfo, HttpMethod.GET,
        JsonConvert.SerializeObject(req),
        rsp =>
        {
            if (rsp == null)
            {
                LoggerUtils.LogError($"[CabinBoxManager] 获取角色详情响应为空: cabinCharacterId={cabinCharacterId}");
                callback?.Invoke(false, null);
                return;
            }
            CabinCharacterUgcInfo cabinCharacterUgcInfo = rsp.characterInfo;
            if (cabinCharacterUgcInfo == null)
            {
                LoggerUtils.LogError($"[CabinBoxManager] 获取角色详情数据为空: cabinCharacterId={cabinCharacterId}");
                callback?.Invoke(false, null);
                return;
            }
            cabinCharacterUgcInfo.activation ??= new();
            cabinCharacterUgcInfo.voiceCommands ??= new();
            cabinCharacterUgcInfo.extensionPackList ??= new();
            LoggerUtils.Log($"[CabinBoxManager] 获取角色详情成功: id={cabinCharacterUgcInfo.id}");
            callback?.Invoke(true, cabinCharacterUgcInfo);
        }, errRsp =>
        {
            LoggerUtils.LogError($"[CabinBoxManager] 获取角色详情失败: {errRsp.rmsg}");
            callback?.Invoke(false, null);
        });
    }


    #endregion

    #region Box 连接
    /// <summary>将新 Box 数据加入本地字典，若已存在则忽略</summary>
    public void AddBudBox(CabinBudBoxData boxData)
    {
        if (boxData == null)
        {
            return;
        }
        if (!_budBoxDic.ContainsKey(boxData.deviceId))
        {
            _budBoxDic.Add(boxData.deviceId, boxData);
        }
        if (IsConnected)
            StartDeviceHeartbeat(boxData.deviceId);
        MessageHelper.Broadcast(MessageName.OnBudBoxListInit);
    }

    /// <summary>
    /// BUD BOX 入口路由：拉取设备列表后根据数量决定跳转目标。
    /// <list type="bullet">
    /// <item>0 台 → 打开绑定设备流程（IncubationCabinLinkBox）</item>
    /// <item>1 台 → 直接进入控制台（JumpToBox）</item>
    /// <item>2+ 台 → 打开设备列表页（IncubationCabinBoxMain）</item>
    /// </list>
    /// </summary>
    public void OpenBoxEntrance()
    {
        ConnectMqttBroker();
        InitBudBoxDic(success =>
        {
            if (!success)
                return;

            var list = GetBudBoxList();

            if (list.Count == 0)
            {
                UIManager.Inst.OpenPanel(PanelId.IncubationCabinLinkBox);
                return;
            }

            if (list.Count == 1)
            {
                JumpToBox(list[0]);
                return;
            }

            UIManager.Inst.OpenPanel(PanelId.IncubationCabinBoxMain);
        });
    }

    public void JumpToBox(CabinBudBoxData boxData)
    {
        PublishAppToken();
        ConnectToBox(boxData);
        StartAllHeartbeatTimers();
    }
    /// <summary>
    /// 连接 Box：退订旧 Topic，订阅新 Topic，广播跳转控制台事件。
    /// </summary>
    public void ConnectToBox(CabinBudBoxData boxData)
    {
        if (boxData == null)
            return;

        if (!IsConnected)
        {
            LoggerUtils.Log($"[CabinBoxManager] MQTT 尚未连接，暂存设备 {boxData.deviceId}，等待连接完成后自动跳转");
            _pendingBoxData = boxData;
            MessageHelper.AddListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnectedForPendingBox);
            return;
        }

        // 切换设备时退订旧 Topic
        if (_budBoxData != null && _budBoxData.deviceId != boxData.deviceId)
            UnsubscribeBox(_budBoxData.deviceId);
        if (!string.IsNullOrEmpty(boxData.deviceState.characterId))
        {
            GetCabinCharacterInfo(boxData.deviceState.characterId, (isSc, charcaterInfo) =>
            {
                if (isSc)
                {
                    boxData.SetCharacterUgcInfo(charcaterInfo);
                    var defaultSkin = CabinTools.GetDefaultSkin(charcaterInfo.skinPack);
                    boxData.deviceState.skinPackId = defaultSkin?.packId ?? string.Empty;
                }
                SetCurBudBoxData(boxData);
            });
        }
        else
        {
            SetCurBudBoxData(boxData);
        }
    }

    /// <summary>
    /// MQTT 连接成功回调：处理连接前已暂存的待跳转设备。
    /// 取消监听后重新执行 ConnectToBox 完成跳转。
    /// </summary>
    private void OnMqttConnectedForPendingBox()
    {
        MessageHelper.RemoveListener(MessageName.OnLinkMqttBrokerFinish, OnMqttConnectedForPendingBox);

        if (_pendingBoxData == null)
            return;

        var data = _pendingBoxData;
        _pendingBoxData = null;
        ConnectToBox(data);
    }

    /// <summary>
    /// 深拷贝 boxData 后存入 _budBoxData，订阅设备 Topic，查询在线状态，并广播跳转控制台事件
    /// </summary>
    private void SetCurBudBoxData(CabinBudBoxData boxData)
    {
        if (boxData == null) return;
        SetActiveBox(boxData);
        // 广播 Box 切换事件：面板已在 window 中时 OpenPanel 不会调用 OnShow，
        // 由监听方主动刷新 UI，确保新仓数据正确显示
        MessageHelper.Broadcast(MessageName.OnActiveBudBoxChanged);
        UIManager.Inst.OpenPanel(PanelId.IncubationCabinControll);
    }

    /// <summary>
    /// 将指定 Box 设置为当前活动 Box，订阅 Topic 并启动心跳，但不打开控制台面板。
    /// 用于需要向 Box 发送指令但不跳转 UI 的场景（如通话弹窗中的导入操作）。
    /// </summary>
    public void SetActiveBox(CabinBudBoxData boxData)
    {
        if (boxData == null) return;
        //string strdata = JsonConvert.SerializeObject(boxData);
        //_budBoxData = JsonConvert.DeserializeObject<CabinBudBoxData>(strdata);
        _budBoxData = boxData;
        SubscribeBox(boxData.deviceId);
        StartDeviceHeartbeat(boxData.deviceId);
    }

    /// <summary>退订当前 Box Topic 并清空选中状态</summary>
    public void ClearBudBoxData()
    {
        if (_budBoxData != null)
            UnsubscribeBox(_budBoxData.deviceId);
        _budBoxData = null;
    }

    #endregion

    #region Box 数据读写

    /// <summary>返回所有已绑定 Box 列表，供 UI 遍历展示</summary>
    public List<CabinBudBoxData> GetBudBoxList() => new List<CabinBudBoxData>(_budBoxDic.Values);

    /// <summary>
    /// 将角色数据写入当前连接的 Box 并通过 MQTT 下发 set_character 指令。
    /// 测试模式下直接同步更新 _budBoxDic 中的对应数据，并广播导入完成消息。
    /// </summary>
    public void ImportBoxCharacterData(CabinCharacterUgcInfo info)
    {
        if (_budBoxData == null || info == null)
            return;
        string strdata = JsonConvert.SerializeObject(info);
        var characterInfo = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(strdata);
        _budBoxData.SetCharacterUgcInfo(characterInfo);
        var confirmedSkin = CabinTools.GetDefaultSkin(characterInfo.skinPack);
        _budBoxData.deviceState.skinPackId = confirmedSkin.packId;
        //var boxData = GetCabinBudBoxData(GetCurrentDeviceId());
        //if (boxData != null)
        //{
        //    boxData.SetCharacterUgcInfo(characterInfo);
        //    boxData.deviceState.skinPackId = confirmedSkin.packId;
        //}
        // 本地缓存已更新，立即广播让 UI 用本地数据先刷新，无需等待 MQTT+HTTP 异步链路
        //MessageHelper.Broadcast(MessageName.OnImportBoxCharacterFinish);
        //MessageHelper.Broadcast(MessageName.OnBoxCharacterChanged, GetCurrentDeviceId());
        SendMqttMessage(MqttMsgOperType.import_character);
    }

    /// <summary>
    /// 获取指定deviceId的BUDBOX
    /// </summary>
    /// <param name="deviceId">不传默认使用临时的数据，传的话就找真实的数据</param>
    /// <returns></returns>
    public CabinBudBoxData GetCabinBudBoxData(string deviceId = null)
    {
        if (string.IsNullOrEmpty(deviceId))
            return _budBoxData;
        if (_budBoxDic.TryGetValue(deviceId, out var data))
            return data;
        return null;
    }
    /// <summary>获取当前 Box 的角色 UGC 数据，未连接时返回 null</summary>
    public CabinCharacterUgcInfo GetBoxCharacterData() => _budBoxData?.characterInfo;

    /// <summary>获取当前已连接 Box 的设备 ID，未连接时返回 null</summary>
    public string GetCurrentDeviceId() => _budBoxData?.deviceId;


    /// <summary>获取指定设备的二维码 JSON 字符串，未收到过上报时返回 null</summary>
    public string GetDeviceQrCode(string deviceId)
    {
        _deviceQrCodeDic.TryGetValue(deviceId, out var qrcode);
        if (string.IsNullOrEmpty(qrcode))
        {
            qrcode = PlayerPrefs.GetString("Device2QRCode_" + deviceId, "");
        }
        return qrcode;
    }

    /// <summary>
    /// 缓存扫码得到的原始 qrcode 字符串，并在 MQTT 已连接时立即发布 retain 消息。
    /// 若 MQTT 尚未连接，数据暂存于 _pendingQrCodeRetainDic，待 SubscribeBox 时补发。
    /// </summary>
    /// <param name="deviceId">设备 ID</param>
    /// <param name="rawQrCode">扫码原始字符串（Base64Url 编码，非 JSON）</param>
    public void CacheQrCodeForRetain(string deviceId, string rawQrCode)
    {
        if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(rawQrCode))
            return;

        // 同时写入本地缓存，供 GetDeviceQrCode 立即可用
        _deviceQrCodeDic[deviceId] = rawQrCode;

        string topic = string.Format(TopicQrCode, deviceId);

        if (IsConnected)
        {
            // MQTT 已连接，立即发布 retain
            Task.Run(() => MqttPublishRetainAsync(topic, rawQrCode));
            LoggerUtils.Log($"[CabinBoxManager] 扫码二维码 retain 已发布: deviceId={deviceId} qrcode={rawQrCode}");
        }
        else
        {
            // MQTT 未连接，暂存待 SubscribeBox 时补发
            _pendingQrCodeRetainDic[deviceId] = rawQrCode;
            LoggerUtils.Log($"[CabinBoxManager] 扫码二维码 retain 暂存（MQTT 未连接）: deviceId={deviceId} qrcode={rawQrCode}");
        }
    }

    /// <summary>
    /// 更新指定设备的在线状态。不传 deviceId 时更新当前选中设备；
    /// 若设备从非 Online 变为 Online，自动触发一次基础数据同步。
    /// </summary>
    public void SetBoxState(BoxState state, string deviceId = null)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            if (_budBoxData != null)
                deviceId = _budBoxData.deviceId;
        }
        if (string.IsNullOrEmpty(deviceId)) return;

        // 任意设备 Online 跃变时拉取全量基础数据（skinPackId 由 sync_baseMsg 回包写入）
        if (_budBoxDic.TryGetValue(deviceId, out var boxData))
        {
            var prev = boxData.deviceState.emBoxState;
            boxData.deviceState.emBoxState = state;

            if (prev != BoxState.Online && state == BoxState.Online)
            {
                SendSyncBaseMsgTo(deviceId);
                // 每次上线都刷新一次 Wi-Fi 数据，保证缓存最新
                SendMqttMessage(MqttMsgOperType.get_wifidata, deviceId: deviceId);
            }
        }

        if (_budBoxData != null && _budBoxData.deviceId.Equals(deviceId))
        {
            _budBoxData.deviceState.emBoxState = state;
            if (state == BoxState.Online)
                PublishAppToken();
        }
    }

    /// <summary>直接向指定设备发送 sync_baseMsg，不依赖 _budBoxData</summary>
    public void SendSyncBaseMsgTo(string deviceId)
    {
        if (!IsConnected || !_budBoxDic.TryGetValue(deviceId, out var boxData)) return;
        BoxControlCmd cmd = new BoxControlCmd
        {
            oper = MqttMsgOperType.sync_baseMsg.ToString(),
            version = ++boxData.deviceState.version,
            reported = new BoxReported()
        };
        string topic = string.Format(TopicSet, deviceId);
        string json = JsonConvert.SerializeObject(cmd);
        Task.Run(() => MqttPublishAsync(topic, json));
        LoggerUtils.Log($"[CabinBoxManager] SendSyncBaseMsgTo -> {topic}");
    }

    public string GetCurrentBaseVersion() => _budBoxData?.deviceState.currentBasePackVersion ?? string.Empty;
    public string GetLatestBaseVersion() => _budBoxData?.deviceState.latestBasePackVersion ?? string.Empty;
    public string GetLatestBaseDest() => _budBoxData?.deviceState.latestBaseDest ?? string.Empty;
    public string GetCurrentFirmwareVersion() => _budBoxData?.deviceState.currentFirmwareVersion ?? string.Empty;
    public string GetLatestFirmwareVersion() => _budBoxData?.deviceState.latestFirmwareVersion ?? string.Empty;
    public string GetFirmwareProductDetail() => _budBoxData?.deviceState.productDetail ?? string.Empty;
    public string GetCurrentHotVersion() => _budBoxData?.deviceState.currentHotVersion ?? string.Empty;
    public string GetLatestHotVersion() => _budBoxData?.deviceState.hotUpdateVersion ?? string.Empty;
    public string GetHotUpdateDest() => _budBoxData?.deviceState.hotUpdateDest ?? string.Empty;


    /// <summary>获取指定设备的在线状态，设备不存在时返回 <see cref="BoxState.None"/></summary>
    public BoxState GetBoxState(string deviceId = null)
    {
        // deviceId 为空时，读取当前选中设备状态；若也无选中设备则视为离线
        if (string.IsNullOrEmpty(deviceId))
        {
            return _budBoxData != null ? _budBoxData.deviceState.emBoxState : BoxState.Offline;
        }
        if (_budBoxDic.TryGetValue(deviceId, out var boxData))
            return boxData.deviceState.emBoxState;
        return BoxState.Offline;
    }

    /// <summary>设置唤醒状态（0 = 待机，1 = 唤醒），不传 deviceId 时更新当前选中设备</summary>
    public void SetActive(int active, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
        {
            return;
        }
        boxData.deviceState.active = active;
    }

    /// <summary>获取唤醒状态（0 = 待机，1 = 唤醒），不传 deviceId 时取当前选中设备，未连接时返回 0</summary>
    public int GetActive(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
        {
            return 0;
        }
        return boxData.deviceState.active;
    }
    public string GetSkinPackId(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
        {
            return string.Empty;
        }
        return boxData.deviceState.skinPackId;
    }
    public void GetSkinPackInfo(Action<SkinPackInfo> action, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
        {
            action?.Invoke(null);
            return;
        }

        if (boxData.characterInfo == null)
        {
            action?.Invoke(null);
            return;
        }

        if (boxData.deviceState == null)
        {
            action?.Invoke(null);
            return;
        }

        var ugcCharacterInfo = boxData.characterInfo;
        string boxSkinPackID = boxData.deviceState.skinPackId;

        // 守卫：skinPack 为空时无法匹配，直接回调 null
        if (ugcCharacterInfo.skinPack == null || ugcCharacterInfo.skinPack.Count == 0)
        {
            LoggerUtils.Log($"[CabinBoxManager] GetSkinPackInfo：ugcCharacterInfo.skinPack 为空，回调 null");
            action?.Invoke(null);
            return;
        }

        if (ugcCharacterInfo.skinPack[0].packId.Equals(boxSkinPackID))
        {
            action?.Invoke(ugcCharacterInfo.skinPack[0]);
            return;
        }

        if (ugcCharacterInfo.extensionPackList?.Count > 0)
        {
            CabinNetManager.Inst.GetExtensionPackBatchInfo(ugcCharacterInfo.extensionPackList, (isS, _List) =>
            {
                if (isS)
                {
                    foreach (var packInfo in _List)
                    {
                        if (packInfo.ugcclass == (int)UGCClass.Published || packInfo.ugcclass == (int)UGCClass.Buy)
                        {
                            // 守卫：extensionPack 的 skinPack 同样可能为空
                            if (packInfo.skinPack == null || packInfo.skinPack.Count == 0)
                                continue;

                            if (packInfo.skinPack[0].packId.Equals(boxSkinPackID))
                            {
                                action?.Invoke(packInfo.skinPack[0]);
                                return;
                            }
                        }
                    }
                }
            });
        }
        else
        {
            action?.Invoke(null);
        }
    }

    /// <summary>设置通话状态（0 = 未通话，1 = 通话中），不传 deviceId 时更新当前选中设备</summary>
    public void SetCall(int call, long callBeginTime, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null) return;
        boxData.deviceState.call = call;
        if (call == 0)
        {
            boxData.deviceState.callBeginTime = 0;
        }
        else if (call == 1)
        {
            if (callBeginTime != -1)
            {
                //约定-1的情况不设置
                boxData.deviceState.callBeginTime = callBeginTime;
            }
        }
    }

    /// <summary>获取通话状态（0 = 未通话，1 = 通话中），不传 deviceId 时取当前选中设备，未连接时返回 0</summary>
    public int GetCall(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
        {
            return 0;
        }
        return boxData.deviceState.call;
    }

    /// <summary>
    /// 设置屏幕状态（0 = 熄屏，1 = 亮屏），不传 deviceId 时更新当前选中设备。
    /// 调用后需配合 <see cref="SendMqttMessage"/> 将新状态下发给硬件。
    /// </summary>
    public void SetScreen(int screen, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return;

        boxData.deviceState.screen = screen;
    }

    /// <summary>获取屏幕状态（0 = 熄屏，1 = 亮屏），不传 deviceId 时取当前选中设备，未连接时返回 1（亮屏）</summary>
    public int GetScreen(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return 1;

        return boxData.deviceState.screen;
    }

    /// <summary>设置屏幕保护开关（0=关闭，1=开启），不传 deviceId 时更新当前选中设备</summary>
    public void SetProtectScreenState(int state, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return;

        boxData.deviceState.protectScreenState = state;
    }

    /// <summary>获取屏幕保护开关（0=关闭，1=开启），未连接时返回默认值 1（开启）</summary>
    public int GetProtectScreenState(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return 1;

        return boxData.deviceState.protectScreenState;
    }

    /// <summary>设置屏幕保护方式（0=定时熄屏，1=自动熄屏），不传 deviceId 时更新当前选中设备</summary>
    public void SetProtectScreenSwith(int swith, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return;

        boxData.deviceState.protectScreenSwith = swith;
    }

    /// <summary>获取屏幕保护方式（0=定时熄屏，1=自动熄屏），未连接时返回默认值 1（自动熄屏）</summary>
    public int GetProtectScreenSwith(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return 1;

        return boxData.deviceState.protectScreenSwith;
    }

    /// <summary>设置定时熄屏时间字符串，格式为"开始分钟数_结束分钟数"，不传 deviceId 时更新当前选中设备</summary>
    public void SetProtectScreenTime(string time, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return;

        boxData.deviceState.protectScreenTime = time;
    }

    /// <summary>获取定时熄屏时间字符串，格式为"开始分钟数_结束分钟数"，未连接时返回默认值"120_420"（2:00-7:00）</summary>
    public string GetProtectScreenTime(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return "120_420";

        return boxData.deviceState.protectScreenTime;
    }

    /// <summary>设置自动熄屏时间（分钟），不传 deviceId 时更新当前选中设备</summary>
    public void SetProtectScreenAuto(int minutes, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return;

        boxData.deviceState.protectScreenAuto = minutes;
    }

    /// <summary>获取自动熄屏时间（分钟），未连接时返回默认值 10 分钟</summary>
    public int GetProtectScreenAuto(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);

        if (boxData == null)
            return 10;

        return boxData.deviceState.protectScreenAuto;
    }

    /// <summary>获取当前 Box 的音量值，未连接时返回 0</summary>
    public int GetBoxVolume() => _budBoxData?.deviceState.volume ?? 50;

    /// <summary>在本地缓存中更新当前 Box 的音量，调用后需手动调用 <seecref=SyncBoxData/> 下发</summary>
    public void SetBoxVolume(int value)
    {
        if (_budBoxData == null)
            return;

        _budBoxData.deviceState.volume = value;
    }

    /// <summary>获取当前 Box 的屏幕亮度值，未连接时返回 0</summary>
    public int GetBoxIuminance(string deviceId = null)
    {
        var boxdata = GetCabinBudBoxData(deviceId);
        return boxdata?.deviceState.brightness ?? 50;
    }

    /// <summary>在本地缓存中更新指定 Box 的屏幕亮度，调用后需手动调用 <seecref="SyncBoxData"/> 下发</summary>
    public void SetBoxIuminance(int value, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
            return;
        boxData.deviceState.brightness = value;
    }

    public void SetWifiInfoList(List<WifiInfo> wifiInfos, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
            return;
        boxData.deviceState.wifiInfoList = wifiInfos;
    }

    /// <summary>获取当前已连接的 Wi-Fi 信息，未连接或无数据时返回 null</summary>
    public WifiInfo GetConnectedWifi(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData?.deviceState.wifiInfoList == null)
            return null;
        return boxData.deviceState.wifiInfoList.Find(w => w.isConnected);
    }

    /// <summary>获取本地缓存中该设备历史成功连接过的 Wi-Fi 列表，无数据时返回空列表</summary>
    public List<WifiInfo> GetOtherWifiList(string deviceId = null)
    {
        var id = string.IsNullOrEmpty(deviceId) ? GetCurrentDeviceId() : deviceId;
        if (string.IsNullOrEmpty(id)) return new List<WifiInfo>();
        var json = PlayerPrefs.GetString(OtherWifiPrefsKey(id), "");
        if (string.IsNullOrEmpty(json)) return new List<WifiInfo>();
        try { return JsonConvert.DeserializeObject<List<WifiInfo>>(json) ?? new List<WifiInfo>(); }
        catch { return new List<WifiInfo>(); }
    }

    /// <summary>将成功连接的 Wi-Fi 写入本地缓存；若该 ssid 已存在则更新 securityType</summary>
    public void MarkWifiConnected(string ssid, string securityType,string password, string deviceId = null)
    {
        if (string.IsNullOrEmpty(ssid)) return;
        var id = string.IsNullOrEmpty(deviceId) ? GetCurrentDeviceId() : deviceId;
        if (string.IsNullOrEmpty(id)) return;

        var list = GetOtherWifiList(id);
        var entry = list.Find(w => w.ssid == ssid);
        if (entry == null)
            list.Add(new WifiInfo { ssid = ssid, securityType = securityType,password = password });
        else
        {
            entry.securityType = securityType;
            entry.password = password;
        }

        PlayerPrefs.SetString(OtherWifiPrefsKey(id), JsonConvert.SerializeObject(list));
        PlayerPrefs.Save();
    }

    private static string OtherWifiPrefsKey(string deviceId) => $"CabinBox_OtherWifi_{deviceId}";

    /// <summary>设置休眠时间（分钟），0 表示不休眠，不传 deviceId 时更新当前选中设备</summary>
    public void SetBoxDormantTimer(int timer, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
            return;
        boxData.deviceState.dormantTimer = timer;
    }

    /// <summary>获取休眠时间（分钟），未连接时返回默认值 1</summary>
    public int GetBoxDormantTimer(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
            return 1;
        return boxData.deviceState.dormantTimer;
    }

    /// <summary>获取设备静音状态</summary>
    public bool GetBoxIsMute(string deviceId = null)
    {
        var boxdata = GetCabinBudBoxData(deviceId);
        if (boxdata == null)
        {
            return false;
        }
        return boxdata.deviceState.souceOff == 0 ? false : true;
    }

    /// <summary>在本地缓存中更新指定 Box 的静音状态</summary>
    public void SetBoxIsMute(int value, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
            return;
        boxData.deviceState.souceOff = value;
    }

    /// <summary>获取静音前保存的音量值；0 表示无保存值</summary>
    public int GetBoxPreMuteVolume(string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        return boxData?.deviceState.preMuteVolume ?? 0;
    }

    /// <summary>保存静音前的音量值；传入 0 表示清除</summary>
    public void SetBoxPreMuteVolume(int value, string deviceId = null)
    {
        var boxData = GetCabinBudBoxData(deviceId);
        if (boxData == null)
            return;
        boxData.deviceState.preMuteVolume = value;
    }

    /// <summary>获取当前 Box 的场景路径，未连接时返回空字符串</summary>
    public string GetBoxScene()
    {
        if (_budBoxData != null)
        {
            return _budBoxData.deviceState.boxId;
        }
        return string.Empty;
    }

    /// <summary>
    /// 将待同步的场景 ID 写入本地 deviceState.scenePath，供 SendMqttMessage(set_scene) 打包发送。
    /// 空字符串表示清除设备场景（对应「默认场景」Item）。
    /// 同时更新 boxInfo.metaDataUrl，供 BudBoxModel 3D 预览使用。
    /// </summary>
    /// <param name="scenePath">场景 ID，空字符串表示清除场景</param>
    /// <param name="metaDataUrl">场景元数据 URL，空或 null 时展示默认盒子外观</param>
    public void SetBoxScenePath(string scenePath, string metaDataUrl = null)
    {
        if (_budBoxData == null)
            return;

        _budBoxData.deviceState.boxId = scenePath ?? string.Empty;

        // 同步更新 boxInfo，供 BudBoxModel 3D 预览读取 metaDataUrl
        if (_budBoxData.boxInfo == null)
        {
            _budBoxData.boxInfo = new CharacterBoxInfo();
        }

        _budBoxData.boxInfo.id = scenePath;
        _budBoxData.boxInfo.metaDataUrl = metaDataUrl ?? string.Empty;
    }

    /// <summary>获取当前 Box 的灯光颜色值（十六进制字符串），未连接时返回空字符串</summary>
    public string GetBoxLight()
    {
        if (_budBoxData != null)
        {
            return _budBoxData.deviceState.sceneLight;
        }
        return string.Empty;
    }

    /// <summary>
    /// 切换当前 Box 角色所使用的皮肤：将旧皮肤的 isDefault 清零，并将指定皮肤的 isDefault 置 1，
    /// 之后触发变更检测广播。
    /// </summary>
    public void SetCharacterUseSkin(SkinPackInfo skin)
    {
        if (_budBoxData == null || _budBoxData.characterInfo == null)
            return;
        if (_budBoxData.characterInfo.skinPack == null)
        {
            return;
        }
        var skinPack = _budBoxData.characterInfo.skinPack;
        // 更新 isDefault：旧的清 0，新的置 1
        foreach (var s in skinPack)
            s.isDefault = 0;
        skin.isDefault = 1;
        _budBoxData.deviceState.skinPackId = skin.packId;
        bool isChange = GetBoxDataChange();
        MessageHelper.Broadcast(MessageName.OnDetectionChange, isChange);
        MessageHelper.Broadcast(MessageName.OnBoxCharacterChanged, GetCurrentDeviceId());
    }

    /// <summary>
    /// 将当前 Box 的 deviceState 从 _budBoxDic 中重新拷贝（深拷贝），
    /// 用于 sync_baseMsg 收到硬件全量数据后刷新本地临时缓存，并广播 OnBudBoxBaseDataRest。
    /// </summary>
    private void RestCurBoxData(string deviceId)
    {
        if (_budBoxData == null)
        {
            return;
        }
        if (!_budBoxDic.TryGetValue(deviceId, out var data))
        {
            return;
        }
        string strdata = JsonConvert.SerializeObject(data.deviceState);
        _budBoxData.deviceState = JsonConvert.DeserializeObject<DeviceState>(strdata);
        MessageHelper.Broadcast(MessageName.OnBudBoxBaseDataRest, deviceId);
    }

    /// <summary>
    /// 将角色信息序列化为 JSON 后计算 MD5，用于与硬件上报值比较。
    /// info 为 null 时返回 string.Empty。
    /// </summary>
    private string ComputeCharacterMd5(CabinCharacterUgcInfo info)
    {
        if (info == null)
            return string.Empty;

        var updateTime = info.updateTime;
        info.updateTime = 0;  //因为每次拉数据的时候 updateTime都会刷新.做个操作 重置为0 确保计算md5正确
        string json = JsonConvert.SerializeObject(info);
        info.updateTime = updateTime;

        byte[] hash = System.Security.Cryptography.MD5.Create()
            .ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 判断数据是否有更变。
    /// 硬件未上报 characterMd5 时返回 false（无法判断），已上报时与服务器侧计算值比较。
    /// </summary>
    public bool GetBoxDataChange()
    {
        if (_budBoxData == null)
            return false;

        var boxData = GetCabinBudBoxData(_budBoxData.deviceId);

        if (boxData == null)
            return false;

        // MD5 为空说明硬件尚未通过 sync_baseMsg 上报，无法判断是否有变更
        if (string.IsNullOrEmpty(boxData.deviceState.characterMd5))
            return false;

        // 计算服务器侧角色数据的 MD5，与硬件上报值比较
        string serverMd5 = ComputeCharacterMd5(_budBoxData.characterInfo);
        return !serverMd5.Equals(boxData.deviceState.characterMd5);
    }

    /// <summary>
    /// 判断本地待同步的 skinPackId 是否与设备已确认的 skinPackId 不一致
    /// </summary>
    public bool IsSkinPackChanged()
    {
        if (_budBoxData == null) return false;
        var deviceData = GetCabinBudBoxData(_budBoxData.deviceId);
        if (deviceData == null) return false;
        return _budBoxData.deviceState.skinPackId != deviceData.deviceState.skinPackId;
    }

    #endregion

    #region MQTT 指令下发

    /// <summary>将指定类型的数据通过 MQTT 同步到 Box 硬件</summary>
    /// <param name="textID">play_voiceCmd 专用：要播放的口令语音包 ID</param>
    /// customDataStr 自定义数据
    public void SendMqttMessage(MqttMsgOperType mqttMsgOperType, UpgradeBoxType upgradeType = UpgradeBoxType.APK, string deviceId = "", string textID = "",string customDataStr = "")
    {
        if (!IsConnected)
        {
            LoggerUtils.LogError("[CabinBoxManager] MQTT 未连接，无法发送指令");
            return;
        }

        var boxData = !string.IsNullOrEmpty(deviceId) ? GetCabinBudBoxData(deviceId) : _budBoxData;

        if (boxData == null)
        {
            TipPanel.ShowToast("设备不存在!");
            return;
        }
        if (GetBoxState(boxData.deviceId) != BoxState.Online)
        {
            TipPanel.ShowToast("设备离线无法设置!");
            return;
        }

        BoxReported reported = new();

        switch (mqttMsgOperType)
        {
            case MqttMsgOperType.import_character:
            case MqttMsgOperType.set_character:
                reported.characterId = boxData.deviceState.characterId.ToString();
                reported.skinPackId = boxData.deviceState.skinPackId;
                break;
            case MqttMsgOperType.delete_character:
                break;
            case MqttMsgOperType.set_volume:
                reported.volume = boxData.deviceState.volume;
                break;
            case MqttMsgOperType.set_brightness:
                reported.brightness = boxData.deviceState.brightness;
                break;
            case MqttMsgOperType.set_httpUidToken:
                reported.uid = AccountDataManager.Inst.Uid;
                reported.token = AccountDataManager.Inst.Token;
                break;
            case MqttMsgOperType.set_active:
                reported.active = boxData.deviceState.active;
                reported.textID = textID;
                break;
            case MqttMsgOperType.set_call:
                reported.call = boxData.deviceState.call;
                reported.callBeginTime = boxData.deviceState.callBeginTime;
                reported.customDataStr = customDataStr;
                break;
            case MqttMsgOperType.set_souceOff:
                reported.isMute = boxData.deviceState.souceOff;
                break;
            case MqttMsgOperType.set_dormant:
                reported.dormantTimer = boxData.deviceState.dormantTimer;
                break;
            case MqttMsgOperType.set_scene:
                // 携带本地已设置好的场景路径（BoxSceneInfo.id）
                reported.boxId = boxData.deviceState.boxId;
                break;
            case MqttMsgOperType.sync_baseMsg:
                // 无需携带参数，硬件收到后主动上报当前全量基础数据
                break;
            case MqttMsgOperType.start_upgrade:
                reported.upgradeType = (int)upgradeType;
                reported.upgradeResult = (int)UpgradeBoxState.StartUpgrade;
                switch (upgradeType)
                {
                    case UpgradeBoxType.APK:
                        reported.appDownloadUrl = boxData.deviceState.appDownloadUrl;
                        reported.latestBasePackVersion = boxData.deviceState.latestBasePackVersion;
                        break;
                    case UpgradeBoxType.Firmware:
                        reported.appDownloadUrl = boxData.deviceState.firmwareDownloadUrl;
                        reported.latestBasePackVersion = boxData.deviceState.latestFirmwareVersion;
                        break;
                    case UpgradeBoxType.HotUpdate:
                        reported.appDownloadUrl = boxData.deviceState.downloadURL;
                        reported.latestBasePackVersion = boxData.deviceState.hotUpdateVersion;
                        break;
                }
                break;
            case MqttMsgOperType.start_rest:
                // 无需携带参数，硬件收到后执行恢复出厂并上报结果
                break;
            case MqttMsgOperType.openBacklight:
            case MqttMsgOperType.closeBacklight:
                // 无需携带参数，硬件收到后执行亮屏/熄屏
                break;
            case MqttMsgOperType.set_screen:
                // 携带当前屏幕状态：0=熄屏，1=亮屏
                reported.screenState = boxData.deviceState.screen;
                break;
            case MqttMsgOperType.set_protectScreenState:
                // 携带屏幕保护开关状态：0=关闭，1=开启
                reported.protectScreenState = boxData.deviceState.protectScreenState;
                reported.protectScreenSwith = boxData.deviceState.protectScreenSwith;
                reported.protectScreenTime = boxData.deviceState.protectScreenTime;
                reported.protectScreenAuto = boxData.deviceState.protectScreenAuto;
                break;
            case MqttMsgOperType.set_protectScreenSwith:
                // 携带屏幕保护方式：0=定时熄屏，1=自动熄屏
                reported.protectScreenSwith = boxData.deviceState.protectScreenSwith;
                reported.protectScreenTime = boxData.deviceState.protectScreenTime;
                reported.protectScreenAuto = boxData.deviceState.protectScreenAuto;
                break;
            case MqttMsgOperType.set_protectScreenTime:
                // 携带定时熄屏时间字符串，格式：开始分钟数_结束分钟数
                reported.protectScreenTime = boxData.deviceState.protectScreenTime;
                break;
            case MqttMsgOperType.set_protectScreenAuto:
                // 携带自动熄屏倒计时时间（分钟）
                reported.protectScreenAuto = boxData.deviceState.protectScreenAuto;
                break;
            case MqttMsgOperType.get_wifidata:
                // 无需携带参数，硬件调用 getConnectedWifiInfo 后通过 curWifiData 回包
                break;
            case MqttMsgOperType.sw2hw_askBleOpen:
                reported.askBleOpenResult = 1; // 1=给硬件添加 WiFi
                break;
            default:
                LoggerUtils.LogError($"[CabinBoxManager] MQTT 发送的指令类型错误 mqttMsgOperType={mqttMsgOperType.ToString()}");
                return;
        }

        boxData.deviceState.version++;

        BoxControlCmd cmd = new BoxControlCmd();
        cmd.oper = mqttMsgOperType.ToString();
        cmd.reported = reported;
        cmd.version = boxData.deviceState.version;

        string topic = string.Format(TopicSet, boxData.deviceId);
        string json = JsonConvert.SerializeObject(cmd);
        Task.Run(() => MqttPublishAsync(topic, json));
        LoggerUtils.Log($"[CabinBoxManager] MQTT Publish -> {topic} : {json}");
    }

    /// <summary>
    /// MQTT 连接成功后，向所有已绑定设备下发 set_httpUidToken 指令，
    /// 内容为当前 App 的 uid 与 HTTP token。
    /// </summary>
    public void PublishAppToken()
    {
        if (!IsConnected || _budBoxDic.Count == 0) return;

        var reported = new BoxReported()
        {
            uid = AccountDataManager.Inst.Uid,
            token = AccountDataManager.Inst.Token
        };

        foreach (var kvp in _budBoxDic)
        {
            kvp.Value.deviceState.version++;
            BoxControlCmd cmd = new BoxControlCmd
            {
                oper = MqttMsgOperType.set_httpUidToken.ToString(),
                version = kvp.Value.deviceState.version,
                reported = reported
            };
            string topic = string.Format(TopicSet, kvp.Key);
            string json = JsonConvert.SerializeObject(cmd);
            Task.Run(() => MqttPublishAsync(topic, json));
            LoggerUtils.Log($"[CabinBoxManager] PublishAppToken -> {topic}");
        }
    }

    #endregion

    #region MQTT 消息接收

    private Task OnMqttMessageReceivedProxy(object e)
    {
        try
        {
            var appMsg = e.GetType().GetProperty("ApplicationMessage")?.GetValue(e);
            string topic = appMsg?.GetType().GetProperty("Topic")?.GetValue(appMsg) as string ?? string.Empty;

            var rawPayload = appMsg?.GetType().GetProperty("Payload")?.GetValue(appMsg);
            byte[] payloadBytes = rawPayload is byte[] b ? b
                : (byte[])(rawPayload?.GetType().GetMethod("ToArray")?.Invoke(rawPayload, null));
            string payload = payloadBytes != null ? Encoding.UTF8.GetString(payloadBytes) : string.Empty;

            MainThreadDispatcher.Enqueue(() =>
            {
                LoggerUtils.Log($"[CabinBoxManager] MQTT Received <- {topic} : {payload}");
                HandleReportedMessage(topic, payload);
            });
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] OnMqttMessageReceivedProxy error: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 解析硬件上报消息，从 topic 中取出 deviceId，找到对应 Box，按 oper 类型更新本地数据。
    /// 若 topic 为 $events/ 开头则转交 <seecref="HandleEventMessage"/> 处理。
    /// </summary>
    private void HandleReportedMessage(string topic, string payload)
    {
        if (topic.StartsWith("$events/"))
        {
            HandleEventMessage(topic, payload);
            return;
        }

        // 从 topic "device/{deviceId}/state/reported" 中解析 deviceId
        var parts = topic.Split('/');
        if (parts.Length < 2)
        {
            LoggerUtils.LogError($"[CabinBoxManager] 无法解析 topic: {topic}");
            return;
        }
        string deviceId = parts[1];

        if (!_budBoxDic.TryGetValue(deviceId, out var boxData))
        {
            LoggerUtils.LogError($"[CabinBoxManager] 未找到设备: {deviceId}");
            return;
        }

        // 收到任意消息，重置离线超时定时器
        ResetOfflineTimer(deviceId);

        // QRCode retain 消息：仅存储二维码数据，无需当前设备匹配，直接返回
        if (topic == string.Format(TopicQrCode, deviceId))
        {
            if (!string.IsNullOrEmpty(payload))
            {
                _deviceQrCodeDic[deviceId] = payload;
                LoggerUtils.Log($"[CabinBoxManager] 收到设备 {deviceId} 二维码 retain 数据: {payload}");
            }

            return;
        }

        // get_hw_isOnline 回包：无论是否是当前选中设备都要处理，优先拦截
        if (payload.Contains(MqttMsgOperType.get_hw_isOnline.ToString()))
        {
            SetBoxState(BoxState.Online, deviceId);
#if UNITY_EDITOR
            // LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 在线确认");
#endif
            MessageHelper.Broadcast(MessageName.OnBoxDeviceStateChanged, deviceId);
            return;
        }

        // sync_baseMsg 回包：非活跃设备也提取 skinPackId，供多设备匹配场景使用
        if (payload.Contains(MqttMsgOperType.sync_baseMsg.ToString()) &&
            (_budBoxData == null || _budBoxData.deviceId != boxData.deviceId))
        {
            try
            {
                var syncMsg = JsonConvert.DeserializeObject<BoxReceivedCmd>(payload);
                if (syncMsg?.reported != null && !string.IsNullOrEmpty(syncMsg.reported.skinPackId))
                {
                    boxData.deviceState.skinPackId = syncMsg.reported.skinPackId;
                    SetCall(syncMsg.reported.call, syncMsg.reported.callBeginTime, deviceId);
                }
            }
            catch { }
            MessageHelper.Broadcast<string>(MessageName.OnBudBoxBaseDataRest, deviceId);
            return;
        }

        if (_budBoxData == null || _budBoxData.deviceId != boxData.deviceId)
        {
            return;
        }

        BoxReceivedCmd msg;
        try
        {
            msg = JsonConvert.DeserializeObject<BoxReceivedCmd>(payload);
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] 消息解析失败: {ex.Message}");
            return;
        }

        if (msg?.reported == null)
            return;

        if (!Enum.TryParse<MqttMsgOperType>(msg.oper, out var operType))
        {
            LoggerUtils.LogError($"[CabinBoxManager] 未知 oper 类型: {msg.oper}");
            return;
        }

        if (msg.version < _budBoxData.deviceState.version)
        {
            //暂时去掉 有概率错误   其实不必要校验这个
            // LoggerUtils.Log($"[MqttHardware] 丢弃旧版本  oper={msg.oper}  version={msg.version} < lastVersion={boxData.deviceState.version}");
            // return;
        }

        boxData.deviceState.version = msg.version;
        _budBoxData.deviceState.version = msg.version;

        switch (operType)
        {
            case MqttMsgOperType.set_volume:
                boxData.deviceState.volume = msg.reported.volume;
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 音量更新: {boxData.deviceState.volume}");
                //MessageHelper.Broadcast(MessageName.OnBudBoxBaseDataRest, deviceId);
                break;
            case MqttMsgOperType.set_brightness:
                SetBoxIuminance(msg.reported.brightness, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 亮度更新: {msg.reported.brightness}");
                //MessageHelper.Broadcast(MessageName.OnBudBoxBaseDataRest, deviceId);
                break;
            case MqttMsgOperType.import_character:
            case MqttMsgOperType.set_character:
                //修改了角色，或者 导入了角色
                if (msg.reported.characterId == boxData.deviceState.characterId || string.IsNullOrEmpty(boxData.deviceState.characterId))
                {
                    string confirmedSkinPackId = msg.reported.skinPackId;
                    GetCabinCharacterInfo(msg.reported.characterId, (isSuccess, characterInfo) =>
                    {
                        if (isSuccess)
                        {
                            boxData.SetCharacterUgcInfo(characterInfo);
                            // 同步确认后的 skinPackId 到设备状态
                            if (string.IsNullOrEmpty(confirmedSkinPackId))
                            {
                                var confirmedSkin = CabinTools.GetDefaultSkin(characterInfo.skinPack);
                                confirmedSkinPackId = confirmedSkin?.packId ?? string.Empty;
                            }

                            boxData.deviceState.skinPackId = confirmedSkinPackId;
                            if (_budBoxData != null && _budBoxData.deviceId == boxData.deviceId)
                                _budBoxData.deviceState.skinPackId = confirmedSkinPackId;

                            if (operType == MqttMsgOperType.import_character)
                            {
                                // 同步成功：将 characterMd5 更新为服务器侧 MD5，使 GetBoxDataChange 返回 false
                                string syncedMd5 = ComputeCharacterMd5(_budBoxData?.characterInfo);
                                boxData.deviceState.characterMd5 = syncedMd5;
                                if (_budBoxData != null && _budBoxData.deviceId == boxData.deviceId)
                                {
                                    _budBoxData.deviceState.characterMd5 = syncedMd5;
                                }
                                MessageHelper.Broadcast(MessageName.OnImportBoxCharacterFinish);
                            }
                            else if (operType == MqttMsgOperType.set_character)
                            {
                                // 同步成功：将 characterMd5 更新为服务器侧 MD5，使 GetBoxDataChange 返回 false
                                string syncedMd5 = ComputeCharacterMd5(_budBoxData?.characterInfo);
                                boxData.deviceState.characterMd5 = syncedMd5;
                                if (_budBoxData != null && _budBoxData.deviceId == boxData.deviceId)
                                {
                                    _budBoxData.deviceState.characterMd5 = syncedMd5;
                                }
                            }
                        }

                        // HTTP 失败时也通知 UI，防止同步状态永久卡在"同步中..."
                        if (operType == MqttMsgOperType.set_character || operType == MqttMsgOperType.import_character)
                        {
                            bool isChange = GetBoxDataChange();
                            MessageHelper.Broadcast(MessageName.OnDetectionChange, isChange);
                        }
                    });
                }
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 角色数据更新");
                //因为需要在走一遍Http的请求所以这里直接返回就行，通过Http的请求回调去判断数据是否有更变。
                return;
            case MqttMsgOperType.delete_character:
                boxData.SetCharacterUgcInfo(null);
                _budBoxData.SetCharacterUgcInfo(null);
                MessageHelper.Broadcast(MessageName.OnDelectBoxCharacterFinish);
                bool isChange = GetBoxDataChange();
                MessageHelper.Broadcast(MessageName.OnDetectionChange, isChange);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 角色数据已删除");
                break;
            case MqttMsgOperType.set_active:
                SetActive(msg.reported.active, deviceId);
                boxData.deviceState.oper_state = msg.reported.oper_state;
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxActiveChange, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 激活状态更新{msg.reported.active} oper_state={msg.reported.oper_state}");
                break;
            case MqttMsgOperType.set_call:
                SetCall(msg.reported.call, msg.reported.callBeginTime, deviceId);
                if (msg.reported.call == 0 && msg.reported.oper_reason == 1)
                {
                    //能量不足挂断
                    TipPanel.ShowToast("当前AI能量不足，通话已结束");
                }
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxCallChange, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 通话状态更新{msg.reported.call}");
                break;
            case MqttMsgOperType.set_screen:
                SetScreen(msg.reported.screenState, deviceId);
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxScreenChange, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 屏幕状态更新 screen={msg.reported.screenState}");
                break;
            case MqttMsgOperType.set_souceOff:
                SetBoxIsMute(msg.reported.isMute, deviceId);
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxBaseDataRest, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 静音设置更新{msg.reported.isMute}");
                break;
            case MqttMsgOperType.set_dormant:
                SetBoxDormantTimer(msg.reported.dormantTimer, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId}  休眠时间更新{msg.reported.dormantTimer}");
                break;
            case MqttMsgOperType.get_wifi:
                SetWifiInfoList(msg.reported.wifiInfoList, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId}  wifi信息更新{msg.reported.dormantTimer}");
                break;
            case MqttMsgOperType.sw2hw_askBleOpen:
                {
                    LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} sw2hw_askBleOpen 回包: {msg.reported.askBleOpenResultData}");
                    AskBleOpenResultData bleResult = null;
                    try { bleResult = JsonConvert.DeserializeObject<AskBleOpenResultData>(msg.reported.askBleOpenResultData); } catch { }
                    if (bleResult != null && bleResult.result)
                    {
                        LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 蓝牙/WiFi 就绪 reason={bleResult.reason}");
                        MessageHelper.Broadcast(MessageName.OnSw2HwBleOpenReady, deviceId);
                    }
                    else
                    {
                        LoggerUtils.LogError($"[CabinBoxManager] 设备 {deviceId} sw2hw_askBleOpen 失败 reason={bleResult?.reason}");
                    }
                    return;
                }
            case MqttMsgOperType.get_wifidata:
                {
                    var wifiList = new List<WifiInfo>();
                    if (!string.IsNullOrEmpty(msg.reported.curWifiData))
                    {
                        var raw = JsonConvert.DeserializeObject<ConnectedWifiRawData>(msg.reported.curWifiData);
                        if (raw != null && raw.connected)
                        {
                            wifiList.Add(new WifiInfo
                            {
                                ssid = raw.ssid,
                                bssid = raw.bssid,
                                level = raw.rssi,
                                frequency = raw.frequency,
                                isConnected = true,
                            });
                        }
                    }
                    SetWifiInfoList(wifiList, deviceId);
                    LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 当前WiFi数据更新: {msg.reported.curWifiData}");
                    MessageHelper.Broadcast(MessageName.OnBudBoxBaseDataRest, deviceId);
                    return;
                }
            case MqttMsgOperType.sync_baseMsg:
                SetWifiInfoList(msg.reported.wifiInfoList, deviceId);
                SetBoxIuminance(msg.reported.brightness, deviceId);

                boxData.deviceState.volume = msg.reported.volume;

                SetActive(msg.reported.active, deviceId);
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxActiveChange, deviceId);

                SetCall(msg.reported.call, msg.reported.callBeginTime, deviceId);
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxCallChange, deviceId);

                SetScreen(msg.reported.screenState, deviceId);
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxScreenChange, deviceId);

                SetBoxIsMute(msg.reported.isMute, deviceId);
                MessageHelper.Broadcast<string>(MessageName.OnBudBoxBaseDataRest, deviceId);

                string reportedSkinPackId = msg.reported.skinPackId ?? string.Empty;
                if (!string.IsNullOrEmpty(reportedSkinPackId))
                {
                    boxData.deviceState.skinPackId = reportedSkinPackId;
                    _budBoxData.deviceState.skinPackId = reportedSkinPackId;
                }

                string BasePackVersion = msg.reported.currentBasePackVersion ?? string.Empty;
                boxData.deviceState.currentBasePackVersion = BasePackVersion;
                _budBoxData.deviceState.currentBasePackVersion = BasePackVersion;

                string FirmwareVersion = msg.reported.currentFirmwareVersion ?? string.Empty;
                boxData.deviceState.currentFirmwareVersion = FirmwareVersion;
                _budBoxData.deviceState.currentFirmwareVersion = FirmwareVersion;

                string HotVersion = msg.reported.currentHotVersion ?? string.Empty;
                boxData.deviceState.currentHotVersion = HotVersion;
                _budBoxData.deviceState.currentHotVersion = HotVersion;

                string productModel = msg.reported.productModel ?? string.Empty;
                boxData.deviceState.productModel = productModel;
                _budBoxData.deviceState.productModel = productModel;

                string snCode = msg.reported.snCode ?? string.Empty;
                boxData.deviceState.snCode = snCode;
                _budBoxData.deviceState.snCode = snCode;

                string productType = msg.reported.productType ?? string.Empty;
                boxData.deviceState.productType = productType;
                _budBoxData.deviceState.productType = productType;

                int protectScreenState = msg.reported.protectScreenState;
                boxData.deviceState.protectScreenState = protectScreenState;
                _budBoxData.deviceState.protectScreenState = protectScreenState;

                int protectScreenSwith = msg.reported.protectScreenSwith;
                boxData.deviceState.protectScreenSwith = protectScreenSwith;
                _budBoxData.deviceState.protectScreenSwith = protectScreenSwith;

                string protectScreenTime = msg.reported.protectScreenTime;
                boxData.deviceState.protectScreenTime = protectScreenTime;
                _budBoxData.deviceState.protectScreenTime = protectScreenTime;

                int protectScreenAuto = msg.reported.protectScreenAuto;
                boxData.deviceState.protectScreenAuto = protectScreenAuto;
                _budBoxData.deviceState.protectScreenAuto = protectScreenAuto;

                if (!string.IsNullOrEmpty(msg.reported.qrcode))
                {
                    _deviceQrCodeDic[deviceId] = msg.reported.qrcode;
                    PlayerPrefs.SetString("Device2QRCode_" + deviceId, msg.reported.qrcode);
                }

                // 提取并存储硬件上报的角色数据 MD5，供 GetBoxDataChange 使用
                string reportedCharMd5 = msg.reported.characterMd5 ?? string.Empty;
                if (!string.IsNullOrEmpty(reportedCharMd5))
                {
                    boxData.deviceState.characterMd5 = reportedCharMd5;
                    _budBoxData.deviceState.characterMd5 = reportedCharMd5;
                }

                RequestLatestBaseVersion(BasePackVersion, deviceId, productType);

                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 基础数据同步完成 {payload}");
                RestCurBoxData(deviceId);

                // 计算数据是否有变更并广播，供控制台「同步到BOX」按钮实时更新
                bool isSyncBaseMsgChange = GetBoxDataChange();
                MessageHelper.Broadcast(MessageName.OnDetectionChange, isSyncBaseMsgChange);
                return;
            case MqttMsgOperType.start_upgrade:
                UpgradeBoxState upgradeBoxState = (UpgradeBoxState)msg.reported.upgradeResult;
                var latestBaseVersion = msg.reported.latestBasePackVersion;
                MessageHelper.Broadcast<UpgradeBoxState>(MessageName.OnBudBoxUpgradeResult, upgradeBoxState);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 升级状态: {upgradeBoxState}");
                return;
            case MqttMsgOperType.start_rest:
                RestBoxState restBoxState = (RestBoxState)msg.reported.restResult;
                if (restBoxState == RestBoxState.RestFinish)
                {
                    //现在因为只有一个box所以就先全部清空了。
                    _budBoxDic.Clear();
                }
                MessageHelper.Broadcast<RestBoxState>(MessageName.OnBudBoxRestInit, restBoxState);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 恢复出厂状态: {restBoxState}");
                return;
            case MqttMsgOperType.set_scene:
                {
                    // 用硬件回包的 scenePath 覆盖本地状态（以硬件确认值为准）
                    string boxId = msg.reported.boxId ?? string.Empty;
                    boxData.deviceState.boxId = boxId;

                    if (_budBoxData != null && _budBoxData.deviceId == boxData.deviceId)
                    {
                        _budBoxData.deviceState.boxId = boxId;
                    }

                    MessageHelper.Broadcast<bool>(MessageName.OnBoxSceneSyncResult, true);
                    LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 场景同步成功，scenePath={boxId}");
                    return;
                }
            default:
                LoggerUtils.Log($"[CabinBoxManager] oper={msg.oper} 无需处理上报数据");
                break;
        }
    }

    /// <summary>
    /// 处理 $events/ 系统事件消息。
    /// 从 payload 中解析 clientid，遍历 _budBoxDic 匹配对应设备（clientid 中包含 deviceId），
    /// 更新 <seecref="DeviceState.emBoxState"/> 并广播 <seecref="MessageName.OnBoxDeviceStateChanged"/>。
    /// </summary>
    private void HandleEventMessage(string topic, string payload)
    {
        BoxEventPayload evt;
        try
        {
            evt = JsonConvert.DeserializeObject<BoxEventPayload>(payload);
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] 事件消息解析失败: {ex.Message}");
            return;
        }

        if (evt == null || string.IsNullOrEmpty(evt.clientid))
            return;

        var arr = evt.clientid.Split("@");
        if (arr == null && arr.Length == 0)
        {
            return;
        }
        // 通过 clientid 是否包含 deviceId 来匹配设备
        string deviceId = arr[0];
        if (!_budBoxDic.TryGetValue(deviceId, out var boxData))
        {
            return;
        }
        if (topic == TopicEventClientConnected)
        {
            TimerManager.Inst.RunOnce("SetBoxStateOnline", 2, () =>
            {
                //连接后马上发送set  发现硬件包还没进入监听，先延后2秒
                SetBoxState(BoxState.Online, deviceId);
                MessageHelper.Broadcast(MessageName.OnBoxDeviceStateChanged, deviceId);
                LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 上线 (clientid={evt.clientid})");
            });
        }
        else
        {
            SetBoxState(BoxState.Offline, deviceId);
            MessageHelper.Broadcast(MessageName.OnBoxDeviceStateChanged, deviceId);

            LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 离线 (clientid={evt.clientid})");
        }

    }

    #endregion

    #region 心跳检测

    /// <summary>向指定设备发送 get_hw_isOnline 心跳探测包</summary>
    private void SendHeartbeatProbe(string deviceId)
    {
        if (!IsConnected || !_budBoxDic.TryGetValue(deviceId, out var boxData)) return;
        string topic = string.Format(TopicSet, deviceId);
        BoxControlCmd cmd = new BoxControlCmd
        {
            oper = MqttMsgOperType.get_hw_isOnline.ToString(),
            version = ++boxData.deviceState.version,
            reported = new()
        };
        string json = JsonConvert.SerializeObject(cmd);
        Task.Run(() => MqttPublishAsync(topic, json));
        LoggerUtils.Log($"[CabinBoxManager] 心跳探测 -> {topic}");
    }

    /// <summary>
    /// 启动指定设备的心跳：立即并每隔 HeartbeatIntervalSec 秒发送探测包，
    /// 同时启动 OfflineTimeoutSec 秒离线定时器。设备状态置为 Connecting。
    /// </summary>
    private void StartDeviceHeartbeat(string deviceId)
    {
        if (!IsConnected || !_budBoxDic.ContainsKey(deviceId)) return;

        CancelDeviceHeartbeat(deviceId);

        // 设备已确认在线时不覆盖状态，避免 SendMqttMessage 被 Online 检测拦截
        if (GetBoxState(deviceId) != BoxState.Online)
        {
            SetBoxState(BoxState.Connecting, deviceId);
            MessageHelper.Broadcast(MessageName.OnBoxDeviceStateChanged, deviceId);
        }

        var hbTimer = TimerManager.Inst.Run($"Heartbeat_{deviceId}", 0, HeartbeatIntervalSec, () =>
        {
            SendHeartbeatProbe(deviceId);
        });
        _heartbeatTimers[deviceId] = hbTimer;

        ResetOfflineTimer(deviceId);
    }

    /// <summary>重置指定设备的离线超时定时器，每次收到该设备任意消息时调用</summary>
    private void ResetOfflineTimer(string deviceId)
    {
        if (!_budBoxDic.ContainsKey(deviceId)) return;

        if (_offlineTimers.TryGetValue(deviceId, out var existing))
        {
            TimerManager.Inst.Stop(existing);
            _offlineTimers.Remove(deviceId);
        }

        var timer = TimerManager.Inst.RunOnce($"OfflineTimeout_{deviceId}", OfflineTimeoutSec, () =>
        {
            _offlineTimers.Remove(deviceId);
            SetBoxState(BoxState.Offline, deviceId);
            LoggerUtils.Log($"[CabinBoxManager] 设备 {deviceId} 超时未收到消息，判定为离线");
            MessageHelper.Broadcast(MessageName.OnBoxDeviceStateChanged, deviceId);
        });
        _offlineTimers[deviceId] = timer;
    }

    /// <summary>停止指定设备的心跳和离线超时定时器</summary>
    private void CancelDeviceHeartbeat(string deviceId)
    {
        if (_heartbeatTimers.TryGetValue(deviceId, out var hb))
        {
            TimerManager.Inst.Stop(hb);
            _heartbeatTimers.Remove(deviceId);
        }
        if (_offlineTimers.TryGetValue(deviceId, out var off))
        {
            TimerManager.Inst.Stop(off);
            _offlineTimers.Remove(deviceId);
        }
    }

    /// <summary>停止所有设备的心跳和离线超时定时器，断开连接时调用</summary>
    private void CancelAllHeartbeatTimers()
    {
        foreach (var t in _heartbeatTimers.Values)
            TimerManager.Inst.Stop(t);
        _heartbeatTimers.Clear();
        foreach (var t in _offlineTimers.Values)
            TimerManager.Inst.Stop(t);
        _offlineTimers.Clear();
    }

    /// <summary>
    /// 订阅 _budBoxDic 中所有设备的上报 Topic。
    /// 用于需要同时监听多台设备消息的场景（如通话弹窗探测所有设备在线状态），
    /// 不受 _subscribedBoxDeviceId 单设备去重守卫限制。
    /// </summary>
    public void SubscribeAllBoxes()
    {
        if (!IsConnected) return;
        foreach (var deviceId in _budBoxDic.Keys)
        {
            string topicReported = string.Format(TopicReported, deviceId);
            string topicReported2App = string.Format(TopicReported2App, deviceId);
            Task.Run(() => MqttSubscribeAsync(topicReported, topicReported2App));
            LoggerUtils.Log($"[CabinBoxManager] SubscribeAllBoxes 已订阅: {topicReported}");
        }
    }

    /// <summary>为 _budBoxDic 中所有设备启动心跳，MQTT 连接成功后调用</summary>
    public void StartAllHeartbeatTimers()
    {
        foreach (var key in _budBoxDic.Keys)
            StartDeviceHeartbeat(key);
    }

    #endregion

    #region MQTT 反射基础设施

    private static bool? _mqttVersionCache;
    private static bool CheckMqttVersion()
    {
        if (_mqttVersionCache.HasValue) return _mqttVersionCache.Value;

        _mqttVersionCache = DeviceInfoManager.Inst.CheckVersion_1_0_19();

        return _mqttVersionCache.Value;
    }
    private void CacheReflectionMetadata()
    {
        var clientType = _mqttClient.GetType();
        var ifaceType = Type.GetType("MQTTnet.Client.IMqttClient, MQTTnet") ?? clientType;

        _isConnectedProp = ifaceType.GetProperty("IsConnected") ?? clientType.GetProperty("IsConnected");
        LoggerUtils.Log($"[CabinBoxManager] _isConnectedProp 反射结果: {_isConnectedProp != null}, ifaceType={ifaceType.Name}, clientType={clientType.Name}");
        // 使用 add_*/remove_* 访问器方法，避免调用 EventInfo.AddEventHandler/RemoveEventHandler（HybridCLR 中会触发 MethodNotFind）
        _appMsgReceivedAddMethod = clientType.GetMethod("add_ApplicationMessageReceivedAsync");
        _appMsgReceivedRemoveMethod = clientType.GetMethod("remove_ApplicationMessageReceivedAsync");
        _disconnectedAddMethod = clientType.GetMethod("add_DisconnectedAsync");
        _disconnectedRemoveMethod = clientType.GetMethod("remove_DisconnectedAsync");
        LoggerUtils.Log($"[CabinBoxManager] 事件访问器: appMsgAdd={_appMsgReceivedAddMethod != null} appMsgRemove={_appMsgReceivedRemoveMethod != null} disconnAdd={_disconnectedAddMethod != null} disconnRemove={_disconnectedRemoveMethod != null}");

        foreach (var m in clientType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var pars = m.GetParameters();
            switch (m.Name)
            {
                case "ConnectAsync" when _connectAsyncMethod == null &&
                    pars.Length >= 1 && pars[0].ParameterType.Name.Contains("MqttClientOptions"):
                    _connectAsyncMethod = m; break;
                case "DisconnectAsync" when _disconnectAsyncMethod == null:
                    _disconnectAsyncMethod = m; break;
                case "PublishAsync" when _publishAsyncMethod == null &&
                    pars.Length >= 1 && pars[0].ParameterType.Name.Contains("MqttApplicationMessage"):
                    _publishAsyncMethod = m; break;
                case "SubscribeAsync" when _subscribeAsyncMethod == null &&
                    pars.Length >= 1 && pars[0].ParameterType.Name.Contains("SubscribeOptions"):
                    _subscribeAsyncMethod = m; break;
                case "UnsubscribeAsync" when _unsubscribeAsyncMethod == null &&
                    pars.Length >= 1 && pars[0].ParameterType.Name.Contains("UnsubscribeOptions"):
                    _unsubscribeAsyncMethod = m; break;
            }
        }

        var qosType = Type.GetType("MQTTnet.Protocol.MqttQualityOfServiceLevel, MQTTnet");
        if (qosType != null)
            _atLeastOnceQos = Enum.Parse(qosType, "AtLeastOnce");
    }

    // Pre-compiled generic adapter: wraps Func<object,Task> into Func<T,Task>.
    // Uses Delegate.CreateDelegate (existing IL) instead of Expression.Compile (dynamic IL).
    // Dynamic IL is stripped by IL2CPP/HybridCLR and causes TypeLoadException.
    private sealed class MqttEventProxy<T>
    {
        private readonly Func<object, Task> _h;
        public MqttEventProxy(Func<object, Task> h) { _h = h; }
        public Task Handle(T e) => _h(e);
    }

    private static object CreateMqttEventDelegate(Type delegateType, Func<object, Task> handler)
    {
        if (delegateType == null) return null;
        var typeArgs = delegateType.GetGenericArguments(); // e.g. [MqttAppMsgReceivedEventArgs, Task]
        if (typeArgs.Length == 0) return null;
        var argType = typeArgs[0];                         // MqttAppMsgReceivedEventArgs
        try
        {
            var proxyType = typeof(MqttEventProxy<>).MakeGenericType(argType);
            var proxy = Activator.CreateInstance(proxyType, handler);
            var handleMethod = proxyType.GetMethod("Handle");
            return Delegate.CreateDelegate(delegateType, proxy, handleMethod);
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] CreateMqttEventDelegate failed ({argType?.Name}): [{ex.GetType().Name}] {ex.Message}");
            return null;
        }
    }

    private static object ReflInvoke(object target, string name, params object[] args)
    {
        var type = target.GetType();
        MethodInfo fallback = null;
        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            if (m.Name != name) continue;
            var pars = m.GetParameters();
            // 提供的参数不能多于方法参数
            if (pars.Length < args.Length) continue;
            // 尾部多余参数必须全部有默认值
            if (pars.Length > args.Length)
            {
                bool allOptional = true;
                for (int i = args.Length; i < pars.Length; i++)
                {
                    if (!pars[i].HasDefaultValue) { allOptional = false; break; }
                }
                if (!allOptional) continue;
            }
            bool match = true;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null) continue;
                // 处理 Nullable<T>：int 可以匹配 int? 参数
                var paramType = pars[i].ParameterType;
                if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    paramType = Nullable.GetUnderlyingType(paramType);
                }
                if (!paramType.IsAssignableFrom(args[i].GetType()))
                { match = false; break; }
            }
            if (match)
            {
                // 为尾部可选参数填入默认值
                var fullArgs = new object[pars.Length];
                Array.Copy(args, fullArgs, args.Length);
                for (int i = args.Length; i < pars.Length; i++)
                {
                    fullArgs[i] = pars[i].DefaultValue;
                }
                return m.Invoke(target, fullArgs);
            }
            fallback ??= m;
        }
        return fallback?.Invoke(target, args);
    }

    private static object[] BuildAsyncArgs(MethodInfo method, params object[] leadingArgs)
    {
        var pars = method.GetParameters();
        var args = new object[pars.Length];
        for (int i = 0; i < leadingArgs.Length && i < pars.Length; i++)
            args[i] = leadingArgs[i];
        for (int i = leadingArgs.Length; i < pars.Length; i++)
        {
            if (pars[i].ParameterType == typeof(CancellationToken))
                args[i] = CancellationToken.None;
            else if (pars[i].HasDefaultValue)
                args[i] = pars[i].DefaultValue;
        }
        return args;
    }

    private Task MqttPublishAsync(string topic, string json)
    {
        if (_mqttClient == null || _publishAsyncMethod == null || _atLeastOnceQos == null)
            return Task.CompletedTask;
        try
        {
            var builderType = Type.GetType("MQTTnet.MqttApplicationMessageBuilder, MQTTnet");
            if (builderType == null) return Task.CompletedTask;
            var builder = Activator.CreateInstance(builderType);
            ReflInvoke(builder, "WithTopic", topic);
            ReflInvoke(builder, "WithPayload", Encoding.UTF8.GetBytes(json));
            ReflInvoke(builder, "WithQualityOfServiceLevel", _atLeastOnceQos);
            ReflInvoke(builder, "WithRetainFlag", false);
            var message = ReflInvoke(builder, "Build");
            return (Task)_publishAsyncMethod.Invoke(_mqttClient, BuildAsyncArgs(_publishAsyncMethod, message));
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] MqttPublishAsync error: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 以 retain=true 发布 MQTT 消息，Broker 会永久保存最新一条，
    /// 新订阅者订阅后立即收到，用于持久化设备二维码等不频繁变更的数据。
    /// </summary>
    private Task MqttPublishRetainAsync(string topic, string payload)
    {
        if (_mqttClient == null || _publishAsyncMethod == null || _atLeastOnceQos == null)
            return Task.CompletedTask;

        try
        {
            var builderType = Type.GetType("MQTTnet.MqttApplicationMessageBuilder, MQTTnet");
            if (builderType == null)
                return Task.CompletedTask;

            var builder = Activator.CreateInstance(builderType);
            ReflInvoke(builder, "WithTopic", topic);
            ReflInvoke(builder, "WithPayload", Encoding.UTF8.GetBytes(payload));
            ReflInvoke(builder, "WithQualityOfServiceLevel", _atLeastOnceQos);
            ReflInvoke(builder, "WithRetainFlag", true);
            var message = ReflInvoke(builder, "Build");
            return (Task)_publishAsyncMethod.Invoke(_mqttClient, BuildAsyncArgs(_publishAsyncMethod, message));
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] MqttPublishRetainAsync error: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    private Task MqttSubscribeAsync(params string[] topics)
    {
        if (_mqttClient == null || _subscribeAsyncMethod == null || _atLeastOnceQos == null)
            return Task.CompletedTask;
        try
        {
            var tfbTypeName = "MQTTnet.MqttTopicFilterBuilder, MQTTnet";
            var sobType = Type.GetType("MQTTnet.Client.MqttClientSubscribeOptionsBuilder, MQTTnet");
            if (sobType == null) return Task.CompletedTask;
            var sob = Activator.CreateInstance(sobType);
            foreach (var t in topics)
            {
                var tfbType = Type.GetType(tfbTypeName);
                if (tfbType == null) break;
                var tfb = Activator.CreateInstance(tfbType);
                ReflInvoke(tfb, "WithTopic", t);
                ReflInvoke(tfb, "WithQualityOfServiceLevel", _atLeastOnceQos);
                var filter = ReflInvoke(tfb, "Build");
                ReflInvoke(sob, "WithTopicFilter", filter);
            }
            var options = ReflInvoke(sob, "Build");
            return (Task)_subscribeAsyncMethod.Invoke(_mqttClient, BuildAsyncArgs(_subscribeAsyncMethod, options));
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] MqttSubscribeAsync error: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    private Task MqttUnsubscribeAsync(params string[] topics)
    {
        if (_mqttClient == null || _unsubscribeAsyncMethod == null)
            return Task.CompletedTask;
        try
        {
            var uobType = Type.GetType("MQTTnet.Client.MqttClientUnsubscribeOptionsBuilder, MQTTnet");
            if (uobType == null) return Task.CompletedTask;
            var uob = Activator.CreateInstance(uobType);
            foreach (var t in topics)
                ReflInvoke(uob, "WithTopicFilter", t);
            var options = ReflInvoke(uob, "Build");
            return (Task)_unsubscribeAsyncMethod.Invoke(_mqttClient, BuildAsyncArgs(_unsubscribeAsyncMethod, options));
        }
        catch (Exception ex)
        {
            LoggerUtils.LogError($"[CabinBoxManager] MqttUnsubscribeAsync error: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    #endregion

}
