using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 结果item
    /// </summary>
    public class ChatResultItem : MonoBehaviour
    {
        public Text text_content;
        public Button btn_dangan;
        public ChatSelectionDetailCom chatSelectionDetailCom;

        CabinChatCreateBotProfileData _data;

        void Awake()
        {
            btn_dangan.onClick.AddListener(OnDanganBtnClick);
        }

        public void SetData(CabinChatCreateBotProfileData data)
        {
            _data = data;
            text_content.text = data.name + "\n" + data.one_line_note;
        }

        void OnDanganBtnClick()
        {
            if (chatSelectionDetailCom == null) return;
            chatSelectionDetailCom.gameObject.SetActive(true);
            chatSelectionDetailCom.SetData(_data);
        }
    }
}
