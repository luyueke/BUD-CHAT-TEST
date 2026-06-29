using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Store;
using GameData.Rewards;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using Product;

public class GashaponRewardItemView : MonoBehaviour
{
    [SerializeField] private Image bg;
    [SerializeField] private Image itemImg;
    [SerializeField] private Text numText;
    [SerializeField] private GameObject replacedObj;
    [SerializeField] private GameObject criticalObj;

    public void SetData(RewardInfo data)
    {
        replacedObj.gameObject.SetActive(false);
        criticalObj.gameObject.SetActive(false);
        // ErrLevel = 0; S = 1; A = 2; B = 3;
        if (data.level == (int)RewardLevel.Gold)
        {
            bg.color = DataUtil.DeSerializeColorByHex("#FFA95A");
        } else if (data.level == (int)RewardLevel.Purple)
        {
            bg.color = DataUtil.DeSerializeColorByHex("#9F72FF");
        } else if (data.level == (int)RewardLevel.Blue)
        {
            bg.color = DataUtil.DeSerializeColorByHex("#92BEFF");
        }


        string picName = "";
        if (data.amount != 0)
        {
            numText.gameObject.SetActive(true);
            numText.text = $"x{data.amount}";
        }
        else
        {
            numText.gameObject.SetActive(false);
        }

        if (data.rewardType == (int)Product.RewardType.RewardBadge)
        {
            picName = "ic_rewards_big_2";
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
            itemImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, picName, gameObject);
        }
        else if (data.rewardType == (int)Product.RewardType.RewardHomepageSkin)
        {
            var sprite = ProfileThemeManager.Inst.LoadThemeIcon("RewardHomepageSkin_1", gameObject);
            if (sprite != null)
            {
                itemImg.sprite = sprite;
            }
        }
        else if (data.rewardType == (int)Product.RewardType.RewardCoin)
        {
            picName = "ic_rewards_big_1";
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
            itemImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, picName, gameObject);
        }
        else if (data.rewardType == (int)Product.RewardType.RewardGem)
        {
            picName = "ic_rewards_big_3";
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
            itemImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, picName, gameObject);
        }
        else if (data.rewardType == (int)BUDRewardType.RewardEnergyCoin)
        {
            picName = "ic_rewards_big_16";
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
            itemImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, picName, gameObject);
        }
        else if (data.rewardType == (int)Product.RewardType.RewardPgcResource)
        {
            var pgcId = data.pgcId;
            if (string.IsNullOrEmpty(pgcId))
            {
                return;
            }

            itemImg.sprite = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
        }
        else if (data.rewardType == (int)BUDRewardType.RewardPinkCoin)
        {
            picName = "ic_rewards_big_7";
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
            itemImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, picName, gameObject);
		}
        else if (data.rewardType == (int)BUDRewardType.RewardChatBubbles)
        {
            UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(data.pgcId,gameObject, (iconSprite) =>
            {
                if (this != null && itemImg != null && iconSprite != null)
                {
                    itemImg.sprite = iconSprite;
                }
            });
        }
        else if (data.rewardType == (int)BUDRewardType.RewardAvatarFrame)
        {
            UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.pgcId,gameObject, (iconSprite) =>
            {
                if (this != null && itemImg != null && iconSprite != null)
                {
                    itemImg.sprite = iconSprite;
                }
            });
        }
        else if (data.rewardType == (int)BUDRewardType.RewardPgcBundle)
        {
            var sprite = PgcUtils.LoadBundleIcon(data.bundleId, gameObject);
            if (sprite != null)
            {
                itemImg.sprite = sprite;
            }
        }
        else if (data.rewardType == (int)BUDRewardType.RewardUgcTemplateResource)
        {
            PgcUtils.LoadPetUGCTemplateAsync(data.pgcId, gameObject, (iconSprite) =>
            {
                if (this != null && itemImg != null && iconSprite != null)
                {
                    itemImg.sprite = iconSprite;
                }
            });
        }
        else if(data.rewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
        {
            var sprite = UserUIWidgetManager.Inst.GetNicknameBg(int.Parse(data.pgcId), this.gameObject);
            if (sprite != null)
            {
                itemImg.sprite = sprite;
                itemImg.SetNativeSize();
                itemImg.transform.localScale *= 0.5f;
                itemImg.transform.localPosition = Vector3.zero;
            }
        }
        else if(data.rewardType == (int)BUDRewardType.RewardTypeTitle)
        {
            var sprite = UserUIWidgetManager.Inst.GetTitleBg(int.Parse(data.pgcId), this.gameObject);
            if (sprite != null)
            {
                itemImg.sprite = sprite;
                itemImg.SetNativeSize();
                itemImg.transform.localScale *= 0.8f;
                itemImg.transform.localPosition = Vector3.zero;
            }
        }
        else if(data.rewardType == (int)BUDRewardType.RewardBabyShrimp)
        {
            picName = "ic_shrimp_yuan";
            var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
            itemImg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, picName, gameObject);
        }
        else if (Enum.IsDefined(typeof(BUDRewardType), data.rewardType))
        {
            var sprite = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
            if (sprite != null)
            {
                itemImg.sprite = sprite;
            }
        }

        if (data.isCritical == 1) {
            criticalObj.SetActive(true);
        }

        if (data.isReplaced == 1) {
            replacedObj.SetActive(true);
        }
    }
}
