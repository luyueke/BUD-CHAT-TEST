using System;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Base;
using Game.KinematicCharacter;
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.SurfaceDetection.Base
{
    public class SurfaceDetectManager : GameInstance<SurfaceDetectManager>, IAutoInit
    {
        private bool _enable = false;

        public bool Enable
        {
            get => _enable;
            set
            {
                if (value == _enable) return;
                ClearData();
                _enable = value;
            }
        }

        private Dictionary<string, BaseSurfaceHandler> _handlers;
        private BaseSurfaceHandler _curSurfaceHandler;
        private BaseSurfaceHandler _nextSurfaceHandler;
        private GameObject _newHitSurfaceObj;
        public SurfaceRaycastController RaycastCtr;
        private Collider[] _curRaycastHits = new Collider[16]; //最大球形射线检测数 TODO:已用Layer限定，暂定容量为16
        
        //Cache减少GameObject.Tag访问时的GC
        private Dictionary<Collider, BaseSurfaceHandler> _colliderHandlerCache;
        private const int MAX_CH_CACHE_CAPACITY = 16 * 2;

        public void Init()
        {
            RaycastCtr = new SurfaceRaycastController();
            InitSurfaceHandler();
            KinematicCharacterSystem.DoGroundDetect = OnFixedUpdate;
            _colliderHandlerCache ??= new Dictionary<Collider, BaseSurfaceHandler>(MAX_CH_CACHE_CAPACITY);
        }

        public override void Release()
        {
            KinematicCharacterSystem.DoGroundDetect = null;
            base.Release();
            RaycastCtr?.Release();
            RaycastCtr = null;
            ClearSurfaceHandler();
            _colliderHandlerCache?.Clear();
        }


        private void ClearData()
        {
            _curSurfaceHandler = null;
            _nextSurfaceHandler = null;
            _colliderHandlerCache?.Clear();
        }

        private void OnFixedUpdate()
        {
            if (!Enable)
            {
                return;
            }

            // 各handler可以控制IsCanSurfaceDetect返回值来决定是否要进行地表检测
            if (_curSurfaceHandler != null && !_curSurfaceHandler.IsCanSurfaceDetect())
            {
                return;
            }

            Profiler.BeginSample($"{nameof(SurfaceDetectManager)} OnFixedUpdate Start--Raycast");

            if (RaycastCtr == null) return;
            AutoCleanColliderHandlerCache();
            Array.Clear(_curRaycastHits, 0, _curRaycastHits.Length);
            RaycastCtr.OverlapSphereNonAlloc(ref _curRaycastHits);

            _nextSurfaceHandler = null;
            _newHitSurfaceObj = null;
            foreach (var hitCollider in _curRaycastHits)
            {
                if (hitCollider == null) continue;
                
                //跳过检测玩家自身
                if (hitCollider.gameObject == AvatarController.Inst.SelfController.Motor.gameObject)
                {
                    continue;
                }

                // get tag 导致252B GC, 使用cache以减少GC
                BaseSurfaceHandler handler = GetColliderCacheHandler(hitCollider);
                if (handler == null)
                {
                    // Debug.Log($"hitCollider.tag-----{hitCollider.tag}, layer:{hitCollider.gameObject.layer}");
                    if (_handlers.TryGetValue(hitCollider.tag, out var handler1))
                    {
                        handler = handler1;
                    }
                    else
                    {
                        //检测到非GameSurface层，默认回到Default
                        handler = _handlers.First().Value;
                    }
                }

                if (handler == null) continue;
                AddToColliderHandlerCache(hitCollider, handler);
                
                //优先级比较，只处理检测到优先级最高的地形
                if (_nextSurfaceHandler == null || handler.Priority >= _nextSurfaceHandler.Priority)
                {
                    _nextSurfaceHandler = handler;
                    _newHitSurfaceObj = hitCollider.gameObject;
                }
            }

            Profiler.EndSample();


            if (_nextSurfaceHandler == null) return;
            // LoggerUtils.Log($"SurfaceDetect: fixUpdate：last:{_curSurfaceHandler?.HitGameObject?.name}, next:{_nextSurfaceHandler?.HitGameObject?.name}, _curRaycastHits:{_curRaycastHits.Length}");

            Profiler.BeginSample($"{nameof(SurfaceDetectManager)} OnFixedUpdate Start--HandleResult");

            var secondResult = _nextSurfaceHandler.HandleOverlapRaycastResult(_curRaycastHits);
            if (secondResult)
            {
                if (_curSurfaceHandler != _nextSurfaceHandler)
                {
                    // 切换地表
                    SwitchSurface();
                }
                else if (_curSurfaceHandler != null && _curSurfaceHandler.HitGameObject != _newHitSurfaceObj)
                {
                    // 只切换同地表的GameObject
                    SwitchSurfaceGameObject();
                }
            }
            // else
            // {
            //     LoggerUtils.Log("SurfaceDetect: 二次检测未通过，不切换");
            // }

            Profiler.EndSample();
        }

        #region TODO:未来网络逻辑帧下发数据可能使用

        public void OnServerDetectSurface(GameObject newHitObj)
        {
            if (newHitObj)
            {
                _nextSurfaceHandler = _handlers[newHitObj.tag];
            }
        }

        public void OnServerSetNewHitObj(GameObject newHitObj)
        {
            _newHitSurfaceObj = newHitObj;
            OnServerDetectSurface(newHitObj);
        }

        #endregion

        #region Surface Control

        private void SwitchSurface()
        {
            ExistLastSurface();
            EnterNewSurface();
        }

        private void SwitchSurfaceGameObject()
        {
            _curSurfaceHandler.OnChange(_curSurfaceHandler.HitGameObject, _newHitSurfaceObj);
            _curSurfaceHandler.HitGameObject = _newHitSurfaceObj;
            // LoggerUtils.Log($"SwitchSurfaceGameObject: 只切换同地表的GameObject last:{_lastSurfaceHandler?.HitGameObject?.name}");
        }

        private void ExistLastSurface()
        {
            if (_curSurfaceHandler != null)
            {
                _curSurfaceHandler.HitGameObject = null;
                _curSurfaceHandler.OnExit();
                // LoggerUtils.Log($"ExistLastSurface: 切换地表 : last:{_lastSurfaceHandler?.Tag}");
            }
        }

        private void EnterNewSurface()
        {
            _curSurfaceHandler = _nextSurfaceHandler;
            _curSurfaceHandler.HitGameObject = _newHitSurfaceObj;
            _curSurfaceHandler.OnEnter();
            // LoggerUtils.Log($"EnterNewSurface: 切换地表 : last:{_lastSurfaceHandler?.HitGameObject?.name}");
        }

        #endregion


        #region Handler

        private void InitSurfaceHandler()
        {
            _handlers ??= new Dictionary<string, BaseSurfaceHandler>();
            AddSurfaceHandler(new DefaultHandler());
            _curSurfaceHandler = _handlers?.First().Value;
        }

        private void ClearSurfaceHandler()
        {
            _handlers?.Clear();
        }

        public void AddSurfaceHandler(BaseSurfaceHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.Tag))
                return;

            if (_handlers.TryAdd(handler.Tag, handler))
            {
                LoggerUtils.Log($"RegisterDetector success :{handler.Tag}");
            }
            else
            {
                LoggerUtils.LogError($"RegisterDetector failed :{handler.Tag}");
            }
        }

        #endregion

        #region Cache

        private BaseSurfaceHandler GetColliderCacheHandler(Collider c)
        {
            if (_colliderHandlerCache != null && _colliderHandlerCache.TryGetValue(c, out var handler))
            {
                return handler;
            }

            return null;
        }
        
        private void AddToColliderHandlerCache(Collider c, BaseSurfaceHandler handler)
        {
            if (_colliderHandlerCache == null || c == null || handler == null) 
                return;
            AutoCleanColliderHandlerCache();
            
            _colliderHandlerCache[c] = handler;
        }

        private void AutoCleanColliderHandlerCache()
        {
            //粗略计算，cache即将满，自动清理一次
            if (_colliderHandlerCache == null) return;
            if (_colliderHandlerCache.Count + 1 >= MAX_CH_CACHE_CAPACITY)
            {
                _colliderHandlerCache?.Clear();
                // LoggerUtils.Log("cache即将满，自动清理一次");
            }
        }

        #endregion

        #region 外部读数据接口

        /// <summary>
        /// 获取检测到表面Tag
        /// </summary>
        /// <returns></returns>
        public string GetDetectedSurfaceTag()
        {
            return _curSurfaceHandler?.Tag ?? "DefaultGround";
        }

        #endregion
    }
}