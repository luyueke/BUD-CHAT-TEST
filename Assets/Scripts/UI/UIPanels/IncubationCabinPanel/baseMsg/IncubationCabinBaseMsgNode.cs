using UI.UIWidgets;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 孵化舱角色基础信息节点，负责展示和编辑角色名称、背景故事、音色。
    /// </summary>
    public class IncubationCabinBaseMsgNode : MonoBehaviour
    {
        [SerializeField] internal TextInputView eitNameBox;
        [SerializeField] internal TextInputView editBackgroundStoryBox;
        [SerializeField] internal IncubationToneShopItem incubationToneShopItem;

        private IncubationCabinPanel _panel;

        /// <summary>当前编辑的角色数据，始终只改内存，不触发网络请求</summary>
        private CabinCharacterUgcInfo _localInfo;

        /// <summary>
        /// 初始化节点，绑定父 Panel 引用。
        /// </summary>
        internal void Init(IncubationCabinPanel panel)
        {
            _panel = panel;
        }

        /// <summary>
        /// 设置当前编辑的角色数据引用，创建和编辑模式均调用此方法。
        /// </summary>
        public void SetLocalInfo(CabinCharacterUgcInfo localInfo)
        {
            _localInfo = localInfo;
        }

        /// <summary>
        /// 刷新基础信息面板显示，更新名称、背景故事及音色。
        /// </summary>
        public void RefreshBaseMsg()
        {
            if (_localInfo == null)
                return;
            eitNameBox.SetInputWithoutNotify(_localInfo.name);
            eitNameBox.SetOnInput((input) =>
            {
                if (string.IsNullOrEmpty(input))
                {
                    eitNameBox.SetInputWithoutNotify(_localInfo.name);
                    TipPanel.ShowToast("伙伴名不能为空");
                    return;
                }
                _localInfo.name = input;
            });

            editBackgroundStoryBox.SetInputWithoutNotify(_localInfo.desc);
            editBackgroundStoryBox.SetOnInput((input) =>
            {
                _localInfo.desc = input;
            });
            _panel.ChangeToneID(_localInfo.toneId);
            incubationToneShopItem.SetData(_localInfo.toneId,
                () => UIManager.Inst.OpenPanel(PanelId.IncubationCabinVoicePopPanel, _localInfo));
        }
    }
}
