using Es;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditPlant : SettingContentBase
    {
        public AIParkSelectColor Color;
        public Transform ColorParent;

        private List<AIParkSelectColor> Images = new List<AIParkSelectColor>();
        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            var ls = Es.DataTables.GetColorDataConfigList();

            foreach (var item in ls)
            {
                var tem = GameObject.Instantiate(Color,ColorParent).GetComponent<AIParkSelectColor>();
                tem.InitData(item.Color, OnSelect);
                Images.Add(tem);
            }
            Color.gameObject.SetActive(false);



        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();
        }

        public override void SaveData()
        {
            base.SaveData();
        }

        private void Start()
        {
            var ls = Es.DataTables.GetColorDataConfigList();
            var idx = 0;
            idx = ls.FindIndex((x) => x.Color == curMapInfo.gameSetting.AICommonGameConfig.plantColor);
            idx = idx < 0 ? 0 : idx;
            Images[idx].Toggle.isOn = true;
        }

        private void OnSelect(string _color)
        {
            curMapInfo.gameSetting.AICommonGameConfig.plantColor = _color;
        }
    }
}