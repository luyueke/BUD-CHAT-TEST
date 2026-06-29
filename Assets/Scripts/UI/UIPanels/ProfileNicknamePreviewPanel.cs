using System;
using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class ProfileNicknamePreviewPanel : BasePanel<ProfileNicknamePreviewPanel>
{
    [SerializeField] private Button Btn_Close;
    [SerializeField] private Button Btn_Bg;

    [SerializeField] private Text title; // 主页皮肤标题
    [SerializeField] private Text previewTitle; // 主页皮肤标题

    [SerializeField] private GameObject previewRoot; 
    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_Bg.onClick.AddListener(CloseSelf);
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var Id = (int)args[0];
        var config = UserUIWidgetManager.Inst.GetNicknameData((int)Id);
        if (config != null)
        {
            title.text = config.Title;
            previewTitle.text = config.Desc;
            var o = Loader.Load<GameObject>(config.PreviewPath, gameObject);
            GameObject.Instantiate(o,previewRoot.transform);
        }
    }
    
}
