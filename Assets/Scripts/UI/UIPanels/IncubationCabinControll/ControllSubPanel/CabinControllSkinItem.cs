using Com.TheFallenGames.OSA.Util.IO;
using Game.BudBox;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台界面皮肤列表 Item，展示单个皮肤包的封面
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllSkinItem : MonoBehaviour
    {
        [SerializeField] private RemoteImageBehaviour CoverImage;  // 皮肤封面（远程加载）
        [SerializeField] private GameObject DefaultTag;            // 默认皮肤标识（isDefault == 1 时显示）
        [SerializeField] private Button SelectBtn;                 // 点击选中按钮

        /// <summary>点击 Item 时触发，携带对应皮肤数据，由父级面板绑定</summary>
        public Action<CabinCharacterBaseInfo> onSelectClick;

        private CabinCharacterBaseInfo _data;

        #region 初始化

        void Awake()
        {
            SelectBtn.onClick.AddListener(() =>
            {
                var currentSkinPackId = CabinBoxManager.Inst.GetCabinBudBoxData()?.deviceState.skinPackId ?? string.Empty;
                if (_data.skinPack[0].packId == currentSkinPackId)
                    return;
                onSelectClick?.Invoke(_data);
            });
        }

        #endregion

        #region 数据绑定

        /// <summary>
        /// 绑定皮肤数据并刷新显示：设置默认标识和封面图
        /// </summary>
        public void SetData(CabinCharacterBaseInfo data)
        {
            _data = data;

            if (DefaultTag != null)
            {
                var currentSkinPackId = CabinBoxManager.Inst.GetCabinBudBoxData()?.deviceState.skinPackId ?? string.Empty;
                DefaultTag.SetActive(data.skinPack[0].packId == currentSkinPackId);
            }

            // 先隐藏，加载完成后再显示，避免加载过程中露出残留图片
            CoverImage.gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(data.skinPack[0].cover))
            {
                CoverImage.Load(data.skinPack[0].cover, onCompleted: (fromCache, success) =>
                {
                    CoverImage.gameObject.SetActive(true);
                });
            }
        }

        /// <summary>
        /// 根据当前缓存数据重新刷新 DefaultTag 显示状态
        /// </summary>
        public void RefreshDefaultTag()
        {
            if (DefaultTag != null && _data != null)
            {
                var currentSkinPackId = CabinBoxManager.Inst.GetCabinBudBoxData()?.deviceState.skinPackId ?? string.Empty;
                DefaultTag.SetActive(_data.skinPack[0].packId == currentSkinPackId);
            }
        }

        /// <summary>
        /// 清理数据引用，在销毁前调用以防止悬挂引用
        /// </summary>
        public void ClearData()
        {
            _data = null;
        }

        #endregion
    }
}
