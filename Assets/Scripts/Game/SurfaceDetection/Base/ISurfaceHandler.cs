using UnityEngine;

namespace Game.SurfaceDetection.Base
{
    public interface ISurfaceHandler
    {
        /// <summary>
        /// 二次处理Physics.OverlapSphere的检测结果，例如水方块还需要二次进行射线检测，并自行返回bool值确定是否进入水
        /// </summary>
        /// <param name="colliders">Physics.OverlapSphere的检测结果</param>
        /// <returns>true:进入某地表</returns>
        public bool HandleOverlapRaycastResult(Collider[] colliders);

        /// <summary>
        /// 进入某地表
        /// </summary>
        public void OnEnter();

        /// <summary>
        /// 切换地表物体，例如从A雪方块->B雪方块
        /// </summary>
        public void OnChange(GameObject oldGo, GameObject newGo);

        /// <summary>
        /// 退出某地表
        /// </summary>
        public void OnExit();

        /// <summary>
        /// 控制下一帧是否进行地表检测
        /// </summary>
        /// <returns></returns>
        public bool IsCanSurfaceDetect();
    }
}