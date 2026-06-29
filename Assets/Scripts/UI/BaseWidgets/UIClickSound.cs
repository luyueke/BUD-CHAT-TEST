using Game.Audio;
using UnityEngine;
using UnityEngine.EventSystems;




public enum SoundTiming
{
    Click = 0,
    Down,
    Up
}

public class UIClickSound : ClickEventListener
{
    //音效相关参数
    public bool HasSound = true;
    public UISoundType SoundType;
    public SoundTiming Timing = SoundTiming.Click;
    
    private void Start() 
    {
        AddClickEventHandler((go, eventData) => { OnClickEffect(go, eventData); });
        AddPointerDownHandler((go, eventData) => { OnDownEffect(go, eventData); });
        AddPointerUpHandler((go, eventData) => { OnUpEffect(go, eventData); });
    }
    
    private void PlaySound()
    {
        if(HasSound)
        {
            AkSoundManager.Inst.PlayUIEffectSound(SoundType);
        }
    }

    private void OnClickEffect(GameObject go, BaseEventData eventData)
    {
        if (Timing == SoundTiming.Click)
        {
            PlaySound(); 
        }
    }

    private void OnDownEffect(GameObject go, BaseEventData eventData)
    {
        if (Timing == SoundTiming.Down)
        {
            PlaySound(); 
        }
    }
    
    private void OnUpEffect(GameObject go, BaseEventData eventData)
    {
        if (Timing == SoundTiming.Up)
        {
            PlaySound(); 
        }
    }
}
