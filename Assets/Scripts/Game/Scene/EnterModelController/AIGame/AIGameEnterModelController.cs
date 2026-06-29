using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using GameData;
using GameData.MapData;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using HLOD;
using Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Map;
using UnityEngine;

namespace Game.Scene.EnterModelController
{
    public class AIGameEnterModelController : BaseEnterModelController
    {
        internal string _localMetaJsonPath = "";
        protected PMapData pMapData = null;
        
        //需要在这里传入本地PB路径
        public virtual void StartAIGame()
        {
            if (!isInitialized)
            {
                Init();
                LoadAIGameMap();
            }
        }
        
        protected virtual void LoadAIGameMap()
        {
             this.pMapData = ConvertJsonToBytes(this._localMetaJsonPath);
             BuildMap();
        }
        
        protected virtual bool BuildMap()
        {
            try
            {
                if (pMapData != null)
                {
                    MessageHelper.Broadcast<EnterGameModel, object>(MessageName.StartBuildMap, GameController.enterGameModel, pMapData);
                    SceneBuilder.Inst.BuildMapByData(pMapData);
                    MessageHelper.Broadcast<EnterGameModel>(MessageName.OverBuildMap, GameController.enterGameModel);
                    GameController.ChangeMode(GameMode.AIGuset);
                }
                return true;
            }
            catch (Exception e)
            {
                LoggerUtils.LogError("地图解析失败:" + e.Message + ", e:" + e.StackTrace);
                GameController.ExitGame("地图解析失败:" + e.Message);
                return false;
            }
        }
        
        #region 地图Json解析 + Json转PB 转PMapData
        private PMapData ConvertJsonToBytes(string jsonFilePath)
        {
            // 读取 JSON 文件
            var jsonObj = new GameObject();
            var jsonContent = Loader.Load<TextAsset>(_localMetaJsonPath, jsonObj);

            // 解析 JSON 数据到 JObject
            var jsonData = JsonConvert.DeserializeObject<JObject>(jsonContent.text); // Use JObject here

            // 创建 PMapData 实例
            PMapData pMapData = new PMapData();

            // 填充 BasicData
            if (jsonData["BasicData"] is JObject basicData)
            {
                pMapData.BasicData = new PGameBasicData
                {
                    Version = (int)basicData["Version"],
                    EditTime = (ulong)basicData["EditTime"]
                };
            }

            // 填充 PropData
            if (jsonData["PropData"] is JObject propData)
            {
                PGamePropData gamePropData = new PGamePropData();

                // 处理 Pref
                if (propData["Pref"] is JArray prefList)
                {
                    foreach (var prefItem in prefList)
                    {
                        PNodeData nodeData = new PNodeData
                        {
                            Uid = (uint)prefItem["Uid"],
                            PropId = (string)prefItem["PropId"],
                            Pos = new PVector3
                            {
                                X = (float)prefItem["Pos"]["X"],
                                Y = (float)prefItem["Pos"]["Y"],
                                Z = (float)prefItem["Pos"]["Z"]
                            },
                            Rotation = new PVector3
                            {
                                X = (float)prefItem["Rotation"]["X"],
                                Y = (float)prefItem["Rotation"]["Y"],
                                Z = (float)prefItem["Rotation"]["Z"]
                            },
                            Scale = new PVector3
                            {
                                X = (float)prefItem["Scale"]["X"],
                                Y = (float)prefItem["Scale"]["Y"],
                                Z = (float)prefItem["Scale"]["Z"]
                            }
                        };

                        // 处理 Attrs
                        if (prefItem["Attrs"] is JArray attrsList)
                        {
                            foreach (var attrItem in attrsList)
                            {
                                PComponentData componentData = new PComponentData
                                {
                                    CmpId = (uint)attrItem["CmpId"],
                                    CmpData = ParseCmpData(attrItem["CmpData"]) // 处理 CmpData（Any 类型）
                                };
                                nodeData.Attrs.Add(componentData);
                            }
                        }

                        // 处理 Prims（暂时为空）
                        if (prefItem["Prims"] is JArray primsList)
                        {
                            foreach (var prim in primsList)
                            {
                                // Handle prims if any (empty in your sample JSON)
                            }
                        }

                        gamePropData.Pref.Add(nodeData);
                    }
                }

                // 填充 UgcMatList (为空)
                if (propData["UgcMatList"] is JObject ugcMatList)
                {
                    // Handle UgcMatList if needed
                }

                pMapData.PropData = gamePropData;
            }

            // 填充 SettingData
            if (jsonData["SettingData"] is JObject settingData)
            {
                PGameSettingData gameSettingData = new PGameSettingData();

                // 处理 Attrs
                if (settingData["Attrs"] is JArray attrsList)
                {
                    foreach (var attrItem in attrsList)
                    {
                        PComponentData componentData = new PComponentData
                        {
                            CmpId = (uint)attrItem["CmpId"],
                            CmpData = ParseCmpData(attrItem["CmpData"]) // 处理 CmpData（Any 类型）
                        };
                        gameSettingData.Attrs.Add(componentData);
                    }
                }

                pMapData.SettingData = gameSettingData;
            }

            // 填充 UgcmatData (为空)
            if (jsonData["UgcmatData"] is JObject ugcMatData)
            {
                PGameUGCMatMapData gameUgcMatData = new PGameUGCMatMapData();

                // Handle MatDataList if needed
                pMapData.UgcmatData = gameUgcMatData;
            }

            // 填充 UgcItemData (为空)
            if (jsonData["UgcItemData"] is JObject ugcItemData)
            {
                PGameUGCItemMapData gameUgcItemData = new PGameUGCItemMapData();

                // Handle ItemDataList if needed
                pMapData.UgcItemData = gameUgcItemData;
            }

            // 填充 HlodData (为空)
            if (jsonData["HlodData"] is JObject hlodData)
            {
                PGameHLODData gameHlodData = new PGameHLODData();

                // Handle GridData if needed
                pMapData.HlodData = gameHlodData;
            }
            
            return pMapData;
        }

