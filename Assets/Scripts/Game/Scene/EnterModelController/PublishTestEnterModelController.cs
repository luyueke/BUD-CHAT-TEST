using System.IO;
using Game.Base;
using Game.Scene.ModeController;
using GameData;
using GameData.Base;
using GameData.MapData;
using SceneController.Attribute;
using UGCAsset;

namespace Game.Scene.EnterModelController
{
    /// <summary>
    /// 发布前的自测模式
    /// </summary>
    [EnterModel(EnterGameModel.PublishTest, EnterGameModel.UpdatePublishTest)]
    public class PublishTestEnterModelController : UGCMapEnterModelController
    {
        public override void Start(UgcBaseInfo baseInfo)
        {
            base.Start(baseInfo);
            LoadRemoteMetaData(mapInfo.metaDataUrl);
        }
        
        protected override bool BuildMap()
        {
            if (!base.BuildMap())
            {
                return false;
            }
            GameController.ChangeMode(GameMode.Play);
            return true;
        }
    }
}