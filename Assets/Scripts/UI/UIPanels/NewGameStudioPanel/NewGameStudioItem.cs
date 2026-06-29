using System;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class NewGameStudioItem : MonoBehaviour
{
    public RawImage mapImage;
    public CButton draftsBtn;
    public CButton createBtn;
    public GameObject UpLoading;
    public GameObject UpLoadFail;
    

    //Data
    public DraftListItem _curData;
    private Action<DraftListItem> _onClickAct;

    public void Init(Action<DraftListItem> act, DraftListItem data)
    {
        if (data == null || GameStudioUtils.GetBaseInfo(data) == null)
        {
            return;
        }

        _onClickAct = act;
        _curData = data;
        
        draftsBtn.onClick.RemoveAllListeners();
        draftsBtn.onClick.AddListener(OnDraftsBtnClick);
        
        createBtn.onClick.RemoveAllListeners();
        createBtn.onClick.AddListener(() => { });
    }

    private void OnDraftsBtnClick()
    {
        _onClickAct?.Invoke(_curData);
    }

    public void SetUpLoadState(UploadStatus state)
    {
        switch (state)
        {
            case UploadStatus.Uploading:
                UpLoading.gameObject.SetActive(true);
                UpLoadFail.gameObject.SetActive(false);
                break;
            
            case UploadStatus.UploadFail:
                UpLoading.gameObject.SetActive(false);
                UpLoadFail.gameObject.SetActive(true);
                break;
            
            default:
                UpLoading.gameObject.SetActive(false);
                UpLoadFail.gameObject.SetActive(false);
                break;
        }
    }
}
