using System;
using Game.Base;
using Game.ECS;
using UnityEngine;

namespace HLOD {
    public interface IHLODNode {
        public virtual bool isEnabled { get { return true; } }

        public GameObject assetObj { get; set; }

        public HLODGroup hlodGroup { get; set; }

        /// <summary>
        /// 首次资源加载出, 只调用一次
        /// </summary>
        public Action onAssetFirstLoaded { get; set; }

        public void AddAssetFirstLoaded(Action callBack)
        {
            if (hlodGroup != null && hlodGroup.isAssetLoaded)
            {
                callBack?.Invoke();
            }
            else
            {
                if (onAssetFirstLoaded == null)
                {
                    onAssetFirstLoaded = callBack;
                }
                else
                {
                    onAssetFirstLoaded += callBack;
                }
            }
        }

        public void Init(NodeBaseBehaviour nodeBaseBehaviour)
        {
        }

        public void ChangeToLow();

        public void ChangeToHigh();

        public void ChangeToCull();


        public virtual void Release()
        {
        }

        public void RefreshAsset(GameObject asset);

    }
}
