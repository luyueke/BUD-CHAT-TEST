// using System;
// using System.Collections.Generic;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
// using UnityEngine;
// using MiHome.Mqtt;
// using Sirenix.OdinInspector;
// using Network;

// /// <summary>
// /// BOX硬件侧 MQTT 控制器（单例，DontDestroyOnLoad）。
// /// 负责连接 MQTT 服务器、订阅指令 Topic、发布状态上报 Topic。
// ///
// /// Topic 约定（cabinMqttReadme）：
// ///   Subscribe : device/{deviceId}/command/set     ← 软件包下发指令
// ///   Publish   : device/{deviceId}/state/reported  ← 硬件执行完后上报
// ///
// /// 版本去重：同一 oper 只处理 version >= 已处理最大值的消息，旧版本直接丢弃。
// /// </summary>
// public class MqttHardwareConnectController : MonoBehaviour
// {
//     public static MqttHardwareConnectController Instance { get; private set; }

//     #region 默认配置（与 AppManager 保持一致）

//     private const string DefaultHost = "mqtt-829rbd2k-bj-public.mqtt.tencenttdmq.com";
//     private const int DefaultPort = 1883;
//     private const string DefaultUsername = "box_client";
//     private const string DefaultPassword = "skdcd62dccf020c030";
//     private const string DefaultInstanceId = "mqtt-829rbd2k";

//     #endregion

//     #region Inspector

//     [Header("MQTT 服务器")]
//     [SerializeField] private string host = DefaultHost;
//     [SerializeField] private int port = DefaultPort;
//     [SerializeField] private string username = DefaultUsername;
//     [SerializeField] private string password = DefaultPassword;
//     [SerializeField] private string instanceId = DefaultInstanceId;

//     [Header("设备 ID（为空时读取 PlayerPrefs[\"deviceId\"]，再为空用 device001）")]
//     [SerializeField] private string deviceId = "";

//     [Header("是否在 Start() 时自动连接")]
//     [SerializeField] private bool connectOnStart = false;

//     #endregion

//     #region 数据模型

//     public class CommandPayload
//     {
//         public string oper;
//         public int version;
//         public ReportedData reported;
//     }

//     /// <summary>set_httpUidToken 指令的 token 数据，对应 reported.httpUidToken 反序列化结果</summary>
//     public class HttpUidTokenData
//     {
//         public string uid;
//         public string token;
//     }

//     /// <summary>
//     /// volume / brightness 在实际数据中可能为字符串 "64" 或整数 64，
//     /// 用 FlexibleIntConverter 统一处理。
//     /// </summary>
//     public class ReportedData
//     {
//         [JsonConverter(typeof(FlexibleIntConverter))]
//         public int volume;

//         [JsonConverter(typeof(FlexibleIntConverter))]
//         public int brightness;

//         public string characterId;
//         public string scenePath;
//         public string sceneLight;
//         /// <summary>set_httpUidToken 专用，不回传 state/reported。值为 JSON 字符串 {"uid":xx,"token":xx}</summary>
//         public string uid;
//         public string token;
//         public bool isHWOnline;
//     }

//     /// <summary>能同时反序列化 JSON number 和 JSON string 为 int</summary>
//     private class FlexibleIntConverter : JsonConverter
//     {
//         public override bool CanConvert(Type t) => t == typeof(int);

//         public override object ReadJson(JsonReader reader, Type t, object existing, JsonSerializer s)
//         {
//             var token = JToken.Load(reader);
//             if (token.Type == JTokenType.Integer) return token.Value<int>();
//             if (token.Type == JTokenType.Float) return (int)token.Value<float>();
//             if (token.Type == JTokenType.String && int.TryParse(token.ToString(), out int n)) return n;
//             return 0;
//         }

//         public override void WriteJson(JsonWriter writer, object value, JsonSerializer s)
//             => writer.WriteValue((int)value);
//     }

//     #endregion

//     #region 事件

//     /// <summary>收到任意指令（topic, payload）</summary>
//     public event Action<string, CommandPayload> OnCommand;

//     /// <summary>set_volume 指令 → volume 值</summary>
//     public event Action<int> OnSetVolume;

