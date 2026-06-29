using System;
using UnityEngine;
using System.Collections.Generic;
using Game.Pet;
using GameData.GameSync;
using Pb.Game;
using UnityEngine.UI;

namespace Game.GameSetting
{
    public class RoomMenuGlobalSettingPanel : MonoBehaviour
    {
        private string[] LEFT_TABS = { "通用", "图形", "声音" };
        private SettingItemData[] ITEMS_GENERAL;
        private SettingItemData[] ITEMS_GRAPHICS;
        private SettingItemData[] ITEMS_SOUND;
        private SettingItemData[][] TAB_ITEMS;

        private Transform leftTabParent;
        private GameObject[][] gos;
        private ItemShowStatus[][] itemShowStatus;
        private SettingLeftTab[] tabs;
        private Transform rightContent;

        private const int UN_SELECT_HEIGHT = 96;
        private const int SELECT_HEIGHT = 163;

        private void Awake()
        {
            InitConfigs();
            leftTabParent = transform.Find("Left/Tabs");
            rightContent = transform.Find("Right/Scroll View/Viewport/Content");
            InitLeftTabs();
        }


        private void OnEnable()
        {
            SyncGameView();
            ChooseFirstTab();
            ScrollToTop();
        }

        private void ScrollToTop()
        {
            rightContent.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        private void ChooseFirstTab()
        {
            ClickTab(0);
        }

        private void SyncGameView()
        {
            GameObject gameViewItem = FindItem("Game View");
            if (gameViewItem != null)
            {
                gameViewItem.GetComponent<SettingTwoChooseItem>().SetSelected((int)GlobalSettingManager.Inst.GetGameView());
            }
        }

        private GameObject FindItem(string title)
        {
            for (int i = 0; i < TAB_ITEMS.Length; i++)
            {
                for (int j = 0; j < TAB_ITEMS[i].Length; j++)
                {
                    SettingItemData settingItemData = TAB_ITEMS[i][j];
                    if (settingItemData.title == title)
                    {
                        if (i < gos.Length && j < gos[i].Length && gos[i][j] != null)
                        {
                            return gos[i][j];
                        }
                    }
                }
            }

            return null;
        }

        private void SetItemVisibility(string title, bool status)
        {
            for (int i = 0; i < TAB_ITEMS.Length; i++)
            {
                for (int j = 0; j < TAB_ITEMS[i].Length; j++)
                {
                    SettingItemData settingItemData = TAB_ITEMS[i][j];
                    if (settingItemData.title == title)
                    {
                        if (i < itemShowStatus.Length && j < itemShowStatus[i].Length)
                        {
                            itemShowStatus[i][j].canShowBySpecialControl = status;
                            if (i < gos.Length && j < gos[i].Length && gos[i][j] != null)
                            {
                                gos[i][j].SetActive(itemShowStatus[i][j].ShouldShow());
                            }
                        }
                    }
                }
            }
        }

        public void OnDisable()
        {

        }

        #region 配置数据

        private void InitConfigs()
        {
            ITEMS_GENERAL = new SettingItemData[]
            {
                // new SettingTwoChooseItemData
                // {
                //     title = "游戏视图",
                //     firstChoose = "第一人称",
                //     secondChoose = "第三人称",
                //     defaultChoose = GlobalSettingManager.Inst.GetGameView() == GameView.FirstPerson ? 0 : 1,
                //     intercept = CheckIfCanChangeGameView,
                //     OnChooseChange = GameViewChange
                // },
                new SettingTwoChooseItemData
                {
                    title = "自动奔跑",
                    firstChoose = "开",
                    secondChoose = "关",
                    defaultChoose = GlobalSettingManager.Inst.IsAutoRunningOpen() ? 0 : 1,
                    OnChooseChange = AutomaticRunChange
                },
                // new SettingTwoChooseItemData
                // {
                //     title = "锁定遥感",
                //     textLength = 40,
                //     widthLimit = 480,
                //     firstChoose = "开",
                //     secondChoose = "关",
                //     defaultChoose = GlobalSettingManager.Inst.IsLockMoveStick() ? 0 : 1,
                //     OnChooseChange = LockMoveStickChange
                // },
                new SettingSliderItemData()
                {
                    title = "相机平移灵敏度",
                    textLength = 40,
                    widthLimit = 480,
                    minValue = 0.1f,
                    maxValue = 10f,
                    tips = "更高的摄像机平移灵敏度会增加相机平移时的移动速度",
                    defaultValue = GlobalSettingManager.Inst.GetCameraSensitive()*10,
                    OnValueChange = CameraPanSensitivityChange
                },
                new SettingTwoChooseItemData
                {
                    title = "宠物跟随",
                    firstChoose = "开",
                    secondChoose = "关",
                    defaultChoose = AccountDataManager.Inst.PetInfo.isGameHidden,
                    OnChooseChange = SetGamePetVisible
                },
            };
            ITEMS_GRAPHICS = new SettingItemData[]
            {
                new SettingTwoChooseItemData
                {
                    title = "帧率",
                    firstChoose = "高",
                    secondChoose = "低",
                    defaultChoose = GlobalSettingManager.Inst.GetFps() == 60 ? 0 : 1,
                    tips = "更高的帧率可以带来更流畅的画面，但可能会导致手机发热。",
                    OnChooseChange = FPSChange
                },
                // new SettingTwoChooseItemData
                // {
                //     title = "发光特效",
                //     firstChoose = "开",
                //     secondChoose = "关",
                //     defaultChoose = GlobalSettingManager.Inst.IsBloomOpen() ? 0 : 1,
                //     tips = "打开“发光特效”功能会增加光源的亮度，但会导致手机发热。",
                //     OnChooseChange = BloomChange,
                //     OnEnableRefreshView = IsBloomOpen,
                // },
                new SettingTwoChooseItemData
                {
                    title = "阴影",
                    firstChoose = "开",
                    secondChoose = "关",
                    defaultChoose = GlobalSettingManager.Inst.IsShadowOpen() ? 0 : 1,
                    tips = "启用阴影会优化阴影效果但可能导致手机发热。",
                    OnChooseChange = ShadowChange
                },
                new SettingSliderItemData()
                {
                    title = "视距",
                    textLength = 40,
                    widthLimit = 480,
                    minValue = 50f,
                    maxValue = 800,
                    isReal = true,
                    tips = "进一步的视距可能导致低端设备延迟。",
                    defaultValue = GlobalSettingManager.Inst.GetViewDistance(),
                    OnValueChange = ViewDistanceChange
                }
            };
            ITEMS_SOUND = new SettingItemData[]
            {
                new SettingTwoChooseItemData()
                {
                    title = "脚步",
                    firstChoose = "开",
                    secondChoose = "关",
                    defaultChoose = GlobalSettingManager.Inst.IsFootstepOpen() ? 0 : 1,
                    OnChooseChange = FootStepChange
                },
                new SettingSliderItemData()
                {
                    title = "音乐",
                    minValue = 0,
                    maxValue = 100,
                    defaultValue = GlobalSettingManager.Inst.GetBgmVolume(),
                    OnValueChange = BgmChange
                },
                new SettingSliderItemData()
                {
                    title = "声音效果",
                    textLength = 28,
                    widthLimit = 500,
                    minValue = 0,
                    maxValue = 100,
                    defaultValue = GlobalSettingManager.Inst.GetSoundEffectVolume(),
                    OnValueChange = SoundEffectChange
                },
            };
            TAB_ITEMS = new SettingItemData[][]
            {
                ITEMS_GENERAL,
                ITEMS_GRAPHICS,
                ITEMS_SOUND
            };
            gos = new GameObject[][]
            {
                new GameObject[ITEMS_GENERAL.Length],
                new GameObject[ITEMS_GRAPHICS.Length],
                new GameObject[ITEMS_SOUND.Length],
            };
            itemShowStatus = new ItemShowStatus[][]
            {
                new ItemShowStatus[ITEMS_GENERAL.Length],
                new ItemShowStatus[ITEMS_GRAPHICS.Length],
                new ItemShowStatus[ITEMS_SOUND.Length],
            };
            for (int i = 0; i < itemShowStatus.Length; i++)
            {
                for (int j = 0; j < itemShowStatus[i].Length; j++)
                {
                    itemShowStatus[i][j] = new ItemShowStatus();
                    itemShowStatus[i][j].showByTab = false;
                    itemShowStatus[i][j].canShowBySpecialControl = true;
                }
            }
        }

        private bool CheckIfCanChangeGameView(object arg)
        {
            return false;
        }

        #endregion

        #region 事件触发
        public void GameViewChange(int index)
        {
            GlobalSettingManager.Inst.GameViewChange((GameView)index);
        }

        public void AutomaticRunChange(int index)
        {
            GlobalSettingManager.Inst.AutomaticRunChange(index);
        }
        
        public void SetGamePetVisible(int index)
        {
            SetPetHidden(AccountDataManager.Inst.Uid,index);
            AccountDataManager.Inst.SyncGamePetData(index);
            var netData = new HiddenPetNetData
            {
                Op = index
            };
            NetSyncManager.Inst.SendAllRoom(SubCmdType.HiddenPet, netData);
        }

     

        private void SetPetHidden(string playerId,int op)
        {
            var stateCtr = PetAvatarController.Inst.GetPetKCCtrl(playerId);
            stateCtr.kinematicCharacterController.gameObject.SetActive(op == 0);
        }

        public void LockMoveStickChange(int index)
        {
            GlobalSettingManager.Inst.LockMoveStickChange(index);
        }

        public void CameraPanSensitivityChange(float value)
        {
            GlobalSettingManager.Inst.CameraPanSensitivityChange(value/10);
        }

        public void ViewDistanceChange(float value)
        {
            GlobalSettingManager.Inst.ViewDistanceChange(value);
        }

        public void FPSChange(int index)
        {
            GlobalSettingManager.Inst.FPSChange(index);
        }

        public void BloomChange(int index)
        {
            GlobalSettingManager.Inst.BloomChange(index);
        }

        public bool IsBloomOpen()
        {
            return GlobalSettingManager.Inst.IsBloomOpen();
        }

        public void ShadowChange(int index)
        {
            GlobalSettingManager.Inst.ShadowChange(index);
        }

        public void FootStepChange(int index)
        {
            GlobalSettingManager.Inst.FootStepChange(index);
        }

        public void BgmChange(float value)
        {
            GlobalSettingManager.Inst.BgmChange(value);
        }

        public void SoundEffectChange(float value)
        {
            GlobalSettingManager.Inst.SoundEffectChange(value);
        }
        #endregion

        private void InitLeftTabs()
        {
            GameObject prefabTab = XAssetLoaderMgr.Inst.LoadResource<GameObject>("Assets/Loadable/UI/UIPanel/GameSettingPanel/SettingLeftTab/SettingLeftTab.prefab", gameObject);
            tabs = new SettingLeftTab[LEFT_TABS.Length];
            for (int i = 0; i < LEFT_TABS.Length; i++)
            {
                GameObject tab = Instantiate(prefabTab, leftTabParent);
                SettingLeftTab settingTab = tab.GetComponent<SettingLeftTab>();
                tabs[i] = settingTab;
                settingTab.Init(LEFT_TABS[i], i != LEFT_TABS.Length - 1);
                int index = i;
                settingTab.OnClick += () => { ClickTab(index); };
                settingTab.StatusChange += (status) => { ChangeRightItems(index, status); };
            }

            if (tabs.Length > 0)
            {
                tabs[0].SetSelected(true);
                RefreshLeftTabsItem();
            }
        }

        public void RefreshLeftTabsItem()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                var rectTrans = tabs[i].GetComponent<RectTransform>();
                var width = rectTrans.sizeDelta.x;
                var height = UN_SELECT_HEIGHT;
                if (tabs[i].IsSelected())
                {
                    height = SELECT_HEIGHT;
                }

                rectTrans.sizeDelta = new Vector2(width, height);
            }

