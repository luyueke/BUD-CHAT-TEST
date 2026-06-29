using System;
using BUD.AnimPose;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.BaseInfo;
using GameUI;
using UnityEngine;
using UnityEngine.UI;
public class ActionRoleItem : MonoBehaviour
{
    public RemoteImageBehaviour remoteIcon;
    public Button selectBtn;
    public Button cancelBtn;
    public GameObject hadAddGo;
    public GameObject selectActionGo;
    public Button addBtn;

    public Action<bool> onAddAction; //添加事件


    void Awake()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
        cancelBtn.onClick.AddListener(OnCancelBtnClick);
        addBtn.onClick.AddListener(OnAddBtnClick);
        selectActionGo?.SetActive(false);
        hadAddGo?.SetActive(false);
    }

    public void Init(PoseInfo data)
    {
        remoteIcon.Load(data.textureUrl, true, null);
    }

    void OnSelectBtnClick()
    {
        selectActionGo.SetActive(true);
    }

    void OnCancelBtnClick()
    {
        selectActionGo.SetActive(false);
        onAddAction?.Invoke(false);
    }

    void OnAddBtnClick()
    {
        
        onAddAction?.Invoke(true);
    }

   
}
