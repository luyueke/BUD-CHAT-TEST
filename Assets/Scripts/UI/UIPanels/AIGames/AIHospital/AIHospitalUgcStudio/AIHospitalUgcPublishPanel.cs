using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using UI.BaseWidgets;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;

namespace AIGame.Base
{
    public class AIHospitalUgcPublishPanel : BasePanel<AIHospitalUgcPublishPanel>
    {
        public Transform BG;
        public RemoteImageBehaviour RM_Cover;

        // 名称输入相关
        public CButton Btn_InputName;
        public GameObject emptyNameObj;
        public SuperTextMesh nameText;
        public Text nameCountText; // 名称字数限制显示
        private KeyBoardInfo _nameKBInfo;
        private int NameLimitCount = 25;

        // 描述输入相关
        public CButton Btn_InputDesc;
        public GameObject emptyDescObj;
        public SuperTextMesh descriptionText;
        public Text descCountText; // 描述字数限制显示
        private KeyBoardInfo _descKBInfo;
        private int DescLimitCount = 320;

        // 发布按钮
        public LoadingButton Btn_Publish;
        public LoadingButton Btn_Update;
        public CButton Btn_Close;

        //屏蔽输入的遮罩
        public GameObject InputMask;

        // 地图信息
        private MapInfo _curMapInfo;

        private Action<string,bool> _updateMapAction;

        public override void OnCreate()
        {
            base.OnCreate();
            InitBG();
            InitUIComponent();
        }
        
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            _curMapInfo = (MapInfo)args[0];
            UpdateUI();
            RM_Cover.Load(_curMapInfo.cover);
            if (args != null && args.Length > 1 && args[1] is bool showUpdateBtn)
            {
                Btn_Publish.gameObject.SetActive(!showUpdateBtn);
                Btn_Update.gameObject.SetActive(showUpdateBtn);
            }
        }


