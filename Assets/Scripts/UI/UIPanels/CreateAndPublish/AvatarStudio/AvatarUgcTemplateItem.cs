
using System;
using Es;
using UnityEngine;
using UnityEngine.UI;
using Game.Audio;
using Game.Event;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UnityEngine.U2D;
using Newbie;

public class AvatarUgcTemplateItem : MonoBehaviour
{
    [SerializeField] private CButton itemBtn;
    [SerializeField] private Image iconSprite;
    [SerializeField] private Image background;
    [SerializeField] private Text titleTxt;
    // [SerializeField] private GameObject vipLogo;
    // [SerializeField] private GameObject freeTag;
    [SerializeField] private GameObject timeLimite;
    private ClothesTemplate itemInfo;
    private Action<ClothesTemplate> onSelectItem;
    private bool isFree = false;
    public static bool isFirst = true;

    string key = "FirstOpenAvatrStudio_" + AccountDataManager.Inst.Uid;
    public void OnItemCreate(ClothesTemplate info, Action<ClothesTemplate> onSelect,SpriteAtlas atlas , bool isAvatar = true)
    {
        if (isFirst && isAvatar)
        {

            TimerManager.Inst.RunOnce("Boot", 0.2f, () =>
            {
                var bootMono = this.gameObject.GetComponent<BootMaskMono>();
                if (bootMono == null)
                {
                    this.gameObject.AddComponent<BootMaskMono>().id.Add(111);
                }
                else
                {
                    bootMono.id.Add(111);
                }
                if (!PlayerPrefs.HasKey(key) && !BootPanel.isPlaying && AccountDataManager.Inst.UserInfo.isNewUser==1)
                {
                    PlayerPrefs.SetInt(key, 1);
                    PlayerPrefs.Save();
                    UIManager.Inst.OpenPanel(PanelId.BootPanel , WindowId.AvatarStudioWindow, 111);
                }
            });
                isFirst = false;
            
            
        }
        
        itemInfo = info;
        onSelectItem = onSelect;
        // SetVipLogo();
        AdjustNewTag();
        iconSprite.sprite = atlas.GetSprite(info.Cover);
        itemBtn.onClick.AddListener(OnItemClick);
        titleTxt.SetLocalText(info.Name);
        timeLimite.SetActive(itemInfo.IsLimiteTime && !LimitTimePropManager.Inst.CheckLimitPetProp(info.Id));
    }

    private void AdjustNewTag()
    {
        // var key = itemInfo?.editInfo?.showNewTag;
        // if (string.IsNullOrEmpty(key))
        // {
        //     return;
        // }
        //
        // var value = PlayerPrefs.GetInt(key);
        // newTag.gameObject.SetActive(value == 0);
    }

    private void RemoveNewTag()
    {
        // var key = itemInfo?.editInfo?.showNewTag;
        // if (string.IsNullOrEmpty(key))
        // {
        //     return;
        // }
        // PlayerPrefs.SetInt(key, 1);
        // PlayerPrefs.Save();
        // newTag.gameObject.SetActive(false);
    }

    private void SetVipLogo()
    {
        // vipLogo.SetActive(itemInfo.editInfo.isVip);
        // //1.82.0 freebie需求先这样处理
        // if (itemInfo.editInfo.templateId == 2000)
        // {
        //     // //如果不是vip，并且完成了创作者任务,并且领取了帽子
        //     // if (!CreatorQualifyManager.Inst.IsOverHighVip && CreativeRewardManager.Inst.IsClaimRewardHat())
        //     // {
        //     //     isFree = true;
        //     //     vipLogo.SetActive(false);
        //     //     freeTag.SetActive(true);
        //     // }
        // }
    }

    private void OnItemClick()
    {
        RemoveNewTag();
        AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_EnterGame_A1);
        if (timeLimite.activeSelf)
        {
            // GashaponDataManager.Inst.JumpToGashapon("lottery.welcomeSpring");
            TipPanel.ShowToast("活动已结束");
            return;
        }
        onSelectItem?.Invoke(itemInfo);
#if UNITY_EDITOR || LOCAL_BUILD
        // onSelectItem?.Invoke();
#else
        // if (itemInfo.editInfo.isVip)
        // {
        //     if (isFree)
        //     {
        //         onSelectItem?.Invoke(partType, tempId);
        //     }
        //     else
        //     {
        //         if (CreatorQualifyManager.Inst.IsOverHighVip||CreatorQualifyManager.Inst.IsInAnyProgram())
        //         {
        //             onSelectItem?.Invoke(partType, tempId);
        //         }
        //         else
        //         {
        //             AvatarTemplatePreviewPanel.Show();
        //             AvatarTemplatePreviewPanel.Instance.SetModle(itemInfo.editInfo.templateId);
        //             AvatarTemplatePreviewPanel.Instance.logChannel = GetLogChannel();
        //         }
        //     }
        // }
        // else
        // {
        //     onSelectItem?.Invoke(partType, tempId);
        // }
#endif
    }

}

