using System;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class TreasureHuntLevelItem : MonoBehaviour
{
    [SerializeField] private CButton GetBtn;
    [SerializeField] private Image Icon;
    [SerializeField] private GameObject GetOver;
    [SerializeField] private GameObject Lock;
    [SerializeField] private GameObject NextPath;
    [SerializeField] private CButton JumpBtn;

    public void Setup(string iconId, int eventStatus, Action onGetClick, Action onJumpClick = null)
    {
        bool isLock = eventStatus == (int)ClaimStatus.Lock;
        bool isClaimed = eventStatus == (int)ClaimStatus.Claimed;
        bool isUnlocked = eventStatus == (int)ClaimStatus.Unlocked;

        if (Lock != null) Lock.SetActive(isLock);
        if (GetOver != null) GetOver.SetActive(isClaimed);
        if (NextPath != null) NextPath.SetActive(isUnlocked);
        if (GetBtn != null)
        {
            GetBtn.onClick.RemoveAllListeners();
            GetBtn.onClick.AddListener(() => onGetClick());
        }
        if (JumpBtn != null)
        {
            JumpBtn.onClick.RemoveAllListeners();
            if (onJumpClick != null)
                JumpBtn.onClick.AddListener(() => onJumpClick());
        }

        LoadIcon(iconId);
    }

    public void RefreshStatus(int eventStatus)
    {
        bool isLock = eventStatus == (int)ClaimStatus.Lock;
        bool isClaimed = eventStatus == (int)ClaimStatus.Claimed;
        bool isUnlocked = eventStatus == (int)ClaimStatus.Unlocked;
        if (Lock != null) Lock.SetActive(isLock);
        if (GetOver != null) GetOver.SetActive(isClaimed);
        if (NextPath != null) NextPath.SetActive(isUnlocked);
    }

    private void LoadIcon(string iconId)
    {
        if (string.IsNullOrEmpty(iconId) || Icon == null) return;

        if (iconId.StartsWith("1801"))
        {
            Icon.preserveAspect = true;
            UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(iconId, gameObject,
                sp => { if (sp != null) Icon.sprite = sp; });
        }
        else if (iconId.StartsWith("1900"))
        {
            Icon.preserveAspect = true;
            var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(iconId);
            if (titleData != null)
            {
                var sp = Loader.Load<Sprite>(titleData.Icon, gameObject);
                if (sp != null) Icon.sprite = sp;
            }
        }
        else if (long.TryParse(iconId, out _))
        {
            Icon.preserveAspect = false;
            PgcUtils.GetIconSpriteByPgcIdAsync(iconId, gameObject, sp => { if (sp != null) Icon.sprite = sp; });
        }
        else if (Enum.TryParse<CurrencyType>(iconId, out var ct))
        {
            Icon.preserveAspect = false;
            var sp = PgcUtils.LoadCurrencyIcon(ct, gameObject);
            if (sp != null) Icon.sprite = sp;
        }
    }
}
