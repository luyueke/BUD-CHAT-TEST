using System;
using System.Collections;
using System.Collections.Generic;

namespace AIGame.Base
{
    /// <summary>
    /// AI游戏的场景和地图加载管理
    /// </summary>
    public class AIGameController : GlobalInstance<AIGameController>
    {
        private const string TAG = "AIGameController";
        private BaseAIGame currentAIGame = null;
        
        public AIGameController()
        {
        }

        public void OnEnterAIGame(BaseAIGame aiGameController)
        {
            this.currentAIGame = aiGameController;
        }
        
        public void OnExitAIGame()
        {
            this.currentAIGame = null;
        }

        public T GetCurAIGame<T>() where T : BaseAIGame
        {
            return currentAIGame as T;
        }

        public void Restart()
        {
            currentAIGame?.Restart();
        }
    }
}

