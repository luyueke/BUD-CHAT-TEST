using System;
using Game.Store;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SnowBoardItem : MonoBehaviour
{
    [SerializeField] public string ID;
    [SerializeField] public ResourceType itemType;
    [SerializeField] public GameObject hasImage;
    [SerializeField] protected CButton clickBtn;
    [SerializeField] public Image ItemIcon;
    Action clickAct;
    private void Start()
    {
        clickBtn.onClick.AddListener(OnClickBtn);
        //todo显示道具图标
        ShowImage();
    }
    void OnClickBtn()
    {
        clickAct?.Invoke();
    }
    public void Init(out bool isHave, Action _clickAct)
    {
        clickAct = _clickAct;
        isHave = AssetsDataManager.IsOwned(ID);
        hasImage.SetActive(isHave);
    }

    private void ShowImage()
    {
        if(string.IsNullOrEmpty(ID))
        {
            return;
        }
        if (itemType == ResourceType.Emote)
        {
            PgcUtils.LoadEmoteIconAsync(ID, gameObject, (iconSprite) =>
            {
                ItemIcon.sprite = iconSprite;
            });
        }
        else if (itemType == ResourceType.Avatar)
        {
            PgcUtils.LoadAvatarIconAsync(ID, gameObject, (iconSprite) =>
            {
                ItemIcon.sprite = iconSprite;
            });
        }
        else if(itemType == ResourceType.Currency)
        {
            ItemIcon.sprite = PgcUtils.LoadRewardIcon(BUDRewardType.RewardTypeMiaoCoin, ItemIcon.gameObject);
        }
    }
}
