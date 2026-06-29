using System;
using UnityEngine;

public class PlayAnimation : AnimationEventEx, IPlayAnimation
{
    [SerializeField]
    private AnimatorOverrideController m_AniOverrideCtrl;
    protected AnimatorOverrideController AniOverrideCtrl
    {
        get
        {
            if (m_AniOverrideCtrl == null)
            {
                InitAnimatorOverrideController();
            }

            return m_AniOverrideCtrl;
        }
    }
    protected AnimationClipOverrides clipOverrides;

    protected Action<int> EnterTempClip;
    private readonly string TempClip = "TempClip";
    private int[] curTempIndex;
    private int[] tempClipCount;

    private readonly string Anim = ".anim";


    public override void Init()
    {
        InitAnimatorOverrideController();
        SetTempClipCount();

        base.Init();
    }

    #region IPlayAnimation


    public IAnimationEvent Play(string stateName, int layer = 0, float normalizedTime = 0)
    {
        if (string.IsNullOrEmpty(stateName)) return null;

        SetCurAnimationInfo(layer, stateName, AniOverrideCtrl[stateName]);
        m_Animator.Play(stateName, layer, normalizedTime);

        return this;
    }

    public IAnimationEvent LoadPlay(string path, int layer = 0, float normalizedTime = 0)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string stateName = GetTempClipName(layer);
        var warpper =  Loader.Load<AnimationClip>(path + Anim);
        if (warpper == null) return null;
        AnimationClip clipRes = warpper.RetainAsset(m_Animator.gameObject);
        if (clipRes == null) return null;
        AnimationClip clip = AnimationClip.Instantiate(clipRes, transform);
        SetCurAnimationInfo(layer, stateName, clip);
        m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, clip);

        EnterTempClip?.Invoke(layer);
        m_Animator.Play(stateName, layer, normalizedTime);
        m_Animator.Update(0);
        return this;
    }


    public IAnimationEvent LoadPlay(AnimationClip clip, int layer = 0, float normalizedTime = 0)
    {
        if (clip == null) return null;
        string stateName = GetTempClipName(layer);
        SetCurAnimationInfo(layer, stateName, clip);
        m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, clip);
        EnterTempClip?.Invoke(layer);
        m_Animator.Play(stateName, layer, normalizedTime);
        return this;
    }

    /// <summary>
    /// 清除动画
    /// </summary>
    public void ClearClip(int layer = 0)
    {
        for (int i = 1; i <= tempClipCount[layer]; i++)
        {
            string stateName = TempClip + layer.ToString() + i.ToString();
            m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, null);
        }
    }

    public IAnimationEvent CrossFade(string stateName, float normalizedTransitionDuration, int layer = 0, float normalizedTimeOffset = 0, float normalizedTransitionTime = 0)
    {
        if (string.IsNullOrEmpty(stateName)) return null;

        SetCurAnimationInfo(layer, stateName, AniOverrideCtrl[stateName]);
        m_Animator.CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);

        return this;
    }

    public IAnimationEvent LoadCrossFade(string path, float normalizedTransitionDuration, int layer = 0, float normalizedTimeOffset = 0, float normalizedTransitionTime = 0)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string stateName = GetTempClipName(layer);

        var warpper = Loader.Load<AnimationClip>(path + Anim);
        if (warpper == null) return null;
        AnimationClip clipRes = warpper.RetainAsset(m_Animator.gameObject);
        if (clipRes == null) return null;
        AnimationClip clip = AnimationClip.Instantiate(clipRes, transform);

        SetCurAnimationInfo(layer, stateName, clip);
        m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, clip);

        EnterTempClip?.Invoke(layer);
        m_Animator.CrossFade(stateName, normalizedTransitionDuration, layer, normalizedTimeOffset, normalizedTransitionTime);

        return this;
    }

    public IAnimationEvent CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer = 0, float fixedTimeOffset = 0, float normalizedTransitionTime = 0)
    {
        if (string.IsNullOrEmpty(stateName)) return null;

        SetCurAnimationInfo(layer, stateName, AniOverrideCtrl[stateName]);
        m_Animator.CrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);

        return this;
    }

    public IAnimationEvent LoadCrossFadeInFixedTime(string path, float fixedTransitionDuration, int layer = 0, float fixedTimeOffset = 0, float normalizedTransitionTime = 0)
    {
        if (string.IsNullOrEmpty(path)) return null;

        string stateName = GetTempClipName(layer);

        var warpper = Loader.Load<AnimationClip>(path + Anim);
        if (warpper == null) return null;
        AnimationClip clipRes = warpper.RetainAsset(m_Animator.gameObject);
        if (clipRes == null) return null;
        AnimationClip clip = AnimationClip.Instantiate(clipRes, transform);

        SetCurAnimationInfo(layer, stateName, clip);
        m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, clip);

        EnterTempClip?.Invoke(layer);
        m_Animator.CrossFadeInFixedTime(stateName, fixedTransitionDuration, layer, fixedTimeOffset, normalizedTransitionTime);

        return this;
    }
    #endregion

    #region Other Public
    /// <summary>
    /// 设置给定索引处层的权重
    /// </summary>
    /// <param name="layerIndex"></param>
    /// <param name="weight"></param>
    public void SetLayerWeight(int layerIndex, float weight)
    {
        m_Animator.SetLayerWeight(layerIndex, weight);
    }

    /// <summary>
    /// 根据 deltaTime 计算动画器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void UpdateAnim(float deltaTime)
    {
        m_Animator.Update(deltaTime);
    }

    /// <summary>
    /// 获取当前播放动画名称
    /// </summary>
    /// <param name="layer"></param>
    /// <returns></returns>
    public string GetCurPlayClipName(int layer)
    {
        if (m_AniInfoArray != null && m_AniInfoArray.Length > layer && m_AniInfoArray[layer].animationClip != null)
        {
            return m_AniInfoArray[layer].animationClip.name;
        }

        return null;
    }

    public override AnimationClip GetClip(string stateName)
    {
        return AniOverrideCtrl[stateName];
    }

    /// <summary>
    /// 获取当前动画状态时长
    /// </summary>
    /// <param name="stateName"></param>
    /// <returns></returns>
    public float GetClipLength(string stateName)
    {
        if (AniOverrideCtrl[stateName] == null)
        {
            return 0;
        }

        return AniOverrideCtrl[stateName].length;
    }

    /// <summary>
    /// 是否播放当前动画
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public bool IsName(string stateName, int layer = 0)
    {
        return m_Animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName);
    }
    #endregion

    #region private
    private void InitAnimatorOverrideController()
    {
        m_AniOverrideCtrl = new AnimatorOverrideController(m_Animator.runtimeAnimatorController);
        m_Animator.runtimeAnimatorController = m_AniOverrideCtrl;
        clipOverrides = new AnimationClipOverrides(m_AniOverrideCtrl.overridesCount);
        m_AniOverrideCtrl.GetOverrides(clipOverrides);
    }

    protected void ApplyOverrides()
    {
        AniOverrideCtrl.ApplyOverrides(clipOverrides);
        m_Animator.runtimeAnimatorController = AniOverrideCtrl;
    }

    private string GetTempClipName(int layer)
    {
        curTempIndex[layer] = curTempIndex[layer] == tempClipCount[layer] ? 1 : curTempIndex[layer] + 1;

        return TempClip + layer.ToString() + curTempIndex[layer].ToString();
    }

    protected void SetCurAnimationInfo(int layer, string stateName, AnimationClip clip)
    {
        layer = layer < 0 ? 0 : layer;

        ClearAnimationEvent(layer);

        m_CurAniInfo = m_AniInfoArray[layer];
        m_CurAniInfo.animationClip = clip;
        m_CurAniInfo.stateHashCode = Animator.StringToHash(stateName);
    }

    private void SetTempClipCount()
    {
        curTempIndex = new int[m_Animator.layerCount];
        tempClipCount = new int[m_Animator.layerCount];

        foreach (var clip in clipOverrides)
        {
            if (clip.Key.name.Contains(TempClip))
            {
                int layer = int.Parse(clip.Key.name[TempClip.Length].ToString());
                tempClipCount[layer]++;
            }
        }
    }
    #endregion
}
