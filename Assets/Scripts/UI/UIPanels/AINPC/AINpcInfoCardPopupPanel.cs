using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AINpcInfoCardPopupPanel : BasePanel<AINpcInfoCardPopupPanel>
{
    [Header("UI相关")] 
    public CButton Btn_Close;
    public Text Txt_Name;
    public Text Txt_Gender;
    public Text Txt_Age;
    public NpcDescWidget DescWidget;

    [Header("动画")] 
    public NpcAnimWidget NpcAnimWidget;
    [Header("口头禅对话框")]
    public NpcDialogBox npcDialogBox;
    //Npc口头禅对话框
    private Coroutine npcDelayHideDialogCoroutine;
    
    private List<TabItem> allItems;
    private AINpcInfo _curNpcInfo;
    private AINpcAnimType _curNpcType = AINpcAnimType.Idle;

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _curNpcInfo = (AINpcInfo)args[0];

        Btn_Close.onClick.AddListener(CloseSelf);
        InitUI();
    }

    private void InitUI()
    {
        NpcAnimWidget.InitData(_curNpcInfo);
        DescWidget.SetDescription(_curNpcInfo.npcDesc);
        SetNpcName(_curNpcInfo.npcName);
        SetGender(_curNpcInfo.npcGender);
        SetAge(_curNpcInfo.npcAge);
        // StartPreviewPetPhrase(_curNpcInfo);

    }
    
    private void SetNpcName(string npcName)
    {
        Txt_Name.text = npcName;
    }

    private void SetGender(int gender)
    {
        Txt_Gender.SetText("性别：" + (gender == 1 ? "男" : "女"));
    }

    private void SetAge(int age)
    {
        Txt_Age.SetLocalText(age < 0 ? "不详" : age.ToString());
    }
    
    #region NPC口头禅对话框

    private void StartPreviewPetPhrase(AINpcInfo npcInfo)
    {
        var petPhrase = npcInfo.npcPetPhrases;
        if (petPhrase == null)
        {
            return;
        }
        
        SetNpcTalk(petPhrase[0]);
    }
        
    public void SetNpcTalk(string text, bool needAni = true)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        npcDialogBox.gameObject.SetActive(true);
        npcDialogBox.ResetContent();
        if (npcDelayHideDialogCoroutine != null)
        {
            StopCoroutine(npcDelayHideDialogCoroutine);
        }
        npcDialogBox.SetTextAndSpeak(2, text, needAni,true,true, () =>
        {
            npcDelayHideDialogCoroutine = StartCoroutine(DelayHideFloatDialog(npcDialogBox,5));
        });
    }
        
    private IEnumerator DelayHideFloatDialog(NpcDialogBox dialogBox,float time)
    {
        yield return new WaitForSeconds(time);
        dialogBox.gameObject.SetActive(false);
        npcDelayHideDialogCoroutine = null;
    }
    #endregion
}
