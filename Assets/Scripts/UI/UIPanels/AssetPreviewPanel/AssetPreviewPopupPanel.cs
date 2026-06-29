using System;
using UI.Base;
using UI.BaseWidgets;

public class AssetPreviewPopupPanel : BasePanel<AssetPreviewPopupPanel>
{
    private CText _txt_Title;
    private AssetPreviewImp _assetPreviewImp;

    public override void OnCreate()
    {
        base.OnCreate();
        _txt_Title = GameObjectEx.FindChildByName(this.transform, "").GetComponent<CText>();
        
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        PreviewPopupData data = (PreviewPopupData)args[0];
        _txt_Title.text = data.TopTitle;
        
        _assetPreviewImp = Activator.CreateInstance(data.popupType) as AssetPreviewImp;
        if(_assetPreviewImp != null)
            _assetPreviewImp.ShowPanel(args);
    }
}

public class PreviewPopupData
{
    public string TopTitle;
    public Type popupType;
}

public class ToppicksBundlesPreview : AssetPreviewImp
{
    
}