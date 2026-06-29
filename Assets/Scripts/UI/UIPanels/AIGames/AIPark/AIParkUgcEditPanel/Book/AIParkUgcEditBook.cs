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
    public class AIParkUgcEditBook : SettingContentBase
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

            if (curMapInfo.gameSetting.AICommonGameConfig.book == null)
            {
                curMapInfo.gameSetting.AICommonGameConfig.book = new AICommonGameConfig_Book();
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
            PasterPreview.ResetRawImage();
            PasterPreview.gameObject.SetActive(false);

            var count = PasterParent.transform.childCount;
            var paster = curMapInfo.gameSetting.AICommonGameConfig.book.pasterUrls;
            for (int i = 0; i < count; i++)
            {
                var idx = i;
                var v = PasterParent.GetChild(i).GetComponent<AIParkUgcEditPaster>();
                v.InitData(OnSelectPic, idx, OnUnSelectBtn, OnClickBtn);
                PasterLs.Add(v);

                if (i < paster.Count)
                {
                    PasterLs[i].SetData(paster[i]);
                    if (i == 0)
                    {
                        PasterLs[i].SetDefault(paster[i]);
                    }
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
            curMapInfo.gameSetting.AICommonGameConfig.book.pasterUrls = new();
            foreach (var v in PasterLs)
            {
                if (v.Config != null)
                {
                    curMapInfo.gameSetting.AICommonGameConfig.book.pasterUrls.Add(v.Config);
                }
            }
        }

        private void Start()
        {
            var ls = Es.DataTables.GetColorDataConfigList();
            var index = 0;
            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.book.bgColor1);
            if (index >= 0)
            {
                ImageBgs1[index].Toggle.isOn = true;
            }

            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.book.bgColor2);
            if (index >= 0)
            {
                ImageBgs2[index].Toggle.isOn = true;
            }

            index = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.book.fontColor);
            if (index >= 0)
            {
                ImageFonts[index].Toggle.isOn = true;
            }

            PitchCover.gameObject.SetActive(false);
            RefreshUgc();
        }

        private void OnSelectBg1(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.book.bgColor1 = _color;
            RefreshUgc();
        }

        private void OnSelectBg2(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.book.bgColor2 = _color;
            RefreshUgc();
        }

        private void OnSelectFont(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.book.fontColor = _color;
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


        public void RefreshUgc() 
        {
            var color1 = curMapInfo.gameSetting.AICommonGameConfig.book.bgColor1;
            if (!string.IsNullOrEmpty(color1) &&
                ColorUtility.TryParseHtmlString("#" + color1, out var c))
            {
                foreach (var item in UgcImage)
                {
                    item.color = c;
                }
            }

            var color2 = curMapInfo.gameSetting.AICommonGameConfig.book.bgColor2;
            if (!string.IsNullOrEmpty(color2) &&
                ColorUtility.TryParseHtmlString("#" + color2, out var c2))
            {
                foreach (var item in UgcImage2)
                {
                    item.color = c2;
                }
            }
        }
    }
}