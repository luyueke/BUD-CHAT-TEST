using System.Collections.Generic;
using GameData.Base;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileMap : MonoBehaviour
    {
        public UISegmentView uiSegmentView;
        public ProfileMapEntry gameEntry;
        public ProfileMapDataManager profileMapDataManager;
        public Button hideOrLike;
        public Image hideOrLikeIcon;
        public Text hideOrLikeText;
        public Sprite isSelect;
        public Sprite unSelect;
        public Text emptyText;
        public GameObject listView;

        private bool isInit = false;

        private bool isHideSelect = false;

        private string _currentUid;

        private Category _currentCategory;
        private SubCategory _currentSubCategory;

        /// <summary>
        /// 是否是自己的主页，用于判断是否显示隐藏点赞或者拥有
        /// </summary>
        private bool isMe = false;

        public void InitUI(Category category, string uid)
        {
            if (isInit)
            {
                return;
            }

            isInit = true;
            _currentUid = uid;
            _currentCategory = category;
            isMe = uid == AccountDataManager.Inst.Uid;
            uiSegmentView.selectTextColor = Color.black;
            uiSegmentView.normalTextColor = Color.white;
            if (category == Category.Map)
            {
                List<string> lists = new List<string>
                {
                    "发布", "点赞", "收藏", "游玩"
                };
                uiSegmentView.SetSegementData(lists, defaultIndex: 0, selectIndex =>
                {
                    hideOrLike.gameObject.SetActive(false);
                    switch (selectIndex)
                    {
                        case 0:
                            profileMapDataManager.SetData(category, SubCategory.Published);
                            _currentSubCategory = SubCategory.Published;
                            break;
                        case 1:
                            profileMapDataManager.SetData(category, SubCategory.Liked);
                            hideOrLike.gameObject.SetActive(isMe);
                            hideOrLikeText.text = "在公共主页隐藏点赞";
                            _currentSubCategory = SubCategory.Liked;
                            break;
                        case 2:
                            profileMapDataManager.SetData(category, SubCategory.Collected);
                            _currentSubCategory = SubCategory.Collected;
                            break;
                        case 3:
                            profileMapDataManager.SetData(category, SubCategory.Played);
                            _currentSubCategory = SubCategory.Played;
                            break;
                    }

                    RequestDataList();
                });
            }
            else
            {
                List<string> lists = new List<string>
                {
                    "发布", "点赞", "拥有"
                };
                uiSegmentView.SetSegementData(lists, defaultIndex: 0, selectIndex =>
                {
                    hideOrLike.gameObject.SetActive(false);
                    switch (selectIndex)
                    {
                        case 0:
                            profileMapDataManager.SetData(category, SubCategory.Published);
                            _currentSubCategory = SubCategory.Published;
                            break;
                        case 1:
                            profileMapDataManager.SetData(category, SubCategory.Liked);
                            hideOrLike.gameObject.SetActive(isMe);
                            hideOrLikeText.text = "在公共主页隐藏点赞";
                            _currentSubCategory = SubCategory.Liked;
                            break;
                        case 2:
                            profileMapDataManager.SetData(category, SubCategory.Owned);
                            hideOrLike.gameObject.SetActive(isMe);
                            hideOrLikeText.text = "在公共主页隐藏拥有";
                            _currentSubCategory = SubCategory.Owned;
                            break;
                    }

                    RequestDataList();
                });
            }


            // gameEntry.SetLoader(profileMapDataManager);
            // gameEntry.SetActions(GameLoopGridViewItemTrigger, IsEmptyAction);

            profileMapDataManager.SetData(category, SubCategory.Published);
            RequestDataList();

            hideOrLike.onClick.AddListener(HideLikeOrOwn);
        }

        /// <summary>
        /// 隐藏点赞或者拥有
        /// </summary>
        private void HideLikeOrOwn()
        {
            if (isHideSelect)
            {
                isHideSelect = false;
                SetUpHideLikeOrOwn();
                RequestHideLikeOrOwn();
            }
            else
            {
                isHideSelect = true;
                SetUpHideLikeOrOwn();
                RequestHideLikeOrOwn();
            }
        }

        private void RequestHideLikeOrOwn()
        {
            int permissionType = isHideSelect ? 1 : 0;
            SetPermissionReq setPermissionReq = new SetPermissionReq
            {
                pageType = 1,
                permissionType = permissionType,
                interactType = ProfileMapUtils.GetProfileInteractType(_currentCategory, _currentSubCategory)
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setPermission,
                HttpMethod.POST,
                JsonConvert.SerializeObject(setPermissionReq),
                SetPermissionSuccess,
                SetPermissionFail);
        }

        private void SetPermissionSuccess(string message)
        {
        }

        private void SetPermissionFail(string failMessage)
        {
        }

        private void SetUpHideLikeOrOwn()
        {
            hideOrLikeIcon.sprite = isHideSelect ? isSelect : unSelect;
        }

        private void RequestDataList()
        {
            // gameEntry.GetFirstPageDatas(GetPageDatas, _currentUid);
        }

        private void SetIsHideSelect(bool isPrivate)
        {
            if (_currentSubCategory == SubCategory.Liked || _currentSubCategory == SubCategory.Owned)
            {
                isHideSelect = isPrivate;
                SetUpHideLikeOrOwn();
            }
        }

        private void GetPageDatas(List<ResInfo> infos, bool isPrivate)
        {
            if (UpdateMapDataManager.Inst.isRequestingData)
            {
                return;
            }

            SetIsHideSelect(isPrivate);
            if (isPrivate && !isMe)
            {
                //私有页面
                emptyText.text = "该用户设置" + ProfileMapUtils.GetSubCategoryName(_currentSubCategory) + "内容不可见";
                emptyText.gameObject.SetActive(true);
                listView.gameObject.SetActive(false);
                return;
            }

            if (infos == null || infos.Count == 0)
            {
                //展示空白占位UI
                emptyText.text = "还没有" + ProfileMapUtils.GetSubCategoryName(_currentSubCategory) + "的作品";
                emptyText.gameObject.SetActive(true);
                listView.gameObject.SetActive(false);
            }
            else
            {
                //隐藏空白占位UI
                emptyText.gameObject.SetActive(false);
                listView.gameObject.SetActive(true);
            }
        }

    }
}