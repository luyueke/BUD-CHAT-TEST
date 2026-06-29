using Com.TheFallenGames.OSA.Util.IO;
using Es;
using GameData;
using System.Collections;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class OcCompetitionPanelBtn : MonoBehaviour
    {
        public Button Btn;

        public RemoteImageBehaviour RemoteImage;

        private void Awake()
        {
            gameObject.SetActive(false);//先隐藏这个功能
            return;
            var oc = LobbyInfoManager.Inst.GetContestInfo(BUDContestType.OC);
            if (oc != null) 
            {
                RemoteImage.Load(oc.fittingRoomIconUrl);
            }
            else
            {
                LobbyInfoManager.Inst.GetLobbyInfo((t) => {
                    var oc = LobbyInfoManager.Inst.GetContestInfo(BUDContestType.OC);
                    if (oc != null)
                    {
                        RemoteImage.Load(oc.fittingRoomIconUrl);
                    }
                });
            }
            Btn.onClick.AddListener(OnBtn);
        }

        // private void OnEnable()
        // {
        //     if (!OcCompetitionSystem.Inst.Exist())
        //     {
        //         gameObject.SetActive(false);
        //     }
        // }

        void OnBtn() {
            OcCompetitionSystem.Inst.OpenPanel();
        }
    }
}