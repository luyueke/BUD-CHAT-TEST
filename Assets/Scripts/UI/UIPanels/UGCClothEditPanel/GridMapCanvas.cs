using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Basic;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;

namespace Game.UGCEditor
{
    public class UGCPartTextures
    {
        public Texture2D mTex;
        public Texture2D aTex;

        public RenderTexture mRT;
        public RenderTexture aRT;

        public RenderTexture finalRT;
    }
    
    
    public class GridMapCanvas : MonoBehaviour
    {
        public enum TextureType
        {
            Cloth,
            Cloth_Alpha,
            Hat,
            Hat_Alpha,
            Material
        }
        public Material XBRMaterial;
        public RawImage DrawRawImage;
        public Transform lineParent;
        public Image linePrefab;
        
        private int pixelOffset;
        public float gridOffset;
        private Vector3 gridOffsetX;
        private Vector3 gridOffsetY;
        private Vector3 leftDownPosition;
        public int GridCountX = 32;
        public int GridCountY = 32;
        public float lineHeight = 3;
        public Vector2Int GenerateTextureSize = new Vector2Int(32,32);
        public Vector2Int FinalTextureSize = new Vector2Int(256, 256);
        public Action OnPointerDownEvent;

        private RenderTexture finalTex;
        private Material clothesPartMat;
        private Material clonePartMat;

        private Vector3 lineRotation = new Vector3(0, 0, 90);
        private bool isScissors;
        private Vector2 mapRowSize;
        private Vector2 mapColSize;

        private Texture2D genTexture;
        private RenderTexture filterTexture;
        private Texture2D genAlphaTexture;
        private RenderTexture filterAlphaTexture;
        private RectTransform rawRectTransform;
        private bool isStartDraw = false;
        private int texPass = 4;
        private Dictionary<Vector2Int, Color> partsPixels;
        private List<Vector2Int> inoperableArea = new List<Vector2Int>();

        public UGCDrawMode curMode = UGCDrawMode.Normal;
        //与背景相同的颜色
        private Color backgroundColor = new Color(0.9372f, 0.9372f, 0.9372f, 0);
        private RawImage dyRawImage;
        private Dictionary<Vector2Int, Color> beforeDrowGridPairs = new Dictionary<Vector2Int, Color>();
        private Dictionary<Vector2Int, Color> afterDrowGridPairs = new Dictionary<Vector2Int, Color>();
        public Action<RenderTexture> OnDrawChange;
        public Action<Dictionary<Vector2Int, Color>, Dictionary<Vector2Int, Color>> OnAddRecordEvent;

        // Start is called before the first frame update
        public void OnCreate(CanvasType canvasType)
        {
            var count = GetCanvasTypeCount(canvasType);
            GridCountX = count;
            GridCountY = count;
            GenerateTextureSize = new Vector2Int(count, count);
            Vector3 mapSize = DrawRawImage.rectTransform.sizeDelta;
            leftDownPosition = DrawRawImage.transform.localPosition - mapSize / 2;
            float gridSizeX = mapSize.x / GridCountX;
            float gridSizeY = mapSize.y / GridCountX;
            if (gridSizeX != gridSizeY)
            {
                Debug.LogError("RawImage is not Square");
            }
            pixelOffset = GenerateTextureSize.x / GridCountX;
            gridOffset = gridSizeX;
            gridOffsetX = new Vector3(gridOffset, 0, 0);
            gridOffsetY = new Vector3(0, gridOffset, 0);
            mapRowSize = new Vector2(mapSize.x, lineHeight);
            mapColSize = new Vector2(mapSize.y, lineHeight);
            rawRectTransform = DrawRawImage.GetComponent<RectTransform>();
            DrawMapGrid();
        }
        public int GetCanvasTypeCount(CanvasType canvasType)
        {
            switch (canvasType)
            {
                case CanvasType.Canvas_32:
                    return 32;
                case CanvasType.Canvas_64:
                    return 64;
                default:
                    return 32;
            }
        }
        private void DrawMapGrid()
        {
            for (int x = 0; x < GridCountX + 1; x++)
            {
                var lineGo = GameObject.Instantiate(linePrefab, lineParent);
                lineGo.rectTransform.anchoredPosition = leftDownPosition + gridOffsetX * x;
                lineGo.rectTransform.sizeDelta = mapRowSize;
                lineGo.transform.localEulerAngles = lineRotation;
                lineGo.gameObject.SetActive(true);
            }

            for (int y = 0; y < GridCountY + 1; y++)
            {
                var lineGo = GameObject.Instantiate(linePrefab, lineParent);
                lineGo.rectTransform.anchoredPosition = leftDownPosition + gridOffsetY * y;
                lineGo.rectTransform.sizeDelta = mapColSize;
                lineGo.transform.localEulerAngles = Vector3.zero;
                lineGo.gameObject.SetActive(true);
            }
        }


