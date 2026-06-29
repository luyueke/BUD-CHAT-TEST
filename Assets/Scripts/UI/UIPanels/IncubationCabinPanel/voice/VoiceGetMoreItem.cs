using UnityEngine;
using UnityEngine.UI;
using System;
using UI.UIPanels.IncubationCabin;
public class VoiceGetMoreItem : MonoBehaviour
{
    public Button getMoreBtn;

    public Action onGetMoreAction;

    void Awake()
    {
        getMoreBtn.onClick.AddListener(OnGetMoreBtnClick);
    }

    public void Init()
    {
        
    }

    void OnGetMoreBtnClick()
    {
        // UIManager.Inst.OpenPanel(PanelId.TimbreStorePanel);
        UIManager.Inst.OpenPanel<PublishUgcAnimTonePanel>(PanelId.UgcAnimToneStorePanel);
        // onGetMoreAction?.Invoke();
    }
}
