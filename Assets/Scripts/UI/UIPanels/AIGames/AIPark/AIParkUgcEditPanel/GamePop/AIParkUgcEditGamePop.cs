using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML.Model.Tag;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditGamePop : SettingContentBase
    {
        public AIParkSelectColor BgColor1;

        public AIParkSelectColor BgColor2;

        public AIParkSelectColor FontColor;

        public Transform ColorBgParent1;

        public Transform ColorBgParent2;

        public Transform ColorFontParent;

        public Transform PasterParent;

        public AIParkUgcInputAnim InputAnim;

        public List<Image> UgcImage = new();

        public List<Image> UgcImage2 = new();

        public List<Text> Text = new();

        public List<Text> Text2 = new();

        private List<AIParkSelectColor> ImageBgs1 = new List<AIParkSelectColor>();

        private List<AIParkSelectColor> ImageBgs2 = new List<AIParkSelectColor>();

        private List<AIParkSelectColor> ImageFonts = new List<AIParkSelectColor>();

        private List<AIParkUgcEditPaster> PasterLs = new List<AIParkUgcEditPaster>();

        public RemoteImageBehaviour PasterPreview;
        public RectTransform RectPreview => PasterPreview.transform as RectTransform;
        public CButton DelBtn;
        public CButton ClickBtn;
        public Transform PitchCover;

        private AICommonGamePaster CurUrl;
        private int Idx;


        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            if (curMapInfo.gameSetting.AICommonGameConfig.gamePop == null)
            {
                curMapInfo.gameSetting.AICommonGameConfig.gamePop = new AICommonGameConfig_GamePop();
            }

            var ls = Es.DataTables.GetColorDataConfigList();

            foreach (var item in ls)
            {
                var tem = GameObject.Instantiate(BgColor1, ColorBgParent1).GetComponent<AIParkSelectColor>();
                tem.InitData(item.Color, OnSelectBg1);
                ImageBgs1.Add(tem);
            }
            BgColor1.gameObject.SetActive(false);

            foreach (var item in ls)
            {
                var tem = GameObject.Instantiate(BgColor2, ColorBgParent2).GetComponent<AIParkSelectColor>();
                tem.InitData(item.Color, OnSelectBg2);
                ImageBgs2.Add(tem);
            }
            BgColor2.gameObject.SetActive(false);

            foreach (var item in ls)
            {
                var tem = GameObject.Instantiate(FontColor, ColorFontParent).GetComponent<AIParkSelectColor>();
                tem.InitData(item.Color, OnSelectFont);
                ImageFonts.Add(tem);
            }
            FontColor.gameObject.SetActive(false);

            var count = PasterParent.transform.childCount;
            var paster = curMapInfo.gameSetting.AICommonGameConfig.gamePop.pasterUrls;
            for (int i = 0; i < count; i++)
            {
                var idx = i;
                var v = PasterParent.GetChild(i).GetComponent<AIParkUgcEditPaster>();
                v.InitData(OnSelectPic, idx, OnUnSelectBtn, OnClickBtn);
                PasterLs.Add(v);

                if (i < paster.Count)
                {
                    PasterLs[i].SetData(paster[i]);
                }
                else
                {
                    PasterLs[i].SetData(null);
                }
                PasterLs[i].gameObject.SetActive(false);
            }

            for (int i = 0; i < PasterLs.Count; i++)
            {
                PasterLs[i].gameObject.SetActive(true);
                if (PasterLs[i].Config == null)
                {
                    break;
                }
            }
            PasterPreview.ResetRawImage();
            PasterPreview.gameObject.SetActive(false);
        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();

            DelBtn.onClick.AddListener(OnDelBtn);
            ClickBtn.onClick.AddListener(OnClickBtn);
        }

        public override void SaveData()
        {
            base.SaveData();
            if (CurUrl != null)
            {
                OnClickBtn(CurUrl, Idx);
            }
            curMapInfo.gameSetting.AICommonGameConfig.gamePop.pasterUrls = new();
            foreach (var v in PasterLs)
            {
                if (v.Config != null)
                {
                    curMapInfo.gameSetting.AICommonGameConfig.gamePop.pasterUrls.Add(v.Config);
                }
            }
        }


        private void Start()
        {
            var ls = Es.DataTables.GetColorDataConfigList();
            var index = 0;
            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.gamePop.bgColor1);
            if (index >= 0)
            {
                ImageBgs1[index].Toggle.isOn = true;
            }


            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.gamePop.bgColor2);
            if (index >= 0)
            {
                ImageBgs2[index].Toggle.isOn = true;
            }


            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.gamePop.fontColor);
            if (index >= 0)
            {
                ImageFonts[index].Toggle.isOn = true;
            }

            PitchCover.gameObject.SetActive(false);

            RefreshUgc();
        }
        private void OnSelectBg1(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.gamePop.bgColor1 = _color;
            RefreshUgc();
        }

        private void OnSelectBg2(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.gamePop.bgColor2 = _color;
            RefreshUgc();
        }

        private void OnSelectFont(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.gamePop.fontColor = _color;
            RefreshUgc();
        }

        private void OnSelectPic(AICommonGamePaster _pic, int _idx)
        {
            if (CurUrl != null)
            {
                CurUrl.x = RectPreview.anchoredPosition.x;
                CurUrl.y = RectPreview.anchoredPosition.y;
                CurUrl.scale = RectPreview.localScale.x;
            }
            CurUrl = _pic;
            Idx = _idx;
            PasterPreview.Load(CurUrl.url);
            PasterPreview.gameObject.SetActive(true);
            RectPreview.anchoredPosition = new Vector2(CurUrl.x, CurUrl.y);
            RectPreview.localScale = Vector3.one * CurUrl.scale;
            for (int i = 0; i < PasterLs.Count; i++)
            {
                if (PasterLs[i].Config == null)
                {
                    PasterLs[i].gameObject.SetActive(true);
                    PasterLs[i].transform.SetAsLastSibling();
                    break;
                }
            }
            OnClickBtn(CurUrl, _idx);
            OnClickBtn();
        }

        private void OnUnSelectBtn(AICommonGamePaster _pic, int _idx)
        {
            PasterPreview.ResetRawImage();
            PasterPreview.gameObject.SetActive(false);

            bool bo = true;
            for (int i = 0; i < PasterLs.Count; i++)
            {
                if (PasterLs[i].Config != null)
                {
                    bo = false;
                    break;
                }
            }

            if (bo)
            {
                for (int i = 0; i < PasterLs.Count; i++)
                {
                    if (i == _idx)
                    {
                        PasterLs[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        PasterLs[i].gameObject.SetActive(false);
                    }
                }

            }
            else
            {
                //PasterLs[_idx].gameObject.SetActive(false);
            }

            CurUrl = null;
            Idx = 0;
        }

        private void OnClickBtn(AICommonGamePaster _pic, int _idx)
        {
            ClickBtn.enabled = true;
            if (CurUrl != null)
            {
                CurUrl.x = RectPreview.anchoredPosition.x;
                CurUrl.y = RectPreview.anchoredPosition.y;
                CurUrl.scale = RectPreview.localScale.x;
            }
            CurUrl = _pic;
            Idx = _idx;
            PasterPreview.Load(CurUrl.url);
            PasterPreview.gameObject.SetActive(true);
            RectPreview.anchoredPosition = new Vector2(CurUrl.x, CurUrl.y);
            RectPreview.localScale = Vector3.one * CurUrl.scale;
        }

        private void OnDelBtn()
        {
            PasterLs[Idx].OnCancelBtnClick();
            PitchCover.gameObject.SetActive(false);
        }

        private void OnClickBtn()
        {
            InputAnim.target = PasterPreview.transform;
            InputAnim.trigger = true;
            ClickBtn.enabled = false;
            PitchCover.gameObject.SetActive(true);
        }


        private void RefreshUgc() {
            var gamePop = curMapInfo.gameSetting.AICommonGameConfig.gamePop;
            if (!string.IsNullOrEmpty(gamePop.bgColor1) &&
                ColorUtility.TryParseHtmlString("#" + gamePop.bgColor1, out var c))
            {
                foreach (var item in UgcImage)
                {
                    item.color = c;
                }
            }

            if (!string.IsNullOrEmpty(gamePop.bgColor2) &&
                ColorUtility.TryParseHtmlString("#" + gamePop.bgColor2, out var c2))
            {
                foreach (var item in UgcImage2)
                {
                    item.color = c2;
                }

                foreach (var item in Text2)
                {
                    item.color = c2;
                }
            }


            if (!string.IsNullOrEmpty(gamePop.fontColor) &&
                ColorUtility.TryParseHtmlString("#" + gamePop.fontColor, out var c3))
            {
                foreach (var item in Text)
                {
                    item.color = c3;
                }
            }
        }
    }
}