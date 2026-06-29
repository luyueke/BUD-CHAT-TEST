using Com.TheFallenGames.OSA.Util.IO;
using Game.Props.PropsManagers;
using GameData.UGCData;
using System;
using System.Collections;
using System.Threading.Tasks.Sources;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameStageNpcItem : MonoBehaviour
    {
        public Transform On;

        public Text NameTxt;

        public Text ProgressTxt;

        public RemoteImageBehaviour Rm_Cover;

        public Image Head;

        public CButton Btn;

        [HideInInspector] public string ID;

        Func<string, bool> CanClick;
        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);

            On.gameObject.SetActive(false);
        }

        public void SetData(string id, Func<string, bool> canClick) { 
            ID = id;
            CanClick = canClick;

            NameTxt.text = AIPark_NpcUtil.GetName(id);

            Rm_Cover.gameObject.SetActive(false);
            Head.gameObject.SetActive(false);

            var cover = AIPark_NpcUtil.GetHead(id,Head);
            if (!string.IsNullOrEmpty(cover))
            {
                Rm_Cover.Load(cover);
                Rm_Cover.gameObject.SetActive(true);
            }
 
        }

        private void OnBtn() {
            if (On.gameObject.activeSelf) 
            {
                On.gameObject.SetActive(!On.gameObject.activeSelf);
                return;
            }
            if (CanClick(ID))
            {
                On.gameObject.SetActive(!On.gameObject.activeSelf);
            }
        }
    }
}