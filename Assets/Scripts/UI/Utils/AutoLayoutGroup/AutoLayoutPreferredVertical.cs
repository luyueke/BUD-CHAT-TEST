using UnityEngine;
using System.Threading.Tasks;

public class AutoLayoutPreferredVertical : AutoLayoutContents
{
    public bool PreferredVertical = false;
    public float PaddingBottom = 0f;


    public override bool LayoutChildrenObjectsSync(GameObject parent = null) {
        if (parent == null)
        {
            parent = gameObject;
        }
        RectTransform parentRectTransform = parent.GetComponent<RectTransform>();
        Rows = 0;
        if (GetActiveChildCount(parent.transform) < 1)
        {
            if (PreferredVertical) SetParentPreferredDefHeight(parentRectTransform);
            return true;
        }
        RectTransform rectTransform = GetFirstActiveChild(parent.transform) as RectTransform;
        RectTransform prevRectTransform = rectTransform;
        bool isFirst = true;
        Rows = 1;
        Vector2 firstPos = new Vector2(-parentRectTransform.rect.width / 2f + rectTransform.rect.width / 2f + PaddingLeft,
            parentRectTransform.rect.height / 2f - rectTransform.rect.height / 2f - PaddingTop);
        UIPositionHelper.SetAbsoluteAnchoredCenterPosition(rectTransform, firstPos);

        // Start iterating through children
        Vector2 pos = UIPositionHelper.GetAbsoluteAnchoredCenterPosition(rectTransform);
        Vector2 offSet; // how much further from the previous object
        float minPosY = GetLocalBottomPosY(rectTransform); // min local posY of all children bottom
        foreach (Transform child in parent.transform)
        {
            if (!child.gameObject.activeSelf) continue;
            rectTransform = child.gameObject.GetComponent<RectTransform>();
            if (isFirst)
            {
                isFirst = false;
                continue;
            }
            offSet.x = prevRectTransform.rect.width / 2f + rectTransform.rect.width / 2f + SpacingHorizontal;
            offSet.y = 0f;

            // When overflowing to the right, go to next row
            if (pos.x + offSet.x + rectTransform.rect.width / 2f + PaddingRight > parentRectTransform.rect.width / 2f)
            {
                offSet.y = -prevRectTransform.rect.height - SpacingVertical;
                pos = pos + offSet;
                pos.x = -parentRectTransform.rect.width / 2f + rectTransform.rect.width / 2f + PaddingLeft;
                Rows++;
            }
            else
            {
                pos = pos + offSet;
            }
            UIPositionHelper.SetAbsoluteAnchoredCenterPosition(rectTransform, pos);
            prevRectTransform = rectTransform;
            // get local posY of every child bottom
            var posY = GetLocalBottomPosY(rectTransform);
            if (posY < minPosY) minPosY = posY;
        }
        if (PreferredVertical) SetParentPreferredHeight(parentRectTransform, minPosY);
        return true;
    }

    private void SetParentPreferredDefHeight(RectTransform parentRectTF)
    {
        var size = parentRectTF.sizeDelta;
        size.y = PaddingTop + PaddingBottom;
        parentRectTF.sizeDelta = size;
    }

    private void SetParentPreferredHeight(RectTransform parentRectTF, float childLocalBottomPosY)
    {
        var size = parentRectTF.sizeDelta;
        float height = parentRectTF.rect.height;
        float pivotY = 1 - parentRectTF.pivot.y;
        float distanceToTop = height * pivotY;
        size.y = distanceToTop - childLocalBottomPosY + PaddingBottom;
        parentRectTF.sizeDelta = size;
    }

    private float GetLocalBottomPosY(RectTransform rectTF)
    {
        float height = rectTF.rect.height;
        float pivotY = rectTF.pivot.y;
        float distanceToBottom = pivotY * height;
        return rectTF.localPosition.y - distanceToBottom;
    }

    #region only active children can be auto layout
    private int GetActiveChildCount(Transform parentTF)
    {
        int count = 0;
        foreach (Transform child in parentTF)
        {
            if (child.gameObject.activeSelf) count++;
        }
        return count;
    }

    private Transform GetFirstActiveChild(Transform parentTF)
    {
        foreach (Transform child in parentTF)
        {
            if (child.gameObject.activeSelf)
                return child;
        }
        return null;
    }
    #endregion
}
