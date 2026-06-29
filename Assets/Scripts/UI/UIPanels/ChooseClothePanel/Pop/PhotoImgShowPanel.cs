using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.Base.Common;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PhotoImgShowPanel : BasePanel<PhotoImgShowPanel>
{
    [SerializeField] private RemoteImageBehaviour PhotoImage;
    [SerializeField] private CButton MaskBtn;
    [SerializeField] private SuperTextMesh PosName;
    [SerializeField] private CButton GoBtn;
    [SerializeField] private CButton LikeBtn;
    [SerializeField] private Text LikeNum;
    [SerializeField] private GameObject LikedIcon;
    [SerializeField] private CButton RelayBtn;
    [SerializeField] private CButton JubaoBtn;
    [SerializeField] private CButton DelectBtn;
    [SerializeField] private CButton LeftBtn;
    [SerializeField] private CButton RightBtn;
    [SerializeField] private CButton BigGlassBtn;
    [SerializeField] private Text CreatTime;

    private AlbumPhotoInfo _pack;

    private List<AlbumPhotoInfo> _photoList = new List<AlbumPhotoInfo>();
    private int curSelectIndex = 0;
    private string _curUid;

    // 以 PhotoImage（RawImage）原本大小为基准框，做 Cover（不留白）
    private Vector2 _photoImageOriginalSize;
    private bool _photoImageOriginalSizeCached;

    public override void OnCreate()
    {
        base.OnCreate();
        MaskBtn.onClick.AddListener(CloseSelf);
        GoBtn.onClick.AddListener(OnGoBtnClick);
        LikeBtn.onClick.AddListener(OnLikeBtnClick);
        RelayBtn.onClick.AddListener(OnRelayBtnClick);
        JubaoBtn.onClick.AddListener(OnJubaoBtnClick);
        DelectBtn.onClick.AddListener(OnDelectBtnClick);
        LeftBtn.onClick.AddListener(OnLeftBtnClick);
        RightBtn.onClick.AddListener(OnRightBtnClick);
        BigGlassBtn.onClick.AddListener(OnBigGlassBtnClick);
        CacheOriginalSize(PhotoImage != null ? PhotoImage.RawImage : null, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);

    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _pack = args[0] as AlbumPhotoInfo;
        _photoList = args[1] as List<AlbumPhotoInfo> ?? new List<AlbumPhotoInfo>();
        _curUid = args[2] as string;

        var packId = _pack?.albumItem?.id;
        if (!string.IsNullOrEmpty(packId) && _photoList != null && _photoList.Count > 0)
        {
            for (int i = 0; i < _photoList.Count; i++)
            {
                if (_photoList[i]?.albumItem?.id == packId)
                {
                    curSelectIndex = i;
                    break;
                }
            }
        }

        LoadPhoto();
    }


    private void LoadPhoto()
    {
        var coverUrl = _pack?.albumItem?.coverFull;
        if (string.IsNullOrEmpty(coverUrl))
        {
            coverUrl = _pack?.albumItem?.cover;
        }
        
        if (PhotoImage != null)
        {
            if (string.IsNullOrEmpty(coverUrl))
            {
                PhotoImage.gameObject.SetActive(false);
            }
            else
            {
                PhotoImage.gameObject.SetActive(true);
                PhotoImage.Load(coverUrl, true, (fromCache, success) =>
                {
                    if (!success && PhotoImage != null)
                    {
                        PhotoImage.gameObject.SetActive(false);
                        return;
                    }

                    var raw = PhotoImage != null ? PhotoImage.RawImage : null;
                    if (raw != null && raw.texture != null)
                    {
                        ApplyCoverSizeByOriginal(raw, raw.texture, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);
                    }
                });
            }
        }

        if (_pack.interactInfo != null)
        {
            LikeNum.text = _pack.interactInfo.likeAmount.ToString();
            LikedIcon.SetActive(_pack.interactInfo.liked == 1);
        }
        var ct = _pack?.albumItem?.createTime == 0 ? (long)_pack?.albumItem?.updateTime : (long)_pack?.albumItem?.createTime;
        if (ct <= 0)
        {
            CreatTime.text = string.Empty;
        }
        else
        {
            // 项目内统一工具：DataUtil.GetTimeStrByStampFormat 传入毫秒
            double ms = ct > 9999999999L ? ct : ct * 1000d;
            CreatTime.text = DataUtil.GetTimeStrByStampFormat(ms, "yyyy-MM-dd");
        }
        PosName.SetText(_pack.albumItem.locationInfo?.locationName);

        DelectBtn.gameObject.SetActive(_curUid == AccountDataManager.Inst.Uid);
        RelayBtn.gameObject.SetActive(_curUid == AccountDataManager.Inst.Uid);
    }

    private static void ApplyCoverSizeByOriginal(RawImage raw, Texture tex, ref Vector2 cachedBoxSize, ref bool cached)
    {
        if (raw == null || tex == null) return;
        if (tex.width <= 0 || tex.height <= 0) return;

        var rt = raw.rectTransform;
        if (rt == null) return;

        if (!cached)
        {
            CacheOriginalSize(raw, ref cachedBoxSize, ref cached);
        }

        var box = cached && cachedBoxSize.x > 0f && cachedBoxSize.y > 0f
            ? cachedBoxSize
            : (rt.sizeDelta.x > 0f && rt.sizeDelta.y > 0f ? rt.sizeDelta : rt.rect.size);

        if (box.x <= 0f || box.y <= 0f) return;
        // 保持旧行为：使用现有 box 计算，不强制居中（不改 anchoredPosition）
        CameraImgDataUtils.TryApplyRawImageCover(raw, tex.width, tex.height, box, center: false);
    }

    private static void CacheOriginalSize(RawImage raw, ref Vector2 size, ref bool cached)
    {
        if (raw == null)
        {
            cached = false;
            size = default;
            return;
        }

        var rt = raw.rectTransform;
        if (rt == null)
        {
            cached = false;
            size = default;
            return;
        }

        // 优先使用设计时 sizeDelta（更稳定）
        var s = rt.sizeDelta;
        if (s.x <= 0f || s.y <= 0f)
        {
            s = rt.rect.size;
        }

        size = s;
        cached = size.x > 0f && size.y > 0f;
    }

    private void OnGoBtnClick()
    {
        if (_pack == null || _pack.albumItem == null || _pack.albumItem.locationInfo == null || string.IsNullOrEmpty(_pack.albumItem.locationInfo.mapId))
        {
            TipPanel.ShowToast("图片数据异常");
            return;
        }
        UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, _pack.albumItem.locationInfo.mapId);
    }

    private void OnLikeBtnClick()
    {
        if (_pack == null || _pack.albumItem == null || string.IsNullOrEmpty(_pack.albumItem.id))
        {
            TipPanel.ShowToast("图片数据异常");
            return;
        }

        if (_pack.interactInfo == null)
        {
            _pack.interactInfo = new InteractInfo();
        }

        bool isLiked = _pack.interactInfo.liked == 1;
        var reqLikeType = isLiked ? UGCCommonReq.LikeType.UnLike : UGCCommonReq.LikeType.Like;
        UGCCommonReq.Inst.UGCLikeReq(_pack.albumItem.id, reqLikeType, (success) =>
        {
            if(success)
            {
                if (isLiked)
                {
                    _pack.interactInfo.likeAmount--;
                    if (_pack.interactInfo.likeAmount < 0) _pack.interactInfo.likeAmount = 0;
                    _pack.interactInfo.liked = 0;
                }
                else
                {
                    _pack.interactInfo.likeAmount++;
                    _pack.interactInfo.liked = 1;
                }
                LoadPhoto();
            }
        });
    }
    private void OnRelayBtnClick()
    {
        var sharePack = BuildCameraImagePackForShare(_pack);
        if (sharePack == null)
        {
            TipPanel.ShowToast("图片数据异常，无法分享");
            return;
        }
        UIManager.Inst.OpenPanel(PanelId.PhotoSharePanel, sharePack);
    }

    private void OnDelectBtnClick()
    {
        if (_pack == null || _pack.albumItem == null || string.IsNullOrEmpty(_pack.albumItem.id))
        {
            TipPanel.ShowToast("图片数据异常");
            return;
        }

        var commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("提示", "是否删除上传的云端照片？", "确定", "取消");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            // 本地先删除：清理本地索引与加密文件（若存在）
            CameraImgDataUtils.Inst.DeleteByAlbumId(_pack.albumItem.id);
            // 云端删除
            AlbumRequestCtrl.Inst.PublicPhotoInfo(new List<CameraAlbumInfo>(){_pack.albumItem}, 1, (list, opt) =>
            {
                CameraImgDataUtils.Inst.DelectPhotoImg(list,opt);
            });
            CloseSelf();
        }, null);
    }
    private void OnLeftBtnClick()
    {
        if (_photoList == null || _photoList.Count <= 0)
        {
            TipPanel.ShowToast("暂无照片");
            return;
        }

        // 第一张：提示，不切换
        if (curSelectIndex <= 0)
        {
            TipPanel.ShowToast("已经是第一张了");
            return;
        }

        curSelectIndex--;
        _pack = _photoList[curSelectIndex];
        LoadPhoto();
    }
    private void OnRightBtnClick()
    {
        if (_photoList == null || _photoList.Count <= 0)
        {
            TipPanel.ShowToast("暂无照片");
            return;
        }

        // 最后一张：提示，不切换
        if (curSelectIndex >= _photoList.Count - 1)
        {
            TipPanel.ShowToast("已经是最后一张了");
            return;
        }

        curSelectIndex++;
        _pack = _photoList[curSelectIndex];
        LoadPhoto();
    }

    private void OnJubaoBtnClick()
    {
        var _reportReq = new ErrReportReq()
        {
            Uid = AccountDataManager.Inst.Uid,
            bizId = _pack.albumItem.id,
            scenesType = 14,
        };
        UIManager.Inst.OpenPanel(PanelId.ReportAssetPanel, _reportReq);
    }

    private CameraImagePack BuildCameraImagePackForShare(AlbumPhotoInfo info)
    {
        return CameraImgDataUtils.BuildCameraImagePackFromAlbumPhotoInfo(info);
    }

    private void OnBigGlassBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.BigPhotoImgPanel, BuildCameraImagePackForShare(_pack));
    }
}
