using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using Game.Store;
using GameData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BannerRootItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, ILowMemoryInterface
{
    [SerializeField] private BannerItem bannerItem;
    private RemoteImageBehaviour remoteImageBehaviour;
    private Button button;
    private string contestId;
    private string _curCoverUrl;
    private bool IsDrag = false;
    private BannerSkipData skipData;

    public BannerRootView banner;
    public bool isSale;

    private void Awake()
    {
        remoteImageBehaviour = GetComponentInChildren<RemoteImageBehaviour>();

        button = GetComponent<Button>();
        if (button) button.onClick.AddListener(OnMainClick);
    }

    public void ChangeRawImageState(bool isRelease)
    {
        if (remoteImageBehaviour) remoteImageBehaviour.ChangeRawImageState(isRelease);
        if (bannerItem) bannerItem.ChangeRawImageState(isRelease);
    }

    public void RefreshItem(ShapeBannerData info, IPool pool)
    {
        this._curCoverUrl = info.banner_url;
        if (bannerItem)
        {
            bannerItem.SetImgPool(pool);
            bannerItem.SetItemInfo(info, OnMainClick);
        }
        if (remoteImageBehaviour)
        {
            remoteImageBehaviour.Load(_curCoverUrl, true, null);
        }
        skipData = info.GetBannerSkip();
        //todo跳转id
        //contestId = info.contestId;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        IsDrag = true;
        banner?.BeginDrag(this, eventData.delta);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        banner?.EndDrag(this, eventData.delta);
    }

    public void OnDrag(PointerEventData eventData)
    {
        banner?.Drag(this, eventData.delta);
    }

    private void OnMainClick()
    {
        if (isSale)
        {
            if (IsDrag == false) banner?.EndDrag(this, new UnityEngine.Vector2());
            IsDrag = false;
            return;
        }
        banner?.EndDrag(this, new UnityEngine.Vector2());
        switch(skipData.bannerType)
        {
            case 2:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, skipData.id);
                break;
            case 3:
                UIManager.Inst.OpenPanel(PanelId.ShapeThemePanel, skipData.id);
                UIManager.Inst.FindPanel(WindowId.FittingRoomWindow,PanelId.FittingRoomPanel).gameObject.SetActive(false);
                //UIManager.Inst.ClosePanel(PanelId.FittingRoomPanel);
                break;
        }
        //ContestEventManager.Inst.OpenContestSelectPage(contestId);
        IsDrag = false;
    }
}
