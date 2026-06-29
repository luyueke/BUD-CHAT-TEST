using System.Collections.Generic;
using UnityEngine;

namespace BUD.AnimPose
{
    public class AnimPropCache
    {
        public string uid;
        public GameObject prop;
    }
    
    public class AnimPropPool
    {
        private List<AnimPropCache> unUseList = new List<AnimPropCache>();
        private int cacheLength = 10;
        private GameObject bindNode;
        
        public GameObject GetUnUseProp(string uid)
        {
            var propCache = unUseList.Find(x => x.uid.Equals(uid));
            if (propCache != null)
            {
                unUseList.Remove(propCache);
                return propCache.prop;
            }
            return null;
        }

        public void RemoveProp(string uid, GameObject prop, bool isDestory = false)
        {
            if (isDestory)
            {
                Object.Destroy(prop);
                return;
            }
          
            if (unUseList.Count >= cacheLength)
            {
                var needRemoveCache = unUseList[0];
                Object.Destroy(needRemoveCache.prop);
                unUseList.RemoveAt(0);
            }
            
            if (bindNode == null)
            {
                bindNode = new GameObject("UgcAnimPropNode");
                bindNode.SetActive(false);
            }
            
            prop.transform.SetParent(bindNode.transform);
            AnimPropCache propCache = new AnimPropCache();
            propCache.prop = prop;
            propCache.uid = uid;
            unUseList.Add(propCache);
        }

        public void Release()
        {
            for (var i = 0; i < unUseList.Count; i++)
            {
                if (unUseList[i] != null)
                {
                    Object.Destroy(unUseList[i].prop);
                }
            }
            unUseList.Clear();
            Object.Destroy(bindNode);
        }

    }
}