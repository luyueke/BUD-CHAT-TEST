using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameData;

public class ContestBannerItem: MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, ILowMemoryInterface
{
    [SerializeField] private ActiveContestItem contestItem;
    private RemoteImageBehaviour remoteImageBehaviour;
    private Button button;
    private string contestId;
    private string _curCoverUrl;
    private bool IsDrag = false;
    
    public ContestBanner banner;
    public bool isHall;
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
        if (contestItem) contestItem.ChangeRawImageState(isRelease);
    }

    public void RefreshItem(ContestInfo info, IPool pool, bool useStreamer = false)
    {
        this._curCoverUrl = useStreamer ? info.streamerUrl : info.bannerUrl;
        if (contestItem)
        {
            contestItem.SetImgPool(pool);
            contestItem.SetItemInfo(info, OnMainClick, useStreamer);
        }
        if (remoteImageBehaviour)
        {
            remoteImageBehaviour.Load(_curCoverUrl, true, null);
        }

        contestId = info.contestId;
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
        ContestEventManager.Inst.OpenContestSelectPage(contestId);
        IsDrag = false;
    }
}
