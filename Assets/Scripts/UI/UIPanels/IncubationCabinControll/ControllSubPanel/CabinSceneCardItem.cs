using Com.TheFallenGames.OSA.Util.IO;
using Es;
using GameData.BaseInfo;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台场景列表 Item，展示单个场景方案的封面
    /// Date: 26-04-09
    /// </summary>
    public class CabinSceneCardItem : MonoBehaviour
    {
        [SerializeField] private GameObject DefaultTag;  // 保留字段（Prefab 中其他视觉元素可使用）
        [SerializeField] private GameObject SelectObj;   // 设备当前展示场景的选中指示器（仅同步成功后由 Adapter 驱动）
        [SerializeField] private Button ClickBtn;        // 点击选中按钮
        [SerializeField] private RemoteImageBehaviour IconImage;
        [SerializeField] private GameObject BoxIcon;      // 封面图区域根节点，特殊 Item 时隐藏
        [SerializeField] private GameObject GoBuyObj; // 去购买专属视觉对象，仅 GoBuyObj 类型时显示
        [SerializeField] private GameObject DataObj; // 去购买专属视觉对象，仅 GoBuyObj 类型时显示
        [SerializeField] private GameObject DefIcon;     // 默认场景专属图标对象，仅 DefaultScene 类型时显示

        /// <summary>点击 Item 时触发，携带对应场景数据，由父级绑定</summary>
        public Action<BoxSceneInfo> onSelectClick;

        private BoxSceneInfo _data;

        #region 初始化

        // 绑定选中按钮点击事件，触发时将当前场景数据传递给回调
        void Awake()
        {
            ClickBtn.onClick.AddListener(() => onSelectClick?.Invoke(_data));
        }

        #endregion

        #region 数据绑定

        /// <summary>
        /// 绑定场景数据并刷新显示：根据 specialType 切换封面图区域与各专属图标对象的显隐；
        /// 普通场景才加载远程封面图，特殊 Item 跳过以避免空 URL 错误。
        /// </summary>
        public void SetData(BoxSceneInfo data)
        {
            _data = data;

            bool isGoPurchase = data.specialType == BoxSceneSpecialType.GoPurchase;
            bool isDefaultScene = data.specialType == BoxSceneSpecialType.DefaultScene;

            // 特殊 Item 时隐藏封面图区域，普通场景时显示
            if (BoxIcon != null)
            {
                BoxIcon.SetActive(!isGoPurchase && !isDefaultScene);
            }

            // 仅 GoPurchase 时显示去购买对象
            if (GoBuyObj != null)
            {
                GoBuyObj.SetActive(isGoPurchase);
            }

            if (DataObj != null)
            {
                DataObj.SetActive(!isGoPurchase);
            }

            // 仅 DefaultScene 时显示默认场景图标
            if (DefIcon != null)
            {
                DefIcon.SetActive(isDefaultScene);
            }

            // 普通场景才加载远程封面图
            if (data.specialType == BoxSceneSpecialType.None)
            {
                IconImage.Load(data.cover);
            }
        }

        /// <summary>
        /// 由 Adapter 驱动选中指示器：仅当设备已同步该场景时显示，不随点击立即变化。
        /// 触发时机：CabinControllScenePanel 收到 OnBoxSceneSyncResult 后调用 Adapter.Refresh()。
        /// </summary>
        public void SetSelectState(bool isSelect)
        {
            if (SelectObj != null)
            {
                SelectObj.SetActive(isSelect);
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
