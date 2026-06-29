using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UGCEditor
{

    public abstract class BasePaintingHandler
    {
        protected GridMapCanvas mapCanvas;

        public BasePaintingHandler(GridMapCanvas _canvas)
        {
            mapCanvas = _canvas;
        }

        public virtual void OnEnable()
        {
            
        }

        public virtual void OnDisabled()
        {
            
        }

        public virtual void OnPointDownClick(PointerEventData eventData)
        {
        }

        public virtual void OnPointUpClick(PointerEventData eventData)
        {
        }

        public virtual void OnPointClick(PointerEventData eventData)
        {
        }

        public virtual void OnUpdate()
        {
        }

    }
    
    public class UGCTextHander: UGCImportHander
    {
        public UGCTextHander(GridMapCanvas _canvas) : base(_canvas)
        {
            curManager = UGCImportTextManager.Inst;
        }
    }

    public class UGCPhotoHander: UGCImportHander
    {
        public UGCPhotoHander(GridMapCanvas _canvas) : base(_canvas)
        {
            curManager = UGCImportPhotoManager.Inst;
        }
        
    }

    public class UGCImportHander: BasePaintingHandler
    {
        protected UGCBaseImportManger curManager;

        public UGCImportHander(GridMapCanvas _canvas) : base(_canvas)
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();
            curManager.SetElementHandleCanUse(true);
        }
        

        public override void OnDisabled()
        {
            base.OnDisabled();
            if (TransformInteractorController.Inst.InterActor)
            {
                TransformInteractorController.Inst.InterActor.ResetInfo();
                UGCImportPhotoManager.Inst.CurrentSelectBehaviour = null;
            }
            curManager.SetElementHandleCanUse(false);
        }

        public override void OnPointClick(PointerEventData eventData)
        {
            if (TransformInteractorController.Inst.InterActor)
            {
                TransformInteractorController.Inst.InterActor.ResetInfo();
                curManager.CurrentSelectBehaviour = null;
            }
        }
    }


    public class UGCOilDrumHandler : BasePaintingHandler
    {
        public UGCOilDrumHandler(GridMapCanvas _canvas) : base(_canvas)
        {
        }

        public override void OnPointDownClick(PointerEventData eventData)
        {
            var fileType = UGCPaintToolSettings.Current.oilDrumFileType;
            var cellPos = mapCanvas.GetCurrentPixelPosition();
            if (eventData.pointerId == 0 && mapCanvas.IsCanDrawArea(cellPos))
            {
                switch (fileType)
                {
                    case UGCFillType.Fill:
                        FillPart(cellPos);
                        break;
                    case UGCFillType.FillAll:
                        var col = UGCPaintToolSettings.Current.UGCColor;
                        mapCanvas.SetFullPixel(col);
                        mapCanvas.ApplyGenerateTexture();
                        break;
                }
                mapCanvas.AddRecord();
            }
        }
        
        private void FillPart(Vector2Int clickCoords)
        {
            Color startColor = mapCanvas.GetColorByPixel(clickCoords);
            Color targetColor = UGCPaintToolSettings.Current.UGCColor;
        
            if (startColor == targetColor)
                return;
        
            Stack<Vector2Int> fillStack = new Stack<Vector2Int>();
            fillStack.Push(clickCoords);
        
            while (fillStack.Count > 0)
            {
                Vector2Int coords = fillStack.Pop();
                int x = coords.x;
                int y = coords.y;
                var cellColor = mapCanvas.GetColorByPixel(coords);
                if (cellColor != startColor)
                    continue;
        
                mapCanvas.SetColorByPixel(coords, targetColor);
        
                // 向上填充
                Vector2Int upPixel = new Vector2Int(x, y + 1);
                if (mapCanvas.IsCanDrawArea(upPixel))
                    fillStack.Push(upPixel);
        
                // 向下填充
                Vector2Int downPixel = new Vector2Int(x, y - 1);
                if (mapCanvas.IsCanDrawArea(downPixel))
                    fillStack.Push(downPixel);
        
                // 向左填充
                Vector2Int leftPixel = new Vector2Int(x - 1, y);
                if (mapCanvas.IsCanDrawArea(leftPixel))
                    fillStack.Push(leftPixel);
        
                // 向右填充
                Vector2Int rightPixel = new Vector2Int(x + 1, y);
                if (mapCanvas.IsCanDrawArea(rightPixel))
                    fillStack.Push(rightPixel);
            }
            mapCanvas.ApplyGenerateTexture();
        }

        public override void OnPointUpClick(PointerEventData eventData)
        {
        }
        
    }
    
    

    public class UGCPaintingHandler : BasePaintingHandler
    {
        private bool isStartDraw = false;
        public UGCPaintingHandler(GridMapCanvas _canvas) : base(_canvas)
        {
        }
        
        public override void OnPointDownClick(PointerEventData eventData)
        {
            if (eventData.pointerId == 0)
            {
                isStartDraw = true;
            }
            else if (eventData.pointerId == 1)
            {
                isStartDraw = false;
                mapCanvas.AddRecord();
            }
        }

        public override void OnPointUpClick(PointerEventData eventData)
        {
            if (isStartDraw)
            {
                mapCanvas.AddRecord();
            }
            
            isStartDraw = false;
        }

        
        public override void OnUpdate()
        {
            if (Input.touchCount > 1 && isStartDraw)
            {
                isStartDraw = false;
                mapCanvas.AddRecord();
            }

            if (isStartDraw)
            {
                
                var cellPos = mapCanvas.GetCurrentPixelPosition();
                if (mapCanvas.IsCanDrawArea(cellPos))
                {
                    OnDrawPaint(cellPos);
                }
            }
        }
        
        private void OnDrawPaint(Vector2Int textureCoordinate)
        {
            Color color =UGCPaintToolSettings.Current.UGCColor;
            if (UGCPaintToolSettings.Current.ToolType == EditorToolType.Scissors)
            {
                color.a = 0;
            }
            UGCDrawSize dSize = UGCPaintToolSettings.Current.PaintingSize;
            if (UGCPaintToolSettings.Current.PaintingMode == UGCDrawMode.Normal)
            {
                mapCanvas.SetMultiGrids(textureCoordinate, color, (int)dSize);
            }
            else if (UGCPaintToolSettings.Current.PaintingMode == UGCDrawMode.Mirror)
            {
                mapCanvas.SetMultiGrids(textureCoordinate, color, (int)dSize);
                Vector2Int mirrorTarget = mapCanvas.GetMirrorPos(textureCoordinate);
                mapCanvas.SetMultiGrids(mirrorTarget, color, (int)dSize, true);
            }
            mapCanvas.ApplyGenerateTexture();
        }

    }

    public class UGCCanvasToolHandler : MonoBehaviour,IPointerDownHandler, IPointerUpHandler,IPointerClickHandler
    {
        private List<BasePaintingHandler> handlers = new List<BasePaintingHandler>();

        public void AddPaintingHandler(BasePaintingHandler handler)
        {
            handler.OnEnable();
            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        public void RemoveAllHandle()
        {
            handlers.ForEach(x=>x.OnDisabled());
            handlers.Clear();
        }

        public void RemovePaintHandler(BasePaintingHandler handler)
        {
            handler.OnDisabled();
            if (handlers.Contains(handler))
            {
                handlers.Remove(handler);
            }
        }


        private void Update()
        {
            if (handlers != null && handlers.Count > 0)
            {
                handlers.ForEach(x=>x.OnUpdate());
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {

            if (handlers != null && handlers.Count > 0)
            {
                handlers.ForEach(x=>x.OnPointDownClick(eventData));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handlers != null && handlers.Count > 0)
            {
                handlers.ForEach(x=>x.OnPointUpClick(eventData));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (handlers != null && handlers.Count > 0)
            {
                handlers.ForEach(x=>x.OnPointClick(eventData));
            }
        }
    }
}