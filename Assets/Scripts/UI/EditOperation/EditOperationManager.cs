using System;
using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Utils;
using UI.EditOperation.Rules;
using UnityEngine;
using UnityEngine.Profiling;

namespace UI.EditOperation
{
    public class EditOperationManager : GameInstance<EditOperationManager>
    {
        private Dictionary<OperationType, BasicRule> _rules;
        private Dictionary<OperationType, BasicPropRule> _propRules;


        private SceneEntity _currentSelectEntity;

        public SceneEntity CurrentSelectEntity
        {
            get => _currentSelectEntity;
            set
            {
                _currentSelectEntity = value;
                SetCurOperationConfig();
            }
        }

        private GamePropEditOperation _curOperationConfig;

        public GamePropEditOperation CurOperationConfig
        {
            get
            {
                if (_curOperationConfig == null)
                {
                    SetCurOperationConfig();
                }

                return _curOperationConfig;
            }
        }

        public NodeBaseBehaviour CurSelectBev => CurrentSelectEntity?.GetNodeBaseBehaviour();

        private void SetCurOperationConfig()
        {
            ResetRuleCombineValue();
            if (CurSelectBev is CombineBehaviour)
            {
                _curOperationConfig = null;
                GetPropEditOpDataWhenCombineSelect();
            }
            else
            {
                _curOperationConfig = GetPropEditOpDataById(CurrentSelectEntity);
            }
        }

        private void ResetRuleCombineValue()
        {
            foreach (var r in _rules.Values)
            {
                if (r != null)
                {
                    r.CombineData = null;
                    r.IsFinish = false;
                }
            }
        }

        private GamePropEditOperation GetPropEditOpDataById(SceneEntity entity)
        {
            if (entity == null) return null;
            var gCmp = entity.GetGameObjectComponent();
            var config = GamePropDataHelper.GetPropDataByID(gCmp.PropId);
            var handleType = config.HandleType;
            return Es.DataTables.GetGamePropEditOperation(handleType);
        }

        private void GetPropEditOpDataWhenCombineSelect()
        {
            // 组合后刷一遍OperationConfig数据再Trigger
            if (CurSelectBev is CombineBehaviour)
            {
                var allChildBev = CurSelectBev.gameObject.GetComponentsInChildren<NodeBaseBehaviour>();
                foreach (var bev in allChildBev)
                {
                    if (!bev || bev is CombineBehaviour) continue;
                    var subBevData = GetPropEditOpDataById(bev.entity);
                    foreach (var r in _rules.Values)
                    {
                        if (!(r is BasicMultiRule))
                            r?.CalculateCombineData(subBevData);
                    }
                }
                foreach (var bev in allChildBev)
                {
                    if (!bev || bev is CombineBehaviour) continue;
                    var subBevData = GetPropEditOpDataById(bev.entity);
                    foreach (var r in _rules.Values)
                    {
                        if (r is BasicMultiRule) // 多组合晚点执行
                            r?.CalculateCombineData(subBevData);
                    }
                }
            }
        }

        public void Init()
        {
            Profiler.BeginSample($"EditOperationManager Init");
            _rules = new Dictionary<OperationType, BasicRule>();
            _propRules = new Dictionary<OperationType, BasicPropRule>();
            InitAttrs();
            Profiler.EndSample();
        }

        private void InitAttrs()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.IsSubclassOf(typeof(BasicRule))))
                .ToArray();
            var opAttr = typeof(OperationTypeAttribute);
            foreach (var tmpPipelineType in types)
            {
                var mAttrs = tmpPipelineType.GetCustomAttributes(opAttr, true);
                if (mAttrs.FirstOrDefault(tmp => tmp.GetType() == opAttr) is not OperationTypeAttribute mAtt) continue;
                if (!_rules.ContainsKey(mAtt.OperationType))
                {
                    _rules.Add(mAtt.OperationType, (BasicRule)Activator.CreateInstance(tmpPipelineType));
                }
            }

            var pTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.IsSubclassOf(typeof(BasicPropRule))))
                .ToArray();
            foreach (var t in pTypes)
            {
                var mAttrs = t.GetCustomAttributes(opAttr, true);
                if (mAttrs.FirstOrDefault(tmp => tmp.GetType() == opAttr) is not OperationTypeAttribute mAtt) continue;
                if (!_propRules.ContainsKey(mAtt.OperationType))
                {
                    _propRules.Add(mAtt.OperationType, (BasicPropRule)Activator.CreateInstance(t));
                }
            }
        }

        public override void Release()
        {
            base.Release();
            _rules?.Clear();
            _rules = null;
            _propRules?.Clear();
            _propRules = null;
        }

        private bool CheckOpType(OperationType operationType)
        {
            if (_rules != null && _rules.ContainsKey(operationType))
            {
                return true;
            }

            LoggerUtils.LogError($"{nameof(EditOperationManager)} : Can not find operationType {operationType}");
            return false;
        }

        private bool CheckPropOpType(OperationType operationType)
        {
            if (_propRules != null && _propRules.ContainsKey(operationType))
            {
                return true;
            }

            LoggerUtils.LogError($"{nameof(EditOperationManager)} : Can not find prop operationType {operationType}");
            return false;
        }

        #region Public
        public void ReleaseRuleUI(OperationType type)
        {
            var rule = _rules[type];
            rule.ControlledUIList.Clear();
        }

        public void BindRuleUI(OperationType type, params GameObject[] uiGameObjects)
        {
            if (!CheckOpType(type)) return;
            if (uiGameObjects == null) return;

            var rule = _rules[type];
            foreach (var ui in uiGameObjects)
            {
                if (!rule.ControlledUIList.Contains(ui))
                {
                    rule.ControlledUIList.Add(ui);
                }
            }
        }

        public void TriggerRule(OperationType type, params object[] paras)
        {
            if (!CheckOpType(type))
            {
                LoggerUtils.LogError("TriggerRule OperationType illegal!");
                return;
            }

            if (CurrentSelectEntity == null) return;
            var rule = _rules[type];
            rule?.OnTrigger(CurOperationConfig, paras);
        }

        //是否是等比缩放，包含组合时的判断
        public bool IsEqualProportionScale()
        {
            var result = false;
            if (CurSelectBev is CombineBehaviour)
            {
                result = _rules[OperationType.Scale].CombineData.Value == (int)ScaleRule.ScaleType.EqualProportionScale;
            }
            else if (CurOperationConfig is { Scale: (int)ScaleRule.ScaleType.EqualProportionScale })
            {
                result = true;
            }

            return result;
        }

        public BasicRule GetRule(OperationType type)
        {
            if (!CheckOpType(type))
            {
                LoggerUtils.LogError("TriggerRule OperationType illegal!");
                return null;
            }
            return _rules[type];
        }

        #endregion

        #region Prop Trigger

        public void TriggerPropRuleStart(OperationType type)
        {
            if (!CheckPropOpType(type))
            {
                LoggerUtils.LogError("TriggerRule OperationType illegal!");
                return;
            }

            var rule = _propRules[type];
            rule?.OnTriggerStart(CurOperationConfig);
        }

        public void TriggerPropRuleEnd(OperationType type)
        {
            if (!CheckPropOpType(type))
            {
                LoggerUtils.LogError("TriggerRule OperationType illegal!");
                return;
            }

            var rule = _propRules[type];
            rule?.OnTriggerEnd(CurOperationConfig);
        }

        #endregion
    }
}