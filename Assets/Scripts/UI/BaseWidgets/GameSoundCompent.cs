using AIGame.Base;
using System.Collections;
using UnityEngine;

public class GameSoundCompent : MonoBehaviour
{
    public bool InitPlay;

    public float InitPlayInterval;

    public float PlayInterval;

    public bool Loop;

    public string SoundEventName;

    private float time;
    public void OnEnable()
    {
        if (InitPlay)
        {
            TimerManager.Inst.RunOnce("GameSoundCompent", InitPlayInterval, () =>
            {
                AIGameSoundUtils.Inst.PlaySound(SoundEventName, gameObject);
                if (Loop)
                {
                    time = PlayInterval;
        
                }
                else
                {
                    gameObject.SetActive(false);
                }
            });
        }
        else
        {
            time = PlayInterval;
        }
    }

    private void Update()
    {
        if (time > 0 && gameObject.activeSelf)
        {
            time -= Time.deltaTime;
            if (time <= 0)
            {
                AIGameSoundUtils.Inst.PlaySound(SoundEventName, gameObject);
                if (Loop)
                {
                    time = PlayInterval;
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }


}