        private void InitBG()
        {
            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var itemObj = Loader
                .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
                .Instantiate(BG);
            var item = itemObj.GetComponent<ActivityCenterBgItem>();
            item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "S9BgElement1_green", "S9BgElement2_green", "S9BgElement3_green"
            });
            item.gameObject.SetActive(true);
        }

        private void InitUIComponent()
        {
            // 初始化按钮监听
            Btn_InputName.onClick.AddListener(OnBtnInputNameClick);
            Btn_InputDesc.onClick.AddListener(OnBtnInputDescClick);
            Btn_Publish.onClick.AddListener(OnBtnPublishClick);
            Btn_Update.onClick.AddListener(OnBtnUpdateClick);
            Btn_Close.onClick.AddListener(OnBtnCloseClick);

            // 初始化键盘信息
            _nameKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = NameLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            _descKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = DescLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
        }
        
        private void UpdateUI()
        {
            UpdateNameUI();
            UpdateDescriptionUI();

        }

        #region 名称相关

        private void OnBtnInputNameClick()
        {
            // 设置默认文本为当前名称
            _nameKBInfo.defaultText = _curMapInfo.name;

            // 注册回调并显示键盘
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
        }

        private void OnGetNameFromNative(string name)
        {
            // 移除回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            // 更新名称
            _curMapInfo.name = name;

            // 更新UI
            UpdateNameUI();
        }

        private void UpdateNameUI()
        {
            if (string.IsNullOrEmpty(_curMapInfo.name))
            {
                // 显示空名称提示
                emptyNameObj.SetActive(true);
                if (nameText != null)
                {
                    nameText.gameObject.SetActive(false);
                }

                // 更新字数显示
                if (nameCountText != null)
                {
                    nameCountText.text = $"0/{NameLimitCount}";
                }
            }
            else
            {
                // 显示名称内容
                if (nameText != null)
                {
                    nameText.text = _curMapInfo.name;
                    nameText.gameObject.SetActive(true);
                }

                emptyNameObj.SetActive(false);

                // 更新字数显示
                if (nameCountText != null)
                {
                    int currentLength = _curMapInfo.name.Length;
                    nameCountText.text = $"{currentLength}/{NameLimitCount}";
                }
            }
        }

        #endregion

        #region 描述相关

        private void OnBtnInputDescClick()
        {
            // 设置默认文本为当前描述
            _descKBInfo.defaultText = _curMapInfo.desc;

            // 注册回调并显示键盘
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescriptionFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_descKBInfo));
        }

        private void OnGetDescriptionFromNative(string description)
        {
            // 移除回调
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);

            // 更新描述
            _curMapInfo.desc = description;

            // 更新UI
            UpdateDescriptionUI();
        }

        private void UpdateDescriptionUI()
        {
            if (string.IsNullOrEmpty(_curMapInfo.desc))
            {
                // 显示空描述提示
                emptyDescObj.SetActive(true);
                if (descriptionText != null)
                {
                    descriptionText.gameObject.SetActive(false);
                }

                // 更新字数显示
                if (descCountText != null)
                {
                    descCountText.text = $"0/{DescLimitCount}";
                }
            }
            else
            {
                // 显示描述内容
                if (descriptionText != null)
                {
                    descriptionText.text = _curMapInfo.desc;
                    descriptionText.gameObject.SetActive(true);
                }

                emptyDescObj.SetActive(false);

                // 更新字数显示
                if (descCountText != null)
                {
                    int currentLength = _curMapInfo.desc.Length;
                    descCountText.text = $"{currentLength}/{DescLimitCount}";
                }
            }
        }

        #endregion

        private void OnBtnPublishClick()
        {
            Btn_Publish.SetLoadingVisible(true);
            // 发布逻辑，可以在这里添加发布前的验证
            if (string.IsNullOrEmpty(_curMapInfo.name))
            {
                Btn_Publish.SetLoadingVisible(false);
                // 提示用户输入名称
                TipPanel.ShowToast(LocalizationManager.Inst.GetLocalizedText("请输入作品名称"));
                return;
            }

            //屏蔽输入
            InputMask.SetActive(true);

            // 这里可以添加发布的具体逻辑
            var req = new SetMapInfoReq
            {
                mapInfo = _curMapInfo,
                setType = (int)SetType.Publish
            };
            var reqParam = JsonConvert.SerializeObject(req);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST, reqParam, (content) =>
            {
                InputMask.SetActive(false);
                TipPanel.ShowToast("发布地图成功");
                // 发布成功后关闭面板
                CloseSelf();
                Btn_Publish.SetLoadingVisible(false);
            }, (error) =>
            {
                Btn_Publish.SetLoadingVisible(false);
                InputMask.SetActive(false);
            });
        }

        private void OnBtnUpdateClick()
        {
            SendUpdateHttpRequest();
        }

        public void SendUpdateHttpRequest()
        {
            if (Btn_Update.IsLoading)
            {
                return;
            }
            Btn_Update.ShowLoading();
            var req = new SetMapInfoReq
            {
                mapInfo = _curMapInfo,
                setType = (int)SetType.Update,
                overwriteId = _curMapInfo.id
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setMap, HttpMethod.POST, JsonConvert.SerializeObject(req), OnUpdateSuccess, OnUpdateFail);
        }

        private void OnUpdateSuccess(string msg)
        {
            TipPanel.ShowToast("更新地图成功");
            Btn_Update.HideLoading();
            CloseSelf();
            _updateMapAction?.Invoke(msg,true);
        }

        private void OnUpdateFail(string failMsg)
        {
            Btn_Update.HideLoading();
            CloseSelf();
            _updateMapAction?.Invoke(failMsg,false);
        }

        public void SetUpdateMapAction(Action<string, bool> updateMapAction)
        {
            _updateMapAction = updateMapAction;
        }

        private void OnBtnCloseClick()
        {
            CloseSelf();
        }
    }
}
