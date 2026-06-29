using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class UGCCommonItem : MonoBehaviour
{
    public BUD_Text Name;
    public BUD_Text userName;
    public Text NumText;

    public CButton button;
    public RemoteImageBehaviour iconRemoteImageBehaviour;

    private Action click;
    
    public virtual void Init(string url, Action act)
    {
        iconRemoteImageBehaviour.Load(url, true, null);
        Init(act);
    }
    
    public virtual void Init(Texture tx, Action act)
    {
        iconRemoteImageBehaviour.RawImage.texture = tx;
        Init(act);
    }
    
    public virtual void Init(Action act)
    {
        click = act;
        Init();
    }
    
    public virtual void Init()
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
        gameObject.SetActive(true);
    }
    
    protected virtual void OnClick()
    {
        click?.Invoke();
    }
}
