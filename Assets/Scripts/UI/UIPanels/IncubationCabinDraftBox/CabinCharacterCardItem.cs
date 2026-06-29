using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    /// <summary>
    /// Author:
    /// Desc: budbox 角色卡面item
    ///       订阅 MessageName.OnCabinCharacterUpdated 事件，在数据局部变更时按类型刷新对应 UI，
    ///       无需重建整个列表。
    /// Date:26-04-01
    /// </summary>
    public class CabinCharacterCardItem : IncubationDraftBaseItem
    {
        [SerializeField] private Image CardImgEx;                   // 卡面底图

        [Header("新建卡 / 数据卡 双态根节点")]
        [SerializeField] private GameObject DataRoot;              // 有数据时显示：卡面所有数据视图（含选中框/徽章）
        [SerializeField] private GameObject CreatRoot;             // 新建态显示：内含创建按钮
        [SerializeField] private Button CreatDraftBoxBtn;          // 新建按钮（CreatRoot 下）

        /// <summary>
        /// 切到"新建"态：隐藏数据视图（DataRoot），显示新建视图（CreatRoot），并绑定新建按钮点击。
        /// 由 OSA 列表第一个 Cell（占位项 characterInfo==null）触发，走创建角色流程。
        /// </summary>
        /// <param name="onCreate">点击新建按钮时触发的回调</param>
        public void SetCreateMode(Action onCreate)
        {
            // 清掉从数据态复用残留的角色数据与更新事件订阅，避免隐藏的 DataRoot 仍响应数据变更
            ClearData();

            if (DataRoot != null)
            {
                DataRoot.SetActive(false);
            }

            if (CreatRoot != null)
            {
                CreatRoot.SetActive(true);
            }

            if (CreatDraftBoxBtn != null)
            {
                CreatDraftBoxBtn.onClick.RemoveAllListeners();
                CreatDraftBoxBtn.onClick.AddListener(() => onCreate?.Invoke());
            }
        }

        /// <summary>
        /// 切到"数据"态：显示数据视图（DataRoot），隐藏新建视图（CreatRoot）。
        /// 适配器在调用基类 SetData 前先调用本方法，确保 Cell 从新建态复用为数据态时视图正确。
        /// </summary>
        public void SetDataMode()
        {
            if (CreatRoot != null)
            {
                CreatRoot.SetActive(false);
            }

            if (DataRoot != null)
            {
                DataRoot.SetActive(true);
            }
        }

        protected override void SetCardColor(DraftBoxCardColorConfig config)
        {
            if (config==null)
            {
                return;
            }
            base.SetCardColor(config);
            var pathEx = $"Assets/Loadable/UI/UIPanel/IncubationCabinDraftBox/CardSprite/{config.Color}_Ex.png";
            var spriteEx = XAssetLoaderMgr.Inst.LoadResource<Sprite>(pathEx, gameObject);
            if (spriteEx != null)
                CardImgEx.sprite = spriteEx;
        }
        protected override void UpdateName()
        {
            base.UpdateName();
            var data = (CabinCharacterUgcInfo)_data;
            bool showPubName = data.ugcclass != (int)UGCClass.Draft && !string.IsNullOrEmpty(data.name);
            string name = data.GetName();

            NameTxt.text = CabinTools.TruncateName(name, 7);
        }
    }
}

