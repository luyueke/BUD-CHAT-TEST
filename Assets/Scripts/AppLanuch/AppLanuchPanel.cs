  using System;
using UnityEngine;

public class AppLanuchPanel : MonoBehaviour
{
    private float _aniDur = 2f;
    private bool _isLanuchFinsh = false;

    /// <summary>
    /// 是否启动完成。动画播放有最低时长限制
    /// </summary>
    public bool isLanuchFinsh => _isLanuchFinsh;

    public Animator anim;
    public AudioSource audioSource;
   
    
    private const string audioName = "Play_App_ScreenOpening_Logo";
    private Action lanuchCompleteAction;

     
    public void PlayLanuchAni(Action lanuchComplete)
    {
        _isLanuchFinsh = false;
        lanuchCompleteAction = lanuchComplete;
        anim.Play("ui_op_ani");
        PlayLanuchAudio();
        Invoke("DelayAnimation", _aniDur);
    }

    private void DelayAnimation()
    {
        _isLanuchFinsh = true;
        lanuchCompleteAction?.Invoke();
    }
        
    private void PlayLanuchAudio()
    {
        audioSource.Play();
        // AKSoundManager.Inst.PostEvent(audioName, gameObject);
    }
}
