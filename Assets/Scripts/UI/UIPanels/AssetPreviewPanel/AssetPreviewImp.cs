using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IAssetPreviewPanel
{
    void ShowPanel(params object[] args);
    void InitBottomPanel(GameObject panel);
}

public class AssetPreviewImp : IAssetPreviewPanel
{
    #region ColorBg
    protected Image _imgTopBg;
    protected Image _imgBottomBg;
    #endregion
    
    #region 预览UI
    protected RawImage _previewRawImage;
    
    protected Action onCloseAct;
    protected Action buyAct;
    #endregion
    
    public virtual void ShowPanel(params object[] args)
    {
        this.buyAct = buyAct;
        this.onCloseAct = onCloseAct;
    }
    
    public virtual void InitBottomPanel(GameObject panel)
    {
        
    }
    
}
