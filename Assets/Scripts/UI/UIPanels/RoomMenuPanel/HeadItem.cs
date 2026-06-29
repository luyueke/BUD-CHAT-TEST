using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HeadItem : MonoBehaviour
{
    [SerializeField] private HeadViewWidget headViewWidget;
    [SerializeField] private BUD_Text nickText;
    [SerializeField] private CButton headBtn;
    [SerializeField] private AddFriendButton addFriendButton;
    [SerializeField] private CButton banTextButton;
    [SerializeField] private CButton banVoiceButton;
    [SerializeField] private GameObject banTextOnImage;
    [SerializeField] private GameObject banTextOffImage;
    [SerializeField] private GameObject banVoiceOnImage;
    [SerializeField] private GameObject banVoiceOffImage;
    public string Uid  = "";
    public bool isTextBan  = false;
    public bool isVoiceBan  =false;
    
    public UnityEvent<HeadItem> onClick;
    public UnityEvent<HeadItem> onTextBan;
    public UnityEvent<HeadItem> onVoiceBan;
    private void Awake()
    {
        headBtn.onClick.AddListener(OnHeadBtnClick);
        banTextButton.onClick.AddListener(OnTextBanClick);
        banVoiceButton.onClick.AddListener(OnVoiceBanClick);
    }
    
    private void OnHeadBtnClick()
    {
        onClick?.Invoke(this);
    }
    private void OnTextBanClick()
    {
        SetTextBan(!isTextBan);
        onTextBan?.Invoke(this);
       
    }
    private void OnVoiceBanClick()
    {
        SetVoiceBan(!isVoiceBan);
        onVoiceBan?.Invoke(this);
    }
    public void SetNick(string nickStr,int clip = -1)
    {
        nickText.text = nickStr;
    }

    public void SetHeadUrl(AccountUserInfo userInfo)
    {
        headViewWidget?.InitHeadCycle(userInfo);
    }

    public void SetRelation(string targetUid, RelationShipInfo relationShipInfo)
    {
        addFriendButton.SetRelation(targetUid, relationShipInfo);
    }

    public void SetTextBan(bool ban)
    {
        banTextButton.gameObject.SetActive(true);
        isTextBan = ban;
        banTextOnImage.gameObject.SetActive(!ban);
        banTextOffImage.gameObject.SetActive(ban);
    }
    public void SetVoiceBan(bool ban)
    {
        banVoiceButton.gameObject.SetActive(true);
        isVoiceBan = ban;
        banVoiceOnImage.gameObject.SetActive(!ban);
        banVoiceOffImage.gameObject.SetActive(ban);
    }
}
