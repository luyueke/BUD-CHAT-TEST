using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Basic.Utils
{
    /// <summary>
    /// 移出屏幕外工具, 移出屏幕的MonoBehaviour不停止
    /// 只给3d场景内使用
    /// </summary>
    public class FlyToTheMoonUtils : GameInstance<FlyToTheMoonUtils>
    {
        private static Vector3 OutOfSpacePos => new Vector3(99999, 99999, 99999);

        private Transform _moon;

        public Transform Moon
        {
            get
            {
                if (_moon == null)
                {
                    _moon = new GameObject(nameof(Moon)).transform;
                    _moon.position = OutOfSpacePos;
                }

                return _moon;
            }
        }

        private Dictionary<int, SpaceShipsAirline> _airlines;

        public Dictionary<int, SpaceShipsAirline> Airlines
        {
            get
            {
                if (_airlines == null)
                {
                    _airlines = new Dictionary<int, SpaceShipsAirline>();
                    _commonAirlineId = CreateAirline().Id; //初始化时自带一个,用于放一些单个的GO
                }

                return _airlines;
            }
        }

        private int _commonAirlineId = 0;

        public override void Release()
        {
            Object.Destroy(Moon.gameObject);
            base.Release();
            _airlines?.Clear();
            _airlines = null;
        }

        #region Private

        private void FlyAwayInTargetAirline(SpaceShipsAirline airline, GameObject go)
        {
            if (airline == null || go == null) return;
            if (airline.IsContainsShip(go))
            {
                LoggerUtils.LogError($"FlyAwayInTargetAirline: Already Contains the gameObject :{go.name}");
                return;
            }

            airline.AddShip(go, (ship) => { ship.StartVoyage(airline.RootTrans); });
        }

        private int GetNewAirlineId()
        {
            return Airlines.Count + 1;
        }

        private SpaceShipsAirline FindAirline(GameObject go)
        {
            foreach (var airline in Airlines.Values)
            {
                if (airline != null && airline.IsContainsShip(go))
                {
                    return airline;
                }
            }

            return null;
        }

        private SpaceShipsAirline FindAirline(int airlineId)
        {
            if (airlineId == 0) return null;
            if (Airlines.TryGetValue(airlineId, out var airline))
            {
                return airline;
            }

            return null;
        }

        private SpaceShipsAirline CreateAirline()
        {
            var newId = GetNewAirlineId();
            var newAirline = new SpaceShipsAirline()
            {
                Id = newId,
                SpaceShips = new Dictionary<GameObject, SpaceShip>()
            };

            if (Airlines.TryAdd(newId, newAirline))
            {
                var newAirlineGo = new GameObject(newAirline.Id.ToString());
                newAirlineGo.transform.parent = Moon;
                newAirlineGo.transform.localPosition = Vector3.zero;
                newAirline.RootTrans = newAirlineGo.transform;
                return newAirline;
            }

            LoggerUtils.LogError("CreateAirline 创建Airline失败!");
            return null;
        }

        private void DestroyAirline(SpaceShipsAirline airline)
        {
            airline.SpaceShips.Clear();
            Airlines.Remove(airline.Id);
            var aGo = airline.RootTrans.gameObject;
            airline.RootTrans = null;
            Object.Destroy(aGo);
        }

        #endregion


        #region Public

        /// <summary>
        /// 用于单个UI移出屏幕，还原时使用对应的ReturnInCommonAirline
        /// </summary>
        /// <param name="go">要移出屏幕的GameObject</param>
        public void FlyAwayInCommonAirline(GameObject go)
        {
            if (Airlines.TryGetValue(_commonAirlineId, out var commonAirline))
            {
                FlyAwayInTargetAirline(commonAirline, go);
            }
        }

        /// <summary>
        /// 用于一堆GameObject批量移出屏幕，还原时使用对应的ReturnAllShipsInAirline
        /// </summary>
        /// <param name="airlineId">业务层自己记录一个批次Id，如果为空则会自动生成一个批次，批量还原时可以传入ReturnAllShipsInAirline</param>
        /// <param name="go">要移出屏幕的GameObject</param>
        /// <returns>批次Id</returns>
        public int FlyAwayInNewAirline(int airlineId, GameObject go)
        {
            var airline = FindAirline(airlineId);
            if (airline == null)
            {
                airline = CreateAirline();
            }

            FlyAwayInTargetAirline(airline, go);

            return airline.Id;
        }

        public void ReturnAllShipsInAirline(int airlineId)
        {
            if (airlineId <= 0 || airlineId == _commonAirlineId) return; // 外部不允许删除公共Airline
            if (Airlines.TryGetValue(airlineId, out var airline))
            {
                if (airline.SpaceShips == null) return;

                var tobeDeleteGameObjs = new List<GameObject>();

                foreach (var ship in airline.SpaceShips.Values)
                {
                    tobeDeleteGameObjs.Add(ship.GameObj);
                }

                foreach (var obj in tobeDeleteGameObjs)
                {
                    ReturnInTargetAirline(airlineId, obj);
                }

                if (airline.SpaceShips.Count <= 0)
                {
                    LoggerUtils.Log($"Remove Airline:{airlineId}");
                    DestroyAirline(airline);
                }
            }
            else
            {
                LoggerUtils.LogError($"Not found airline from Id :{airlineId}");
            }
        }

        public void ReturnInCommonAirline(GameObject go)
        {
            if (_commonAirlineId == 0) return;
            if (go == null) return;

            if (Airlines.TryGetValue(_commonAirlineId, out var commonAirline))
            {
                var ship = commonAirline.GetSpaceShip(go);
                if (ship == null) return;
                ship.ReturnHome();
                commonAirline.RemoveShip(ship.GameObj);
            }
        }

        public void ReturnInTargetAirline(int airlineId, GameObject go)
        {
            if (Airlines.TryGetValue(airlineId, out var airline))
            {
                var ship = airline.GetSpaceShip(go);
                if (ship == null)
                {
                    LoggerUtils.LogError($"Not found GameObject in airline Id :{airlineId}");
                    return;
                }

                ship.ReturnHome();
                airline.RemoveShip(ship.GameObj);
            }
            else
            {
                LoggerUtils.LogError($"Not found airline from Id :{airlineId}");
            }
        }

        #endregion


        #region Ship

        public class SpaceShipsAirline
        {
            public int Id;
            public Transform RootTrans;
            public Dictionary<GameObject, SpaceShip> SpaceShips;

            public bool IsContainsShip(GameObject gameObject)
            {
                return SpaceShips != null && SpaceShips.ContainsKey(gameObject);
            }

            public SpaceShip GetSpaceShip(GameObject gameObject)
            {
                if (SpaceShips != null && SpaceShips.TryGetValue(gameObject, out var findShip))
                {
                    return findShip;
                }

                return null;
            }

            public bool AddShip(GameObject gameObject, Action<SpaceShip> afterAdd)
            {
                if (IsContainsShip(gameObject))
                {
                    return false;
                }

                var newShip = new SpaceShip()
                {
                    GameObj = gameObject
                };

                if (SpaceShips != null && SpaceShips.TryAdd(newShip.GameObj, newShip))
                {
                    afterAdd?.Invoke(newShip);
                    return true;
                }

                afterAdd?.Invoke(null);
                return false;
            }

            public bool RemoveShip(GameObject gameObject)
            {
                if (!SpaceShips.ContainsKey(gameObject)) return false;
                SpaceShips.Remove(gameObject);
                return true;
            }
        }

        public class SpaceShip
        {
            public GameObject GameObj;
            public Transform SourceParentTrans;
            public Vector3 SourceLocalPos;

            public void StartVoyage(Transform targetTrans)
            {
                if (GameObj == null) return;
                SourceParentTrans = GameObj.transform.parent;
                SourceLocalPos = GameObj.transform.localPosition;
                GameObj.transform.parent = targetTrans;
                GameObj.transform.localPosition = Vector3.zero;
            }

            public void ReturnHome()
            {
                if (GameObj == null) return;
                GameObj.transform.parent = SourceParentTrans;
                GameObj.transform.localPosition = SourceLocalPos;
            }
        }

        #endregion
    }
}