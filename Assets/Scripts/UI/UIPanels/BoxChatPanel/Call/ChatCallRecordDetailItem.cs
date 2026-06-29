using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通话记录详情页的通话记录item(日期 时间 时长)
    /// </summary>
    public class ChatCallRecordDetailItem : MonoBehaviour
    {
        public Text dateText;
        public Text timeText;
        public Text durationText;

        public void Init(CabinAudioHistory data)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(data.startTime).ToLocalTime();

            if (dateText != null) dateText.text = dt.ToString("yyyy/MM/dd");
            if (timeText != null) timeText.text = dt.ToString("HH:mm");
            if (durationText != null) durationText.text = FormatDuration(data.duration);
        }

        private string FormatDuration(int seconds)
        {
            if (seconds <= 0) return "00:00";
            int m = seconds / 60;
            int s = seconds % 60;
            return $"{m:D2}:{s:D2}";
        }
    }
}
