using System.Collections.Generic;
using GameData.BaseInfo;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class CabinPublishUgcAnimTonePanel : BasePanel<CabinPublishUgcAnimTonePanel>
    {
        public Button backBtn;
        public CreatToneView creatToneView;
        public CabinPublishToneView publishToneView;

        private CabinCharacterUgcInfo cabinCharacter;

        public override void OnCreate()
        {
            base.OnCreate();
            backBtn.onClick.AddListener(OnBackBtnClick);

            creatToneView.onGoToPublish = OnGoToPublish;
            creatToneView.Init();
            publishToneView.onPublishSuccess = OnPublishSuccess;
            publishToneView.onGoToCreatTone = OnGoToCreatTone;
            publishToneView.Init();
            creatToneView.gameObject.SetActive(true);
            publishToneView.gameObject.SetActive(false);
        }

        public override void OnShow(params object[] args)
        {
            if (args != null && args.Length >= 1 && args[0] is CabinCharacterUgcInfo characterInfo)
            {
                this.cabinCharacter = characterInfo;
            }
            creatToneView.RefreshCloneCountUI();
        }

        /// <summary>
        /// 面板隐藏时取消正在进行的录制，清理 CabinToneNetManager 中的 recordingClip，
        /// 并重置 CreatToneView 的录制 UI，防止下次进入时无法录制或 UI 状态异常。
        /// </summary>
        public override void OnHidden()
        {
            base.OnHidden();
            CabinToneNetManager.Inst.CancelRecordVoice();
            creatToneView.ResetRecordingUI();
        }

        private void OnGoToPublish(string metaDataUrl, List<ToneLanguageData> languageList)
        {
            creatToneView.gameObject.SetActive(false);
            publishToneView.gameObject.SetActive(true);
            publishToneView.SetData(metaDataUrl, languageList);
        }

        private void OnGoToCreatTone()
        {
            publishToneView.gameObject.SetActive(false);
            creatToneView.gameObject.SetActive(true);
        }

        /// <summary>
        /// Back 按钮点击处理：若当前在发布音色子视图，则返回创建音色子视图；否则关闭整个面板。
        /// </summary>
        private void OnBackBtnClick()
        {
            if (publishToneView.gameObject.activeSelf)
            {
                OnGoToCreatTone();
            }
            else
            {
                CloseSelf();
            }
        }

        private void OnPublishSuccess()
        {
            CloseSelf();
            var panel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
            if (panel != null)
            {
                panel.RefreshOwnedList();
            }
        }
    }
}
