using System.Collections.Generic;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Audio;
using GameData;
using NetBusiness.Store;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class ContestAllPricesPanel : BasePanel<ContestAllPricesPanel>
{
    private RemoteImageBehaviour bgImage;
    private Transform content;
    private RawImage playerRawImage;
    private Image rewardRawImage;
    private Text rewardRawText;
    private Text previewName;
    private List<ContestAllPricesItem> items;
    private GameObject itemPrefab;
    private Button backBtn;
    private Image descBg;
    private Text descText;
    public override void OnCreate()
    {
        base.OnCreate();
        InitViews();
        //TODO:人物预览
        // Preview3DHelper.Inst.RefreshRole(playerRawImage);
    }
    private void InitViews()
    {
        descBg = GameObjectEx.FindChildByName(transform, "descBg").GetComponent<Image>();
        descText =  GameObjectEx.FindChildByName(transform, "desc").GetComponent<Text>();
        backBtn = GameObjectEx.FindChildByName(transform, "BackBtn").GetComponent<Button>();
        bgImage = GameObjectEx.FindChildByName(transform, "BG").GetComponent<RemoteImageBehaviour>();
        rewardRawImage = GameObjectEx.FindChildByName(transform, "RewardRawImage").GetComponent<Image>();
        rewardRawText = GameObjectEx.FindChildByName(rewardRawImage.transform, "Text").GetComponent<Text>();
        playerRawImage = GameObjectEx.FindChildByName(transform, "PlayerRawImage").GetComponent<RawImage>();
        previewName = GameObjectEx.FindChildByName(transform, "PreviewName").GetComponent<Text>();
        content = GameObjectEx.FindChildByName(transform, "MultiContent");
        itemPrefab =
            XAssetLoaderMgr.Inst.LoadResource<GameObject>("Assets/Loadable/Prefabs/UIPanel/ContestEventPanel/ContestAllPricesItem.prefab",gameObject);
        items = new List<ContestAllPricesItem>();
        backBtn.onClick.AddListener(OnCloseClick);
    }
    private bool IsPicPreviewType(ContestPrizeInfo info)
    {
        return info.currencyType != 0;
    }
    public void SetRewardInfo(ContestInfo info)
    {
        SetBgImage(info);
        if (info.prizes==null)
        {
            return;
        }
        for (int i = 0; i < info.prizes.Count; i++)
        {
            GameObject newIns = Instantiate(itemPrefab, content);
           
            ContestAllPricesItem itemScript = newIns.GetComponent<ContestAllPricesItem>();
            DataUtil.TryGetFromList(info.themeColorList, 0, out string themeColor1);
            itemScript.Init(info.prizes[i], themeColor1);
            itemScript.OnItemClick = ItemClick;
            items.Add(itemScript);
        }
        SetDescBg(info);
        TrySelectFirstItem();
    }
    private void SetBgImage(ContestInfo info)
    {
        if (!string.IsNullOrEmpty(info.background))
        {
            ContestEventManager.Inst.SetRawImage(bgImage, info.background);
        }
        else if (!string.IsNullOrEmpty(info.backgroundColor))
        {
            bgImage.RawImage.texture = null;
            bgImage.RawImage.color = DataUtil.DeSerializeColorCheckHash(info.backgroundColor);
            ContestEventManager.Inst.SetCustomBg(bgImage.transform, info.backgroundIconUrlList, info.backgroundColor);
        }
    }
    private void SetDescBg(ContestInfo info)
    {
        DataUtil.TryGetFromList(info.themeColorList, 1, out string themeColor2);
        descBg.color = DataUtil.DeSerializeColorCheckHash(themeColor2);
    }
    private void TrySelectFirstItem()
    {
        if (items.Count > 0)
        {
            if (!items[0].IsSelect())
            {
                ItemClick(items[0].GetBindInfo());
            }
        }
    }

    private void OnCloseClick()
    {
        CloseSelf();
    }

    private void ItemClick(ContestPrizeInfo info)
    {
        descText.SetText(info.desc);
        //选中态
        for (int i = 0; i < items.Count; i++)
        {
            if (info == items[i].GetBindInfo())
            {
                if (items[i].IsSelect())
                {
                    return;
                }
                else
                {
                    items[i].SwitchSelect();
                }
            }
            else
            {
                items[i].SetSelect(false);
            }
        }

        if (IsPicPreviewType(info))
        {
            SetRewardRawImageShow(true);
            PgcUtils.LoadCurrencyIconAsync((CurrencyType)info.currencyType, gameObject, (sprite) =>
            {
                if (this != null && gameObject != null && rewardRawImage!=null && sprite != null)
                {
                    rewardRawImage.sprite = sprite;
                }
            });
            if (info.num>1)
            {
                rewardRawText.gameObject.SetActive(true);
                rewardRawText.text = $"X{info.num}";
            }
            else
            {
                rewardRawText.gameObject.SetActive(false);
            }
        }
        else
        {
            SetRewardRawImageShow(false);
            //TODO:人物预览
            // Preview3DHelper.Inst.PreviewAsync(playerRawImage, new ProductData()
            // {
            //     pgcId = info.pgcId,
            //     classifyType = info.classifyType,
            //     type = info.productType
            // },null);
        }
        OnItemSelect(info);
    }

    private void SetRewardRawImageShow(bool isShow){
        playerRawImage.gameObject.SetActive(!isShow);
        rewardRawImage.gameObject.SetActive(isShow);
    }
    private void OnItemSelect(ContestPrizeInfo info)
    {
        previewName.SetText(info.name);
        for (int i = 0; i < items.Count; i++)
        {
            if (info == items[i].GetBindInfo())
            {
                continue;
            }

            items[i].SetSelect(false);
        }
    }
}
