using UnityEngine;
using UnityEngine.UI;
using System;
using Game.Store;
public class VoiceItem : MonoBehaviour
{
    public GameObject roleGo;
    public Text textName;
    public Button selectBtn;
    public GameObject currentGo;
    public GameObject selectAcitonGo;
    public Button addBtn;
    public Button cancelBtn;

    public Action onAddAction;

    void Awake()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
        addBtn.onClick.AddListener(OnAddBtnClick);
        cancelBtn.onClick.AddListener(OnCancelBtnClick);
    }


    public void Init(GoodsData data)
    {
    }

    void OnSelectBtnClick()
    {
        selectAcitonGo.SetActive(true);
    }
    void OnAddBtnClick()
    {
        onAddAction?.Invoke();
    }
    void OnCancelBtnClick()
    {
        selectAcitonGo.SetActive(false);
    }
}