        public void ChangeClothesPart(UGCPartTextures texData, List<Vector2Int> inoperable,
            Dictionary<Vector2Int, Color> datas)
        {
            inoperableArea = inoperable;
            partsPixels = datas;
            genTexture = texData.mTex;
            filterTexture = texData.mRT;
            genAlphaTexture = texData.aTex;
            filterAlphaTexture = texData.aRT;
            finalTex = texData.finalRT;
            AbtuAliasing();
        }
        


        public UGCPartTextures GenerateNoFilterTexture2D(Dictionary<Vector2Int, Color> datas)
        {
            UGCPartTextures texData = new UGCPartTextures();
            texData.mTex = new Texture2D(GenerateTextureSize.x, GenerateTextureSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texData.aTex = new Texture2D(GenerateTextureSize.x, GenerateTextureSize.y, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            foreach (var keyValue in datas)
            {
                DrawSingleGrid(keyValue.Key, keyValue.Value, texData.mTex, texData.aTex);
            }
            texData.mTex.Apply();
            texData.aTex.Apply();
            return texData;
        }
        
       
        public void SetFullPixel(Color col)
        {
            Vector2Int pixel = Vector2Int.zero;
            for (int i = 0; i < GridCountX; i++)
            {
                for (int j = 0; j < GridCountY; j++)
                {
                    pixel.x = i;
                    pixel.y = j;
                    if (IsCanDrawArea(pixel))
                    {
                        SetPartsPixel(pixel, col);
                        DrawSingleGrid(pixel,col, genTexture, genAlphaTexture);
                    }
                }
            }
        }

        public void SetMultiGrids(Vector2Int textureCoordinate, Color col, int size, bool isFlip = false)
        {
            Vector2Int pixel = Vector2Int.zero;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!isFlip)
                    {
                        pixel.x = textureCoordinate.x + i;
                        pixel.y = textureCoordinate.y - j;
                    }
                    else
                    {
                        pixel.x = textureCoordinate.x - i;
                        pixel.y = textureCoordinate.y - j;
                    }


                    if (IsCanDrawArea(pixel))
                    {
                        SetPartsPixel(pixel, col);
                        DrawSingleGrid(pixel, col, genTexture, genAlphaTexture);
                    }
                }
            }
        }

        #region UGC Inoperable Area
        //仅工具使用
        public void SetInoperableArea(Dictionary<Vector2Int, Color> inoArea)
        {
            foreach (var keyValuePair in inoArea)
            {
                partsPixels[keyValuePair.Key] = keyValuePair.Value;
                DrawSingleGrid(keyValuePair.Key, keyValuePair.Value, genTexture, genAlphaTexture);
            }
            genTexture.Apply();
            genAlphaTexture.Apply();
            AbtuAliasing();
        }
        #endregion

        /// <summary>
        /// 绘制单个单元格
        /// </summary>
        /// <param name="textureCoordinate"></param>
        /// <param name="col"></param>
        /// <param name="tex2D"></param>
        /// <param name="tex2DAlpha"></param>
        private void DrawSingleGrid(Vector2Int textureCoordinate, Color col, Texture2D tex2D, Texture2D tex2DAlpha)
        {

            for (int i = 0; i < pixelOffset; i++)
            {
                for (int j = 0; j < pixelOffset; j++)
                {
                    if (col.a == 0)
                    {
                        col = Color.black;
                        tex2DAlpha.SetPixel(pixelOffset * textureCoordinate.x + i,
                            pixelOffset * textureCoordinate.y + j, col);
                        col = backgroundColor;
                        tex2D.SetPixel(pixelOffset * textureCoordinate.x + i, pixelOffset * textureCoordinate.y + j,
                            col);
                    }
                    else
                    {
                        tex2D.SetPixel(pixelOffset * textureCoordinate.x + i, pixelOffset * textureCoordinate.y + j,
                            col);
                        col = Color.white;
                        tex2DAlpha.SetPixel(pixelOffset * textureCoordinate.x + i,
                            pixelOffset * textureCoordinate.y + j, col);
                    }


                }
            }
        }

