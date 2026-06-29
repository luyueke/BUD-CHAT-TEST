using System;
using System.Collections;
using System.Collections.Generic;
using Game.BLE;
using Game.BudBox;
using Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 设置养成舱的wifi连接
/// </summary>
public class IncubationCabinWifiSetting : BasePanel<IncubationCabinWifiSetting>
{
    public Button backBtn;
    public Button refreshWifiBtn;
    public GameObject SettleRoot;
    public GameObject BGRoot;
    // public InputField SettleRoot_InputName;

    public Text SettleRoot_InputNameText;
    public Button SettleRoot_InputNameTextBtn;
    public GameObject SettleRoot_SearchWifi; //wifi搜索中
    public ScrollRect SettleRoot_WifiListSRGo; //wifi列表
    public GameObject SettleRoot_WifiListItem; //wifi项

    public Button SettleRoo_NextBtn;


    public GameObject InputPasswordRoot; //手动输入wifi
    public Text InputPasswordRoot_WifiName;
    public Text InputPasswordRoot_inputPasswordText;
    public Button InputPasswordRoot_inputPasswordTextBtn;
    public Toggle InputPasswordRoot_toggle;
    public Button InputPasswordRoot_NextBtn;
    public Text InputPasswordRoot_Placeholder; // 密码输入框占位提示文字

    public GameObject WifiConnectingGo; //wifi连接中

    public GameObject SuccGo;
    public Text SuccBoxName;

    public Button SettleSucc_NextBtn;

    bool canRefreshWifi = false;
    private string _selectedSsid;
    private string _selectedSecurityType = "WPA2";
    private bool _selectedFromList;
    private readonly List<GameObject> _wifiItems = new List<GameObject>();
    private bool _isConnecting;
    private bool _fromCabinControll;
    private string _currentPassword = "";

    private const string Tag = "[WifiSetting]";

    string _deviceId = "";

    public override void OnShow(params object[] args)
    {
        _fromCabinControll = args != null && args.Length > 0 && args[0] is string s && s == "CabinControll";
        Debug.Log($"{Tag} OnShow — 面板打开，fromCabinControll={_fromCabinControll}");
        InitUI();
        if (_fromCabinControll)
            StartCoroutine(ConnectBleAndScanWifi());
        else
            StartCoroutine(PrepareAndScanWifi());
    }

