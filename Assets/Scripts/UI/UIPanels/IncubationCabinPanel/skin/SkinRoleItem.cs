using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using System;
public class SkinRoleItem : MonoBehaviour
{
    public GameObject defaultImage;
    public Button cancelBtn;
    public Button selectBtn;
    public GameObject selectActionGo;
    public Button editBtn;
    public Button deleteBtn;

    public SkinPackInfo data;
    public RemoteImageBehaviour remoteAssetsIcon;
    public Action<SkinPackInfo> onSelectAction;
    public Action<SkinPackInfo, CharacterData> onEditClose;
    public Action<SkinPackInfo> onDeleteConfirm;

    void Awake()
    {
        selectBtn.onClick.AddListener(OnSelectBtnClick);
        // cancelBtn.onClick.AddListener(OnCancelBtnClick);
        editBtn.onClick.AddListener(OnEditBtnClick);
        deleteBtn.onClick.AddListener(OnDeleteBtnClick);
        selectActionGo?.SetActive(false);
    }

    public void Init(SkinPackInfo data)
    {
        this.data = data;
        RefreshDefaultState();

        remoteAssetsIcon.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(data.cover))
        {
            remoteAssetsIcon.Load(data.cover, onCompleted: (bool fromCache, bool success) =>
            {
                remoteAssetsIcon.gameObject.SetActive(true);
            });
        }
    }

    public void RefreshDefaultState()
    {
        defaultImage.SetActive(data.isDefault == 1);
    }

    public void RefreshCover(string url)
    {
        data.cover = url;
        remoteAssetsIcon.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(url))
        {
            remoteAssetsIcon.Load(url, loadCachedIfAvailable: false, onCompleted: (fromCache, success) =>
            {
                remoteAssetsIcon.gameObject.SetActive(true);
            });
        }
    }

    void OnSelectBtnClick()
    {
        selectActionGo.SetActive(true);
        onSelectAction?.Invoke(data);
    }

    void OnCancelBtnClick()
    {
        // selectActionGo.SetActive(false);
    }

    void OnEditBtnClick()
    {
        OnCancelBtnClick();
        FittingRoomPanel panel = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, "IncubationCabin", CharacterData.DeserializeObject(data.avatarJson));
        panel.OnCloseAction = (characterData) =>
        {
            onEditClose?.Invoke(data, characterData);
        };
    }

    void OnDeleteBtnClick()
    {
        OnCancelBtnClick();
        if (data.isDefault == 1)
        {
            TipPanel.ShowToast("默认形象不能删除");
            return;
        }
        onDeleteConfirm?.Invoke(data);
    }
}
