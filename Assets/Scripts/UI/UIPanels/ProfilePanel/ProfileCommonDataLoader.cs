using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileCommonDataLoader : MonoBehaviour
    {
        public string ToUid { get; set; }
        protected string cookie;
        protected bool isEnd;
        
        
        public virtual void GetPublishList(Action<bool, List<DraftListItem>> resultAction)
        {
  
        }

        public void SetCookie(string cookie)
        {
            this.cookie = cookie;
        }
        
        public void SetIsEnd(int intEnd)
        {
            isEnd = intEnd == 1;
        }
    }
}
