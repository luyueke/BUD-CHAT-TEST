using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AnniversaryCalendarDailyItem : MonoBehaviour
{
    public Text title;
    public Text time;
    public GameObject activityObj;

    public void SetDaily(string text)
    {
        time.text = text;
    }
    public void SetWeekDay(string text)
    {
        title.text = text;
    }
    public void SetToday(bool isShow)
    {
        activityObj.SetActive(isShow);
    }
}
