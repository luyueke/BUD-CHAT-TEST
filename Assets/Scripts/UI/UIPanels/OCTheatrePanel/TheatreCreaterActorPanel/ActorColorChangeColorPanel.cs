using System;
using System.Collections.Generic;
using Game.GameSetting;
using GameData;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;


public class ActorColorChangeColorPanel : BasePanel<ActorColorChangeColorPanel>
{
    public Slider HueSlider;//H Slide
    public Slider ChormaSlider;//V Slide
    public Slider BrightSlider;//S Slide
    public Image HueImg;//HImg
    public Image ChormaImg;//VImg
    public Image BrightImg;//SImg
    public Image colorImg;//左侧颜色展示Image
    public Button btn_closs;
    private Color selectColor;
    
    private object[] m_args;
    public override void OnCreate()
    {
        btn_closs.onClick.AddListener(CloseSelf);
        HueSlider.onValueChanged.AddListener(OnHueSliderValueChanged);
        BrightSlider.onValueChanged.AddListener(OnBightSliderValueChanged);
        ChormaSlider.onValueChanged.AddListener(OnChormaSliderValueChanged);
        InitSprite();
    }
    public override void OnShow(params object[] args)
    {
        m_args = args;
        selectColor = args[1] as Color? ?? default;
        SetColorHSV(selectColor);
    }
    public void InitSprite()
    {
        HueImg.sprite = Sprite.Create(ColorPink.colorPink.HueTex, new Rect(Vector2.zero, new Vector2(7, 1)), Vector2.one * 0.5f);
        ChormaImg.sprite = Sprite.Create(ColorPink.colorPink.ChromaTex, new Rect(new Vector2(0.5f, 0), Vector2.one), Vector2.one * 0.5f);
        BrightImg.sprite = Sprite.Create(ColorPink.colorPink.BrightTex, new Rect(new Vector2(0.5f, 0), Vector2.one), Vector2.one * 0.5f);
        ColorPink.colorPink.SetHueSprite();
    }
    
    /// <summary>
    /// 设置颜色值对应的HSV
    /// </summary>
    public void SetColorHSV(Color color)
    {
        float H, S, V;
        Color.RGBToHSV(color, out H, out S, out V);
        HueSlider.value = H;
        ChormaSlider.value = S;
        BrightSlider.value = V;
        ColorPink.colorPink.UpdateChormaSprite(H, V);
        ColorPink.colorPink.UpdateBrightSprite(H, S);
        colorImg.color = color;
    }
    private Color GetColor()
    {
        var color = Color.HSVToRGB(HueSlider.value, ChormaSlider.value, BrightSlider.value);
        return color;
    }
    
    private void OnHueSliderValueChanged(float value)
    {
        ColorPink.colorPink.UpdateChormaSprite(value, BrightSlider.value);
        ColorPink.colorPink.UpdateBrightSprite(value, ChormaSlider.value);
        colorImg.color = GetColor();
        m_args[1] = colorImg.color;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
    }
    private void OnChormaSliderValueChanged(float value)
    {
        ColorPink.colorPink.UpdateBrightSprite(HueSlider.value, value);
        colorImg.color = GetColor();
        m_args[1] = colorImg.color;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
    }
    private void OnBightSliderValueChanged(float value)
    {
        ColorPink.colorPink.UpdateChormaSprite(HueSlider.value, value);
        colorImg.color = GetColor();
        m_args[1] = colorImg.color;
        MessageHelper.Broadcast(MessageName.OnSliderValueChanged,m_args);
    }
    

    private void OnDestroy()
    {
        //LocalDataUtils.Inst.NotifySaveLocal();
        ColorPink.colorPink.Release();
    }
}
