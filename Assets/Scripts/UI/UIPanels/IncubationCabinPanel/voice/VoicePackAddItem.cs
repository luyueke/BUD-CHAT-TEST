using System;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;
public class VoicePackAddItem : MonoBehaviour
{
    public Text textName;
    public Button addBtn;
    public GameObject vipGo;
    protected CabinCharacterUgcInfo _characterUgcInfo;

    public Action onAddAction;

    void Awake()
    {
        addBtn.onClick.AddListener(OnAddBtnClick);
    }

    public void Init(CabinCharacterUgcInfo _characterUgcInfo)
    {
        vipGo.SetActive(false);
        this._characterUgcInfo = _characterUgcInfo;
    }

    void OnAddBtnClick()
    {
        var panel = UIManager.Inst.OpenPanel<IncubationCabinPopPanel>(PanelId.IncubationCabinPopPanel, _characterUgcInfo, _characterUgcInfo.toneId);
        panel.SetData(PopType.BigWin1);
        onAddAction?.Invoke();
    }
}
