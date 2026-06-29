using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class S5ChristmasCoinWinnerView : MonoBehaviour
{
    public CButton Btn_Close;
    public Text Txt_BigPrize;
    public RewardPrizeUserItem ItemPrefab;
    public Transform FirstPrizeContent;
    public Transform SecondPrizeContent;
    public GameObject Go_Unable;

    private ChristmasCoinRebatesInfo _curInfo;

    private void Awake()
    {
        Btn_Close.onClick.AddListener(() => { gameObject.SetActive(false);});
    }

    public void InitData(ChristmasCoinRebatesInfo info)
    {
        this._curInfo = info;
        if (this._curInfo.isPrizeDraw == 0)
        {
            Go_Unable.SetActive(true);
            return;
        }
        else
        {
            Go_Unable.SetActive(false);
        }
        
        Btn_Close.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
        });
        Txt_BigPrize.text = "锦鲤大奖：" + this._curInfo.koiPrizeList[0];
        InitFirstPrizeContent(this._curInfo.firstPrizeList);
        InitSecondPrizeContent(this._curInfo.secondPrizeList);
    }

    private void InitFirstPrizeContent(List<string> nameList)
    {
        FirstPrizeContent.ClearChildren();
        for (int i = 0; i < nameList.Count; i++)
        {
            var itemComp = GameObject.Instantiate(ItemPrefab, FirstPrizeContent);
            itemComp.SetData(i, nameList[i]);
        } 
    }

    private void InitSecondPrizeContent(List<string> nameList)
    {
        SecondPrizeContent.ClearChildren();
        for (int i = 0; i < nameList.Count; i++)
        {
            var itemComp = GameObject.Instantiate(ItemPrefab, SecondPrizeContent);
            itemComp.SetData(i, nameList[i]);
        } 
    }
}
