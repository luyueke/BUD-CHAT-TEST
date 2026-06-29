using GameData.BaseInfo;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkUgcEditBillboard : SettingContentBase
    {
        public List<Toggle> TogLs;
        public List<AIParkPicUpLoad> PicLs;

        private int curIdx;
        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);

            if (curMapInfo.gameSetting.AICommonGameConfig.billboard == null)
            {
                curMapInfo.gameSetting.AICommonGameConfig.billboard = new AICommonGameConfig_Billboard();
            }

            // 兼容老存档数量
            var billboard = curMapInfo.gameSetting.AICommonGameConfig.billboard;
            var count = 0;
            if (billboard.woodenhorseUrls.Count < 4)
            {
                count = 4 - billboard.woodenhorseUrls.Count;
                for (int i = 0; i < count; i++)
                {
                    billboard.woodenhorseUrls.Add("");
                }
            }
            if (billboard.fountainUrls.Count < 4)
            {
                count = 4 - billboard.fountainUrls.Count;
                for (int i = 0; i < count; i++)
                {
                    billboard.fountainUrls.Add("");
                }
            }
            if (billboard.parkUrls.Count < 4)
            {
                count = 4 - billboard.parkUrls.Count;
                for (int i = 0; i < count; i++)
                {
                    billboard.parkUrls.Add("");
                }
            }

            for (int i = 0; i < PicLs.Count; i++)
            {
                var idx = i;
                PicLs[i].InitData(OnSelect, idx, OnDel,null);
            }

            curIdx = 0;
            TogLs[curIdx].isOn = true;
            OnTog(true, curIdx);
        }

        public override void InitUIComponent()
        {
            base.InitUIComponent();

            for (int i = 0; i < TogLs.Count; i++)
            {
                var idx = i;
                TogLs[idx].onValueChanged.AddListener((succ) => { OnTog(succ, idx); });
            }
        }

        public override void SaveData()
        {
            base.SaveData();
        }

        private void RefreshView() {
            List<string> str = null;
            int show = 1;
            switch (curIdx)
            {
                case 0: str = curMapInfo.gameSetting.AICommonGameConfig.billboard.woodenhorseUrls; show = 4; break;
                case 1: str = curMapInfo.gameSetting.AICommonGameConfig.billboard.fountainUrls; show = 4; break;
                case 2: str = curMapInfo.gameSetting.AICommonGameConfig.billboard.parkUrls; show = 1; break;
                default:
                    break;
            }


            for (int i = 0; i < PicLs.Count; i++)
            {
                if (i < show)
                {
                    PicLs[i].gameObject.SetActive(true);
                    if (i < str.Count)
                    {
                        PicLs[i].SetData(str[i]);
                    }
                    else
                    {
                        PicLs[i].SetData("");
                    }
                }
                else
                {
                    PicLs[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnTog(bool bo,int idx)
        {
            OnToggleValueChanged(TogLs[idx].gameObject,bo);
            if (bo)
            {
                curIdx = idx;
                RefreshView();
            }
        }

        public void OnToggleValueChanged(GameObject obj, bool isOn)
        {
            var togSwitch = obj.GetComponent<CommonToggleSwitch>();
            togSwitch.SetSelectState(isOn);
        }

        private void OnSelect(string url,int idx)
        {
            switch (curIdx)
            {
                case 0: curMapInfo.gameSetting.AICommonGameConfig.billboard.woodenhorseUrls[idx] = url; break;
                case 1: curMapInfo.gameSetting.AICommonGameConfig.billboard.fountainUrls[idx] = url; break;
                case 2: curMapInfo.gameSetting.AICommonGameConfig.billboard.parkUrls[idx] = url; break;
                default:
                    break;
            }
        }
        private void OnDel(string url, int idx)
        {
            switch (curIdx)
            {
                case 0: curMapInfo.gameSetting.AICommonGameConfig.billboard.woodenhorseUrls[idx] = ""; break;
                case 1: curMapInfo.gameSetting.AICommonGameConfig.billboard.fountainUrls[idx] = ""; break;
                case 2: curMapInfo.gameSetting.AICommonGameConfig.billboard.parkUrls[idx] = ""; break;
                default:
                    break;
            }
        }
    }
}