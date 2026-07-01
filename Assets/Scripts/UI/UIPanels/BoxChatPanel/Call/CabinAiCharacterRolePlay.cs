using Game.BudBox;
using Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 角色扮演台本配置面板（MonoBehaviour，由外部通过 SetData 初始化，SetActive 控制显隐）。
/// </summary>
public class CabinAiCharacterRolePlay : MonoBehaviour
{
    [SerializeField] private Button backBtn;

    [SerializeField] private Button randomProduceBtn;
    [SerializeField] private GameObject produce;    // "随机生成" 正常状态
    [SerializeField] private GameObject producing;  // "生成中" 加载状态

    [SerializeField] private Button beginRolePlayBtn;
    [SerializeField] private GameObject btnNormal;  // 按钮可用状态
    [SerializeField] private GameObject btnGray;    // 按钮置灰状态

    [SerializeField] private InputField inputUserRole;    // 你的角色
    [SerializeField] private InputField inputPartnerRole; // 伙伴角色
    [SerializeField] private InputField inputScene;       // 场景

    private const string KeyUserRole    = "RolePlay_UserRole";
    private const string KeyPartnerRole = "RolePlay_PartnerRole";
    private const string KeyScene       = "RolePlay_Scene";

    private CabinPublishData _character;
    private CabinBudBoxData  _box;
    private bool _isGenerating;

    public Action<bool> backAc;

    bool isRolePlay = false;

    void Awake()
    {
        if (backBtn         != null) backBtn.onClick.AddListener(OnBack);
        if (randomProduceBtn != null) randomProduceBtn.onClick.AddListener(OnRandomGenerate);
        if (beginRolePlayBtn != null) beginRolePlayBtn.onClick.AddListener(OnBeginRolePlay);

        if (inputUserRole    != null) inputUserRole.onValueChanged.AddListener(_ => ValidateInputs());
        if (inputPartnerRole != null) inputPartnerRole.onValueChanged.AddListener(_ => ValidateInputs());
        if (inputScene       != null) inputScene.onValueChanged.AddListener(_ => ValidateInputs());
    }

    /// <summary>打开面板：传入角色和设备信息，恢复上次填写内容</summary>
    public void SetData(CabinPublishData character, CabinBudBoxData box)
    {
        _character    = character;
        _box          = box;
        _isGenerating = false;

        SetGeneratingState(false);

        if (inputUserRole    != null) inputUserRole.text    = PlayerPrefs.GetString(KeyUserRole, "");
        if (inputPartnerRole != null) inputPartnerRole.text = PlayerPrefs.GetString(KeyPartnerRole, "");
        if (inputScene       != null) inputScene.text       = PlayerPrefs.GetString(KeyScene, "");

        ValidateInputs();
    }

    void OnDisable()
    {
        _isGenerating = false;
        MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallStartConfirmed);
    }

    // ── 输入验证 ──────────────────────────────────────────────────────────────

    private void ValidateInputs()
    {
        bool valid = inputUserRole    != null && !string.IsNullOrEmpty(inputUserRole.text)
                  && inputPartnerRole != null && !string.IsNullOrEmpty(inputPartnerRole.text)
                  && inputScene       != null && !string.IsNullOrEmpty(inputScene.text);

        if (beginRolePlayBtn != null) beginRolePlayBtn.interactable = valid;
        if (btnNormal != null) btnNormal.SetActive(valid);
        if (btnGray   != null) btnGray.SetActive(!valid);
    }

    // ── 按钮事件 ──────────────────────────────────────────────────────────────

    private void OnBack()
    {
        gameObject.SetActive(false);
        backAc?.Invoke(isRolePlay);
    }

    private void OnRandomGenerate()
    {
        if (_isGenerating) return;
        _isGenerating = true;
        SetGeneratingState(true);

        string deviceId = _box != null ? _box.deviceId : "";
        CabinChatManager.Inst.GetAiCharacterSceneRandom(deviceId,
            resp =>
            {
                _isGenerating = false;
                SetGeneratingState(false);
                if (resp != null)
                {
                    if (inputUserRole    != null) inputUserRole.text    = resp.userRole    ?? "";
                    if (inputPartnerRole != null) inputPartnerRole.text = resp.partnerRole ?? "";
                    if (inputScene       != null) inputScene.text       = resp.scene       ?? "";
                    ValidateInputs();
                }
            },
            err =>
            {
                _isGenerating = false;
                SetGeneratingState(false);
                LoggerUtils.LogError("[CabinAiCharacterRolePlay] 随机生成失败: " + err);
            });
    }

    private void OnBeginRolePlay()
    {
        isRolePlay = true;
        PlayerPrefs.SetString(KeyUserRole,    inputUserRole    != null ? inputUserRole.text    : "");
        PlayerPrefs.SetString(KeyPartnerRole, inputPartnerRole != null ? inputPartnerRole.text : "");
        PlayerPrefs.SetString(KeyScene,       inputScene       != null ? inputScene.text       : "");
        PlayerPrefs.Save();

        if (beginRolePlayBtn != null) beginRolePlayBtn.interactable = false;

        var roleplayData = new AiCharacterSceneRandomResponse
        {
            scene       = inputScene       != null ? inputScene.text       : "",
            userRole    = inputUserRole    != null ? inputUserRole.text    : "",
            partnerRole = inputPartnerRole != null ? inputPartnerRole.text : "",
        };
        string roleplayJson = JsonConvert.SerializeObject(roleplayData);
        JObject jo = new();
        jo.Add("roleplay", roleplayJson);

        string deviceId = _box != null ? _box.deviceId : "";
        CabinBoxManager.Inst.SetCall(1, -1, deviceId);
        CabinBoxManager.Inst.SendMqttMessage(MqttMsgOperType.set_call, deviceId: deviceId, customDataStr: jo.ToString());
        MessageHelper.AddListener<string>(MessageName.OnBudBoxCallChange, OnCallStartConfirmed);
    }

    private void OnCallStartConfirmed(string deviceId)
    {
        if (_box != null && deviceId != _box.deviceId) return;

        MessageHelper.RemoveListener<string>(MessageName.OnBudBoxCallChange, OnCallStartConfirmed);

        if (beginRolePlayBtn != null) beginRolePlayBtn.interactable = true;

        CabinPublishData character = _character;
        CabinBudBoxData  box       = _box;
        UIManager.Inst.OpenPanel(PanelId.PorVideoCallNodePanel, character, box, true);
        OnBack();
    }

    private void SetGeneratingState(bool isGenerating)
    {
        if (produce          != null) produce.SetActive(!isGenerating);
        if (producing        != null) producing.SetActive(isGenerating);
        // if (randomProduceBtn != null) randomProduceBtn.interactable = !isGenerating;
    }
}
