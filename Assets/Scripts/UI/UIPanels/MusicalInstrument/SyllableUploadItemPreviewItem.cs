using Game.MusicalInstrument;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SyllableUploadItemPreviewItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image Img_Icon;
    public SyllableUploadItem UploadItem;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if(!Img_Icon.gameObject.activeInHierarchy)
            return;
        
        Img_Icon.color = DataUtil.DeSerializeColorCheckHash("#FF76D0");
        var url = UploadItem.GetCurUrl();
        if (!string.IsNullOrEmpty(url))
        {
            MusicalInstrumentManager.Inst.PlaySingleUgcSyllable(url, UploadItem.gameObject);
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if(!Img_Icon.gameObject.activeInHierarchy)
            return;
        
        Img_Icon.color = DataUtil.DeSerializeColorCheckHash("#A982FF");
    }
}
