using System;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.AvatarImage;
using UnityEngine;

public class AvatarCategoryBar : MonoBehaviour
{
    private AvatarType CurrentAvatarType = AvatarType.Body;
    private List<AvatarCatagoryItem> topBarItems = new List<AvatarCatagoryItem>();

    private Transform topBarContent;
    private AvatarCatagoryItem topBarItem;

    /// <summary>
    /// 记录索引
    /// </summary>
    private Dictionary<string, int> indexDict = new Dictionary<string, int>();

    private Action<AvatarCategoryData> ClickAction;

    private void Awake()
    {
        InitUIIfNeed();
    }

    private void InitUIIfNeed()
    {
        if (topBarContent == null)
        {
            topBarContent = GameObjectEx.FindChildByName(transform, "Scroll View/Viewport/Content");
            topBarItem = GameObjectEx.FindChildByName(transform, "AvatarCatagoryItem").GetComponent<AvatarCatagoryItem>();

            var values = System.Enum.GetNames(typeof(AvatarType));
            for (int i = 0; i < values.Length; i++)
            {
                indexDict[values[i]] = 0;
            }
        }
    }

    public void SetAvatarData(AvatarType type, bool isNewUser = false, Action<AvatarCategoryData> clickAction = null)
    {
        InitUIIfNeed();
        CurrentAvatarType = type;
        ClickAction = clickAction;
        SetupUI(type, isNewUser);
    } 
    
    public void SetDefaultMenu(AvatarMenuType type)
    {
        AvatarCategoryData target = null;
        int index = 0;
        for (int i = 0; i < topBarItems.Count; i++)
        {
            var element = topBarItems[i];
            if (element.cData.CurrentMenuType == type)
            {
                index = i;
                target = element.cData;
                break;
            }
        }

        if (target == null)
        {
            return;
        }

        OnClickSecondMenu(index, target);
    }
    
    private void SetupUI(AvatarType type, bool isNewUser) {
        foreach (var avatarCatagoryItem in topBarItems)
        {
            GameObject.Destroy(avatarCatagoryItem.gameObject);
        }
        topBarItems.Clear();

        List<AvatarCategoryData> newItems;
        if (type == AvatarType.Body)
        {
            newItems = AvatarConfigTool.BodySubItems(isNewUser);
        }
        else
        {
            newItems = AvatarConfigTool.FaceSubItems(isNewUser);
        }

        var key = System.Enum.GetName(typeof(AvatarType), CurrentAvatarType);
        int defaultIndex = 0;
        if (!string.IsNullOrEmpty(key))
        {
            defaultIndex = indexDict[key];
        }
        
        for (int i = 0; i < newItems.Count; i++)
        {
            var data = newItems[i];
            var item = GameObject.Instantiate(topBarItem, topBarContent);
            item.gameObject.SetActive(true);
            item.SetData(data);
            topBarItems.Add(item);
            bool isSelected = defaultIndex == i;
            item.UpdateSelected(isSelected);
            
            var index = i;
            item.GetComponent<CButton>().onClick.AddListener(() => { OnClickSecondMenu(index, data); });

            if (isSelected)
            {
                OnClickSecondMenu(index, data);
            }
        }
    }

    private void OnClickSecondMenu(int index, AvatarCategoryData data)
    {
        LoggerUtils.Log($"OnClickSecondMenu {data}");
        for (int i = 0; i < topBarItems.Count; i++)
        {
            var item = topBarItems[i];
            item.UpdateSelected(index == i);
        }
        var key = System.Enum.GetName(typeof(AvatarType), CurrentAvatarType);
        if (!string.IsNullOrEmpty(key))
        {
            indexDict[key] = index;
        }
        ClickAction?.Invoke(data);
    }
}
