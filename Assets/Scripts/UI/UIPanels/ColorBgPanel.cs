using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Base;

public class ColorBgPanel : BasePanel<ColorBgPanel>
{
    public Sprite[] sprites;
    private Vector3 start = new Vector3(-2048, 1024);
    private int rowNum = 10;
    private int lineNum = 8;
    private int xDistance = 250;
    private int yDistance = 40;
    private float offset = 200;
    private List<Image> imgs = new List<Image>();

    private Color _itemColor = Color.clear;
    
    protected override void Awake()
    {
        InitData();
    }

    public void InitData()
    {
        if (sprites == null || sprites.Length == 0)
        {
            return;
        }

        // 过滤掉可能的空元素，避免后续对 null.sprite 的访问
        var validSprites = new List<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                validSprites.Add(sprites[i]);
            }
        }
        sprites = validSprites.ToArray();
        if (sprites.Length == 0)
        {
            return;
        }

        transform.eulerAngles = new Vector3(0, 0, 15);

        int index = 0;
        float currentPositionX = 0;
        if (sprites == null || sprites.Length == 0)
        {
            return;
        }
        float currentPositionY = sprites[0].rect.height;
        for (int i = 0; i < lineNum * 2; i++)
        {
            for (int j = 0; j < rowNum; j++)
            {
                var obj = new GameObject();
                var img = obj.AddComponent<Image>();
                obj.transform.SetParent(transform);
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                img.sprite = sprites[index];
                img.SetNativeSize();
                currentPositionX += img.sprite.rect.width / 2;
                obj.transform.localPosition = new Vector3(currentPositionX, -(currentPositionY + yDistance) * i);
                obj.transform.localPosition += start;
                currentPositionX += img.sprite.rect.width / 2 + xDistance;
                index++;
                index %= sprites.Length;
                imgs.Add(img);
                if (_itemColor != Color.clear)
                {
                    img.color = _itemColor;
                }
            }

            if (i % 2 == 0)
            {
                currentPositionX = offset;
            }
            else
            {
                currentPositionX = 0;
            }
        }
    }


    public void SetRoration(Vec3 roration)
    {
        transform.eulerAngles = roration;
    }


    public void RefreshSprite()
    {
        int index = 0;
        for (int i = 0; i < imgs.Count; i++)
        {
            imgs[i].sprite = sprites[index];
            imgs[i].SetNativeSize();
            index++;
            index %= sprites.Length;
        }
    }

    public void SetImagesColor(Color color)
    {
        _itemColor = color;
        imgs.ForEach(i =>
        {
            if (i != null) i.color = color;
        });
    }
}