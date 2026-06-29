using System;
using Es;
using Game.Base;
using UI.Manager;
using UnityEngine;

namespace UI.EditOperation.Rules
{
    [OperationType(OperationType.Move)]
    public class MoveRule : BasicAxisRule
    {
        private enum MoveType
        {
            NotSupport = 0,
            FreeMove = 1,
        }

        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Move;
        }

        private bool IsForbidden(MoveType type)
        {
            return type == MoveType.NotSupport;
        }

        public override void CalculateCombineData(GamePropEditOperation subConfig)
        {
            if (IsFinish)
            {
                return;
            }

            var subRotType = (MoveType)GetTableValue(subConfig);
            if (CombineData == null)
            {
                //第一个子Bev
                CombineData = new BasicRuleCombineData();
                CombineData.Value = (int)subRotType;
            }
            else
            {
                //第二个以及后续子Bev
                CombineData.Value = (int)subRotType;
            }

            if (IsForbidden((MoveType)CombineData.Value))
            {
                IsFinish = true;
            }
        }

        public override void OnTrigger(GamePropEditOperation config, params object[] otherParams)
        {
            base.OnTrigger(config, otherParams);
            var value = (MoveType)GetTriggerValue(config);
            Debug.Log($"OnTrigger Rule : Move, value:{value}");

            switch (value)
            {
                case MoveType.NotSupport:
                    SetEntryUI(false);
                    break;
                case MoveType.FreeMove:
                    if (IsControlAxis(otherParams))
                    {
                        SetRotGizmoAndAxisUI(true, true, true);
                    }
                    else
                    {
                        SetEntryUI(true);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// 0：不支持旋转
    /// 1:支持多轴旋转
    /// 2:仅支持y轴旋转
    /// </summary>
    [OperationType(OperationType.Rotate)]
    public class RotateRule : BasicAxisRule
    {
        private enum RotateType
        {
            NotSupport = 0,
            FreeRotate = 1,
            JustYRotate = 2,
        }
        
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Rotate;
        }

        // 直接ban掉
        private bool IsForbidden(RotateType type)
        {
            return type == RotateType.NotSupport;
        }

        // 优先级：NotSupport>JustYRotate>FreeRotate
        public override void CalculateCombineData(GamePropEditOperation subConfig)
        {
            if (IsFinish)
            {
                return;
            }

            var subRotType = (RotateType)GetTableValue(subConfig);

            if (CombineData == null)
            {
                //第一个子Bev
                CombineData = new BasicRuleCombineData();
                CombineData.Value = (int)subRotType;
                if (IsForbidden((RotateType)CombineData.Value))
                {
                    IsFinish = true;
                    return;
                }
            }
            else
            {
                //第二个以及后续子Bev
                if (IsForbidden(subRotType))
                {
                    CombineData.Value = (int)RotateType.NotSupport;
                    IsFinish = true;
                    return;
                }

                if ((int)subRotType > CombineData.Value)
                {
                    CombineData.Value = (int)subRotType;
                }
            }
        }

        public override void OnTrigger(GamePropEditOperation config, params object[] otherParams)
        {
            base.OnTrigger(config);
            var value = (RotateType)GetTriggerValue(config);
            Debug.Log($"OnTrigger Rule : Rotate, value:{value}");
            switch (value)
            {
                case RotateType.NotSupport:
                    SetEntryUI(false);
                    break;
                case RotateType.FreeRotate:
                    if (IsControlAxis(otherParams))
                    {
                        SetRotGizmoAndAxisUI(true, true, true);
                    }
                    else
                    {
                        SetEntryUI(true);
                    }

                    break;
                case RotateType.JustYRotate:
                    if (IsControlAxis(otherParams))
                    {
                        SetRotGizmoAndAxisUI(false, true, false);
                    }
                    else
                    {
                        SetEntryUI(true);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// 0:不支持缩放
    /// 1：自由缩放
    /// 2：等比缩放
    /// 3：仅X-Z缩放
    /// 4：仅Y缩放
    /// 5：仅Z缩放
    /// </summary>
    [OperationType(OperationType.Scale)]
    public class ScaleRule : BasicAxisRule
    {
        private GameObject scaleToggleGo; //等比缩放控制UI

        public enum ScaleType
        {
            NotSupport = 0,
            FreeScale = 1,
            EqualProportionScale = 2, //等比缩放
            JustX = 3,
            JustY = 4,
            JustZ = 5,
            JustXZ = 6,
        }

        protected override void InitUI()
        {
            if (!_isInit)
            {
                EntryUI = ControlledUIList[0];
                XAxisUI = ControlledUIList[1];
                YAxisUI = ControlledUIList[2];
                ZAxisUI = ControlledUIList[3];
                scaleToggleGo = ControlledUIList[4];
                _isInit = true;
            }
        }

        protected override void ResetUI()
        {
            base.ResetUI();
            scaleToggleGo.SetActive(true);
        }

        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Scale;
        }

        private bool IsForbidden(ScaleType type)
        {
            return type is ScaleType.NotSupport;
        }

        private bool IsSingleAxis(ScaleType type)
        {
            return type is ScaleType.JustX or ScaleType.JustY or ScaleType.JustZ or ScaleType.JustXZ;
        }

        /*
         *  优先级: 不支持 > 仅x-z轴/仅y轴/仅z轴 > 等比缩放 > 自由缩放
         *  - 如果出现互斥的缩放，组合后为不允许缩放（特殊轴缩放和等比缩放定义为互斥缩放）
         *  - 仅x-z轴/仅y轴/仅z轴  - 针对非多轴缩放的，组合之后不支持缩放
         */
        public override void CalculateCombineData(GamePropEditOperation subConfig)
        {
            if (IsFinish)
            {
                return;
            }

            var subScaleType = (ScaleType)GetTableValue(subConfig);

            if (CombineData == null)
            {
                //第一个子Bev
                CombineData = new BasicRuleCombineData();
                CombineData.Value = (int)subScaleType;
                if (IsForbidden((ScaleType)CombineData.Value))
                {
                    IsFinish = true;
                }
            }
            else
            {
                //第二个以及后续子Bev

                if (IsForbidden(subScaleType))
                {
                    CombineData.Value = (int)subScaleType;
                    IsFinish = true;
                    return;
                }

                if (IsSingleAxis((ScaleType)CombineData.Value) && IsSingleAxis(subScaleType))
                {
                    CombineData.Value = (int)ScaleType.NotSupport;
                    IsFinish = true;
                    return;
                }

                if (((ScaleType)CombineData.Value is ScaleType.EqualProportionScale && IsSingleAxis(subScaleType))
                    ||
                    (IsSingleAxis(subScaleType) && ((ScaleType)CombineData.Value is ScaleType.EqualProportionScale)))
                {
                    CombineData.Value = (int)ScaleType.NotSupport;
                    IsFinish = true;
                    return;
                }

                if ((int)subScaleType > CombineData.Value)
                {
                    CombineData.Value = (int)subScaleType;
                }
            }
        }

        public override void OnTrigger(GamePropEditOperation config, params object[] otherParams)
        {
            base.OnTrigger(config);
            var value = (ScaleType)GetTriggerValue(config);
            Debug.Log($"OnTrigger Rule : Scale, value:{value}");
            switch (value)
            {
                case ScaleType.NotSupport:
                    if (!IsControlAxis(otherParams)) SetEntryUI(false);
                    break;
                case ScaleType.FreeScale:
                    if (!IsControlAxis(otherParams)) SetEntryUI(true);
                    if (IsControlAxis(otherParams)) SetScaleGizmoAndAxisUI(true, true, true);
                    break;
                case ScaleType.JustX:
                    if (!IsControlAxis(otherParams)) SetEntryUI(true);
                    if (IsControlAxis(otherParams)) SetScaleGizmoAndAxisUI(true, false, false);
                    scaleToggleGo.SetActive(false);
                    break;
                case ScaleType.JustY:
                    if (!IsControlAxis(otherParams)) SetEntryUI(true);
                    if (IsControlAxis(otherParams)) SetScaleGizmoAndAxisUI(false, true, false);
                    scaleToggleGo.SetActive(false);
                    break;
                case ScaleType.JustZ:
                    if (!IsControlAxis(otherParams)) SetEntryUI(true);
                    if (IsControlAxis(otherParams)) SetScaleGizmoAndAxisUI(false, false, true);
                    scaleToggleGo.SetActive(false);
                    break;
                case ScaleType.JustXZ:
                    if (!IsControlAxis(otherParams)) SetEntryUI(true);
                    if (IsControlAxis(otherParams)) SetScaleGizmoAndAxisUI(true, false, true);
                    break;
                case ScaleType.EqualProportionScale:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [OperationType(OperationType.Copy)]
    public class CopyRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Copy;
        }
    }

    [OperationType(OperationType.Lock)]
    public class LockRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Lock;
        }
    }

    [OperationType(OperationType.Hide)]
    public class HideRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Hide;
        }
    }

    [OperationType(OperationType.PublishProp)]
    public class PublishPropRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.PublishProp;
        }
    }

