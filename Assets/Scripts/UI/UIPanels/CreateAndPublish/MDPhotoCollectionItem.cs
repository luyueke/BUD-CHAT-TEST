using Com.TheFallenGames.OSA.Util.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO.Pools;

public class MDPhotoCollectionItem : MonoBehaviour, IRefreshable<PhotoListItemInfo>
{
    [SerializeField] private RemoteImageBehaviour remoteImage;
    [SerializeField] private Button mainBtn;

    private Action<MDPhotoCollectionItem> onClick;

    public PhotoListItemInfo CurInfo { get; private set; }

    private void Start()
    {
        mainBtn.onClick.AddListener(OnClickInternal);
    }

    public void Refresh(PhotoListItemInfo data)
    {
        CurInfo = data;
        if (data == null) return;
        remoteImage.Load(data.photoCover);
    }

    public void SetPool(IPool pool)
    {
        remoteImage.InitializeWithPool(pool);
    }

    public void SetOnClick(Action<MDPhotoCollectionItem> act)
    {
        onClick = act;
    }

    private void OnClickInternal()
    {
        onClick?.Invoke(this);
    }
}

public class PhotoListRsp
{
    public int isEnd;
    public string cookie;
    public List<PhotoListItemInfo> photos;
}

public class PhotoListItemInfo
{
    public string photoId;
    public string photoCover;
    public string photoCoverFull;
}
