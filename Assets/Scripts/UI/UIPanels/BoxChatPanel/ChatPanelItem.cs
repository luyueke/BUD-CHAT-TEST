using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Fsbm.Runtime;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Game
{
    [Serializable]
    public class ChatPanelItemCom
    {
        public RectTransform BgRect;
        public RectTransform TextRect;
        public ContentSizeFitter TextSize;
        public Text Name;
        public Text Text;
        public Image WaitImg;
        public RemoteImageBehaviour portrait;
        public Image portraitBg;

        public GameObject voiceNode;
        public GameObject voiceNode_playGo; //播放
        public GameObject voiceNode_retryGo; //重试
        public GameObject voiceNode_loadingGo; //加载中
        public GameObject voiceNode_playingGo; //播放中
        public Button voiceNode_btn; //播放/重试
        public Text voiceNode_voiceDuration; //比如10'   
    }
    public class ChatPanelItem : ItemRenderer
    {
        public RectTransform ItemRect;

        public ChatPanelItemCom RobotItem;

        public ChatPanelItemCom MineItem;

        public ChatItemMsgProducingCom ChatItemMsgProducing;

        ChatPanelItemCom Com
        {
            get
            {
                if (type)
                {
                    return MineItem;
                }
                else
                {
                    return RobotItem;
                }
            }
        }

        bool type;
        string str;
        public int chatType; // 1=创建角色聊天 2=正常角色聊天

        BudTimer budTimer;

        // ─── 语音播放状态 ─────────────────────────────────────────────────────
        private static ChatPanelItem _currentPlayingItem; // 全局唯一播放实例

        private string _voiceAudioUrl;
        private bool _isPlayingVoice;
        private Coroutine _voiceCoroutine;
        private AudioSource _voiceAudioSource;

        // TTS 请求参数（用于首次加载和重试）
        private string _sessionId;
        private string _msgId;
        private string _voiceContent;
        protected override void Init()
        {
            base.Init();
        }

        protected override void OnDisable()
        {
            TimerManager.Inst.Stop(budTimer);
            StopVoice();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            TimerManager.Inst.Stop(budTimer);
            StopVoice();
            base.OnDestroy();
        }

        public void BeginProducing()
        {
            ChatItemMsgProducing.gameObject.SetActive(true);
            Com.BgRect.GetComponent<LayoutElement>().minHeight = 100;
            Com.BgRect.GetComponent<LayoutElement>().preferredWidth = 250;
            if (RobotItem.voiceNode != null) RobotItem.voiceNode.SetActive(false);
        }

        public void EndProducing()
        {
            ChatItemMsgProducing.gameObject.SetActive(false);
            Com.BgRect.GetComponent<LayoutElement>().minHeight = 100;
            Com.BgRect.GetComponent<LayoutElement>().preferredWidth = 900;
        }

        /// <summary>初始化为 bot 侧占位状态，显示点阵等待动画，等首个 stream chunk 到来后由 SetData 接管</summary>
        public void SetBotProducing()
        {
            TimerManager.Inst.Stop(budTimer);
            type = false;
            RobotItem.BgRect.gameObject.SetActive(false);
            MineItem.BgRect.gameObject.SetActive(false);
            Com.BgRect.gameObject.SetActive(true);
            Com.Text.text = "";
            // Com.WaitImg.fillAmount = 0;
            BeginProducing();
        }

        public void SetData((bool, string) dt)
        {
            TimerManager.Inst.Stop(budTimer);
            RobotItem.BgRect.gameObject.SetActive(false);
            MineItem.BgRect.gameObject.SetActive(false);

            //type = (((bool, string))data).Item1;
            //str = (((bool, string))data).Item2;
            type = dt.Item1;
            str = dt.Item2;

            EndProducing();
            // Com.WaitImg.fillAmount = 0;
            Com.BgRect.gameObject.SetActive(true);
            if (type)
            {
                // Com.Name.text = AccountDataManager.Inst.UserInfo.nickname;
            }
            else
            {
                // Com.Name.text = AccountDataManager.Inst.KibooInfo.name;
            }

            StopVoice();

            // 普通聊天模式下预存内容，等流式吐字结束后由 ChatPanel 调 SetVoiceNodeData 触发拉取
            if (!type && chatType == 2)
            {
                _voiceAudioUrl = null;
                _voiceContent = str;
                if (RobotItem.voiceNode != null) RobotItem.voiceNode.SetActive(false);
            }
            else if (RobotItem.voiceNode != null)
            {
                RobotItem.voiceNode.SetActive(false);
            }

            Com.Text.text = str;
            Com.TextSize.SetLayoutVertical();
            Com.BgRect.GetComponent<LayoutElement>().preferredWidth = Math.Min(Math.Min(900, Com.Text.preferredWidth) + 120,900);


            if (Com.TextRect.sizeDelta.y > 153)
            {
                var offset = Com.TextRect.sizeDelta.y - 153;
                Com.BgRect.sizeDelta = new Vector2(Com.BgRect.sizeDelta.x, 250 + offset);
                ItemRect.sizeDelta = new Vector2(ItemRect.sizeDelta.x, 290 + offset);
            }
            else
            {
                Com.BgRect.sizeDelta = new Vector2(Com.BgRect.sizeDelta.x, 250);
                ItemRect.sizeDelta = new Vector2(ItemRect.sizeDelta.x, 290);
            }
        }

        public void AppendText(string chunk)
        {
            str += chunk;
            Com.Text.text = str;
            Com.TextSize.SetLayoutVertical();

            Com.BgRect.GetComponent<LayoutElement>().preferredWidth = Math.Min(Math.Min(900, Com.Text.preferredWidth) + 120,900);
            if (Com.TextRect.sizeDelta.y > 153)
            {
                var offset = Com.TextRect.sizeDelta.y - 153;
                Com.BgRect.sizeDelta = new Vector2(Com.BgRect.sizeDelta.x, 250 + offset);
                ItemRect.sizeDelta = new Vector2(ItemRect.sizeDelta.x, 290 + offset);
            }
            else
            {
                Com.BgRect.sizeDelta = new Vector2(Com.BgRect.sizeDelta.x, 250);
                ItemRect.sizeDelta = new Vector2(ItemRect.sizeDelta.x, 290);
            }
        }

        public void SetWaitAnim()
        {
            type = false;

            TimerManager.Inst.Stop(budTimer);

            RobotItem.BgRect.gameObject.SetActive(false);
            MineItem.BgRect.gameObject.SetActive(false);
            Com.BgRect.gameObject.SetActive(true);

            if (type)
            {
                // Com.Name.text = AccountDataManager.Inst.UserInfo.nickname;
            }
            else
            {
                // Com.Name.text = AccountDataManager.Inst.KibooInfo.name;
            }

            Com.Text.text = "";

            // Com.WaitImg.fillAmount = 0;
            // budTimer = TimerManager.Inst.Run("SetWaitAnim", 0, 0.5f, () => {
            //     var val = Com.WaitImg.fillAmount;
            //     switch (val)
            //     {
            //         case 0: Com.WaitImg.fillAmount = 0.3f; break;
            //         case 0.3f: Com.WaitImg.fillAmount = 0.7f; break;
            //         case 0.7f: Com.WaitImg.fillAmount = 1; break;
            //         case 1: Com.WaitImg.fillAmount = 0; break;
            //     }
            // });
        }



        // ─── 语音节点 ─────────────────────────────────────────────────────────

        /// <summary>为 bot 消息绑定 TTS 参数；有 audioUrl 直接播放，无则请求 TTS</summary>
        public void SetVoiceBotData(string sessionId, string msgId, string content, string audioUrl = null, int audioDurationMs = 0)
        {
            _sessionId = sessionId;
            _msgId = msgId;
            _voiceContent = content;
            SetVoiceNodeData(audioUrl, audioDurationMs);
        }

        /// <summary>为 bot 消息绑定语音；始终显示 voiceNode，有 audioUrl 直接使用，无则请求 TTS</summary>
        public void SetVoiceNodeData(string audioUrl, int audioDurationMs)
        {
            var com = RobotItem;
            if (com.voiceNode == null) return;

            _voiceAudioUrl = audioUrl;
            com.voiceNode.SetActive(true);

            if (audioDurationMs > 0 && com.voiceNode_voiceDuration != null)
            {
                int secs = Mathf.Max(1, audioDurationMs / 1000);
                com.voiceNode_voiceDuration.text = $"{secs}''";
            }

            if (com.voiceNode_btn != null)
            {
                com.voiceNode_btn.onClick.RemoveAllListeners();
                com.voiceNode_btn.onClick.AddListener(OnVoiceBtnClick);
            }

            if (string.IsNullOrEmpty(audioUrl))
                FetchTextAudio();
            else
                VoiceSetPlay();
        }

        private void FetchTextAudio()
        {
            VoiceSetLoading();
            CabinChatManager.Inst.GetBoxTextAudio(
                _sessionId, _msgId, _voiceContent,
                (data) =>
                {
                    if (this == null) return;
                    if (!string.IsNullOrEmpty(data?.audioUrl))
                    {
                        _voiceAudioUrl = data.audioUrl;
                        if (data.audioDuration > 0 && RobotItem.voiceNode_voiceDuration != null)
                        {
                            int secs = Mathf.Max(1, data.audioDuration / 1000);
                            RobotItem.voiceNode_voiceDuration.text = $"{secs}''";
                        }
                        VoiceSetPlay();
                    }
                    else
                    {
                        VoiceSetError();
                    }
                },
                (_) =>
                {
                    if (this == null) return;
                    VoiceSetError();
                });
        }

        private void VoiceSetPlay()
        {
            var com = RobotItem;
            if (com.voiceNode_playGo != null)   com.voiceNode_playGo.SetActive(true);
            if (com.voiceNode_loadingGo != null) com.voiceNode_loadingGo.SetActive(false);
            if (com.voiceNode_playingGo != null) com.voiceNode_playingGo.SetActive(false);
            if (com.voiceNode_retryGo != null)   com.voiceNode_retryGo.SetActive(false);
        }

        private void VoiceSetLoading()
        {
            var com = RobotItem;
            if (com.voiceNode_playGo != null)   com.voiceNode_playGo.SetActive(false);
            if (com.voiceNode_loadingGo != null) com.voiceNode_loadingGo.SetActive(true);
            if (com.voiceNode_playingGo != null) com.voiceNode_playingGo.SetActive(false);
            if (com.voiceNode_retryGo != null)   com.voiceNode_retryGo.SetActive(false);
        }

        private void VoiceSetPlaying()
        {
            var com = RobotItem;
            if (com.voiceNode_playGo != null)   com.voiceNode_playGo.SetActive(false);
            if (com.voiceNode_loadingGo != null) com.voiceNode_loadingGo.SetActive(false);
            if (com.voiceNode_playingGo != null) com.voiceNode_playingGo.SetActive(true);
            if (com.voiceNode_retryGo != null)   com.voiceNode_retryGo.SetActive(false);
        }

        private void VoiceSetError()
        {
            var com = RobotItem;
            if (com.voiceNode_playGo != null)   com.voiceNode_playGo.SetActive(false);
            if (com.voiceNode_loadingGo != null) com.voiceNode_loadingGo.SetActive(false);
            if (com.voiceNode_playingGo != null) com.voiceNode_playingGo.SetActive(false);
            if (com.voiceNode_retryGo != null)   com.voiceNode_retryGo.SetActive(true);
        }

        private void OnVoiceBtnClick()
        {
            if (_isPlayingVoice)
            {
                // 播放中 → 停止
                StopVoice();
                VoiceSetPlay();
                return;
            }

            // 停掉当前全局播放的其他 item
            var prevPlaying = _currentPlayingItem;
            if (prevPlaying != null && prevPlaying != this)
            {
                prevPlaying.StopVoice();
                prevPlaying.VoiceSetPlay();
            }

            if (string.IsNullOrEmpty(_voiceAudioUrl))
            {
                // 无 URL → 重新请求 TTS
                FetchTextAudio();
            }
            else
            {
                // 有 URL → 直接播放
                _voiceCoroutine = StartCoroutine(PlayVoiceCoroutine());
            }
        }

        private void StopVoice()
        {
            if (_voiceCoroutine != null)
            {
                StopCoroutine(_voiceCoroutine);
                _voiceCoroutine = null;
            }
            if (_voiceAudioSource != null && _voiceAudioSource.isPlaying)
                _voiceAudioSource.Stop();
            _isPlayingVoice = false;
            if (_currentPlayingItem == this)
                _currentPlayingItem = null;
        }

        private IEnumerator PlayVoiceCoroutine()
        {
            _isPlayingVoice = true;
            _currentPlayingItem = this;
            VoiceSetLoading();

            using var req = UnityWebRequestMultimedia.GetAudioClip(_voiceAudioUrl, AudioType.MPEG);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                _isPlayingVoice = false;
                _currentPlayingItem = null;
                VoiceSetError();
                _voiceCoroutine = null;
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                _isPlayingVoice = false;
                _currentPlayingItem = null;
                VoiceSetError();
                _voiceCoroutine = null;
                yield break;
            }

            if (_voiceAudioSource == null)
                _voiceAudioSource = gameObject.AddComponent<AudioSource>();

            _voiceAudioSource.clip = clip;
            _voiceAudioSource.Play();
            VoiceSetPlaying();

            yield return new WaitUntil(() => !_voiceAudioSource.isPlaying || !_isPlayingVoice);

            _isPlayingVoice = false;
            _currentPlayingItem = null;
            VoiceSetPlay();
            _voiceCoroutine = null;
        }

        /// <summary>加载头像并设置对方头像底色；自己头像自动从 AccountDataManager.Inst.UserInfo 获取</summary>
        public void SetPortrait(string robotPortraitUrl, int colorId = 0)
        {
            if (!string.IsNullOrEmpty(robotPortraitUrl) && RobotItem.portrait != null)
                RobotItem.portrait.Load(robotPortraitUrl);

            if (colorId != 0 && RobotItem.portraitBg != null)
            {
                var config = DataTables.GetDraftBoxCardColorConfig(colorId);
                if (config != null && ColorUtility.TryParseHtmlString($"#{config.Color}", out Color bgColor))
                    RobotItem.portraitBg.color = bgColor;
            }

            var mineUrl = AccountDataManager.Inst.UserInfo.portraitUrl;
            if (!string.IsNullOrEmpty(mineUrl) && MineItem.portrait != null)
                MineItem.portrait.Load(mineUrl);
        }

        protected override void UpdateView()
        {
            base.UpdateView();
        }
    }
}