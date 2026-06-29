using System;
using System.Collections.Generic;
using Game.Audio;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.IncubationCabin;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BoxRewardItemData
{
    public enum ItemType { Common, Character, Skin }
    public ItemType itemType;
    public CommonRewardItemData commonData;
    public CabinCharacterBaseInfo characterData;
    public TagType tagType = TagType.none;
    public specialIconType specialIcon = specialIconType.none;
}

public class CommonBoxRewardPanel : BasePanel<CommonBoxRewardPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private Transform RewardRoot;
    [SerializeField] private CommonBoxRewardItem RewardItemPrefab;
    [SerializeField] private CabinCharacterCardItem CabinCharacterCardItem;
    [SerializeField] private CabinSkinCardItem CabinSkinCardItem;
    [SerializeField] private GameObject AddItem;
    [SerializeField] private GameObject VertAnimation;
    [SerializeField] private GameObject HorAnimation;

    [Header("角标")]
    [SerializeField] private Sprite TagZengsongSprite;

    [Header("Special Icons")]
    [SerializeField] private Sprite IconAICreditSprite;

    private ContentSizeFitter _rewardRootFitter;
    private Action _onClose;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        AkSoundManager.Inst.PlayUIEffectSound("UI_Award_Newbie");
        _rewardRootFitter = RewardRoot.GetComponent<ContentSizeFitter>();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        _onClose?.Invoke();
        _onClose = null;
    }

    public void ShowReward(List<BoxRewardItemData> rewards, Action onClose = null)
    {
        _onClose = onClose;

        bool isPortrait = Screen.orientation == ScreenOrientation.Portrait
                       || Screen.orientation == ScreenOrientation.PortraitUpsideDown;
        VertAnimation.SetActive(isPortrait);
        HorAnimation.SetActive(!isPortrait);

        if (rewards == null || rewards.Count == 0) return;

        var specialItems = new List<BoxRewardItemData>();
        var commonItems = new List<BoxRewardItemData>();
        foreach (var r in rewards)
        {
            if (r.itemType == BoxRewardItemData.ItemType.Common) commonItems.Add(r);
            else specialItems.Add(r);
        }

        // 当包含角色时，自动追加 AICredit 奖励项，数量 30000
        bool hasCharacter = false;
        foreach (var r in specialItems)
        {
            if (r.itemType == BoxRewardItemData.ItemType.Character) { hasCharacter = true; break; }
        }
        if (hasCharacter)
        {
            commonItems.Add(new BoxRewardItemData
            {
                itemType    = BoxRewardItemData.ItemType.Common,
                specialIcon = specialIconType.aiCredit,
                tagType = TagType.zengsong,
                commonData  = new CommonRewardItemData { RewardAmount = 30000, rewardName = "AI能量" },
            });
        }

        bool hasMixed = specialItems.Count > 0 && commonItems.Count > 0;

        if (hasMixed)
        {
            foreach (var r in specialItems) InstantiateSpecial(r);
            Instantiate(AddItem, RewardRoot).SetActive(true);
            foreach (var r in commonItems) InstantiateCommon(r);
        }
        else
        {
            foreach (var r in rewards)
            {
                if (r.itemType == BoxRewardItemData.ItemType.Common)
                    InstantiateCommon(r);
                else
                    InstantiateSpecial(r);
            }
        }

        if (_rewardRootFitter != null) _rewardRootFitter.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)RewardRoot);
    }

    private void InstantiateCommon(BoxRewardItemData r)
    {
        var item = Instantiate(RewardItemPrefab, RewardRoot);
        item.gameObject.SetActive(true);
        item.Init(r.commonData, GetTagSprite(r.tagType), GetSpecialIconSprite(r.specialIcon));
    }

    private void InstantiateSpecial(BoxRewardItemData r)
    {
        if (r.itemType == BoxRewardItemData.ItemType.Character)
        {
            var item = Instantiate(CabinCharacterCardItem, RewardRoot);
            item.gameObject.SetActive(true);
            item.SetData(r.characterData);
            item.SetBadgesVisible(false);
        }
        else
        {
            var item = Instantiate(CabinSkinCardItem, RewardRoot);
            item.gameObject.SetActive(true);
            item.SetData(r.characterData);
            item.SetBadgesVisible(false);
        }
    }

    /// <summary>返回指定 TagType 对应的角标 Sprite</summary>
    public Sprite GetTagSprite(TagType tagType)
    {
        return tagType switch
        {
            TagType.zengsong => TagZengsongSprite,
            _ => null,
        };
    }

    private Sprite GetSpecialIconSprite(specialIconType iconType)
    {
        return iconType switch
        {
            specialIconType.aiCredit => IconAICreditSprite,
            _ => null,
        };
    }

    /// <summary>向奖励列表追加一个 AICredit 特殊图标项</summary>
    public void AddSpecialIcon()
    {
        var item = Instantiate(RewardItemPrefab, RewardRoot);
        item.gameObject.SetActive(true);
        item.Init(new CommonRewardItemData { RewardAmount = 30000, rewardName = "AI能量" },
                  null, IconAICreditSprite);
        if (_rewardRootFitter != null) _rewardRootFitter.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)RewardRoot);
    }
}



public enum TagType
{
    none,     // 没有角标
    zengsong, // Assets\Loadable\UI\UIPanel\CommonPanel\CommonBoxReward/tag/tag_zengsong.png
}

public enum specialIconType
{
    none,
    aiCredit, // Assets\Loadable\UI\UIPanel\CommonPanel\CommonBoxReward/icon/aiCredit.png
}
