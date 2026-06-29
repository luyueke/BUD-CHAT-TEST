using System;
using UnityEngine;

public class PGCVehicleParameterAnimation : IParameterAnimation<PGCVehicleAnimParameter>
{
    public PGCVehicleParameterAnimation(Animator animator) : base(animator)
    {
    }
    public override int GetParametersLength()
    {
        return allParameterDic.Count;
    }
    public override void InitAnimatorParameters()
    {
        if(m_Animator == null)
        {
            return;
        }
        foreach (var parameter in m_Animator.parameters)
        {
            PGCVehicleAnimParameter parameterEnum;
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
