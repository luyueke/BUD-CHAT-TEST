using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class ContestCreateOcItemView : MonoBehaviour
{
    [SerializeField] private GameObject AddObj;
    [SerializeField] private GameObject SelectObj;
    [SerializeField] private RawImage coverObj;
    [SerializeField] private CButton ActionBtn;
    
    private AvatarOcFixData cData;
    private Action<bool, AvatarOcFixData> cSelectAction;
    public void SetData(AvatarOcFixData data, Action<bool, AvatarOcFixData> selectAction)
    {
        cData = data;
        cSelectAction = selectAction;
        
        ActionBtn.onClick.RemoveAllListeners();
        ActionBtn.onClick.AddListener(OnClickItemView);

        bool isAdd = isAddItem();
        
        AddObj.SetActive(isAdd);
        coverObj.gameObject.SetActive(!isAdd);
        SetSelected(data.isSelect);
    }
    
    private void SetSelected(bool isSelect)
    {
        if (isAddItem())
        {
            return;
        }
        SelectObj.SetActive(isSelect);
    }

    private bool isAddItem()
    {
        var ocId = cData?.ocInfo?.ocId ?? "";
        if (ocId == ContestCreateOcRequester.gAddOcKey)
        {
            return true;
        }
        return false;
    }
    
    private void OnClickItemView()
    {
        if (cData == null)
        {
            return;
        }

        cSelectAction?.Invoke(isAddItem(), cData);
    }
    
}
