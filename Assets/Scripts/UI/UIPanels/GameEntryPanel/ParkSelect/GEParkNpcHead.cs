using Com.TheFallenGames.OSA.Util.IO;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using System;
using System.Collections;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public class GEParkNpcHead : MonoBehaviour
    {
        public Image Image;

        public RemoteImageBehaviour RemoteImage;

        public void SetDate(string id,string cover) {
            if (Enum.TryParse(id, out ParkNpcRoleType type))
            {
                Image.sprite = PgcUtils.LoadNpcHeadIcon(Es.DataTables.GetPgcNpcConfig((int)type).HeadPath, Image.gameObject);
                Image.gameObject.SetActive(true);
                RemoteImage.gameObject.SetActive(false);
            }
            else
            {
                RemoteImage.Load(cover);
                RemoteImage.gameObject.SetActive(true);
                Image.gameObject.SetActive(false);
            }
        }
    }
}