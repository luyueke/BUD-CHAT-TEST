using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.MapData;
using GameData.UGCData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    public class MapDetailView : MonoBehaviour
    {
        [SerializeField] private RemoteImageBehaviour imgBehav;
        [SerializeField] private SuperTextMesh mapName;
        [SerializeField] private SuperTextMesh mapDesc;
        [SerializeField] private Text visitsTxt;
        [SerializeField] private Text likesTxt;
        [SerializeField] private Text favoritesTxt;
        [SerializeField] private CButton mainBtn;
        [SerializeField] private GameObject auditView;
        public int titleLimit = 65;
        public int descLimit = 120;

        private MapResInfo mResInfo;

        private void Start()
        {
            mainBtn.onClick.AddListener(OnClick);
        }

        
        public void Refresh(MapResInfo resInfo)
        {
            mResInfo = resInfo;
            var mapInfo = mResInfo.mapInfo;
            
            if (mapInfo == null) return;
            
            imgBehav.Load(mapInfo.cover);
            mapName.SetText(DataUtil.GetTextWithEmoji(mapInfo.name, titleLimit));
            mapDesc.SetText(DataUtil.GetTextWithEmoji(mapInfo.desc, descLimit));
            
            RefreshInteractInfo(mResInfo.interactInfo);
            
            auditView.SetActive(FormatUtils.IsAuditing(mapInfo));
        }

        private void RefreshInteractInfo(BaseInteractInfo interactInfo)
        {
            if (interactInfo == null) return;
            visitsTxt.SetText(interactInfo.consumeAmount + "");
            likesTxt.SetText(interactInfo.likeAmount + "");
            favoritesTxt.SetText(interactInfo.collectAmount + "");
        }

        private void OnClick()
        {
            if (mResInfo == null || mResInfo.mapInfo == null || string.IsNullOrEmpty(mResInfo.mapInfo.id)) return;
            if (GameController.IsInHallScene())
            {
                UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, mResInfo.mapInfo.id);
            }
            else
            {
                TipPanel.ShowToast("您已经在游戏内，请退出房间后再试");
            }

            
        }
    }
}
