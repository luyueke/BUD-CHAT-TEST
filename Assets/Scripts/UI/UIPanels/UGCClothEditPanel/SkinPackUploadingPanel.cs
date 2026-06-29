using System;
using GameData;
using GameData.MapData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class SkinPackUploadingPanel : BasePanel<SkinPackUploadingPanel>
{
    [SerializeField] private CButton cancelBtn;
    [SerializeField] private CButton closeBtn;
    public Action OnCancel { get; set; }

    private void Start()
    {
        if (cancelBtn) cancelBtn.onClick.AddListener(OnCancelInternal);
        if (closeBtn) closeBtn.onClick.AddListener(OnCancelInternal);
    }

    private void OnCancelInternal()
    {
        OnCancel?.Invoke();
    }
}