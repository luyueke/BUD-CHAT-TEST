using System;
using Com.TheFallenGames.OSA.Util.IO;
using Basic.Utils;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class GameStudioDetailView : BasePanel<GameStudioDetailView>
{
    [SerializeField] protected CButton closeBtn;
    [SerializeField] protected SuperTextMesh detailName;
    [SerializeField] protected CButton detailRenameBtn;
    [SerializeField] protected CButton detailCopyBtn;
    [SerializeField] protected CButton detailDeleteBtn;
    [SerializeField] protected CButton detailEditInfoBtn;
    [SerializeField] protected CButton detailPublishBtn;
    [SerializeField] protected CButton detailUpdateBtn;
    [SerializeField] protected CButton detailEditBtn;
    [SerializeField] protected CButton detailShareBtn;
    [SerializeField] protected CButton detailViewGameBtn;
    [SerializeField] protected CText lastEditText;
    [SerializeField] protected RemoteImageBehaviour detailCover;
    private Action OnRenameAct { get; set; }
    private Action OnCopyAct { get; set; }
    private Action OnDeleteAct { get; set; }
    private Action OnEditInfoAct { get; set; }
    private Action OnPublishAct { get; set; }
    private Action OnUpdateAct { get; set; }
    private Action OnEditAct { get; set; }
    private Action OnViewGameAct { get; set; }

    private MapInfo _curMapInfo;
    private bool _isEdit;


    public override void OnShow(params object[] args)
    {
        base.OnShow();
        _curMapInfo = (MapInfo)args[0];
        _isEdit = (bool)args[1];

        detailName.text = _curMapInfo.name;
        detailCover.Load(_curMapInfo.cover);
        RefreshLastEditText();
        ShowIsEdit(_isEdit);
    }

    private void ShowIsEdit(bool isEdit)
    {
        if (detailPublishBtn) detailPublishBtn.gameObject.SetActive(isEdit);
        if (detailRenameBtn) detailRenameBtn.gameObject.SetActive(isEdit);
        if (detailCopyBtn) detailCopyBtn.gameObject.SetActive(isEdit);
        if (detailDeleteBtn) detailDeleteBtn.gameObject.SetActive(true);
        if (detailEditBtn) detailEditBtn.gameObject.SetActive(isEdit);

        if (detailEditInfoBtn) detailEditInfoBtn.gameObject.SetActive(!isEdit);
        if (detailUpdateBtn) detailUpdateBtn.gameObject.SetActive(!isEdit);
        if (detailViewGameBtn) detailViewGameBtn.gameObject.SetActive(!isEdit);
    }

    protected void OnRename()
    {
        OnRenameAct?.Invoke();
    }

    protected void OnCopy()
    {
        OnCopyAct?.Invoke();
    }

    protected void OnDelete()
    {
        OnDeleteAct?.Invoke();
    }

    protected void OnEdit()
    {
        OnEditAct?.Invoke();
    }

    protected void OnPublish()
    {
        OnPublishAct?.Invoke();
    }

    protected void OnUpdate()
    {
        OnUpdateAct?.Invoke();
    }

    protected void OnEditInfo()
    {
        OnEditInfoAct?.Invoke();
    }

    protected void OnViewGame()
    {
        OnViewGameAct?.Invoke();
    }

    protected void RefreshLastEditText()
    {
        DateTime lastEditTime = _curMapInfo.lastModifiedTime;
        long timeStamp = GameUtils.GetUnixTimeStamp(lastEditTime);
        if (timeStamp == 0)
        {
            lastEditText.gameObject.SetActive(false);
        }
        else
        {
            lastEditText.gameObject.SetActive(true);
            string formattedDateTime = lastEditTime.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            lastEditText.text = "last edit" + ": " + formattedDateTime;
        }
    }
}
