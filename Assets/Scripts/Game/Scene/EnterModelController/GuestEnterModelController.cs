// @Author: YangJie
// @Description:
// @Date:  2023/07/18
// @Modify:

using Game.Base;
using Game.Scene.ModeController;
using GameData;
using GameData.Base;
using GameData.MapData;
using HLOD;
using SceneController.Attribute;

namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.GuestScene)]
    public class GuestEnterModelController : UGCMapEnterModelController
    {

        public override void Start(UgcBaseInfo baseInfo)
        {
            base.Start(baseInfo);
            LoadRemoteMetaData(baseInfo.metaDataUrl);
        }

        protected override bool BuildMap()
        {
            if (!base.BuildMap())
            {
                return false;
            }
            GameController.ChangeMode(GameMode.Guest);
            return true;
        }

        protected override void LoadRemoteOfflineRenderData()
        {
            HLODManager.Inst.Init(pMapData);
            GameOfflineRenderManager.Inst.InitAndLoadMapOfflineRenderData(HLODManager.Inst.GetHighItemList() ,() =>
            {
                BuildMap();
            });
        }
    }
}
