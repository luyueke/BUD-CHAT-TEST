using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public interface IActivity 
    {
        void LoginActivityInfo(ActivityInfo activityInfo);

        bool IsOpen();
        
        void ShowPanel();

        List<ReddotType> GetReddotTypes();


    }
}