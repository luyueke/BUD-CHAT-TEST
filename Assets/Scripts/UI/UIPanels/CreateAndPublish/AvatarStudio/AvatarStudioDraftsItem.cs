using System;
using UI.BaseWidgets;
using UnityEngine;

public class AvatarStudioDraftsItem : AvatarStudioBaseItem
{
    public CButton draftsBtn;
    public CButton createBtn;
    //Data
    public DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;
    private Action _onCreateClickAct;
    public override void Init(Action<DraftListItem> onSelect,Action onCreateSelect, DraftListItem data)
    {
        //说明是CreateBtn
        bool isCreate = (data == null) || GameStudioUtils.GetBaseInfo(data) == null;
        createBtn.gameObject.SetActive(isCreate);
        draftsBtn.gameObject.SetActive(!isCreate);

        _onCreateClickAct = onCreateSelect;
        if (isCreate)
        {
            createBtn.onClick.RemoveAllListeners();
            createBtn.onClick.AddListener(()=>
            {
                _onCreateClickAct?.Invoke();
            });
            return;
        }
        
        _onClickAct = onSelect;
        _curData = data;
        
        draftsBtn.onClick.RemoveAllListeners();
        draftsBtn.onClick.AddListener(OnDraftsBtnClick);
    }
    private void OnDraftsBtnClick()
    {
        if (_curData == null)
        {
            return;
        }
        _onClickAct?.Invoke(_curData);
    }
  
}
