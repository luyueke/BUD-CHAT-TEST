using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnniversaryCalendarView : MonoBehaviour
{
    public AnniversaryCalendarActivityItem activityItem;
    public Transform activityContent;

    public List<AnniversaryCalendarDailyItem> items;
    public int startY = -67;
    public int delayY = 117;
    public Text lastTime;
    
    // 活动固定时间范围
    private static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 7, 25);
    private static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 9, 1);

    DateTime testday;// = new DateTime(2025, 8, 8);

    public List<Es.AnniversaryCalendarView> configs { get; private set; }
    public List<AnniversaryCalendarActivityItem> activityItems = new List<AnniversaryCalendarActivityItem>();
    private void Start()
    {
        configs = Es.DataTables.GetAnniversaryCalendarViewList();
        RefreshCalendarDisplay();
        
    }

    private void UpdateDailyItems()
    {
        testday = DateTime.Now;
        //var startDay = new DateTime(2025, 8, 25);
        //if(testday < startDay)
        //{
        //    testday = startDay;
        //}
        testday = new DateTime(2025, 8, 25);
        // 获取当前周区间
        var weekRange = GetCurrentWeekRange(testday);
        if (weekRange == null)
        {
            Debug.LogWarning("当前不在活动时间范围内");
            return;
        }

        // 计算这个区间内的所有日期
        List<DateTime> datesInRange = new List<DateTime>();
        DateTime currentDate = weekRange.StartDate;
        while (currentDate <= weekRange.EndDate)
        {
            datesInRange.Add(currentDate);
            currentDate = currentDate.AddDays(1);
        }

        // 确保items列表长度正确（通常应该是7个）
        if (items == null || items.Count < datesInRange.Count)
        {
            Debug.LogError($"日期项数量不足：需要{datesInRange.Count}个，实际只有{items?.Count ?? 0}个");
            return;
        }

        // 获取当前日期（用于比较）
        DateTime targetDate = DateTime.Now; // 使用与GetCurrentWeekRange相同的日期

        // 遍历设置每一天的显示
        for (int i = 0; i < datesInRange.Count; i++)
        {
            if (items[i] != null)
            {
                DateTime itemDate = datesInRange[i];

                // 格式化日期文本：X月X日
                string dateText = $"{itemDate.Month}月{itemDate.Day}日";
                items[i].SetDaily(dateText);

                // 设置是否是当天
                bool isToday = itemDate.Year == targetDate.Year &&
                              itemDate.Month == targetDate.Month &&
                              itemDate.Day == targetDate.Day;
                items[i].SetToday(isToday);

                items[i].gameObject.SetActive(true); // 确保物体是激活的
            }
        }

        // 如果一周不满7天，将剩余的item隐藏或设置为空
        for (int i = datesInRange.Count; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].gameObject.SetActive(false);
            }
        }
    }
    private void UpdateLastTime()
    {
        if (lastTime == null) return;

        // 获取当前时间
        DateTime now = DateTime.Now;
        
        // 如果已经超过结束时间
        if (now >= ACTIVITY_END_TIME)
        {
            lastTime.text = "活动已结束";
            return;
        }

        // 如果还没到开始时间
        if (now < ACTIVITY_START_TIME)
        {
            lastTime.text = "活动未开始";
            return;
        }
        
        // 计算剩余时间
        TimeSpan remainingTime = ACTIVITY_END_TIME - now;
        
        // 格式化显示
        string timeText = string.Format("{0}天{1}小时",
            remainingTime.Days,
            remainingTime.Hours);

        lastTime.text = timeText;
    }
    private void UpdateActivityItems()
    {
        // 清理旧的活动项
        foreach (var item in activityItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        activityItems.Clear();

        var weekRange = GetCurrentWeekRange(testday);
        if (weekRange == null) return;

        // 强制更新布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)activityContent);
        // 创建日期到位置的映射
        Dictionary<DateTime, float> dateToXPosition = new Dictionary<DateTime, float>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].gameObject.activeSelf)
            {
                RectTransform itemRect = items[i].GetComponent<RectTransform>();
                float xPos = itemRect.anchoredPosition.x;
                DateTime itemDate = weekRange.StartDate.AddDays(i);
                dateToXPosition[itemDate] = xPos;
            }
        }

        int currentRow = 0;  // 当前行号
        int remainingDays = 7;  // 当前行剩余天数
        DateTime currentRowStart = weekRange.StartDate;  // 当前行可用的开始日期
        int j = 0;
        foreach (var config in configs)
        {
            DateTime activityStart = DateTime.Parse(config.StartTime);
            DateTime activityEnd = DateTime.Parse(config.EntTime);

            if (activityStart <= weekRange.EndDate && activityEnd >= weekRange.StartDate)
            {
                j++;
                DateTime displayStart = activityStart > weekRange.StartDate ? activityStart : weekRange.StartDate;
                DateTime displayEnd = activityEnd < weekRange.EndDate ? activityEnd : weekRange.EndDate;

                int displayDays = (int)(displayEnd - displayStart).TotalDays + 1;

                bool needNewRow = false;
                if (displayStart < currentRowStart)
                {
                    needNewRow = true;
                }
                else if (displayDays > remainingDays)
                {
                    needNewRow = true;
                }

                if (needNewRow)
                {
                    currentRow++;
                    remainingDays = 7;
                    currentRowStart = weekRange.StartDate;
                }

                var newItem = Instantiate(activityItem, activityContent);

                RectTransform rectTransform = newItem.GetComponent<RectTransform>();

                float xPos = 0;
                if (dateToXPosition.TryGetValue(displayStart, out float startX))
                {
                    xPos = startX;
                }
                float yPos = startY + (currentRow * -delayY);
                //if (j == 2) xPos += 70;
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                int startOffset = (int)(displayStart - weekRange.StartDate).TotalDays;

                // 判断是否从本周第一天开始
                bool isStartFromWeekBegin = displayStart == weekRange.StartDate;

                // 设置数据，增加新参数
                newItem.SetData(
                    config.iconName,
                    displayDays,
                    config.title,
                    startOffset,
                    isStartFromWeekBegin,
                    (testday.Date >= DateTime.Parse(config.StartTime).Date && testday.Date <= DateTime.Parse(config.EntTime).Date) ? config.SkipId : -1
                );

                activityItems.Add(newItem);

                remainingDays -= displayDays;
                currentRowStart = displayEnd.AddDays(1);
            }
        }
    }
    private string GetChineseWeekDay(DayOfWeek dayOfWeek)
    {
        string[] weekDays = { "日", "一", "二", "三", "四", "五", "六" };
        return $"星期{weekDays[(int)dayOfWeek]}";
    }

    private void UpdateWeekDayDisplay()
    {
        var weekRange = GetCurrentWeekRange(testday);
        if (weekRange == null) return;

        DateTime currentDate = weekRange.StartDate;
        int index = 0;

        while (currentDate <= weekRange.EndDate && index < items.Count)
        {
            if (items[index] != null)
            {
                // 假设AnniversaryCalendarDailyItem有设置星期几的方法
                items[index].SetWeekDay(GetChineseWeekDay(currentDate.DayOfWeek));
            }
            currentDate = currentDate.AddDays(1);
            index++;
        }
    }

    public void RefreshCalendarDisplay()
    {
        UpdateDailyItems();
        UpdateWeekDayDisplay(); // 如果需要显示星期几
        UpdateLastTime();
        UpdateActivityItems();
    }

    public class WeekRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WeekIndex { get; set; }  // 第几周 (从0开始)

        public override string ToString()
        {
            return $"{StartDate:MM.dd}-{EndDate:MM.dd}";
        }
    }

    public static WeekRange GetCurrentWeekRange(DateTime? currentDate = null)
    {
        DateTime date = currentDate ?? DateTime.Now;

        //// 如果不在活动时间范围内，返回null
        //if (date < ACTIVITY_START_TIME || date > ACTIVITY_END_TIME)
        //{
        //    return null;
        //}

        //// 计算当前是第几周
        //int daysSinceStart = (date - ACTIVITY_START_TIME).Days;
        //int weekIndex = daysSinceStart / 7;  // 整除得到周数

        // 计算本周的开始和结束日期
        //DateTime weekStart = ACTIVITY_START_TIME.AddDays(weekIndex * 7);
        DateTime weekStart = date;
        DateTime weekEnd = weekStart.AddDays(6);  // 固定显示7天

        // 如果结束日期超过活动结束时间，则使用活动结束时间
        if (weekEnd > ACTIVITY_END_TIME)
        {
            weekEnd = ACTIVITY_END_TIME;
        }

        return new WeekRange
        {
            StartDate = weekStart,
            EndDate = weekEnd,
           // WeekIndex = weekIndex
        };
    }

    //public static int GetTotalWeeks()
    //{
    //    return GetCurrentWeekRange(ACTIVITY_END_TIME).WeekIndex + 1;
    //}

    // 用于测试的方法
    private void TestWeekRanges()
    {
        DateTime[] testDates = new[]
        {
            new DateTime(2025, 7, 27),  // 应该显示 7.25-7.31
            new DateTime(2025, 8, 5),   // 应该显示 8.1-8.7
            new DateTime(2025, 8, 15),  // 应该显示 8.15-8.21
            new DateTime(2025, 8, 30),  // 应该显示 8.29-8.31
        };

        foreach (var date in testDates)
        {
            var range = GetCurrentWeekRange(date);
            Debug.Log($"测试日期 {date:yyyy-MM-dd}: {range}");
        }
    }
}