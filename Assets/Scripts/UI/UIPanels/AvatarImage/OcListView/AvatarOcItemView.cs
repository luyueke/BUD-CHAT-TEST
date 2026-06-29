using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;

public class AvatarOcItemView : MonoBehaviour
{
    public GameObject SelectedView;
    public RemoteImageBehaviour iconView;
    private Action<AvatarOcData> ClickAction;
    private Action<AvatarOcData> LongClickAction;
    private void Awake()
    {
    }
    
    public void Init()
    {
        var cLongBtn = transform.GetComponent<CLongButton>();
        cLongBtn.onClick.AddListener(OnClickItem);
        cLongBtn.onLongClick.AddListener(OnLongClickItem);
    }

    public AvatarOcData CurrentData; 
    public void SetData(AvatarOcData data, Action<AvatarOcData> ClickAct = null, Action<AvatarOcData> LongClickAct = null)
    {
        CurrentData = data;
        ClickAction = ClickAct;
        LongClickAction = LongClickAct;
    }

    
    private void OnClickItem()
    {
        ClickAction?.Invoke(CurrentData);
    }

    public void UpdateSelected(bool isSelected)
    {
        SelectedView.SetActive(isSelected);
    }

    private void OnLongClickItem()
    {
        LongClickAction?.Invoke(CurrentData);
    }
}