        /// <summary>
        /// 根据输入位置获取对应单元格坐标
        /// </summary>
        /// <returns></returns>
        public Vector2Int GetCurrentPixelPosition()
        {
            var uiCamera = GlobalCameraManager.Inst.UICamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle
                (rawRectTransform, Input.mousePosition, uiCamera, out var localPoint);

            var textureCoordinate = new Vector2Int
            (
                Mathf.FloorToInt((localPoint.x + DrawRawImage.rectTransform.sizeDelta.x / 2f) / gridOffset),
                Mathf.FloorToInt((localPoint.y + DrawRawImage.rectTransform.sizeDelta.y / 2f) / gridOffset)
            );
            return textureCoordinate;
        }

        // private void Update()
        // {
        //     if (isStartDraw)
        //     {
        //         var curPixel = GetCurrentPixelPosition();
        //         if (IsCanDrawArea(curPixel))
        //         {
        //             OnDrawPaint(curPixel);
        //         }
        //     }
        // }

        public bool IsCanDrawArea(Vector2Int pixel)
        {
            if (pixel.x >= 0 && pixel.x < GridCountX
                             && pixel.y >= 0 && pixel.y < GridCountY
                             && !inoperableArea.Contains(pixel))
                return true;
            return false;
        }

        private bool IsCanDrawArea(int x, int y)
        {
            Vector2Int pixel = new Vector2Int(x, y);
            return IsCanDrawArea(pixel);
        }

        public void OnUndoSetGrid(Dictionary<Vector2Int, Color> undoPairs)
        {

            if (undoPairs.Count > 0)
            {
                foreach (var item in undoPairs)
                {
                    partsPixels[item.Key] = item.Value;
                    DrawSingleGrid(item.Key, item.Value, genTexture, genAlphaTexture);
                }

                genTexture.Apply();
                genAlphaTexture.Apply();
                AbtuAliasing();
            }

        }

        // private void OnDrawPaint(Vector2Int textureCoordinate)
        // {
        //     Color color = UGCPaintTool.Current.pColor;
        //     int size = (int) UGCPaintTool.Current.pBrushSize;
        //     if (isScissors)
        //     {
        //         color = backgroundColor;
        //     }
        //
        //     SetMultiGrids(textureCoordinate, color, size);
        //     if (curMode == UGCDrawMode.Mirror)
        //     {
        //         Vector2Int mirrorTarget = GetMirrorPos(textureCoordinate);
        //         SetMultiGrids(mirrorTarget, color, size, true);
        //     }
        //
        //     genTexture.Apply();
        //     genAlphaTexture.Apply();
        //     AbtuAliasing();
        //
        // }

        private void SetPartsPixel(Vector2Int textureCoordinate, Color col)
        {
            if (!beforeDrowGridPairs.ContainsKey(textureCoordinate))
            {
                // 像素首次被绘制时 partsPixels 中可能还没有该坐标的记录（新建空白画布），
                // 此时"绘制前颜色"语义上为透明，用 Color.clear 兜底。
                var prevColor = partsPixels.TryGetValue(textureCoordinate, out var c) ? c : Color.clear;
                beforeDrowGridPairs.Add(textureCoordinate, prevColor);
            }
            if (col.a==0)
            {
                partsPixels[textureCoordinate]  = Color.clear;
            }
            else
            {
                partsPixels[textureCoordinate] = col;
            }
            if (!afterDrowGridPairs.ContainsKey(textureCoordinate))
            {
                afterDrowGridPairs.Add(textureCoordinate, partsPixels[textureCoordinate]);
            }
        }

