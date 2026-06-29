using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.Base;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GameUI
{
    public class GEParkGroupPanel : BasePanel<GEParkGroupPanel>
    {
        public CButton CloseBtn;

        public CButton Btn_InputGameName;
        public InputField Txt_MapName;

        public Toggle TemTog;
        public Toggle Tog;
        public Transform TogParent;

        public GEParkSelectAdpter SelectAdpter;
        public PullToRefreshBehaviour PullToRefreshBehaviour;

        public GameObject SearchNone;

        private List<Toggle> TogList = new List<Toggle>();

        private KeyBoardInfo _nameKBInfo;

        GameEntrySystemData data => GameEntrySystem.Inst.data;
        public override void OnCreate()
        {
            base.OnCreate();
            SearchNone.gameObject.SetActive(false);
            Tog.gameObject.SetActive(false);
            CloseBtn.onClick.AddListener(CloseSelf);
            Btn_InputGameName.onClick.AddListener(OnBtnInputGameNameClick);

            _nameKBInfo = new KeyBoardInfo
            {
                type = 0,
                placeHolder = "请输入名称",
                inputMode = 0,
                maxLength = 10,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            PullToRefreshBehaviour.OnRefreshWithSlideUp.AddListener(OnPullRefresh);
            SelectAdpter.OnItemSelected = OnItemSelected;
            SelectAdpter.Data = new LazyDataHelper<UgcBaseInfo>(SelectAdpter, GetMapInfo);
        }

        public override void OnShow(params object[] args)
        {
            base.OnShow(args);

            // 初始化OSA
            if (!SelectAdpter.IsInitialized)
            {
                SelectAdpter.Init();
            }

            GameEntrySystem.Inst.MapSectionListReq((rsp) =>
            {
                var list = rsp.list;
                for (int i = 0; i < list.Count; i++)
                {
                    var idx = i;
                    var tog = GameObject.Instantiate(Tog, TogParent).GetComponent<Toggle>();
                    var txt = tog.transform.GetChild(0).GetChild(0).GetComponent<Text>();
                    var txt2 = tog.transform.GetChild(1).GetChild(0).GetComponent<Text>();
                    txt.text = list[i].sectionName;
                    txt2.text = txt.text;
                    tog.onValueChanged.AddListener((succ) => { OnTog(idx, succ); });
                    tog.gameObject.SetActive(true);
                    TogList.Add(tog);

                    TogList[0].isOn = true;
                }
            });
        }

        protected override void OnDestroy()
        {
            GameEntrySystem.Inst.MapSearchReq("",null);
            GameEntrySystem.Inst.MapSectionInfoReq("",null);
            base.OnDestroy();
        }

        private void OnBtnInputGameNameClick()
        {
            _nameKBInfo.defaultText = Txt_MapName.text;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(_nameKBInfo));
        }

        private void OnGetNameFormNative(string value)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            Txt_MapName.text = value;
            if (!string.IsNullOrEmpty(value))
            {
                GameEntrySystem.Inst.MapSearchReq(value, (items) =>
                {
                    TemTog.isOn = true;
                    SelectAdpter.Data.ResetItems(GameEntrySystem.Inst.data.SearchMapInfos.Count);
                    SearchNone.gameObject.SetActive(GameEntrySystem.Inst.data.SearchMapInfos.Count <= 0);
                });
            }
        }

        private void OnTog(int idx,bool bo) {
            if (bo)
            {
                SearchNone.gameObject.SetActive(false);
                Txt_MapName.text = "";
                GameEntrySystem.Inst.MapSearchReq("", null);
                GameEntrySystem.Inst.MapSectionInfoReq("", null);
                GameEntrySystem.Inst.MapSectionInfoReq(GameEntrySystem.Inst.data.SectionListRsp.list[idx].sectionId, (items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(GameEntrySystem.Inst.data.SectionMapInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
        }

        private UgcBaseInfo GetMapInfo(int index)
        {
            if (!string.IsNullOrEmpty(data.SearchStr))
            {
                return data.SearchMapInfos[index].ugcInfo;
            }
            else
            {
                return data.SectionMapInfos[index].ugcInfo;
            }
        }

        private void OnItemSelected(UgcBaseInfo mapInfo)
        {
            GameEntrySystem.Inst.OpenParkDetailPanel(mapInfo.id);
        }

        private void OnPullRefresh()
        {
            if (!string.IsNullOrEmpty(data.SearchStr))
            {
                GameEntrySystem.Inst.MapSearchReq(data.SearchStr,(items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.SearchMapInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
            else
            {
                GameEntrySystem.Inst.MapSectionInfoReq(data.SectionId, (items) =>
                {
                    PullToRefreshBehaviour.HideGizmo();
                    SelectAdpter.Data.ResetItems(data.SectionMapInfos.Count);
                    SelectAdpter.Refresh();
                });
            }
        }
    }
}