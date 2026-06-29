using System;
using System.Collections;
using System.Collections.Generic;
using Game.Props.PropsManagers;
using GameData.BaseInfo;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using Pb.Game;
using Google.Protobuf;
using Newtonsoft.Json.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif
namespace AIGame.Base
{
    public class AIParkTestTool : GlobalInstance<AIParkTestTool>
    {
        public AICommonGameConfig aiCommonGameConfig;
        public ParkGameDataRsp parkGameDataRsp;
        private string AICommonGameConfigJsonFilePath = Path.Combine(Application.dataPath, "Scripts/UI/UIPanels/AIGames/AIPark/Test/AICommonGameConfig.json");
        private string ParkGameDataJsonFilePath = Path.Combine(Application.dataPath, "Scripts/UI/UIPanels/AIGames/AIPark/Test/ParkGameDataConfig.json");



        public void CreateAICommonGameData(AICommonGameConfig tempAICommonGameConfig)
        {
            if (tempAICommonGameConfig != null)
            {
                aiCommonGameConfig = tempAICommonGameConfig;
            }
            else
            {
                aiCommonGameConfig ??= new();
                aiCommonGameConfig.plot = "test";
                aiCommonGameConfig.npcData = new();
                aiCommonGameConfig.events = new();
                aiCommonGameConfig.endings = new();
                aiCommonGameConfig.stage = new();
                aiCommonGameConfig.billboard = new();
                aiCommonGameConfig.scene = new();
                aiCommonGameConfig.gamePop = new();
                aiCommonGameConfig.plantColor = "test";
            }
            File.WriteAllText(AICommonGameConfigJsonFilePath, JsonConvert.SerializeObject(aiCommonGameConfig));
        }
        public void ReadAICommonGameData()
        {
            if (!File.Exists(AICommonGameConfigJsonFilePath))
            {
                return;
            }
            string json = File.ReadAllText(AICommonGameConfigJsonFilePath);
            aiCommonGameConfig = JsonConvert.DeserializeObject<AICommonGameConfig>(json);
        }

        public void CreateAIParkGameData(ParkGameDataRsp tempParkGameDataRsp)
        {
            if (tempParkGameDataRsp != null)
            {
                parkGameDataRsp = tempParkGameDataRsp;
            }
            else
            {

                string a = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/UIPanels/AIGames/AIPark/Test/AIParkGameData.json"));

                CreateAICommonGameData(null);
                parkGameDataRsp ??= new();
                parkGameDataRsp.aICommonGameConfig = aiCommonGameConfig;
                // HospitalGameDataRsp rsp=JsonConvert.DeserializeObject<HospitalGameDataRsp>(a);
                // rsp.hospitalScript.ForEach(x=>{
                //     x.plan=new();
                // });
                // parkGameDataRsp.parkScript = JsonConvert.DeserializeObject<List<ParkCharacterScriptData>>(JsonConvert.SerializeObject(rsp.hospitalScript));
                // parkGameDataRsp.hospitalScript = JsonConvert.DeserializeObject<List<ParkCharacterScriptData>>(JsonConvert.SerializeObject(rsp.hospitalScript));
            }
            Debug.LogError(JsonConvert.SerializeObject(parkGameDataRsp));
            File.WriteAllText(ParkGameDataJsonFilePath, JsonConvert.SerializeObject(parkGameDataRsp));

        }
        public void ReadAIParkGameData()
        {
            if (!File.Exists(ParkGameDataJsonFilePath))
            {
                return;
            }
            string json = File.ReadAllText(ParkGameDataJsonFilePath);
            parkGameDataRsp = JsonConvert.DeserializeObject<ParkGameDataRsp>(json);

            var jObject = JObject.Parse(json);
            var parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            parkGameDataRsp.events?.Clear();
            JArray events = jObject["events"] as JArray;
            foreach (var ev in events){
                var ev2 = parser.Parse<AIGameAmusementParkSyncReply.Types.StoryEvent>(ev.ToString());
                parkGameDataRsp.events.Add(ev2);
            }

            // parkGameDataRsp.actions?.Clear();
            // JArray actions = jObject["actions"] as JArray;
            // foreach (var ev in actions){
            //     var ev2 = parser.Parse<History>(ev.ToString());
            //     parkGameDataRsp.actions.Add(ev2);
            // }
            Debug.LogError(JsonConvert.SerializeObject(parkGameDataRsp));
        }

