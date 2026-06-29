using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 草稿/已发布操作菜单的通用基类。
    /// 封装预览卡片、时间标签、关闭逻辑与数据校验，子类只需处理各自的功能按钮。
    /// </summary>
    public abstract class BaseDraftBoxMenu : MonoBehaviour
    {
        public Button Btn_Background;                           // 点击空白处关闭菜单
        [FormerlySerializedAs("EditBoxItem")]
        public CabinCharacterCardItem PreviewItem;              // 菜单顶部预览卡片（仅展示，不可点击）
        public Text NameTxt;                                    // 角色名称
        public Text Txt_LastEditTime;                           // 最后编辑时间

        public GameObject Obj_Default;                          // 缺省状态容器（无数据时显示）
        public GameObject Obj_Content;                          // 正常内容容器（有数据时显示）

        protected CabinCharacterUgcInfo characterInfo;                       // 当前操作的角色数据
        protected bool _isRequesting;                           // 网络请求进行中，防止重复触发


        /// <summary>绑定关闭按钮，子类覆写时须调用 base.InitUI()</summary>
        public virtual void InitUI()
        {
            Btn_Background.onClick.AddListener(()=> 
            {
                HidePanel();
            });
        }

        /// <summary>
        /// 显示菜单并刷新预览卡片与时间标签，子类覆写时须调用 base.UpdateInfo(data)。
        /// 同时重置 _isRequesting，防止切换数据后按钮被上一次请求的标志卡死。
        /// </summary>
        public virtual void UpdateInfo(CabinCharacterUgcInfo data)
        {
            _isRequesting = false;
            characterInfo = data;
            if (Obj_Default != null) Obj_Default.SetActive(false);
            if (Obj_Content != null) Obj_Content.SetActive(true);
            PreviewItem.SetData(data);
            NameTxt.text = data.GetName();
            Txt_LastEditTime.SetLocalText("最后编辑 {0}",
                TimestampConverter.ConvertToDateTimeString(data.updateTime));
        }


        /// <summary>收起内容区，切换到缺省状态（不清数据，供操作按钮关闭时使用）</summary>
        protected virtual void HidePanel()
        {
            //if (Obj_Default != null) Obj_Default.SetActive(true);
            //if (Obj_Content != null) Obj_Content.SetActive(false);
        }

        /// <summary>数据清空时进入缺省状态：清空数据并切换到缺省显示</summary>
        public virtual void ClearInfo()
        {
            characterInfo = null;
            if (Obj_Default != null) Obj_Default.SetActive(true);
            if (Obj_Content != null) Obj_Content.SetActive(false);
        }

        /// <summary>校验 _data 是否合法；非法时打印错误并返回 true，调用方应立即 return</summary>
        protected bool CheckDataIllegal()
        {
            if (characterInfo == null)
            {
                LoggerUtils.LogError($"{GetType().Name} - _data 为空");
                return true;
            }
            return false;
        }
    }
}