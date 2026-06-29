using UnityEngine;
using UnityEngine.UI;

public class UIBGSizeToFit : MonoBehaviour {
    public RawImage customBgImg;

    private void Start()
    {
        Resize();
    }

    public void Resize()
    {
        if (customBgImg == null || customBgImg.texture == null) return;

        int texW = customBgImg.texture.width;
        int texH = customBgImg.texture.height;
        if (texH == 0) return;

        float texRatio = (float)texW / texH;

        var canvasTransform = UIManager.Inst.Canvas.GetComponent<RectTransform>();
        float screenWidth  = Mathf.Max(canvasTransform.rect.width, canvasTransform.rect.height);
        float screenHeight = Mathf.Min(canvasTransform.rect.width, canvasTransform.rect.height);
        float screenRatio  = screenWidth / screenHeight;

        float fixedSizeX, fixedSizeY;
        if (texRatio >= screenRatio)
        {
            fixedSizeY = screenHeight;
            fixedSizeX = texRatio * fixedSizeY;
        }
        else
        {
            fixedSizeX = screenWidth;
            fixedSizeY = fixedSizeX / texRatio;
        }

        customBgImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        customBgImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        customBgImg.rectTransform.sizeDelta = new Vector2(fixedSizeX, fixedSizeY);
    }
}
