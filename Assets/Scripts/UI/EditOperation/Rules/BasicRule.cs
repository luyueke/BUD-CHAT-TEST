using System.Collections.Generic;
using Es;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace UI.EditOperation.Rules
{
    public abstract class BasicRule
    {
        public List<GameObject> ControlledUIList = new List<GameObject>(1);

        public BasicRuleCombineData CombineData; //组合后动态设置的值,
        public bool IsFinish = false; //控制组合后刷数据的条件，减少代码执行次数

        protected abstract int GetTableValue(GamePropEditOperation config);
        
        public abstract void OnTrigger(GamePropEditOperation config, params object[] otherParams);

        public abstract void CalculateCombineData(GamePropEditOperation subConfig);

        public virtual int GetTriggerValue(GamePropEditOperation config)
        {
            var curSelect = EditOperationManager.Inst.CurSelectBev;
            if (curSelect && curSelect is CombineBehaviour && CombineData != null)
            {
                return CombineData.Value;
            }
            else
            {
                return GetTableValue(config);
            }
        }
    }

    public class BasicRuleCombineData
    {
        public int Value;
    }
}