    /// <summary>
    /// From=CabinControll 流程：① MQTT 通知硬件开启蓝牙/WiFi → ② BLE 连接 → ③ WiFi 扫描。
    /// </summary>
    private IEnumerator ConnectBleAndScanWifi()
    {
        Debug.Log($"{Tag} ConnectBleAndScanWifi — 开始");
        PlayScanWifi();

#if UNITY_EDITOR
        Debug.Log($"{Tag} ConnectBleAndScanWifi — Editor 模式，直接进入 WiFi 扫描");
        StartCoroutine(PrepareAndScanWifi());
        yield break;
#endif
        // ① 通过 MQTT 要求硬件开启蓝牙和 WiFi
        Debug.Log($"{Tag} ① 发送 sw2hw_askBleOpen，等待硬件就绪...");

        string deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
        _deviceId = deviceId;
        string qrCode = (!string.IsNullOrEmpty(deviceId))
            ? CabinBoxManager.Inst.GetDeviceQrCode(deviceId)
            : null;

        // ② BLE 连接
        bool connectDone = false;
        bool connectOk = false;
        string connectMsg = "";

        if (string.IsNullOrEmpty(qrCode))
        {
            LoggerUtils.LogError($"{Tag} ② 未获取到设备二维码数据，无法进行 BLE 连接");
            TipPanel.ShowToast("获取设备信息失败，请确保设备曾经上线");
            yield break;
        }

        var qrCodeData = QRPayloadEncoder.Decode(qrCode);
        try
        {
            //releaseSessionSilently
#if UNITY_ANDROID
            const string managerClass = "cn.budapp.biyoudideshijie.extend.ble.bridge.UnityBleClientManager";
            using var jc = new AndroidJavaClass(managerClass);
            jc.Call("releaseSessionSilently");
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
        //


        BleSoftwareSideController.ScanAndConnectDevice(JsonConvert.SerializeObject(qrCodeData), (ok, msg) =>
        {
            connectOk = ok;
            connectMsg = msg;
            connectDone = true;
        }, isReconnect: true);
        while (!connectDone) yield return null;

        Debug.Log($"{Tag} ② BLE 回调 ok={connectOk} msg={connectMsg}");

        if (connectMsg == "连接断开")
        {
            LoggerUtils.LogError($"{Tag} ② BLE 连接失败: {connectMsg}");
            TipPanel.ShowToast("蓝牙连接失败，请重试");
            yield break;
        }

        // ③ BLE 连接成功，进入 WiFi 扫描流程
        Debug.Log($"{Tag} ③ BLE 连接成功，开始 WiFi 扫描流程");
        StartCoroutine(PrepareAndScanWifi());
    }

    /// <summary>
    /// WiFi 扫描前置流程：蓝牙开启 → 位置权限 → WiFi 可用，全部就绪后再发起列表请求。
    /// CheckWifiUsable / RequestEnableWifi 带 5 秒超时，Android 客户端侧若未实现则自动跳过。
    /// </summary>
    private IEnumerator PrepareAndScanWifi()
    {
        var json = PlayerPrefs.GetString(IncubationCabinLinkBox.QrJsonPrefsKey, "");
        Debug.Log($"{Tag} BindCabinBoxFromQrJson — PlayerPrefs 数据: {(string.IsNullOrEmpty(json) ? "(空)" : json.Substring(0, Mathf.Min(80, json.Length)))}");

        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var qrData = JsonConvert.DeserializeObject<QrCodeData>(json);
                if (qrData != null && !string.IsNullOrEmpty(qrData.deviceId))
                {
                    _deviceId = qrData.deviceId;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }



        PlayScanWifi();
        Debug.Log($"{Tag} PrepareAndScanWifi — 开始前置检查");


#if UNITY_EDITOR
        Debug.Log($"{Tag} PrepareAndScanWifi — Editor 模式，跳过前置检查");
        StartScanWifi();
        yield break;
#endif
        // ① 蓝牙开启检查
        Debug.Log($"{Tag} ① 检查蓝牙状态...");
        bool btEnabled = false, btCheckDone = false;
        BleSoftwareSideController.IsBluetoothEnabled(enabled => { btEnabled = enabled; btCheckDone = true; });
        while (!btCheckDone) yield return null;
        Debug.Log($"{Tag} ① 蓝牙状态: {(btEnabled ? "已开启" : "未开启")}");

        if (!btEnabled)
        {
            Debug.Log($"{Tag} ① 请求开启蓝牙...");
            bool btEnableDone = false;
            BleSoftwareSideController.RequestEnableBluetooth(enabled => { btEnabled = enabled; btEnableDone = true; });
            while (!btEnableDone) yield return null;
            Debug.Log($"{Tag} ① 请求开启蓝牙结果: {(btEnabled ? "成功" : "失败")}");

            if (!btEnabled)
                LoggerUtils.LogError($"{Tag} 蓝牙开启失败，WiFi 列表请求可能无法送达硬件");
        }

        // ② 位置权限（BLE 扫描 Android 侧需要）
        Debug.Log($"{Tag} ② 请求位置权限...");
        bool locDone = false;
        BleSoftwareSideController.RequestLocationPermission(result =>
        {
            Debug.Log($"{Tag} ② 位置权限结果: granted={result.granted} locationEnabled={result.locationEnabled}");
            locDone = true;
        });
        while (!locDone) yield return null;

        // ③ WiFi 可用检查（带超时：Android 客户端未实现时自动跳过，不卡流程）
        const float kTimeout = 5f;
        bool wifiCheckDone = false;
        bool wifiUsable = false;
        float wifiTimer = 0f;
        Debug.Log($"{Tag} ③ 检查 WiFi 可用性（超时 {kTimeout}s）...");
        BleSoftwareSideController.CheckWifiUsable(usable => { wifiUsable = usable; wifiCheckDone = true; });
        while (!wifiCheckDone && wifiTimer < kTimeout)
        {
            wifiTimer += Time.deltaTime;
            yield return null;
        }

        if (!wifiCheckDone)
        {
            Debug.LogWarning($"{Tag} ③ CheckWifiUsable 超时（{kTimeout}s），跳过 WiFi 检查");
        }
        else if (!wifiUsable)
        {
            Debug.Log($"{Tag} ③ WiFi 不可用，请求开启 WiFi...");
            bool wifiEnableDone = false;
            float enableTimer = 0f;
            BleSoftwareSideController.RequestEnableWifi(requested =>
            {
                Debug.Log($"{Tag} ③ RequestEnableWifi 回调: requested={requested}");
                wifiEnableDone = true;
            });
            while (!wifiEnableDone && enableTimer < kTimeout)
            {
                enableTimer += Time.deltaTime;
                yield return null;
            }
            if (!wifiEnableDone)
                Debug.LogWarning($"{Tag} ③ RequestEnableWifi 超时，继续流程");
        }
        else
        {
            Debug.Log($"{Tag} ③ WiFi 可用");
        }

        // ④ 就绪，发起 WiFi 列表扫描
        Debug.Log($"{Tag} ④ 前置检查完成，发起 WiFi 列表扫描");
        StartScanWifi();
    }

    public override void OnHidden()
    {
        Debug.Log($"{Tag} OnHidden — 面板隐藏，执行清理");
        Cleanup();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Debug.Log($"{Tag} OnDestroy — 面板销毁，执行清理");
        Cleanup();
    }

    private void Cleanup()
    {
        BleSoftwareSideController.OnDataReceived -= OnBleDataReceived;
        ClearWifiList();
    }

    private void InitUI()
    {
        SettleRoot.SetActive(true);
        InputPasswordRoot.SetActive(false);
        WifiConnectingGo.SetActive(false);
        SuccGo.SetActive(false);
        BGRoot.SetActive(true);
        SettleRoot_SearchWifi.SetActive(false);
        SettleRoot_WifiListSRGo.gameObject.SetActive(false);
        SettleRoot_WifiListItem.SetActive(false);

        _selectedSsid = null;
        _selectedFromList = false;
        _isConnecting = false;

        RefreshSettleNextBtn();
        InputPasswordRoot_NextBtn.gameObject.SetActive(false);

        // 密码显示初始化
        _currentPassword = "";
        InputPasswordRoot_toggle.isOn = false;

        // 注册监听（先移除再添加，避免重复）
        backBtn.onClick.RemoveListener(backBtnClick);
        backBtn.onClick.AddListener(backBtnClick);
        refreshWifiBtn.onClick.RemoveListener(onRefreshWifiClick);
        refreshWifiBtn.onClick.AddListener(onRefreshWifiClick);


        SettleRoot_InputNameTextBtn.onClick.RemoveListener(OnSettleRoot_InputNameTextBtnClick);
        SettleRoot_InputNameTextBtn.onClick.AddListener(OnSettleRoot_InputNameTextBtnClick);

        InputPasswordRoot_inputPasswordTextBtn.onClick.RemoveListener(OnInputPasswordRoot_inputPasswordTextBtnClick);
        InputPasswordRoot_inputPasswordTextBtn.onClick.AddListener(OnInputPasswordRoot_inputPasswordTextBtnClick);




        SettleRoo_NextBtn.onClick.RemoveListener(OnSettleNextBtnClick);
        SettleRoo_NextBtn.onClick.AddListener(OnSettleNextBtnClick);

        SettleSucc_NextBtn.onClick.RemoveListener(OnSettleSucNextBtnClick);
        SettleSucc_NextBtn.onClick.AddListener(OnSettleSucNextBtnClick);


        // InputPasswordRoot_inputPassword.onValueChanged.RemoveListener(OnPasswordChanged);
        // InputPasswordRoot_inputPassword.onValueChanged.AddListener(OnPasswordChanged);

        InputPasswordRoot_NextBtn.onClick.RemoveListener(OnPasswordNextBtnClick);
        InputPasswordRoot_NextBtn.onClick.AddListener(OnPasswordNextBtnClick);

        InputPasswordRoot_toggle.onValueChanged.RemoveListener(OnToggleChanged);
        InputPasswordRoot_toggle.onValueChanged.AddListener(OnToggleChanged);

        BleSoftwareSideController.OnDataReceived -= OnBleDataReceived;
        BleSoftwareSideController.OnDataReceived += OnBleDataReceived;
    }

    void onRefreshWifiClick()
    {
        if (!canRefreshWifi)
        {
            return;
        }
        canRefreshWifi = false;
        PlayScanWifi();
        StartScanWifi();
    }

    void backBtnClick()
    {
        if (InputPasswordRoot.activeInHierarchy)
        {
            SettleRoot.SetActive(true);
            InputPasswordRoot.SetActive(false);
            SettleRoot_InputNameText.text = "手动输入Wi-Fi名称";
            _currentPassword = "";
            RefreshPasswordDisplay();

            return;
        }

        if (!_fromCabinControll)
        {
            //通知硬件重置回到二维码界面
            BleSoftwareSideController.SendDataToDevice("{\"data\":\"back2Code\"}");
        }
        else
        {
            BleSoftwareSideController.SendDataToDevice("{\"data2\":\"StartBleAdvertising\"}"); //开启蓝牙广播
        }

        CloseSelf();
    }

    // ─── 扫描 wifi ───────────────────────────────────────────

    void PlayScanWifi()
    {
        //扫描wifi
        SettleRoot_SearchWifi.SetActive(true);
        SettleRoot_WifiListSRGo.gameObject.SetActive(false);
        ClearWifiList();
    }

    private void StartScanWifi()
    {
        Debug.Log($"{Tag} StartScanWifi — 发起 WiFi 列表请求");
#if UNITY_EDITOR

        TimerManager.Inst.RunOnce("StartScanWifi", 2, () =>
        {
            Debug.Log($"{Tag} StartScanWifi — Editor 模拟返回 WiFi 列表");
            var mockList = new WifiListResponse
            {
                resultCode = 0,
                message = "ok",
                timestamp = 0,
                wifiList = new List<WifiInfo>
            {
                new WifiInfo { ssid = "HomeWifi_5G",   securityType = "WPA2", level = -45, frequency = 5200, isConnected = true },
                new WifiInfo { ssid = "OfficeNetwork", securityType = "WPA2", level = -60, frequency = 2437 },
                new WifiInfo { ssid = "GuestWifi",     securityType = "WPA",  level = -72, frequency = 2412 },
                new WifiInfo { ssid = "OpenHotspot",   securityType = "OPEN", level = -80, frequency = 2462 },
            }
            };
            OnWifiListReceived(mockList);
        });

        return;
#endif

        BleSoftwareSideController.RequestWifiList((success, msg) =>
        {
            if (!success)
            {
                LoggerUtils.LogError($"{Tag} RequestWifiList 失败: {msg}");
                SettleRoot_SearchWifi.SetActive(false);
            }
            else
            {
                Debug.Log($"{Tag} RequestWifiList 请求已发送，等待硬件返回列表...");
            }
        });
    }

    // ─── BLE 数据回调（wifi列表 & wifi连接结果）────────────────

    private void OnBleDataReceived(string data)
    {
        Debug.Log($"{Tag} OnBleDataReceived — 收到 BLE 数据: {data.Substring(0, Mathf.Min(120, data.Length))}");

        // 优先尝试解析为 wifi 列表
        try
        {
            var listRsp = JsonConvert.DeserializeObject<WifiListResponse>(data);
            if (listRsp?.wifiList != null)
            {
                Debug.Log($"{Tag} OnBleDataReceived — 识别为 WiFi 列表，数量: {listRsp.wifiList.Count}");
                OnWifiListReceived(listRsp);
                return;
            }
        }
        catch { }

        // 再尝试解析为 wifi 连接结果（ssid 在失败时可能为空，不能用它做判断）
        if (_isConnecting)
        {
            try
            {
                var connectRsp = JsonConvert.DeserializeObject<WifiConnectResult>(data);
                if (connectRsp != null)
                {
                    Debug.Log($"{Tag} OnBleDataReceived — 识别为 WiFi 连接结果: resultCode={connectRsp.resultCode} msg={connectRsp.message}");
                    OnWifiConnectResult(connectRsp);
                }
            }
            catch { }
        }
        else
        {
            Debug.Log($"{Tag} OnBleDataReceived — 当前非连接中状态，忽略连接结果");
        }
    }

    private void OnWifiListReceived(WifiListResponse response)
    {
        canRefreshWifi = true;
        Debug.Log($"{Tag} OnWifiListReceived — resultCode={response.resultCode} count={response.wifiList?.Count ?? 0}");
        SettleRoot_SearchWifi.SetActive(false);

        if (response.wifiList == null || response.wifiList.Count == 0)
        {
            Debug.LogWarning($"{Tag} OnWifiListReceived — 列表为空");
            return;
        }

        SettleRoot_WifiListSRGo.gameObject.SetActive(true);

        foreach (var wifi in response.wifiList)
        {
            if (string.IsNullOrEmpty(wifi.ssid)) continue;
            Debug.Log($"{Tag}   WiFi: ssid={wifi.ssid} security={wifi.securityType} level={wifi.level} connected={wifi.isConnected}");
            CreateWifiItem(wifi);
        }
    }

    private void CreateWifiItem(WifiInfo wifi)
    {
        var item = Instantiate(SettleRoot_WifiListItem, SettleRoot_WifiListSRGo.content);
        item.SetActive(true);
        _wifiItems.Add(item);

        var nameText = item.GetComponentInChildren<Text>();
        if (nameText != null)
            nameText.text = wifi.ssid;

        var btn = item.GetComponent<Button>() ?? item.GetComponentInChildren<Button>();
        if (btn != null)
        {
            var capturedWifi = wifi;
            btn.onClick.AddListener(() => OnWifiItemSelected(capturedWifi));
        }
    }

    // ─── SettleRoot 交互 ──────────────────────────────────────

    private void OnWifiItemSelected(WifiInfo wifi)
    {
        Debug.Log($"{Tag} OnWifiItemSelected — 选择列表 WiFi: ssid={wifi.ssid} security={wifi.securityType}");
        _selectedSsid = wifi.ssid;
        _selectedSecurityType = string.IsNullOrEmpty(wifi.securityType) ? "WPA2" : wifi.securityType;
        _selectedFromList = true;
        // SettleRoot_InputName.text = "";
        SettleRoot_InputNameText.text = "手动输入Wi-Fi名称";
        EnterPasswordPage();
    }

    private void OnInputNameChanged(string value)
    {
        // 手动输入时清除列表选中状态
        _selectedSsid = null;
        _selectedFromList = false;
        RefreshSettleNextBtn();
    }

    private void OnInputNameEndEdit(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        Debug.Log($"{Tag} OnInputNameEndEdit — 手动输入 WiFi 名称: {value}");
        _selectedSsid = value;
        _selectedSecurityType = "WPA2";
        EnterPasswordPage();
    }
    /// <summary>
    /// 手动输入wifi名称
    /// </summary>
    void OnSettleRoot_InputNameTextBtnClick()
    {
        var wifiNamePlaceholder = LocalizationManager.Inst.GetLocalizedText("手动输入Wi-Fi名称");
        var currentInputText = SettleRoot_InputNameText.text;
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = wifiNamePlaceholder,
            inputMode = 2,
            maxLength = 20,
            inputFlag = 0,
            textSecurity = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            // 若当前文本是占位提示文字，则不预填（避免提示文字出现在输入框中）
            defaultText = currentInputText == wifiNamePlaceholder ? string.Empty : currentInputText,
            returnKeyType = (int)ReturnType.Send,
            source = (int)KeyboardSource.RoomChat
        };
        // 打开新键盘前先清除可能残留的旧回调，防止 OnKeyboard1/OnKeyboard2 并存导致逻辑混乱
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard1);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));

    }

    private void OnKeyboard1(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }
        SettleRoot_InputNameText.text = msg;

        // 手动输入时清除列表选中状态
        _selectedSsid = null;
        _selectedFromList = false;
        RefreshSettleNextBtn();
        OnInputNameEndEdit(msg);
    }
    void OnInputPasswordRoot_inputPasswordTextBtnClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入密码"),
            inputMode = 2,
            maxLength = 20,
            inputFlag = 0,
            textSecurity = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = _currentPassword,
            returnKeyType = (int)ReturnType.Send,
            source = (int)KeyboardSource.RoomChat
        };
        // 打开新键盘前先清除可能残留的旧回调，防止 OnKeyboard1/OnKeyboard2 并存导致逻辑混乱
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard2);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));


    }

    private void OnKeyboard2(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }
        _currentPassword = msg;
        RefreshPasswordDisplay();
        OnPasswordChanged(msg);
    }

    private void OnSettleNextBtnClick()
    {
        if (_selectedFromList && !string.IsNullOrEmpty(_selectedSsid))
        {
            Debug.Log($"{Tag} OnSettleNextBtnClick — 列表选中，直接连接: ssid={_selectedSsid}");
            SettleRoot.SetActive(false);
            DirectConnect();
        }
        else
        {
            string inputName = SettleRoot_InputNameText.text;
            if (string.IsNullOrEmpty(inputName)) return;
            Debug.Log($"{Tag} OnSettleNextBtnClick — 手动输入，进入密码页: ssid={inputName}");
            _selectedSsid = inputName;
            _selectedSecurityType = "WPA2";
            EnterPasswordPage();
        }
    }

    /// <summary>
    /// 连接成功后 下一步
    /// </summary>
    void OnSettleSucNextBtnClick()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
        CabinBoxManager.Inst.InitBudBoxDic((result) =>
        {
            CloseSelf();

            var boxList = CabinBoxManager.Inst.GetBudBoxList();
            if (boxList?.Count > 0)
            {
                // 多仓场景：优先跳转到本次新绑定/配网的设备（通过 deviceId 匹配）
                var targetBox = boxList.Find(box => box.deviceId == _deviceId);

                if (targetBox == null)
                {
                    LoggerUtils.LogError($"[IncubationCabinWifiSetting] 未找到 deviceId={_deviceId} 对应的 Box，兜底跳转第一个");
                    targetBox = boxList[0];
                }

                CabinBoxManager.Inst.JumpToBox(targetBox);
            }
        });
    }

    private void DirectConnect()
    {
        if (_isConnecting) return;
        _isConnecting = true;
        WifiConnectingGo.SetActive(true);

        var config = new WifiConfig
        {
            ssid = _selectedSsid,
            password = "",
            securityType = _selectedSecurityType
        };
        Debug.Log($"{Tag} DirectConnect — 发送 WiFi 配置: ssid={config.ssid} security={config.securityType}");
        curWifiConfig = config;

#if UNITY_EDITOR
        Debug.Log($"{Tag} DirectConnect — Editor 模拟连接成功");
        OnWifiConnectResult(new WifiConnectResult { resultCode = 0, message = "connected", ssid = _selectedSsid });
        return;
#endif

        SendWifi2Box(config);
        BleSoftwareSideController.SendWifiConfig(config, (success, msg) =>
        {
            if (!success)
            {
                LoggerUtils.LogError($"{Tag} DirectConnect SendWifiConfig 失败: {msg}");
                _isConnecting = false;
                WifiConnectingGo.SetActive(false);
            }
            else
            {
                Debug.Log($"{Tag} DirectConnect — WiFi 配置已发送，等待连接结果...");
            }
        });
    }

    private void EnterPasswordPage()
    {
        Debug.Log($"{Tag} EnterPasswordPage — 进入密码输入页: ssid={_selectedSsid}");
        SettleRoot.SetActive(false);
        InputPasswordRoot.SetActive(true);
        InputPasswordRoot_WifiName.text = _selectedSsid;
        _currentPassword = "";
        RefreshPasswordDisplay();
        RefreshInputPasswordNextBtn();
    }

    // ─── InputPasswordRoot 交互 ───────────────────────────────

    private void OnPasswordChanged(string value)
    {
        RefreshInputPasswordNextBtn();
    }

    private void OnToggleChanged(bool isOn)
    {
        RefreshPasswordDisplay();
    }

    /// <summary>
    /// 根据当前密码内容和 Toggle 状态刷新密码显示区域：
    /// - 无密码时显示 Placeholder 占位提示，有密码时隐藏
    /// - Toggle 关闭时显示遮罩字符（●），开启时显示明文
    /// </summary>
    private void RefreshPasswordDisplay()
    {
        bool hasPassword = _currentPassword.Length > 0;

        // 有密码时隐藏占位提示，无密码时显示
        if (InputPasswordRoot_Placeholder != null)
        {
            InputPasswordRoot_Placeholder.gameObject.SetActive(!hasPassword);
        }

        if (InputPasswordRoot_toggle.isOn)
        {
            InputPasswordRoot_inputPasswordText.text = _currentPassword;
        }
        else
        {
            InputPasswordRoot_inputPasswordText.text = new string('●', _currentPassword.Length);
        }
    }

    /// <summary>
    /// 刷新密码输入页下一步按钮的显示状态。
    /// 需同时满足：WiFi 名称已选定（_selectedSsid 非空）且密码长度 >= 8，按钮才显示。
    /// </summary>
    private void RefreshInputPasswordNextBtn()
    {
        bool hasWifiName = !string.IsNullOrEmpty(_selectedSsid);
        bool hasPassword = _currentPassword.Length >= 8;
        InputPasswordRoot_NextBtn.gameObject.SetActive(hasWifiName && hasPassword);
    }

    WifiConfig curWifiConfig;
    string _curPassword = "";
    private void OnPasswordNextBtnClick()
    {
        string password = _currentPassword;
        if (password.Length < 8 || _isConnecting) return;

        Debug.Log($"{Tag} OnPasswordNextBtnClick — 提交密码，发起连接: ssid={_selectedSsid} pwdLen={password.Length}");
        _isConnecting = true;
        InputPasswordRoot_NextBtn.interactable = false;
        WifiConnectingGo.SetActive(true);
        _curPassword = password;

        var config = new WifiConfig
        {
            ssid = _selectedSsid,
            password = password,
            securityType = _selectedSecurityType
        };
        curWifiConfig = config;

#if UNITY_EDITOR
        Debug.Log($"{Tag} OnPasswordNextBtnClick — Editor 模拟连接成功");
        OnWifiConnectResult(new WifiConnectResult { resultCode = 0, message = "connected", ssid = _selectedSsid });
        return;
#endif

        SendWifi2Box(config);
        BleSoftwareSideController.SendWifiConfig(config, (success, msg) =>
        {
            if (!success)
            {
                LoggerUtils.LogError($"{Tag} SendWifiConfig 失败: {msg}");
                _isConnecting = false;
                WifiConnectingGo.SetActive(false);
                InputPasswordRoot_NextBtn.interactable = true;
            }
            else
            {
                Debug.Log($"{Tag} SendWifiConfig — 配置已发送，等待连接结果...");
            }
        });
    }

    void SendWifi2Box(WifiConfig wifiCfg)
    {
        // var json = JsonConvert.SerializeObject(wifiCfg);
        // var j = new JObject();
        // j.Add("data2",json);
        // BleSoftwareSideController.SendDataToDevice(j.ToString(), (success, msg) =>
        // {
        //     if (!success)
        //         LoggerUtils.LogError($"{Tag} SendWifi2Box 失败: {msg}");
        //     else
        //         Debug.Log($"{Tag} SendWifi2Box — WiFi 数据已发送");
        // });
    }

    private void OnWifiConnectResult(WifiConnectResult result)
    {
        Debug.Log($"{Tag} OnWifiConnectResult — resultCode={result.resultCode} ssid={result.ssid} msg={result.message}");
        _isConnecting = false;
        WifiConnectingGo.SetActive(false);
        if (result.resultCode == 0)
        {
            Debug.Log($"{Tag} OnWifiConnectResult — WiFi 连接成功，准备绑定设备");
            CabinBoxManager.Inst.MarkWifiConnected(result.ssid, _selectedSecurityType, _curPassword, _deviceId);
            SettleRoot.SetActive(false);
            InputPasswordRoot.SetActive(false);
            if (SuccBoxName)
            {
                SuccBoxName.text = "";
            }
            SuccGo.SetActive(true);
            BGRoot.SetActive(false);
            BindCabinBoxFromQrJson();
        }
        else
        {
            TipPanel.ShowToast(result.message);
            LoggerUtils.LogError($"{Tag} OnWifiConnectResult — WiFi 连接失败: code={result.resultCode} msg={result.message}");
            InputPasswordRoot_NextBtn.interactable = true;
        }
    }

    // ─── 绑定养成舱 ───────────────────────────────────────────

    private void BindCabinBoxFromQrJson()
    {
        var json = PlayerPrefs.GetString(IncubationCabinLinkBox.QrJsonPrefsKey, "");
        Debug.Log($"{Tag} BindCabinBoxFromQrJson — PlayerPrefs 数据: {(string.IsNullOrEmpty(json) ? "(空)" : json.Substring(0, Mathf.Min(80, json.Length)))}");

        if (string.IsNullOrEmpty(json))
        {
            LoggerUtils.LogError($"{Tag} PlayerPrefs 中无扫码数据，跳过绑定");
            return;
        }

        try
        {
            var qrData = JsonConvert.DeserializeObject<QrCodeData>(json);
            if (qrData == null || string.IsNullOrEmpty(qrData.deviceId))
            {
                LoggerUtils.LogError($"{Tag} 扫码数据中缺少 deviceId，跳过绑定");
                return;
            }

            Debug.Log($"{Tag} BindCabinBoxFromQrJson — 发起绑定: deviceId={qrData.deviceId}");
            CabinBoxManager.Inst.BindCabinBox(qrData.deviceId, success =>
            {
                if (success)
                {
                    Debug.Log($"{Tag} BindCabinBox — 绑定成功，清除 PlayerPrefs");
                    //绑定成功发送数据
                    JObject jo = new();
                    jo.Add("uid", AccountDataManager.Inst.Uid);
                    jo.Add("token", AccountDataManager.Inst.Token);
                    JObject jo2 = new();
                    jo2.Add("data", jo.ToString());
                    BleSoftwareSideController.SendDataToDevice(jo2.ToString()); //发送uid/token
                    PlayerPrefs.DeleteKey(IncubationCabinLinkBox.QrJsonPrefsKey);
                    CabinBoxManager.Inst.InitBudBoxDic();
                    PlayerPrefs.SetInt("BUDBOXBindTime_" + qrData.deviceId, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                }
                else
                {
                    LoggerUtils.LogError($"{Tag} BindCabinBox — 绑定失败");
                }
            });
        }
        catch (Exception e)
        {
            LoggerUtils.LogError($"{Tag} 解析扫码 JSON 失败: {e.Message}");
        }
    }

    // ─── 工具 ─────────────────────────────────────────────────

    private void RefreshSettleNextBtn()
    {
        SettleRoo_NextBtn.interactable = _selectedFromList || !string.IsNullOrEmpty(SettleRoot_InputNameText.text);
    }

    private void ClearWifiList()
    {
        foreach (var item in _wifiItems)
        {
            if (item != null) Destroy(item);
        }
        _wifiItems.Clear();
    }

    [Button("编辑器模拟测试返回wifi列表")]
    void EditorTestWifiList()
    {
#if UNITY_EDITOR
        Debug.Log($"{Tag} [Test] 触发模拟 WiFi 列表");
        var mockResponse = new WifiListResponse
        {
            resultCode = 0,
            message = "ok",
            timestamp = 0,
            wifiList = new System.Collections.Generic.List<WifiInfo>
            {
                new WifiInfo { ssid = "HomeWifi_5G",   securityType = "WPA2", level = -45, frequency = 5200, isConnected = true },
                new WifiInfo { ssid = "OfficeNetwork", securityType = "WPA2", level = -60, frequency = 2437 },
                new WifiInfo { ssid = "GuestWifi",     securityType = "WPA",  level = -72, frequency = 2412 },
                new WifiInfo { ssid = "OpenHotspot",   securityType = "OPEN", level = -80, frequency = 2462 },
            }
        };
        OnBleDataReceived(JsonConvert.SerializeObject(mockResponse));
#endif
    }

    [Button("编辑器模拟测试发送wifi然后返回成功")]
    void EditorTestSendWifiSuc()
    {
#if UNITY_EDITOR
        Debug.Log($"{Tag} [Test] 触发模拟 WiFi 连接成功");
        var mockResult = new WifiConnectResult
        {
            resultCode = 0,
            message = "connected",
            ssid = string.IsNullOrEmpty(_selectedSsid) ? "MockWifi" : _selectedSsid,
            timestamp = 0,
        };
        _isConnecting = true;
        OnBleDataReceived(JsonConvert.SerializeObject(mockResult));
#endif
    }

    [Button("编辑器模拟测试发送wifi然后返回失败")]
    void EditorTestSendWifiFail()
    {
#if UNITY_EDITOR
        Debug.Log($"{Tag} [Test] 触发模拟 WiFi 连接失败");
        var mockResult = new WifiConnectResult
        {
            resultCode = 13, // 13 = WiFi 密码错误
            message = "WiFi密码错误",
            ssid = string.IsNullOrEmpty(_selectedSsid) ? "MockWifi" : _selectedSsid,
            timestamp = 0,
        };
        _isConnecting = true;
        OnBleDataReceived(JsonConvert.SerializeObject(mockResult));
#endif
    }


    [Button("编辑器测试绑定")]
    void EditorTestBind()
    {
        Debug.Log($"{Tag} [Test] 触发模拟绑定");
        CabinBoxManager.Inst.BindCabinBox("deviceID2222", success =>
            {
                if (success)
                    PlayerPrefs.DeleteKey(IncubationCabinLinkBox.QrJsonPrefsKey);
                else
                    LoggerUtils.LogError($"{Tag} BindCabinBox 失败");
            });
    }

    [Button("保存wifi")]
    void OnWifiConnectResultTest()
    {
        _curPassword = "11111";
        OnWifiConnectResult(new()
        {
            resultCode = 0,
            ssid = "aaaaa",
            message = "",
        });
        _curPassword = "22222";
        OnWifiConnectResult(new()
        {
            resultCode = 0,
            ssid = "bbbbb",
            message = "",
        });
    }

}
