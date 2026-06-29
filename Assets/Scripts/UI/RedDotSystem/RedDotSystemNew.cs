using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class RedDotSystemNew : GlobalInstance<RedDotSystemNew>
    {
        public Dictionary<ReddotType,Func<string,bool>> ReddotActions = new Dictionary<ReddotType, Func<string, bool>>();


        public void AddReddotType(ReddotType type,Func<string, bool> func) {
            if (!ReddotActions.ContainsKey(type))
            {
                ReddotActions.Add(type,func);
            }
            else
            {
                ReddotActions[type] = func;
            }
        }

        public Func<string, bool> GetReddotType(ReddotType type)
        {
            if (ReddotActions.ContainsKey(type))
            {
                return ReddotActions[type];
            }
            return null;
        }


    }
}