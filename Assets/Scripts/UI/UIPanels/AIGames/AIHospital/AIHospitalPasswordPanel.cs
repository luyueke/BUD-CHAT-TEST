using AIGame.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIHospitalPasswordPanel : BasePanel<AIHospitalPasswordPanel>
{
    [SerializeField] private Button _bgBtn;
    [SerializeField] private Button _inputBtn;
    [SerializeField] private CButton _closeBtn;
    [SerializeField] private CButton _closeBtn2;
    [SerializeField] private CButton _okBtn;
    [SerializeField] private Text _pwdText;

    private AIHospitalGame _aiGame;
    private Action _callback;

    public override void OnCreate()
    {
        base.OnCreate();
        AddListener();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _aiGame = AIGameController.Inst.GetCurAIGame<AIHospitalGame>();
        _pwdText.text = _aiGame.Pwd == AIGameHospitalConfig.defaultPwd ? "" : _aiGame.Pwd.ToString();
        if (args != null && args[0] is Action action)
        {
            _callback = action;
        }
    }

    // Start is called before the first frame update
    private void AddListener()
    {
        // 添加按钮监听
        _bgBtn.onClick.AddListener(OnBgClick);
        _closeBtn.onClick.AddListener(OnCloseClick);
        _closeBtn2.onClick.AddListener(OnCloseClick);
        _okBtn.onClick.AddListener(OnOkClick);
        _inputBtn.onClick.AddListener(OnClickText);
        // 添加输入框监听
        //_passwordInput.onValueChanged.AddListener(OnPasswordChanged);
    }

    private void OnBgClick()
    {
        CloseSelf();
    }

    private void OnCloseClick()
    {
        CloseSelf();
    }

    private void OnOkClick()
    {
        ValidatePassword();
    }

    private void OnPasswordChanged(string value)
    {
        // 隐藏错误提示
        //_errorText.gameObject.SetActive(false);
        
        // 更新确认按钮状态
        _okBtn.interactable = !string.IsNullOrEmpty(value);
    }

    private void ValidatePassword()
    {
        
        if (string.IsNullOrEmpty(_pwdText.text))
        {
            TipPanel.ShowToast("请输入密码");
            return;
        }
        string msg = _pwdText.text;
        if (!msg.All(char.IsDigit))
        {
            TipPanel.ShowToast("请输入数字");
            return;
        }

        // 验证长度
        if (msg.Length != 4)
        {
            TipPanel.ShowToast("请输入4位密码");
            return;
        }

        // 尝试转换为数字
        if (int.TryParse(msg, out int password))
        {
            if (password == AIGameController.Inst.GetCurAIGame<AIHospitalGame>().Pwd)
            {
                OnPasswordCorrect();
            }
            else
            {
                OnPasswordWrong();
            }
        }
        else
        {
            TipPanel.ShowToast("无效的密码格式");
        }
    }

    private void OnPasswordCorrect()
    {
        // 密码正确的处理
        LoggerUtils.Log("密码正确");
        CloseSelf();
        _callback();
        //AIGameController.Inst.GetCurAIGame<AIHospitalGame>().OnSuccess();
        // 发送密码正确的消息
        //MessageHelper.SendMessage(MessageName.OnPasswordCorrect);

        // 播放成功音效
        //AudioManager.Inst.PlaySound("password_correct");

        // 关闭面板
        //HidePanel();

        // 可能需要触发其他游戏逻辑
        //AIHospital_CharacterManager.Inst.OnPasswordCorrect();
    }

    private void OnPasswordWrong()
    {
        LoggerUtils.LogError("密码错误");

        //_wrongAttempts++;

        //if (_wrongAttempts >= MAX_ATTEMPTS)
        //{
        //    // 超过最大尝试次数
        //    OnMaxAttemptsReached();
        //}
        //else
        //{
        //    // 显示错误信息
        //    ShowError($"密码错误，还有{MAX_ATTEMPTS - _wrongAttempts}次机会");

        //    // 清空输入
        //    _passwordInput.text = string.Empty;

        //    // 播放错误音效
        //    AudioManager.Inst.PlaySound("password_wrong");
        //}
    }

    private void OnClickText()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = LocalizationManager.Inst.GetLocalizedText("输入消息..."),
            inputMode = 2,
            maxLength = 4,
            inputFlag = 0,
            textSecurity = 0,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = _pwdText.text,
            returnKeyType = (int)ReturnType.Send,
            source = (int)KeyboardSource.RoomChat
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnKeyboard);
        MobileInterface.Instance.ShowKeyboard(JsonUtility.ToJson(keyBoardInfo));
    }

    private void OnKeyboard(string msg)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }

        // 更新密码文本
        _pwdText.text = msg;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // 移除监听
        if (_bgBtn != null) _bgBtn.onClick.RemoveAllListeners();
        if (_closeBtn != null) _closeBtn.onClick.RemoveAllListeners();
        if (_okBtn != null) _okBtn.onClick.RemoveAllListeners();
        if (_closeBtn2 != null) _closeBtn2.onClick.RemoveAllListeners();
        //if (_passwordInput != null) _passwordInput.onValueChanged.RemoveAllListeners();
    }
}
