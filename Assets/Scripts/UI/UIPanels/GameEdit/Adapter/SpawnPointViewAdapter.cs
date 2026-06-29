using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UI.UIPanels.GameEdit;

namespace UI.UIPanels.GameEdit
{
    public class SpawnPointViewAdapter : BasePropertyAdapter
    {
        private SpawnPointSubView colorSubView;
        private SpawnPointBehaviour sBehv;
        private SpawnPointComponent sComp;
        protected override void OnCreate()
        {
            colorSubView = AddTabView<SpawnPointSubView>("");
            colorSubView.SetDefault.AddListener(SetDefaultClick);
        }

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetNodeBaseBehaviour();
            sComp = selectEntity.GetComp<SpawnPointComponent>();
            sBehv = go as SpawnPointBehaviour;
            sBehv.SetDefault(sComp.SpawnDefault);
            colorSubView.DefaultToggle.isOn = sBehv.IsDefault;
        }

        private void SetDefaultClick(bool val)
        {
            if (selectEntity != null)
            {
                var mgr = GlobalNodeManager.Inst.Get<SpawnPointManager>();
                mgr.UpdateSpawnPointDefaultState();
                sComp.SpawnDefault = val ? 1 : 0;
                sBehv.SetDefault(sComp.SpawnDefault);
            }
        }
    }
}