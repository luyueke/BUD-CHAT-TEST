using Com.TheFallenGames.OSA.Util.IO;
using Game.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using ChocDino.UIFX;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace BUD.AnimPose
{
    public class QuickPoseItem : MonoBehaviour
    {
        [SerializeField] GameObject assetRoot;
        [SerializeField] GameObject buyRoot;
        [SerializeField] GameObject emptyRoot;
        [SerializeField] Image emptyImage;
        [SerializeField] GameObject banRoot;
        [SerializeField] CButton btn_Item;
        [SerializeField] CButton btn_Delete;
        [SerializeField] Text buyText;
        [SerializeField] Toggle operationToggle;
        [SerializeField] Image selectedImage;
        [SerializeField] RemoteImageBehaviour remoteAssetsIcon;
        [SerializeField] Sprite[] iconSprites;
        private Action<PoseOcServerData, bool> onItemSelected;
        private Action<PoseOcServerData> onItemDelete;
        private PoseOcServerData mData;

        public RemoteImageBehaviour RemoteImage => remoteAssetsIcon;

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
            assetRoot.SetActive(false);
            buyRoot.SetActive(false);
            emptyRoot.SetActive(false);
            banRoot.SetActive(false);
            btn_Item.SetClickAble(true);
            operationToggle.gameObject.SetActive(false);
        }

        public void UpdateViews(PoseOcServerData info, Action<PoseOcServerData, bool> action, Action<PoseOcServerData> deleteAct)
        {
            mData = info;
            onItemSelected = action;
            onItemDelete = deleteAct;
            UgcPoseSubType curPoseType = (UgcPoseSubType)info.poseType;
            switch (curPoseType)
            {
                case UgcPoseSubType.Single:
                    emptyImage.sprite = iconSprites[0];
                    break;
                case UgcPoseSubType.Double:
                    emptyImage.sprite = iconSprites[1];
                    break;
                case UgcPoseSubType.PetSingle:
                    emptyImage.sprite = iconSprites[2];
                    break;
                case UgcPoseSubType.PetWithPlayer:
                    emptyImage.sprite = iconSprites[3];
                    break;
            }
            emptyImage.SetNativeSize();
            ResetAllUI();
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
            remoteAssetsIcon.gameObject.SetActive(false);
            if (info.poseInfo != null)
            {
                remoteAssetsIcon.Load(info.poseInfo.cover, onCompleted: (bool fromCache, bool success) =>
                {
                    if (gameObject == null) return;
                    remoteAssetsIcon.gameObject.SetActive(true);
                });
            }

            if (info?.poseInfo?.isBan == 1)
            {
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
}
