using System;
using System.Collections.Generic;
using Game.Database;
using GameData.PgcData;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Store {
    public class PetAvatarUgcSceneHandler : AvatarUgcSceneHandler {
        internal override UGCDataRequest GetUgcDataRequest(int classType, int ugcType, int recommendId = 0) {
            if (ugcDataDict.ContainsKey(classType)) return ugcDataDict[classType];


            var dataRequest = new UGCDataRequest(ugcType, recommendId, classType == (int)OtherClass.LikeList, 1);
            ugcDataDict.Add(classType, dataRequest);
            return dataRequest;
        }

        protected override UGCAssetsData CreateUgcAssetsData(int classType, RecommendItemData data) {
            UGCAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UGCPetAvatar;
            assetsData.AvatarSubType = (AvatarSubType)data.skinInfo.subType;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.skinInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.skinInfo.paymentInfo.currencyType,
                    Value = data.skinInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        public override Action SearchGoodsData(int classType, string searchKey, Action<string, bool, List<GoodsData>> action) {
            var url = HttpUrlDefine.SearchSkin;
            var resourcesType = UniqueType.ResourceType(classType);
            switch (resourcesType)
            {
                case ResourceType.UgcAvatar:
                    url = HttpUrlDefine.SearchSkin;
                    break;
                case ResourceType.MusicScore:
                    url = HttpUrlDefine.SearchMusicScore;
                    break;
                case ResourceType.UgcEmote:
                    url = HttpUrlDefine.SearchAnimation;
                    break;
                case ResourceType.UgcPose:
                    url = HttpUrlDefine.SearchPose;
                    break;
            }
            var handler = new HttpPageRequestHandle<UgcAvatarPageUseData>(url, JsonConvert.SerializeObject(new JObject() { ["searchWord"] = searchKey, ["subType"] = (int)UniqueType.AvatarSubType(classType) , ["skinType"] = 1}), HttpMethod.GET);
            List<GoodsData> goodsDatas = new() { };
            AddGoodsData(ref goodsDatas, classType, handler.GetAllData());
            handler.AddSuccessAction((handlerData) =>
            {
                AddGoodsData(ref goodsDatas, classType, new List<UgcAvatarPageUseData>() { handlerData });
                action?.Invoke(searchKey, handler.IsEnd, goodsDatas);
            });
            handler.AddFailAction((HttpResponseRawData) =>
            {
                action?.Invoke(searchKey, true, goodsDatas);
            });

            handler.Start();
            return handler.Next;
        }
    }
}
