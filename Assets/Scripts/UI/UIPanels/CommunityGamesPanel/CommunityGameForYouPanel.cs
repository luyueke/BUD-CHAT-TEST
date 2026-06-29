using System.Collections.Generic;
using Network;
using Network.Http;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using Game.PublicSever;

namespace Game.CommunityGame
{
    public class CommunityGameForYouPanel : MonoBehaviour
    {
        private Transform _sectionContent;
        private GameObject _sectionItemPrefab;
        private GameObject _emptyGo;
        private BaseSectionInfoPanel _sectionInfoPanel; 
        private BaseSectionInfoPanel _sectionInfoCollectPanel;
        private List<CommunitySectionItem> _sectionItems = new List<CommunitySectionItem>();
        private string _curSectionId = "null";
        private string _collectSectionId = "";
        private bool _pendingShowCollect = false;
        private bool _isShowingCollect = false;
        private CButton _btnJoinFriend;
        private CButton _btnPublicSever;

        private void Start()
        {
            BindUI();
            // 若 ShowCollectPanel 在 Start 之前被调用（CommunityPanel 首次 SetActive），不能覆盖已有状态
            if (!_isShowingCollect)
                _sectionInfoCollectPanel.gameObject.SetActive(false);
            //初始化Section栏目
            InitSectionPanel();



            _btnJoinFriend.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<JoinFriendRoomPanel>(PanelId.JoinFriendRoomPanel);
            });
            _btnPublicSever.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanel<PublicSeverPanel>(PanelId.PublicSeverPanel);
            });
        }

        private void BindUI()
        {
            _emptyGo = GameObjectEx.FindChildByName(this.transform, "CommunityForyouEmpty").gameObject;
            _sectionContent = GameObjectEx.FindChildByName(this.transform, "SectionContent");
            _sectionItemPrefab = GameObjectEx.FindChildByName(this.transform, "SectionItemPrefab").gameObject;
            _sectionInfoPanel =  GameObjectEx.FindChildByName(this.transform, "SectionInfoPanel").GetComponent<BaseSectionInfoPanel>();
            _sectionInfoCollectPanel = GameObjectEx.FindChildByName(this.transform, "SectionInfoCollectPanel").GetComponent<BaseSectionInfoPanel>();

            _btnJoinFriend = GameObjectEx.FindChildByName(this.transform, "JoinFriendBtn").GetComponent<CButton>();
            _btnPublicSever = GameObjectEx.FindChildByName(this.transform, "PublicServerBtn").GetComponent<CButton>();
        }

        private void InitSectionPanel()
        {
            JObject req = new JObject()
            {
                ["ugcType"] = 1,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionList, HttpMethod.GET, JsonConvert.SerializeObject(req), OnGetSectionSuccess, OnGetSectionFail);
        }

        private void OnGetSectionSuccess(string content)
        {
            LoggerUtils.Log("OnGetSectionSuccess content = ", content);
            SectionListRsp sectionListRsp = JsonConvert.DeserializeObject<SectionListRsp>(content);
            if (sectionListRsp == null || sectionListRsp.list == null || sectionListRsp.list.Count == 0)
                return;

            for (int i = 0; i < sectionListRsp.list.Count; i++)
            {
                var curData = sectionListRsp.list[i];
                if (curData.sectionName == "收藏")
                {
                    // 收藏 section 由外层 ShowCollectTog 接管，不在 _sectionContent 里生成 toggle
                    _collectSectionId = curData.sectionId;
                    if (_pendingShowCollect)
                    {
                        _pendingShowCollect = false;
                        _sectionInfoCollectPanel.gameObject.SetActive(true);
                        _sectionInfoCollectPanel.OnSelectSection(_collectSectionId, 0);
                    }
                    continue;
                }
                var sectionItemItem = GameObject.Instantiate(_sectionItemPrefab, _sectionContent).GetComponent<CommunitySectionItem>();
                sectionItemItem.InitItem(curData, _sectionItems.Count, OnSectionItemClick);
                _sectionItems.Add(sectionItemItem);
                sectionItemItem.gameObject.SetActive(true);
            }
            if (_sectionItems.Count > 0 && !_isShowingCollect)
                _sectionItems[0].Tog.isOn = true;
            _sectionItemPrefab.gameObject.SetActive(false);
        }

        public void ShowCollectPanel(bool show)
        {
            BindUI();
            _isShowingCollect = show;
            if (show)
            {
                _sectionInfoPanel.gameObject.SetActive(false);
                _sectionContent.gameObject.SetActive(false);
                _sectionInfoCollectPanel.gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(_collectSectionId))
                {
                    _sectionInfoCollectPanel.OnSelectSection(_collectSectionId, 0);
                }
                else
                {
                    _pendingShowCollect = true;
                }
            }
            else
            {
                _sectionInfoCollectPanel.gameObject.SetActive(false);
                _sectionContent.gameObject.SetActive(true);
                // item 可能是在 _sectionContent inactive 时创建的，ContentSizeFitter 计算不准，激活后补算尺寸
                foreach (var item in _sectionItems)
                    item.RefreshLayout();
                _sectionInfoPanel.gameObject.SetActive(true);
                // 收藏模式下 section 数据加载被推迟，这里补触发
                // 先 SetIsOnWithoutNotify(false) 保证 isOn = true 一定能触发回调和视觉选中
                if (_curSectionId == "null" && _sectionItems.Count > 0)
                {
                    _sectionItems[0].Tog.SetIsOnWithoutNotify(false);
                    _sectionItems[0].Tog.isOn = true;
                }
            }
        }
        
        private void OnGetSectionFail(string error)
        {
            LoggerUtils.LogError("OnGetSection error = " + error);
        }

        private void OnSectionItemClick(string sectionId)
        {
            ShowCurRightPanel(sectionId);
        }

        private void ShowCurRightPanel(string sectionId)
        {
            if (_curSectionId == sectionId)
                return;
            
            _sectionInfoPanel.gameObject.SetActive(false);
            //_emptyGo.SetActive(true);
            _curSectionId = sectionId;
            _sectionInfoPanel.OnSelectSection(_curSectionId, 0,OnGetDatas);
        }

        private void OnGetDatas()
        {
            _emptyGo.SetActive(false);
            if (!_isShowingCollect)
                _sectionInfoPanel.gameObject.SetActive(true);
        }
    }
}
