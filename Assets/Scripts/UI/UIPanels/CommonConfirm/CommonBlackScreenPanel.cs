using AIGame.Base;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// 黑屏过渡数据类
/// </summary>
public class CommonBlackScreenData
{
    /// <summary>
    /// 要显示的文本内容
    /// </summary>
    public string disPlayText;
    
    /// <summary>
    /// 目标面板ID
    /// </summary>
    public PanelId targetPanelID;
    
    /// <summary>
    /// 医院游戏结束类型
    /// </summary>
    public AIHospitalEnd endType;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="_disPlayText">显示文本</param>
    /// <param name="_targetPanelID">目标面板ID</param>
    /// <param name="_endType">结束类型</param>
    public CommonBlackScreenData(string _disPlayText, PanelId _targetPanelID, AIHospitalEnd _endType)
    {
        disPlayText = _disPlayText;
        targetPanelID = _targetPanelID;
        endType = _endType;
    }
}

/// <summary>
/// 黑屏过渡面板：用于显示过渡文本并跳转到指定面板
/// </summary>
public class CommonBlackScreenPanel : BasePanel<CommonBlackScreenPanel>
{
    [SerializeField] private Button _bgBtn;        // 背景按钮
    [SerializeField] private Button _skipBtn;      // 跳过按钮
    [SerializeField] private Text txt_title1;      // 显示文本组件

    private Tweener titleTween1;                   // 文本动画控制器1
    private Tweener titleTween2;                   // 文本动画控制器2
    private CommonBlackScreenData _screenData;     // 面板数据

    private string _successTips = "成功逃离了废弃医院后，眼前一道白光闪过，耳边传来不知何人的声音：“恭喜你进入下一层梦境”...";
    private string _fialedTips = "在第三次被打镇静剂睡着后，你进入了迷失域，再也没有醒来，永远留在了这废弃医院当中...";
    private string _timeUpTips = "由于没有在限时中离开，你进入了迷失域，再也没有醒来，永远留在了这废弃医院中...";
    /// <summary>
    /// 初始化面板
    /// </summary>
    public override void OnCreate()
    {
        base.OnCreate();
        InitButtons();
    }

    /// <summary>
    /// 初始化按钮监听
    /// </summary>
    private void InitButtons()
    {
        if (_bgBtn != null)
            _bgBtn.onClick.AddListener(OnBgClick);
        if (_skipBtn != null)
            _skipBtn.onClick.AddListener(OnSkipClick);
    }

    /// <summary>
    /// 显示面板时的处理
    /// </summary>
    /// <param name="args">参数数组，第一个参数需要是CommonBlackScreenData类型</param>
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            var type = (int)args[0];

            if (type == 0)
            {
                _screenData = new CommonBlackScreenData(_fialedTips, PanelId.AIHospitalEndPanel, AIHospitalEnd.Failed);
            }
            else if(type == 1)
            {
                _screenData = new CommonBlackScreenData(_successTips, PanelId.AIHospitalEndPanel, AIHospitalEnd.Completed);
            }
            else if (type == 2)
            {
                _screenData = new CommonBlackScreenData(_timeUpTips, PanelId.AIHospitalEndPanel, AIHospitalEnd.Failed);
            }
            
            if (txt_title1 != null)
            {
                StartTypewriterEffect();
            }
        }
    }

    /// <summary>
    /// 启动打字机效果
    /// </summary>
    private void StartTypewriterEffect()
    {
        if (string.IsNullOrEmpty(_screenData.disPlayText))
            return;

        AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Typing_Loop);
        txt_title1.text = "";
        // 创建打字机效果，完成后显示跳过按钮
        titleTween1 = txt_title1.DOText(_screenData.disPlayText, _screenData.disPlayText.Length * 0.1f)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
                _skipBtn.gameObject.SetActive(true); 
            });
    }

    /// <summary>
    /// 点击背景处理
    /// </summary>
    private void OnBgClick()
    {
        CompleteTweens();
    }

    /// <summary>
    /// 点击跳过按钮处理
    /// </summary>
    private void OnSkipClick()
    {
        CompleteTweens();
        GoToTargetPanel();
    }

    /// <summary>
    /// 完成所有文本动画
    /// </summary>
    private void CompleteTweens()
    {
        if (titleTween1 != null && titleTween1.IsPlaying())
        {
            titleTween1.Complete();
        }
        if (titleTween2 != null && titleTween2.IsPlaying())
        {
            titleTween2.Complete();
        }
        txt_title1.text = _screenData.disPlayText;
        AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);
    }

    /// <summary>
    /// 跳转到目标面板
    /// </summary>
    private void GoToTargetPanel()
    {
        CloseSelf();
        UIManager.Inst.OpenPanel(_screenData.targetPanelID, _screenData.endType);
    }

    /// <summary>
    /// 销毁时清理资源
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();

        // 清理按钮监听
        if (_bgBtn != null)
            _bgBtn.onClick.RemoveAllListeners();
        if (_skipBtn != null)
            _skipBtn.onClick.RemoveAllListeners();

        // 清理动画
        if (titleTween1 != null)
        {
            titleTween1.Kill();
            titleTween1 = null;
        }
        if (titleTween2 != null)
        {
            titleTween2.Kill();
            titleTween2 = null;
        }
    }
}