        private void SetPartsPixel(Vector2Int textureCoordinate, Color col, int size)
        {
            if (!beforeDrowGridPairs.ContainsKey(textureCoordinate))
            {
                // 同上：新建空白画布时像素可能尚未存入 partsPixels，兜底使用 Color.clear。
                var prevColor = partsPixels.TryGetValue(textureCoordinate, out var c) ? c : Color.clear;
                beforeDrowGridPairs.Add(textureCoordinate, prevColor);
            }

            partsPixels[textureCoordinate] = col;
            if (!afterDrowGridPairs.ContainsKey(textureCoordinate))
            {
                afterDrowGridPairs.Add(textureCoordinate, partsPixels[textureCoordinate]);
            }
        }

        public RenderTexture GenerateFilterTexture(Texture2D tex)
        {
            RenderTexture filterRenderTexture = new RenderTexture((int) (GenerateTextureSize.x * texPass),
                (int) (GenerateTextureSize.y * texPass), 0, RenderTextureFormat.ARGB32);
            filterRenderTexture.Create();
            UpdateFilterTexture(tex, ref filterRenderTexture);
            return filterRenderTexture;
        }

        public RenderTexture GenerateFinalTexture()
        {
            RenderTexture filterRenderTexture =
                new RenderTexture(FinalTextureSize.x, FinalTextureSize.y, 0, RenderTextureFormat.ARGB32);
            filterRenderTexture.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            filterRenderTexture.Create();
            return filterRenderTexture;
        }


        public void UpdateFilterTexture(Texture2D tex, ref RenderTexture rt)
        {
            XBRMaterial.SetVector("texture_size", new Vector4(GenerateTextureSize.x, GenerateTextureSize.y, 0, 0));
            XBRMaterial.SetTexture("decal", tex);
            XBRMaterial.SetTexture("_BackgroundTexture", tex);
            XBRMaterial.SetTexture("_MainTex", tex);
            Graphics.Blit(tex, rt, XBRMaterial);
        }

        public void ApplyGenerateTexture()
        {
            genTexture.Apply();
            genAlphaTexture.Apply();
            AbtuAliasing();
        }


        private void AbtuAliasing()
        {
            DrawRawImage.texture = genTexture;
            RenderTexture.active = filterTexture;
            UpdateFilterTexture(genTexture, ref filterTexture);
            RenderTexture.active = null;
            UpdateFilterTexture(genAlphaTexture, ref filterAlphaTexture);
            OnDrawChange?.Invoke(finalTex);
        }

        // private bool InCantClick()
        // {
        //     if (UGCPaintTool.Current.pType == PaintType.Text
        //         || UGCPaintTool.Current.pType == PaintType.Photo)
        //     {
        //         return true;
        //     }
        //
        //     return false;
        // }

        // public void OnPointerDown(PointerEventData eventData)
        // {
        //     if (InCantClick())
        //     {
        //         return;
        //     }
        //
        //     if (eventData.pointerId == 0)
        //     {
        //         UIInputReceiver.Inst.enabled = false;
        //         OnPointerDownEvent?.Invoke();
        //         isScissors = false;
        //         switch (UGCPaintTool.Current.pType)
        //         {
        //             case PaintType.Brush:
        //                 isStartDraw = true;
        //                 break;
        //             case PaintType.OilDrum:
        //                 OnOilDrumPointerDown();
        //                 break;
        //             case PaintType.Scissors:
        //                 isStartDraw = true;
        //                 isScissors = true;
        //                 break;
        //         }
        //     }
        //     else if (eventData.pointerId == 1)
        //     {
        //         if (isStartDraw)
        //         {
        //             isStartDraw = false;
        //             AddRecord();
        //
        //         }
        //
        //         if (!UIInputReceiver.Inst.enabled)
        //         {
        //             UIInputReceiver.Inst.enabled = true;
        //         }
        //
        //     }
        // }

        private void ClearDrawGridPairs()
        {
            beforeDrowGridPairs.Clear();
            afterDrowGridPairs.Clear();
        }
        
        public void SetFlip(bool isVertical = false)
        {
            Dictionary<Vector2Int, Color> flipPixels = GetFlipDic(isVertical);
            foreach (var item in flipPixels)
            {
                SetPartsPixel(item.Key, item.Value);
                DrawSingleGrid(item.Key, item.Value, genTexture,genAlphaTexture);
            }
            genTexture.Apply();
            genAlphaTexture.Apply();
            UGCDrawMode lastMode = curMode;
            curMode = UGCDrawMode.Flip;
            AddRecord();
            curMode = lastMode;
            AbtuAliasing();
        }
        
