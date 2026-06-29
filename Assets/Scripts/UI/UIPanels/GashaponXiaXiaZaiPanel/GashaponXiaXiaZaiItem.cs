using System;
using Basic.Utils;
using Game.Audio;
using Game.Store;
using GameData.PgcData;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponXiaXiaZaiItem : MonoBehaviour
{
    [SerializeField] public Image bgImage;
    [SerializeField] public Image bgImage_big;
    [SerializeField] public Image icon_bg_anim;
    [SerializeField] private GameObject selectObj;
    [SerializeField] private GameObject anim;
    [SerializeField] public Image iconBgImage;
    [SerializeField] public Image iconImage;
    [SerializeField] private CButton btn;
    [SerializeField] private Text txt_count;
    private int itemIndex;
    private Action<int> onClickCallback;
    private bool _hasReward;
    private Vector2 _originalIconSize;

    public void Init(int index, Action<int> onClick)
    {
        itemIndex = index;
        onClickCallback = onClick;
        _originalIconSize = iconImage.rectTransform.sizeDelta;
        btn.onClick.AddListener(() => {
            if (_hasReward) return;
            //AkSoundManager.Inst.PlayUIEffectSound("Play_UI_XiaxiaCard_Click");
            //Game.Audio.AkSoundManager.Inst.PostEvent("UI_XiaxiaCard_Click", gameObject);
            onClickCallback?.Invoke(itemIndex);
        });
        selectObj.SetActive(false);
        iconImage.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        selectObj.SetActive(selected);
    }

    public void ShowRewardIcon(GashaponRewardData data, Sprite bgSprite = null)
    {
        _hasReward = true;
        SetIconBg(bgSprite);
        if (txt_count != null) txt_count.gameObject.SetActive(false);

        var currencyType = GameUtils.ConvertRewardType((int)data.RewardType);
        if (currencyType != CurrencyType.None)
        {
            iconImage.rectTransform.sizeDelta = new Vector2(108, 108);
            PgcUtils.LoadCurrencyIconAsync(currencyType, gameObject, SetIcon);
            if (txt_count != null)
            {
                txt_count.text = data.Num > 0 ? "x" + data.Num : "";
                txt_count.gameObject.SetActive(data.Num > 0);
            }
            return;
        }

        if (GashaponUtils.HasPGCData(data))
        {
            switch (data.PgcDatas[0].ResourceType)
            {
                case ResourceType.Vehicle:
                    SetIcon(PgcUtils.LoadVehicleIcon(data.Id, gameObject));
                    return;
                case ResourceType.Avatar:
                    if (!string.IsNullOrEmpty(data.BundleId))
                        PgcUtils.LoadBundleIconAsync(data.BundleId, gameObject, SetIcon);
                    else
                        PgcUtils.LoadAvatarIconAsync(data.Id, gameObject, SetIcon);
                    return;
                case ResourceType.PGCPetAvatar:
                    PgcUtils.LoadPetAvatarIconAsync(data.Id, gameObject, SetIcon);
                    return;
                case ResourceType.Emote:
                    PgcUtils.LoadEmoteIconAsync(data.Id, gameObject, SetIcon);
                    return;
            }
        }

        var rewardType = (int)data.RewardType;
        if (rewardType == (int)RewardType.RewardAvatarFrame)
            UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.Id, gameObject, SetIcon);
        else if (rewardType == (int)RewardType.RewardChatBubbles)
            UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.Id, gameObject, SetIcon);
        else if (rewardType == (int)BUDRewardType.RewardUgcTemplateResource)
            PgcUtils.LoadPetUGCTemplateAsync(data.Id, gameObject, SetIcon);
        else if (rewardType == (int)BUDRewardType.RewardHomepageSkin)
            SetIcon(ProfileThemeManager.Inst.LoadThemeIcon(data.Id, gameObject));
        else if (rewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
            UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(data.Id, gameObject, SetIcon);
        else if (rewardType == (int)BUDRewardType.RewardTypeTitle)
            UserUIWidgetManager.Inst.GetTitleImgByPgcIdAsync(data.Id, gameObject, SetIcon);
        else if (rewardType == (int)BUDRewardType.RewardPgcBundle && !string.IsNullOrEmpty(data.BundleId))
            PgcUtils.LoadBundleIconAsync(data.BundleId, gameObject, SetIcon);
        else
            SetIcon(PgcUtils.LoadRewardIcon((BUDRewardType)data.RewardType, gameObject));
    }

    // 仅有 pgcId 时的简化版（如抽奖后翻牌场景）
    public void ShowRewardIcon(string pgcId, Sprite bgSprite = null)
    {
        _hasReward = true;
        SetIconBg(bgSprite);
        PgcUtils.GetIconSpriteByPgcIdAsync(pgcId, gameObject, SetIcon);
    }

    // 已有 Sprite 时直接设置（如 bundle icon 异步回调后）
    public void ShowRewardIcon(Sprite iconSprite, Sprite bgSprite = null)
    {
        _hasReward = true;
        SetIconBg(bgSprite);
        SetIcon(iconSprite);
    }

    public void SetCount(int amount)
    {
        if (txt_count == null) return;
        txt_count.text = amount > 1 ? $"x{amount}" : "";
        txt_count.gameObject.SetActive(amount > 1);
    }
    public void ShowEffect(Action onAnimFinished = null)
    {
        if (btn != null) btn.interactable = false;
        if (anim == null)
        {
            onAnimFinished?.Invoke();
            return;
        }
        anim.SetActive(true);
        if (onAnimFinished == null) return;
        TimerManager.Inst.RunOnce($"XiaXiaZaiAnimWait_{itemIndex}", 2, () =>
        {
            anim.SetActive(false);
            onAnimFinished?.Invoke();
        });
    }

    public void EnableBtn()
    {
        if (btn != null) btn.interactable = true;
    }

    public void Reset()
    {
        _hasReward = false;
        selectObj.SetActive(false);
        iconImage.sprite = null;
        iconImage.rectTransform.sizeDelta = _originalIconSize;
        iconImage.gameObject.SetActive(false);
        if (iconBgImage != null)
        {
            iconBgImage.sprite = null;
            iconBgImage.gameObject.SetActive(false);
        }
        if (txt_count != null) txt_count.gameObject.SetActive(false);
    }

    private void SetIconBg(Sprite bgSprite)
    {
        if (iconBgImage == null) return;
        if (bgSprite != null) iconBgImage.sprite = bgSprite;
        iconBgImage.gameObject.SetActive(iconBgImage.sprite != null);
    }

    private void SetIcon(Sprite sprite)
    {
        if (this == null) return;
        if (sprite != null) iconImage.sprite = sprite;
        if (iconImage.sprite == null) return;
        iconImage.gameObject.SetActive(true);
    }

}
