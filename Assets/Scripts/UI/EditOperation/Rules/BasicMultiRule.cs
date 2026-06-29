
using System.Linq;
using Es;
using Game.Props.PropsBehaviours;
using System.Collections.Generic;
/**
* @ Author: Jun Zhou
* @ Create Time: 2023-08-10 16:04:12
* @ Modified by: Jun Zhou
* @ Modified time: 2023-08-10 16:05:22
* @ Description: 用于包含多个规则的集合
*/
namespace UI.EditOperation.Rules
{
	public abstract class BasicMultiRule : BasicRule
	{
        List<int> combineMulitList;

		public override void CalculateCombineData(GamePropEditOperation subConfig)
		{
            if (IsFinish)
            {
                return;
            }
            var multiTypes = GetMultiOperate();
            if (CombineData == null)
            {
                CombineData = new BasicRuleCombineData();
                combineMulitList = Enumerable.Repeat(0, multiTypes.Length).ToList();
            }

            for (int i = 0; i < multiTypes.Length; i++)
            {
                var rule = EditOperationManager.Inst.GetRule(multiTypes[i]);
                var configValue = rule.GetTriggerValue(subConfig);
                combineMulitList[i] += configValue;
                if (configValue == 0)
                    combineMulitList[i] = 0;
            }

            var CombineDataCount = 0;
            combineMulitList.ForEach(x => CombineDataCount+=x);
            CombineData.Value = CombineDataCount;
            if (CombineData.Value == 0)
            {
                IsFinish = true;
            }
		}

		public override void OnTrigger(GamePropEditOperation config, params object[] otherParams)
		{
            var configValue = GetTriggerValue(config);
            var isShow = true;
            if (otherParams is { Length: > 0 } && otherParams[0] is bool argIsShow)
            {
               isShow = argIsShow;
            }

            if (ControlledUIList is { Count: > 0 })
            {
                var ui = ControlledUIList[0];
                ui.gameObject.SetActive(configValue > 0 && isShow);
            }
		}

		public override int GetTriggerValue(GamePropEditOperation config)
		{
            var curSelect = EditOperationManager.Inst.CurSelectBev;
            if (curSelect is CombineBehaviour && CombineData != null)
            {
                return CombineData.Value;
            }
            // 只要有一个满足就可以触发
            var multiTypes = GetMultiOperate();
            for (int i = 0; i < multiTypes.Length; i++)
            {
                var rule = EditOperationManager.Inst.GetRule(multiTypes[i]);
                if (rule.GetTriggerValue(config) > 0)
                {
                    return 1;
                }
            }

            return 0;
		}		

        public abstract OperationType[] GetMultiOperate();

        /// <summary>
        /// 多个规则不需要关心这个方法
        /// </summary>
		protected override int GetTableValue(GamePropEditOperation config)
		{
			return -1;
		}
	}
}