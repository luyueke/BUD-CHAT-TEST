using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AIParkGuestChatItem : MonoBehaviour
{
    public SuperTextMesh STM;
    private Tweener tween;
    public void SetText(string text,bool needAni,TweenCallback chat = null)
    {
        if (needAni)
        {
            if (tween != null)
            {
                tween.Kill();
            }
            tween = STM.DOText(text, YandereDataManager.Inst.TextAnimDuration);
            tween.OnUpdate(chat);
        }
        else
        {
            STM.text = text;
            STM.Rebuild();
            chat?.Invoke();
        }
    }

}