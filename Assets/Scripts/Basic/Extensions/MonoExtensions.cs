using System;
using System.Collections;
using UnityEngine;

namespace Basic.Extensions
{
    public static class MonoExtensions
    {

        /// <summary>
        /// 等待一定时间执行
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="timeOut"></param>
        /// <param name="callBack"></param>
        public static void SetTimeCallBack(this MonoBehaviour behaviour, float timeOut, Action callBack) {
            if (behaviour == null || !behaviour.gameObject.activeSelf)
            {
                return;
            }
            behaviour.StartCoroutine(CallBack(timeOut, callBack));
        }

        public static void SetFrameCallBack(this MonoBehaviour behaviour, int frameCount, Action callBack) {
            if (behaviour == null || !behaviour.gameObject.activeSelf)
            {
                return;
            }
            behaviour.StartCoroutine(CallBack(frameCount, callBack));

        }


        private static IEnumerator CallBack(float timeOut, Action callBack) {
            yield return new WaitForSeconds(timeOut);
            callBack?.Invoke();
        }

        private static IEnumerator CallBack(int frameCount, Action callBack) {
            while (frameCount > 0)
            {
                yield return null;
                frameCount--;
            }
            callBack?.Invoke();
        }


    }
}