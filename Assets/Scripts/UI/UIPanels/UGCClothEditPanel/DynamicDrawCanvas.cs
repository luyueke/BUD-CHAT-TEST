using System.Collections;
using System.Collections.Generic;
using Game.Config;
using UnityEngine;
using UnityEngine.UI;

public class DynamicDrawCanvas : MonoBehaviour
{
    public RawImage mRawImage;
    public Camera mDrawCamera;
    public Transform mDrawPanel;

    //用于做提前透明裁剪
    public Material mTransparentMat;
    public Material mFullFaceTransparentMat;
    
    private Material _transparentMaterial;
    private GameObject mPaint;

    
   
    public void InitCanvas(RectTransform mDrawBoard, CanvasScaler mMainCanvas,Vector2Int texSize)
    {
        var realHeight = mMainCanvas.referenceResolution.x * Screen.currentResolution.height / Screen.currentResolution.width;
        var ratio = (float) mDrawBoard.rect.height /realHeight;
        mDrawCamera.orthographicSize *= ratio;
        mDrawCamera.Render();
        var scaleRatio = (float)texSize.y / mDrawBoard.rect.height;
        mDrawPanel.localScale = new Vector3(scaleRatio, scaleRatio, 1);
    }

    public void SetCameraTargetTexture(RenderTexture rt)
    {
        mDrawCamera.targetTexture = rt;
        mDrawCamera.Render();
    }

    public void SetRawImage(RenderTexture rt)
    {
        mRawImage.texture = rt;
    }
    
    /// <summary>
    /// isFullFace用于判断是否为全脸模版，全脸模版因缩放原因需要用特殊shader
    /// </summary>
    /// <param name="finalTex"></param>
    /// <param name="filterAlphaTexture"></param>
    /// <param name="isFullFace"></param>
    public void SetTransparentMat(string templateId,Texture finalTex,Texture filterAlphaTexture)
    {
        if (GameConsts.TransparentPart.Contains(templateId))
        {
            bool isFullFace = templateId.Equals(GameConsts.FullTransparentPartId);
            if (_transparentMaterial == null)
            {
                _transparentMaterial = new Material(isFullFace ? mFullFaceTransparentMat : mTransparentMat);
                mRawImage.material = _transparentMaterial;
            }
            _transparentMaterial.SetTexture("_MainTex", finalTex);
            _transparentMaterial.SetTexture("_opacitytex", filterAlphaTexture);
        }
    }

    public bool IsTransparentMat(string templateId)
    {
        return GameConsts.TransparentPart.Contains(templateId);
    }
    // public void ResetUIRoot(GameObject prefab, UGCData data, RenderTexture render, RenderTexture mask, RenderTexture result)
    // {
    //     mPaint = Instantiate(prefab, transform.parent);
    //     mPaint.transform.localPosition = transform.localPosition + new Vector3(0, -30, 0);
    //     mDrawPanel.SetParent(mPaint.transform.Find("DrawCanvas"), false);
    //     mMaterial = mPaint.transform.Find("DrawCanvas/RawImage").GetComponent<RawImage>().material;
    //     mMaterial.SetTexture(data.faceMatTextureName, render);
    //     mMaterial.SetTexture(data.faceMatTextureName + "_mask", mask);
    //     mPaint.GetComponent<Camera>().targetTexture = result;
    // }
}
