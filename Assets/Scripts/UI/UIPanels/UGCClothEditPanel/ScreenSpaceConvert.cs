using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    /// <summary>
    /// Canvas空间与屏幕空间转化算法
    /// 来源于截图功能
    /// Date: 2023/8/22
    /// Author: Tee Li
    /// </summary>
    public class ScreenSpaceConvert
    {
        private Canvas canvas;
        private RectTransform canvasRectTr;

        public Vector2 CanvasResolution => canvasRectTr.sizeDelta;
        private float CanvasWidth => CanvasResolution.x;
        private float CanvasHeight => CanvasResolution.y;

        public ScreenSpaceConvert(Canvas canvas)
        {
            this.canvas = canvas;
            canvasRectTr = canvas.transform as RectTransform;
        }

        /// <summary>
        /// 将Canvas空间里的Rect转换为屏幕空间里的Rect
        /// </summary>
        /// <param name="rectOnCanvas">Canvas空间里的Rect</param>
        /// <returns>屏幕空间里的Rect</returns>
        public Rect FindScreenRect(Rect rectOnCanvas)
        {
            float scaleFactX = Screen.width / CanvasWidth;
            float scaleFactY = Screen.height / CanvasHeight;
            Vector2 posOnScreen = new Vector2(rectOnCanvas.min.x * scaleFactX, rectOnCanvas.min.y * scaleFactY);
            Vector2 sizeOnScreen = new Vector2(rectOnCanvas.size.x * scaleFactX, rectOnCanvas.size.y * scaleFactY);
            return new Rect(posOnScreen, sizeOnScreen);
        }

        /// <summary>
        /// 将一个UI的RectTransform转换成屏幕空间里的Rect
        /// </summary>
        /// <param name="target">UI的RectTransform</param>
        /// <returns>对应屏幕上的Rect</returns>
        public Rect FindScreenRect(RectTransform target)
        {
            Rect rectOnCanvas = FindRectOnCanvas(target);
            return FindScreenRect(rectOnCanvas);
        }

        /// <summary>
        /// 获取UI对应的Canvas空间Rect(相对于Canvas的0,0点)
        /// </summary>
        /// <param name="target">UI的RectTransform</param>
        /// <returns>对应Canvas上的Rect</returns>
        public Rect FindRectOnCanvas(RectTransform target)
        {
            RectTransform canvasTr = canvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 botLeft = canvasTr.InverseTransformPoint(corners[0]);
            Vector3 topRight = canvasTr.InverseTransformPoint(corners[2]);
            Vector3 topLeft = new Vector3(botLeft.x, topRight.y, 0);

            Vector2 rectSize = topRight - botLeft;
            Vector2 rectPos = new Vector2(CanvasWidth / 2 + topLeft.x, CanvasHeight / 2 - topLeft.y);
            return new Rect(rectPos, rectSize);
        }

        /// <summary>
        /// 将一个UI的RectTransform转换成屏幕空间里的Rect(copy需求中发现原方法有问题，改用新方法但不修改原方法)
        /// </summary>
        /// <param name="target">UI的RectTransform</param>
        /// <returns>对应屏幕上的Rect</returns>
        public Rect FindCopyScreenRect(RectTransform target)
        {
            Rect rectOnCanvas = FindCopyRectOnCanvas(target);
            return FindScreenRect(rectOnCanvas);
        }

        /// <summary>
        /// 获取UI对应的Canvas空间Rect(相对于Canvas的0,0点)(copy需求中发现原方法有问题，改用新方法但不修改原方法)
        /// </summary>
        /// <param name="target">UI的RectTransform</param>
        /// <returns>对应Canvas上的Rect</returns>
        public Rect FindCopyRectOnCanvas(RectTransform target)
        {
            RectTransform canvasTr = canvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 botLeft = canvasTr.InverseTransformPoint(corners[0]);
            Vector3 topRight = canvasTr.InverseTransformPoint(corners[2]);
            Vector3 topLeft = new Vector3(botLeft.x, topRight.y, 0);

            Vector2 rectSize = topRight - botLeft;
            Vector2 rectPos = new Vector2(CanvasWidth / 2 + botLeft.x, CanvasHeight / 2 + botLeft.y);
            return new Rect(rectPos, rectSize);
        }
    }
}