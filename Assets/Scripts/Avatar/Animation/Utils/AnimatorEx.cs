using System.Collections.Generic;
using UnityEngine;

public static class AnimatorEx
{
    /// <summary>
    /// 批量替换动画
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="aniOverrideCtrl"></param>
    /// <param name="updateClips"></param>
    public static void OverrideAnimationClip(this Animator animator, AnimatorOverrideController aniOverrideCtrl, AnimationClipOverrides clipOverrides, Dictionary<string, AnimationClip> updateClips)
    {
        if (updateClips == null || updateClips.Count == 0) return;

        foreach (var stateName in updateClips.Keys)
        {
            if (clipOverrides[stateName] != null)
            {
                GameObject.Destroy(clipOverrides[stateName]);
            }
            clipOverrides[stateName] = updateClips[stateName];
        }

        ApplyOverrides(animator, aniOverrideCtrl, clipOverrides);
    }

    /// <summary>
    /// 替换一个动画
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="aniOverrideCtrl"></param>
    /// <param name="stateName"></param>
    /// <param name="clip"></param>
    public static void OverrideAnimationClip(this Animator animator, AnimatorOverrideController aniOverrideCtrl, AnimationClipOverrides clipOverrides, string stateName, AnimationClip clip)
    {
        clipOverrides[stateName] = clip;
        ApplyOverrides(animator, aniOverrideCtrl, clipOverrides);
    }

    private static void ApplyOverrides(Animator animator, AnimatorOverrideController aniOverrideCtrl, AnimationClipOverrides clipOverrides)
    {
        aniOverrideCtrl.ApplyOverrides(clipOverrides);
        animator.runtimeAnimatorController = aniOverrideCtrl;
    }
}

/**
* 为了进行批量替换配合 AnimatorOverrideController.ApplyOverrides 方法的参数，自定义的片段重写列表类
*/
public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
{
    public AnimationClipOverrides(int capacity) : base(capacity) { }

    public AnimationClip this[string name]
    {
        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
        set
        {
            int index = this.FindIndex(x => x.Key.name.Equals(name));
            if (index != -1)
            {
                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
            }
        }
    }
}
