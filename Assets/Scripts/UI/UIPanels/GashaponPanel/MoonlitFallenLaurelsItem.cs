using Game.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class MoonlitFallenLaurelsItem : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public string[] ID;
    [SerializeField] public GameObject hasImage;
    [SerializeField] protected CButton clickBtn;
    Action clickAct;
    private void Start()
    {
        clickBtn.onClick.AddListener(OnClickBtn);
    }
    void OnClickBtn()
    {
        clickAct?.Invoke();
    }
    public void Init(out bool isHave , Action _clickAct)
    {
        clickAct += _clickAct;
        isHave = false;
        for(int i = 0; i < ID.Length; i++){
            isHave = AssetsDataManager.IsOwned(ID[i]);
            if(!isHave) break;
        }
        hasImage.SetActive(isHave);
    }
}
