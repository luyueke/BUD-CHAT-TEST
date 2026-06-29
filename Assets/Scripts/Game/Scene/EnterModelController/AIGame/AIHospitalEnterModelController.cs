using System.Collections.Generic;
using AIGame.Base;
using Game.Props.PropsBehaviours;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using Network;
using Network.Http;
using Newtonsoft.Json;
using SceneController.Attribute;
using UIAgent;


namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.AIHospital)]
    public class AIHospitalEnterModelController : AIGameEnterModelController
    {
        public override void StartAIGame()
        {
            this._localMetaJsonPath = "Assets/Loadable/UI/UIPanel/AIHospitalGame/AIHospitalConfig/AIHospital_MapData.json";
            base.StartAIGame();

            var mapId = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().id;
            if(mapId != "PGC")
            {
                UGCCommonReq.Inst.UGCBuyReq(GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().id, isSuccess =>
                {
                
                });
            }

        }
    }
}
