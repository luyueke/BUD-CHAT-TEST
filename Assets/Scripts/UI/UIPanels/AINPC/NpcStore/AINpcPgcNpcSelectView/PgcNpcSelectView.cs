using System;
using System.Collections;
using System.Collections.Generic;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class PgcNpcSelectView : MonoBehaviour
    {
        public PgcNpcSelectItem ItemPrefab;
        public Transform ItemContent;

        private Action<AINpcInfo> _onSelectedAct;
        private Func<string> GetCurSelectedIdFunc;
        public List<AINpcInfo> _PgcAINpcInfos = new List<AINpcInfo>();
        private List<PgcNpcSelectItem> _npcSelectItems = new List<PgcNpcSelectItem>();
        
        public void Init()
        {
            string yumiStr = "{\"npcAge\":-1,\"npcGender\":1,\"npcAvatarJson\":\"{\\\"partDatas\\\":[{\\\"Id\\\":\\\"0\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"#834CCF\\\",\\\"Pos\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Rot\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Sca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0},{\\\"Id\\\":\\\"12200000\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"#FFDDDC\\\",\\\"Pos\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Rot\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Sca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0},{\\\"Id\\\":\\\"12300001\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"\\\",\\\"Pos\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Rot\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Sca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0},{\\\"Id\\\":\\\"10400006\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"\\\",\\\"Pos\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Rot\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Sca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0},{\\\"Id\\\":\\\"12400002\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"\\\",\\\"Pos\\\":{\\\"x\\\":-0.06,\\\"y\\\":-0.09,\\\"z\\\":0},\\\"Rot\\\":{\\\"x\\\":0,\\\"y\\\":-90,\\\"z\\\":-180},\\\"Sca\\\":{\\\"x\\\":1,\\\"y\\\":1,\\\"z\\\":1},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0},{\\\"Id\\\":\\\"11100014\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"\\\",\\\"Pos\\\":{\\\"x\\\":-0.05,\\\"y\\\":0.22,\\\"z\\\":0},\\\"Rot\\\":{\\\"x\\\":-166,\\\"y\\\":90,\\\"z\\\":0},\\\"Sca\\\":{\\\"x\\\":1,\\\"y\\\":1,\\\"z\\\":1},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0},{\\\"Id\\\":\\\"10600027\\\",\\\"Uid\\\":\\\"\\\",\\\"Url\\\":\\\"\\\",\\\"LRType\\\":0,\\\"Cr\\\":\\\"\\\",\\\"Pos\\\":{\\\"x\\\":-0.16,\\\"y\\\":0.21,\\\"z\\\":0.1},\\\"Rot\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"Sca\\\":{\\\"x\\\":1,\\\"y\\\":1,\\\"z\\\":1},\\\"CSca\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"CAnchor\\\":{\\\"x\\\":0,\\\"y\\\":0,\\\"z\\\":0},\\\"UgcStyle\\\":0,\\\"bodyId\\\":0}]}\",\"animResType\":0,\"npcAnimations\":null,\"npcPetPhrases\":null,\"npcPortraitUrl\":null,\"isBan\":0,\"paymentInfo\":{\"price\":80,\"currencyType\":5},\"id\":\"2qv4c6hAU2JLEuEpmspL5P0sapc\",\"name\":\" 80\",\"desc\":\"2024-12-26\",\"cover\":\"https://u3d-business-data-1318932159.cos.accelerate.myqcloud.com/AINpc/characterInfo/1820446869097504768/1820446869097504768_1735196339.png\",\"metaDataUrl\":\"\",\"creator\":\"1820446869097504768\",\"createTime\":1735529791,\"updateTime\":1735529791,\"draftVersion\":0,\"textureUrl\":null,\"isLocal\":false,\"editTime\":0,\"auditInfo\":{\"rejectReason\":null,\"auditResult\":3},\"designCode\":\"C24DRST\",\"coverAutoSaved\":0,\"forceUpdate\":0,\"migrateData\":0,\"templateId\":\"\"}";

            var yumiInfo = JsonConvert.DeserializeObject<AINpcInfo>(yumiStr);
            _PgcAINpcInfos.Add(yumiInfo);

            for (int i = 0; i < _PgcAINpcInfos.Count; i++)
            {
                 var item = GameObject.Instantiate(ItemPrefab, ItemContent);
                 item.InitData(_PgcAINpcInfos[i], this._onSelectedAct);
                 _npcSelectItems.Add(item);
            }
        }

        public void SetOnClickAction(Action<AINpcInfo> acr)
        {
            this._onSelectedAct = acr;
        }
        
        public void SetGetSelectedIdFunc(Func<string> func)
        {
            this.GetCurSelectedIdFunc = func;
        }

        public void Refresh()
        {
            _npcSelectItems.ForEach(x=>x.SetSelectState(false));
        }
    }
}