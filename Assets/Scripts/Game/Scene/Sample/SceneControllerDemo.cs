// @Author: YangJie
// @Description:
// @Date:  2023/07/17
// @Modify:

using System;
using Game.Base;
using GameData;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.Base.Sample
{
    public class SceneControllerDemo : MonoBehaviour
    {

        public EnterGameModel model;
        

        private void Start()
        {
  
            GameController.StartGame(model, new MapInfo());
        }

    }
}