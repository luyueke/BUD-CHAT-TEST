using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using GameData;
using Message;
using UnityEngine;
using UnityEngine.U2D;

namespace AIGame.Base
{
    public abstract class BaseAIGame : MonoBehaviour
    {
        private void Awake()
        {
            AddListener();
        }

        private void OnDestroy()
        {
            RemoveListener();
            AIGameController.Inst.OnExitAIGame();
        }

        protected virtual void AddListener()
        {
            MessageHelper.AddListener<EnterGameModel, object>(MessageName.StartBuildMap, OnStartBuildMap);
            MessageHelper.AddListener<EnterGameModel>(MessageName.OverBuildMap, OnOverBuildMap);
        }

        protected virtual void RemoveListener()
        {
            MessageHelper.RemoveListener<EnterGameModel, object>(MessageName.StartBuildMap, OnStartBuildMap);
            MessageHelper.RemoveListener<EnterGameModel>(MessageName.OverBuildMap, OnOverBuildMap);
        }

        public virtual void OnInitByCreate()
        {
            AIGameController.Inst.OnEnterAIGame(this);
        }

        public virtual void OnStart()
        {
            
        }

        public virtual void Restart()
        {
            
        }
        
        private void OnStartBuildMap(EnterGameModel enterGameModel, object obj)
        {
            OnInitByCreate();
        }

        private void OnOverBuildMap(EnterGameModel enterGameModel)
        {
            OnStart();
        }
    }
}
