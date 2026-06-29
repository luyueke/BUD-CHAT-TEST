using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingWidget : MonoBehaviour
{

    [SerializeField]
    private RectTransform contentTransform;
    private const int designWidth = 1257;
    private const int designHigh = 762;

    void Reset() {
        contentTransform = transform.Find("Content") as RectTransform;
        if (transform.parent != null)
        {
            var rect = ((RectTransform)transform.parent.transform).rect;
            float scaleX = rect.width / designWidth;
            float scaleY = rect.height / designHigh;
            contentTransform.localScale = new Vector3(scaleX, scaleY, 1);
        }
    }

}
