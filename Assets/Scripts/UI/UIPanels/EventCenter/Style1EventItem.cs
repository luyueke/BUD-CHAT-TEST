using System.Collections.Generic;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Event
{
    public class Style1EventItem : BaseEventItem
    {
        private Image Img_EventIcon;
        private Text Txt_Progress;
        private Slider Progress;
        private CButton Btn_GoFinish;
        private GameObject Go_New;

        public override void BindUI()
        {
            base.BindUI();
            Txt_Progress = GameObjectEx.FindChildByName(this.transform, "Txt_Progress").GetComponent<Text>();
            Img_EventIcon = GameObjectEx.FindChildByName(this.transform, "Img_EventIcon").GetComponent<Image>();
            Progress = GameObjectEx.FindChildByName(this.transform, "Progress").GetComponent<Slider>();
            Btn_GoFinish = GameObjectEx.FindChildByName(this.transform, "Btn_GoFinish").GetComponent<CButton>();
            Go_New = GameObjectEx.FindChildByName(this.transform, "Go_New").gameObject;
            
            Btn_GoFinish.onClick.AddListener(() =>
            {
                EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_curData.skipType);
            });
        }

        public override void InitData(string taskId, TaskItemData data)
        {
            base.InitData(taskId, data);
            SetEventIcon((EventCenterSkipType)this._curData.skipType);
            SetProgress(this._curData.finishAmount, this._curData.targetAmount);
        }
        
        public void SetEventIcon(EventCenterSkipType Id)
        {
            var spriteName = "TaskItemIcon_" + (int)Id;
            Img_EventIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, spriteName, gameObject);
        }
        
        public void SetProgress(int FinishedCount, int TargetCount)
        {
            if (FinishedCount >= TargetCount)
            {
                Progress.value = 1;
                Txt_Progress.text = TargetCount + " / " + TargetCount;
                return;
            }


            float progress = FinishedCount * 1.0f / TargetCount * 1.0f;
            Progress.value = progress;
            Txt_Progress.text = FinishedCount + " / " + TargetCount;
        }

        public override void SetClaimState(TaskClaimState state)
        {
            base.SetClaimState(state);
            Go_New.SetActive(false);
            switch (state)
            {
                case TaskClaimState.Unable:
                    Btn_GoFinish.gameObject.SetActive(true);
                    if ((EventCenterSkipType)_curData.skipType == EventCenterSkipType.Login
                        ||(EventCenterSkipType)_curData.skipType == EventCenterSkipType.OnlineTime)
                    {
                        Btn_GoFinish.gameObject.SetActive(false);
                    }
                    break;
                case TaskClaimState.Enable:
                    Btn_GoFinish.gameObject.SetActive(false);
                    Go_New.SetActive(true);
                    break;
                case TaskClaimState.Finished:
                    Btn_GoFinish.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
