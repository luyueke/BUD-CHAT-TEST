using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PartnerDetailSkinItem : MonoBehaviour
{
    [SerializeField] private CButton ItemBtn;
    [SerializeField] private Text TagName;
    [SerializeField] private RemoteImageBehaviour remoteImage;
    [SerializeField] private GameObject SelectedIndicator;

    private SkinPackInfo _data;
    private Action<SkinPackInfo> _action;

    void Awake()
    {
        ItemBtn.onClick.AddListener(() => _action?.Invoke(_data));
    }

    public void SetData(SkinPackInfo data, Action<SkinPackInfo> action, int idx, bool isSelected = false)
    {
        _data = data;
        _action = action;
        TagName.text = "";
        remoteImage.Load(data.cover, true, null);
        if(SelectedIndicator!=null)
        {
            SelectedIndicator.SetActive(isSelected);
        }
    }
}