        public Dictionary<Vector2Int, Color> GetFlipDic(bool isVertical)
        {
            Vector2Int pixel = Vector2Int.zero;
            Vector2Int fPixel = Vector2Int.zero;
            Dictionary<Vector2Int, Color> flipPixels = new Dictionary<Vector2Int, Color>();
            for (int i = 0; i < GridCountX; i++)
            {
                for (int j = 0; j < GridCountY; j++)
                {
                    pixel.x = i;
                    pixel.y = j;
                    fPixel.x = i;
                    fPixel.y = j;
                    if (isVertical)
                    {
                        if (fPixel.y > GridCountY)
                        {
                            fPixel.y -= fPixel.y - GridCountY/2-1;
                        }
                        else if(fPixel.y < GridCountY)
                        {
                            fPixel.y+= (GridCountY/2 - fPixel.y)*2-1;
                        }
                    }
                    else
                    {
                        if (fPixel.x > GridCountX)
                        {
                            fPixel.x -= fPixel.x - GridCountX/2-1;
                        }
                        else if(fPixel.x < GridCountX)
                        {
                            fPixel.x+= (GridCountX/2 - fPixel.x)*2-1;
                        }
                    }
             
                    flipPixels.Add(fPixel,partsPixels[pixel]);
                }
            
            }

            return flipPixels;
        }

        // private void OnOilDrumPointerDown()
        // {
        //     var curPixel = GetCurrentPixelPosition();
        //     if (IsCanDrawArea(curPixel))
        //     {
        //         FillType fillType = UGCPaintTool.Current.pFillType;
        //         if (fillType == FillType.Fill)
        //         {
        //             FillPart(curPixel);
        //         }
        //         else
        //         {
        //             SetFullPixel();
        //         }
        //
        //         AddRecord();
        //
        //         AbtuAliasing();
        //     }
        // }

        // public void OnPointerUp(PointerEventData eventData)
        // {
        //     if (InCantClick())
        //     {
        //         return;
        //     }
        //
        //     UIInputReceiver.Inst.enabled = false;
        //
        //     if (isStartDraw)
        //     {
        //         AddRecord();
        //
        //         isStartDraw = false;
        //     }
        // }

#region copy功能

public List<Color> copyPartsPixels = new List<Color>();
    public void GetCopyInfoOnGrid(int[] indexList)
    {
        copyPartsPixels.Clear();
        Vector2Int vc = new Vector2Int();
        for (int i = indexList[2]; i <= indexList[3]; i++)
        {
            for (int j = indexList[0]; j <= indexList[1]; j++)
            {
                vc.x = j;
                vc.y = i;
                copyPartsPixels.Add(partsPixels[vc]);
            }
        }
    }

    public void SetPaste(int[] indexList,int angle)
    {
        if (copyPartsPixels.Count ==0)
        {
            return;
        }

        Vector2Int vc = new Vector2Int();
        int index = 0;
        int nAngle = angle % 360;
        switch (nAngle)
        {
            case 0 :
            case 360:
                for (int i = indexList[2]; i <= indexList[3]; i++)
                {
                    for (int j = indexList[0]; j <= indexList[1]; j++)
                    {
                        vc.x = j;
                        vc.y = i;
                        if (index>=copyPartsPixels.Count)
                        {
                            return;
                        }
                        if (partsPixels.ContainsKey(vc))
                        {
                            SetPasteSingle(vc,index);
                        }
                        index++;
                    }
                }   
                break;
            case 90:
                for (int i = indexList[1]; i >= indexList[0]; i--)
                {
                    for (int j = indexList[2]; j <= indexList[3]; j++)
                    {
                        vc.y = j;
                        vc.x = i;
                        if (index>=copyPartsPixels.Count)
                        {
                            return;
                        }
                        if (partsPixels.ContainsKey(vc))
                        {
                            SetPasteSingle(vc,index);
                        }
                        index++;
                    }
                }   
                break;
            case 180:
                for (int i = indexList[3]; i >= indexList[2]; i--)
                {
                    for (int j = indexList[1]; j >= indexList[0]; j--)
                    {
                        vc.y = i;
                        vc.x = j;
                        if (index>=copyPartsPixels.Count)
                        {
                            return;
                        }
                        if (partsPixels.ContainsKey(vc))
                        {
                            SetPasteSingle(vc,index);
                        }
                        index++;
                    }
                }
                break;
            case 270:
                for (int i = indexList[0]; i <= indexList[1]; i++)
                {
                    for (int j = indexList[3]; j >= indexList[2]; j--)
                    {
                        vc.y = j;
                        vc.x = i;
                        if (index>=copyPartsPixels.Count)
                        {
                            return;
                        }
                        if (partsPixels.ContainsKey(vc))
                        {
                            SetPasteSingle(vc,index);
                        }
                        index++;
                    }
                }   
                break;
        }

        //填充画布纹理
        genTexture.Apply();
        genAlphaTexture.Apply();
        UGCDrawMode lastMode = curMode;
        curMode = UGCDrawMode.Copy;
        AddRecord();
        curMode = lastMode;
        AbtuAliasing();
    }

