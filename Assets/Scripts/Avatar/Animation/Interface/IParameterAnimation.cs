using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class IParameterAnimation<T> where T : Enum
{
    protected Animator m_Animator;

    protected Dictionary<T, int> allParameterDic = new Dictionary<T, int>();
    protected List<T> boolParameterList = new List<T>();
    protected List<T> intParameterList = new List<T>();
    protected List<T> floatParameterList = new List<T>();

    public IParameterAnimation(Animator animator)
    {
        m_Animator = animator;
        InitAnimatorParameters();
    }

    public virtual bool GetBool(T parameter)
    {
        return m_Animator.GetBool(TryGetNameHash(parameter));
    }

    public virtual float GetFloat(T parameter)
    {
        return m_Animator.GetFloat(TryGetNameHash(parameter));
    }

    public virtual int GetInteger(T parameter)
    {
        
        return m_Animator.GetInteger(TryGetNameHash(parameter));
    }


    public virtual void SetBool(T parameter, bool value)
    {
        if (m_Animator != null)
        {
            m_Animator.SetBool(TryGetNameHash(parameter), value);
        }
    }

    public virtual void SetFloat(T parameter, float value)
    {
        if (m_Animator != null)
        {
            m_Animator.SetFloat(TryGetNameHash(parameter), value);
        }
    }

    public virtual void SetInteger(T parameter, int value)
    {
        if (m_Animator != null)
        {
            m_Animator.SetInteger(TryGetNameHash(parameter), value);
        }
    }

    public virtual void SetTrigger(T parameter)
    {
        if (m_Animator != null)
        {
            m_Animator.SetTrigger(TryGetNameHash(parameter));
        }

    }

    public abstract void InitAnimatorParameters();

    public abstract int GetParametersLength();

    private int TryGetNameHash(T parameter)
    {
        int nameHash = 0;
        allParameterDic.TryGetValue(parameter, out nameHash);

        return nameHash;
    }

}
