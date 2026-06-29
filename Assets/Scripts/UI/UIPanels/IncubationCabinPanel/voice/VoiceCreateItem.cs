using UnityEngine;
using UnityEngine.UI;
using System;
using UI.UIPanels.IncubationCabin;
public class VoiceCreateItem : MonoBehaviour
{
    public Button createBtn;

    public Action onCreateAction;
    private CabinCharacterUgcInfo cabinCharacter;

    void Awake()
    {
        createBtn.onClick.AddListener(OnCreateBtnClick);
    }


    public void Init(CabinCharacterUgcInfo cabinCharacter)
    {
        this.cabinCharacter= cabinCharacter;
    }

    void OnCreateBtnClick()
    {
        UIManager.Inst.OpenPanel<CabinPublishUgcAnimTonePanel>(PanelId.CabinPublishUgcAnimTonePanel, cabinCharacter);
        onCreateAction?.Invoke();
    }
}
