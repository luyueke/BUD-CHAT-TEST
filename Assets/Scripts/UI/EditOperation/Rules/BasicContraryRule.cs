using Es;

namespace UI.EditOperation.Rules
{
    /// <summary>
    /// 功能是否支持：简单的相反规则， 1-True(支持)/0-False(不支持)
    /// </summary>
    public abstract class BasicContraryRule : BasicRule
    {
        public override void OnTrigger(GamePropEditOperation config, params object[] otherParams)
        {
            var configValue = GetTriggerValue(config);
            if (ControlledUIList is { Count: > 0 } && ControlledUIList[0] != null)
            {
                var ui = ControlledUIList[0];
                ui.gameObject.SetActive(configValue == 1);
            }
        }

        public override void CalculateCombineData(GamePropEditOperation subConfig)
        {
            //组合后取更严格的操作,组合子Bev中有一个不支持，则组合后也不支持
            if (IsFinish)
            {
                return;
            }

            if (CombineData == null)
            {
                CombineData = new BasicRuleCombineData();
                CombineData.Value = GetTableValue(subConfig);
            }
            else
            {
                if (CombineData.Value == 0 || GetTableValue(subConfig) == 0)
                {
                    CombineData.Value = 0;
                }
                else
                {
                    CombineData.Value = GetTableValue(subConfig);
                }
            }

            // 只要找到一个为0，后续不再刷组合数据
            if (CombineData?.Value == 0)
            {
                IsFinish = true;
            }
        }
    }
}