using System;
using System.Collections;
using System.Collections.Generic;
using Basic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Avatar;
using Game.Store;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 选择穿搭界面：展示当前穿搭 + 已保存的设子（GridLayoutGroup + 下拉加载更多）。
    /// 用户选择后结合前序步骤的音色，调用 IncubationCabinDraftBox.CreateRole 创建角色。
    ///
    /// 预制连接说明：
    ///   chatChoiceAvatarItem      → item 节点（ChatChoiceAvatarItem 组件）
    ///   chatChoiceAvatarItemParent → Content 节点
    ///   pullToRefreshBehaviour    → Scroll View 节点上的 PullToRefreshBehaviour（需手动添加）
    /// </summary>
    public class ChatChoiceAvatarCom : MonoBehaviour
    {
        public Button hideBtn;
        public Button useBtn;
        public Button SkipBtn;

        public ChatChoiceAvatarItem chatChoiceAvatarItem;    // 预制 item（已在预制中连好）
        public GameObject chatChoiceAvatarItemParent;        // Content 节点（已在预制中连好）

        /// <summary>挂在 Scroll View 节点上的 PullToRefreshBehaviour（需在编辑器中添加并连接）</summary>
        public PullToRefreshBehaviour pullToRefreshBehaviour;

        // ── 运行时状态 ──
        private CabinChatCreateBotProfileData _botProfile;
        private string _toneId;
        private Action _onBack;

        private int _selectedIndex = 0;
        private readonly List<ChatChoiceAvatarItem> _items = new();
        // index 0 = 当前穿搭（null），其余对应 OcInfo
        private readonly List<OcInfo> _ocInfos = new();

        private HttpPageRequestHandle<OcListPageUseData> _dataHandle;

        private void Awake()
        {
            hideBtn?.onClick.AddListener(OnHideClick);
            useBtn?.onClick.AddListener(OnUseClick);
            SkipBtn?.onClick.AddListener(OnSkipClick);

            // 下拉加载下一页
            if (pullToRefreshBehaviour != null)
            {
                pullToRefreshBehaviour.OnRefreshWithSlideUp.RemoveAllListeners();
                pullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(LoadNextPage);
            }
        }

        // ── 公共入口 ──────────────────────────────────────────

        public void Show(CabinChatCreateBotProfileData botProfile, string toneId, Action onBack)
        {
            _botProfile = botProfile;
            _toneId = toneId;
            _onBack = onBack;
            _selectedIndex = 0;

            gameObject.SetActive(true);
            InitDataHandle();
        }

        // ── 数据加载 ──────────────────────────────────────────

        private void InitDataHandle()
        {
            string paramStr = JsonConvert.SerializeObject(new JObject { ["skinType"] = 0 });
            _dataHandle = new HttpPageRequestHandle<OcListPageUseData>(
                HttpUrlDefine.ocList, paramStr, Network.Http.HttpMethod.GET);

            _dataHandle.AddSuccessAction(OnPullData);
            _dataHandle.AddFailAction(_ => pullToRefreshBehaviour?.HideGizmo());
            _dataHandle.Reset();
            _dataHandle.Start();
        }

        private void LoadNextPage()
        {
            _dataHandle?.Next();
        }

        private void OnPullData(OcListPageUseData data)
        {
            pullToRefreshBehaviour?.HideGizmo();

            // if (data.isFirst)
            // {
            //     ClearItems();
            //     // index 0 = 当前穿搭哨兵
            //     _ocInfos.Add(null);
            //     SpawnItem(null);
            // }

            if (data.list != null)
            {
                foreach (var oc in data.list)
                {
                    if (oc?.ocInfo != null && !string.IsNullOrEmpty(oc.ocInfo.avatarJson) && !string.IsNullOrEmpty(oc.ocInfo.ocCover))
                    {
                        _ocInfos.Add(oc.ocInfo);
                        SpawnItem(oc.ocInfo);
                    }
                }
            }

            // 首次加载默认选中 index 0
            if (data.isFirst && _items.Count > 0)
                SelectItem(0);
        }

        // ── Item 实例化 ──────────────────────────────────────

        private void SpawnItem(OcInfo ocInfo)
        {
            chatChoiceAvatarItem.gameObject.SetActive(false);
            var go = Instantiate(chatChoiceAvatarItem.gameObject, chatChoiceAvatarItemParent.transform);
            go.SetActive(true);
            var item = go.GetComponent<ChatChoiceAvatarItem>();
            int idx = _items.Count;
            item.SetData(ocInfo?.ocCover, () => SelectItem(idx));
            _items.Add(item);
        }

        private void ClearItems()
        {
            foreach (var item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();
            _ocInfos.Clear();
        }

        // ── 选中 ──────────────────────────────────────────────

        private void SelectItem(int index)
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i].SetSelected(i == index);
            _selectedIndex = index;
        }

        private string GetSelectedAvatarJson()
        {
            if (_selectedIndex > 0 && _selectedIndex < _ocInfos.Count
                && !string.IsNullOrEmpty(_ocInfos[_selectedIndex]?.avatarJson))
                return _ocInfos[_selectedIndex].avatarJson;
            return GetCurrentAvatarJson();
        }

        private static string GetCurrentAvatarJson()
            => CharacterData.SerializeObject(AccountDataManager.Inst.UserInfo.avatarInfo as CharacterData);

        // ── 按钮回调 ──────────────────────────────────────────

        private void OnHideClick()
        {
            gameObject.SetActive(false);
            _onBack?.Invoke();
        }

        private void OnUseClick()
        {
            DoCreateRole(_toneId, GetSelectedAvatarJson());
        }

        private void OnSkipClick()
        {
            DoCreateRole(_toneId, GetCurrentAvatarJson());
        }

        // ── 创建角色 ──────────────────────────────────────────

        private void DoCreateRole(string toneId, string avatarJson)
        {
            if (_botProfile == null) return;
            _botProfile.toneId = toneId;

            ScreenOrientationHelper.Inst.Switch(ScreenOrientation.LandscapeLeft, () =>
            {
                UIManager.Inst.ClosePanel(PanelId.AICompanionChatPanel);
                CoroutineManager.Inst.StartCoroutine(WaitAndCreate(avatarJson));
            });
        }

        private IEnumerator WaitAndCreate(string avatarJson)
        {
            float elapsed = 0f;
            const float timeout = 2f;
            while (Screen.width < Screen.height && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return null;

            var panel = UIManager.Inst.FindPanel(PanelId.IncubationCabinDraftBox);
            (panel as IncubationCabinDraftBox).CreateRole(_botProfile, avatarJson);
        }
    }
}
