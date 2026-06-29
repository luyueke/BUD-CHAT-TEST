using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using xasset;

#if UNITY_EDITOR
using UnityEditor; // 仅在编辑器中引入
#endif

public class GuideMaskUtils : GlobalInstance<GuideMaskUtils>
{
    private static Shader _cutoutMaskShader;
    public static readonly Color DefaultMaskColor = new Color(0, 0, 0, 0.80f);
    private const string TARGET_CANVAS_PATH = "UIRoot/Canvas";
    public Action removeMaskCallBack;
    public bool isBlock = true;
    public bool isRayPenetration = true;
    private static Canvas GetTargetCanvas()
    {
        GameObject canvasObject = GameObject.Find(TARGET_CANVAS_PATH);
        if (canvasObject == null)
        {
            return null;
        }
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            return null;
        }
        return canvas;
    }
    public void RemoveMask()
    {
        
            GameObject maskInstance = GameObject.Find("ScreenCutoutMask");
            if (maskInstance != null)
            {
                if (Application.isPlaying) GameObject.Destroy(maskInstance);
                else GameObject.DestroyImmediate(maskInstance);
            }
            else
            {
#if UNITY_EDITOR
                EditorUtility.DisplayDialog("Guide Mask Test", "没有可移除的测试遮罩。", "OK");
                Debug.LogWarning("GuideMaskUtils (Editor): 没有可移除的测试遮罩。");
#endif
            }
        
        removeMaskCallBack?.Invoke();
    }
    // 新增重载，推荐业务层都用这个
    public GameObject CreateFullscreenCutoutMaskNormalized(
        Vector2 cutoutCenterNormalized, // (0~1)
        Vector2 cutoutSizeNormalized,   // (0~1)
        Color? maskColor = null,
        Transform parent = null,
        bool _isBlock = true,
        bool _isRayPenetration = true)
        {
        isRayPenetration = _isRayPenetration;
        // 自动归一化：如果传入的坐标大于1，则认为是像素坐标，自动除以设计分辨率
        //const float DESIGN_WIDTH = 2436f;
        //const float DESIGN_HEIGHT = 1125f;
        //if (cutoutCenterNormalized.x > 1f || cutoutCenterNormalized.y > 1f)
        //{
        //    cutoutCenterNormalized = new Vector2(cutoutCenterNormalized.x / DESIGN_WIDTH, cutoutCenterNormalized.y / DESIGN_HEIGHT);
        //}
        //if (cutoutSizeNormalized.x > 1f || cutoutSizeNormalized.y > 1f)
        //{
        //    cutoutSizeNormalized = new Vector2(cutoutSizeNormalized.x / DESIGN_WIDTH, cutoutSizeNormalized.y / DESIGN_HEIGHT);
        //}
        //
        //Canvas parentCanvas = GetTargetCanvas();
        //RectTransform canvasRT = parentCanvas.GetComponent<RectTransform>();
        //Vector2 cutoutCenterScreenSpace = new Vector2(
        //    cutoutCenterNormalized.x * canvasRT.rect.width,
        //    cutoutCenterNormalized.y * canvasRT.rect.height
        //);
        //Vector2 cutoutSizeInPixels = new Vector2(
        //    cutoutSizeNormalized.x * canvasRT.rect.width,
        //    cutoutSizeNormalized.y * canvasRT.rect.height
        //);
        return CreateFullscreenCutoutMask(
            cutoutCenterNormalized, cutoutSizeNormalized, maskColor, parent, _isBlock
        );
    }
    public GameObject CreateFullscreenCutoutMask(
        Vector2 cutoutCenterScreenSpace,
        Vector2 cutoutSizeInPixels,
        Color? maskColor = null,
        Transform parent = null,
        bool _isBlock = true)
        {   
        Canvas parentCanvas = GetTargetCanvas();
        isBlock = _isBlock;
        if (parentCanvas == null)
        {
            return null;
        }

        if (_cutoutMaskShader == null)
        {
            _cutoutMaskShader = Asset.Load("Assets/Loadable/UI/UIPanel/BootSystem/Shaders/CutoutMask.shader", typeof(Shader)).asset as Shader;
            if (_cutoutMaskShader == null)
            {
                return null;
            }
        }

        GameObject maskObject = new GameObject("ScreenCutoutMask");
        if (parent == null)
        {
            maskObject.transform.SetParent(parentCanvas.transform, false);
        }
        else
        {
            maskObject.transform.SetParent(parent, false);
        }
        
        maskObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rt = maskObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image image = maskObject.AddComponent<Image>();
        image.raycastTarget = true; // 保持 Image 的 raycastTarget 为 true
        Material cutoutMaterial = new Material(_cutoutMaskShader);
        image.material = cutoutMaterial;

        // 添加并配置 CutoutRaycastFilter
        CutoutRaycastFilter raycastFilter = maskObject.AddComponent<CutoutRaycastFilter>();

        Color finalMaskColor = maskColor ?? DefaultMaskColor;

        // FINAL FIX: The core issue is a coordinate system mismatch.
        // The user provides a coordinate based on the Canvas's design resolution, with the origin at the bottom-left.
        // The shader, however, needs a local coordinate where the origin is at the center of the mask (and canvas).
        // The following calculation performs the correct transformation.
        // We completely replace the old, incorrect ScreenPointToLocalPointInRectangle call.
        RectTransform canvasRT = parentCanvas.GetComponent<RectTransform>();
        float localX = cutoutCenterScreenSpace.x - (canvasRT.rect.width * 0.5f);
        float localY = cutoutCenterScreenSpace.y - (canvasRT.rect.height * 0.5f);
        Vector2 localCutoutPos = new Vector2(localX, localY);
        // The cutoutSizeInPixels is already in the correct "canvas units", so it does not need scaling.
        
        // 设置Shader参数
        cutoutMaterial.SetVector("_CutoutPos", new Vector4(localCutoutPos.x, localCutoutPos.y, 0, 0));
        cutoutMaterial.SetVector("_CutoutSize", new Vector4(cutoutSizeInPixels.x, cutoutSizeInPixels.y, 0, 0));
        cutoutMaterial.SetColor("_Color", finalMaskColor);

        // 设置RaycastFilter参数
        raycastFilter.SetCutoutParameters(localCutoutPos, cutoutSizeInPixels);

        image.sprite = null;
        image.color = Color.white;

        return maskObject;
    }

    public static void UpdateCutoutMask(
        GameObject maskObject,
        Vector2 cutoutCenterScreenSpace,
        Vector2 cutoutSizeInPixels, // 这个参数是挖洞的像素大小
        Color? maskColor = null)
    {
        if (maskObject == null)
        {
            return;
        }

        Canvas parentCanvas = GetTargetCanvas();
        if (parentCanvas == null)
        {
            return;
        }

        Image image = maskObject.GetComponent<Image>();
        if (image == null || image.material == null || image.material.shader != _cutoutMaskShader)
        {
            return;
        }

        RectTransform rt = maskObject.GetComponent<RectTransform>();
        Material cutoutMaterial = image.material;
        CutoutRaycastFilter raycastFilter = maskObject.GetComponent<CutoutRaycastFilter>();

        // FINAL FIX: Apply the same robust coordinate system transformation here.
        RectTransform canvasRT = parentCanvas.GetComponent<RectTransform>();
        float localX = cutoutCenterScreenSpace.x - (canvasRT.rect.width * 0.5f);
        float localY = cutoutCenterScreenSpace.y - (canvasRT.rect.height * 0.5f);
        Vector2 localCutoutPos = new Vector2(localX, localY);

        // 更新Shader参数
        cutoutMaterial.SetVector("_CutoutPos", new Vector4(localCutoutPos.x, localCutoutPos.y, 0, 0));
        cutoutMaterial.SetVector("_CutoutSize", new Vector4(cutoutSizeInPixels.x, cutoutSizeInPixels.y, 0, 0));
        if (maskColor.HasValue)
        {
            cutoutMaterial.SetColor("_Color", maskColor.Value);
        }

        // 更新RaycastFilter参数
        if (raycastFilter != null)
        {
            raycastFilter.SetCutoutParameters(localCutoutPos, cutoutSizeInPixels);
        }
    }
    
}