        public void CreateAIParkFirstGameData(ParkGameDataRsp tempParkGameDataRsp)
        {
            if (tempParkGameDataRsp != null)
            {
                parkGameDataRsp = tempParkGameDataRsp;
            }
            else
            {

                string a = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/UIPanels/AIGames/AIPark/Test/AIParkGameData.json"));

                CreateAICommonGameData(null);
                parkGameDataRsp ??= new();
                parkGameDataRsp.events = new();
                // parkGameDataRsp.actions = new();
                parkGameDataRsp.sessionId = "1234567890";
                List<Pb.Game.AIGameAmusementParkSyncReply.Types.StoryEventDetail> details = new();
                details.Add(new()
                {
                    EventName = "测试事件1",
                    EventDesc = "测试事件描述1",
                    EventDuration = 15
                });
                details.Add(new()
                {
                    EventName = "测试事件2",
                    EventDesc = "测试事件描述2",
                    EventDuration = 30
                });
                details.Add(new()
                {
                    EventName = "测试事件3",
                    EventDesc = "测试事件描述3",
                    EventDuration = 45
                });

                var se = new AIGameAmusementParkSyncReply.Types.StoryEvent
                {
                    EventType = (int)ParkEventType.Normal,
                    EventStory = "test"
                };
                se.EventOpts.AddRange(details);
                var discuss = new AmusementAIQuoteLine
                {
                    Speaker = "npc1",
                    Content = "第一幕npc1讨论内容"
                };
                se.EventDiscuss.Add(discuss);

                discuss = new AmusementAIQuoteLine
                {
                    Speaker = "npc2",
                    Content = "第一幕npc2讨论内容"

                };
                se.EventDiscuss.Add(discuss);

                discuss = new AmusementAIQuoteLine
                {
                    Speaker = "npc3",
                    Content = "第一幕npc3讨论内容"
                };
                se.EventDiscuss.Add(discuss);

                parkGameDataRsp.events.Add(se);
                var history = new History
                {
                    Location = 101,
                    Action = 101,

                };
                history.Participants.AddRange(new List<string>() { "101" });
                history.Quotes.Add(new AmusementAIQuoteLine
                {
                    Speaker = "101",
                    Content = "npc1在摩天轮的话"
                });
                // parkGameDataRsp.actions.Add(history);

                history = new History
                {
                    Location = 102,
                    Action = 102,

                };
                history.Participants.AddRange(new List<string>() { "102" });
                history.Quotes.Add(new AmusementAIQuoteLine
                {
                    Speaker = "102",
                    Content = "npc2在秋千的话"
                });
                // parkGameDataRsp.actions.Add(history);

                history = new History
                {
                    Location = 103,
                    Action = 103,
                };
                history.Participants.AddRange(new List<string>() { "103" });
                history.Quotes.Add(new AmusementAIQuoteLine
                {
                    Speaker = "103",
                    Content = "npc3在滑梯的话"
                });
                // parkGameDataRsp.actions.Add(history);
                parkGameDataRsp.aICommonGameConfig = aiCommonGameConfig;
                // HospitalGameDataRsp rsp=JsonConvert.DeserializeObject<HospitalGameDataRsp>(a);
                // parkGameDataRsp.parkScript = JsonConvert.DeserializeObject<List<ParkCharacterScriptData>>(JsonConvert.SerializeObject(rsp.hospitalScript));
                // parkGameDataRsp.hospitalScript = JsonConvert.DeserializeObject<List<ParkCharacterScriptData>>(JsonConvert.SerializeObject(rsp.hospitalScript));
            }

            File.WriteAllText(ParkGameDataJsonFilePath, JsonConvert.SerializeObject(parkGameDataRsp));

        }
        public void ReadAIParkFirstGameData()
        {
            if (!File.Exists(ParkGameDataJsonFilePath))
            {
                return;
            }
            string json = File.ReadAllText(ParkGameDataJsonFilePath);
            parkGameDataRsp = JsonConvert.DeserializeObject<ParkGameDataRsp>(json);
        }


        public void EnterParkGame()
        {
            CreateAICommonGameData(null);
            ReadAICommonGameData();
            // CreateAIParkGameData(null);
            // ReadAIParkGameData();
            CreateAIParkFirstGameData(null);
            ReadAIParkFirstGameData();
            AIParkUtils.Inst.EnterParkGameByMapInfo(AIParkUtils.Inst.GetOfficalMapInfo());
        }

        public void EnterParkGame2()
        {
            ReadAICommonGameData();
            // ReadAIParkGameData();
            AIParkUtils.Inst.EnterParkGameByMapInfo(AIParkUtils.Inst.GetOfficalMapInfo());
        }

// #if UNITY_EDITOR

//         [MenuItem("AIPark/CreateAICommonGameData", false, 100)]
//         public static void Editor_CreateAICommonGameData()
//         {
//             AIParkTestTool.Inst.CreateAICommonGameData(null);
//         }
//         [MenuItem("AIPark/CreateAIParkGameData", false, 100)]
//         public static void Editor_CreateAIParkGameData()
//         {
//             AIParkTestTool.Inst.CreateAIParkGameData(null);
//         }

//         [MenuItem("AIPark/构造第一幕数据", false, 100)]
//         public static void Editor_CreateAIParkFirstGameData()
//         {
//             AIParkTestTool.Inst.CreateAIParkFirstGameData(null);
//         }
        
//           [MenuItem("AIPark/test", false, 100)]
//         public static void Editor_test()
//         {
//             AIParkTestTool.Inst.ReadAIParkGameData();
//         }


// #endif
    }


}