//     /// <summary>set_brightness 指令 → brightness 值</summary>
//     public event Action<int> OnSetBrightness;

//     /// <summary>import_character 指令 → characterId</summary>
//     public event Action<string> OnImportCharacter;

//     /// <summary>set_character 指令 → characterId</summary>
//     public event Action<string> OnSetCharacter;

//     /// <summary>delete_character 指令 → characterId</summary>
//     public event Action<string> OnDeleteCharacter;

//     /// <summary>set_scene 指令 → scenePath</summary>
//     public event Action<string> OnSetScene;

//     /// <summary>set_light 指令 → sceneLight</summary>
//     public event Action<string> OnSetLight;

//     /// <summary>set_httpUidToken 指令 → uid/token（不回传 state/reported）</summary>
//     public event Action<HttpUidTokenData> OnSetHttpUidToken;

//     /// <summary>MQTT 已连接</summary>
//     public event Action OnConnected;

//     /// <summary>MQTT 已断开，参数为原因</summary>
//     public event Action<string> OnDisconnected;

//     #endregion

//     #region 属性

//     public bool IsConnected => MqttManager.Instance != null && MqttManager.Instance.IsConnected;

//     #endregion

//     private string _deviceId;
//     private bool _subscribed;
//     // key = oper，value = 已处理的最大 version
//     private readonly Dictionary<string, int> _lastVersion = new Dictionary<string, int>();

//     #region Unity 生命周期

//     private void Awake()
//     {
//         if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//         gameObject.transform.SetParent(null);
//     }

//     private void Start()
//     {
//         // if (connectOnStart) Connect();
//     }

//     private void OnDestroy()
//     {
//         UnregisterMqttEvents();
//         if (Instance == this) Instance = null;
//     }

//     #endregion

//     #region 公共 API

//     public void SetDeviceAndConnect(string device)
//     {
//         _deviceId = device;
//         Connect(device);
//     }

//     /// <summary>
//     /// 连接 MQTT。可传入 deviceId 覆盖 Inspector 配置。
//     /// 重复调用会先取消注册旧事件再重连，防止重复订阅。
//     /// </summary>
//     public void Connect(string overrideDeviceId = null)
//     {
//         _deviceId = !string.IsNullOrEmpty(overrideDeviceId) ? overrideDeviceId
//                   : !string.IsNullOrEmpty(deviceId) ? deviceId
//                   : PlayerPrefs.GetString("deviceId", "device001");

//         // _deviceId = "deviceID"; //测试用

//         _subscribed = false;
//         _lastVersion.Clear();
//         UnregisterMqttEvents();
//         RegisterMqttEvents();

//         // 硬件侧 clientId 加 _hw 后缀，避免与软件侧（APP）因相同 clientId 互相踢连接
//         var config = new MqttConfig
//         {
//             host = string.IsNullOrEmpty(host) ? DefaultHost : host,
//             port = port > 0 ? port : DefaultPort,
//             username = string.IsNullOrEmpty(username) ? DefaultUsername : username,
//             password = string.IsNullOrEmpty(password) ? DefaultPassword : password,
//             instanceId = string.IsNullOrEmpty(instanceId) ? DefaultInstanceId : instanceId,
//             clientId = $"{_deviceId}_hw",
//         };

//         config.clientId = _deviceId;

//         Debug.Log($"[MqttHardware] Connect  deviceId={_deviceId}  host={config.host}:{config.port}");
//         MqttManager.Instance.Connect(config);
//     }

//     /// <summary>断开 MQTT 连接</summary>
//     public void Disconnect()
//     {
//         MqttManager.Instance.Disconnect();
//     }

//     /// <summary>
//     /// 向 device/{deviceId}/state/reported 发布状态上报。
//     /// 被软件包和服务端监听，用于设备最终一致性确认。
//     /// </summary>
//     public void ReportState(string oper, ReportedData reported, int version = 0)
//     {
//         PublishReport($"device/{_deviceId}/state/reported", oper, reported, version);
//     }

//     /// <summary>
//     /// 向 device/{deviceId}/state/reported2App 发布状态上报。
//     /// 仅被 APP 监听，用于 set_httpUidToken / get_hw_isOnline 等指令的回传确认。
//     /// </summary>
//     public void ReportState2App(string oper, ReportedData reported, int version = 0)
//     {
//         PublishReport($"device/{_deviceId}/state/reported2App", oper, reported, version);
//     }

