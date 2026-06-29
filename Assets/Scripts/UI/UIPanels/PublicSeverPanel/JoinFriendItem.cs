using UnityEngine;
using UnityEngine.UI;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Base;
using Game.GameSync;
using GameData;
using GameData.UGCData;
using UI.BaseWidgets;
using UI.UIPanels.ProfilePanel;

namespace Game.PublicSever
{
    public class JoinFriendItem : MonoBehaviour
    {
        public RemoteImageBehaviour RM_MapCover;
        public Text mapName;
        public Text serverCode;
        public Text serverPlayerInfo;
        public LoadingButton joinBtn;
        public GameObject joinDisableGO; // 禁用按钮
        public Button mapBtn; // 地图信息
        public Button copyBtn; // 复制
        public GameObject emptyTipGo;
        public GameObject headMoreGo;
        public Text headMoreTxt;

        public HorizontalLayoutGroup playerTmpRootLayout;

        private JoinFriendData _curData;

        private void Awake()
        {
            joinBtn.onClick.AddListener(OnJoinClick);
            mapBtn.onClick.AddListener(OnMapClick);
            copyBtn.onClick.AddListener(OnCopyClick);
        }

        public void InitData(JoinFriendData data)
        {
            _curData = data;
            serverCode.text = data.roomCode;
            serverPlayerInfo.text = $"{data.roomPlayerNum}/{data.maxPlayer}";

            var canJoin = data.roomPlayerNum < data.maxPlayer;
            joinBtn.gameObject.SetActive(canJoin);
            joinDisableGO.gameObject.SetActive(!canJoin);

            UpdatePlayers();
            UpdateMapInfo();
        }

        private void UpdatePlayers()
        {
            var rootTF = playerTmpRootLayout.GetComponent<RectTransform>();
            int count = _curData.players != null ? _curData.players.Count : 0;
            int tfCount = rootTF.childCount;
            int lastPIndex = tfCount - 1;
            var lastPTF = headMoreGo.transform.Find("people"); // 最后一个头像需要特殊处理一下

            for (int i = 0; i < tfCount; i++)
            {
                var pTF = rootTF.GetChild(i);
                pTF.gameObject.SetActive(false);
                if (i < count)
                {
                    SetPlayerHeadUI(pTF, _curData.players[i]);
                }
            }

            // 超出了5个
            headMoreGo.SetActive(false);
            if (count > tfCount)
            {
                rootTF.GetChild(lastPIndex).gameObject.SetActive(false);
                SetPlayerHeadUI(lastPTF, _curData.players[lastPIndex]);
                headMoreGo.SetActive(true);
                headMoreTxt.text = $"+{count - tfCount}";
            }

            emptyTipGo.SetActive(false);
            if (count == 0)
            {
                emptyTipGo.SetActive(true);
            }
        }

        private void SetPlayerHeadUI(Transform pTF, JoinFriendData.playerInfo info)
        {
            var playerBtn = pTF.GetComponent<Button>();
            var headIcon = pTF.transform.Find("head/Icon");
            pTF.gameObject.SetActive(true);
            if (playerBtn != null)
            {
                playerBtn.onClick.AddListener(() => OnPlayerClick(info.uid)); // 点击事件
            }

            if (headIcon != null)
            {
                var rmPlayerHeadImage =headIcon.GetComponent<RemoteImageBehaviour>();
                if (rmPlayerHeadImage != null)
                {
                    rmPlayerHeadImage.Load(info.headUrl);
                }
            }
            // playerIcTF.gameObject.SetActive(info.officialCert != null && info.officialCert.accountClass == 1);
        }

        private void UpdateMapInfo()
        {
            mapName.text = _curData.mapName;
            RM_MapCover.Load(_curData.mapCover);
        }

        private void OnPlayerClick(string userId)
        {
            UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, userId);
        }

        private void OnJoinClick()
        {
            joinBtn.ShowLoading();
            GameSyncHttpHelper.Inst.EnterRoomByRoomCode(_curData.roomCode, OnEnterRoomSuccess, OnEnterRoomFail);
        }

        private void OnEnterRoomSuccess(UgcInfoRsp rspData)
        {
            if(this == null)
                return;

            var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
            p.Init(rspData.mapInfo, rspData.creator, LoadingType.Map);
            // GameController.StartGame(EnterGameModel.GuestScene, rspData.mapInfo);
            GameController.StartGuestGame(rspData.mapInfo,rspData.creator,rspData.interactInfo);
            joinBtn.HideLoading();
        }

        private void OnEnterRoomFail()
        {
            if(this == null)
                return;

            joinBtn.HideLoading();
        }

        private void OnMapClick()
        {
            UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, _curData.mapId);
        }

        private void OnCopyClick()
        {
            GUIUtility.systemCopyBuffer = _curData.roomCode;
            TipPanel.ShowToast("复制房间码成功");
        }
    }
}
