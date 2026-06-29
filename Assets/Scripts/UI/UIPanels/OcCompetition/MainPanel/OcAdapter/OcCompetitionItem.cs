using ChocDino.UIFX;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using Newbie;
using System;
using System.Collections;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionItem : MonoBehaviour
    {
        public GameObject buyRoot;
        public GameObject infoRoot;
        public CButton btn_Item;
        public Text buyText;
        public Image selectedImage;
        public RemoteImageBehaviour remoteAssetsIcon;

        private Action<OcServerData> onItemSelected;
        private OcServerData mData;
        private int index;
        private void Awake()
        {
            btn_Item.onClick.AddListener(OnItemClick);
            selectedImage.gameObject.SetActive(false);
        }

        private void OnItemClick()
        {
            if (index == 0)
            {
                var panel2 = UIManager.Inst.FindPanel<OcCompetitionPanel>(PanelId.OcCompetitionPanel);
                if (panel2 != null)
                {
                    panel2.MyGroup.AvatarGroup.ShowAvatar(false);
                }

                var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
                if (panel != null)
                {
                    panel.transform.SetAsLastSibling();
                    panel.ShowAvatar(true);
                }
                else
                {
                    UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
                }
            }
            else 
            {
                if (!string.IsNullOrEmpty(mData.ocInfo.avatarJson))
                {
                    onItemSelected?.Invoke(mData);
                    selectedImage.gameObject.SetActive(true);
                }
            }
        }

        public void SetData(OcServerData info, Action<OcServerData> action, int idx)
        {
            index = idx;
            mData = info;
            onItemSelected = action;

            selectedImage.gameObject.SetActive(false);
            if (index == 0)
            {
                buyRoot.gameObject.SetActive(true);
                infoRoot.gameObject.SetActive(false);
                if (info.add)
                {
                    buyText.text = $"{info.cur}/{info.total}";
                }
            }
            else
            {
                buyRoot.gameObject.SetActive(false);
                infoRoot.gameObject.SetActive(true);
                remoteAssetsIcon.gameObject.SetActive(false);
                if (!string.IsNullOrEmpty(info.ocInfo.ocCover)) {
                    remoteAssetsIcon.Load(info.ocInfo.ocCover, onCompleted: (bool fromCache, bool success) =>
                    {
                        remoteAssetsIcon.gameObject.SetActive(true);
                    });
                }

            }

            //emptyIcon.sprite = info?.skinType == 1 ? petOcSprite : characterOcSprite;
        }
    }
}