using Com.TheFallenGames.OSA.Util.IO;
using Game.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using ChocDino.UIFX;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newbie;

namespace UI.UIPanels.FittingRoom
{
    public class OcItem : MonoBehaviour
    {
        [SerializeField] GameObject assetRoot;
        [SerializeField] GameObject buyRoot;
        [SerializeField] GameObject emptyRoot;
        [SerializeField] GameObject banRoot;
        [SerializeField] CButton btn_Item;
        [SerializeField] CButton btn_Delete;
        [SerializeField] Text buyText;
        [SerializeField] Toggle operationToggle;
        [SerializeField] Image selectedImage;
        [SerializeField] Image assetsIcon;
        [SerializeField] RemoteImageBehaviour remoteAssetsIcon;
        [SerializeField] BlurFilter iconBlurFilter;

        [SerializeField] Image emptyIcon;
        [SerializeField] Sprite characterOcSprite;
        [SerializeField] Sprite petOcSprite;
        [SerializeField] private GameObject timeNode;
        [SerializeField] private Text timeText;
        private Action<OcServerData, bool> onItemSelected;
        private Action<OcServerData> onItemDelete;
        private OcServerData mData;

        static int OCcnt = 0; 
        public RemoteImageBehaviour RemoteImage => remoteAssetsIcon;

        /// <summary>
        /// 重置OC计数器，用于重新初始化时重置引导状态
        /// </summary>
        /// <param name="forceReset">是否强制重置，默认为true</param>
        public static void ResetOcCounter(bool forceReset = true)
        {
            if (forceReset)
            {
                OCcnt = 0;
            }
        }

        private void Awake()
        {
            btn_Item.onClick.AddListener(OnItemClick);
            btn_Delete.onClick.AddListener(OnDeleteItemClick);
            operationToggle.onValueChanged.AddListener(OnItemSelected);
        }

        private void OnItemClick()
        {
            onItemSelected?.Invoke(mData, true);
        }

        private void OnDeleteItemClick()
        {
            onItemDelete?.Invoke(mData);
        }

        private void OnItemSelected(bool isOn)
        {
            onItemSelected?.Invoke(mData, isOn);
        }

        private void ResetAllUI()
        {
            iconBlurFilter.Blur = 0;
            assetRoot.SetActive(false);
            buyRoot.SetActive(false);
            emptyRoot.SetActive(false);
            banRoot.SetActive(false);
            btn_Item.SetClickAble(true);
            operationToggle.gameObject.SetActive(false);
        }

        public void UpdateViews(OcServerData info, Action<OcServerData, bool> action, Action<OcServerData> deleteAct)
        {
            if (!info.add)
            {
                OCcnt++;
            }
            if (OCcnt == 2)
            {
                if (transform.TryGetComponent<BootMaskMono>(out var comp))
                {
                    comp.id.Add(103);
                }
                else
                {
                    var compo = gameObject.AddComponent<BootMaskMono>();
                    if (!compo.id.Exists(x => x == 103))
                    {
                        compo.id.Add(103);
                    }

                }
                string BagTagkey = "FirstOpenFittingRoomPanel_Bag" + AccountDataManager.Inst.UserInfo.uid;
                var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
                if (!PlayerPrefs.HasKey(BagTagkey) && !BootPanel.isPlaying && AccountDataManager.Inst.UserInfo.isNewUser == 1 && panel.GetisCharacterFittingRoom())
                {
                    TimerManager.Inst.RunOnce("Boot", 0.2f, () => //延迟防止没创建出TAG
                    {
                        panel.JumpTo(MainTabs.Tab.Bag, 1);
                    });
                    PlayerPrefs.SetInt(BagTagkey, 1);
                    PlayerPrefs.Save();
                    TimerManager.Inst.RunOnce("Boot", 0.3f, () =>
                    {
                        UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.FittingRoomWindow,103);
                    });
                }
            }
            ResetAllUI();
            mData = info;
            onItemSelected = action;
            onItemDelete = deleteAct;
            if (info == null||string.IsNullOrEmpty(info.leftTime))
            {
                timeNode.SetActive(false);
            }
            else
            {
                timeNode.SetActive(true);
                timeText.text = info.leftTime;
            }
            
            emptyIcon.sprite = info?.skinType == 1 ? petOcSprite : characterOcSprite;

            

            //展示空槽
            if (info?.isSlot == 1)
            {
                emptyRoot.SetActive(true);
                return;
            }

            



            if (info.add)
            {
                buyRoot.SetActive(true);
                buyText.text = $"{info.cur}/{info.total}";
                return;
            }

            assetRoot.SetActive(true);
            assetsIcon.gameObject.SetActive(true);
            assetsIcon.sprite = info?.skinType == 1 ? petOcSprite : characterOcSprite;
            remoteAssetsIcon.gameObject.SetActive(false);
            remoteAssetsIcon.Load(info.ocInfo.ocCover, onCompleted: (bool fromCache, bool success) =>
            {
                if (gameObject == null) return;
                assetsIcon.gameObject.SetActive(false);
                remoteAssetsIcon.gameObject.SetActive(true);
            });

            if (info?.ocInfo?.isBan == 1)
            {
                iconBlurFilter.Blur = 10;
                btn_Item.SetClickAble(false);
                banRoot.SetActive(true);
                selectedImage.gameObject.SetActive(false);
                operationToggle.gameObject.SetActive(false);
                return;
            }

            if (info.operation)
            {
                selectedImage.gameObject.SetActive(false);
                operationToggle.gameObject.SetActive(info.operation);
                operationToggle.SetIsOnWithoutNotify(info.selected);
                return;
            }
            else
            {
                selectedImage.gameObject.SetActive(info.selected);
            }
        }
    }

    public class OcInfo
    {
        public string ocId;
        public string avatarJson;

        public Int64 createTime;
        public string ocCover;
        public int isBan;
        public int skinType;
    }
}
