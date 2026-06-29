using System;
using System.Collections.Generic;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Game.AssetToolBox
{
    public class ToolBoxStoreDataLoader : ToolBoxBaseDataLoader
    {
        public UgcType ugcType = UgcType.Prop;
        public override void GetDatas(Action<List<ToolBoxItemData>> resultAction)
        {
            base.GetDatas(resultAction);

            if (_isEnd)
                return;

            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["toolBoxUgcType"] = (int)ugcType
            };
            var reqParam = JsonConvert.SerializeObject(jb);

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionInfo, HttpMethod.GET, reqParam, (content) =>
            {
                GetStoreToolBoxDataSuccess(content, resultAction);
            }, GetStoreToolBoxDataFail);
        }
        
        
        private void GetStoreToolBoxDataSuccess(string content, Action<List<ToolBoxItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<ToolBoxDataRsp>(content);
            this._isEnd = sectionInfoRsp.IsEnd == 1;
            this._cookie = sectionInfoRsp.cookie;
            
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<ToolBoxItemData>());
            }
            else
            {
                resultAction?.Invoke(sectionInfoRsp.list);
            }
        }

        private void GetStoreToolBoxDataFail(string error)
        {
            LoggerUtils.LogError("GetStoreToolBoxDataFail" + error);
        }
    }
    
}
