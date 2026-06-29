using DG.Tweening;
using GameData.BaseInfo;
using GameData.UGCData;
using GmaeUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIGameLoadingPanel : BasePanel<AIGameLoadingPanel>
{
    public GameObject AIHospitalGroup;
    public Slider AIHospitalSlider;

    public AIGameLoadingParkGroup AIParkGroup;
    public Image AIParkSlider;

    public Action OnComplete { set; private get; }
    private Tween _tween_Progress;

    public static bool ParkSceneLoaded; // 暂时用于乐园场景加载完毕 不影响其他游戏 后续需要同步一起使用

    public override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        ParkSceneLoaded = false;
        base.OnDestroy();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            var preStartReq = (PreStartReq)args[0];
            var req = new JObject()
            {
                ["gameId"] = preStartReq.gameId,
                ["npcId"] = preStartReq.npcId,
                ["mapId"] = preStartReq.mapId,
            };

            if (preStartReq.gameId == (int)PGCGameType.AIPark)
            {
                AIParkGroup.SetData((UgcInfoRsp)args[1]);
                AIParkGroup.gameObject.SetActive(true);
                AIHospitalGroup.gameObject.SetActive(false);
            }
            else
            {
                AIParkGroup.gameObject.SetActive(false);
                AIHospitalGroup.gameObject.SetActive(true);
            }

            SetSliderOne();

            var paramStr = JsonConvert.SerializeObject(req);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PreStart, HttpMethod.POST, paramStr,
                (content) =>
                {
                    LoggerUtils.Log("AI准备阶段消息返回 ", content);
                    OnComplete?.Invoke();
                    preStartReq?.onSuccess.Invoke(content);
                    SetSlider(1, 1f, () =>
                    {
                        if (AIParkGroup.gameObject.activeSelf)
                        {
                            if (ParkSceneLoaded)
                            {
                                CloseSelf();
                            }
                        }
                        else
                        {
                            CloseSelf();
                        }
                    });
                },
                (err) =>
                {
                    LoggerUtils.Log("病娇消息发送失败", err);
                    CloseSelf();
                    preStartReq?.onFail.Invoke(err);
                },
                null, 60, 0);
        }
    }

    public override void OnHidden()
    {
        _tween_Progress.Kill();
        base.OnHidden();
    }

    private void SetSliderOne()
    {
        SetSlider(0.6f, 3f, SetSliderTwo);
    }

    private void SetSliderTwo()
    {
        SetSlider(0.9f, 7f, SetSliderThree);
    }

    private void SetSliderThree()
    {
        SetSlider(1f, 50, CloseSelf);
    }

    private void SetSlider(float val, float duration, Action ac)
    {
        _tween_Progress.Pause();
        _tween_Progress.Kill();
        if (AIHospitalGroup.gameObject.activeSelf)
        {
            _tween_Progress = AIHospitalSlider.DOValue(val, duration).OnComplete(() =>
            {
                ac?.Invoke();
            });
        }
        else if (AIParkGroup.gameObject.activeSelf)
        {
            var cur = AIParkSlider.fillAmount;
            _tween_Progress = DOTween.To(() => cur, x =>
            {
                AIParkSlider.fillAmount = x;
            }, val, duration).OnComplete(() =>
            {
                ac?.Invoke();
            });
        }
    }

}

public class PreStartReq
{
    public int gameId;
    public string npcId;
    public string mapId;
    public Action<string> onSuccess;
    public Action<string> onFail;
}
