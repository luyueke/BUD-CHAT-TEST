using System;
using Game.AnimationStudio;
using UnityEngine;
using UnityEngine.UI;
public class ActionCreateItem : MonoBehaviour
{
    public Button createBtn;
    public Action onCreateAction; //创建事件


    void Awake()
    {
        createBtn.onClick.AddListener(OnCreateBtnClick);
    }

    public void Init()
    {
        // iconImage.sprite = data.skinPack.cover;
    }

    /// <summary>
    /// 动作编辑器
    /// </summary>
    void OnCreateBtnClick()
    {
        UIManager.Inst.OpenPanel<AnimationStudioMainPanel>(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);

        onCreateAction?.Invoke();
    }


}
