using System;
using UnityEngine;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO;
public class InteractNode4BottomRoleItem : MonoBehaviour
{
    public GameObject addIconGo;
    public Button addBtn;
    public Button deleteBtn;

    public RemoteImageBehaviour remoteImageBehaviour;

    void Awake()
    {
        addBtn.onClick.AddListener(OnAddBtnClick);
        deleteBtn.onClick.AddListener(OnDeleteBtnClick);
    }

    public void SetData(int type, string emoteId)
    {
        if(type == 0)
        {
            //添加
            addIconGo.SetActive(true);
            addBtn.gameObject.SetActive(true);
            remoteImageBehaviour.gameObject.SetActive(false);
            deleteBtn.gameObject.SetActive(false);
        }else if(type == 1)
        {
            //动作icon图标
            addIconGo.SetActive(false);
            addBtn.gameObject.SetActive(false);
            remoteImageBehaviour.gameObject.SetActive(true);
            deleteBtn.gameObject.SetActive(true);
        }
    }

    void OnAddBtnClick()
    {
    }
    void OnDeleteBtnClick()
    {
    }




}
