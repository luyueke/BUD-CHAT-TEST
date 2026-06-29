using System;
using System.Collections.Generic;
using Game.GameSetting;
using GameData;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:Meimei-LiMei
/// Description:UGC衣服自定义颜色UI界面
/// Date: 2022/6/10 13:11:29
/// </summary>
public class ColorPinkerPanel : MonoBehaviour
{
    public Slider HueSlider;//H Slide
    public Slider ChormaSlider;//V Slide
    public Slider BrightSlider;//S Slide
    public Image HueImg;//HImg
    public Image ChormaImg;//VImg
    public Image BrightImg;//SImg
    public Image colorImg;//左侧颜色展示Image
    public UGCColorToggleItem ColorPrefab;
    public Transform ColorItemParent;
    public Button SaveBtn;
    public Button DeleteBtn;
    public Action<Color> SetElementColor;
    private Color reservedColor = new Color(0.9f, 0.9f, 0.9f);//预留位颜色
    private int MaxUgcColorCount = 30;//UGC颜色最大数量
    private List<Color> ugcColors = new List<Color>();//UGC颜色值集合
    public List<UGCColorToggleItem> ugcColorItems = new List<UGCColorToggleItem>();
    private UGCColorToggleItem curColorToggleItem;
    private Color selectColor = Color.red;
    public void Init()
    {
        HueSlider.onValueChanged.AddListener(OnHueSliderValueChanged);
        BrightSlider.onValueChanged.AddListener(OnBightSliderValueChanged);
        ChormaSlider.onValueChanged.AddListener(OnChormaSliderValueChanged);
        SaveBtn.onClick.AddListener(OnSaveBtnClick);
        DeleteBtn.onClick.AddListener(OnDeleteBtnClick);
        this.gameObject.SetActive(false);
        ugcColors = GetUgcColorDatas();
        InitSprite();
        InitColorItems();
        UpdateUgcColors();
    }
    public void InitSprite()
    {
        HueImg.sprite = Sprite.Create(ColorPink.colorPink.HueTex, new Rect(Vector2.zero, new Vector2(7, 1)), Vector2.one * 0.5f);
        ChormaImg.sprite = Sprite.Create(ColorPink.colorPink.ChromaTex, new Rect(new Vector2(0.5f, 0), Vector2.one), Vector2.one * 0.5f);
        BrightImg.sprite = Sprite.Create(ColorPink.colorPink.BrightTex, new Rect(new Vector2(0.5f, 0), Vector2.one), Vector2.one * 0.5f);
        ColorPink.colorPink.SetHueSprite();
    }
    /// <summary>
    /// 初始化颜色预留位
    /// </summary>
    public void InitColorItems()
    {
        for (int i = 0; i < MaxUgcColorCount; i++)
        {
            var item = GameObject.Instantiate(ColorPrefab, ColorItemParent);
            item.gameObject.SetActive(true);
            item.ColorCheckImage.SetActive(false);
            item.SetColor(i,reservedColor, null);
            ugcColorItems.Add(item);
        }
        ColorPrefab.gameObject.SetActive(false);
    }
    /// <summary>
    /// 更新色盘颜色展示
    /// </summary>
    private void UpdateUgcColors()
    {
        for (int i = 0; i < ugcColorItems.Count; i++)
        {
            if (i < ugcColors.Count)
            {
                ugcColorItems[i].SetColor(i,ugcColors[i], SetSelectColor);
            }
            else
            {
                ugcColorItems[i].SetColor(i,reservedColor, null);
            }
        }
        if (ugcColors.Count > MaxUgcColorCount / 2)
        {
            SetColorItemShow(true);
        }
        else
        {
            SetColorItemShow(false);
        }
    }
    
    
    /// <summary>
    /// 设置色盘展示（一行/两行）
    /// </summary>
    private void SetColorItemShow(bool isShow)
    {
        for (int i = MaxUgcColorCount / 2; i < ugcColorItems.Count; i++)
        {
            ugcColorItems[i].gameObject.SetActive(isShow);
        }
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
    public void SetSelectColor(Color color)
    {
        var index = ugcColors.FindIndex(x=>x == color);
        SetSelectColor(index,color);
    }
    
    public void SetSelectColor(int index, Color color)
    {
        if (curColorToggleItem != null)
        {
            curColorToggleItem.ColorCheckImage.SetActive(false);
        }
        if (index < ugcColorItems.Count && index >= 0)
        {
            curColorToggleItem = ugcColorItems[index];
            curColorToggleItem.ColorCheckImage.SetActive(true);
        }

        selectColor = color;
        SetColorHSV(color);
        SetElementColor?.Invoke(color);
    }
    
    private void OnHueSliderValueChanged(float value)
    {
        ColorPink.colorPink.UpdateChormaSprite(value, BrightSlider.value);
        ColorPink.colorPink.UpdateBrightSprite(value, ChormaSlider.value);
        colorImg.color = GetColor();
    }
    private void OnChormaSliderValueChanged(float value)
    {
        ColorPink.colorPink.UpdateBrightSprite(HueSlider.value, value);
        colorImg.color = GetColor();
    }
    private void OnBightSliderValueChanged(float value)
    {
        ColorPink.colorPink.UpdateChormaSprite(HueSlider.value, value);
        colorImg.color = GetColor();
    }
    private void OnSaveBtnClick()
    {
        if (ugcColors.Count >= MaxUgcColorCount)
        {
            TipPanel.ShowToast("数量达到上限");
            return;
        }
        var index = ugcColors.Count;
        selectColor = colorImg.color;
        ugcColors.Add(selectColor);
        UpdateUgcColors();
        SetSelectColor(index,selectColor);
        if (ugcColors.Count == (MaxUgcColorCount / 2) + 1)
        {
            SetColorItemShow(true);
        }
        LocalDataUtils.Inst.SetUgcClothsColors( AccountDataManager.Inst.Uid ?? string.Empty,ugcColors);
    }

    public static List<Color> GetUgcColorDatas()
    {
        string id = AccountDataManager.Inst.Uid ?? string.Empty;
        var info = LocalDataUtils.Inst.GetLocalInfo();
        var cList = new List<Color>();
        if (info.PlayerLocalInfoDic != null && info.PlayerLocalInfoDic.ContainsKey(id))
        {
            for (int i = 0; i < info.PlayerLocalInfoDic[id].UgcColorDatas.Count; i++)
            {
                cList.Add(DataUtil.DeSerializeColor(info.PlayerLocalInfoDic[id].UgcColorDatas[i]));
            }
        }
        return cList;
    }

    private void OnDeleteBtnClick()
    {
        if (curColorToggleItem != null)
        {
            ugcColors.Remove(curColorToggleItem.color);
            if (curColorToggleItem != null)
            {
                curColorToggleItem.ColorCheckImage.SetActive(false);
            }
            UpdateUgcColors();
            curColorToggleItem = null;
            LocalDataUtils.Inst.SetUgcClothsColors( AccountDataManager.Inst.Uid ?? string.Empty,ugcColors);
           
        }
    }

    private void OnDestroy()
    {
        LocalDataUtils.Inst.NotifySaveLocal();
        ColorPink.colorPink.Release();
    }
}
