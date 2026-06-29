using System;
using UnityEngine;
using UnityEngine.UI;

public class UGCColorToggleItem : MonoBehaviour
{
    public Button ColorBtn;

    public Image ColorBGImage;

    public GameObject ColorCheckImage;

    public int colorIndex = 0;
    public Color color;

    private Action<int,Color> OnSelectColor;

    private void Start()
    {
        ColorBtn.onClick.AddListener(OnToggleClick);
    }

    public void SetColor(int index, Color color,Action<int,Color> onSelect)
    {
        colorIndex = index;
        this.color=color;
        ColorBGImage.color = color;
        OnSelectColor = onSelect;
    }

    public void OnToggleClick()
    {
        OnSelectColor?.Invoke(colorIndex,color);
    }
}
