using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassTipsView : MonoBehaviour
{
    [SerializeField] private Text levelText;
    [SerializeField] private Text descText;
    [SerializeField] private Button CloseBtn;

    private void Awake()
    {
        CloseBtn?.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
        });
    }

    public void SetData(int hasNum, int currentMin)
    {
        levelText.SetLocalText("当前拥有：{0}", hasNum);
        //descText.SetLocalText("继续游玩{0}分钟就可以解锁下一等级哦！\n注意单日最高计算游玩时长是60分钟。", currentMin);
    }
}
