using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel 
{
    public class PhotoCard : BaseCard
    {
        [SerializeField] private CButton AlbunBtn;
        [SerializeField] private Transform NoneTips;
        public CameraOpenPhotoAdpter Adpter;
        private readonly List<AlbumPhotoInfo> datas = new List<AlbumPhotoInfo>();
        private string _curUid;
        public override void OnCreate(ProfilePanel profilePanel)
        {
            base.OnCreate(profilePanel);
            cardBgType = ProfileCardBgType.Bg3;
            Adpter.OnItemSelected = OnItemSelected;
            if (Adpter.Data == null)
            {
                Adpter.Data = new LazyDataHelper<AlbumPhotoInfo>(Adpter, GetInfo);
            }
            AlbunBtn.onClick.AddListener(OnAlbunBtnClick);
            TryInitAdapter();

            MessageHelper.AddListener<int>(MessageName.OnAlbumPhotoDataChanged, OnAlbumPhotoDataChanged);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<int>(MessageName.OnAlbumPhotoDataChanged, OnAlbumPhotoDataChanged);
        }

        public bool RefreshWithData(string uid, List<AlbumPhotoInfo> photoList)
        {
            _curUid = uid;
            AlbunBtn.gameObject.SetActive(_curUid == AccountDataManager.Inst.Uid);
            if (photoList == null || photoList.Count == 0)
            {
                NoneTips.gameObject.SetActive(true);
                datas.Clear();
                Adpter.Data?.ResetItems(0);
                Adpter.Refresh();

                
                return false;
            }
            NoneTips.gameObject.SetActive(false);
            //Show(true);

            // PhotoCard 在 InitUI 时默认被隐藏（SetActive=false），OSA 适配器可能还未初始化，
            // 这里确保在显示后再 Init，避免 ResetItems 抛异常
            TryInitAdapter();

            datas.Clear();
            datas.AddRange(photoList);
            Adpter.Data.ResetItems(datas.Count);
            Adpter.Refresh();
            return true;
        }

        private void OnAlbumPhotoDataChanged(int opt)
        {
            // 仅在个人主页打开且 PhotoCard 存在 uid 时刷新，避免无意义请求
            if (string.IsNullOrEmpty(_curUid)) return;
            if (_profilePanel == null || !_profilePanel || !_profilePanel.gameObject.activeInHierarchy) return;

            switch (opt)
            {
                case 0: // 上传
                case 1: // 删除
                case 2: // 更新
                case 3: // 公开状态更新
                    RequestPhotoList(_curUid);
                    break;
            }
        }

        private void RequestPhotoList(string uid)
        {
            AlbumRequestCtrl.Inst.RequestRemoteAlbumAllPages(uid, 0, 1, pageRes =>
            {
                if (!_profilePanel || !_profilePanel.gameObject.activeInHierarchy) return;

                if (pageRes?.list == null || pageRes.list.Count == 0)
                {
                    RefreshWithData(uid, null);
                    return;
                }
            //    Debug.LogError("pageRes: " + JsonConvert.SerializeObject(pageRes));
                var photoList = new List<AlbumPhotoInfo>();
                for (int i = 0; i < pageRes.list.Count; i++)
                {
                    var item = pageRes.list[i];
                    if (item?.albumItem == null) continue;
                    // 个人主页只展示公开图片（isPublic==0）
                    if (item.albumItem.isPublic == 1)
                    {
                        photoList.Add(item);
                    }
                }

                RefreshWithData(uid, photoList);
            }, error =>
            {
                LoggerUtils.LogError($"[PhotoCard] RequestRemoteAlbumAllPages failed: {error}");
            });
        }

        private void TryInitAdapter()
        {
            if (Adpter == null) return;
            if (Adpter.IsInitialized) return;
            if (!Adpter.gameObject.activeInHierarchy) return;
            Adpter.Init();
        }

        private AlbumPhotoInfo GetInfo(int index)
        {
            return datas[index];
        }
        private void OnItemSelected(AlbumPhotoInfo info)
        {
            UIManager.Inst.OpenPanel<PhotoImgShowPanel>(PanelId.PhotoImgShowPanel, info, datas, _curUid);
        }
        private void OnAlbunBtnClick()
        {
            UIManager.Inst.OpenPanel<AlbumPanel>(PanelId.AlbumPanel);
        }
    }
}