//     private void PublishReport(string topic, string oper, ReportedData reported, int version)
//     {
//         if (!IsConnected)
//         {
//             Debug.LogWarning("[MqttHardware] 未连接，无法上报状态");
//             return;
//         }
//         string payload = JsonConvert.SerializeObject(new CommandPayload
//         {
//             oper = oper,
//             version = version,
//             reported = reported,
//         });
//         MqttManager.Instance.Publish(topic, payload);
//         Debug.LogError($"[MqttHardware] 上报  topic={topic}  oper={oper}  reported={JsonConvert.SerializeObject(reported)}");
//     }

//     /// <summary>
//     /// 硬件主动同步当前音量（oper = sync_volume）。
//     /// 例如用户通过物理按钮调整了音量后主动上报。
//     /// </summary>
//     public void SyncVolume(int volume, int version = 0)
//     {
//         ReportState("sync_volume", new ReportedData { volume = volume }, version);
//     }

//     #endregion

//     #region MQTT 事件处理

//     private void RegisterMqttEvents()
//     {
//         MqttManager.Instance.OnConnected += HandleConnected;
//         MqttManager.Instance.OnDisconnected += HandleDisconnected;
//         MqttManager.Instance.OnMessage += HandleMessage;
//     }

//     private void UnregisterMqttEvents()
//     {
//         if (MqttManager.Instance == null) return;
//         MqttManager.Instance.OnConnected -= HandleConnected;
//         MqttManager.Instance.OnDisconnected -= HandleDisconnected;
//         MqttManager.Instance.OnMessage -= HandleMessage;
//     }

//     private void HandleConnected()
//     {
//         if (!_subscribed)
//         {
//             _subscribed = true;
//             string topic = $"device/{_deviceId}/command/set";
//             MqttManager.Instance.Subscribe(topic);
//             Debug.Log($"[MqttHardware] 已连接，订阅 {topic}");
//         }
//         OnConnected?.Invoke();
//     }

//     private void HandleDisconnected(string reason)
//     {
//         _subscribed = false;
//         Debug.LogWarning($"[MqttHardware] 断开: {reason}");
//         OnDisconnected?.Invoke(reason);
//     }

//     private void HandleMessage(string topic, string payload)
//     {
//         if (topic != $"device/{_deviceId}/command/set") return;

//         Debug.LogError($"[MqttHardware] 收到指令  payload={payload}");

//         CommandPayload cmd;
//         try
//         {
//             cmd = JsonConvert.DeserializeObject<CommandPayload>(payload);
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"[MqttHardware] 指令解析失败: {e.Message}");
//             return;
//         }
//         if (cmd == null) return;

//         // ── 版本去重：同一 oper 只处理 version >= 已处理最大值的消息 ──
//         int lastVer = _lastVersion.TryGetValue(cmd.oper, out int v) ? v : int.MinValue;
//         if (cmd.version < lastVer)
//         {
//             Debug.Log($"[MqttHardware] 丢弃旧版本  oper={cmd.oper}  version={cmd.version} < lastVersion={lastVer}");
//             return;
//         }
//         _lastVersion[cmd.oper] = cmd.version;

//         OnCommand?.Invoke(topic, cmd);

