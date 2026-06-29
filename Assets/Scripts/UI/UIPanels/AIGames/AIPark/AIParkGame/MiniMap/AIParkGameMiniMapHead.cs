using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameMiniMapHead : MonoBehaviour
    {
        public CButton Btn;

        public Transform Flag;

        public RemoteImageBehaviour RM_Cover;

        public Image Head;

        public RectTransform Rect;

        [HideInInspector] public PlayerStateController Player;

        private Action<AIParkGameMiniMapHead> ClickAc;

        [HideInInspector] public bool allRefresh = false;
        private void Awake()
        {
            Btn.onClick.AddListener(OnBtn);
        }

        public void SetData(PlayerStateController _player,string _cover, Action<AIParkGameMiniMapHead> _click) {
            ClickAc = _click;
            Player = _player;
            if (Player == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            Head.gameObject.SetActive(false);
            RM_Cover.gameObject.SetActive(false);
            if (string.IsNullOrEmpty(_cover))
            {
                _cover = AIPark_NpcUtil.GetHead(Player.PlayerID, Head);
            }
            if (!string.IsNullOrEmpty(_cover))
            {
                RM_Cover.Load(_cover);
                RM_Cover.gameObject.SetActive(true);
            }
        }

        private void OnBtn() {
            ClickAc?.Invoke(this);
            if (ClickAc != null)
            {

            }
        }

        public void Update()
        {
            if (allRefresh) {
                Rect.anchoredPosition = AIParkGameMiniMapTool.GetUIPos(new Vector2(Player.transform.position.x, Player.transform.position.z));
            }
            else if (gameObject.activeSelf && Player != null && !Flag.gameObject.activeSelf)
            {
                Rect.anchoredPosition = AIParkGameMiniMapTool.GetUIPos(new Vector2(Player.transform.position.x, Player.transform.position.z) );
            }
        }
    }
}