            var trans = leftTabParent.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(trans);
        }

        private void ClickTab(int index)
        {
            if (tabs == null)
            {
                return;
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].SetSelected(index == i);
            }

            RefreshLeftTabsItem();

        }

        private void ChangeRightItems(int index, bool status)
        {
            if (index < 0 || index >= TAB_ITEMS.Length)
            {
                return;
            }

            SettingItemData[] itemDatas = TAB_ITEMS[index];
            for (int i = 0; i < itemDatas.Length; i++)
            {
                if (index < 0 || index >= gos.Length || index >= itemShowStatus.Length)
                {
                    continue;
                }

                if (i < 0 || i >= gos[index].Length || i >= itemShowStatus[index].Length)
                {
                    continue;
                }

                itemShowStatus[index][i].showByTab = status;
                bool exist = gos[index][i] != null;
                if (!exist)
                {
                    if (status)
                    {
                        SettingItem settingItem = InitItems(index, i, itemDatas[i]);
                        if (settingItem == null)
                        {
                            continue;
                        }

                        settingItem.Init(itemDatas[i]);
                        exist = true;
                    }
                }

                if (exist)
                {
                    gos[index][i].SetActive(itemShowStatus[index][i].ShouldShow());
                }
            }

            //回到顶部
            if (status)
            {
                ScrollToTop();
            }
        }

        private SettingItem InitItems(int index, int i, SettingItemData itemData)
        {
            GameObject prefabItem = null;
            Type t = null;
            if (itemData is SettingTwoChooseItemData)
            {
                prefabItem = XAssetLoaderMgr.Inst.LoadResource<GameObject>(
                    "Assets/Loadable/UI/UIPanel/GameSettingPanel/SettingTwoChooseItem/SettingTwoChooseItem.prefab",
                    gameObject);
                t = typeof(SettingTwoChooseItem);
            }
            else if (itemData is SettingSliderItemData)
            {
                prefabItem = XAssetLoaderMgr.Inst.LoadResource<GameObject>(
                    "Assets/Loadable/UI/UIPanel/GameSettingPanel/SettingSliderItem/SettingSliderItem.prefab",
                    gameObject);
                t = typeof(SettingSliderItem);
            }
            else if (itemData is SettingTwoSliderItemData)
            {
                prefabItem = XAssetLoaderMgr.Inst.LoadResource<GameObject>(
                    "Assets/Loadable/UI/UIPanel/GameSettingPanel/SettingTwoSliderItem/SettingTwoSliderItem.prefab",
                    gameObject);
                t = typeof(SettingTwoSliderItem);
            }
            else if (itemData is SettingFiveChooseData)
            {
                prefabItem = XAssetLoaderMgr.Inst.LoadResource<GameObject>(
                    "Assets/Loadable/UI/UIPanel/GameSettingPanel/SettingFiveChooseItem/SettingFiveChooseItem.prefab",
                    gameObject);
                t = typeof(SettingFiveChooseItem);
            }

            if (prefabItem == null)
            {
                return null;
            }

            GameObject item = Instantiate(prefabItem, rightContent);
            gos[index][i] = item;
            SettingItem settingItem = item.GetComponent(t) as SettingItem;
            return settingItem;
        }
        

        public class ItemShowStatus
        {
            public bool showByTab;
            public bool canShowBySpecialControl;

            public bool ShouldShow()
            {
                return showByTab && canShowBySpecialControl;
            }
        }
    }
}