        // 处理 Any 类型的字段（CmpData）
// 处理 Any 类型的字段（CmpData）
        private Google.Protobuf.WellKnownTypes.Any ParseCmpData(JToken cmpDataJson)
        {
            // 解析 Any 类型的 TypeUrl
            string typeUrl = (string)cmpDataJson["TypeUrl"];

            // 创建 Any 类型对象
            Google.Protobuf.WellKnownTypes.Any any = new Google.Protobuf.WellKnownTypes.Any
            {
                TypeUrl = typeUrl
            };

            // 检查是否有 Value 字段
            var value = cmpDataJson["Value"];

            if (value != null && value is JArray valueArray)
            {
                // 如果 Value 是数组格式，直接将数组转换为 byte[]
                byte[] valueBytes = valueArray.ToObject<byte[]>();
                any.Value = Google.Protobuf.ByteString.CopyFrom(valueBytes);
            }
            else if (value == null)
            {
                // // 如果没有 Value 字段，则将整个 CmpData（去除 TypeUrl）转为 JSON 字符串
                // // 提取 TypeUrl 以外的其他字段
                // var fields = cmpDataJson.DeepClone() as JObject;
                // fields.Remove("TypeUrl");  // 移除 TypeUrl 字段
                //
                // // 将剩余字段序列化为 JSON 格式的字节数据
                // byte[] valueBytes = System.Text.Encoding.UTF8.GetBytes(fields.ToString(Newtonsoft.Json.Formatting.None));  // 序列化为无格式的 JSON 字符串
                //
                // // 设置 Any 的 Value 字段
                // any.Value = Google.Protobuf.ByteString.CopyFrom(valueBytes);
                
                // 如果 TypeUrl 是 AIYandereTriggerAreaComponent 的类型
                if (typeUrl == "type.googleapis.com/pb.map.AIYandereTriggerAreaComponentData")
                {
                    // 解析 SpawnDefault 和 SpawnIndex
                    var tag = (int)cmpDataJson["tag"];

                    // 创建 PSpawnPointComponentData 对象
                    var taTriggerAreaComponentData = new AIYandereTriggerAreaComponentData()
                    {
                        Tag = tag
                    };
                    
                    any.Value = ByteString.CopyFrom(taTriggerAreaComponentData.ToByteArray());
                    any = Any.Pack(taTriggerAreaComponentData);
                }
                
                else if (typeUrl == "type.googleapis.com/pb.map.AIYandereCommonComponentData")
                {
                    // 解析 SpawnDefault 和 SpawnIndex
                    var tag = (int)cmpDataJson["tag"];
                    var pos = (string)cmpDataJson["pos"];
                    var rot = (string)cmpDataJson["rot"];
                    var index = (int)cmpDataJson["index"];
                    // 创建 PSpawnPointComponentData 对象
                    var taTriggerAreaComponentData = new AIYandereCommonComponentData()
                    {
                        Tag = tag,
                        Pos = pos,
                        Rot = rot,
                        Index = index,
                    };
                    
                    any.Value = ByteString.CopyFrom(taTriggerAreaComponentData.ToByteArray());
                    any = Any.Pack(taTriggerAreaComponentData);
                }
                
                else if (typeUrl == "type.googleapis.com/pb.map.AIGameCommonComponentData")
                {
                    // 解析 SpawnDefault 和 SpawnIndex
                    var tag = (int)cmpDataJson["tag"];
                    var pos = (string)cmpDataJson["pos"];
                    var rot = (string)cmpDataJson["rot"];
                    var index = (int)cmpDataJson["index"];
                    var location = (int)cmpDataJson["location"];
                    string color = (string)cmpDataJson["color"];
                    var neighbor = JsonConvert.DeserializeObject<int[]>(cmpDataJson["neighbor"].ToString());
                    // 创建 PSpawnPointComponentData 对象
                    var taTriggerAreaComponentData = new AIGameCommonComponentData()
                    {
                        Tag = tag,
                        Pos = pos,
                        Rot = rot,
                        Index = index,
                        Location = location,
                        Color = color,
                    };
                    taTriggerAreaComponentData.Neighbor.AddRange(neighbor);
                    any.Value = ByteString.CopyFrom(taTriggerAreaComponentData.ToByteArray());
                    any = Any.Pack(taTriggerAreaComponentData);
                }
                
                else if (typeUrl == "type.googleapis.com/pb.map.AIYandereNodeComponentData")
                {
                    // 解析 SpawnDefault 和 SpawnIndex
                    var nodeName = (string)cmpDataJson["nodeName"];
                    // 创建 PSpawnPointComponentData 对象
                    var taTriggerAreaComponentData = new AIYandereNodeComponentData()
                    {
                        NodeName = nodeName,
                    };
                    any.Value = ByteString.CopyFrom(taTriggerAreaComponentData.ToByteArray());
                    any = Any.Pack(taTriggerAreaComponentData);
                }
            }
            else
            {
                // 处理其他未知格式，如果有 Value 字段但不是数组或 null
                throw new InvalidOperationException("Unsupported 'Value' format. Expected either an array or no Value.");
            }

            return any;
        }
        #endregion
    }
}