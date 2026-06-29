using AIGame.Base;
using DG.Tweening;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 黑屏过渡面板：用于显示过渡文本并跳转到指定面板
/// </summary>
public class TipsBlackPanel : BasePanel<TipsBlackPanel>
{
    [SerializeField] 
    private Text txt_title1;      // 显示文本组件
    private Tweener titleTween1;                   // 文本动画控制器1
    private string disPlayText;
    
    /// <summary>
    /// 显示面板时的处理
    /// </summary>
    /// <param name="args">参数数组，第一个参数需要是CommonBlackScreenData类型</param>
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        
        
        if (args != null && args.Length > 0 && args[0] is string tips)
        {
            disPlayText = tips;
            StartTypewriterEffect();
        }
    }

    /// <summary>
    /// 启动打字机效果
    /// </summary>
    private void StartTypewriterEffect()
    {
        if (string.IsNullOrEmpty(disPlayText))
            return;

        AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_Typing_Loop);
        txt_title1.text = "";
        // 创建打字机效果，完成后显示跳过按钮
        titleTween1 = txt_title1.DOText(disPlayText, disPlayText.Length * 0.1f)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                CloseSelf();
            });
    }
    

    /// <summary>
    /// 销毁时清理资源
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        AIGameSoundUtils.Inst.StopSound(YandereConfig.YE_Typing_Loop);

        // 清理动画
        if (titleTween1 != null)
        {
            titleTween1.Kill();
            titleTween1 = null;
        }
    }
}
