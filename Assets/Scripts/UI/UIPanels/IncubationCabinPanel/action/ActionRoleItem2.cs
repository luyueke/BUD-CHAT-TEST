using System;
using UnityEngine;
using UnityEngine.UI;
public class ActionRoleItem2 : MonoBehaviour
{
    public Image iconImage;
    public GameObject vipGo;
    public Button deleteBtn;

    public Action onDeleteAction; //删除事件


    void Awake()
    {
        deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        vipGo?.SetActive(false);
        deleteBtn.gameObject.SetActive(false);
    }

    public void Init( )
    {
        // iconImage.sprite = data.skinPack.cover;
    }

    void OnDeleteBtnClick()
    {
        onDeleteAction?.Invoke();
    }


   
}
