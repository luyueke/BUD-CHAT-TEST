using GameData.BaseInfo;
using Newtonsoft.Json;
using Pb.Game;
using System;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    /// 修改昵称页面
    /// </summary>
    public class ChangeVehicleBannerPanel : BasePanel<ChangeVehicleBannerPanel>
    {
        public Text PanelTip;
        public TextInputView nameEditBox;
        public CButton BackBtn;
        public LoadingButton SaveBtn;
        public Action<string> OnComplete { set; private get; }

        public override void OnCreate()
        {
            nameEditBox.filterEmoji = true;
            SaveBtn.onClick.AddListener(OnConfirmClick);
            BackBtn.onClick.AddListener(OnBackBtnClick);
        }

        private void OnConfirmClick()
        {
            if (string.IsNullOrEmpty(nameEditBox.Input))
            {
                TipPanel.ShowToast("输入不能为空");
                return;
            }

            var uid = AccountDataManager.Inst.Uid;
            // 经 GameVehicleManager 单一真源合并发送（保留娃娃机 capturedSlots，避免覆盖抓取持久化）
            GameVehicleManager.Inst.SyncSelfBannerText(nameEditBox.Input);
            GameVehicleManager.Inst.PlayerChangeBannerText(uid, nameEditBox.Input);
            CloseSelf();
        }

        private void OnBackBtnClick()
        {
            CloseSelf();
        }


        public override void OnShow(params object[] args)
        {
            if (args!= null && args.Length > 0)
            {
                var nickName = args[0] as string;

                if (!string.IsNullOrEmpty(nickName))
                {
                    nameEditBox.SetInputWithoutNotify(nickName);
                }
            }
        }
    }
}