using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Store;
using GameData.BaseInfo;
using Product;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditStageMusical : MonoBehaviour
    {
        public CButton AddBtn;

        public CButton DelBtn;

        public RemoteImageBehaviour RemoteIcon;

        public Image Icon;

        public Image IconMask;

        public Image IconBg;

        private AIParkUgcEditStage Root;

        public AICommonGameConfig_Musical Config_Musical;
        private void Awake()
        {
            AddBtn.onClick.AddListener(OnAddBtn);
            DelBtn.onClick.AddListener(OnDelBtn);
        }

        public void Init(AIParkUgcEditStage root)
        {
            Root = root;
        }

        public void SetData(AICommonGameConfig_Musical _Config_Musical) {
            Config_Musical = _Config_Musical;
            Icon.gameObject.SetActive(false);
            RemoteIcon.gameObject.SetActive(false);
            if (Config_Musical == null)
            {
                IconMask.gameObject.SetActive(false);
                IconBg.gameObject.SetActive(false);
                AddBtn.gameObject.SetActive(true);
                DelBtn.gameObject.SetActive(false);
            }
            else
            {
                IconMask.gameObject.SetActive(true);
                IconBg.gameObject.SetActive(true);
                DelBtn.gameObject.SetActive(true);
                AddBtn.gameObject.SetActive(false);
                if (Config_Musical.isUgc)
                {
                    RemoteIcon.Load(_Config_Musical.icon, onCompleted: (bool fromCache, bool success) =>
                    {
                        if (this == null) return;
                        RemoteIcon.gameObject.SetActive(true);
                    });
                }
                else
                {
                    var sprite = PgcUtils.GetIconSpriteByPgcId(Config_Musical.id, gameObject);
                    if (sprite != null) 
                    {
                        Icon.sprite = sprite;
                        Icon.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnAddBtn() {
            Root.musicalGroup.gameObject.SetActive(true);
        }

        private void OnDelBtn()
        {
            SetData(null);
        }
    }
}