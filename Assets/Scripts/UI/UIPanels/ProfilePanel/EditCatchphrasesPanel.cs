using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;

public class EditCatchphrasesPanel : BasePanel<EditCatchphrasesPanel>
{
    public Transform BG;
    public TextInputView[] TextInputs;
    
    public CButton CloseBtn;
    public CButton ComfirmBtn;
    public Action<List<string>> OnComplete;
    private List<string> catchphrases;
    public override void OnCreate()
    {
        base.OnCreate();
       
        ComfirmBtn.onClick.AddListener(OnComfirmClick);
        CloseBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        InitBG();
        if (args[0] != null)
        {
            List<string> chats = args[0] as List<string>;
            catchphrases = chats;
            for (var i = 0; i < chats.Count; i++)
            {
                TextInputs[i].SetInputWithoutNotify(chats[i]);
            }
        }
    }

    private void InitBG()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3","avatar_icon_4"
        });
        item.SetBgImageVisible(false);
        item.gameObject.SetActive(true);
    }
    
    private void OnComfirmClick()
    {
        catchphrases = new List<string>();
        for (var i = 0; i < TextInputs.Length; i++)
        {
            catchphrases.Add(TextInputs[i].Input);
        }
        
        OnComplete?.Invoke(catchphrases);
        CloseSelf();
    }
}
