using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class TipPanel : BasePanel<TipPanel>
{
    private Animator anim;
    private Text Title;
    private Vector3 startPos = new Vector3(0, -1000, 0);
    private bool isOnShow = false;

    public override void OnCreate()
    {
        base.OnCreate();
        Title = this.GetComponentInChildren<Text>();
        anim = this.transform.GetComponentInChildren<Animator>();
        anim.enabled = false;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        var str = args[0] as string;
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        Title.SetLocalText(str);
        
        if (isOnShow)
            return;
        isOnShow = true;
        anim.enabled = true;
        anim.Play("ToastAnim", 0, 0);
        CancelInvoke("OnHide");
        Invoke("OnHide", 2f);
    }

    private void OnHide()
    {
        isOnShow = false;
        anim.enabled = false;
        CloseSelf();
    }

    public static void ShowToast(string content)
    {
        UIManager.Inst.OpenPanel(PanelId.TipPanel, content);
        // TipPanel.Instance.transform.SetAsLastSibling();
    }


    public static void HideToast(string content = null)
    {
        var tipPanel = UIManager.Inst.FindPanel<TipPanel>(WindowId.CommonWindow, PanelId.TipPanel);
        if(string.IsNullOrEmpty(content) || tipPanel?.Title.text == content)
        {
            tipPanel?.CloseSelf();
        }
    }

    public static void ShowToast(string content,params object[] formatArgs)
    {
        string result = LocalizationManager.Inst.GetLocalizedText(content, formatArgs);
        UIManager.Inst.OpenPanel(PanelId.TipPanel, result);
    }

    // public void SetTitle(string content, params object[] formatArgs)
    // {
    //     // LocalizationConManager.Inst.SetLocalizedContent(Title, content, formatArgs);
    // }

}