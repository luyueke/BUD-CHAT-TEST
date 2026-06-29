using Message;
using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIHospitalStrongGuide : BasePanel<AIHospitalStrongGuide>
{
    public int _currentStep;

    public List<GameObject> _guideObjList = new();

    [SerializeField] private Button _bgBtn;
    [SerializeField] private AIHospitalInputAnimation _animation;

    public Dictionary<int, string> _guideBindPath = new()
    {
        {(int)AIGameHospitalConfig.EPgcGuideID.ChatBtnTips,"UIRoot/Canvas/GuestWindow/AIHospitalGuestPanel(Clone)/AIHospitalHUDPanel/AIHospitalNpcHudItem(Clone)/TargetView/Btn_ChatToNpc" },
        {(int)AIGameHospitalConfig.EPgcGuideID.ProgressTips,"UIRoot/Canvas/GuestWindow/AIHospitalGuestPanel(Clone)/AIHospitalHUDPanel/AIHospitalNpcHudItem(Clone)/TargetView/ScroBarBg" },
    };

    /// <summary>
    /// 引导的后续配置,如果后一个不是None，则要继续引导
    /// </summary>
    private Dictionary<AIGameHospitalConfig.EPgcGuideID, AIGameHospitalConfig.EPgcGuideID> _guideConfig = new Dictionary<AIGameHospitalConfig.EPgcGuideID, AIGameHospitalConfig.EPgcGuideID>()
    {
        { AIGameHospitalConfig.EPgcGuideID.TargetTips,AIGameHospitalConfig.EPgcGuideID.Nonoe  },
        { AIGameHospitalConfig.EPgcGuideID.ChatBtnTips,AIGameHospitalConfig.EPgcGuideID.Nonoe},
        { AIGameHospitalConfig.EPgcGuideID.SendMsgTips,AIGameHospitalConfig.EPgcGuideID.Nonoe  },
        { AIGameHospitalConfig.EPgcGuideID.SendEmoteMsgTips,AIGameHospitalConfig.EPgcGuideID.Nonoe  },
        { AIGameHospitalConfig.EPgcGuideID.ProgressTips,AIGameHospitalConfig.EPgcGuideID.Nonoe  },
        { AIGameHospitalConfig.EPgcGuideID.HeartTips,AIGameHospitalConfig.EPgcGuideID.Nonoe  },
    };

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _bgBtn.onClick.AddListener(OnBgBtnClick);
        if (args != null && args.Length > 0 && args[0] is AIGameHospitalConfig.EPgcGuideID step)
        {
            _currentStep = (int)step;
            ShowGuideStep((int)step);
            if (step == AIGameHospitalConfig.EPgcGuideID.SendMsgTips)
            {
                _animation.gameObject.SetActive(true);
                _animation.PlayEnterAnimation();
            }
        }
        else
            ShowGuideStep(0);
    }

    public void ShowGuideStep(int step)
    {
        _currentStep = step;
        //MessageHelper.Broadcast(MessageName.OnS9GuideStepClick, _currentStep, false);
        if (step >= _guideObjList.Count)
        {
            OnCloseSelf();
            return;
        }
        FindBindTarget(_currentStep);
        _guideObjList[step].SetActive(true);
    }

    public void OnBgBtnClick()
    {
        MessageHelper.Broadcast(MessageName.OnS9GuideStepClick, _currentStep, false);
        PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + AIGameHospitalConfig.pgcGuideId, _currentStep);
        LoggerUtils.Log($"写入引导信息 {_currentStep}");
        _guideObjList[_currentStep].SetActive(false);
        _guideConfig.TryGetValue((AIGameHospitalConfig.EPgcGuideID)_currentStep, out AIGameHospitalConfig.EPgcGuideID nextStepID);
        if (nextStepID == AIGameHospitalConfig.EPgcGuideID.Nonoe)
        {
            OnCloseSelf();
        }
        else
        {
            ShowGuideStep((int)nextStepID);
        }
    }

    private void UpdateGuidePosition(int step,Vector2 pos)
    {
        var obj = _guideObjList[step].gameObject;
        obj.transform.localPosition = pos;
    }

    private void FindBindTarget(int step)
    {
        if (_guideBindPath.TryGetValue(step,out string path))
        {
            var targetObj = GameObject.Find(path);
            if (targetObj != null)
            {
                
                var screenPos = Camera.main.WorldToScreenPoint(targetObj.transform.position);
                var canvasRect = GetComponent<RectTransform>();
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, Camera.main, out localPos);
                UpdateGuidePosition(step,localPos);
            }
        }
    }

    public void OnCloseSelf()
    {
        if (_currentStep == (int)AIGameHospitalConfig.EPgcGuideID.SendMsgTips)
        {
            var panel = UIManager.Inst.FindPanel(PanelId.AIHospitalQuickEmotePanel);
            panel?.gameObject.SetActive(true);
        }
        CloseSelf();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _bgBtn.onClick.RemoveListener(OnBgBtnClick);
    }
}



