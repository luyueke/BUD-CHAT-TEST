using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TheatreClose : MonoBehaviour
{
    [SerializeField] private Button cancelBtn;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private Button confirmNoSaveBtn;


    private Action onConfirm;
    private Action onConfirmNoSave;

    public void Init(Action onConfirm, Action onConfirmNoSave = null){
        this.onConfirm = onConfirm;
        this.onConfirmNoSave = onConfirmNoSave;
        cancelBtn.onClick.AddListener(OnCancelBtnClick);
        confirmBtn.onClick.AddListener(OnConfirmBtnClick);
        confirmNoSaveBtn?.onClick.AddListener(OnConfirmNoSaveBtnClick);
    }
    
    private void OnCancelBtnClick(){
        gameObject.SetActive(false);
    }

    private void OnConfirmBtnClick(){
        onConfirm?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnConfirmNoSaveBtnClick(){
        onConfirmNoSave?.Invoke();
        gameObject.SetActive(false);
    }
}
