using System;
using Com.TheFallenGames.OSA.Util.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class ChatChoiceAvatarItem : MonoBehaviour
    {
        public RemoteImageBehaviour remoteImageBehaviour;
        public GameObject selectedImgGo;

        private Action _onSelect;

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => _onSelect?.Invoke());
        }

        /// <summary>
        /// coverUrl: 设子封面，为 null 时表示「当前穿搭」（不加载远程图）
        /// </summary>
        public void SetData(string coverUrl, Action onSelect)
        {
            _onSelect = onSelect;

            if (!string.IsNullOrEmpty(coverUrl))
            {
                remoteImageBehaviour?.gameObject.SetActive(false);
                remoteImageBehaviour?.Load(coverUrl, true,
                    (_, ok) => { if (remoteImageBehaviour != null) remoteImageBehaviour.gameObject.SetActive(true); });
            }
            else
            {
                remoteImageBehaviour?.gameObject.SetActive(false);
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            selectedImgGo?.SetActive(selected);
        }
    }
}
