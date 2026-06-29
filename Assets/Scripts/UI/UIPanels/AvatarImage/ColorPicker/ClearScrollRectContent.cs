using UnityEngine;
using UnityEngine.UI;

public class ClearScrollRectContent : MonoBehaviour
{
    public ScrollRect scrollRect; // Reference to your Scroll Rect component

    public void ClearContent()
    {
        // Get the content container of the Scroll Rect
        Transform contentContainer = scrollRect.content;

        // Destroy all child objects immediately
        int childCount = contentContainer.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contentContainer.GetChild(i).gameObject);
        }
    }
}