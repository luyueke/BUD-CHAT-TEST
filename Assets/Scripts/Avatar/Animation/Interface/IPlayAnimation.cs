using UnityEngine;

public interface IPlayAnimation
{
    #region PlayAnim
    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="layer"></param>
    /// <param name="normalizedTime"></param>
    IAnimationEvent Play(string stateName, int layer = 0, float normalizedTime = 0);

    /// <summary>
    /// 加载AssetBundle播放动画
    /// </summary>
    /// <param name="path"></param>
    /// <param name="layer"></param>
    /// <param name="animSpeed"></param>
    /// <param name="normalizedTime"></param>
    /// <returns></returns>
    IAnimationEvent LoadPlay(string path, int layer = 0, float normalizedTime = 0);

    /// <summary>
    /// 百分比过度播放动画
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="normalizedTransitionDuration"></param>
    /// <param name="layer"></param>
    /// <param name="normalizedTimeOffset"></param>
    /// <param name="normalizedTransitionTime"></param>
    IAnimationEvent CrossFade(string stateName, float normalizedTransitionDuration, int layer = 0, float normalizedTimeOffset = 0, float normalizedTransitionTime = 0);

    /// <summary>
    /// 加载AssetBundle百分比果冻播放动画
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="normalizedTransitionDuration"></param>
    /// <param name="layer"></param>
    /// <param name="animSpeed"></param>
    /// <param name="normalizedTimeOffset"></param>
    /// <param name="normalizedTransitionTime"></param>
    /// <returns></returns>
    IAnimationEvent LoadCrossFade(string path, float normalizedTransitionDuration, int layer = 0, float normalizedTimeOffset = 0, float normalizedTransitionTime = 0);

    /// <summary>
    /// 固定时间过度播放动画
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="fixedTransitionDuration"></param>
    /// <param name="layer"></param>
    /// <param name="fixedTimeOffset"></param>
    /// <param name="normalizedTransitionTime"></param>
    IAnimationEvent CrossFadeInFixedTime(string stateName, float fixedTransitionDuration, int layer = 0, float fixedTimeOffset = 0, float normalizedTransitionTime = 0);

    /// <summary>
    /// 加载AssetBundle固定时间过度播放动画
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="fixedTransitionDuration"></param>
    /// <param name="layer"></param>
    /// <param name="animSpeed"></param>
    /// <param name="fixedTimeOffset"></param>
    /// <param name="normalizedTransitionTime"></param>
    /// <returns></returns>
    IAnimationEvent LoadCrossFadeInFixedTime(string path, float fixedTransitionDuration, int layer = 0, float fixedTimeOffset = 0, float normalizedTransitionTime = 0);

    #endregion
}

public class AnimationInfo
{
    public int layer;
    public int stateHashCode;
    public AnimationClip animationClip;

    public float AnimationTime => animationClip ? animationClip.length : 0.1f;
}