using System;
using UnityEngine;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO;
public class InteractNode1AddActionItem : MonoBehaviour
{
    public Button addBtn;

    public Action onAddAction; //添加事件



    void Awake()
    {
        addBtn.onClick.AddListener(OnAddBtnClick);
    }

    public void Init()
    {
        // iconImage.sprite = data.skinPack.cover;
    }

    void OnAddBtnClick()
    {
        onAddAction?.Invoke();
    }




}
