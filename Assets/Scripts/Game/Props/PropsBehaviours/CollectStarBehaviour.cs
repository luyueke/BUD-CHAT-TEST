using Game.Audio;
using Game.Avatar;
using Game.Base;
using Game.Config;
using Game.MapSetting;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using GameData;
using Message;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// 收集星星bev
    /// </summary>
    public class CollectStarBehaviour : NodeBaseBehaviour, StarBehaviour
    {
        public CrystalStoneItemBehaviour starItem;

        private BudTimer gotUIEnterTimer;
        private BudTimer gotUIEndTimer;
        private BudTimer addStarTimer;

        private bool isEdit;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            starItem = GetComponentInChildren<CrystalStoneItemBehaviour>();
            starItem.Init(this);
        }

        public void OnDestroy()
        {
            AkSoundManager.Inst.PostEvent("Stop_PGC_SFX_Star_Shine_Loop", gameObject);
        }


        public void OnChangeMode(GameMode gameMode)
        {
            entity.GetComp<CollectStarComponent>().IsCollect = false;
            if (gameMode == GameMode.Edit)
            {
                isEdit = true;
                starItem.gameObject.SetActive(true);
                AkSoundManager.Inst.PostEvent("Stop_PGC_SFX_Star_Shine_Loop", gameObject);
            }
            else
            {
                AkSoundManager.Inst.PostEvent("Play_PGC_SFX_Star_Shine_Loop", gameObject);
                isEdit = false;
            }

            if (starItem)
            {
                starItem.OnChangeMode(gameMode, false);
            }
        }

        public override void OnTrigEnter()
        {
            base.OnTrigEnter();

            if (PassLevelDataManager.Inst.CurStatus != PassLevelStatus.Running)
            {
                return;
            }

            var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
            if (!playerStateCtrl.CanEnterState(PlayerState.CollectedStar)) return;
            CollectedBrightStar();
        }

        public override void OnTrigExit()
        {
            base.OnTrigExit();
        }

        #region 收集表现

        //收集高亮星星
        private void CollectedBrightStar()
        {
            AkSoundManager.Inst.PostEvent("Stop_PGC_SFX_Star_Shine_Loop", gameObject);
            var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
            var comp = this.entity.GetComp<CollectStarComponent>();
            var starName = string.IsNullOrEmpty(comp.StarName) ? $"星星{comp.Id}" : comp.StarName;
            playerStateCtrl.EnterState(PlayerState.CollectedStar, this, false, starName);

            DoCollectStart();

            //开启计时器--定格结束恢复idle、UI消失、更新本地收集数据
            TimerManager.Inst.Stop(gotUIEndTimer);
            gotUIEndTimer = TimerManager.Inst.RunOnce("gotStarUIEnd", 3.5f, () =>
            {
                //恢复输入
                DoCollectFinish();
            });
        }

        private void SetCurItemPos(Transform parent, Vector3 pos, Vector3 rot)
        {
            Transform starTrans;
            (starTrans = starItem.transform).SetParent(parent);
            starTrans.localPosition = pos;
            starTrans.localEulerAngles = rot;
        }

        private void DoCollectStart()
        {
            //暂停倒计时，保存数据
            MessageHelper.Broadcast(MessageName.CountDownPauseAndRecord);
        }

        //收集动画完全结束
        private void DoCollectFinish()
        {
            GlobalNodeManager.Inst.Get<CollectStarManager>().DoStarCollected(this);
            MessageHelper.Broadcast(MessageName.CountDownContinue);
            MessageHelper.Broadcast(MessageName.PassLevelJudging);
            // MessageHelper.Broadcast(MessageName.ReleaseTrigger);
        }

        public void SetNewbieEmo(bool isOn)
        {
        }

        public void PlayCollectAnim(Transform parent)
        {
            SetCurItemPos(parent, new Vector3(0, 0.4f, 0.2f), Vector3.zero);
            starItem.PlayCollectAnim(true); //播放星星动画
            //收集音效
            starItem.StopCrystalStoneLoop(gameObject);
            starItem.PlayCrystalStonePickUp(gameObject);
        }

        public void ResetCollectAnim()
        {
            SetCurItemPos(this.transform, Vector3.zero, Vector3.zero);
            if (isEdit) return;
            starItem.gameObject.SetActive(false);
        }

        #endregion
    }
}
