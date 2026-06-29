using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using Game.Store;
using UI.UIPanels.GashaponPanel;
using Es;

namespace SuperTreeView
{
    public class LotteryItem : MonoBehaviour
    {

        public GameObject mSelect;
        public Button mClickBtn;
        public Text mLabelText;
        public Text mUnLabelText;
        public GameObject mTag2;
        public Text mTag2Text;
        public GameObject mTag1;
        public Text mTag1Text;

        public GameObject mUnSelect;

        public Image icon;

        private GashaponViewConfig _gashaponData;
        private GameObject reddotObj;

        public GashaponViewConfig gashaponData => _gashaponData;

        void Start()
        {
            //mExpandBtn.onClick.AddListener(OnExpandBtnClicked);
            mClickBtn.onClick.AddListener(OnItemClicked);
        }

        public void Init()
        {
            IsSelected = false;
        }

        void OnExpandBtnClicked()
        {
            TreeViewItem item = GetComponent<TreeViewItem>();
            item.DoExpandOrCollapse();
        }

        public void CheckReddot()
        {
            GashaponDataManager.Inst.RequestGashaponInfo(_gashaponData.GashaId, (rsp) => {
                reddotObj?.SetActive(false);
                if (rsp.taskList == null || rsp.taskList.Count == 0)
                {
                    return;
                }

                foreach (var task in rsp.taskList)
                {
                    if (task.rewardStatus == 2)
                    {
                        reddotObj?.SetActive(true);
                        return;
                    }
                }

            });

        }

        public void SetItemInfo(LotteryConfig config)
        {
            Init();
            _gashaponData = GashaponDataManager.Inst.GetGashaponView(config.lotteryId.Trim());
         //   mIcon.sprite = ResManager.Instance.GetSpriteByName(iconSpriteName);
            mUnLabelText.text = mLabelText.text = _gashaponData.GashaName;
            string spriteatlasPath = "Assets/Loadable/UI/UIPanel/StorePanel/TreeView/GashaponTreeView.spriteatlas";
            string spriteName = string.IsNullOrEmpty(_gashaponData.CurrencyType) ? "youyou": _gashaponData.CurrencyType;
            Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, spriteName, gameObject);
            icon.sprite = sp1;
            icon.SetNativeSize();
            if(string.IsNullOrEmpty(config.tag))
            {
                mTag1.SetActive(false);
                mTag2.SetActive(false);
            }else if(config.tag.Length == 1)
            {
                mTag1.SetActive(true);
                mTag2.SetActive(false);
                mTag1Text.text = config.tag;
            }
            else 
            {
                mTag1.SetActive(false );
                mTag2.SetActive(true);
                mTag2Text.text = config.tag;
            }
            CheckReddot();
        }

        void OnItemClicked()
        {
            TreeViewItem item = GetComponent<TreeViewItem>();
            item.RaiseCustomEvent(CustomEvent.ItemClicked, null);
        }



        public bool IsSelected
        {
            get
            {
                return mSelect.activeSelf;
            }
            set
            {
                mSelect.SetActive(value);
                mUnSelect.SetActive(!value);
            }
        }



    }

}