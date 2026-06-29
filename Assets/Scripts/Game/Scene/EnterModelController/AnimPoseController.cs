using System;
using Game.Base;
using GameData;
using GameData.Base;
using GameData.MapData;
using GameData.BaseInfo;
using SceneController.Attribute;
using UGCAsset;
using System.IO;
using Message;
using UIAgent;

namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.AnimPoseEmpty, EnterGameModel.AnimPoseContinueEdit)]
    public class AnimPoseController : UGCMapEnterModelController
    {
        protected PoseInfo poseInfo;

        public override void Start(UgcBaseInfo baseInfo)
        {
            base.Start(baseInfo);
            poseInfo = baseInfo as PoseInfo;
            if (string.IsNullOrEmpty(poseInfo.id))
            {
                poseInfo.cover = "https://cdn.budapp.cn/UgcAnimStudio/TemplateCover/UGCAnim_" + poseInfo.poseType + ".png";
                PoseAssetManager.Inst.CreatePoseInServer(poseInfo, (success) =>
                {
                    if (success)
                    {
                        MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, poseInfo);
                    }
                    else
                    {
                        GameController.ExitGame("创建草稿失败");
                    }
                });
            }
            else
            {
                MessageHelper.Broadcast<EnterGameModel, UgcBaseInfo>(MessageName.OverBuildMap, GameController.enterGameModel, poseInfo);
            }
        }
    }
}