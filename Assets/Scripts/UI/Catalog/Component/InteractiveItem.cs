using Es;
using Game.Avatar;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Catalog;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class InteractiveItem : MonoBehaviour
{
    public Text nameText;
    public Text LockText;
    public Button showButton;
    public GameObject lockImage;
    public Button lockBtn;

    public Image emoteImage;

    string grallyId;
    string roleAvatar;
    // 红点组件
    public GameObject redPoint;

    RoleEmote emote; 

    // 当前是否已注册事件
    private bool _hasRegisteredEvents = false;
    
    private void Awake()
    {
        lockBtn.onClick.AddListener(ClickedLock);
    }

    public void InitUI(RoleEmote _emote ,string _grallyId , string _roleAvatar)
    {
        grallyId = _grallyId;
        emote = _emote;
        roleAvatar = _roleAvatar;
        UpdateUI();
    }

    private void UpdateUI()
    {
        showButton.interactable = emote.islock !=1;

        var emoConfig = DataTables.GetEmoUIConfig(emote.emoteId);
        if (emoConfig != null)
        {
            nameText.text = emoConfig.name;  // 获取动作名字
        }
        LockText.text = emote.islock==1 ? "未解锁" : "已解锁";
        lockImage.SetActive(emote.islock == 1);
        emoteImage.sprite = PgcUtils.GetIconSpriteByPgcId(emote.emoteId, gameObject);
        UpdateRedPoint();
    }
    
    public void UpdateRedPoint()
    {   
        var key = AccountDataManager.Inst.Uid + grallyId + emote.emoteId;
        if (emote.islock==1)
        {
            redPoint.SetActive(false);

        }
        if (PlayerPrefs.HasKey(key)) {
            redPoint.SetActive(false);
            return;
        }
        HospitalCatalogPanel.Instant.UpdateRedPoint();
    }
    // 按钮点击事件
    public void OnShowButtonClicked()
    {
        var key = AccountDataManager.Inst.Uid +  grallyId + emote.emoteId;
        if (!PlayerPrefs.HasKey(key))
        {   
            redPoint.SetActive(false);
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            HospitalCatalogPanel.Instant.UpdateRedPoint();
        }
        //播放动画
        HospitalCatalogPanel.Instant.ShowAnime(emote.emoteId);

    }

    public void  ClickedLock()
    {
        TipPanel.ShowToast("您还未解锁该交互动作，快去游玩解锁吧");
    }
}
