using System;
using System.Collections.Generic;
using Basic;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Utils;
using UI.Base;
using UnityEngine;

public class OxygenPanel : BasePanel<OxygenPanel>
{
    public Animator oxyFlashingAnim;
    private ProgressBarItem oxyBar;
    private List<ProgressBarItem> divingBars=new List<ProgressBarItem>();
    private ProgressBarItem divingBar2;
    public float maxColor = 120;
    private string flashingAnimName = "flashing";
    private string defaultAnimName = "default";
    private RectTransform oxygenRoot;
    private RectTransform parentRectTransform;
    
    private Transform _selfPlayer => AvatarController.Inst.SelfController.transform;
    private Camera mainCamera => GlobalCameraManager.Inst.GlobalMainCamera;
    private Camera uiCamera => GlobalCameraManager.Inst.UICamera;
    
    private Vector2 uiOffset = new Vector2(-144,304);
    
    public override void OnCreate()
    {
        base.OnCreate();
        parentRectTransform = GetComponent<RectTransform>();
        oxygenRoot = GameObjectEx.FindChildByName(transform, "OxygenRoot").GetComponent<RectTransform>();
        oxyBar = GameObjectEx.FindChildByName(transform,"oxygenbar").GetComponent<ProgressBarItem>();
        divingBars.Add(GameObjectEx.FindChildByName(transform,"divingbar").GetComponent<ProgressBarItem>());
        divingBars.Add(GameObjectEx.FindChildByName(transform, "divingbar2").GetComponent<ProgressBarItem>());
       
        
        OxygenManager.Inst.AddOxygenUpdateListener(RefreshOxygenBar);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        OxygenManager.Inst.RemoveOxygenUpdateListener(RefreshOxygenBar);
    }

    protected override void Update()
    {
        var pos = GameCameraUtils.Inst.ConvertPlayerPosToUI(parentRectTransform);
        if (pos != Vector2.zero)
        {
            oxygenRoot.localPosition = pos + uiOffset;
        }
    }
    

    public void RefreshOxygenBar(float cueValue, float maxValue)
    {
        oxyBar.SetProgress(cueValue, maxValue);
    }

    public void RefreshOxygenBar(OxygenBarConfig config)
    {
        oxyBar.SetProgress(config.oxygenRatio);
        Setbar(config);
        float curColor = config.oxygenRatio * (maxColor / 360);
        oxyBar.image.color = Color.HSVToRGB(curColor, 1, 1);
        SetFlashingAnim(config.oxygenRatio <= 0);
    }
    
    void Setbar(OxygenBarConfig config)
    {
        for (int i = 0; i < divingBars.Count; i++)
        {
            if (divingBars.Count <= config.ProgressBarItemCount)
            {
                divingBars[i].gameObject.SetActive(true);
            }
            else
            {
                if (i <= config.ProgressBarItemCount - 1)
                {
                    divingBars[i].gameObject.SetActive(true);
                }
                else
                {
                    divingBars[i].gameObject.SetActive(false);
                }
            }
            // divingBars[i].SetProgress(config.divingRatio[i]);
        }
    }

    public void SetFlashingAnim(bool open)
    {
        if (open)
        {
            oxyFlashingAnim.Play(flashingAnimName);
        }
        else
        {
            oxyFlashingAnim.Play(defaultAnimName);
        }
    }
}
