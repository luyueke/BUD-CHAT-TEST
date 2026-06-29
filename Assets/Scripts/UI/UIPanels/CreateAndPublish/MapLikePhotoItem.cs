using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class MapLikePhotoItem : MonoBehaviour
{
    public RemoteImageBehaviour PhotoImg;

    public CButton Btn;

    public Text NameText;

    public CButton LikeBtn;

    public Text LikeNumText;

    public GameObject LikedIcon;

    private AlbumPhotoInfo _data;

    // Cover 显示：按“盒子”尺寸等比铺满（可裁剪不留白）
    private Vector2 _photoBoxSize;
    private bool _photoBoxSizeCached;

    void Awake()
    {
        Btn.onClick.AddListener(OnBtn);
        LikeBtn.onClick.AddListener(OnLikeBtn);
        if (PhotoImg == null)
        {
            PhotoImg = GetComponentInChildren<RemoteImageBehaviour>(true);
        }
        CachePhotoBoxSize();
    }

    void OnBtn()
    {
        //var pack = CameraImgDataUtils.BuildCameraImagePackFromAlbumPhotoInfo(_data);
        if (_data == null) return;
        UIManager.Inst.OpenPanel(PanelId.MapPhotoShowPanel, _data);
    }

    void OnLikeBtn()
    {
        var albumItem = _data?.albumItem;
        if (albumItem == null || string.IsNullOrEmpty(albumItem.id))
        {
            TipPanel.ShowToast("图片数据异常");
            return;
        }

        if (_data.interactInfo == null)
        {
            _data.interactInfo = new InteractInfo();
        }

        bool isLiked = _data.interactInfo.liked == 1;
        var reqLikeType = isLiked ? UGCCommonReq.LikeType.UnLike : UGCCommonReq.LikeType.Like;

        if (LikeBtn != null) LikeBtn.interactable = false;
        UGCCommonReq.Inst.UGCLikeReq(albumItem.id, reqLikeType, success =>
        {
            if (LikeBtn != null) LikeBtn.interactable = true;
            if (!success) return;

            if (isLiked)
            {
                _data.interactInfo.likeAmount--;
                if (_data.interactInfo.likeAmount < 0) _data.interactInfo.likeAmount = 0;
                _data.interactInfo.liked = 0;
            }
            else
            {
                _data.interactInfo.likeAmount++;
                _data.interactInfo.liked = 1;
            }

            RefreshLikeUI();
        });
    }

    public void SetData(AlbumPhotoInfo data, Action<AlbumPhotoInfo> action, int idx)
    {
        _data = data;
        
        var coverUrl = data?.albumItem?.cover;
        if (string.IsNullOrEmpty(coverUrl))
        {
            coverUrl = data?.albumItem?.coverFull;
        }

        if (PhotoImg != null)
        {
            if (string.IsNullOrEmpty(coverUrl))
            {
                PhotoImg.gameObject.SetActive(false);
            }
            else
            {
                PhotoImg.gameObject.SetActive(true);
                PhotoImg.Load(coverUrl, true, (fromCache, success) =>
                {
                    if (!success && PhotoImg != null)
                    {
                        PhotoImg.gameObject.SetActive(false);
                        return;
                    }
                    TryApplyPhotoCoverFill();
                });
            }
        }

        if (NameText != null)
        {
            NameText.text = data?.creator?.nickname ?? string.Empty;
        }

        RefreshLikeUI();
    }

    private void RefreshLikeUI()
    {
        bool liked = _data != null && _data.interactInfo != null && _data.interactInfo.liked == 1;

        if (LikedIcon != null)
        {
            LikedIcon.SetActive(liked);
        }

        if (LikeNumText != null)
        {
            LikeNumText.text = (_data?.interactInfo?.likeAmount ?? 0).ToString();
        }
    }

    private void CachePhotoBoxSize()
    {
        if (_photoBoxSizeCached) return;
        if (PhotoImg == null || PhotoImg.RawImage == null) return;
        if (CameraImgDataUtils.TryGetRawImageCoverBoxSize(PhotoImg.RawImage, out var boxSize))
        {
            _photoBoxSize = boxSize;
            _photoBoxSizeCached = true;
        }
    }

    private void TryApplyPhotoCoverFill()
    {
        if (PhotoImg == null || PhotoImg.RawImage == null) return;
        var raw = PhotoImg.RawImage;
        if (raw.texture == null) return;

        CachePhotoBoxSize();
        if (!_photoBoxSizeCached) return;
        CameraImgDataUtils.TryApplyRawImageCover(raw, raw.texture.width, raw.texture.height, _photoBoxSize, center: true);
    }

}
