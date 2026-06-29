using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Com.TheFallenGames.OSA.Util.IO;
using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AIParkChatItem : MonoBehaviour
{
    public SuperTextMesh STM;
    public Text textName;
    private Tweener tween;
    public Image HeadIcon;
    public RemoteImageBehaviour RM_Cover;
    AIParkChatInfo _chatInfo;
    public void SetData(AIParkChatInfo chatInfo)
    {
        _chatInfo = chatInfo;
        SetHead();
        textName.text = AIPark_NpcUtil.GetName(chatInfo.npcId);
        SetText(chatInfo.content, false);
    }

    public void SetText(string text, bool needAni, TweenCallback chat = null)
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
            if (STM.preferredWidth > 770)
            {
                STM.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                STM.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(770, STM.transform.GetComponent<RectTransform>().sizeDelta.y);
                STM.GetComponent<ContentSizeFitter>().SetLayoutVertical();
            }
            else
            {
                STM.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                STM.GetComponent<ContentSizeFitter>().SetLayoutHorizontal();
            }
            STM.Rebuild();
            chat?.Invoke();
        }
    }

    [Button("测试")]
    public void Test()
    {

        STM.Rebuild();
    }

    public void SetHead()
    {
        HeadIcon.gameObject.SetActive(false);
        string cover = AIPark_NpcUtil.GetHead(_chatInfo.npcId, HeadIcon);
        if (!string.IsNullOrEmpty(cover))
        {
            RM_Cover.Load(cover);
            RM_Cover.gameObject.SetActive(true);
        }
    }
}