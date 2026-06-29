using Game.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

namespace AIGame.Base
{
    public class AIParkGameChooseNpc : BasePanel<AIParkGameChooseNpc>
    {
        public CButton CloseBtn;

        public CButton RangBtn;

        public CButton ConfirmBtn;

        public Transform ItemParent;

        public AIParkGameStageNpcItem Item;

        private List<AIParkGameStageNpcItem> ItemLs = new();

        private int NpcCount;

        private Action<List<string>> ConfirmAc;
        protected override void Awake()
        {
            base.Awake();
            RangBtn.onClick.AddListener(OnRangBtn);
            CloseBtn.onClick.AddListener(OnRangBtn);
            ConfirmBtn.onClick.AddListener(OnConfirmBtn);
        }

        public void SetData(int count, Action<List<string>> confirmAc) {
            NpcCount = count;
            ConfirmAc = confirmAc;

            var player = AIBuddyAvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < player.Count; i++)
            {
                if (i >= ItemLs.Count)
                {
                    var obj = GameObject.Instantiate(Item, ItemParent).GetComponent<AIParkGameStageNpcItem>();
                    ItemLs.Add(obj);
                }
                ItemLs[i].SetData(player[i].Value.PlayerID, CanClick);
            }

            //var ls = new List<string>();
            //foreach (var item in player) {
            //    if (!int.TryParse(item.Value.PlayerID,out int id))
            //    {
            //        ls.Add(item.Value.PlayerID);
            //    }
            //}
            //MsgUtils.GetNpcInfoLs(ls, (playerData) => {
            //    for (var i = 0; i < playerData.Count; i++) 
            //    {
            //        ItemLs[i].RefreshView(playerData[i]);
            //    }
            //});

            Item.gameObject.SetActive(false);
        }

        private bool CanClick(string id) {
            var count = NpcCount;
            foreach (var item in ItemLs)
            {
                if (item.On.gameObject.activeSelf)
                {
                    count--;
                }
            }
            if (count > 0)
            {
                return true;
            }
            else
            {
                TipPanel.ShowToast("邀请同伴数量不可超过非主控乐器数量！");
                return false;
            }
        }

        private void OnConfirmBtn()
        {
            var ls = new List<string>();
            foreach (var item in ItemLs)
            {
                if (item.On.gameObject.activeSelf)
                {
                    ls.Add(item.ID);
                }
            }
            if (ls.Count != NpcCount)
            {
                TipPanel.ShowToast("邀请同伴数量不足");
                return;
            }
            ConfirmAc?.Invoke(ls);
            CloseSelf();
        }

        private void OnRangBtn() {
            var ls = new List<string>();
            var player = AIBuddyAvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < NpcCount; i++)
            {
                if (i < player.Count)
                {
                    ls.Add(player[i].Value.PlayerID);
                }
            }
            ConfirmAc?.Invoke(ls);
            CloseSelf();
        }
    }
}