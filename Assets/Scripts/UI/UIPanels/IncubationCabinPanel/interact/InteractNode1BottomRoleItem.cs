using System;
using UnityEngine;
using UnityEngine.UI;
public class InteractNode1BottomRoleItem : MonoBehaviour
{
    public Image iconImage;
    public GameObject vipImgGo;
    public Button deleteBtn;

    public Action<string> onDeleteAction; //删除事件

    public string id;


    void Awake()
    {
        deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        vipImgGo?.SetActive(false);
    }

    public void Init( )
    {
        // iconImage.sprite = data.skinPack.cover;
    }

    void OnDeleteBtnClick()
    {
        onDeleteAction?.Invoke(id);
    }



   
}
