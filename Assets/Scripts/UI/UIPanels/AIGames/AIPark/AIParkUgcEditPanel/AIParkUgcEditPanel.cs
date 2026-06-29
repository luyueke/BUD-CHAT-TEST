using System;
using System.Collections.Generic;
using GameData.BaseInfo;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    [Serializable]
    public class AIParkToggle {
        public Toggle tog;
        public GameObject obj;

        public Toggle subTog;
    }
    public class AIParkUgcEditPanel : BasePanel<AIParkUgcEditPanel>
    {
        public CButton Btn_Close;
        public LoadingButton Btn_Save;
        public LoadingButton Btn_SaveAndPlay;
        public GameObject InputMask;

        public AIParkUgcEditHelpGroup HelpGroup;

        public List<AIParkToggle> itemTogs = new List<AIParkToggle>();

        private List<SettingContentBase> settingBase = new List<SettingContentBase>();
        private Action _onSaveAndCloseAct;

        //数据层
        private EditType _editType = EditType.Create;
        private MapInfo _mapInfo;

        private AIParkToggle curTog;
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _editType = (EditType)args[0];
            _mapInfo = _editType == EditType.Create ? AIParkUtils.Inst.GetDefMapInfo() : (MapInfo)args[1];

            Btn_Close.onClick.AddListener(CloseSelf);
            Btn_Save.onClick.AddListener(OnBtnSaveClick);
            Btn_SaveAndPlay.onClick.AddListener(OnBtnSaveAndPlayClick);

            settingBase = GameObjectEx.GetAllChildren<SettingContentBase>(gameObject,false,true);

            if (_editType == EditType.Create)
            {
                SaveMapReq(_mapInfo, (content) =>
                {
                    UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                    _mapInfo = rspData.mapInfo;

                    _editType = EditType.Edit;
                    foreach (var item in settingBase)
                    {
                        item.InitData(_mapInfo, _editType);
                    }
                });
            }
            else
            {
                foreach (var item in settingBase)
                {
                    item.InitData(_mapInfo, _editType);
                }
            }


            foreach (var item in itemTogs)
            {
                item.tog.onValueChanged.AddListener((isOn) =>
                {
                    if (curTog == item)
                    {
                        return;
                    }
                    OnToggleValueChanged(item.tog.gameObject, isOn);
                    if (isOn)
                    {
                        foreach (var item2 in itemTogs)
                        {
                            if (item2.obj != null && item2.obj.name != "Group")
                            {
                                item2.obj.gameObject.SetActive(false);
                            }
                        }
                        if (item.obj != null) item.obj.gameObject.SetActive(true);
                        if (item.subTog != null)
                        {
                            if (item.subTog.isOn)
                            {
                                item.subTog.onValueChanged.Invoke(true);
                            }
                            else
                            {
                                item.subTog.isOn = true;
                            }
                        }
                    }
                    else
                    {
                        if (item.obj != null) item.obj.gameObject.SetActive(false);
                    }
                });
            }

            OnToggleValueChanged(itemTogs[0].tog.gameObject,true);
        }

        public void OnToggleValueChanged(GameObject obj, bool isOn)
        {
            var togSwitch = obj.GetComponent<CommonToggleSwitch>();
            togSwitch.SetSelectState(isOn);
        }

        private void OnBtnSaveClick()
        {
            Btn_Save.SetLoadingVisible(true);
            SaveMapReq((content) =>
            {
                Btn_Save.SetLoadingVisible(false);
                this._onSaveAndCloseAct?.Invoke();
                CloseSelf();
            }, (error) =>
            {
                Btn_Save.SetLoadingVisible(false);
            });
        }

        private void OnBtnSaveAndPlayClick()
        {
            Btn_SaveAndPlay.SetLoadingVisible(true);
            SaveMapReq((content) =>
            {
                Btn_SaveAndPlay.SetLoadingVisible(false);
                this._onSaveAndCloseAct?.Invoke();
                CloseSelf();
                AIParkUtils.Inst.EnterUgcParkGame(this._mapInfo.id);
                //AIParkUtils.Inst.EnterParkGameByMapInfo(this._mapInfo);
            }, (error) =>
            {
                Btn_SaveAndPlay.SetLoadingVisible(false);
            });
        }

        private void SaveMapReq(Action<string> onSuccess = null, Action<string> onFail = null)
        {
            foreach (var item in settingBase)
            {
                item.SaveData();
            }

            if (!CheckUgcHospitalDataIsLegal(_mapInfo))
            {
                onFail?.Invoke("");
                return;
            }

            //本地构建
            //AIParkTestTool.Inst.CreateAICommonGameData(_mapInfo.gameSetting.aICommonGameConfig);
            //return;

            //1.屏蔽输入
            InputMask.SetActive(true);

            _mapInfo.npcIds = new List<string>() { };
            foreach (var item in _mapInfo.gameSetting.AICommonGameConfig.npcData) {
                if (item != null)
                {
                    _mapInfo.npcIds.Add(item.id);
                }
            }

            var req = new SetMapInfoReq
            {
                mapInfo = _mapInfo,
                setType = _editType == EditType.Create ? (int)SetType.Create : (int)SetType.Edit,

            };
            req.mapInfo.gameType = 1;
            req.mapInfo.gameSetting.aiGameId = (int)PGCGameType.AIPark;
            req.mapInfo.gameSetting.SerializeData();
            var reqParam = JsonConvert.SerializeObject(req);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST, reqParam, (content) =>
            {
                InputMask.SetActive(false);
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                onSuccess?.Invoke(content);
            }, (error) =>
            {
                InputMask.SetActive(false);
                onFail?.Invoke(error);
            });
        }

        private void SaveMapReq(MapInfo mapInfo, Action<string> onSuccess = null, Action<string> onFail = null)
        {
            //1.屏蔽输入
            InputMask.SetActive(true);

            var req = new SetMapInfoReq
            {
                mapInfo = mapInfo,
                setType = (int)SetType.Create
            };
            req.mapInfo.gameSetting.aiGameId = (int)PGCGameType.AIPark;
            req.mapInfo.gameSetting.SerializeData();
            var reqParam = JsonConvert.SerializeObject(req);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST, reqParam, (content) =>
            {
                InputMask.SetActive(false);
                MessageHelper.Broadcast(DraftMessage.RefreshDraft);
                onSuccess?.Invoke(content);
            }, (error) =>
            {
                InputMask.SetActive(false);
                onFail?.Invoke(error);
            });
        }

        #region 保存数据校验

        public static bool CheckUgcHospitalDataIsLegal(MapInfo mapInfo)
        {
            var isLegal = true;

            if (mapInfo == null)
            {
                LoggerUtils.LogError("CheckUgcHospitalDataIsLegal mapInfo is null");
                return false;
            }

            var config = mapInfo.gameSetting.AICommonGameConfig;
            if (config.npcData == null || config.npcData.Count <= 00)
            {
                TipPanel.ShowToast("试玩前请添加至少1个NPC");
                isLegal = false;
                return isLegal;
            }

            if (string.IsNullOrEmpty(config.plot))
            {
                TipPanel.ShowToast("试玩前请填写故事背景");
                isLegal = false;
                return isLegal;
            }

            if (config.events == null || config.events.Count <= 0)
            {
                TipPanel.ShowToast("试玩前请填写至少1个事件");
                isLegal = false;
                return isLegal;
            }
            else
            {
                foreach (var item in config.events)
                {
                    if (item.limitDuration < 30)
                    {
                        TipPanel.ShowToast("试玩前请填写事件时长大于30秒");
                        isLegal = false;
                        return isLegal;
                    }
                    if (item.limitDuration >999)
                    {
                        TipPanel.ShowToast("试玩前请填写事件时长小于999秒");
                        isLegal = false;
                        return isLegal;
                    }
                    if (item.type == 1 && item.answers.Count == 1)
                    {
                        TipPanel.ShowToast("试玩前请填写选择事件至少添加2个选项");
                        isLegal = false;
                        return isLegal;
                    }
                }
            }

            if (config.endings == null || config.endings.Count <= 0)
            {
                TipPanel.ShowToast("试玩前请填写至少1个普通结局");
                isLegal = false;
                return isLegal;
            }
            else
            {
                bool toast = true;
                foreach (var item in config.endings)
                {
                    if (item.type == 0)
                    {
                        toast = false;
                        break;
                    }
                }
                if (toast)
                {
                    TipPanel.ShowToast("试玩前请填写至少1个普通结局");
                    isLegal = false;
                    return isLegal;
                }
            }

            if (config.stage == null || config.stage.musicalInstruments == null)
            {
                TipPanel.ShowToast("试玩前请在舞台添加至少1个乐器");
                isLegal = false;
                return isLegal;
            }
            else
            {
                bool toast = true;
                foreach (var item in config.stage.musicalInstruments)
                {
                    if (item != null)
                    {
                        toast = false;
                        break;
                    }
                }
                if (toast)
                {
                    TipPanel.ShowToast("试玩前请在舞台添加至少1个乐器");
                    isLegal = false;
                    return isLegal;
                }
            }

            return isLegal;
        }

        #endregion
    }
}
