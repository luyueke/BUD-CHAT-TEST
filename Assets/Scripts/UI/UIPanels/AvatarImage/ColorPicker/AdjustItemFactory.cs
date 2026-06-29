using UnityEngine;

public class AdjustItemFactory
{
    public AdjustItem Create(AdjustItemContext itemContext, GameObject gameObject)
    {
        AdjustItem item = new AdjustItem(itemContext);
        item.mGameObject = gameObject;
        item.Init();
        return item;
    }
}
