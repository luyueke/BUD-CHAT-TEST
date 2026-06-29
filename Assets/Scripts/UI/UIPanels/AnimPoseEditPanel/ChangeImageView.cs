using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class ChangeImageView : PoseBaseView
    {
        [SerializeField] private CButton selectBtn;
        [SerializeField] private GameObject imagePanel;
        [SerializeField] private CButton curBtn;
        [SerializeField] private CButton bodyBtn;

        [SerializeField] private Transform arrowDir;
        public Action<PoseImageType> ChangeImageModeAction { set; private get; }

        private void Awake()
        {
            selectBtn.onClick.AddListener(ChangeImageMode);
            curBtn.onClick.AddListener(OnCurrentImage);
            bodyBtn.onClick.AddListener(OnBodyImage);
        }

        private void ChangeImageMode()
        {
            SetImageVisibie(true);
        }

        private void OnCurrentImage()
        {
            SetImageVisibie(false);
            ChangeImageModeAction?.Invoke(PoseImageType.Current);
            selectBtn.GetComponentInChildren<Text>().text = curBtn.GetComponentInChildren<Text>().text;
            AnimDataManager.Inst.curImageMode = PoseImageType.Current;
        }


        private void OnBodyImage()
        {
            SetImageVisibie(false);
            ChangeImageModeAction?.Invoke(PoseImageType.WhiteBody);
            selectBtn.GetComponentInChildren<Text>().text = bodyBtn.GetComponentInChildren<Text>().text;
            AnimDataManager.Inst.curImageMode = PoseImageType.WhiteBody;
        }

        public void SetText(PoseImageType imageType)
        {
            var tempBtn = imageType == PoseImageType.Current ? curBtn : bodyBtn;
            selectBtn.GetComponentInChildren<Text>().text = tempBtn.GetComponentInChildren<Text>().text;
        }

        private void SetImageVisibie(bool isVisibie)
        {
            arrowDir.localEulerAngles = isVisibie ? new Vector3(0, 0, 180) : new Vector3(0, 0, -90);
            imagePanel.gameObject.SetActive(isVisibie);
        }
    }
}