using System;
using UnityEngine;

public class PlayerParameterAnimation : IParameterAnimation<PlayerAniPrameter>
{
    public PlayerParameterAnimation(Animator animator) : base(animator)
    {
    }
    public override int GetParametersLength()
    {
        return allParameterDic.Count;
    }
    public override void InitAnimatorParameters()
    {
        foreach (var parameter in m_Animator.parameters)
        {
            PlayerAniPrameter parameterEnum;
            if (Enum.TryParse(parameter.name, out parameterEnum))
            {
                allParameterDic.Add(parameterEnum, parameter.nameHash);

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        floatParameterList.Add(parameterEnum);
                        break;
                    case AnimatorControllerParameterType.Int:
                        intParameterList.Add(parameterEnum);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        boolParameterList.Add(parameterEnum);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