    [OperationType(OperationType.CanDelete)]
    public class CanDeletePropRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.CanDelete;
        }
    }    
    
    [OperationType(OperationType.CanCombine)]
    public class CanGroupPropRule : BasicPropRule
    {
        public override void OnTriggerStart(GamePropEditOperation config)
        {
            GamePropNodeManager.Inst.OnCombineStart();
        }

        public override void OnTriggerEnd(GamePropEditOperation config)
        {
            GamePropNodeManager.Inst.OnCombineEnd();
        }
    }
    
    [OperationType(OperationType.Animation)]
    public class AnimationRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Animation;
        }
    }

    [OperationType(OperationType.Movement)]
    public class MovementRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Movement;
        }
    }

    [OperationType(OperationType.Visibility)]
    public class VisibilityRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Visibility;
        }
    }

    [OperationType(OperationType.Transaction)]
    public class TransactionRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Transaction;
        }
    }

    [OperationType(OperationType.Interaction)]
    public class InteractionRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Interaction;
        }
    }

    [OperationType(OperationType.Collision)]
    public class CollisionRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Collision;
        }
    }

    [OperationType(OperationType.Rendering)]
    public class RenderingRule : BasicContraryRule
    {
        protected override int GetTableValue(GamePropEditOperation config)
        {
            return config.Rendering;
        }
    }

	[OperationType(OperationType.Properties)]
	public class PropertiesRule : BasicMultiRule
	{
		public override OperationType[] GetMultiOperate()
		{
			return new OperationType[]{OperationType.Animation, OperationType.Movement, OperationType.Visibility, OperationType.Transaction, OperationType.Interaction, OperationType.Collision, OperationType.Rendering};
		}
	}
}