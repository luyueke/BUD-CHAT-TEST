using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 为创建的角色匹配伙伴声音组件。
    /// 共 3 页，每页 9 个：3 个来自角色名搜索，3 个来自 3-4 标签搜索，3 个来自 1-2 标签搜索。
    /// 页内按匹配度排序：角色名 > 3-4标签 > 1-2标签。
    /// </summary>
    public class ChatVoiceCom : MonoBehaviour
    {
        public Text title_txt;              // 为【xx】匹配音色
        public Button closeBtn;
        public GameObject tagTxtTempGo;     // 标签预设，获取 Text 组件设置内容
        public GameObject tagLayoutGo;      // 标签父节点

        public List<ChatVoiceItem> chatVoiceItems; // 9 个

        public Text pageText;               // 第x/3页
        public Button changeBtn;
        public ChatBuyVoiceCom chatBuyVoiceCom; // 购买面板

        public Text text_buyBtn; //当选中的是已拥有的，显示"就是它了"。当未拥有时，显示"去购买"

        public Button jumpBtn; //跳过

        public ChatChoiceAvatarCom chatChoiceAvatarCom;
        public GameObject chatNode;


        // ── 搜索每次请求数量 ──
        private const int FetchCount = 10;
        private const int ItemsPerPage = 9;
        private const int TotalPages = 3;
        private const int SlotsPerSource = 3; // 每页每类来源占 3 个槽

        // ── 搜索状态 ──
        private string _nameKeyword = "";
        private List<string> _allTags = new();   // 3-4 标签
        private List<string> _partialTags = new(); // 1-2 标签

        private readonly List<CabinCharacterToneSearchSubData> _nameBuffer   = new();
        private readonly List<CabinCharacterToneSearchSubData> _tag34Buffer  = new();
        private readonly List<CabinCharacterToneSearchSubData> _tag12Buffer  = new();

        private string _nameCookie  = "";
        private string _tag34Cookie = "";
        private string _tag12Cookie = "";

        private bool _nameEnd  = false;
        private bool _tag34End = false;
        private bool _tag12End = false;

        private readonly HashSet<string> _shownIds = new();

        // ── 页面数据 ──
        // Item 附带匹配分：3=名称，2=3-4标签，1=1-2标签
        private readonly List<List<(CabinCharacterToneSearchSubData data, int score)>> _pages = new();

        private int _currentPage = 0;
        private int _selectedIndex = -1;
        private readonly List<GameObject> _tagObjects = new();
        private CabinChatCreateBotProfileData _botProfile;

        private void Awake()
        {
            closeBtn?.onClick.AddListener(OnCloseClick);
            changeBtn?.onClick.AddListener(OnChangePageClick);
            jumpBtn?.onClick.AddListener(JumpChoiceAvatarComClick);

            // text_buyBtn 所在的父 Button 即为"确认"按钮
            if (text_buyBtn != null)
            {
                var confirmBtn = text_buyBtn.GetComponentInParent<Button>();
                confirmBtn?.onClick.AddListener(OnConfirmBtnClick);
            }
        }

        private void OnDestroy()
        {
            StopPreview();
        }

        // ── 公共入口 ──────────────────────────────────────────

        public void SetData(CabinChatCreateBotProfileData botProfile)
        {
            if (botProfile == null) return;
            gameObject.SetActive(true);
            _botProfile = botProfile;

            SetupTitle(botProfile);
            SetupTags(botProfile);
            PrepareSearchParams(botProfile);

            _pages.Clear();
            _nameBuffer.Clear();
            _tag34Buffer.Clear();
            _tag12Buffer.Clear();
            _shownIds.Clear();
            _nameCookie = _tag34Cookie = _tag12Cookie = "";
            _nameEnd = _tag34End = _tag12End = false;
            _currentPage = 0;
            _selectedIndex = -1;

            HideAllItems();
            if (pageText != null) pageText.text = "";

            StartCoroutine(LoadAllPages());
        }

        // ── UI 初始化 ──────────────────────────────────────────

        private void SetupTitle(CabinChatCreateBotProfileData botProfile)
        {
            if (title_txt == null) return;
            string displayName = botProfile.characterName ?? botProfile.name ?? "";
            title_txt.text = $"为【{displayName}】匹配音色";
        }

        private void SetupTags(CabinChatCreateBotProfileData botProfile)
        {
            // 清理旧标签
            foreach (var go in _tagObjects)
                if (go != null) Destroy(go);
            _tagObjects.Clear();

            if (botProfile.botMatchTags == null || tagTxtTempGo == null || tagLayoutGo == null)
                return;

            tagTxtTempGo.SetActive(false);
            foreach (var tag in botProfile.botMatchTags)
            {
                if (string.IsNullOrEmpty(tag?.matchedTag)) continue;
                var go = Instantiate(tagTxtTempGo, tagLayoutGo.transform);
                go.SetActive(true);
                var txt = go.GetComponentInChildren<Text>();
                if (txt != null) txt.text = tag.matchedTag;
                _tagObjects.Add(go);
            }
        }

        private void PrepareSearchParams(CabinChatCreateBotProfileData botProfile)
        {
            _nameKeyword = botProfile.characterName ?? botProfile.name ?? "";

            _allTags.Clear();
            _partialTags.Clear();

            if (botProfile.botMatchTags != null)
            {
                foreach (var t in botProfile.botMatchTags)
                    if (!string.IsNullOrEmpty(t?.matchedTagId))
                        _allTags.Add(t.matchedTagId);
            }

            // 1-2 标签：取前 2 个
            _partialTags = _allTags.Take(Mathf.Min(2, _allTags.Count)).ToList();
        }

        // ── 搜索与分页加载 ────────────────────────────────────

        private IEnumerator LoadAllPages()
        {
            for (int p = 0; p < TotalPages; p++)
            {
                // 确保三类缓存各有足够 items
                yield return EnsureBuffer(SearchSource.Name,  SlotsPerSource);
                yield return EnsureBuffer(SearchSource.Tag34, SlotsPerSource);
                yield return EnsureBuffer(SearchSource.Tag12, SlotsPerSource);

                var page = BuildPage();
                _pages.Add(page);
            }

            ShowPage(0, defaultSelect: true);
        }

        /// <summary>确保 buffer 中至少有 needed 条（去重后）；不足时自动翻页请求</summary>
        private IEnumerator EnsureBuffer(SearchSource source, int needed)
        {
            var buffer = GetBuffer(source);
            while (buffer.Count < needed && !GetEnd(source))
            {
                bool done = false;
                var tagLists = GetTags(source);
                string tagIds = "";
                if(tagLists != null)
                {
                    tagIds = string.Join(",",GetTags(source));
                }
                CabinToneNetManager.Inst.SearchCabinTone(
                    GetKeyword(source), tagIds, GetCookie(source), FetchCount,
                    (ok, rsp) =>
                    {
                        if (ok && rsp?.list != null)
                        {
                            foreach (var item in rsp.list)
                            {
                                string tid = item?.ugcInfo?.id;
                                if (!string.IsNullOrEmpty(tid) && !_shownIds.Contains(tid))
                                    buffer.Add(item);
                            }
                            SetCookie(source, rsp.cookie ?? "");
                            SetEnd(source, rsp.IsEnd == 1);
                        }
                        else
                        {
                            SetEnd(source, true);
                        }
                        done = true;
                    });

                while (!done) yield return null;
            }
        }

        private enum SearchSource { Name, Tag34, Tag12 }

        private List<CabinCharacterToneSearchSubData> GetBuffer(SearchSource s) => s switch
        {
            SearchSource.Name  => _nameBuffer,
            SearchSource.Tag34 => _tag34Buffer,
            _                  => _tag12Buffer,
        };

        private string GetKeyword(SearchSource s) => s == SearchSource.Name ? _nameKeyword : "";
        private List<string> GetTags(SearchSource s) => s switch
        {
            SearchSource.Tag34 => _allTags,
            SearchSource.Tag12 => _partialTags,
            _                  => null,
        };
        private string GetCookie(SearchSource s) => s switch
        {
            SearchSource.Name  => _nameCookie,
            SearchSource.Tag34 => _tag34Cookie,
            _                  => _tag12Cookie,
        };
        private bool GetEnd(SearchSource s) => s switch
        {
            SearchSource.Name  => _nameEnd,
            SearchSource.Tag34 => _tag34End,
            _                  => _tag12End,
        };
        private void SetCookie(SearchSource s, string v) { if (s == SearchSource.Name) _nameCookie = v; else if (s == SearchSource.Tag34) _tag34Cookie = v; else _tag12Cookie = v; }
        private void SetEnd(SearchSource s, bool v)     { if (s == SearchSource.Name) _nameEnd  = v; else if (s == SearchSource.Tag34) _tag34End  = v; else _tag12End  = v; }

        /// <summary>从三个缓存各取 SlotsPerSource 条，合并排序，构建一页</summary>
        private List<(CabinCharacterToneSearchSubData data, int score)> BuildPage()
        {
            var page = new List<(CabinCharacterToneSearchSubData, int)>();

            AppendSlots(page, _nameBuffer,  3, _shownIds); // score 3
            AppendSlots(page, _tag34Buffer, 2, _shownIds); // score 2
            AppendSlots(page, _tag12Buffer, 1, _shownIds); // score 1

            // 按匹配度降序
            page.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return page;
        }

        private static void AppendSlots(
            List<(CabinCharacterToneSearchSubData, int)> page,
            List<CabinCharacterToneSearchSubData> buffer,
            int score,
            HashSet<string> shownIds)
        {
            int taken = 0;
            for (int i = 0; i < buffer.Count && taken < SlotsPerSource; i++)
            {
                string tid = buffer[i]?.ugcInfo?.id;
                if (string.IsNullOrEmpty(tid) || shownIds.Contains(tid)) continue;
                page.Add((buffer[i], score));
                shownIds.Add(tid);
                buffer.RemoveAt(i);
                i--;
                taken++;
            }
        }

        // ── 页面显示 ──────────────────────────────────────────

        private void ShowPage(int page, bool defaultSelect = false)
        {
            if (page < 0 || page >= _pages.Count) return;
            _currentPage = page;

            if (pageText != null)
                pageText.text = $"第{page + 1}/{TotalPages}页";

            var items = _pages[page];

            for (int i = 0; i < chatVoiceItems.Count; i++)
            {
                if (i < items.Count)
                {
                    int idx = i; // capture
                    chatVoiceItems[i].SetData(
                        items[i].data,
                        onItemClick: () => OnItemSelect(idx, autoPlay: true),
                        onPlayClick: () => OnPlayBtnClick(idx),
                        onBuyClick:  () => OnBuyItemClick(idx));
                }
                else
                {
                    chatVoiceItems[i].SetEmpty();
                }
            }

            // 默认选中第一个，不播放
            _selectedIndex = -1;
            if (defaultSelect && items.Count > 0)
                SelectItem(0, autoPlay: false);
            else if (!defaultSelect && items.Count > 0)
                SelectItem(0, autoPlay: false);
        }

        private void HideAllItems()
        {
            foreach (var item in chatVoiceItems)
                item.SetEmpty();
        }

        // ── 选中与播放 ────────────────────────────────────────

        private void OnItemSelect(int index, bool autoPlay)
        {
            SelectItem(index, autoPlay);
        }

        private void OnPlayBtnClick(int index)
        {
            // 点击播放按钮时：选中并播放
            SelectItem(index, autoPlay: true);
        }

        private void SelectItem(int index, bool autoPlay)
        {
            if (_selectedIndex >= 0 && _selectedIndex < chatVoiceItems.Count)
                chatVoiceItems[_selectedIndex].SetSelected(false);

            _selectedIndex = index;

            if (index >= 0 && index < chatVoiceItems.Count)
            {
                chatVoiceItems[index].SetSelected(true);
                if (autoPlay)
                    PlayPreview(chatVoiceItems[index]);
            }

            RefreshConfirmBtn();
        }

        private void RefreshConfirmBtn()
        {
            if (text_buyBtn == null) return;
            bool owned = IsSelectedOwned();
            text_buyBtn.text = owned ? "就是它了" : "去购买";
        }

        private bool IsSelectedOwned()
        {
            if (_selectedIndex < 0 || _selectedIndex >= chatVoiceItems.Count) return false;
            var data = chatVoiceItems[_selectedIndex].GetData();
            if (data == null) return false;
            int price = data.ugcInfo?.paymentInfo?.price ?? 0;
            return price == 0 || data.interactInfo?.consumed == 1;
        }

        private string GetSelectedToneId()
        {
            if (_selectedIndex < 0 || _selectedIndex >= chatVoiceItems.Count) return "";
            return chatVoiceItems[_selectedIndex].GetData()?.ugcInfo?.id ?? "";
        }

        private void PlayPreview(ChatVoiceItem item)
        {
            if (item == null) return;
            string url = item.GetPreviewUrl();
            if (string.IsNullOrEmpty(url)) return;

            AkSoundManager.Inst.StopUGCAudio(gameObject);
            AkSoundManager.Inst.PlayUGCAudioByUrl(url, false, gameObject);
        }

        private void StopPreview()
        {
            AkSoundManager.Inst?.StopUGCAudio(gameObject);
        }

        // ── 购买 ──────────────────────────────────────────────

        private void OnBuyItemClick(int index)
        {
            if (index < 0 || index >= chatVoiceItems.Count) return;
            var data = chatVoiceItems[index].GetData();
            if (data == null) return;

            SelectItem(index, autoPlay: false);

            if (chatBuyVoiceCom != null)
            {
                gameObject.SetActive(false);
                chatBuyVoiceCom.Show(data, this);
            }
        }

        /// <summary>从 ChatBuyVoiceCom 返回时调用</summary>
        public void ShowSelf()
        {
            gameObject.SetActive(true);
        }

        public CabinChatCreateBotProfileData BotProfile => _botProfile;

        // ── 翻页 ──────────────────────────────────────────────

        private void OnChangePageClick()
        {
            StopPreview();
            int next = (_currentPage + 1) % TotalPages;
            if (next < _pages.Count)
                ShowPage(next, defaultSelect: false);
        }

        // ── 确认按钮（"就是它了" / "去购买"） ────────────────────

        private void OnConfirmBtnClick()
        {
            if (IsSelectedOwned())
                OpenChoiceAvatarCom(GetSelectedToneId());
            else
                OnBuyItemClick(_selectedIndex);
        }

        private void OpenChoiceAvatarCom(string toneId)
        {
            if (chatChoiceAvatarCom == null) return;
            StopPreview();
            gameObject.SetActive(false);
            chatChoiceAvatarCom.Show(_botProfile, toneId, ShowSelf);
        }

        // ── 跳过音色选择 ──────────────────────────────────────

        private void JumpChoiceAvatarComClick()
        {
            // 跳过音色选择，以空 toneId 进入穿搭选择
            OpenChoiceAvatarCom(toneId: "");
        }

        // ── 关闭 ──────────────────────────────────────────────

        private void OnCloseClick()
        {
            StopPreview();
            gameObject.SetActive(false);
            chatNode?.SetActive(true);
        }

        // ── 工具 ──────────────────────────────────────────────
    }
}
