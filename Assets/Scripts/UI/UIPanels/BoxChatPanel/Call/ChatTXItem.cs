using Com.TheFallenGames.OSA.Util.IO;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 通讯录item(头像 名字),点击进入ChatCallTXDetailNode
    /// </summary>
    public class ChatTXItem : MonoBehaviour
    {
        public RemoteImageBehaviour remoteImageBehaviour;
        public Text nameText;
        public Button btn;

        private CabinPublishData _data;
        private Action<CabinPublishData> _onClick;

        public void Init(CabinPublishData data, Action<CabinPublishData> onClick)
        {
            _data = data;
            _onClick = onClick;

            if (nameText != null)
                nameText.text = data.characterInfo?.GetName() ?? string.Empty;

            if (remoteImageBehaviour != null && !string.IsNullOrEmpty(data.characterInfo?.characterPortraitUrl))
                remoteImageBehaviour.Load(data.characterInfo.characterPortraitUrl);

            btn?.onClick.RemoveAllListeners();
            btn?.onClick.AddListener(() => _onClick?.Invoke(_data));
        }
    }
}
