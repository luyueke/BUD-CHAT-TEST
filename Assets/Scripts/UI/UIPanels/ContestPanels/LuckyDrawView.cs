using System.Collections;
using System.Collections.Generic;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class LuckyDrawView : MonoBehaviour
{
    [HideInInspector]
    public string CurContestID; //当前活动id
    public GameObject emptyTextGO;
    public GameObject mainViewGO;
    public ScrollRect scrollRect;
    public Transform contentParentTF;
    public GameObject luckyDrawItem;
    public Image bgImg;
    private readonly float maxHeight = 813; //view满屏高度临界值

    private void Awake()
    {
        ShowNameListView(false);
    }

    public void InitView(ContestInfo contestInfo)
    {
        CurContestID = contestInfo.contestId;
        var nameList = ContestDataManager.Inst.GetLuckyDrawCache(CurContestID);
        ShowContent(nameList);
        SetScrollViewControl();
        SetBgColor(contestInfo);
        
        ContestDataManager.Inst.GetLuckDraw(CurContestID, (b, data) =>
        {
            if (b && data.userNameList != null)
            {
                ShowContent(data.userNameList);
            }
        });
    }

    public void ShowContent(List<string> nameList)
    {
        if (nameList == null || nameList.Count == 0)
        {
            ShowNameListView(false);
        }
        else
        {
            ShowNameListView(true);
            for (int i = 0; i < contentParentTF.childCount; i++)
            {
                if (i >= nameList.Count)
                {
                    contentParentTF.GetChild(i).gameObject.SetActive(false);
                    continue;
                }
                RefreshItem(contentParentTF.GetChild(i), nameList[i]);
            }

            for (int i = contentParentTF.childCount; i < nameList.Count; i++)
            {
                CreatItem(nameList[i]);
            }
        }
    }

    public void SetScrollViewControl()
    {
        StartCoroutine(SetScrollViewControlCor());
    }

    private void ShowNameListView(bool isShow)
    {
        emptyTextGO.SetActive(!isShow);
        mainViewGO.SetActive(isShow);
    }

    private void RefreshItem(Transform item, string name)
    {
        var textEps = item.GetComponent<TextEllipsis>();
        SetText(textEps, name);
        item.gameObject.SetActive(true);
    }

    private void CreatItem(string name)
    {
        var itemObj = Object.Instantiate(luckyDrawItem, contentParentTF);
        var itemTextEps = itemObj.GetComponent<TextEllipsis>();
        SetText(itemTextEps, name);
    }

    private void SetText(TextEllipsis textEps, string userName)
    {
        string name = "";
        if (!string.IsNullOrEmpty(userName))
        {
            name = userName.StartsWith('@') ? userName.Substring(1) : userName;
        }
        textEps.SetText(name);
    }

    private IEnumerator SetScrollViewControlCor()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.vertical = scrollRect.content.rect.height > maxHeight;
    }

    private void SetBgColor(ContestInfo contestInfo)
    {
        DataUtil.TryGetFromList(contestInfo.themeColorList, 0, out string themeColor1);
        if (!string.IsNullOrEmpty(themeColor1))
        {
            bgImg.color = DataUtil.DeSerializeColorCheckHash(themeColor1);
        }
    }
}