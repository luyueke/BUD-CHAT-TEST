using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using UnityEngine;

public class ContestBgItem : MonoBehaviour
{
    public RemoteImageBehaviour bgRawImage;
    public Transform customBgRoot;
    public ItemBgColor bgColorItem;

    public void Refresh(ContestInfo info)
    {
        if (!string.IsNullOrEmpty(info.background))
        {
            bgColorItem.gameObject.SetActive(false);
            customBgRoot.gameObject.SetActive(false);
            if (bgRawImage) bgRawImage.gameObject.SetActive(true);
            ContestEventManager.Inst.SetRawImage(bgRawImage, info.background);
        }
        else if (!string.IsNullOrEmpty(info.backgroundColor))
        {
            bgColorItem.gameObject.SetActive(true);
            customBgRoot.gameObject.SetActive(true);
            bgColorItem.SetColor(info.backgroundColor);
            customBgRoot.ClearChildren();
            ContestEventManager.Inst.SetCustomBg(customBgRoot, info.backgroundIconUrlList, info.backgroundColor);
        }        
    }
}
