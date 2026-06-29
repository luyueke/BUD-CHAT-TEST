using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using UI.BaseWidgets;
using UnityEngine;

public class TheatreProfileItem : MonoBehaviour
{
    [SerializeField] private RemoteImageBehaviour cover;
    [SerializeField] private CButton clickBtn;

    private DraftListItem _data;

    private void Awake()
    {
        if (cover == null) cover = GetComponentInChildren<RemoteImageBehaviour>(true);
        if (clickBtn == null) clickBtn = GetComponentInChildren<CButton>(true);
    }

    public void SetData(DraftListItem data)
    {
        _data = data;
        var coverUrl = data?.theatreInfo?.cover;
        if (cover != null)
        {
            if (string.IsNullOrEmpty(coverUrl))
                cover.gameObject.SetActive(false);
            else
            {
                cover.gameObject.SetActive(true);
                cover.Load(coverUrl, true, null);
            }
        }

        if (clickBtn != null)
        {
            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() =>
                UIManager.Inst.OpenPanel(PanelId.TheatreInfoPanel, _data.theatreInfo, (int)TheatreEnterType.Store));
        }
    }
}
