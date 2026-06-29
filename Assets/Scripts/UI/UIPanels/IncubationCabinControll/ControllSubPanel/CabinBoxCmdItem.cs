using Game.BudBox;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 唤醒指令界面 - 单条口令列表 Item
    /// 展示口令触发词，并提供「播放」按钮。
    /// disabled==1 时按钮置灰不可交互。
    /// 点击播放后等待 MQTT 回包：
    ///   set_active active=1 oper_state=1 → 进入播放中状态
    ///   set_active active=1 oper_state=2 → 还原为播放按钮
    /// </summary>
    public class CabinBoxCmdItem : MonoBehaviour
    {
        [SerializeField] private Text IndexText;   // 序号
        [SerializeField] private Text TitleTxt;    // 口令触发词
        [SerializeField] private Button PlayBtn;   // 播放按钮
        [SerializeField] private GameObject PlayingObj;  // 播放按钮文本

        /// <summary>点击「播放」时触发：参数为口令数据，由父级 CabinBoxCmdPanel 注入</summary>
        public Action<voiceCommands, Action> onPlayClick;

        private static CabinBoxCmdItem _playingItem; // 全局唯一播放实例

        private voiceCommands _data;
        private bool _isPlaying;
        private string _deviceId;

        private void Awake()
        {
            PlayBtn.onClick.AddListener(OnPlayClicked);
        }

        /// <summary>绑定口令数据并刷新显示</summary>
        public void Init(voiceCommands data, int idx)
        {
            _data = data;
            _isPlaying = false;

            IndexText.text = $"{idx + 1}";
            TitleTxt.text = data.command;
            PlayingObj.SetActive(false);
            PlayBtn.gameObject.SetActive(data.disabled != 1);

            _deviceId = CabinBoxManager.Inst.GetCurrentDeviceId();
            MessageHelper.AddListener<string>(MessageName.OnBudBoxActiveChange, OnBudBoxActiveChange);
        }

        /// <summary>销毁前清理回调与消息监听</summary>
        public void ClearData()
        {
            onPlayClick = null;
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxActiveChange, OnBudBoxActiveChange);
        }

        private void OnDestroy()
        {
            if (_playingItem == this) _playingItem = null;
            MessageHelper.RemoveListener<string>(MessageName.OnBudBoxActiveChange, OnBudBoxActiveChange);
        }

        private void OnPlayClicked()
        {
            if (_isPlaying) return;
            if (_playingItem != null && _playingItem != this)
            {
                TipPanel.ShowToast("正在播放其他指令");
                return;
            }
            _isPlaying = true;
            _playingItem = this;
            PlayBtn.gameObject.SetActive(false);
            // 传 null 作为 onDone，状态完全由 MQTT 回包驱动
            onPlayClick?.Invoke(_data, null);
        }

        private void OnPlayDone()
        {
            _isPlaying = false;
            if (_playingItem == this) _playingItem = null;
            PlayingObj.SetActive(false);
            PlayBtn.gameObject.SetActive(_data?.disabled != 1);
        }

        private void OnBudBoxActiveChange(string deviceId)
        {
            if (!_isPlaying) return;
            if (!string.IsNullOrEmpty(_deviceId) && deviceId != _deviceId) return;

            var boxData = CabinBoxManager.Inst.GetCabinBudBoxData(_deviceId);
            if (boxData == null) return;

            int active = boxData.deviceState.active;
            int operState = boxData.deviceState.oper_state;

            if (active == 1 && operState == 1)
            {
                PlayingObj.SetActive(true);
            }
            else if (active == 1 && operState == 2)
            {
                OnPlayDone();
            }
        }
    }
}
