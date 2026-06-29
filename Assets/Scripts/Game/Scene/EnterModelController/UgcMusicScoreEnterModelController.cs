using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Basic.Extensions;
using Basic.Utils;
using Game.Base;
using Game.Config;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using SceneController.Attribute;
using UGCAsset;
using UIAgent;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scene.EnterModelController {
    [EnterModel(EnterGameModel.UgcMusicScoreEmpty, EnterGameModel.UgcMusicScoreContinueEdit)]
    public class UgcMusicScoreEnterModelController : BaseEnterModelController {
        public MusicScoreInfo musicScoreInfo;
        public override void Start(UgcBaseInfo baseInfo) {
            musicScoreInfo = baseInfo as MusicScoreInfo;
            base.Start(baseInfo);
            StartMusicScore();
        }
        private void StartMusicScore() {
            
            if (string.IsNullOrEmpty(musicScoreInfo.id)) {
                MusicScoreAssetManager.Inst.CreateMusicScoreInServer(musicScoreInfo, (success) => {
                    if (success)
                    {
                        UIAgentManager.Inst.OpenPanel(PanelId.UGCMusicScoreEditPanel, musicScoreInfo);
                        UIAgentManager.Inst.ClosePanel(WindowId.GameHallWindow,PanelId.MusicScoreEditInfoPanel);
                    }
                    else
                    {
                        GameController.ExitGame("创建草稿失败");
                    }
                });
            } else {
                UIAgentManager.Inst.OpenPanel(PanelId.UGCMusicScoreEditPanel, musicScoreInfo);
            }
        }


    }
}
