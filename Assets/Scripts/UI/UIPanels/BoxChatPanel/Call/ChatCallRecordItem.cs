using Com.TheFallenGames.OSA.Util.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通话记录(头像 名字 时间),点击进入ChatCallDetailNode
    /// </summary>
    public class ChatCallRecordItem : MonoBehaviour
    {
        public RemoteImageBehaviour portraitImage;
        public Text nameText;
        public Text timeText;
        public Button btn;

        private CabinChatSessionData _data;
        private Action<CabinChatSessionData> _onClick;

        public void Init(CabinChatSessionData data, Action<CabinChatSessionData> onClick)
        {
            _data = data;
            _onClick = onClick;

            if (nameText != null) nameText.text = data.name ?? string.Empty;

            if (portraitImage != null && !string.IsNullOrEmpty(data.portraitUrl))
                portraitImage.Load(data.portraitUrl);

            if (timeText != null)
                timeText.text = FormatTimestamp(data.timestamp);

            btn?.onClick.RemoveAllListeners();
            btn?.onClick.AddListener(() => _onClick?.Invoke(_data));
        }

        public void Init(CabinAudioHistory data, Action<CabinAudioHistory> onClick)
        {
            if (nameText != null) nameText.text = data.name ?? string.Empty;
            if (portraitImage != null && !string.IsNullOrEmpty(data.portraitUrl))
                portraitImage.Load(data.portraitUrl);
            if (timeText != null) timeText.text = FormatTimestamp(data.timestamp > 0 ? data.timestamp : data.startTime);
            btn?.onClick.RemoveAllListeners();
            btn?.onClick.AddListener(() => onClick?.Invoke(data));
        }

        private string FormatTimestamp(int unixSeconds)
        {
            if (unixSeconds <= 0) return string.Empty;
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(unixSeconds).ToLocalTime();
            var now = DateTime.Now;
            if (dt.Date == now.Date) return dt.ToString("HH:mm");
            if ((now - dt).TotalDays < 7) return dt.ToString("ddd HH:mm");
            if (dt.Year == now.Year) return dt.ToString("MM/dd");
            return dt.ToString("yyyy/MM/dd");
        }
    }
}
