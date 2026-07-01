
using Game.BLE;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Game.BudBox
{
    #region MQTT 消息结构
    /// <summary>
    /// 发送给 Box 的控制指令
    /// Topic: device/{deviceId}/command/set
    /// </summary>
    public class BoxControlCmd
    {
        /// <summary>操作类型，对应 <see cref="MqttMsgOperType"/> 枚举名称字符串</summary>
        public string oper;
        /// <summary>指令版本号，每次下发时自增，用于去重和顺序校验</summary>
        public int version;
        /// <summary>指令携带的参数键值对，内容因 oper 类型而异</summary>
        public BoxReported reported;
    }

    /// <summary>
    /// 从 Box 硬件接收的上报消息
    /// Topic: device/{deviceId}/state/reported
    /// </summary>
    public class BoxReceivedCmd
    {
        /// <summary>操作类型字符串，用于判断 reported 的具体结构</summary>
        public string oper;
        /// <summary>消息版本号，与下行指令版本对应</summary>
        public int version;
        /// <summary>上报数据，所有 oper 类型共用，按需读取对应字段</summary>
        public BoxReported reported;
    }

    /// <summary>askBleOpenResultData 字段的 JSON 结构，sw2hw_askBleOpen 回包反序列化用</summary>
    public class AskBleOpenResultData
    {
        /// <summary>原因（1=给硬件添加 WiFi）</summary>
        public int reason;
        /// <summary>操作是否成功</summary>
        public bool result;
    }

    /// <summary>getConnectedWifiInfo 回调的原始 JSON 结构，curWifiData 字段反序列化用</summary>
    public class ConnectedWifiRawData
    {
        public bool connected;
        public string ssid;
        public string bssid;
        public int rssi;
        public int linkSpeed;
        public int frequency;
    }

    /// <summary>
    /// MQTT硬件上报消息的统一数据结构，涵盖所有 oper 类型的字段。
    /// JSON 反序列化时不存在的字段保持默认值，按 oper 类型取用即可。
    /// </summary>
    public class BoxReported
    {
        public string skinPackId; //当前使用的皮肤id
        public string uid;
        public string token;

        /// <summary> 设备音量 </summary>
        public int volume;
        /// <summary> 屏幕亮度 </summary>
        public int brightness;
        /// <summary> 角色ID </summary>
        public string characterId;
        /// <summary> boxID </summary>
        public string boxId;
        /// <summary> 场景灯光 </summary>
        public string sceneLight;
        /// <summary> 硬件包在线状态 </summary>
        public bool isHWOnline;
        /// <summary> 0静音 1有声音 </summary>
        public int isMute;
        /// <summary> 休眠时间 </summary>
        public int dormantTimer;
        /// <summary> 唤醒状态 </summary>
        public int active;
        /// <summary> 通话状态 </summary>
        public int call;
        /// <summary>获取硬件网络信息(当前使用的wifi，已添加的wifi)</summary>
        public List<WifiInfo> wifiInfoList;
        /// <summary>当前设备底包版本</summary>
        public string currentBasePackVersion;
        /// <summary>服务器最新底包版本</summary>
        public string latestBasePackVersion;
        /// <summary>当前固件版本信息</summary>
        public string currentFirmwareVersion;
        /// <summary>最新固件版本信息</summary>
        public string latestFirmwareVersion;
        /// <summary>当前热更到的版本信息</summary>
        public string currentHotVersion;

        /// <summary>升级结果，直接对应 <see cref="UpgradeBoxState"/>：0=None，1=StartUpgrade，2=UpgradeSuccess，3=UpgradeFail</summary>
        public int upgradeResult;
        /// <summary>升级类型，直接对应 <see cref="UpgradeBoxType"/>：1=APK，2=Firmware</summary>
        public int upgradeType;
        /// <summary>恢复出厂结果，直接对应 <see cref="RestBoxState"/>：0=None，1=StartRest，2=RestFinish，3=RestFail</summary>
        public int restResult;
        /// <summary>get_wifidata 回包：当前已连接 WiFi 的 JSON 字符串（getConnectedWifiInfo 原始数据）</summary>
        public string curWifiData;
        /// <summary>sw2hw_askBleOpen 下行：通知硬件开启蓝牙的原因（1=给硬件添加 WiFi）</summary>
        public int askBleOpenResult;
        /// <summary>sw2hw_askBleOpen 回包：硬件返回的结果 JSON 字符串，结构为 {reason:xx,result:true}</summary>
        public string askBleOpenResultData;
        /// <summary>start_upgrade 下行：底包 OTA 下载地址</summary>
        public string appDownloadUrl;

        /// <summary>设备型号</summary>
        public string productModel;
        /// <summary>设备SN码</summary>
        public string snCode;
        /// <summary>产品类型</summary>
        public string productType;

        /// <summary>设备二维码 JSON 字符串，用于 BLE 重连</summary>
        public string qrcode;

        public long callBeginTime; //时间戳  调用set_call进入通话时 会在收到set_call里面收到callBeginTime。通话开始时间

        /// <summary>硬件当前角色数据的 MD5 值，sync_baseMsg 时由硬件上报</summary>
        public string characterMd5;

        /// <summary>play_voiceCmd 下行：要播放的口令语音包 ID</summary>
        public string textID;

        /// <summary>操作状态：1=box同意操作进行中 2=box操作结束 3=box拒绝操作</summary>
        public int oper_state;

        /// <summary>
        /// 操作原因
        /// </summary>
        public int oper_reason;  //0:无原因

        /// <summary>屏幕状态--0熄屏-1亮屏</summary>
        public int screenState;

        /// <summary>屏幕保护开关 0关闭 1开启</summary>
        public int protectScreenState;

        /// <summary>屏幕保护方式 0定时熄屏 1自动熄屏</summary>
        public int protectScreenSwith;

        /// <summary>定时熄屏时间，格式：开始分钟数_结束分钟数，如"120_420"表示2:00-7:00</summary>
        public string protectScreenTime;

        /// <summary>自动熄屏时间（分钟）</summary>
        public int protectScreenAuto;

        public string customDataStr;

    }


    /// <summary>MQTT 系统事件($events/client_connected 等)消息体</summary>
    public class BoxEventPayload
    {
        /// <summary>触发事件的客户端 ID</summary>
        public string clientid;
    }

    #endregion

    #region Box 设备数据

    /// <summary>
    /// HTTP同步的Box的数据
    /// </summary>
    public class DeviceState
    {
        /// <summary>当前设备连接状态</summary>
        public BoxState emBoxState = BoxState.Offline;

        /// <summary>本地下行指令版本号，每次调用 <seecref="CabinBoxManager.SyncBoxData"/> 时自增</summary>
        public int version;

        /// <summary> 当前角色ID </summary>
        public string characterId;

        /// <summary>设备当前音量（0~100）</summary>
        public int volume = 50;

        /// <summary>设备当前屏幕亮度（0~100）</summary>
        public int brightness = 50;

        /// <summary>唤醒状态-0待机-1唤醒</summary>
        public int active = 0;

        /// <summary>通话状态--0挂断-1通话</summary>
        public int call = 0;

        /// <summary>本次通话开始的 Unix 时间戳（秒），call 变为 1 时写入，call 变为 0 时清零</summary>
        public long callBeginTime = 0;

        /// <summary>设置休眠时间--具体休眠时间分钟</summary>
        public int dormantTimer = 1;

        /// <summary>设置静音--0关闭静音，1开启静音</summary>
        public int souceOff = 1;

        /// <summary>设置语音唤醒--0关闭语音唤醒，1开启语音唤醒</summary>
        public int voiceActive = 1;

        /// <summary>设置语音休眠--0关闭语音休眠，1开启语音休眠</summary>
        public int voiceDormant = 1;

        /// <summary>获取硬件网络信息(当前使用的wifi，已添加的wifi)</summary>
        public List<WifiInfo> wifiInfoList;

        /// <summary>当前设备底包版本，由 sync_baseMsg 上报写入</summary>
        public string currentBasePackVersion = string.Empty;
        /// <summary>服务器最新版本，由 HTTP /configuration/hotUpdate/cabin 接口写入；高于 currentBaseVersion 时显示升级入口</summary>
        public string latestBasePackVersion = string.Empty;
        /// <summary>服务器底包下载地址，由 HTTP /configuration/hotUpdate/cabin 接口写入</summary>
        public string appDownloadUrl = string.Empty;
        /// <summary>最新版本信息</summary>
        public string latestBaseDest = string.Empty;

        /// <summary>当前设备固件系统版本，由 sync_baseMsg 上报写入</summary>
        public string currentFirmwareVersion = string.Empty;
        /// <summary>当前最新固件系统版本，由 HTTP /configuration/hotUpdate/cabin 接口写入</summary>
        public string latestFirmwareVersion = string.Empty;
        /// <summary>服务器固件下载地址，由 HTTP /configuration/hotUpdate/cabin 接口写入</summary>
        public string firmwareDownloadUrl = string.Empty;
        /// <summary>最新固件信息，由 HTTP /configuration/hotUpdate/cabin 接口写入</summary>
        public string zipInfo = string.Empty;

        /// <summary>当前硬件的热更版本，由 sync_baseMsg 上报写入</summary>
        public string currentHotVersion = string.Empty;
        /// <summary>最新的热更版本，由 HTTP /configuration/hotUpdate/cabin  上报写入</summary>
        public string hotUpdateVersion = string.Empty;
        /// <summary>服务器热更下载地址，由 HTTP /configuration/hotUpdate/cabin 接口写入</summary>
        public string downloadURL = string.Empty;
        /// <summary>热更描述（comment 字段），由 HTTP /configuration/hotUpdate/cabin 接口写入</summary>
        public string hotUpdateDest = string.Empty;

        /// <summary>当前设备的场景id</summary>
        public string boxId = string.Empty;

        /// <summary>当前设备的场景灯光</summary>
        public string sceneLight = string.Empty;

        /// <summary>设备型号</summary>
        public string productModel = string.Empty;

        /// <summary>设备sn码</summary>
        public string snCode = string.Empty;

        /// <summary>产品类型</summary>
        public string productType = string.Empty;

        /// <summary>固件升级内容描述</summary>
        public string productDetail = string.Empty;

        /// <summary>当前皮肤包ID，本地待同步态与硬件确认态均存储在此字段</summary>
        public string skinPackId;

        /// <summary>静音前保存的音量值（0 表示未保存），用于取消静音时恢复音量</summary>
        public int preMuteVolume = 0;

        /// <summary>硬件上报的角色数据 MD5，由 sync_baseMsg 写入，用于与服务器侧 MD5 比较</summary>
        public string characterMd5 = string.Empty;

        /// <summary>
        /// box同步某一个操作的状态
        /// </summary>
        public int oper_state = 1; //1:box同意操作 2:box同意操作结束 3:box拒绝操作 (比如一些需要时间的操作,box操作结束后同步状态)

        /// <summary>屏幕状态--0熄屏-1亮屏，默认亮屏</summary>
        public int screen = 1;

        /// <summary>屏幕保护开关 0关闭 1开启，默认开启</summary>
        public int protectScreenState = 1;

        /// <summary>屏幕保护方式 0定时熄屏 1自动熄屏，默认自动熄屏</summary>
        public int protectScreenSwith = 1;

        /// <summary>定时熄屏时间，格式：开始分钟数_结束分钟数，默认2:00-7:00即"120_420"</summary>
        public string protectScreenTime = "120_420";

        /// <summary>自动熄屏时间（分钟），默认10分钟</summary>
        public int protectScreenAuto = 10;
    }

    /// <summary>单台 BUD BOX 的完整本地数据，包含设备标识、角色信息和运行时状态</summary>
    public class CabinBudBoxData
    {
        /// <summary>服务器下发的设备唯一标识，用于拼接 MQTT Topic</summary>
        public string deviceId = string.Empty;
        /// <summary>BUD BOX 设备名称</summary>
        public string deviceName = string.Empty;
        /// <summary>当前绑定在 Box 上的角色信息</summary>
        [JsonProperty("characterInfo")]
        public CabinCharacterUgcInfo characterInfo
        {
            get; private set;
        }

        /// <summary>当前设备绑定的盒子场景信息，用于 3D 模型预览和纹理加载</summary>
        public CharacterBoxInfo boxInfo;

        /// <summary>设备运行时状态（音量、亮度、连接状态等）</summary>
        public DeviceState deviceState = new DeviceState();

        public CabinBudBoxData(string deviceId = "TAB-502B398B")
        {
            this.deviceId = deviceId;
        }

        public void SetCharacterUgcInfo(CabinCharacterUgcInfo info)
        {
            characterInfo = info;
            if (characterInfo == null)
            {
                deviceState.characterId = string.Empty;
            }
            else
            {
                deviceState.characterId = characterInfo.id;
            }
        }


        /// <summary>
        /// 比较两个 Box 数据是否一致（目前仅对比 <see cref="characterInfo"/>）。
        /// 用于判断本地缓存与已绑定列表中的数据是否存在差异，从而决定是否显示"应用"按钮。
        /// </summary>
        public bool Equals(CabinBudBoxData obj)
        {
            if (obj == null)
                return false;
            //if (deviceId != obj.deviceId)
            //    return false;
            //if (emBoxState != obj.emBoxState)
            //    return false;
            //if (nVolume != obj.nVolume)
            //    return false;
            //if (nBrightness != obj.nBrightness)
            //    return false;

            string selfJson = JsonConvert.SerializeObject(this.characterInfo);
            string otherJson = JsonConvert.SerializeObject(obj.characterInfo);
            if (!selfJson.Equals(otherJson))
                return false;

            return true;
        }
    }



    public class BudBoxDeviceList
    {
        public List<BudBoxDeviceData> deviceList = new();
    }

    public class BudBoxDeviceData
    {
        public string deviceId;
        public string deviceName;
    }

    #endregion

    #region 枚举

    /// <summary>
    /// 消息操作类型
    /// </summary>
    public enum MqttMsgOperType
    {
        // 导入角色
        import_character,
        // 设置角色
        set_character,
        // 删除角色
        delete_character,
        // 设置音量
        set_volume,
        // 设置屏幕亮度
        set_brightness,
        // 上报 App 的 HTTP uid/token，MQTT 连接成功后发送
        set_httpUidToken,
        // 获取设备离线状态
        get_hw_isOnline,
        // 发送唤醒状态-0待机-1唤醒
        set_active,
        // 发送通话状态--0挂断-1通话
        set_call,
        // 设置休眠时间--具体休眠时间分钟
        set_dormant,
        // 设置静音--0关闭静音，1开启静音
        set_souceOff,
        // 设置语音唤醒--0关闭语音唤醒，1开启语音唤醒
        set_voiceActive,
        // 设置语音休眠--0关闭语音休眠，1开启语音休眠
        set_voiceDormant,
        //获取硬件网络信息(当前使用的wifi，已添加的wifi)
        get_wifi,
        // 获取当前连接的 WiFi 信息（硬件调用 getConnectedWifiInfo，回包 curWifiData 字段）
        get_wifidata,
        // 软件侧要求硬件侧开启蓝牙和 WiFi，硬件就绪后回包确认
        sw2hw_askBleOpen,
        // 设置场景
        set_scene,
        // 设置灯光颜色
        set_light,
        // 请求硬件上报当前基础状态数据
        sync_baseMsg,
        // 触发底包 OTA 升级
        start_upgrade,
        // 触发恢复出厂设置
        start_rest,
        // 触发硬件播放指定口令
        play_voiceCmd,
        // 亮屏
        openBacklight,
        // 熄屏
        closeBacklight,
        // 发送屏幕状态--0熄屏-1亮屏
        set_screen,
        // 设置屏幕保护开关 0关闭 1开启
        set_protectScreenState,
        // 设置屏幕保护方式 0定时熄屏 1自动熄屏
        set_protectScreenSwith,
        // 设置定时熄屏时间 格式：开始分钟数_结束分钟数
        set_protectScreenTime,
        // 设置自动熄屏时间（分钟）
        set_protectScreenAuto,
    }

    public enum BoxState
    {
        /// <summary>
        /// 离线
        /// </summary>
        Offline,
        /// <summary>
        /// 连接中
        /// </summary>
        Connecting,
        /// <summary>
        /// 在线
        /// </summary>
        Online
    }

    /// <summary>
    /// 恢复出厂设置状态
    /// </summary>
    public enum RestBoxState
    {
        None,
        /// <summary>
        /// 开始恢复出厂设置
        /// </summary>
        StartRest,
        /// <summary>
        /// 恢复出厂设置成功
        /// </summary>
        RestFinish,
        /// <summary>
        /// 恢复出厂设置失败
        /// </summary>
        RestFail,
    }

    public enum UpgradeBoxType : int
    {
        APK = 1,      // 底包
        Firmware = 2, // 固件版本
        HotUpdate = 3,// 热更新
    }

    public enum UpgradeBoxState : int
    {
        //无
        None,
        //开始升级
        StartUpgrade,
        //升级成功
        UpgradeSuccess,
        //升级失败
        UpgradeFail,
    }

    #endregion

    /// <summary>GET /configuration/hotUpdate/cabin 接口响应数据</summary>
    public class CabinBoxHotUpdateData
    {
        public string appVersion;
        public string appDownloadUrl;
        public string appInfo;

        public string hotUpdateVersion;
        public string downloadURL;
        public string comment;

        public string systemVersion;
        public string systemDownloadUrl;
        public string zipInfo;
        public string productDetail;
    }
}

