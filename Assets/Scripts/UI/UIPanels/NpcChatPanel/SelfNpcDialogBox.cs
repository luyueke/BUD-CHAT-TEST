using System;
using System.Text.RegularExpressions;
using Basic.Utils;
using DG.Tweening;
using Game.Utils;
using Message;
using UnityEngine;
using UnityEngine.UI;

public class SelfNpcDialogBox : MonoBehaviour
{
    public CanvasGroup cGroup;
    public SuperTextMesh textField;
    public bool destroyOnHide;
    public UIFollow Axis { get; private set; }

    private RectTransform axisTrans;
    private const float showTime = 7f; // 单条聊天消息显示时长，超过就走消失逻辑
    private const float animatorDuration = 0.2f; // 气泡显示、移动、消失的动画时长
    private BudTimer autoDisappearTimer;
    // 静音
    
    private void Awake()
    {
        Axis = transform.Find("Axis").GetComponent<UIFollow>();
        axisTrans = Axis.GetComponent<RectTransform>();
        MessageHelper.AddListener<string>(MessageName.StartBuildMap, OnSwitchScene);
    }
    
    public void Hide()
    {
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
    
    public void SetText(string content)
    {
        textField.text = content;
        Appear();
    }

    public void Appear()
    {
        gameObject.SetActive(true);
        cGroup.alpha = 1;
        axisTrans.anchoredPosition = new Vector2(0, -100);
        axisTrans.DOAnchorPosY(0, animatorDuration).SetRelative(true);
        TimerManager.Inst.Stop(autoDisappearTimer);
        autoDisappearTimer = TimerManager.Inst.RunOnce("AvatarChatBox.AutoDisappear", showTime, Disappear);
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
            axisTrans.anchoredPosition = new Vector2(0, -100);
            gameObject.SetActive(false);
        });

    }

    public static SelfNpcDialogBox Create(GameObject parent, Vector3 localPos)
    {
        var uiCanvas = UIManager.Inst.Canvas;
        var wrap = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/NpcChatPanel/SelfDialogBox.prefab");
        GameObject box = wrap.Instantiate(uiCanvas.transform);
        SelfNpcDialogBox dBox = box.GetComponent<SelfNpcDialogBox>();
        dBox.Axis.Target = parent.transform;
        dBox.Axis.offset = localPos;
        return dBox;
    }

    public static SelfNpcDialogBox CreatePark(GameObject parent, Vector3 localPos)
    {
        var uiCanvas = UIManager.Inst.Canvas;
        var wrap = Loader.Load<GameObject>("Assets/Loadable/UI/UIPanel/NpcChatPanel/SelfDialogBox_Park.prefab");
        GameObject box = wrap.Instantiate(uiCanvas.transform);
        SelfNpcDialogBox dBox = box.GetComponent<SelfNpcDialogBox>();
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
        Destroy(gameObject);
    }

    public void ForceHide()
    {
        if (gameObject.activeSelf)
        {
            TimerManager.Inst.Stop(autoDisappearTimer);
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.StartBuildMap, OnSwitchScene);
    }
}
