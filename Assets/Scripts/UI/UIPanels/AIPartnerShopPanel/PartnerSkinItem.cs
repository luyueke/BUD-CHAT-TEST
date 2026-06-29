using System;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using UI.BaseWidgets;
using UnityEngine;

public class PartnerSkinItem : MonoBehaviour
{
    [SerializeField] private RemoteImageBehaviour coverImage;
    [SerializeField] private GameObject selectedIndicator;
    [SerializeField] private CButton clickBtn;

    private Action<SkinPackInfo> _onClick;
    private SkinPackInfo _model;

    public void Init()
    {
        clickBtn.onClick.AddListener(OnClick);
    }

    public void InitPool(IPool pool)
    {
        coverImage.InitializeWithPool(pool);
    }

    public void SetData(SkinPackInfo model, Action<SkinPackInfo> onClick, bool isSelected)
    {
        _model = model;
        _onClick = onClick;
        coverImage.Load(model?.cover, true, null);
        SetSelected(isSelected);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedIndicator != null) selectedIndicator.SetActive(isSelected);
    }

    private void OnClick() => _onClick?.Invoke(_model);
}
