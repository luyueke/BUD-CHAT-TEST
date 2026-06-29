using AIGame.Base;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Reflection;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
    public enum GameEntryPanelType {
        CommunityGame = 0,
        ParkGame,
        HospitalGame,
    }

    public class GameEntryMainPanel : BasePanel<GameEntryMainPanel>
    {
        public List<Toggle> TogList;

        public CButton CloseBtn;

        public CButton GoBtn;

        public Text DescText;

        public GameObject BubbleGo;

        private GameEntryPanelType Type;
        public override void OnCreate()
        {
            base.OnCreate();

            for (int i = 0; i < TogList.Count; i++)
            {
                var t = (GameEntryPanelType)i;
                var tog = TogList[i];
                TogList[i].onValueChanged.AddListener((bo) => { OnTog(bo, t, tog); });
            }

            GoBtn.onClick.AddListener(OnGoBtn);
            CloseBtn.onClick.AddListener(CloseSelf);
            AIParkUtils.Inst.ReportOncePerDay_GamePage();

            GameEntrySystem.Inst.PreloadLastPlayMapInfo();

            BubbleGo.SetActive(false);
            TimerManager.Inst.RunOnce("GameEntryMainPanel_BubbleGo", 0.3f, () => {
                if(this != null && BubbleGo != null)
                {
                    BubbleGo.SetActive(true);
                }
            });
            if(DeviceInfoManager.Inst.CheckVersion_1_0_14())
            {
                if (IsHuaWeiAudit())//华为审核中，暂时去掉剧本杀
                {
                    TogList[(int)GameEntryPanelType.ParkGame].gameObject.SetActive(false);
                    var v1 = TogList[(int)GameEntryPanelType.CommunityGame].transform.localPosition;
                    var v2 = TogList[(int)GameEntryPanelType.HospitalGame].transform.localPosition;

                    TogList[(int)GameEntryPanelType.CommunityGame].transform.localPosition = new Vector3(v1.x + 200, v1.y, v1.z);
                    TogList[(int)GameEntryPanelType.HospitalGame].transform.localPosition = new Vector3(v2.x - 200, v2.y, v2.z);
                }
            }

        }

        private bool IsHuaWeiAudit()
        {
            if (IAPDataManager.Inst.channelId != (int)IAPDataManager.ChannelIdEnum.Huawei)
                return false;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                //Debug.Log("assembly.name=" + assembly.FullName);
                if (assembly.FullName.Contains("xasset"))
                {
                    Type t = assembly.GetType("GlobalConfigManager");
                    if (t != null)
                    {
                        var property = t.GetProperty("IsHuaweiAudit", BindingFlags.Public | BindingFlags.Instance);
                        if (property != null && property.PropertyType == typeof(bool))
                        {
                            object instance = GlobalConfigManager.Instance;
                            return (bool)property.GetValue(instance);
                        }

                        return false; // 默认值（如果属性不存在）

                    }
                }
            }
            return false;
        }
        public override void CloseSelf()
        {
            base.CloseSelf();
        }
        protected override void Start()
        {
            base.Start();

            var idx = GameEntrySystem.Inst.GetSaveEntry();
            TogList[idx].isOn = true;
        }

        private void OnGoBtn() {
            switch (Type)
            {
                case GameEntryPanelType.CommunityGame:
                    UIManager.Inst.OpenPanel(PanelId.CommunityGamesPanel, WindowId.RecommendWindow);
                    break;
                case GameEntryPanelType.HospitalGame:
                    UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel, WindowId.RecommendWindow);
                    break;
                case GameEntryPanelType.ParkGame:
                    var parkInGameBigStep = BootDataManager.Inst.GetParkInGame();
                    if (parkInGameBigStep != 0)
                    {
                        UIManager.Inst.OpenPanel(PanelId.GameEntryParkPanel, WindowId.RecommendWindow);
                    }
                    else
                    {
                        AIParkUtils.Inst.EnterOfficalParkGame();
                    }
                    break;
                default:
                    break;
            }
        }

        void OnTog(bool bo, GameEntryPanelType type,Toggle toggle) {
            var trans = toggle.transform;
            if (bo)
            {
                trans.DOScale(Vector3.one * 1.1f, 0.1f);
                GameEntrySystem.Inst.SetSaveEntry((int)type);
                Type = type;
                switch (type)
                {
                    case GameEntryPanelType.CommunityGame:
                        DescText.text = "休闲放松的社交互动玩法\n在海量玩家自制的地图中拍照打卡互动吧，还可以认识很多新的小伙伴一起游戏噢！";
                        break;
                    case GameEntryPanelType.HospitalGame:
                        DescText.text = "和AI斗智斗勇逃离密室\n你被困在了一座废弃医院里，由许多NPC看守，想办法说服或欺骗他们让你离开医院！";
                        break;
                    case GameEntryPanelType.ParkGame:
                        GameEntrySystem.Inst.SetParkRed(1);
                        DescText.text = "体验AI驱动推演的剧本杀故事\n在一座破旧的游乐园中，和NPC们一起演绎属于你们的故事，你的每一步决定将实时改变故事的走向！";
                        break;
                    default:
                        break;
                }

            }
            else 
            {
                if (trans.localScale.x != 1)
                {
                    trans.DOScale(Vector3.one,0.1f);
                }
            }
        }
    }
}