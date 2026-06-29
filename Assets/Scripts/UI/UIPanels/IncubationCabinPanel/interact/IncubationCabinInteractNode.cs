using GameData.PgcData;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationCabinInteractNode : MonoBehaviour
    {
        [SerializeField] internal CabinBtnToggleParent characterInteractToggleParent;
        [SerializeField] internal CabinBtnToggleParent characterInteractNode1ToggleParent;
        [SerializeField] internal GameObject interactContent1Go;
        [SerializeField] internal GameObject interactContent2Go;
        [SerializeField] internal GameObject interactContent3Go;
        [SerializeField] internal GameObject interactContent4Go;

        [SerializeField] internal IncubationCabinInteractNode1Manager node1Manager;
        [SerializeField] internal IncubationCabinInteractNode2Manager node2Manager;
        [SerializeField] internal IncubationCabinInteractNode3Manager node3Manager;
        [SerializeField] internal IncubationCabinInteractNode4Manager node4Manager;

        private CabinPgcUgcPlayController _cabinPgcUgcPlayController = new CabinPgcUgcPlayController();
        public CabinPgcUgcPlayController PgcUgcController => _cabinPgcUgcPlayController;

        private IncubationCabinPanel _panel;
        private int _currentCharacterInteractIndex = 0; // 0:待机 1:激活 2:口令互动 3:语音对话

        internal void Init(IncubationCabinPanel panel)
        {
            _panel = panel;
            node1Manager.Init(panel, this);
            node2Manager.Init(panel, this);
            node3Manager.Init(panel, this);
            node4Manager.Init(panel, this);
        }

        /// <summary>
        /// 在主面板 InitCharacterWrapper 完成后调用，初始化播放控制器
        /// </summary>
        public void InitPgcUgcController()
        {
            _cabinPgcUgcPlayController.Init(
                _panel.animationCtrl,
                _panel.characterWrapper,
                null,
                _panel.avatarCameraController);
        }

        public void RefreshInteract()
        {
            characterInteractToggleParent.onSelect += OnSelectCharacterInteract;
            characterInteractToggleParent.Init(0);

            characterInteractNode1ToggleParent.onSelect += (index) =>
            {
                node1Manager.SetLoopType(index);
                RfreshInteractContent();
            };
            characterInteractNode1ToggleParent.Init(0);
        }

        public void OnSelectCharacterInteract(int index)
        {
            LoggerUtils.Log("OnSelectCharacterInteract: " + index);
            _currentCharacterInteractIndex = index;
            interactContent1Go.SetActive(index == 0);
            interactContent2Go.SetActive(index == 1);
            interactContent3Go.SetActive(index == 2);
            RfreshInteractContent();
        }

        public void RfreshInteractContent()
        {
            if (_currentCharacterInteractIndex == 0)
            {
                node1Manager.Refresh();
            }
            else if (_currentCharacterInteractIndex == 1)
            {
                node2Manager.Refresh();
            }
            else if (_currentCharacterInteractIndex == 2)
            {
                node3Manager.Refresh();
            }
            else if (_currentCharacterInteractIndex == 3)
            {
                node4Manager.Refresh();
            }
        }

        /// <summary>
        /// 同时刷新唤醒动作（Node2）和口令互动（Node3）的 Item 数据。
        /// 音色变更后由消息 OnCabinRefreshInteractNode2And3 驱动调用，
        /// 无论当前处于哪个 Tab 均全量更新两个列表的 audioUrl 显示。
        /// </summary>
        public void RefreshNode2And3()
        {
            node2Manager.Refresh();
            node3Manager.Refresh();
        }

        /// <summary>
        /// 播放待机动作预览，供 Node1Manager 调用。
        /// isLoop=true 时插播 5s 后恢复自动循环，false 时播完后恢复。
        /// </summary>
        public void PlayEmote(pEmoteData pEmoteData, bool isLoop)
        {
            if (pEmoteData == null)
                return;

            if (isLoop)
                _panel.StandbyAnimCtrl.PlayUserLoopAnim(pEmoteData);
            else
                _panel.StandbyAnimCtrl.PlayUserPerformAnim(pEmoteData);
        }

        /// <summary>
        /// 取消当前动画，供 Node1Manager 调用
        /// </summary>
        public void CancelAnim()
        {
            _cabinPgcUgcPlayController.CancelAnim();
        }

        [Button("暂停动画")]
        void PauseAnim()
        {
            _cabinPgcUgcPlayController.CancelAnim();
        }
    }
}
