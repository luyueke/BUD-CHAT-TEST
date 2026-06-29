using System;
using System.Collections;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHuntPassPanel : BasePanel<TreasureHuntPassPanel>
{
    [SerializeField] private Text PassInfoText;
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private Text RewardText;
    [SerializeField] private Image RewardIcon;
    [SerializeField] private Text DownTimeText;

    private Coroutine _countdownCoroutine;

    public override void OnCreate()
    {
        base.OnCreate();
        if (CloseBtn != null) CloseBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        string iconId = args.Length > 0 ? args[0] as string : null;
        string passText = args.Length > 1 ? args[1] as string : null;
        int rewardNum = args.Length > 2 && args[2] is int num ? num : 1;

        if (PassInfoText != null) PassInfoText.text = passText;
        if (RewardText != null)
            RewardText.text = rewardNum > 1 ? $"获得奖励:{rewardNum}" : "获得奖励:";
        LoadIcon(iconId);

        if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
        _countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public override void OnHidden()
    {
        base.OnHidden();
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    private IEnumerator CountdownCoroutine()
    {
        for (int i = 3; i >= 1; i--)
        {
            if (DownTimeText != null) DownTimeText.text = i + "秒后进入下一关";
            yield return new WaitForSeconds(1f);
        }
        _countdownCoroutine = null;
        CloseSelf();
    }

    private void LoadIcon(string iconId)
    {
        if (string.IsNullOrEmpty(iconId) || RewardIcon == null) return;
        if (iconId.StartsWith("1801"))
        {
            RewardIcon.preserveAspect = true;
            UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(iconId, gameObject,
                sp => { if (sp != null && RewardIcon != null) RewardIcon.sprite = sp; });
        }
        else if (iconId.StartsWith("1900"))
        {
            RewardIcon.preserveAspect = true;
            var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(iconId);
            if (titleData != null)
            {
                var sp = Loader.Load<Sprite>(titleData.Icon, gameObject);
                if (sp != null) RewardIcon.sprite = sp;
            }
        }
        else if (long.TryParse(iconId, out _))
        {
            RewardIcon.preserveAspect = false;
            PgcUtils.GetIconSpriteByPgcIdAsync(iconId, gameObject, sp => { if (sp != null && RewardIcon != null) RewardIcon.sprite = sp; });
        }
        else if (Enum.TryParse<CurrencyType>(iconId, out var ct))
        {
            RewardIcon.preserveAspect = false;
            var sp = PgcUtils.LoadCurrencyIcon(ct, gameObject);
            if (sp != null) RewardIcon.sprite = sp;
        }
    }
}
