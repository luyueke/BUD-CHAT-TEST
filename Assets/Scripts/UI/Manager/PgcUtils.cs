using Es;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Store;
using GameData.Gashapon;
using GameData.Manager;
using GameData.PgcData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Manager {
    public class PgcUtils {
        #region 数据接口

        public static GameResData GetPgcConfigData(int pgcId) {
            return DataTables.GetGameResData(pgcId.ToString());
        }

        public static GameResData GetPgcConfigData(string pgcId) {
            return DataTables.GetGameResData(pgcId);
        }

        public static ResourceType GetPgcResourceType(int pgcId) {
            var resourceType = GetPgcConfigData(pgcId)?.ResourceType;
            if (resourceType != null) return (ResourceType)resourceType;
            return ResourceType.ErrResourceType;
        }

        public static EmoUIConfig GetPgcEmoteUIConfigData(string pgcId) {
            return DataTables.GetEmoUIConfig(pgcId);
        }

        #endregion

        #region Avatar

        public const string AvatarAtlasPath = "Assets/Loadable/UI/SpriteAltas/{0}.spriteatlas";
        public const string PetAvatarAtlasPath = "Assets/Loadable/UI/SpriteAltas/Pet{0}.spriteatlas";
        public const string PetTemplatePath = "Assets/Loadable/UI/SpriteAltas/UGCAvatarIcon.spriteatlas";
        public const string VehicleAtlasPath = "Assets/Loadable/UI/SpriteAltas/VehicleIcon.spriteatlas";
        public static string GetAvatarIconName(string pgcId) {
            var data = Es.DataTables.GetAvatarCommonData(pgcId);
            return data.Icon;
        }

        public static Sprite LoadAvatarIcon(string pgcId, GameObject go) {
            var data = Es.DataTables.GetAvatarCommonData(pgcId);
            return LoadAvatarIcon(data, go);
        }

        public static string GetPetAvatarIconName(string pgcId) {
            var data = Es.DataTables.GetPetAvatarCommonData(pgcId);
            return data.Icon;
        }

        public static Sprite LoadPetAvatarIcon(string pgcId, GameObject go) {
            var data = Es.DataTables.GetPetAvatarCommonData(pgcId);
            return LoadPetAvatarIcon(data, go);
        }

        public static Sprite LoadVehicleIcon(string pgcId, GameObject go) {
            var data = Es.DataTables.GetVehicleUIConfig(pgcId);
            return LoadVehicleIcon(data, go);
        }

        public static void LoadAvatarIconAsync(string pgcId, GameObject go, Action<Sprite> onComplete) {
            var data = Es.DataTables.GetAvatarCommonData(pgcId);
            if (data == null) return;
            LoadAvatarIconAsync(data, go, onComplete);
        }
        public static void LoadPetUGCTemplateAsync(string pgcId, GameObject go, Action<Sprite> onComplete) {
            var data = Es.DataTables.GetPetClothesTemplate(pgcId);
            if (data!=null)
            {
                XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(
                    PetTemplatePath, data.Cover, go,
                    onComplete);
            }
        }
        public static void LoadPetAvatarIconAsync(string pgcId, GameObject go, Action<Sprite> onComplete) {
            var data = Es.DataTables.GetPetAvatarCommonData(pgcId);
            LoadPetAvatarIconAsync(data, go, onComplete);
        }

        public static Sprite LoadAvatarIcon(AvatarCommonData avatarCommonData, GameObject go) {
            if (avatarCommonData == null)
            {
                return null;
            }
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(
                string.Format(AvatarAtlasPath, (AvatarSubType)avatarCommonData.SubType), avatarCommonData.Icon, go);
        }

        public static Sprite LoadPetAvatarIcon(AvatarCommonData avatarCommonData, GameObject go) {
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(
                string.Format(PetAvatarAtlasPath, (AvatarSubType)avatarCommonData.SubType), avatarCommonData.Icon, go);
        }

        public static Sprite LoadVehicleIcon(VehicleUIConfig vehicleUIConfig, GameObject go) {
            if (vehicleUIConfig == null) { return null; }
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(VehicleAtlasPath, vehicleUIConfig.icon, go);
        }

        public static void LoadAvatarIconAsync(AvatarCommonData avatarCommonData, GameObject go,
            Action<Sprite> onComplete) {
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(
                string.Format(AvatarAtlasPath, (AvatarSubType)avatarCommonData.SubType), avatarCommonData.Icon, go,
                onComplete);
        }

        public static void LoadPetAvatarIconAsync(AvatarCommonData avatarCommonData, GameObject go,
            Action<Sprite> onComplete) {
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(
                string.Format(PetAvatarAtlasPath, (AvatarSubType)avatarCommonData.SubType), avatarCommonData.Icon, go,
                onComplete);
        }

        #endregion

        #region Emote

        public static string GetEmoteIconName(string pgcId) {
            var data = DataTables.GetEmoUIConfig(pgcId);
            if (data == null) {
                LoggerUtils.LogError($"Can not find emote ui config ,pgcID:{pgcId}");
                return null;
            }

            return data.pgcEmoteIconName;
        }
        
        public static string GetEmoteName(string pgcId) {
            var data = DataTables.GetEmoUIConfig(pgcId);
            if (data == null) {
                LoggerUtils.LogError($"Can not find emote ui config ,pgcID:{pgcId}");
                return null;
            }

            return data.name;
        }

        public static Sprite LoadEmoteIcon(string pgcId, GameObject go) {
            var data = DataTables.GetEmoUIConfig(pgcId);
            if (data == null) {
                LoggerUtils.LogError($"Can not find emote ui config ,pgcID:{pgcId}");
                return null;
            }

            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, data.pgcEmoteIconName, go);
        }

        public static void LoadEmoteIconAsync(string pgcId, GameObject go, Action<Sprite> onComplete) {
            var data = DataTables.GetEmoUIConfig(pgcId);
            if (data == null) {
                LoggerUtils.LogError($"Can not find emote ui config ,pgcID:{pgcId}");
                return;
            }

            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcEmoteSprite);
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(atlas, data.pgcEmoteIconName, go, onComplete);
        }

        #endregion


        #region 货币

        public static CurrencyType ParseCurrencyType(string pgcID) {
            var configData = DataTables.GetGameResData(pgcID);
            return ParseCurrencyType(configData);
        }

        public static CurrencyType ParseCurrencyType(GameResData configData) {
            if (configData is not { ResourceType: (int)ResourceType.Currency }) {
                return CurrencyType.None;
            }

            return (CurrencyType)configData.SubType;
        }

        public static readonly Dictionary<CurrencyType, string> CurrencyIconPath =
            new Dictionary<CurrencyType, string>() {
                { CurrencyType.None, "" },
                { CurrencyType.Coin, "icn_common_coin_big" },
                { CurrencyType.Badge, "icn_common_badge_big" },
                { CurrencyType.Gem, "icn_common_gem_big" },
                { CurrencyType.PinkCoin, "icn_common_pink_big" },
                { CurrencyType.GreenCoin, "icn_common_green_big" },
                { CurrencyType.EnergyCoin, "icn_common_creator_big" },
                { CurrencyType.LuckyCoin, "icn_common_lucky_big" },
                { CurrencyType.ChristmasCoin, "icn_common_christmas_big" },
                { CurrencyType.MagicCoin, "icn_common_magic_big" },
                { CurrencyType.LuckyTicket, "icn_common_luckyticket_big" },
                { CurrencyType.ChristmasTicket, "icn_common_christmasticket_big" },
                { CurrencyType.CoinTicket, "icn_common_cointicket_big" },
                { CurrencyType.YouYouCoin, "icn_common_youyou_big" },
                { CurrencyType.PurpleDreamCoin, "icn_common_purpledream_big" },
                { CurrencyType.PopularityTicket, "icn_common_greenTicket_big" },
                { CurrencyType.LuckyStar, "icn_common_luckystar_big"},
                { CurrencyType.GiftTicket, "icn_common_giftTicket_big" },
                { CurrencyType.SweetieTicket, "icn_common_sweetTicket_big" },
                { CurrencyType.PurpleDreamTicket, "icn_common_purpledream_ticket_big" },
                { CurrencyType.AISeasonCoin ,"icon_currency_23" },
                { CurrencyType.CelebrationCoin ,"icn_common_CelebrationCoin_big" },
                { CurrencyType.CommunityInstrumentTicket ,"icn_common_CommunityInstrumentTicket_big" },
                { CurrencyType.CommunitySkinTicket ,"icn_common_CommunitySkinTicket_big" },
                { CurrencyType.CommunityAnimationTicket ,"icn_common_CommunityAnimationTicket_big" },
                { CurrencyType.KoiGachaCoin,"icn_reward_lucky_koi"},
                { CurrencyType.MiaoCoin,"icn_common_miao_coin"},
                { CurrencyType.SeasonPassCoin,"icn_seasonpass_coin"},
                { CurrencyType.CollectionTicket, "icn_collection_coin" },
                { CurrencyType.CommunityVehicleTicket,"icn_common_vehicle_Ticket"},
                { CurrencyType.Crystal,"icn_common_crystal"},
                { CurrencyType.CrystalShards,"icn_common_crystal_shards"},
                { CurrencyType.MusicNoteCrystal,"icn_common_music_note_crystal"},
                { CurrencyType.MusicNoteCrystalShards,"icn_common_music_note_crystal_shards"},
                { CurrencyType.SockCoin,"icn_common_wa_coin"},
                { CurrencyType.SockTailTicket,"icn_common_watail_coin"},
                { CurrencyType.SockYunyunTicket,"icn_common_wayunyun_coin"},
                { CurrencyType.CommunityTheaterTicket,"icn_common_theater_onTicket_big"},
                { CurrencyType.ShrimpYuan,"icn_common_shrimp_yuan" },
                { CurrencyType.ZZZCoin,"icn_common_zzz_coin" },
                { CurrencyType.ZZZPhantomCrystal,"icn_common_zzz_phantom_crystal" },
                { CurrencyType.ZZZPhantomCrystalShards,"icn_common_zzz_phantom_crystal_shards" },
                { CurrencyType.Shovel,"icn_common_shovel_big"},
            }; 

        public static readonly Dictionary<BUDRewardType, string> RewardIconPath =
            new Dictionary<BUDRewardType, string>() {
                { BUDRewardType.ErrRewardType, "" },
                { BUDRewardType.RewardCoin, "icn_reward_coin_big" },
                { BUDRewardType.RewardBadge, "icn_reward_badge_big" },
                { BUDRewardType.RewardGem, "icn_common_gem_big" },
                { BUDRewardType.RewardBalloon, "icn_common_balloon_big" },
                { BUDRewardType.RewardPinkCoin, "icn_reward_pink_big" },
                { BUDRewardType.RewardLuckyTicket, "icn_common_luckyticket_big" },
                { BUDRewardType.RewardPianoCoin, "icn_common_piano_big" },
                { BUDRewardType.RewardFrogCoin, "icn_common_frog_big" },
                { BUDRewardType.RewardDressCoin, "icn_common_dress_big" },
                { BUDRewardType.RewardEnergyCoin, "icn_common_creator_big" },
                { BUDRewardType.RewardCrystalBall, "icn_common_crystalBall_big" },
                { BUDRewardType.RewardMovieCoin, "icn_reward_movie_big" },
                { BUDRewardType.ConsumeTwist, "icn_common_consumetwist_big" },
                { BUDRewardType.WinterCarnival, "icn_common_wintercarnival_big" },
                { BUDRewardType.RewardChristmasCoin, "icn_common_christmas_big" },
                { BUDRewardType.RewardMagicCoin, "icn_common_magic_big" },
                { BUDRewardType.RewardLuckyCoin, "icn_common_lucky_big" },
                { BUDRewardType.RewardChristmasTicket, "icn_common_christmasticket_big" },
                { BUDRewardType.RewardCoinTicket, "icn_common_cointicket_big" },
                { BUDRewardType.AnimeShoppingFestival, "icn_common_animfestival_big" },
                { BUDRewardType.RewardYouYouCoin, "icn_common_youyou_big" },
                { BUDRewardType.RewardPurpleDreamCoin, "icn_common_purpledream_big" },
                { BUDRewardType.RewardYuanJing, "icn_common_originite_big" },
                { BUDRewardType.RewardActive, "icn_common_active_big" },
                { BUDRewardType.RewardExperience, "icn_common_exp_big"},
                { BUDRewardType.RewardLuckyStar, "icn_common_luckystar_big"},
                { BUDRewardType.RewardRose, "icn_common_rose_big"},
                { BUDRewardType.RewardKey, "icn_common_key_big"},
                { BUDRewardType.RewardFireCracker, "icn_common_firework_big"},
                { BUDRewardType.RewardGiftTicket, "icn_common_giftTicket_big"},
                { BUDRewardType.RewardPopularityTicket, "icn_common_greenTicket_big"},
                { BUDRewardType.RewardVipFreeTrail, "icn_common_vip_big"},
                { BUDRewardType.RewardYouYouCoinNewYearPack, "icn_common_youyou_pack_big"},
                { BUDRewardType.RewardAIBuddyIntimacyRate, "icn_aibuddy_intimacy_big" },
                { BUDRewardType.RewardPurpleDreamTicket, "icn_common_purpledream_ticket_big"},
                { BUDRewardType.RewardPgcOptionalBox, "icn_common_pgc_optional_box_big"},
                { BUDRewardType.RewardSweetieTicket, "icn_common_sweetTicket_big" },
                { BUDRewardType.RewardAiBuddySlot, "icn_common_aibuddySlot_big"},
                { BUDRewardType.RewardSkinSlot, "icn_common_skinSlot_big" },
                { BUDRewardType.RewardNightCap, "icn_common_nightcap_big"},
                { BUDRewardType.RewardLotusLeaf, "icn_common_leaf_big"},
                { BUDRewardType.RewardAIKey, "icn_common_aiKey_big"},
                { BUDRewardType.RewardAICore, "icn_common_aicore_big"},
                { BUDRewardType.RewardCarrot, "icn_common_carrot_big"},
                { BUDRewardType.RewardAISeasonCoin, "icn_common_aiSeason_big"},
                { BUDRewardType.AbandonedRewardExperience, "icn_common_abandactive_big"},
                { BUDRewardType.RewardZongzi, "icn_common_zongzi_big"},
                { BUDRewardType.RewardCreatorCoin, "icn_common_green_big"},
                { BUDRewardType.RewardCelebrationCoin,"icn_common_CelebrationCoin_big"},
                { BUDRewardType.RewardCommunityAnimationTicket,"icn_common_CommunityAnimationTicket_big"},
                { BUDRewardType.RewardCommunityInstrumentTicket,"icn_common_CommunityInstrumentTicket_big"},
                { BUDRewardType.RewardCommunitySkinTicket,"icn_common_CommunitySkinTicket_big"},
                { BUDRewardType.RewardTypeFries , "icn_common_shutiao_big"},
                { BUDRewardType.RewardLuckyKoiTicket,"icn_reward_lucky_koi"},
                { BUDRewardType.RewardSeasonPassCoin,"icn_seasonpass_coin"},
                { BUDRewardType.RewardTypeMiaoCoin,"icn_common_miao_coin"},
                { BUDRewardType.RewardCollectionTicket,"icn_collection_coin"},
                { BUDRewardType.RewardUGCVehicleTicket,"icn_common_vehicle_Ticket"},
                { BUDRewardType.RewardCrystal,"icn_common_crystal"},
                { BUDRewardType.RewardCrystalShards,"icn_common_crystal_shards"},
                { BUDRewardType.RewardMusicNoteCrystal,"icn_common_music_note_crystal"},
                { BUDRewardType.RewardMusicNoteCrystalShards,"icn_common_music_note_crystal_shards"},
                { BUDRewardType.RewardTypeSockCoin,"icn_common_wa_coin"},
                { BUDRewardType.RewardTypeSockTailTicket,"icn_common_watail_coin"},
                { BUDRewardType.RewardTypeSockYunyunTicket,"icn_common_wayunyun_coin"},
                { BUDRewardType.RewardBib,"icn_common_wabib_coin"},
                { BUDRewardType.RewardCommunityTheaterTicket,"icn_common_theater_onTicket_big"},
                { BUDRewardType.RewardBabyShrimp,"icn_common_shrimp_yuan"},
                { BUDRewardType.RewardTypeShovel,"icn_common_shovel_big"},
                { BUDRewardType.RewardTypeZZZCoin,"icn_common_zzz_coin"},
                { BUDRewardType.RewardTypeZZZPhantomCrystal,"icn_common_zzz_phantom_crystal"},
                { BUDRewardType.RewardTypeZZZPhantomCrystalShards,"icn_common_zzz_phantom_crystal_shards"},


            };


        public static readonly Dictionary<BUDRewardType, string> RewardPreviewIconPath =
            new Dictionary<BUDRewardType, string>() {
                { BUDRewardType.ErrRewardType, "" },
                { BUDRewardType.RewardCoin, "preview_coin" },
                { BUDRewardType.RewardBadge, "preview_badge" },
                { BUDRewardType.RewardPinkCoin, "preview_pink" },
                { BUDRewardType.RewardMagicCoin, "preview_magic" },
                { BUDRewardType.RewardYouYouCoinNewYearPack, "preview_youyou_pack"},
                { BUDRewardType.RewardVipFreeTrail, "preview_vip"},
                { BUDRewardType.RewardUGCVehicleTicket, "preview_vehicle_ticket"},
              { BUDRewardType.RewardCommunityTheaterTicket,"icn_common_theater_onTicket_big"},
                { BUDRewardType.RewardCrystal, "icn_common_crystal"},
                { BUDRewardType.RewardCrystalShards, "icn_common_crystal_shards"},
                { BUDRewardType.RewardMusicNoteCrystal, "icn_common_music_note_crystal"},
                { BUDRewardType.RewardMusicNoteCrystalShards, "icn_common_music_note_crystal_shards"},
                { BUDRewardType.RewardTypeZZZCoin, "icn_common_zzz_coin"},
                { BUDRewardType.RewardTypeZZZPhantomCrystal, "icn_common_zzz_phantom_crystal"},
                { BUDRewardType.RewardTypeZZZPhantomCrystalShards, "icn_common_zzz_phantom_crystal_shards"},
            };

        public static readonly Dictionary<CurrencyType, string> CurrencyName = new Dictionary<CurrencyType, string>() {
            { CurrencyType.None, "" },
            { CurrencyType.Coin, "金币" },
            { CurrencyType.Badge, "徽章" },
            { CurrencyType.Gem, "BUD钻" },
            { CurrencyType.PinkCoin, "社区商品币" },
            { CurrencyType.GreenCoin, "创作者币" },
            { CurrencyType.EnergyCoin, "创作能量币" },
            { CurrencyType.LuckyCoin, "幸运币" },
            { CurrencyType.ChristmasCoin, "圣诞币" },
            { CurrencyType.MagicCoin, "精灵魔法币" },
            { CurrencyType.LuckyTicket, "幸运券" },
            { CurrencyType.ChristmasTicket, "铃铛券" },
            { CurrencyType.CoinTicket, "金币券" },
            { CurrencyType.YouYouCoin, "优优币" },
            { CurrencyType.PurpleDreamCoin, "紫梦币" },
            { CurrencyType.LuckyStar, "幸运星" },
            { CurrencyType.PopularityTicket, "人气券"},
            { CurrencyType.GiftTicket, "礼品券" },
            { CurrencyType.SweetieTicket, "甜心券" },
            { CurrencyType.PurpleDreamTicket, "紫梦券" },
            { CurrencyType.CommunityInstrumentTicket, "社区乐器兑换券" },
            { CurrencyType.CommunitySkinTicket, "社区皮肤兑换券" },
            { CurrencyType.CommunityAnimationTicket, "社区动作兑换券" },
            { CurrencyType.SeasonPassCoin, "通行证币" },
            { CurrencyType.CollectionTicket, "典藏券" },
            { CurrencyType.CommunityVehicleTicket, "社区载具兑换券" },
            { CurrencyType.CommunityTheaterTicket, "剧场兑换券" },
            { CurrencyType.Crystal, "水晶" },
            { CurrencyType.CrystalShards, "水晶碎片" },
            { CurrencyType.MusicNoteCrystal, "音符水晶" },
            { CurrencyType.MusicNoteCrystalShards, "音符水晶碎片" },
            { CurrencyType.ShrimpYuan, "虾元" },
            { CurrencyType.ZZZCoin, "绒币" },
            { CurrencyType.ZZZPhantomCrystal, "啧啧幻音水晶" },
            { CurrencyType.ZZZPhantomCrystalShards, "啧啧幻音碎片" }
        };

        public static readonly Dictionary<CurrencyType, string> CurrencyClearTips =
            new Dictionary<CurrencyType, string>() {
                { CurrencyType.LuckyTicket, "可前往兑换商店进行商品兑换，不会清空，可在后续赛季中持续兑换" },
                { CurrencyType.ChristmasTicket, "可前往兑换商店进行商品兑换，活动结束后会清空" },
                { CurrencyType.CoinTicket, "可前往兑换商店进行商品兑换，不会清空，可在后续赛季中持续兑换" },
            };


        public static readonly Dictionary<BUDRewardType, string> RewardName = new Dictionary<BUDRewardType, string>() {
            { BUDRewardType.ErrRewardType, "" },
            { BUDRewardType.RewardCoin, "金币" },
            { BUDRewardType.RewardBadge, "徽章" },
            { BUDRewardType.RewardGem, "BUD钻" },
            { BUDRewardType.RewardBalloon, "气球" },
            { BUDRewardType.RewardPinkCoin, "社区商品币" },
            { BUDRewardType.RewardPianoCoin, "钢琴活动币" },
            { BUDRewardType.RewardFrogCoin, "青蛙活动币" },
            { BUDRewardType.RewardDressCoin, "套装活动币" },
            { BUDRewardType.RewardEnergyCoin, "创作能量币" },
            { BUDRewardType.RewardCrystalBall, "水晶币" },
            { BUDRewardType.RewardMovieCoin, "电影币" },
            { BUDRewardType.WinterCarnival, "雪花" },
            { BUDRewardType.RewardChristmasCoin, "圣诞币" },
            { BUDRewardType.RewardMagicCoin, "精灵魔法币" },
            { BUDRewardType.RewardLuckyCoin, "幸运币" },
            { BUDRewardType.RewardChristmasTicket, "铃铛券" },
            { BUDRewardType.RewardCoinTicket, "金币券" },
            { BUDRewardType.AnimeShoppingFestival, "购物车" },
            { BUDRewardType.RewardYouYouCoin, "优优币" },
            { BUDRewardType.RewardPurpleDreamCoin, "紫梦币" },
            { BUDRewardType.RewardYuanJing, "缘晶"},
            { BUDRewardType.RewardAvatarFrame, "头像框"},
            { BUDRewardType.RewardChatBubbles, "聊天气泡"},
            { BUDRewardType.RewardLuckyStar, "幸运星"},
            { BUDRewardType.RewardActive, "活跃度"},
            { BUDRewardType.RewardExperience, "通行证经验"},
            { BUDRewardType.AbandonedRewardExperience, "通行证经验"},
            { BUDRewardType.RewardRose, "玫瑰花"},
            { BUDRewardType.RewardKey, "钥匙"},
            { BUDRewardType.RewardFireCracker, "爆竹"},
            { BUDRewardType.RewardGiftTicket, "礼品券"},
            { BUDRewardType.RewardPopularityTicket, "人气券"},
            { BUDRewardType.RewardAIBuddyIntimacyRate, "亲密度" },
            { BUDRewardType.RewardPurpleDreamTicket, "紫梦券"},
            { BUDRewardType.RewardSweetieTicket, "甜心券"},
            { BUDRewardType.RewardAiBuddySlot, "伙伴卡位"},
            { BUDRewardType.RewardSkinSlot, "皮肤卡位"},
            { BUDRewardType.RewardNightCap, "瞌睡帽"},
            { BUDRewardType.RewardLotusLeaf, "荷叶"},
            { BUDRewardType.RewardAIKey, "注射器"},
            { BUDRewardType.RewardAISeasonCoin, "AI赛季币"},
            { BUDRewardType.RewardAICore, "AI核心"},
            { BUDRewardType.RewardCarrot, "胡萝卜"},
            { BUDRewardType.RewardZongzi, "粽子"},
            { BUDRewardType.RewardCelebrationCoin, "庆典币"},
            { BUDRewardType.RewardCommunityAnimationTicket, "社区动作兑换券"},
            { BUDRewardType.RewardCommunityInstrumentTicket, "社区乐器兑换券"},
            { BUDRewardType.RewardTypeFries, "薯条"},
            { BUDRewardType.RewardLuckyKoiTicket, "幸运锦鲤抽奖券"},
            { BUDRewardType.RewardSeasonPassCoin, "通行证币"},
            { BUDRewardType.RewardSeasonPassRedeemPackage, "赛季通行证兑换皮肤动作礼包"},
            { BUDRewardType.RewardCollectionTicket, "典藏券"},
            { BUDRewardType.RewardTypeMiaoCoin, "喵币"},
            { BUDRewardType.RewardUGCVehicleTicket, "社区载具兑换券"},
            { BUDRewardType.RewardCommunityTheaterTicket, "剧场兑换券"},
            { BUDRewardType.RewardCrystal, "水晶"},
            { BUDRewardType.RewardCrystalShards, "水晶碎片"},
            { BUDRewardType.RewardMusicNoteCrystal, "音符水晶"},
            { BUDRewardType.RewardMusicNoteCrystalShards, "音符水晶碎片"},
            { BUDRewardType.TreePlantingDayWater, "水滴"},
            { BUDRewardType.TreePlantingDayFertilizer, "肥料"},
            { BUDRewardType.RewardTypeSockCoin, "袜币"},
            { BUDRewardType.RewardTypeSockTailTicket, "粉绒小尾券"},
            { BUDRewardType.RewardTypeSockYunyunTicket, "暖橙晕晕券"},
            { BUDRewardType.RewardBib, "小围兜"},
            { BUDRewardType.RewardTypeShovel, "铲子"},
            { BUDRewardType.RewardBabyShrimp, "虾元"},
            { BUDRewardType.RewardTypeZZZCoin, "绒币"},
            { BUDRewardType.RewardTypeZZZPhantomCrystal, "啧啧幻音水晶"},
            { BUDRewardType.RewardTypeZZZPhantomCrystalShards, "啧啧幻音碎片"},
            
        };


        // 图标作用域覆盖(如娃娃机)：进入场景时设置、离开时清空；为 null 时对全局零影响
        public static Dictionary<CurrencyType, string> CurrencyIconScopeOverride;
        public static Dictionary<BUDRewardType, string> RewardIconScopeOverride;
        // 货币名称作用域覆盖(如娃娃机把 水晶/碎片 改名为 星辉夹/泡泡夹)：存放原文显示串，非本地化 key；为 null 时零影响
        public static Dictionary<CurrencyType, string> CurrencyNameScopeOverride;

        public static string GetScopeCurrencyIconName(CurrencyType t)
            => (CurrencyIconScopeOverride != null && CurrencyIconScopeOverride.TryGetValue(t, out var n)) ? n : null;
        public static string GetScopeRewardIconName(BUDRewardType t)
            => (RewardIconScopeOverride != null && RewardIconScopeOverride.TryGetValue(t, out var n)) ? n : null;
        public static string GetScopeCurrencyName(CurrencyType t)
            => (CurrencyNameScopeOverride != null && CurrencyNameScopeOverride.TryGetValue(t, out var n)) ? n : null;

        public static Sprite LoadCurrencyIcon(CurrencyType tokenType, GameObject refObj) {
            var ov = GetScopeCurrencyIconName(tokenType);
            string spriteName;
            if (ov != null) spriteName = ov;
            else if (!CurrencyIconPath.TryGetValue(tokenType, out spriteName)) return null;
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, refObj);
        }

        public static Sprite LoadRewardIcon(BUDRewardType tokenType, GameObject refObj) {
            var ov = GetScopeRewardIconName(tokenType);
            string spriteName;
            if (ov != null) spriteName = ov;
            else if (!RewardIconPath.TryGetValue(tokenType, out spriteName)) {
                LoggerUtils.LogError("LoadRewardIcon error: not contains this token " + tokenType);
                return null;
            }
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, refObj);
        }

        public static Sprite LoadRewardPreviewIcon(BUDRewardType tokenType, GameObject refObj) {
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            var ov = GetScopeRewardIconName(tokenType);
            if (ov != null) {
                return XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, ov, refObj);
            }
            if (RewardPreviewIconPath.TryGetValue(tokenType, out string spriteName)) {
                return XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, refObj);
            } else {
                return LoadRewardIcon(tokenType, refObj);
            }
        }


        public static void LoadCurrencyIconAsync(CurrencyType tokenType, GameObject refObj, Action<Sprite> onComplete) {
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            var spriteName = GetScopeCurrencyIconName(tokenType) ?? CurrencyIconPath[tokenType];
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(atlas, spriteName, refObj, onComplete);
        }

        public static string GetBundleName(string bundleId) {
            var bundleDataHandler = AssetsDataManager.GetData<BundleDataHandler>();
            var bundleData = bundleDataHandler.GetBundleData(bundleId);
            return bundleData.Name;
        }

        public static Sprite LoadBundleIcon(string bundleId, GameObject refObj) {
            string spriteName = "Bundle_" + bundleId;
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle);
            var sp =  XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, refObj);
            if (sp == null) {
                LoggerUtils.LogError("LoadBundleIconAsync Error:" + spriteName);
            }
            return sp;
        }

        public static void LoadBundleIconAsync(string bundleId, GameObject refObj, Action<Sprite> onComplete) {
            string spriteName = "Bundle_" + bundleId;
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Bundle);
            XAssetLoaderMgr.Inst.LoadSpriteInAltasAsync(atlas, spriteName, refObj, (sp) => {
                if (sp == null) {
                    LoggerUtils.LogError("LoadBundleIconAsync Error:" + spriteName);
                }
                onComplete?.Invoke(sp);
            });
        }

        public static Sprite LoadCurrencyIcon(int currencyType, GameObject refObj) {
            var atlas = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            bool canConvert = Enum.IsDefined(typeof(CurrencyType), currencyType);
            if (canConvert) {
                CurrencyType tokenType = (CurrencyType)currencyType;
                var spriteName = GetScopeCurrencyIconName(tokenType) ?? CurrencyIconPath[tokenType];
                return XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlas, spriteName, refObj);
            }

            return null;
        }

        public static string GetTokenName(CurrencyType tokenType) {
            return CurrencyName.ContainsKey(tokenType) ? CurrencyName[tokenType] : "";
        }

        public static string GetRewardName(BUDRewardType tokenType) {
            return RewardName.ContainsKey(tokenType) ? RewardName[tokenType] : "";
        }

        public static string GetCurrencyIconName(string pgcId) {
            var tType = ParseCurrencyType(pgcId);
            return CurrencyIconPath[tType];
        }

        public static Sprite LoadCurrencyIcon(string pgcId, GameObject refObj) {
            var tType = ParseCurrencyType(pgcId);
            return LoadCurrencyIcon(tType, refObj);
        }

        public static void LoadCurrencyIconAsync(string pgcId, GameObject refObj, Action<Sprite> onComplete) {
            var tType = ParseCurrencyType(pgcId);
            LoadCurrencyIconAsync(tType, refObj, onComplete);
        }

        #endregion

        #region 通用接口，直接根据PgcId获取Icon

        public static void GetIconSpriteByPgcIdAsync(string pgcId, GameObject refObj, Action<Sprite> onComplete) {
            if (string.IsNullOrEmpty(pgcId)) {
                return;
            }

            var cfg = PgcUtils.GetPgcConfigData(pgcId);
            if (cfg == null) {
                LoggerUtils.LogError($"Not find pgc config :{pgcId}");
                return;
            }

            var resourceType = cfg.ResourceType;
            switch ((ResourceType)resourceType) {
                case ResourceType.Avatar:
                    LoadAvatarIconAsync(pgcId, refObj, onComplete);
                    break;
                case ResourceType.Currency:
                    LoadCurrencyIconAsync(pgcId, refObj, onComplete);
                    break;
                case ResourceType.Emote:
                    LoadEmoteIconAsync(pgcId, refObj, onComplete);
                    break;
                case ResourceType.PGCPetAvatar:
                    LoadPetAvatarIconAsync(pgcId, refObj, onComplete);
                    break;
                case ResourceType.AvatarFrame:
                    UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(pgcId, refObj, onComplete);
                    break;
                case ResourceType.ChatBubble:
                    UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(pgcId, refObj, onComplete);
                    break;

                case ResourceType.ErrResourceType:
                case ResourceType.Other:
                case ResourceType.GameProp:
                default:
                    return;
            }
        }

        public static string GetIconSpriteNameByPgcId(string pgcId) {
            if (string.IsNullOrEmpty(pgcId)) {
                return null;
            }

            var cfg = GetPgcConfigData(pgcId);
            if (cfg == null) {
                LoggerUtils.LogError($"Not find pgc config :{pgcId}");
                return null;
            }

            var resourceType = cfg.ResourceType;
            switch ((ResourceType)resourceType) {
                case ResourceType.Avatar:
                    return GetAvatarIconName(pgcId);
                case ResourceType.Currency:
                    return GetCurrencyIconName(pgcId);
                case ResourceType.Emote:
                    return GetEmoteIconName(pgcId);
                case ResourceType.PGCPetAvatar:
                    return GetPetAvatarIconName(pgcId);

                case ResourceType.ErrResourceType:
                case ResourceType.Other:
                case ResourceType.GameProp:
                default:
                    return null;
            }
        }

        public static Sprite GetIconSpriteByPgcId(string pgcId, GameObject refObj) {
            if (string.IsNullOrEmpty(pgcId)) {
                return null;
            }

            var cfg = PgcUtils.GetPgcConfigData(pgcId);
            if (cfg == null) {
                LoggerUtils.LogError($"Not find pgc config :{pgcId}");
                return null;
            }

            var resourceType = cfg.ResourceType;
            switch ((ResourceType)resourceType) {
                case ResourceType.Avatar:
                    return LoadAvatarIcon(pgcId, refObj);
                case ResourceType.Currency:
                    return LoadCurrencyIcon(pgcId, refObj);
                case ResourceType.Emote:
                    return LoadEmoteIcon(pgcId, refObj);
                case ResourceType.PGCPetAvatar:
                    return LoadPetAvatarIcon(pgcId, refObj);
                case ResourceType.Vehicle:
                    return LoadVehicleIcon(pgcId, refObj);
                case ResourceType.ErrResourceType:
                case ResourceType.Other:
                case ResourceType.GameProp:
                default:
                    return null;
            }
        }

        public static ResourceType GetTypeByPgcId(string pgcId)
        {
            if (string.IsNullOrEmpty(pgcId))
            {
                return ResourceType.ErrResourceType;
            }

            var cfg = PgcUtils.GetPgcConfigData(pgcId);
            if (cfg == null)
            {
                LoggerUtils.LogError($"Not find pgc config :{pgcId}");
                return ResourceType.ErrResourceType;
            }

            return (ResourceType)cfg.ResourceType;
        }

        #endregion

        #region Npc

        public const string NpcHeadPath = "Assets/Loadable/UI/SpriteAltas/NpcHead.spriteatlas";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static Sprite LoadNpcHeadIcon(string name, GameObject go)
        {
            return XAssetLoaderMgr.Inst.LoadSpriteInAltas(NpcHeadPath, name, go);
        }


        #endregion
    }
}
