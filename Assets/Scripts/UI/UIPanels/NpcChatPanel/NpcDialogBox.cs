using System;
using System.Text.RegularExpressions;
using Basic.Utils;
using DG.Tweening;
using Game.Utils;
using Message;
using UnityEngine;
using UnityEngine.UI;

public class NpcDialogBox : MonoBehaviour
{
    public CanvasGroup cGroup;
    public SuperTextMesh textField;
    public bool destroyOnHide;
    public UIFollow Axis { get; private set; }
    private Tweener tween;
    
    private const float showTime = 7f; // 单条聊天消息显示时长，超过就走消失逻辑
    private float animatorDuration = 0.2f; // 气泡显示、移动、消失的动画时长
    private BudTimer autoDisappearTimer;
    // 静音
    
    private void Awake()
    {
        Axis = transform.Find("Axis").GetComponent<UIFollow>();
        MessageHelper.AddListener<string>(MessageName.StartBuildMap, OnSwitchScene);
    }
    
    public void Hide()
    {
        FinishTween();
        gameObject.SetActive(false);
        if (destroyOnHide)
        {
            Destroy(gameObject);
        }
    }
    
    public void SetCamera(Camera cam)
    {
        Axis.SetCamera(cam);
    }
    
    public void ResetContent()
    {
        textField.DOKill(false);
        textField.text = "";
        return;
    }

    public void WaitNpcSpeak(string content)
    {
        if (tween != null)
        {
            tween.Kill();
        }
        Appear();
        textField.text = string.Empty;
        tween = textField.DOText(content, 2).SetLoops(-1,LoopType.Restart);
    }

    public void SetTextAndSpeak(float speed, string content, bool needAni = false, bool isFirstContent = false,
        bool isEndContent = false, Action AniOverCallBack = null)
    {
        if (isFirstContent)
        {
            Appear();
        }

        if (needAni == false)
        {
            textField.DOKill(false);
            textField.text = content;
            return;
        }

        if (content == null)
        {
            return;
        }

        content = content.Replace(" ", "\u00A0");
        if (tween != null)
        {
            tween.Kill();
        }

        tween = textField.DOText(content, speed).SetEase(Ease.Linear).OnComplete(
            () =>
            {
                if (isEndContent)
                {
                    // 启动定时器
                    autoDisappearTimer = TimerManager.Inst.RunOnce("AvatarChatBox.AutoDisappear", showTime, Disappear);
                    AniOverCallBack?.Invoke();
                }
            });
    }

    public void Appear()
    {
        gameObject.SetActive(true);
        TimerManager.Inst.Stop(autoDisappearTimer);
        Tweener t2 = DOTween.To(() => cGroup.alpha, x => cGroup.alpha = x, 1, animatorDuration);
        t2.SetAutoKill();
    }

    public void Disappear()
    {
        Tweener t2 = DOTween.To(() =>  cGroup.alpha, x => cGroup.alpha = x, 0, animatorDuration);
        t2.SetAutoKill();
        t2.OnComplete(() =>
        {
            if (autoDisappearTimer != null)
            {
                TimerManager.Inst.Stop(autoDisappearTimer);
                autoDisappearTimer = null;
            }
            gameObject.SetActive(false);
        });
    }
    
    public void ForceHide()
    {
        if (gameObject.activeSelf)
        {
            tween?.Kill();
            TimerManager.Inst.Stop(autoDisappearTimer);
            gameObject.SetActive(false);
        }
    }

    public static NpcDialogBox Create(GameObject parent, Vector3 localPos)
    {
        var uiCanvas = UIManager.Inst.Canvas;
        var wrap = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/NpcChatPanel/NpcDialogBox.prefab");
        GameObject box = wrap.Instantiate(uiCanvas.transform);
        NpcDialogBox dBox = box.GetComponent<NpcDialogBox>();
        dBox.Axis.Target = parent.transform;
        dBox.Axis.offset = localPos;
        return dBox;
    }
    public static NpcDialogBox CreatePark(GameObject parent, Vector3 localPos)
    {
        var uiCanvas = UIManager.Inst.Canvas;
        var wrap = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/NpcChatPanel/NpcDialogBox_Park.prefab");
        GameObject box = wrap.Instantiate(uiCanvas.transform);
        NpcDialogBox dBox = box.GetComponent<NpcDialogBox>();
        dBox.Axis.Target = parent.transform;
        dBox.Axis.offset = localPos;
        return dBox;
    }
    
    public void SetLocalPos(Vector3 localPos)
    {
        this.Axis.offset = localPos;
    }
    
    private void OnSwitchScene(string scene)
    {
        FinishTween();
        Destroy(gameObject);
    }
    
    public void FinishTween()
    {
        if (tween != null)
        {
            tween.Complete();
        }
        tween = null;
    }
    
    private void OnDestroy()
    {
        tween?.Kill();
        MessageHelper.RemoveListener<string>(MessageName.StartBuildMap, OnSwitchScene);
    }

    public void SetFadeInFadeOutTime(float time)
    {
        animatorDuration = time;
    }
}