    private void SetPasteSingle(Vector2Int vc ,int index)
    {
        
        SetPartsPixel(vc, copyPartsPixels[index]);
        DrawSingleGrid(vc, copyPartsPixels[index], genTexture,genAlphaTexture);
        
    }
    #endregion

        public Vec2Int GetMirrorPos(Vec2Int vec)
        {
            Vec2Int tar = vec;
            if (tar.x > GridCountX)
            {
                tar.x -= tar.x - GridCountX / 2 - 1;
            }
            else if (tar.x < GridCountX)
            {
                tar.x += (GridCountX / 2 - tar.x) * 2 - 1;
            }

            return tar;
        }

        
        /// <summary>
        /// 添加撤销重做记录
        /// </summary>
        public void AddRecord()
        {
            if (beforeDrowGridPairs.Count > 0 || afterDrowGridPairs.Count > 0)
            {
                OnAddRecordEvent?.Invoke(beforeDrowGridPairs, afterDrowGridPairs);
                ClearDrawGridPairs();
            }
        }


        #region 油漆桶填充算法

        public Color GetColorByPixel(Vector2Int pixel)
        {
            if (partsPixels.ContainsKey(pixel))
            {
                return partsPixels[pixel];
            }

            return new Color(0, 0, 0, 0);
        }

        public void SetColorByPixel(Vector2Int pixel, Color color)
        {
            if (IsCanDrawArea(pixel))
            {
                SetPartsPixel(pixel, color);
                DrawSingleGrid(pixel, color, genTexture, genAlphaTexture);
            }
        }

        // private void FillPart(Vector2Int clickCoords)
        // {
        //     Color startColor = GetColorByPixel(clickCoords);
        //     Color targetColor = UGCPaintTool.Current.pColor;
        //
        //     if (startColor == targetColor)
        //         return;
        //
        //     Stack<Vector2Int> fillStack = new Stack<Vector2Int>();
        //     fillStack.Push(clickCoords);
        //
        //     while (fillStack.Count > 0)
        //     {
        //         Vector2Int coords = fillStack.Pop();
        //         int x = coords.x;
        //         int y = coords.y;
        //
        //         if (GetColorByPixel(coords) != startColor)
        //             continue;
        //
        //         SetColorByPixel(coords, targetColor);
        //
        //         // 向上填充
        //         Vector2Int upPixel = new Vector2Int(x, y + 1);
        //         if (IsCanDrawArea(upPixel))
        //             fillStack.Push(upPixel);
        //
        //         // 向下填充
        //         Vector2Int downPixel = new Vector2Int(x, y - 1);
        //         if (IsCanDrawArea(downPixel))
        //             fillStack.Push(downPixel);
        //
        //         // 向左填充
        //         Vector2Int leftPixel = new Vector2Int(x - 1, y);
        //         if (IsCanDrawArea(leftPixel))
        //             fillStack.Push(leftPixel);
        //
        //         // 向右填充
        //         Vector2Int rightPixel = new Vector2Int(x + 1, y);
        //         if (IsCanDrawArea(rightPixel))
        //             fillStack.Push(rightPixel);
        //     }
        //
        //     //填充画布纹理
        //     genTexture.Apply();
        //     genAlphaTexture.Apply();
        // }

        #endregion
    }
}