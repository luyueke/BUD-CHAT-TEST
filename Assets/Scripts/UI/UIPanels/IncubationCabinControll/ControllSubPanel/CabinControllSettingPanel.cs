using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>结算面板内子面板类型枚举</summary>
public enum SettleSubPanelType { Experience, Net, Updata, Rest, Explain }

/// <summary>
/// 结算子面板基类：子面板无需知道其他子面板的存在，导航逻辑统一由父级 CabinControllSettingPanel 通过 GoToggle 管理
/// </summary>
public abstract class CabinControllSettleSubPanel : MonoBehaviour
{
}

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台结算子面板，通过顶部 GoToggleGroup 统一管理 5 个子面板的切换，
    ///       子面板自身只负责功能逻辑，不持有导航按钮
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllSettingPanel : MonoBehaviour
    {
        [SerializeField] private Button BackBtn;       // 关闭整个BOX面板按钮
        [SerializeField] private Button ShowBtn;       // 返回控制台主视图按钮
        [SerializeField] private Button NewLinkBoxBtn; // 新增BOX Box
        [SerializeField] private Button UnLinkBoxBtn;  // 解除BOX Box

        [Header("子面板")]
        [SerializeField] private CabinControllExperiencePanel ExperiencePanel; // 体验设置界面
        [SerializeField] private CabinControllNetPanel        NetPanel;        // 网络设置界面
        [SerializeField] private CabinControllUpdataPanel     UpdataPanel;     // 硬件升级界面
        [SerializeField] private CabinControllRestPanel       RestPanel;       // 恢复出厂设置界面
        [SerializeField] private CabinControllExplainPanel    ExplainPanel;    // 关于BOX界面

        [Header("导航 GoToggle（GoToggleGroup 互斥）")]
        [SerializeField] private GoToggle ExperienceTg; // 体验设置 GoToggle
        [SerializeField] private GoToggle NetTg;        // 网络设置 GoToggle
        [SerializeField] private GoToggle UpdataTg;     // 硬件升级 GoToggle
        [SerializeField] private GoToggle RestTg;       // 恢复出厂 GoToggle
        [SerializeField] private GoToggle ExplainTg;    // 关于BOX GoToggle

        // 由父级 IncubationCabinControll 注入的回调
        public Action onBackClick;
        public Action onShowClick;
        public Action onNewLinkBoxClick;
        public Action onUnLinkBoxClick;

        /// <summary>子面板类型 → 子面板 MonoBehaviour 的映射</summary>
        private Dictionary<SettleSubPanelType, CabinControllSettleSubPanel> _panelMap;

        /// <summary>子面板类型 → 对应 GoToggle 的映射</summary>
        private Dictionary<SettleSubPanelType, GoToggle> _toggleMap;

        #region 初始化

        /// <summary>
        /// 绑定按钮与 Toggle 事件，在 OnCreate 阶段调用一次
        /// </summary>
        public void InitUI()
        {
            BackBtn.onClick.AddListener(() => onBackClick?.Invoke());
            ShowBtn.onClick.AddListener(() => onShowClick?.Invoke());
            NewLinkBoxBtn.onClick.AddListener(() => onNewLinkBoxClick?.Invoke());
            UnLinkBoxBtn.onClick.AddListener(() => onUnLinkBoxClick?.Invoke());

            // 建立子面板映射
            _panelMap = new Dictionary<SettleSubPanelType, CabinControllSettleSubPanel>
            {
                { SettleSubPanelType.Experience, ExperiencePanel },
                { SettleSubPanelType.Net,        NetPanel        },
                { SettleSubPanelType.Updata,     UpdataPanel     },
                { SettleSubPanelType.Rest,        RestPanel      },
                { SettleSubPanelType.Explain,    ExplainPanel    },
            };

            // 建立 GoToggle 映射
            _toggleMap = new Dictionary<SettleSubPanelType, GoToggle>
            {
                { SettleSubPanelType.Experience, ExperienceTg },
                { SettleSubPanelType.Net,        NetTg        },
                { SettleSubPanelType.Updata,     UpdataTg     },
                { SettleSubPanelType.Rest,       RestTg       },
                { SettleSubPanelType.Explain,    ExplainTg    },
            };

            // 为每个 GoToggle 注册切换事件：isOn 时切换到对应子面板
            foreach (var kv in _toggleMap)
            {
                var type = kv.Key;
                kv.Value.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        OpenPanel(type);
                    }
                });
            }
        }

        #endregion

        #region 显示 / 隐藏

        /// <summary>
        /// 显示结算面板，默认打开体验设置子面板
        /// </summary>
        public void OnShow()
        {
            OpenPanel(SettleSubPanelType.Experience);
        }

        /// <summary>
        /// 切换到指定类型的子面板，并同步 Toggle 选中状态
        /// </summary>
        /// <param name="type">目标子面板类型</param>
        private void OpenPanel(SettleSubPanelType type)
        {
            // 隐藏所有子面板
            foreach (var p in _panelMap.Values)
            {
                p.gameObject.SetActive(false);
            }

            // 显示目标子面板
            _panelMap[type].gameObject.SetActive(true);

            // 同步 GoToggle 选中状态，使用 Set(value, false) 避免触发回调循环
            foreach (var kv in _toggleMap)
            {
                kv.Value.Set(kv.Key == type, false);
            }
        }

        /// <summary>
        /// 预留：隐藏时清理逻辑待实现
        /// </summary>
        public void OnHide() { }

        #endregion
    }
}
