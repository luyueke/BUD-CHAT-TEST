using Game.Props.PropsManagers;
using GameData.BaseInfo;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PublishWarningPanel : BasePanel<PublishWarningPanel>
{
    public CText Txt_Mat;
    public CText Txt_Vert;
    public CText Txt_DText;
    public CButton Btn_GotIt;
    public RectTransform ScrollViewRct;

    private LimitType _curType;
    private DetailInfo _curDetailInfo;

    public override void OnCreate()
    {
        base.OnCreate();
        
        Btn_GotIt.onClick.AddListener(OnBtnGotItClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _curType = (LimitType)args[0];
        _curDetailInfo = (DetailInfo)args[1];

        InitUI();
    }

    private void InitUI()
    {
        var materialColor = "#3D3D3D";
        var vertexColor =  "#3D3D3D";
        var textColor =  "#3D3D3D";
        var limitVerticesCount = GameProfilerManager.GetLimitVerticesCount(_curType);
        var limitMaterialsCount = GameProfilerManager.GetLimitMaterialsCount(_curType);
        var limitTextCount = GameProfilerManager.GetLimitTextCount(_curType);
        if (_curDetailInfo.materials > limitMaterialsCount)
        {
            materialColor = "red";
        }
        if (_curDetailInfo.vertexs > limitVerticesCount)
        {
            vertexColor = "red";
        }
        if (_curDetailInfo.dTexts > limitTextCount)
        {
            textColor = "red";
        }
        
        var MatStr = "材质数: <color={0}>{1}</color>/{2}";
        Txt_Mat.SetLocalText(MatStr, materialColor, ConvertNumberToSize(_curDetailInfo.materials), ConvertNumberToSize(limitMaterialsCount));
        
        var VertStr = "顶点数: <color={0}>{1}</color>/{2}";
        Txt_Vert.SetLocalText(VertStr, vertexColor, ConvertNumberToSize(_curDetailInfo.vertexs), ConvertNumberToSize(limitVerticesCount));
        
        var DTextStr = "3D文字数: <color={0}>{1}</color>/{2}";
        Txt_DText.SetLocalText(DTextStr, textColor, ConvertNumberToSize(_curDetailInfo.dTexts), ConvertNumberToSize(limitTextCount));

        if (_curDetailInfo.materials == 0)
        {
            Txt_Mat.gameObject.SetActive(false);
        }
        if (_curDetailInfo.vertexs == 0)
        {
            Txt_Vert.gameObject.SetActive(false);
        }
        if (_curDetailInfo.dTexts == 0)
        {
            Txt_DText.gameObject.SetActive(false);
        }
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(ScrollViewRct);
    }

    private void OnBtnGotItClick()
    {
        CloseSelf();
    }
    
    private string ConvertNumberToSize(int value)
    {
        if (value > 1000)
        {
            return (value / 1000).ToString("#.##") + "k";
        }
        else
        {
            return value.ToString();
        }
    }
}
