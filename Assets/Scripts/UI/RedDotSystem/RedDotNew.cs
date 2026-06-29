using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class RedDotNew : MonoBehaviour
    {
        public List<ReddotType> customType = new List<ReddotType>();

        //周年庆1活动2等等 动态寻找顶点集合活动
        public int Panel;

        public string param;
        void Awake()
        {
            MessageHelper.AddListener(MessageName.ReddotNotice, RefreshState);
            MessageHelper.AddListener(MessageName.TcpTimeUpdate, RefreshState);
        }

        void OnEnable()
        {
            RefreshState();
        }

        void OnDestroy()
        {
            MessageHelper.RemoveListener(MessageName.ReddotNotice, RefreshState);
        }

        public void RefreshState()
        {
            if (Panel > 0 && TcpTimeSystem.Inst.IsInit())
            {
                var add = ActivityManager.Inst.GetActivitys((ActivityPanelType)Panel);
                foreach (var item in add)
                {
                    if (item.GetReddotTypes() != null)
                    {
                        customType.AddRange(item.GetReddotTypes());
                    }
                }
                Panel = 0;
            }
            if (customType != null && customType.Count > 0)
            {
                foreach (var item in customType)
                {
                    var ac = RedDotSystemNew.Inst.GetReddotType(item);
                    if (ac != null && ac.Invoke(param))
                    {
                        gameObject.SetActiveValid(true);
                        return;
                    }
                }
            }
            gameObject.SetActiveValid(false);
        }
    }
}