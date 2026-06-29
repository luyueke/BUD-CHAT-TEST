using System;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using GameData.Base;
using GameData.UGCData;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileGameItem : MonoBehaviour
    {
        [SerializeField] private RemoteImageBehaviour cover;
        [SerializeField] private SuperTextMesh mapName;
        [SerializeField] private CButton clickBtn;
        [SerializeField] private GameObject auditView;
        private MapResInfo _resInfo;
        private Action<MapResInfo, Texture> _onSelect;


        public void InitUI()
        {

        }

        public void SetData(MapResInfo item, Action<MapResInfo, Texture> onSelect)
        {
            if (item == null)
            {
                return;
            }

            _onSelect = onSelect;
            _resInfo = item;
            mapName.text = item.mapInfo.name;
            cover.Load(item.mapInfo.cover);
            auditView.SetActive(FormatUtils.IsAuditing(item.mapInfo));

            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() => { _onSelect?.Invoke(_resInfo, cover.RawImage.texture); });

        }
    }
}