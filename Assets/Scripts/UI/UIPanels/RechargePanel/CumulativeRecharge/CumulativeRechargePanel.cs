using System;
using System.Collections;
using System.Collections.Generic;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CumulativeRechargePanel : BasePanel<CumulativeRechargePanel>
{
    [Header("UI相关")]
    public Text Txt_ChargeCount;
    public Text Txt_ChargeCountHide;
    public CButton Btn_Close;
    public CButton Btn_ShowChargeCount;
    public CButton Btn_HideChargeCount;
    public CButton Btn_GoCharge;
    public CButton Btn_Tips;
    public CButton Btn_CloseTips;
    public GameObject Go_Tips;

    [Header("滑动条相关")] 
    public Transform ChargeItemContent;
    public CumulativeRechargeItem ItemPrefab;
    public Image Img_ChargeProgress;
    
    private RechargeBenifits _rechargeBenifits;
    private const int _boarder = 216;
    private const float _progressLen = 5052.0f;
    private const float _preItemLen = 217.5f;
    private List<CumulativeRechargeItem> _curItems = new List<CumulativeRechargeItem>();

    public override void OnCreate()
    {
        base.OnCreate();
        MessageHelper.AddListener(MessageName.RefreshCumData, RefreshData);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener(MessageName.RefreshCumData, RefreshData);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        InitUI();
        
        if (args != null && args.Length > 0)
        {
            _rechargeBenifits = (RechargeBenifits)args[0];
        }
        GetDataByHttp();
    }

    private void GetDataByHttp()
    {
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            _rechargeBenifits = res.rechargeBenefits;
            if (_rechargeBenifits == null)
            {
                LoggerUtils.LogError("CumulativeRechargePanel _rechargeBenifits is NUll");
                CloseSelf();
                return;
            }

            InitByData();
        });
    }

    private void InitByData()
    {
        // // 测试数据：补充8888挡位
        // if (this._rechargeBenifits.rechargeLevelList != null &&
        //     !this._rechargeBenifits.rechargeLevelList.Exists(d => d.rechargeNum == 8888))
        // {
        //     this._rechargeBenifits.rechargeLevelList.Add(new RechargeLevelData
        //     {
        //         id = 12,
        //         rechargeNum = 8888,
        //         rewardType = 5,
        //         itemName = "8888元累充奖励",
        //         pgcId = 0,
        //         isOptionalReward = 1,
        //         rewardStatus = 2,
        //         optionalPgcList = new List<int> { 190000027, 190000028 },
        //     });
        // }

        // // 测试数据：模拟已累充到8888
        // this._rechargeBenifits.totalRecharged = 8888;

        this.Txt_ChargeCount.text = this._rechargeBenifits.totalRecharged.ToString();
        for (int i = 0; i < this._rechargeBenifits.rechargeLevelList.Count; i++)
        {
            var data = this._rechargeBenifits.rechargeLevelList[i];
            var itemObj = GameObject.Instantiate(ItemPrefab, ChargeItemContent);
            var itemComp = itemObj.GetComponent<CumulativeRechargeItem>();
            itemObj.gameObject.SetActive(true);
            itemComp.InitData(data);
            itemComp.SetIcon(i);
            _curItems.Add(itemComp);
        }
        
        InitProgress(_rechargeBenifits.totalRecharged);
    }

    private void RefreshData()
    {
        AccountDataManager.Inst.BalanceInfo.Refresh();
        IAPDataManager.Inst.GetProductInfo(res =>
        {
           LoggerUtils.Log(JsonConvert.SerializeObject(this._rechargeBenifits.rechargeLevelList));
            _rechargeBenifits = res.rechargeBenefits;
            if (_rechargeBenifits != null)
            {
                for (int i = 0; i < this._rechargeBenifits.rechargeLevelList.Count; i++)
                {
                    var data = this._rechargeBenifits.rechargeLevelList[i];
                    _curItems[i]?.InitData(data);
                }
            }
        });
    }

    private int[] nodes = { 0, 6, 30, 68, 98, 188, 348, 648, 1288, 1888, 3688 , 5688 , 8888 , 8889}; // 节点数组

    /// <summary>
    /// 更新进度条显示
    /// </summary>
    /// <param name="currency">当前值</param>
    public void InitProgress(int cy)
    {
        int currency = cy;
        if (nodes == null || nodes.Length < 2)
        {
            Debug.LogError("节点数组至少需要两个值");
            return;
        }

        // 如果 currency 小于第一个节点
        if (currency <= nodes[0])
        {
            Img_ChargeProgress.fillAmount = 0f;
            return;
        }

        if (currency >= 8888)
        {
            currency = nodes[nodes.Length - 1];
        }

        // 如果 currency 大于等于最后一个节点
        if (currency >= nodes[nodes.Length - 1])
        {
            Img_ChargeProgress.fillAmount = 1f;
            return;
        }

        // 找到 currency 所属的段
        for (int i = 1; i < nodes.Length; i++)
        {
            if (currency <= nodes[i])
            {
                // 当前段的起点和终点
                int start = nodes[i - 1];
                int end = nodes[i];

                // 当前段占总进度条的比例
                float startFill = (float)(i - 1) / (nodes.Length - 1);
                float endFill = (float)i / (nodes.Length - 1);

                // 计算 currency 在当前段的相对进度
                float segmentProgress = (float)(currency - start) / (end - start);

                // 计算总进度条的 fillAmount
                Img_ChargeProgress.fillAmount = Mathf.Lerp(startFill, endFill, segmentProgress);
                return;
            }
        }
    }
    
    private void InitUI()
    {
        Txt_ChargeCount.gameObject.SetActive(false);
        Txt_ChargeCountHide.gameObject.SetActive(true);
        Btn_ShowChargeCount.gameObject.SetActive(true);
        Btn_HideChargeCount.gameObject.SetActive(false);
        
        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_ShowChargeCount.onClick.AddListener(() => { SetChargeCountEnable(true);});
        Btn_HideChargeCount.onClick.AddListener(() => { SetChargeCountEnable(false);});
        Btn_GoCharge.onClick.AddListener(OnBtnGoChargeClick);
        
        Btn_Tips.onClick.AddListener(() => { SetTipsEnable(true);});
        Btn_CloseTips.onClick.AddListener(() => { SetTipsEnable(false);});
    }

    private void SetChargeCountEnable(bool enable)
    {
        Btn_ShowChargeCount.gameObject.SetActive(!enable);
        Btn_HideChargeCount.gameObject.SetActive(enable);
        
        Txt_ChargeCount.gameObject.SetActive(enable);
        Txt_ChargeCountHide.gameObject.SetActive(!enable);
    }

    private void SetTipsEnable(bool enable)
    {
        Go_Tips.SetActive(enable);
    }

    private void OnBtnGoChargeClick()
    {
        CloseSelf();
    }
    
}
