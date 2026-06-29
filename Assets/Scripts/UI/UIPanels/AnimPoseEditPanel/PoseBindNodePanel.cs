using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using GameData.PgcData;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class PoseBindNodePanel : BasePanel<PoseBindNodePanel>
    {
        public PoseBindNodeItem nodeItem;
        public Button CloseBtn;
        public Button DownBtn;
        public Action<int> OnSelect { private get; set; }
        private int curSelectIndex = -1;
        private List<PoseBindNodeItem> itemNodes;//索引对应的bindIndex
        public override void OnCreate()
        {
            base.OnCreate();
            CloseBtn.onClick.AddListener(CloseSelf);
            DownBtn.onClick.AddListener(OnDownClick);
            DownBtn.interactable = false;
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            UgcPoseSubType poseType = (UgcPoseSubType)args[0];
            var bindIndex = (int)args[1];
            var poseModeData = DataTables.GetPoseModeConfig((int)poseType);
            itemNodes = new List<PoseBindNodeItem>();
            for (var i = 0; i < poseModeData.BindNames.Count; i++)
            {
                var clone = GameObject.Instantiate(nodeItem, nodeItem.transform.parent);
                clone.gameObject.SetActive(true);
                clone.UpdateData(i,poseModeData.BindNames[i],OnSelectClick);
                itemNodes.Add(clone);
            }

            OnSelectClick(bindIndex);
        }

        private void OnSelectClick(int index)
        {
            if (curSelectIndex >= 0 && curSelectIndex < itemNodes.Count)
            {
                itemNodes[curSelectIndex].SetSelectIconVisible(false);
            }
            curSelectIndex = index;
            itemNodes[curSelectIndex].SetSelectIconVisible(true);
            DownBtn.interactable = true;
        }

        private void OnDownClick()
        {
            OnSelect?.Invoke(curSelectIndex);
            CloseSelf();
        }
    }
}
