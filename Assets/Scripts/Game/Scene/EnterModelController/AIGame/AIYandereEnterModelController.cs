using System.Collections.Generic;
using AIGame.Base;
using Game.Props.PropsBehaviours;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using SceneController.Attribute;
using UIAgent;


namespace Game.Scene.EnterModelController
{
    [EnterModel(EnterGameModel.AIYandere)]
    public class AIYandereEnterModelController : AIGameEnterModelController
    {
        public override void StartAIGame()
        {
            this._localMetaJsonPath = "Assets/Loadable/UI/UIPanel/AIYandereGame/AIGameMetaData/AIYandere.json";
            base.StartAIGame();
        }
    }
}
