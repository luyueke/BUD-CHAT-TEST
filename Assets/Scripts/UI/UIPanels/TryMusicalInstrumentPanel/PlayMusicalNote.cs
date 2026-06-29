using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.U2D;
using System;
using Pb.Game;
using Random = UnityEngine.Random;

public class PlayMusicalNote : MonoBehaviour, IPointerDownHandler
{
    public static int lastColor;
    public SpriteAtlas spriteAtlas;
    public List<Sprite> colorImages;
    public Image Image;
    public Image LightImage;
    public int mIndex;
    private Action<int> onPointDownAction;

    public void SetIndex(int index, Action<int> action)
    {
        mIndex = index;
        onPointDownAction = action;
        Image.sprite = spriteAtlas.GetSprite($"{index}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointDownAction?.Invoke(mIndex);
        if (colorImages.Count == 0) return;
        LightImage.DOKill();
        LightImage.sprite = colorImages[RandomColor()];
        LightImage.color = Color.white;
        LightImage.DOFade(0, 0.3f).SetDelay(0.5f);
    }

    private int RandomColor()
    {
        var index = Random.Range(0, colorImages.Count);
        if (index == lastColor) index = (index + 1) % colorImages.Count;
        lastColor = index;
        return index;
    }

    public void SetColor(SyllablePlayData data)
    {
        if (colorImages.Count == 0) return;
        LightImage.DOKill();
        LightImage.sprite = colorImages[RandomColor()];
        LightImage.color = Color.white;
        LightImage.DOFade(0, 0.3f).SetDelay(data.Length>0.5f?data.Length:0.5f);
    }
    private void OnDestroy()
    {
        LightImage.DOKill();
    }
}
