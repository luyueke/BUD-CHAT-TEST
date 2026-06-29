using Com.TheFallenGames.OSA.Util.IO;
using Game.Avatar;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameMiniMapPanel : BasePanel<AIParkGameMiniMapPanel>
    {
        public CButton CloseBtn;

        public Transform SwingLight;

        public RectTransform Parent;

        public RectTransform BgRect;

        public RectTransform MiniMap;

        public Image BgPlant;

        public Transform ItemGroup;

        public AIParkGameMiniMapHead Head;

        public RemoteImageBehaviour Paster;

        [HideInInspector] public List<AIParkGameMiniMapHead> HeadLs = new List<AIParkGameMiniMapHead>();

        [HideInInspector] public AIParkGameMiniMapHead FlagHead;

        [HideInInspector] public AIParkGameMiniMapHead SelfHead;

        public float x;
        public float y;
        public override void OnCreate()
        {
            base.OnCreate();
            SwingLight.gameObject.SetActive(false);
            Head.gameObject.SetActive(false);
            CloseBtn.onClick.AddListener(OnCloseClick);
        }

        public void OnCloseClick()
        {
            base.CloseSelf();
            AIParkGuideMgr.Inst.TriggerNextStepWithCheck((int)AIParkGuide1_3.GuideStep1_3.Guide_FirstInGame_Guide_1_3_3);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.scene != null)
            {
                for (int i = 0; i < AIParkGameUgcsetTool.gameConfig.scene.pasterUrls.Count; i++)
                {
                    var item = AIParkGameUgcsetTool.gameConfig.scene.pasterUrls[i];
                    var v = GameObject.Instantiate(Paster, Parent).GetComponent<RemoteImageBehaviour>();
                    v.Load(item.url);
                    (v.transform as RectTransform).anchoredPosition = new Vector2(item.x, item.y);
                    v.transform.localScale = Vector3.one * item.scale;
                    v.gameObject.SetActive(true);
                }
            }
            //if (AIParkGameUgcsetTool.gameConfig != null && !string.IsNullOrEmpty(AIParkGameUgcsetTool.gameConfig.plantColor))
            //{
            //    if (ColorUtility.TryParseHtmlString("#" + AIParkGameUgcsetTool.gameConfig.plantColor, out var c))
            //    {
            //        BgPlant.gameObject.SetActive(true);
            //        BgPlant.color = c;
            //    }
            //}
            //else
            {
                BgPlant.gameObject.SetActive(false);
            }
            RefreshHead();
        }

        public void RefreshHead()
        {
            if (AIParkGameUgcsetTool.gameConfig != null && AIParkGameUgcsetTool.gameConfig.scene != null)
            {
                var v2 = new Vector2(25, 25);
                BgRect.sizeDelta = new Vector2(1522.4f, 924.2f) + v2 * AIParkGameUgcsetTool.gameConfig.scene.width;
                if (!string.IsNullOrEmpty(AIParkGameUgcsetTool.gameConfig.scene.color) && ColorUtility.TryParseHtmlString("#" + AIParkGameUgcsetTool.gameConfig.scene.color, out var c))
                {
                    BgRect.GetComponent<Image>().color = c;
                }
            }


            foreach (var item in HeadLs)
            {
                item.gameObject.SetActive(false);
            }

            var player = AIBuddyAvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < player.Count; i++)
            {
                var item = CreatHead();
                item.SetData(player[i].Value, "", OnClick);
                if (player[i].Value.PlayerID == AccountDataManager.Inst.UserInfo.uid)
                {
                    SelfHead = item;
                }
            }

            player = AvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < player.Count; i++)
            {
                var item = CreatHead();
                item.SetData(player[i].Value, "", OnClick);
                if (player[i].Value.PlayerID == AccountDataManager.Inst.UserInfo.uid)
                {
                    SelfHead = item;
                }
            }

            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            if (guestPanel.guestGroup.MiniMap.FlagHead != null)
            {
                var tem = guestPanel.guestGroup.MiniMap.FlagHead;
                foreach (var item in HeadLs)
                {
                    item.allRefresh = true;
                    item.Flag.gameObject.SetActiveValid(false);
                    if (item.Player != null && item.gameObject.activeSelf && item.Player.PlayerID == tem.Player.PlayerID)
                    {
                        FlagHead = item;
                        FlagHead.Flag.gameObject.SetActiveValid(true);
                        FlagHead.gameObject.transform.SetAsLastSibling();
                        SelfHead.gameObject.transform.SetAsLastSibling();
                    }
                }

            }

            SelfHead.gameObject.transform.SetAsLastSibling();
        }

        private AIParkGameMiniMapHead CreatHead()
        {
            foreach (var item in HeadLs)
            {
                if (!item.gameObject.activeSelf)
                {
                    return item;
                }
            }
            var obj = GameObject.Instantiate(Head, ItemGroup).GetComponent<AIParkGameMiniMapHead>();
            HeadLs.Add(obj);
            return obj;
        }

        private void OnClick(AIParkGameMiniMapHead _head)
        {
            if (_head == SelfHead)
            {
                return;
            }
            if (_head == FlagHead)
            {
                var panel2 = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
                panel2.guestGroup.MiniMap.SetFlagHead(FlagHead.Player.PlayerID);
                FlagHead.Flag.gameObject.SetActiveValid(false);
                FlagHead = null;
                return;
            }
            FlagHead = _head;
            foreach (var item in HeadLs)
            {
                item.Flag.gameObject.SetActiveValid(false);
            }
            FlagHead.Flag.gameObject.SetActiveValid(true);

            FlagHead.gameObject.transform.SetAsLastSibling();
            SelfHead.gameObject.transform.SetAsLastSibling();

            var panel = UIManager.Inst.FindPanel<AIParkGuestPanel>(PanelId.AIParkGuestPanel);
            panel.guestGroup.MiniMap.SetFlagHead(FlagHead.Player.PlayerID);
        }
        /// <summary>
        /// 显示引导高亮 秋千位置
        /// </summary>
        public void ShowGuideHighLightSwing()
        {
            SwingLight.gameObject.SetActive(true);
        }
    }
}