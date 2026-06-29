using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BundleItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] RemoteImageBehaviour remoteImageBehaviour;
    [SerializeField] GameObject ownedRoot;

    private AssetsData mData;
    public void Awake()
    {
        button.onClick.AddListener(() =>
        {
            if (mData == null || mData.UgcInfo == null || mData.UgcInfo.skinInfo == null || string.IsNullOrEmpty(mData.UgcInfo.skinInfo.id)) return;
            UIManager.Inst.OpenPanel(PanelId.AssetDetailPanel, AssetDetailType.Skin, mData.UgcInfo.skinInfo.id, mData.UgcInfo.skinInfo.ugcStyle);
        });
    }

    public void SetData(AssetsData assetsData)
    {
        if (assetsData == null) return;
        mData = assetsData;
        if (assetsData.UgcInfo != null && assetsData.UgcInfo.skinInfo != null)
        {
            remoteImageBehaviour.Load(assetsData.UgcInfo.skinInfo.cover);
        }
        ownedRoot.SetActive(assetsData.InventoryData != null && assetsData.InventoryData.OwnedNum > 0);
    }
}
