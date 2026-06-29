using System;
using System.Linq;
using System.Reflection;
using UIAgent;
using UnityEngine;

namespace Game.AvatarTool {
    public class PropSkinPartCachePool : GlobalInstance<PropSkinPartCachePool> {

        public PropSkinPartCachePool() {

        }

        public GameObject GetSkinPartObj(string id, string url) {


            GameObject propSkinObj = null;
            // 大厅中使用节点创建，在地图中使用离线渲染异步创建， 避免多次请求离线渲染数据
            if (GameAgentManager.Inst.IsInHallScene()) {
                propSkinObj = GameAgentManager.Inst.CreateProp(id,url, (tmpObj) => {
                    if (tmpObj == null) {
                        return;
                    }
                    var colliders = tmpObj.GetComponentsInChildren<Collider>();
                    foreach (var collider in colliders) {
                        collider.enabled = false;
                    }
                });
            } else {
                propSkinObj = GameAgentManager.Inst.CreatePropWithOffline(id, url, tmpObj => {
                    if (tmpObj == null) {
                        return;
                    }
                    var colliders = tmpObj.GetComponentsInChildren<Collider>();
                    foreach (var collider in colliders) {
                        collider.enabled = false;
                    }
                });

            }
            return propSkinObj;

        }


    }
}