//         switch (cmd.oper)
//         {
//             case "set_volume":
//                 Debug.Log($"[MqttHardware] set_volume  volume={cmd.reported?.volume}");
//                 OnSetVolume?.Invoke(cmd.reported?.volume ?? 0);
//                 break;
//             case "set_brightness":
//                 Debug.Log($"[MqttHardware] set_brightness  brightness={cmd.reported?.brightness}");
//                 OnSetBrightness?.Invoke(cmd.reported?.brightness ?? 0);
//                 break;
//             case "import_character":
//                 OnImportCharacter?.Invoke(cmd.reported?.characterId);
//                 break;
//             case "set_character":
//                 OnSetCharacter?.Invoke(cmd.reported?.characterId);
//                 break;
//             case "delete_character":
//                 OnDeleteCharacter?.Invoke(cmd.reported?.characterId);
//                 break;
//             case "set_scene":
//                 Debug.Log($"[MqttHardware] set_scene  scenePath={cmd.reported?.scenePath}");
//                 OnSetScene?.Invoke(cmd.reported?.scenePath);
//                 break;
//             case "set_light":
//                 Debug.Log($"[MqttHardware] set_light  sceneLight={cmd.reported?.sceneLight}");
//                 OnSetLight?.Invoke(cmd.reported?.sceneLight);
//                 break;
//             case "get_hw_isOnline":
//                 Debug.Log("[MqttHardware] get_hw_isOnline → 回传 isHWOnline=true (reported2App)");
//                 ReportState2App("get_hw_isOnline", new ReportedData { isHWOnline = true }, cmd.version);
//                 return;
//             case "set_httpUidToken":
//                 {
//                     Debug.LogError("[MqttHardware] set_httpUidToken");
//                     if (!string.IsNullOrEmpty(cmd.reported?.uid) && !string.IsNullOrEmpty(cmd.reported?.token))
//                     {
//                         var tokenInfo = new Dictionary<string, string>
//                         {
//                             ["uid"]         = cmd.reported.uid,
//                             ["token"]       = cmd.reported.token,
//                             ["environment"] = "master",
//                         };
//                         NetworkManager.Inst.SetHttpTokenInfo(tokenInfo);

//                         PlayerPrefs.SetString("Cabin_uid",cmd.reported.uid);
//                         PlayerPrefs.SetString("Cabin_token",cmd.reported.token);
//                     }
//                     // 回传 uid/token 到 reported2App（APP 监听确认）
//                     ReportState2App("set_httpUidToken", new ReportedData
//                     {
//                         uid   = cmd.reported?.uid,
//                         token = cmd.reported?.token,
//                     }, cmd.version);
//                     return;
//                 }
//             default:
//                 Debug.LogWarning($"[MqttHardware] 未知 oper: {cmd.oper}");
//                 break;
//         }

//         // 原封不动回传 state/reported，供软件包和服务端做最终一致性确认
//         ReportState(cmd.oper, cmd.reported, cmd.version);
//     }

//     #endregion


//     [Header("── 测试用 ──")]
//     [SerializeField] private int _testVolume = 50;
//     [SerializeField] private int _testBrightness = 50;
//     [SerializeField] private string _testCharacterId = "test_character_001";
//     [SerializeField] private string _testScenePath = "";
//     [SerializeField] private string _testSceneLight = "";
//     [SerializeField] private string _testHttpUid = "";
//     [SerializeField] private string _testHttpToken = "";

//     [Button("测试收到角色指令")]
//     void TestReceiveCharacterCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 set_character  characterId={_testCharacterId}");
//         OnSetCharacter?.Invoke(_testCharacterId);
//     }

//     [Button("测试收到亮度指令")]
//     void TestReceiveBrightnessCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 set_brightness  brightness={_testBrightness}");
//         OnSetBrightness?.Invoke(_testBrightness);
//     }

//     [Button("测试收到音量指令")]
//     void TestReceiveVolumeCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 set_volume  volume={_testVolume}");
//         OnSetVolume?.Invoke(_testVolume);
//     }

//     [Button("测试收到删除角色指令")]
//     void TestReceiveDeleteCharacterCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 delete_character  characterId={_testCharacterId}");
//         OnDeleteCharacter?.Invoke(_testCharacterId);
//     }

//     [Button("测试收到场景指令")]
//     void TestReceiveSetSceneCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 set_scene  scenePath={_testScenePath}");
//         OnSetScene?.Invoke(_testScenePath);
//     }

//     [Button("测试收到灯光指令")]
//     void TestReceiveSetLightCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 set_light  sceneLight={_testSceneLight}");
//         OnSetLight?.Invoke(_testSceneLight);
//     }

//     [Button("测试收到 HttpUidToken 指令")]
//     void TestReceiveSetHttpUidTokenCmd()
//     {
//         Debug.Log($"[MqttHardware] 模拟 set_httpUidToken  uid={_testHttpUid}");
//         OnSetHttpUidToken?.Invoke(new HttpUidTokenData { uid = _testHttpUid, token = _testHttpToken });
//     }
// }
