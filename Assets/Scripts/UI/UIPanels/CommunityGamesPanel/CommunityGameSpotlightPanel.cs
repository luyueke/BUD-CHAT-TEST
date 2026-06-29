using Game.PublicSever;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CommunityGame
{
    public class CommunityGameSpotlightPanel : MonoBehaviour
    {
        private RectTransform _bottomViewContent;
        private ScrollRect _bottomViewSR;
        //private CommunityGamesSpotlightLeftItem _leftItemPrefab;
        private CommunityGamesSpotlightRightItem _rightItemPrefab;
        // private AIYandereGamesItem _aiRightItemPrefab;
        
        //private Transform _leftViewContent;

        private CButton _btnJoinFriend;
        private CButton _btnPublicSever;
        
        
        private List<StartEndData> _startEndDatas = new List<StartEndData>();
        private List<CommunityGamesSpotlightLeftItem> _communityGamesSpotlightLeftItems = new List<CommunityGamesSpotlightLeftItem>();
        private List<CommunityGamesSpotlightRightItem> _communityGamesSpotlightRightItems = new List<CommunityGamesSpotlightRightItem>();
        private int currentIndex = -1; // Variable to keep track of the currently selected index
        private bool bo;
        private void BindUI()
        {
            _bottomViewContent = GameObjectEx.FindChildByName(this.transform, "BottomSpotlightContent").GetComponent<RectTransform>();
            _bottomViewSR = GameObjectEx.FindChildByName(this.transform, "BottomViewSV").GetComponent<ScrollRect>();
            //_leftItemPrefab = GameObjectEx.FindChildByName(this.transform, "LeftItemPrefab").GetComponent<CommunityGamesSpotlightLeftItem>();
            _rightItemPrefab = GameObjectEx.FindChildByName(this.transform, "RightItemPrefab").GetComponent<CommunityGamesSpotlightRightItem>();
            // _aiRightItemPrefab = GameObjectEx.FindChildByName(this.transform, "AIRightItemPrefab").GetComponent<AIYandereGamesItem>();
            //_leftViewContent = GameObjectEx.FindChildByName(this.transform, "LeftPanelContent");
            _btnJoinFriend = GameObjectEx.FindChildByName(this.transform, "JoinFriendBtn").GetComponent<CButton>();
            _btnPublicSever = GameObjectEx.FindChildByName(this.transform, "PublicServerBtn").GetComponent<CButton>();
            
            _btnJoinFriend.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<JoinFriendRoomPanel>(PanelId.JoinFriendRoomPanel);
            });
            _btnPublicSever.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<PublicSeverPanel>(PanelId.PublicSeverPanel);
            });
        }

        private void Awake()
        {
            BindUI();
            InitItem();
            _bottomViewSR.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void OnScrollValueChanged(Vector2 position)
        {
            if (_startEndDatas == null || _startEndDatas.Count <= 0)
            {
                return;
            }

            // Calculate the vertical position in terms of the total scrollable height
            float currentScrollPosition = 0;
            if(1 - position.y != 0)
            {
                float totalScrollableHeight = _startEndDatas.Last().end; // Assuming _startEndDatas is sorted by start value
                currentScrollPosition = (1 - position.y) * totalScrollableHeight;
            }


            // Find the index based on the current scroll position
            int index = -1;
            for (int i = 0; i < _startEndDatas.Count; i++)
            {
                if (currentScrollPosition >= _startEndDatas[i].start && currentScrollPosition < _startEndDatas[i].end)
                {
                    index = i;
                    break;
                }
            }

            // Only set the selection state if the index has changed and it's valid
            if (index != -1 && index != currentIndex)
            {
                currentIndex = index;
                SetSelectState(index);
            }
        }

        private void SetSelectState(int index)
        {
            var panel = UIManager.Inst.FindPanel<CommunityGamesPanel>(PanelId.CommunityGamesPanel);
            if (panel != null)
            {
                bo = true;
                panel.SetTog(index);
            }
            for (int i = 0; i < _communityGamesSpotlightLeftItems.Count; i++)
            {
                _communityGamesSpotlightLeftItems[i].SetSelect(i == index);
            }
        }

        private void OnDestroy()
        {
            if (_bottomViewSR != null)
            {
                // 移除监听器
                _bottomViewSR.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        //private void InitSectionList(List<OfficalRecommendItem> spotlightSectionItems)
        //{
        //    if (spotlightSectionItems == null || spotlightSectionItems.Count <= 0)
        //    {
        //        return;
        //    }
        //
        //    foreach (Transform child in _leftViewContent)
        //    {
        //        Destroy(child.gameObject);
        //    }
        //
        //    _communityGamesSpotlightLeftItems.Clear();
        //    
        //    // var aiYandereItem = Instantiate(_leftItemPrefab, _leftViewContent);
        //    // var aiItemData = new OfficalRecommendItem()
        //    // {
        //    //     sectionId = "0",
        //    //     sectionName = "公寓逃脱模拟器"
        //    // };
        //    // aiYandereItem.OnInitCreate(aiItemData, spItem =>
        //    // {
        //    //     for (int i = 0; i < _communityGamesSpotlightLeftItems.Count; i++)
        //    //     {
        //    //         _communityGamesSpotlightLeftItems[i].SetSelect(
        //    //             _communityGamesSpotlightLeftItems[i].spotlightSectionItem.sectionId ==
        //    //             spItem.sectionId);
        //    //         if (_communityGamesSpotlightLeftItems[i].spotlightSectionItem.sectionId ==
        //    //             spItem.sectionId)
        //    //         {
        //    //             SelectSection(i);
        //    //         }
        //    //     }
        //    // });
        //    //
        //    //
        //    // _communityGamesSpotlightLeftItems.Add(aiYandereItem);
        //    
        //    foreach (var spotlightSectionItem in spotlightSectionItems)
        //    {
        //        var communityGamesSpotlightLeftItem = Instantiate(_leftItemPrefab, _leftViewContent);
        //        communityGamesSpotlightLeftItem.OnInitCreate(spotlightSectionItem, spItem =>
        //        {
        //            for (int i = 0; i < _communityGamesSpotlightLeftItems.Count; i++)
        //            {
        //                _communityGamesSpotlightLeftItems[i].SetSelect(
        //                    _communityGamesSpotlightLeftItems[i].spotlightSectionItem.sectionId ==
        //                    spItem.sectionId);
        //                if (_communityGamesSpotlightLeftItems[i].spotlightSectionItem.sectionId ==
        //                    spItem.sectionId)
        //                {
        //                    SelectSection(i);
        //                }
        //            }
        //
        //        });
        //        _communityGamesSpotlightLeftItems.Add(communityGamesSpotlightLeftItem);
        //        _communityGamesSpotlightLeftItems[0].SetSelect(true);
        //    }
        //}

        private void InitSpotlightList(List<OfficalRecommendItem> spotlightSectionItems)
        {
            foreach (Transform child in _bottomViewContent)
            {
                Destroy(child.gameObject);
            }

            _communityGamesSpotlightRightItems.Clear();
            _startEndDatas.Clear();
            
            // var aiGameRightItem = Instantiate(_aiRightItemPrefab, _bottomViewContent);
            // _communityGamesSpotlightRightItems.Add(aiGameRightItem);
            foreach (var spotlightSectionItem in spotlightSectionItems)
            {
                var communityGamesSpotlightRightItem = Instantiate(_rightItemPrefab, _bottomViewContent);
                communityGamesSpotlightRightItem.OnInitCreate(spotlightSectionItem);
                _communityGamesSpotlightRightItems.Add(communityGamesSpotlightRightItem);
            }

            // 强制更新布局
            Canvas.ForceUpdateCanvases();
            if (_communityGamesSpotlightRightItems != null && _communityGamesSpotlightRightItems.Count > 0)
            {
                float prevHeight = 0f;
                for (int i = 0; i < _communityGamesSpotlightRightItems.Count; i++)
                {
                    CommunityGamesSpotlightRightItem communityGamesSpotlightRightItem =
                        _communityGamesSpotlightRightItems[i];
                    RectTransform rectTransform = communityGamesSpotlightRightItem.GetComponent<RectTransform>();
                    // 确保布局已经更新
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                    float height = rectTransform.rect.height;

                    float start = prevHeight;
                    prevHeight += height + 70; // Update prevHeight after calculating start

                    _startEndDatas.Add(new StartEndData()
                    {
                        start = start,
                        end = prevHeight
                    });
                }
            }

        }

        public void SelectSection(int index)
        {
            if (_communityGamesSpotlightRightItems != null && _communityGamesSpotlightRightItems.Count > 0)
            {
                float prevHeight = 0f;
                for (int i = 0; i < index; i++)
                {
                    if (i < _communityGamesSpotlightRightItems.Count)
                    {
                        RectTransform rectTransform = _communityGamesSpotlightRightItems[i].GetComponent<RectTransform>();
                        float height = rectTransform.rect.height;
                        prevHeight += height;
                        prevHeight += 20;
                    }
                }

                _bottomViewContent.anchoredPosition = new Vector2(0, prevHeight);
            }
        }

        //临时方案 等待接入新的ScrollView组件
        public void InitItem()
        {
            JObject req = new JObject
            {
                ["gameType"] = (int)GameType.Normal,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.gameSpotlight, HttpMethod.GET, JsonConvert.SerializeObject(req), OnGetDataSuccess,
                OnGetDataFail);
        }

        private void OnGetDataSuccess(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                OnGetDataFail("Empty Rsp");
                return;
            }

            OfficalRecommendRsp rsp = JsonConvert.DeserializeObject<OfficalRecommendRsp>(content);
            if (rsp != null)
            {
                //InitSectionList(rsp.sections);
                var panel = UIManager.Inst.FindPanel<CommunityGamesPanel>(PanelId.CommunityGamesPanel);
                if (panel != null)
                {
                    panel.RefreshTog(rsp.sections);
                }
                InitSpotlightList(rsp.sections);
            }
            else
            {
                OnGetDataFail("rsp is Null");
            }
        }
        
        private void OnGetDataFail(string error)
        {
            LoggerUtils.LogError("CommunityGameSpotlightPanel OnGetDataFail " + error);
        }
    }
}