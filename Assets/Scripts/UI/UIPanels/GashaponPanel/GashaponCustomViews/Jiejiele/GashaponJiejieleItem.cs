using Game.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class GashaponJiejieleItem : MonoBehaviour
{
    [SerializeField] public string ID;
    [SerializeField] public GameObject hasImage;
    [SerializeField] public Image Icon;
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
    public void Init(out bool isHave, Action _clickAct)
    {
        clickAct += _clickAct;
        isHave = AssetsDataManager.IsOwned(ID);
        hasImage.SetActive(isHave);

        Icon.sprite = PgcUtils.GetIconSpriteByPgcId(ID,gameObject);
    }
}
