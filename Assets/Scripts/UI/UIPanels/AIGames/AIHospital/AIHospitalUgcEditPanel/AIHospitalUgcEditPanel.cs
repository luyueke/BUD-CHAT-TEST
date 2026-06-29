using System;
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
    public enum EditType
    {
        Create,
        Edit,
    }

    public enum ESettingType
    {
        Basic,
        Npc,
        Scene,
    }

    public class AIHospitalUgcEditPanel : BasePanel<AIHospitalUgcEditPanel>
    {
        public CButton Btn_Close;
        public LoadingButton Btn_Save;
        public LoadingButton Btn_SaveAndPlay;
        public GameObject InputMask;
        public BasicSettingContent basicSettingContent;
        public NpcSettingContent npcSettingContent;
        public StorySettingContent storySettingContent;
        public SceneSettingContent sceneSettingContent;
        public PictureSettingContent PictureSettingContent;

        public CButton Btn_Drop;
        public ScrollRect ScRect;

        public GameObject _basicScroll;
        public GameObject _npcScroll;
        public GameObject _sceneScroll;

        public Toggle _basicTog;

        public Toggle _npcTog;

        public Toggle _sceneTog;    

        private Action _onSaveAndCloseAct;
        
        //数据层
        private EditType _editType = EditType.Create;
        private MapInfo _mapInfo;

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            _editType = (EditType)args[0];
            _mapInfo = _editType == EditType.Create ? AIHospitalUtils.Inst.GetDefMapInfo() : (MapInfo)args[1];
            
            Btn_Close.onClick.AddListener(CloseSelf);
            Btn_Save.onClick.AddListener(OnBtnSaveClick);
            Btn_SaveAndPlay.onClick.AddListener(OnBtnSaveAndPlayClick);
            //Btn_Drop.onClick.AddListener(OnBtnDropClick);
            
            // 添加滚动监听
            //ScRect.onValueChanged.AddListener(OnScrollValueChanged);
            
            if (_editType == EditType.Create)
            {
                SaveMapReq(_mapInfo,(content) =>
                {
                    UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                    _mapInfo = rspData.mapInfo;

                    _editType = EditType.Edit;
                    basicSettingContent.InitData(_mapInfo, _editType);
                    npcSettingContent.InitData(_mapInfo, _editType);
                    storySettingContent.InitData(_mapInfo, _editType);
                    sceneSettingContent.InitData(_mapInfo, _editType);
                    PictureSettingContent.InitData(_mapInfo, _editType);
                });
            }
            else
            {
                basicSettingContent.InitData(_mapInfo, _editType);
                npcSettingContent.InitData(_mapInfo, _editType);
                storySettingContent.InitData(_mapInfo, _editType);
                sceneSettingContent.InitData(_mapInfo, _editType);
                PictureSettingContent.InitData(_mapInfo, _editType);

            }
            InitTab();
        }

        private void InitTab()
        {
            _basicTog.onValueChanged.AddListener((isOn)=>
            {
                OnToggleValueChanged(_basicTog.gameObject,isOn);
                if (isOn)
                {
                    SelectTabItem(ESettingType.Basic);
                }
            });
            _npcTog.onValueChanged.AddListener((isOn)=>
            {
                OnToggleValueChanged(_npcTog.gameObject,isOn);
                if (isOn)
                {
                    SelectTabItem(ESettingType.Npc);
                }
            });
            _sceneTog.onValueChanged.AddListener((isOn)=>
            {
                OnToggleValueChanged(_sceneTog.gameObject,isOn);
                if (isOn)
                {
                    SelectTabItem(ESettingType.Scene);
                }
            });
            _basicTog.onValueChanged.Invoke(true);
        }

        public void OnToggleValueChanged(GameObject obj, bool isOn)
        {
            var togSwitch = obj.GetComponent<CommonToggleSwitch>();
            togSwitch.SetSelectState(isOn);
        }

        private void SelectTabItem(ESettingType type)
        {
            _basicScroll.SetActive(type==ESettingType.Basic);
            _npcScroll.SetActive(type == ESettingType.Npc);
            _sceneScroll.SetActive(type == ESettingType.Scene);

            if (type==ESettingType.Scene)
            {
                //sceneSettingContent.InitData(_mapInfo, _editType);
            }
        }

        public void SetSaveSuccessAction(Action act)
        {
            this._onSaveAndCloseAct = act;
        }
        
        private void OnBtnDropClick()
        {
            // 滑动到底部
            ScRect.verticalNormalizedPosition = 0f;
            // 隐藏按钮
            Btn_Drop.gameObject.SetActive(false);
        }
        
        private void OnScrollValueChanged(Vector2 pos)
        {
            // 检查是否滚动到顶部，显示Drop按钮
            if (ScRect.verticalNormalizedPosition >= 0.99f)
            {
                Btn_Drop.gameObject.SetActive(true);
            }
            // 检查是否滚动到底部，隐藏Drop按钮
            else if (ScRect.verticalNormalizedPosition <= 0.01f)
            {
                Btn_Drop.gameObject.SetActive(false);
            }
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
                AIHospitalUtils.Inst.EnterUgcHospitalGame(this._mapInfo.id);
            }, (error) =>
            {
                Btn_SaveAndPlay.SetLoadingVisible(false);
            });
        }

        private void SaveMapReq(Action<string> onSuccess = null, Action<string> onFail = null)
        {
            basicSettingContent.SaveData();
            npcSettingContent.SaveData();
            storySettingContent.SaveData();
            sceneSettingContent.SaveData();
            PictureSettingContent.SaveData();

            
            if (!CheckUgcHospitalDataIsLegal(_mapInfo))
            {
                onFail?.Invoke("");
                return;
            }
            
            //1.屏蔽输入
            InputMask.SetActive(true);
            
            var req = new SetMapInfoReq
            {
                mapInfo = _mapInfo,
                setType = _editType == EditType.Create ? (int)SetType.Create : (int)SetType.Edit,
            };
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

        private bool CheckUgcHospitalDataIsLegal(MapInfo mapInfo)
        {
            var isLegal = true;

            if (mapInfo == null)
            {
                LoggerUtils.LogError("CheckUgcHospitalDataIsLegal mapInfo is null");
                return false;
            }
            
            //后段校验
            //1.至少需要一个监管者
            // var aiGameConfig = mapInfo.gameSetting.aIGameConfig;
            // var provostNpc = aiGameConfig?.hospitalNPCs?.Find(x => x.role == (int)HospitalNPCType.Provost);
            // if (provostNpc == null)
            // {
            //     TipPanel.ShowToast("至少需要一个监管者");
            //     isLegal = false;
            // }
            
            // //2.至少需要一个逃亡者
            // var runagateNpc = aiGameConfig?.hospitalNPCs?.Find(x => x.role == (int)HospitalNPCType.Runagate);
            // if (runagateNpc == null)
            // {
            //     TipPanel.ShowToast("至少需要一个逃亡者");
            //     isLegal = false;
            // }
            
            return isLegal;
        }

        #endregion
    }
}
