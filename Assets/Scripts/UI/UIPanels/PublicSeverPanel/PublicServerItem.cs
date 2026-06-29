

using Game.Base;
using GameData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;

namespace Game.PublicSever
{
    public class PublicServerItem : MonoBehaviour
    {
        public SuperTextMesh mapName;
        public CText Txt_PlayNum;
        public CButton Btn_Join;
        public CButton Btn_View;
        public GameObject emptyTipGo;

        private PublicMapData _curData;

        public void Awake()
        {
            Btn_Join.onClick.RemoveAllListeners();
            Btn_Join.onClick.AddListener(OnJoinClick);
            Btn_View.onClick.RemoveAllListeners();
            Btn_View.onClick.AddListener(OnBtnViewClick);
        }

        public void InitData(PublicMapData data)
        {
            _curData = data;
            mapName.text = data.mapName;
            Txt_PlayNum.text = data.allPlayerCnt;
            // LocalizationConManager.Inst.SetLocalizedContent(playNum, "{0} Playing", DataUtils.NumToString(data.allPlayerCnt));
        }

        private void OnJoinClick()
        {
            if (string.IsNullOrEmpty(_curData?.mapId))
            {
                LoggerUtils.LogError("PublicServerItem OnJoinClick MapId IsNull", " User = ", AccountDataManager.Inst.UserInfo.uid);
                return;
            }
            
            JObject req = new JObject()
            {
                ["id"] = _curData?.mapId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
                (string content) =>
                {
                    UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                    var p = UIManager.Inst.OpenPanel<UgcLoadingPanel>(PanelId.UgcLoadingPanel);
                    p.Init(rspData.mapInfo, rspData.creator, LoadingType.Map);
                    // GameController.StartGame(EnterGameModel.GuestScene, rspData.mapInfo);
                    GameController.StartGuestGame(rspData.mapInfo,rspData.creator,rspData.interactInfo);
                },
                null);
        }

        private void OnBtnViewClick()
        {
            UIManager.Inst.SwapPanel(PanelId.MapDetailPanel, _curData?.mapId);
        }
    }
}
