using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BGResizeFull : MonoBehaviour
{
    // Start is called before the first frame update
    public RectTransform parentView;
    public RawImage targetBg;
    void Start()
    {
        ResizeFull();
    }


    void ResizeFull()
    {
        if (parentView == null)
        {
            parentView = targetBg?.transform.parent.GetComponent<RectTransform>();
        }

        if (targetBg.texture == null) {
            return;
        }

        Rect transSize = parentView.rect;
        float screenWidth = transSize.width;
        float screenHeight = transSize.height;
        if (screenWidth < screenHeight)
        {
            screenWidth = transSize.height;
            screenHeight = transSize.width;
        }

        float screenRatio = screenWidth / screenHeight;//屏幕宽高比

        var customBgImg = targetBg;
        int texW = customBgImg.texture.width;
        int texH = customBgImg.texture.height;
        float texRatio = ((float)texW) / texH;


        if (texRatio > screenRatio) //  高度适配
        {
            customBgImg.rectTransform.sizeDelta = new Vector2(texRatio * transSize.height, transSize.height);
        } else if (texRatio < screenRatio) // 宽度适配
        {
            customBgImg.rectTransform.sizeDelta = new Vector2(transSize.width, transSize.width/texRatio);
        }
        else
        {
            customBgImg.rectTransform.sizeDelta = new Vector2(transSize.width, transSize.height);
        }
    }
}
