using GameData.BaseInfo;
using System.Collections.Generic;

namespace AIGame.Base
{
    public class AIParkUgcEditStage : SettingContentBase
    {
        public List<AIParkBgmSelectBtn> BgmSelectBtns;

        public List<AIParkUgcEditStageMusical> MusicalLs;

        public List<AIParkPicUpLoad> PicUpLoads;

        public AIParkUgcEditMusicalGroup musicalGroup;

        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            musicalGroup.Init(this);

            if (curMapInfo.gameSetting.AICommonGameConfig.stage == null)
            {
                curMapInfo.gameSetting.AICommonGameConfig.stage = new AICommonGameConfig_Stage();
            }

            var CurStage = curMapInfo.gameSetting.AICommonGameConfig.stage;

            var bo = true;
            foreach (var item in CurStage.musicalInstruments)
            {
                if (item != null) {
                    bo = false;
                }
            }
            if (bo) 
            {
                CurStage.DefaultInstrments();
            }


            for (int i = 0; i < PicUpLoads.Count; i++)
            {
                var index = i;
                PicUpLoads[i].InitData(OnSelect, index, OnDel,null);
                PicUpLoads[i].SetData(CurStage.backgroundUrls[i]);
            }

            for (int i = 0; i < MusicalLs.Count; i++)
            {
                MusicalLs[i].Init(this);
                MusicalLs[i].SetData(CurStage.musicalInstruments[i]);
            }

            InitBgmContent();
        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();

            //BgmBtn.onClick.AddListener(OnBgmBtn);
        }

        public override void SaveData()
        {
            base.SaveData();
            var stage = curMapInfo.gameSetting.AICommonGameConfig.stage;
            stage.musicUrls = new List<AICommonGameStageMusic>();
            foreach (var item in BgmSelectBtns)
            {
                if (item.GetSelectState())
                {
                    if (item._curType == AIHospitalBgmType.OfficalBgm)
                    {
                        stage.musicUrls.Add(new AICommonGameStageMusic() { isLocal = true, name = item.OfficalName,musicName = item.MusciName });
                    }
                    else if (item._curType == AIHospitalBgmType.UploadedUgcBgm)
                    {
                        stage.musicUrls.Add(new AICommonGameStageMusic() { isLocal = false, url = item.bgmUrl });
                    }
                }
            }
            for (int i = 0; i < MusicalLs.Count; i++)
            {
                stage.musicalInstruments[i] = MusicalLs[i].Config_Musical;
            }
            for (int i = 0; i < PicUpLoads.Count; i++)
            {
                stage.backgroundUrls[i] = PicUpLoads[i].curUrl;
            }
        }

        private void InitBgmContent()
        {
            var musicUrls = curMapInfo.gameSetting.AICommonGameConfig.stage.musicUrls;

            BgmSelectBtns.ForEach(x =>
            {
                x.SetSelectState(false);
                x.SetData("", "", OnSyncBgmUrl);
                if (x._curType == AIHospitalBgmType.UploadedUgcBgm)
                {
                    x.gameObject.SetActive(false);
                }
            });

            for (int i = 0; i < musicUrls.Count; i++)
            {
                var item = musicUrls[i];
                if (item.isLocal)
                {
                    foreach (var item1 in BgmSelectBtns)
                    {
                        if (item1._curType == AIHospitalBgmType.OfficalBgm && !string.IsNullOrEmpty(item1.OfficalName) && item1.OfficalName == item.name)
                        {
                            item1.SetData(item.name, "", OnSyncBgmUrl);
                            item1.SetSelectState(true);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var item1 in BgmSelectBtns)
                    {
                        if (item1._curType == AIHospitalBgmType.UploadedUgcBgm && string.IsNullOrEmpty(item1.bgmUrl))
                        {
                            item1.SetData(item.name, item.url, OnSyncBgmUrl);
                            item1.gameObject.SetActive(true);
                            item1.SetSelectState(true);
                            break;
                        }
                    }
                }
            }
        }

        private void OnSyncBgmUrl(string bgName, string bgMusicUrl, AIParkBgmSelectBtn selectBtn)
        {
            if (selectBtn._curType == AIHospitalBgmType.OfficalBgm)
            {
                selectBtn.SetSelectState(!selectBtn.GetSelectState());
            }
            else if(selectBtn._curType == AIHospitalBgmType.UploadUgcBgm)
            {
                BgmSelectBtns.ForEach(x =>
                {
                    if (x._curType == AIHospitalBgmType.UploadedUgcBgm)
                    {
                        x.gameObject.SetActive(true);
                        x.SetSelectState(true);
                        x.SetData(bgName, bgMusicUrl, OnSyncBgmUrl);
                    }
                });
            }
        }
        /*
        private void OnBgmBtn() {
            OnBtnUploadClick();
        }
        /*
        private void OnBtnUploadClick()
        {
            bool isCancel = false;
            Action onCancel = () =>
            {
                isCancel = true;
            };
            var panel = UIManager.Inst.OpenPanel<BgMusicUploadingPanel>(PanelId.BgMusicUploadingPanel, onCancel);
            panel.SetTopColor("#68CCBE");
            AlbumUtils.Inst.UploadMusic(120, (remoteUrl) =>
            {
                if (isCancel)
                {
                    return;
                }
                if (!string.IsNullOrEmpty(remoteUrl))
                {
                    var bgName = "提取音乐 " + DataUtil.GetUtcTimeStampAsSpan();
                    for (int i = 0; i < BgmItemLs.Count; i++)
                    {
                        if (string.IsNullOrEmpty(BgmItemLs[i].Str))
                        {
                            BgmItemLs[i].gameObject.SetActive(true);
                            var CurBgmItem = BgmItemLs[i];
                            CurBgmItem.SetData(remoteUrl);
                            break;
                        }
                    }
                }
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
            }, err =>
            {
                UIManager.Inst.ClosePanel(PanelId.BgMusicUploadingPanel);
            });
        }
        */
        public bool CanSelect(string Id)
        {
            foreach (var item in MusicalLs)
            {
                if (item.Config_Musical != null && item.Config_Musical.id == Id)
                {
                    return false;
                }
            }
            return true;
        }

        public void SetMusical(AICommonGameConfig_Musical config_Musical)
        {
            foreach (var item in MusicalLs)
            {
                if (item.Config_Musical == null)
                {
                    item.SetData(config_Musical);
                    return;
                }
            }
        }

        private void OnSelect(string url, int idx)
        {
            var CurStage = curMapInfo.gameSetting.AICommonGameConfig.stage;
            CurStage.backgroundUrls[idx] = url;
        }

        private void OnDel(string url, int idx)
        {
            var CurStage = curMapInfo.gameSetting.AICommonGameConfig.stage;
            CurStage.backgroundUrls[idx] = "";
        }
    }
}