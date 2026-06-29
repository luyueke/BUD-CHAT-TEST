using System;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class PoseBindNodeItem : MonoBehaviour
    {
        [SerializeField] private GameObject selectIcon;
        [SerializeField] private GameObject vipIcon;
        [SerializeField] private Button selectBtn;
        [SerializeField] private Text contentTip;
        private int curIndex;
        private Action<int> OnSelect;
        private void Awake()
        {
            selectBtn.onClick.AddListener(OnItemClick);
        }


        public void UpdateData(int index, string content, Action<int> select)
        {
            curIndex = index;
            contentTip.text = content;
            OnSelect = select;
            vipIcon.SetActive(curIndex > 0);
        }

        private void OnItemClick()
        {
            OnSelect?.Invoke(curIndex);
        }

        public void SetSelectIconVisible(bool isVisible)
        {
            selectIcon.SetActive(isVisible);
        }

    }
}