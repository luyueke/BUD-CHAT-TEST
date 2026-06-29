using GameData.BaseInfo;
using Message;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Dropdown;

namespace AIGame.Base
{
    public class AIParkDropdownTrigger : MonoBehaviour
    {
        public void OnDisable()
        {
            if (gameObject.name != "Template")
            {
                MessageHelper.Broadcast(MessageName.OnDropdownTrigger,transform.parent);
            }
        }
    }
}