
using System.Collections.Generic;
using Pb.Base;
using Basic.Extensions;
using DG.Tweening;
using NetEngine;
using Game.Base;
using UnityEngine;
/**
* @ Author: Jun Zhou
* @ Create Time: 2023-09-24 13:58:57
* @ Modified by: Jun Zhou
* @ Modified time: 2023-09-24 21:16:31
* @ Description: Dotween的联机物体管理
*/
namespace GameSync.Manager
{
    public class DotweenReconnectFrame
    {
        public ulong RunTotalFrame = 0; // 已经运动的总帧数
        public ulong StopTotalFrame = 0; // 已经停止的总帧数
        public ulong StopLastFrame = 0; // 最后停止在哪帧
        

        public bool IsValidate()
        {
            return RunTotalFrame > 0 || StopTotalFrame > 0 || StopLastFrame > 0;
        }
    }
    
    public class DotweenNetManager : GameInstance<DotweenNetManager>, IGameMono
    {
        List<Tween> netTweens = new List<Tween>();
        //用来做开关控制移动的同步，当前版本还用不上
        Dictionary<int, ulong[]> tweenStopFrameDict = new Dictionary<int, ulong[]>();  
        RecvFrameBst _recvFrameBst;
        ulong localFrameId = 0;
        float frameDeltaTime = 0;
        float lastFrameTime = 0;
        float localTimeLine = 0;

        public void Init()
        {
            frameDeltaTime = NetConfig.FrameRate / 1000f;
            Global.Room.OnBstEveryFrameData += OnBstEveryFrameData;
        }

        public void CollectionTween(Tween t)
        {
            var currentGameMode = GameController.GetCurrentGameMode();
            if (currentGameMode == null || currentGameMode != GameData.GameMode.Guest) return;
            if (t == null) return;
            t.SetUpdate(UpdateType.NetFrame);
            netTweens.Add(t);
            
            // 处理动画序列的逻辑
            InitSequenceStartValue(t);
            
            t.GotoPositionWithFrame(localFrameId * frameDeltaTime);
        }
        
        public void RemoveNetTween(Tween t)
        {
            var currentGameMode = GameController.GetCurrentGameMode();
            if (currentGameMode == null ||  currentGameMode != GameData.GameMode.Guest) return;
            if (t == null) return;
            if(netTweens.Contains(t))
            {
                netTweens.Remove(t);
            }
        }
        
        void InitSequenceStartValue(Tween t)
        {
            t.InitStartValue();
        }
        
        public void FixedUpdate()
        {
            if(DealFrameData()) return;
            if (localFrameId == 0) return;
            var intervalT = Time.fixedDeltaTime;
            localTimeLine += intervalT;
            DOTween.instance.NetFrameUpdate(intervalT);
            // LoggerUtils.Log($"###FixedUpdate时间差：curTime:{Time.realtimeSinceStartup}   diffTime:{Time.realtimeSinceStartup - lastFrameTime}  localTimeLine：{localTimeLine}");
            lastFrameTime = Time.realtimeSinceStartup;
        }
        
        private bool DealFrameData()
        {
            bool isAdjust = false;
            if(_recvFrameBst == null) return isAdjust;
           
            ulong frameId = _recvFrameBst.Frame.Id;
            localFrameId = frameId;
            var serverTimeLine = localFrameId * frameDeltaTime;
            if (Mathf.Abs(serverTimeLine - localTimeLine) > frameDeltaTime * 3)
            {
                LoggerUtils.Log($"物体重校准：serverTimeLine:{serverTimeLine} localTimeLine:{localTimeLine} frameId:{frameId}");
                localTimeLine = serverTimeLine;
                isAdjust = true;
                // 重新校准
                for (int i = 0; i < netTweens.Count; i++)
                {
                    if (netTweens[i].IsPlaying())
                    {
                        var keyCode = netTweens[i].GetHashCode();
                        var tTimeLine = serverTimeLine;
                        if (tweenStopFrameDict.ContainsKey(keyCode) && tweenStopFrameDict[keyCode][0] > 0)
                        {
                            tTimeLine -= tweenStopFrameDict[keyCode][0] * frameDeltaTime;
                        }

                        netTweens[i].GotoPositionWithFrame(tTimeLine);
                    }
                }
            }
            _recvFrameBst = null;
            return isAdjust;
        }


        void OnBstEveryFrameData(RecvFrameBst recvFrameBst)
        {
            HandleBstEveryFrameData(recvFrameBst);
        }
        
        public void HandleBstEveryFrameData(RecvFrameBst recvFrameBst)
        {
            _recvFrameBst = recvFrameBst;
        }
        
        public override void Release()
        {
            base.Release();
            netTweens?.Clear();
        }

        public void Update()
        {
        }

        
        #region =======同步开关控制物体移动==========
        public void Recover(Tween t, DotweenReconnectFrame Frame)
        {
            if (!Frame.IsValidate()) return;
            var keyCode = t.GetHashCode();
            if (!tweenStopFrameDict.ContainsKey(keyCode))
            {
                tweenStopFrameDict.Add(keyCode, new ulong[2]);
            }
            tweenStopFrameDict[keyCode][0] = Frame.StopTotalFrame;
            tweenStopFrameDict[keyCode][1] = Frame.StopLastFrame;

            if (Frame.RunTotalFrame > 0)
                t.GotoPositionWithFrame(Frame.RunTotalFrame * frameDeltaTime);
        }
        
        public void Play(Tween t)
        {
            if (!t.IsPlaying())
            {
                if (localFrameId > 0)
                {
                    var keyCode = t.GetHashCode();
                    if (tweenStopFrameDict.ContainsKey(keyCode))
                    {
                        if(tweenStopFrameDict[keyCode][1] > 0 && localFrameId - tweenStopFrameDict[keyCode][1] > 0)
                        {
                            tweenStopFrameDict[keyCode][0] += localFrameId - tweenStopFrameDict[keyCode][1];
                            tweenStopFrameDict[keyCode][1] = 0;
                        }
                    }
                }

                t.Play();
            }
        } 
        
        public void Pause(Tween t)
        {
            if (t.IsPlaying())
            {
                if (localFrameId > 0)
                {
                    var keyCode = t.GetHashCode();
                    if (!tweenStopFrameDict.ContainsKey(keyCode))
                    {
                        tweenStopFrameDict.Add(keyCode, new ulong[2]);
                    }
                    if (tweenStopFrameDict[keyCode][1]!=0)
                        tweenStopFrameDict[keyCode][1] = localFrameId;
                }

                t.Pause();
            }
        }
        
        #endregion =======同步开关控制物体移动=======
    }
}