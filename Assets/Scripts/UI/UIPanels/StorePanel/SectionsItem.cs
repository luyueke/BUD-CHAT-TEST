using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using UI.UIPanels.GashaponPanel;

namespace SuperTreeView
{
    public class SectionsItem : MonoBehaviour
    {
        //public Button mExpandBtn;
        public Image mIcon;
        public GameObject mSelect;
        public Button mClickBtn;
        public Text mLabelText;
        public Text mUnLabelText;

        public SectionsConfig config;
        public GameObject mUnSelect;



        void Start()
        {
            //mExpandBtn.onClick.AddListener(OnExpandBtnClicked);
            mClickBtn.onClick.AddListener(OnItemClicked);
        }

        public void Init()
        {
            //SetExpandBtnVisible(false);
            //SetExpandStatus(true);
            IsSelected = false;
        }

        //void OnExpandBtnClicked()
        //{
        //    TreeViewItem item = GetComponent<TreeViewItem>();
        //    item.DoExpandOrCollapse();
        //}


        public List<LotteryItem> SetItemInfo(TreeViewItem item ,SectionsConfig config)
        {
            List<LotteryItem> list = new List<LotteryItem>();
            Init();
            this.config = config;
            mUnLabelText.text = mLabelText.text = config.name;
            bool isAdd = false;
            bool isAddSkateboard = false;
            bool isAddViolet = false;
            bool isAddSweet = false;
            bool isAddSock = false;
            bool isAddWhiteDew = false;
            bool isAddBabyShrimp = false;
            for (int i=0;i<config.list.Count;i++)
            {
                //这里三个扭蛋合一，特殊处理一下
                if (config.list[i].lotteryId == "lottery.musicalPudding.dreamingGhost" || config.list[i].lotteryId == "lottery.musicalPudding.kumoGhost" || config.list[i].lotteryId == "lottery.musicalPudding.kuroGhost" || config.list[i].lotteryId == "lottery.musicalNoteSuit")
                {
                    if (isAdd)
                    {
                        continue;
                    }
                    config.list[i].lotteryId = "lottery.musicalNoteSuit";
                    isAdd = true;
                }
                if(config.list[i].lotteryId == "lottery.musicalPudding.prank" || config.list[i].lotteryId == "lottery.musicalPudding.umi" || config.list[i].lotteryId == "lottery.musicalPudding.didu" || config.list[i].lotteryId == "lottery.musicalPuddingSuit")
                {
                    if(isAddSkateboard)
                    {
                        continue;
                    }
                    config.list[i].lotteryId = "lottery.musicalPuddingSuit";
                    isAddSkateboard = true;
                }

                if(config.list[i].lotteryId == "lottery.nightButterflyDream" || config.list[i].lotteryId == "lottery.violetFragrance" || config.list[i].lotteryId == "lottery.VioletGashapon")
                {
                    if(isAddViolet)
                    {
                        continue;
                    }
                    isAddViolet = true;
                    config.list[i].lotteryId = "lottery.VioletGashapon";
                }

                if (config.list[i].lotteryId == "lottery.polkaDotBerry" || config.list[i].lotteryId == "lottery.seaSaltRabbit" || config.list[i].lotteryId == "lottery.PolkaDotBerry")
                {
                    if (isAddSweet)
                    {
                        continue;
                    }
                    isAddSweet = true;
                    config.list[i].lotteryId = "lottery.PolkaDotBerry";
                }

                if (config.list[i].lotteryId == "lottery.wawaKindergarten.pinkTail" || config.list[i].lotteryId == "lottery.wawaKindergarten.warmOrange" || config.list[i].lotteryId == "lottery.wawaKindergartenSuit")
                {
                    if (isAddSock)
                    {
                        continue;
                    }
                    isAddSock = true;
                    config.list[i].lotteryId = "lottery.wawaKindergartenSuit";
                }

                if(config.list[i].lotteryId == "lottery.whiteDewDrowningStar" || config.list[i].lotteryId == "lottery.orangeRainBubbles" )
                {
                    if(isAddWhiteDew)
                    {
                        continue;
                    }
                    isAddWhiteDew = true;
                    config.list[i].lotteryId = "lottery.whiteDewDrowningStar";
                }

                if (config.list[i].lotteryId == "lottery.babyShrimp.huhu" || config.list[i].lotteryId == "lottery.babyShrimp.wuwu" || config.list[i].lotteryId == "lottery.babyShrimp.rabbit" || config.list[i].lotteryId == "lottery.babyShrimpSuit")
                {
                    if (isAddBabyShrimp)
                    {
                        continue;
                    }
                    isAddBabyShrimp = true;
                    config.list[i].lotteryId = "lottery.babyShrimpSuit";
                }

                var  _gashaponData = GashaponDataManager.Inst.GetGashaponView(config.list[i].lotteryId.Trim());
                if (_gashaponData == null)
                {
                    Debug.LogError($"配置错误：lotteryId ={config.list[i].lotteryId}");
                    continue;
                }
                bool isLive = BusinessLiveManager.Inst.IsGashaponLive((int)_gashaponData.ViewId);
                if (!isLive && _gashaponData.ViewId == (int)GashaponType.BabyShrimpSuit)
                {
                    isLive = BusinessLiveManager.Inst.IsGashaponLive("lottery.babyShrimp.huhu")
                          || BusinessLiveManager.Inst.IsGashaponLive("lottery.babyShrimp.wuwu")
                          || BusinessLiveManager.Inst.IsGashaponLive("lottery.babyShrimp.rabbit");
                }
                if (isLive || _gashaponData.ViewId == 61 || _gashaponData.ViewId == 62 || _gashaponData.ViewId == 73)
                {
                    TreeViewItem childItem = item.ChildTree.AppendItem("ItemPrefab2");
                    var itemScript = childItem.GetComponent<LotteryItem>();
                    itemScript.SetItemInfo(config.list[i]);
                    list.Add(itemScript);
                }         
            }
            return list;
        }

        void OnItemClicked()
        {
            TreeViewItem item = GetComponent<TreeViewItem>();
            //item.RaiseCustomEvent(CustomEvent.MenuClicked, null);
            Debug.Log("TreeViewItem Clicked " + config.name);
            IsSelected = !item.IsExpand;
            //TreeViewItem item = GetComponent<TreeViewItem>();
            item.DoExpandOrCollapse();

        }

        public void CheckIsExpand()
        {
            TreeViewItem item = GetComponent<TreeViewItem>();
            if (item.IsExpand)
            {
                item.DoExpandOrCollapse();
            }
        }
        //public void SetExpandBtnVisible(bool visible)
        //{
        //    if (visible)
        //    {
        //        mExpandBtn.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        mExpandBtn.gameObject.SetActive(false);
        //    }
        //}

        public bool IsSelected
        {
            get
            {
                return mSelect.activeSelf;
            }
            set
            {
                mUnSelect.SetActive(!value);
                mSelect.SetActive(value);
            }
        }
        //public void SetExpandStatus(bool expand)
        //{
        //    if (expand)
        //    {
        //        mExpandBtn.transform.localEulerAngles = new Vector3(0, 0, -90);
        //    }
        //    else
        //    {
        //        mExpandBtn.transform.localEulerAngles = new Vector3(0, 0, 0);

        //    }
        //}


    }

}