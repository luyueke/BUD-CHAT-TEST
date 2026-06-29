using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UI.Manager
{
    public class RenderTextureManager : GlobalInstance<RenderTextureManager>
    {
        private List<RenderTexture> allRts = new List<RenderTexture>(32);

        public override void Release()
        {
            base.Release();
            ClearAllRt();
        }

        public RenderTexture CreateTempRenderTexture(int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height);
            allRts.Add(rt);
            return rt;
        }

        /// <summary>
        /// 如果是挂在节点上的。Destroy GameObject也可以回收
        /// </summary>
        /// <param name="renderTexture"></param>
        public void ReleaseTempRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture)
            {
                // 只release会回收内存，但profiler统计会不准确
                // renderTexture.Release();
                // renderTexture = null;

                Object.Destroy(renderTexture);
            }
        }

        public void ClearAllRt()
        {
            foreach (var rt in allRts)
            {
                ReleaseTempRenderTexture(rt);
            }
        }
    }
}