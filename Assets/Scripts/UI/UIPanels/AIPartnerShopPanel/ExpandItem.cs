using System;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Database;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ExpandItem : MonoBehaviour
{
    [SerializeField] private CButton SelectBtn;
    [SerializeField] private GameObject SelectImg;
    [SerializeField] private Image CardImg;
    [SerializeField] private RemoteImageBehaviour CoverImage;
    [SerializeField] private Text ItemName;
    [SerializeField] private GameObject SelectObj;
    [SerializeField] private GameObject GetOver;

    public CabinCharacterPackInfo PackInfo { get; private set; }
    public RoleSkinData SkinData { get; private set; }
    public bool IsSelected { get; private set; }

    private bool _isOwned;
    private Action<ExpandItem> _onSelectionChanged;

    void Awake()
    {
        SelectBtn.onClick.AddListener(OnSelectClick);
    }

    public void SetData(RoleSkinData skinData, CabinCharacterPackInfo packInfo, Action<ExpandItem> onSelectionChanged)
    {
        PackInfo = packInfo;
        SkinData = skinData;
        _onSelectionChanged = onSelectionChanged;
        ItemName.text = skinData?.Name;
        IsSelected = false;
        SelectImg.SetActive(false);
        var cover = skinData?.skinPackInfo?.cover;
        if (!string.IsNullOrEmpty(cover))
            CoverImage.Load(cover);
        LoadCardBackground(packInfo);
        RefreshOwnership();
    }

    private void RefreshOwnership()
    {
        var inv = BagDatabase.Inst.Select(PackInfo?.id);
        _isOwned = (inv != null && inv.OwnedNum > 0)
                   || PackInfo?.creator == AccountDataManager.Inst.Uid;
        if (GetOver != null) GetOver.SetActive(_isOwned);
        if (SelectObj != null) SelectObj.SetActive(!_isOwned);
    }

    private void LoadCardBackground(CabinCharacterPackInfo packInfo)
    {
        var colorId = packInfo?.coverInfo?.GetDetail()?.colorId;
        if (colorId == null || colorId == 0) return;
        var config = DataTables.GetDraftBoxCardColorConfig(colorId.Value);
        if (config == null) return;
        var path = $"Assets/Loadable/UI/UIPanel/IncubationCabinDraftBox/SkinSprite/{config.Color}.png";
        var sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(path, gameObject);
        if (sprite != null) CardImg.sprite = sprite;
    }

    private void OnSelectClick()
    {
        if (_isOwned) return;
        IsSelected = !IsSelected;
        SelectImg.SetActive(IsSelected);
        _onSelectionChanged?.Invoke(this);
    }
}