// -----------------------------------------------------------------------------
// CutoutRaycastFilter Component
// -----------------------------------------------------------------------------
[RequireComponent(typeof(RectTransform))]
// [RequireComponent(typeof(Image))] // Image is already added by GuideMaskUtils
public class CutoutRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private Vector2 _cutoutLocalCenter;
    private Vector2 _cutoutSize;
    private RectTransform _rectTransform;
    private bool _hasCutoutParameters = false;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void SetCutoutParameters(Vector2 localCenter, Vector2 size)
    {
        _cutoutLocalCenter = localCenter;
        _cutoutSize = size;
        _hasCutoutParameters = true;
        // Debug.Log($"CutoutRaycastFilter Updated: Center={_cutoutLocalCenter}, Size={_cutoutSize}");
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!_hasCutoutParameters || !isActiveAndEnabled || _rectTransform == null)
        {
            // 如果未配置、未激活或RectTransform丢失，则默认行为（通常是阻挡，因为Image.raycastTarget=true）
            // 或者可以根据具体情况返回true或false，但通常Image的raycastTarget=true会处理
            return true;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPoint, eventCamera, out localPoint);

        Rect localCutoutRect = new Rect(
            _cutoutLocalCenter.x - _cutoutSize.x * 0.5f,
            _cutoutLocalCenter.y - _cutoutSize.y * 0.5f,
            _cutoutSize.x,
            _cutoutSize.y
        );
        if (GuideMaskUtils.Inst.isBlock)
        {
            if (localCutoutRect.Contains(localPoint))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    StartCoroutine(RemoveMaskAtEndOfFrame());
                }
                if (GuideMaskUtils.Inst.isRayPenetration)// 允不允许射线通过
                {
                    return false;
                }
                else
                {
                    return true;
                }

            }
            return true; // 点在挖空区域外，阻挡
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartCoroutine(RemoveMaskAtEndOfFrame());
            }
            return true; //阻挡
        }
    }
    private IEnumerator RemoveMaskAtEndOfFrame()
    {
        // 等待当前帧的渲染结束。
        // 使用 WaitForEndOfFrame 可以确保所有UI事件都已处理完毕。
        // yield return null; (等待下一帧) 也可以，但 WaitForEndOfFrame 更精确。
        yield return new WaitForEndOfFrame();

        // 在下一帧的开始或当前帧的末尾，安全地销毁遮罩
        if (GuideMaskUtils.Inst != null) // 检查一下单例是否还存在
        {
            GuideMaskUtils.Inst.RemoveMask();
        }
    }

}