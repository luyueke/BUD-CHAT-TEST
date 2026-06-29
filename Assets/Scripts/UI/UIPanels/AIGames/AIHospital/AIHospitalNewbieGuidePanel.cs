using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Base;
using UI;
using Game.Utils;

namespace UI.UIPanels.AIGames.AIHospital
{
    public class AIHospitalNewbieGuidePanel : BasePanel<AIHospitalNewbieGuidePanel>
    {
        [Header("导航按钮")]
        [SerializeField] private Button btn_Prev;      // 上一页按钮
        [SerializeField] private Button btn_Next;      // 下一页按钮
        [SerializeField] private Button btn_Close;     // 关闭按钮

        [Header("引导页面设置")]
        [SerializeField] private GameObject[] pageContainers;  // 页面容器数组
        [SerializeField] private Toggle tog_DontShowAgain;    // "不再弹出"勾选框
        [SerializeField] private Text txt_PageIndicator;      // 页面指示器 (当前页/总页数)

        // 当前页面索引
        private int _currentPageIndex = 0;
        // 总页数
        private int _totalPages = 0;
        // 保存玩家选择的键
        private const string KEY_DONT_SHOW_AGAIN = "AIHospital_DontShowGuide";

        public override void OnCreate()
        {
            // 初始化按钮事件
            btn_Prev.onClick.AddListener(OnPrevButtonClick);
            btn_Next.onClick.AddListener(OnNextButtonClick);
            btn_Close.onClick.AddListener(OnCloseButtonClick);
            
            // 初始化"不再弹出"选项
            tog_DontShowAgain.onValueChanged.AddListener(OnDontShowAgainChanged);
            tog_DontShowAgain.isOn = PlayerPrefs.GetInt(KEY_DONT_SHOW_AGAIN, 0) == 1;
            
            // 获取总页数
            _totalPages = pageContainers.Length;
            
            // 初始页面设置
            _currentPageIndex = 0;
            UpdatePageDisplay();
            
            if (PlayerPrefs.GetInt(KEY_DONT_SHOW_AGAIN, 0) == 1)
            {
                CloseSelf();
                return;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 移除事件监听
            btn_Prev.onClick.RemoveAllListeners();
            btn_Next.onClick.RemoveAllListeners();
            btn_Close.onClick.RemoveAllListeners();
            tog_DontShowAgain.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// 更新页面显示
        /// </summary>
        private void UpdatePageDisplay()
        {
            // 更新页码显示
            txt_PageIndicator.text = $"{_currentPageIndex + 1}/{_totalPages}";
            
            // 更新页面显示
            for (int i = 0; i < pageContainers.Length; i++)
            {
                pageContainers[i].SetActive(i == _currentPageIndex);
            }
            
            // 更新导航按钮状态
            btn_Prev.gameObject.SetActive(_currentPageIndex > 0);
            btn_Next.gameObject.SetActive(_currentPageIndex < _totalPages - 1);
        }

        /// <summary>
        /// 上一页按钮点击
        /// </summary>
        private void OnPrevButtonClick()
        {
            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                UpdatePageDisplay();
            }
        }

        /// <summary>
        /// 下一页按钮点击
        /// </summary>
        private void OnNextButtonClick()
        {
            if (_currentPageIndex < _totalPages - 1)
            {
                _currentPageIndex++;
                UpdatePageDisplay();
            }
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void OnCloseButtonClick()
        {
            CloseSelf();
        }

        /// <summary>
        /// "不再弹出"选项变更
        /// </summary>
        private void OnDontShowAgainChanged(bool isOn)
        {
            // 保存玩家选择
            PlayerPrefs.SetInt(KEY_DONT_SHOW_AGAIN, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
