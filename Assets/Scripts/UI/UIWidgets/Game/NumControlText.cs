/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-04 13:33:18
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-07 17:57:58
 * @ Description: 数字增减控制部件
 */

using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace UI.UIWidgets
{
    public class NumControlText : MonoBehaviour 
    {
        [SerializeField]private Text titleTxt;
        [SerializeField]private Text numTxt;
        [SerializeField]private CButton subBtn;
        [SerializeField]private CButton addBtn;
        [Header("配置选项")]
        [SerializeField]private string titleString = ""; // 标题文本
        [SerializeField]private int maxNum = 999; // 最大可控制数量
        [SerializeField]private int minNum = 0; // 最小可控制数量

        int currentNum = 0;
        Action<int> onAddAction;
        Action<int> onSubAction;

        private void Awake() 
        {
            subBtn.onClick.AddListener(OnSubClick);   
            addBtn.onClick.AddListener(OnAddClick);
        }

        void Start() 
        {
            SetTitle(titleString); 
        }

        public void AddOnAddListener(Action<int> callback)
        {
            onAddAction += callback;
        }

        public void AddOnSubListener(Action<int> callback)
        {
            onSubAction += callback;
        }

        public void SetTitle(string name)
        {
            titleTxt.SetLocalText(name);
        }

        public void SetNum(int num)
        {
            if (num <= maxNum && num >= minNum)
            {
                currentNum = num;
                numTxt.text = currentNum.ToString();
            }
            RefreshBtnState();
        }

        void RefreshBtnState()
        {
            subBtn.interactable = currentNum > minNum;
            addBtn.interactable = currentNum < maxNum;
        }

        void OnSubClick()
        {
            var nNum = currentNum - 1;
            SetNum(nNum);
            if (nNum >= minNum)
            {
                onSubAction?.Invoke(nNum);
            }
        }

        void OnAddClick()
        {
            var nNum = currentNum + 1;
            SetNum(nNum);
            if (nNum <= maxNum)
            {
                onAddAction?.Invoke(nNum);
            }
        }

        private void OnValidate() 
        {
#if UNITY_EDITOR
            if (titleTxt == null)
            {
                return;
            }
            titleTxt.text = titleString;
#endif    
        }
    }
}