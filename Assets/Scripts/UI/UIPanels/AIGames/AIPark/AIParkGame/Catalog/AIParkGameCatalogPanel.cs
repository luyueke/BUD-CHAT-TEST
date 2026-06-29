using Game.Avatar;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkGameCatalogPanel : BasePanel<AIParkGameCatalogPanel>
    {
        public CButton CloseButton;

        public GameObject MdelRoot; // 用于放置角色模型的根节点
        public AvatarCameraController AvatarCameraController;
        private CharacterWrap characterWrap;

        public AIParkGameCatalogItem Item;
        public GameObject ItemParent;

        public Text NameTxt;
        public Text Desc;
        public Text Plot;

        [HideInInspector] public Action<bool> redpointEvent;

        private List<AIParkGameCatalogItem> ItemList = new List<AIParkGameCatalogItem>();

        private Dictionary<string,CharacterWrap> WarpDic = new Dictionary<string,CharacterWrap>();
        public override void OnCreate()
        {
            base.OnCreate();
            CloseButton.onClick.AddListener(CloseSelf);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
        }

        public override void OnHidden()
        {
            redpointEvent?.Invoke(false);
            base.OnHidden();
        }

        public void RefreshView() {
            var ls = AIBuddyAvatarController.Inst.GetDic().ToList();
            for (int i = 0; i < ls.Count; i++)
            {
                var obj = GameObject.Instantiate(Item, ItemParent.transform).GetComponent<AIParkGameCatalogItem>();
                obj.SetData(this,ls[i].Value.PlayerID);
                ItemList.Add(obj);
            }
            ItemList[0].Tog.isOn = true;
            Item.gameObject.SetActive(false);
        }

        protected override void Start()
        {
            base.Start();

            RefreshView();
        }

        public void ShowInfo(string playerID) {
            NameTxt.text = AIPark_NpcUtil.GetName(playerID);
            Desc.text = AIPark_NpcUtil.GetDesc(playerID);
            Plot.text = AIPark_NpcUtil.GetPlot(playerID);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Desc.transform.parent as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(Plot.transform.parent as RectTransform);

            if (characterWrap != null && characterWrap.Avatar != null)
            {
                characterWrap.Avatar.gameObject.SetActive(false);
            }
            
            if (!WarpDic.ContainsKey(playerID))
            {
                // 创建角色
                CharacterData avatarInfo = CharacterData.DeserializeObject(AIPark_NpcUtil.GetAvatar(playerID));
                characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
                characterWrap.SetParent(MdelRoot.transform, true);
            
                WarpDic.Add(playerID, characterWrap);
            }
            
            characterWrap = WarpDic[playerID];
            if (characterWrap.Avatar != null)
            {
                characterWrap.Avatar.gameObject.SetActive(true);
            }
            AvatarCameraController.RotateTarget = characterWrap.Avatar.gameObject.transform;
        }

    }
}