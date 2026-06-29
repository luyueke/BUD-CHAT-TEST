using Com.TheFallenGames.OSA.Util.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Extensions;
using Basic.Utils;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class LiteDetailItem : MonoBehaviour
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

    public bool IsShow => gameObject.activeSelf;
    public void Show(bool isShow)
    {
        gameObject.SetActive(isShow);
    }

    [SerializeField] protected MapInfo mapInfo;
    public MapInfo CurMapInfo => mapInfo;

    protected virtual void Start()
    {
        if (closeBtn) closeBtn.onClick.AddListener(() => Show(false));
        if (detailPublishBtn) detailPublishBtn.onClick.AddListener(OnPublish);
        if (detailRenameBtn) detailRenameBtn.onClick.AddListener(OnRename);
        if (detailCopyBtn) detailCopyBtn.onClick.AddListener(OnCopy);
        if (detailDeleteBtn) detailDeleteBtn.onClick.AddListener(OnDelete);
        if (detailEditBtn) detailEditBtn.onClick.AddListener(OnEdit);
        if (detailEditInfoBtn) detailEditInfoBtn.onClick.AddListener(OnEditInfo);
        if (detailUpdateBtn) detailUpdateBtn.onClick.AddListener(OnUpdate);
        if (detailViewGameBtn) detailViewGameBtn.onClick.AddListener(OnViewGame);
    }

    public virtual void Refresh(MapInfo mapInfo)
    {
        this.mapInfo = mapInfo;
        detailName.text = mapInfo.name;
        detailCover.Load(mapInfo.cover);
        RefreshLastEditText();
    }

    protected void RefreshLastEditText()
    {
        DateTime lastEditTime = mapInfo.lastModifiedTime;
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

    protected virtual void OnPublish() { }
    protected virtual void OnEdit() { }
    protected virtual void OnRename() { }
    protected virtual void OnCopy() { }
    protected virtual void OnDelete() { }
    protected virtual void OnEditInfo() { }
    protected virtual void OnUpdate() { }
    protected virtual void OnViewGame() { }
}
