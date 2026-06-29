using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI能量使用规则弹窗（月卡规则 / 充值规则）
/// </summary>
public class AICreditRulesPanel : BasePanel<AICreditRulesPanel>
{
    public enum Tab { Chongzhi, YueKa }

    private GameObject _chongzhiGo;
    private GameObject _yueKaGo;

    private Button _closeBtn;
    private Button _sureBtn;

    public override void OnCreate()
    {
        base.OnCreate();

        var ui = transform.Find("BaseLayout2D/UIContainer");

        var chongzhiTf = ui != null ? ui.Find("ChongzhiGo") : null;
        _chongzhiGo = chongzhiTf != null ? chongzhiTf.gameObject : null;
        var yueKaTf = ui != null ? ui.Find("YueKaGo") : null;
        _yueKaGo    = yueKaTf != null ? yueKaTf.gameObject : null;

        var closeTf = ui != null ? ui.Find("CloseBtn") : null;
        if (closeTf != null && closeTf.TryGetComponent(out _closeBtn))
            _closeBtn.onClick.AddListener(CloseSelf);

        var sureTf = ui != null ? ui.Find("SureBtn") : null;
        if (sureTf != null && sureTf.TryGetComponent(out _sureBtn))
            _sureBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        var tab = args.Length > 0 && args[0] is Tab t ? t : Tab.YueKa;
        if (_chongzhiGo != null) _chongzhiGo.SetActive(tab == Tab.Chongzhi);
        if (_yueKaGo    != null) _yueKaGo.SetActive(tab == Tab.YueKa);
    }
}
