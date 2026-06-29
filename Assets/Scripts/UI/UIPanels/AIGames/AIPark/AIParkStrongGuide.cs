using Basic;
using Message;
using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIParkStrongGuide : BasePanel<AIParkStrongGuide>
{
    public int _currentStep;

    public List<GameObject> _guideObjList = new();

    [SerializeField] private Button _bgBtn;
    [SerializeField] private AIParkInputAnimation _animation;

    public Dictionary<int, string> _guideBindPath = new()
    {
        {(int)AIGameParkConfig.EPgcGuideID.ChatBtnTips,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/AIHospitalHUDPanel/AIHospitalNpcHudItem(Clone)/TargetView/Btn_ChatToNpc/Image_00" },
        {(int)AIGameParkConfig.EPgcGuideID.ProgressTips,"UIRoot/Canvas/GuestWindow/AIParkGuestPanel(Clone)/AIParkHUDPanel/AIParkNpcHudItem(Clone)/TargetView/ScroBarBg" },
        {(int)AIGameParkConfig.EPgcGuideID.CheckMapTips,"MiniMapBg111"},
    };

    /// <summary>
    /// 引导的后续配置,如果后一个不是None，则要继续引导
    /// </summary>
    private Dictionary<AIGameParkConfig.EPgcGuideID, AIGameParkConfig.EPgcGuideID> _guideConfig = new Dictionary<AIGameParkConfig.EPgcGuideID, AIGameParkConfig.EPgcGuideID>()
    {
        { AIGameParkConfig.EPgcGuideID.TargetTips,AIGameParkConfig.EPgcGuideID.Nonoe  },
        { AIGameParkConfig.EPgcGuideID.ChatBtnTips,AIGameParkConfig.EPgcGuideID.Nonoe},
        { AIGameParkConfig.EPgcGuideID.LinkBtnTips,AIGameParkConfig.EPgcGuideID.Nonoe},

        { AIGameParkConfig.EPgcGuideID.SendMsgTips,AIGameParkConfig.EPgcGuideID.Nonoe  },
        { AIGameParkConfig.EPgcGuideID.SendEmoteMsgTips,AIGameParkConfig.EPgcGuideID.Nonoe  },
        { AIGameParkConfig.EPgcGuideID.ProgressTips,AIGameParkConfig.EPgcGuideID.Nonoe  },
        { AIGameParkConfig.EPgcGuideID.CheckMapTips,AIGameParkConfig.EPgcGuideID.Nonoe  },
    };

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _bgBtn.onClick.AddListener(OnBgBtnClick);
        foreach (var item in _guideObjList)
        {
            item.SetActive(false);
        }
        if (args != null && args.Length > 0 && args[0] is AIGameParkConfig.EPgcGuideID step)
        {
            Debug.LogError("AIParkStrongGuide:" + args[0]);

            _currentStep = (int)step;
            ShowGuideStep((int)step);
            if (step == AIGameParkConfig.EPgcGuideID.SendMsgTips)
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
        MessageHelper.Broadcast(MessageName.OnS11GuideStepClick, _currentStep, false);
        // PlayerPrefs.SetInt(AccountDataManager.Inst.Uid + AIGameParkConfig.pgcGuideId, _currentStep);
        LoggerUtils.Log($"写入引导信息 {_currentStep}");
        _guideObjList[_currentStep].SetActive(false);
        _guideConfig.TryGetValue((AIGameParkConfig.EPgcGuideID)_currentStep, out AIGameParkConfig.EPgcGuideID nextStepID);
        if (nextStepID == AIGameParkConfig.EPgcGuideID.Nonoe)
        {
            OnCloseSelf();
        }
        else
        {
            ShowGuideStep((int)nextStepID);
        }
    }

    private void UpdateGuidePosition(int step, Vector2 pos)
    {
        var obj = _guideObjList[step].gameObject;
        obj.transform.localPosition = pos;
    }

    private void FindBindTarget(int step)
    {
        if (_guideBindPath.TryGetValue(step, out string path))
        {
            var targetObj = GameObject.Find(path);
            if (targetObj != null)
            {
                Debug.LogError(targetObj.name);
                Camera cam = null;
                Vector3 screenPos;
                if (path.Contains("UIRoot"))
                {
                    cam = UIManager.Inst.Canvas.worldCamera;
                }
                else
                {
                    cam = Camera.main;
                }

                // 获取锚点中心的世界坐标
                Vector3 pos = Vector3.zero;
                RectTransform rectTransform = targetObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 计算正中心的世界坐标，不受pivot影响
                    // Vector2 centerPoint = new Vector2(0.5f, 0.5f); // 中心点
                    // Vector3 worldCenter;
                    // RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    //     rectTransform,
                    //     RectTransformUtility.WorldToScreenPoint(null, rectTransform.position),
                    //     null,
                    //     out worldCenter
                    // );
                    Vector3[] corners = new Vector3[4];
                    rectTransform.GetWorldCorners(corners);
                    // Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
                    // pos = worldCenter;
                    // Debug.LogError(worldCenter);

                    var screenPos1 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                    var screenPos2 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);


                    var scaleFactor = UIManager.Inst.Canvas.scaleFactor;
                    screenPos1 /= scaleFactor;
                    screenPos2 /= scaleFactor;
                    // 计算屏幕坐标的中心点和大小
                    screenPos = (screenPos1 + screenPos2) * 0.5f;

                }
                else
                {
                    // 如果不是UI元素，使用transform.position
                    pos = targetObj.transform.position;
                    screenPos = cam.WorldToScreenPoint(pos);

                }
                // Debug.LogError(screenPos);
                // Debug.LogError(pos);

                var canvasRect = GetComponent<RectTransform>();
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out localPos);
                UpdateGuidePosition(step, localPos);
            }
        }
    }

    public void OnCloseSelf()
    {
        if (_currentStep == (int)AIGameParkConfig.EPgcGuideID.SendMsgTips)
        {
            var panel = UIManager.Inst.FindPanel(PanelId.AIParkQuickEmotePanel);
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



