using Game.Avatar;
using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameMiniMap : MonoBehaviour
    {
        private static AIParkGameMiniMap ins;
        public static AIParkGameMiniMap Ins => ins;

        public RectTransform View;

        public RectTransform MiniMap;

        public Image Bg;

        public Transform ItemGroup;

        public AIParkGameMiniMapHead Head;

        public CButton Btn;

        [HideInInspector] public List<AIParkGameMiniMapHead> HeadLs = new List<AIParkGameMiniMapHead>();

        [HideInInspector] public AIParkGameMiniMapHead FlagHead;

        [HideInInspector] public AIParkGameMiniMapHead SelfHead;

        private void Awake()
        {
            ins = this;

            AIParkGameMiniMapTool.UISize = MiniMap.sizeDelta;

            Head.gameObject.SetActive(false);

            Btn.onClick.AddListener(OnBtn);

            //if (AIParkGameUgcsetTool.gameConfig != null && !string.IsNullOrEmpty(AIParkGameUgcsetTool.gameConfig.plantColor))
            //{
            //    if (ColorUtility.TryParseHtmlString("#" + AIParkGameUgcsetTool.gameConfig.plantColor, out var c))
            //    {
            //        Bg.gameObject.SetActive(true);
            //        Bg.color = c;
            //    }
            //}
            //else
            {
                Bg.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (gameObject.activeSelf && SelfHead != null)
            {
                MiniMap.anchoredPosition = new Vector2(-SelfHead.Rect.anchoredPosition.x, -SelfHead.Rect.anchoredPosition.y);
                if (FlagHead != null)
                {
                    var pos = AIParkGameMiniMapTool.GetUIPos(new Vector2(FlagHead.Player.transform.position.x, FlagHead.Player.transform.position.z));
                    var kuang = new Vector2(View.sizeDelta.x - FlagHead.Rect.sizeDelta.x, View.sizeDelta.y - FlagHead.Rect.sizeDelta.y);
                    kuang = View.sizeDelta;
                    if (pos.x < SelfHead.Rect.anchoredPosition.x - kuang.x / 2)
                    {
                        pos.x = SelfHead.Rect.anchoredPosition.x - kuang.x / 2;
                    }
                    if (pos.x > SelfHead.Rect.anchoredPosition.x + kuang.x / 2)
                    {
                        pos.x = SelfHead.Rect.anchoredPosition.x + kuang.x / 2;
                    }
                    if (pos.y < SelfHead.Rect.anchoredPosition.y - kuang.y / 2)
                    {
                        pos.y = SelfHead.Rect.anchoredPosition.y - kuang.y / 2;
                    }
                    if (pos.y > SelfHead.Rect.anchoredPosition.y + kuang.y / 2)
                    {
                        pos.y = SelfHead.Rect.anchoredPosition.y + kuang.y / 2;
                    }
                    FlagHead.Rect.anchoredPosition = pos;
                }
            }
        }

        private void OnEnable()
        {
            RefreshHead();
        }

        public void SetFlagHead(string _playerID) {
            foreach (var item in HeadLs)
            {
                if (item.gameObject.activeSelf && item.Player != null && item.Player.PlayerID == _playerID)
                {
                    OnClick(item);
                    break;
                }
            }
        }

        public void RefreshHead()
        {
            foreach (var item in HeadLs)
            {
                item.gameObject.SetActive(false);
            }

            var player = AIBuddyAvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < player.Count; i++)
            {
                var item = CreatHead();
                item.SetData(player[i].Value, "", null);
                if (player[i].Value.PlayerID == AccountDataManager.Inst.UserInfo.uid)
                {
                    SelfHead = item;
                }
            }

            player = AvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < player.Count; i++)
            {
                var item = CreatHead();
                item.SetData(player[i].Value, "", null);
                if (player[i].Value.PlayerID == AccountDataManager.Inst.UserInfo.uid)
                {
                    SelfHead = item;
                }
            }

            SelfHead?.gameObject.transform.SetAsLastSibling();
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

        private void OnBtn()
        {
            UIManager.Inst.OpenPanel(PanelId.AIParkGameMiniMapPanel);
        }

        private void OnClick(AIParkGameMiniMapHead _head)
        {
            if (_head == FlagHead)
            {
                FlagHead.Flag.gameObject.SetActiveValid(false);
                FlagHead = null;
                AvatarController.Inst.SelfStateController.TargetPlayer = null;
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
            AvatarController.Inst.SelfStateController.TargetPlayer = FlagHead.Player;
        }
    }
}