using Es;
using Game.Avatar;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 草稿箱操作菜单，同时处理草稿态和已发布态。
    /// 根据 paymentInfo 是否为 null 动态切换按钮组和扩展包区域。
    /// </summary>
    public class DraftBoxMenu : BaseDraftBoxMenu
    {
        [Header("按状态切换的按钮组")]
        public List<GameObject> DraftActions;       // 草稿状态下显示的元素列表
        public List<GameObject> PublishedActions;   // 已发布状态下显示的元素列表
        public List<GameObject> UnPublishedActions;   // 已下架状态下显示的元素列表

        [Header("草稿操作")]
        public CButton Btn_Issue;       // 发布草稿
        public CButton Btn_Edit;        // 编辑（跳转角色编辑器）
        public CButton Btn_Copy;        // 复制草稿
        public CButton Btn_Delect;      // 删除草稿

        [Header("已发布操作")]
        public CButton Btn_ViewDetail;      // 查看角色详情页
        public CButton Btn_Unpublish;       // 下架商品

        [Header("扩展包区域")]
        //public Text Txt_ExtPackCount;       // 扩展包数量 Badge
        public CButton Btn_CreateExtPack;   // + 创建扩展包
        public RectTransform Content;  // 列表
        public Transform ExtPackContainer;  // 扩展包列表容器
        public GameObject ExtPackItemPrefab;// 扩展包 Item 预制体

        private readonly List<IncubationExtensionPackItem> _extPackItems = new();
        private readonly List<IncubationExtensionPackItem> _extPackItemPool = new();

        private bool _isCreatingExtPack = false;  // 防止创建皮肤按钮重复触发
        private BudTimer _createExtPackTimer;      // 超时保护：弱网时自动恢复按钮
        private IncubationCabinDraftBox _draftBox;

        public void SetDraftBox(IncubationCabinDraftBox draftBox)
        {
            _draftBox = draftBox;
        }

        public override void InitUI()
        {
            base.InitUI();
            Btn_Issue.onClick.AddListener(OnBtnIssueClick);
            Btn_Edit.onClick.AddListener(OnBtnEditClick);
            //Btn_Custom.onClick.AddListener(OnBtnCustomClick);
            //Btn_ReName.onClick.AddListener(OnBtnReNameClick);
            Btn_Copy.onClick.AddListener(OnBtnCopyClick);
            Btn_Delect.onClick.AddListener(OnBtnDelectClick);

            Btn_ViewDetail.onClick.AddListener(OnBtnViewDetailClick);
            Btn_Unpublish.onClick.AddListener(OnBtnUnpublishClick);
            Btn_CreateExtPack.onClick.AddListener(OnBtnCreateExtPackClick);

            MessageHelper.AddListener<CabinCharacterPackInfo>(MessageName.OnCabinExtPackCreated, OnExtPackCreated);
            MessageHelper.AddListener<CabinCharacterPackInfo>(MessageName.OnCabinExtPackUpdated, OnExtPackUpdated);
            MessageHelper.AddListener<CabinCharacterPackInfo>(MessageName.OnCabinExtPackDelect, OnExtPackDelect);
        }

        private void OnDestroy()
        {
            MessageHelper.RemoveListener<CabinCharacterPackInfo>(MessageName.OnCabinExtPackCreated, OnExtPackCreated);
            MessageHelper.RemoveListener<CabinCharacterPackInfo>(MessageName.OnCabinExtPackUpdated, OnExtPackUpdated);
            MessageHelper.RemoveListener<CabinCharacterPackInfo>(MessageName.OnCabinExtPackDelect, OnExtPackDelect);
        }

        public override void UpdateInfo(CabinCharacterUgcInfo data)
        {
            base.UpdateInfo(data);
            bool isPublished = data.ugcclass == 2;
            foreach (var go in DraftActions) go.SetActive(false);
            foreach (var go in PublishedActions) go.SetActive(false);
            foreach (var go in UnPublishedActions) go.SetActive(false);
            if (isPublished)
            {
                if (data.IsUnpublished() > emUnpublished.None)
                {
                    foreach (var go in UnPublishedActions) go.SetActive(true);
                }
                else
                {
                    foreach (var go in PublishedActions) go.SetActive(true);
                }
            }
            else
            {
                foreach (var go in DraftActions) go.SetActive(true);
            }


            RefreshExtPackList();
        }

        public override void ClearInfo()
        {
            ReturnAllToPool();
            base.ClearInfo();
        }

        // ──────────────── 草稿操作 ────────────────

        private void OnBtnIssueClick()
        {
            if (CheckDataIllegal()) return;
            HidePanel();
            UIManager.Inst.OpenPanel<IncubationPublishPanel>(PanelId.IncubationPublishPanel, characterInfo);
        }

        private void OnBtnEditClick()
        {
            if (CheckDataIllegal()) return;
            var skinPack = characterInfo.skinPack?.Find(s => s.isDefault == 1)
                           ?? characterInfo.skinPack?[0];
            if (skinPack == null)
            {
                LoggerUtils.LogError("DraftBoxMenu - skinPack = null");
                return;
            }
            HidePanel();
            UIManager.Inst.OpenPanel(PanelId.IncubationCabinPanel, characterInfo, CabinEntryType.Edit, characterInfo.toneId);
        }

        private void OnBtnCopyClick()
        {
            if (CheckDataIllegal() || _isRequesting) return;
            _isRequesting = true;
            bool shouldSwitchToDraft = _draftBox != null && _draftBox.IsOnPublishedTab;
            CabinNetManager.Inst.SetCabinCharacterInfo(characterInfo, SetType.Copy, (isSuccess) =>
            {
                _isRequesting = false;
                if (!isSuccess) { TipPanel.ShowToast("操作失败，请稍后重试"); return; }
                HidePanel();

                if (shouldSwitchToDraft)
                {
                    _draftBox.SwitchToDraftTab();
                }
            });
        }

        private void OnBtnDelectClick()
        {
            if (CheckDataIllegal()) return;
            CommonConfirmPanel confirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            confirmPanel.SetText("确认删除", "你确定要删除该草稿吗？\n一旦删除就无法找回", "删除", "取消");
            confirmPanel.SetOnClickAction(ConfirmDelete, null);
        }

        private void ConfirmDelete()
        {
            if (CheckDataIllegal()) return;
            CabinNetManager.Inst.SetCabinCharacterInfo(characterInfo, SetType.Delete, (isSuccess) =>
            {
                if (!isSuccess) return;
                HidePanel();
            });
        }

        // ──────────────── 已发布操作 ────────────────

        private void OnBtnViewDetailClick()
        {
            if (CheckDataIllegal()) return;
            HidePanel();
            CabinRolesNetManager.Inst.OpenPanelWithFreshData(characterInfo.stockUgcId);
        }

        private void OnBtnUnpublishClick()
        {
            if (CheckDataIllegal()) return;
            var confirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            confirmPanel.SetLocalText("提示", $"确认下架【{characterInfo.name}】吗？", "确认", "取消");
            confirmPanel.SetOnClickAction(ConfirmUnpublish);
            confirmPanel.HideCloseBtn();
        }

        private void ConfirmUnpublish()
        {
            if (CheckDataIllegal()) return;
            CabinNetManager.Inst.SetCabinCharacterInfo(characterInfo, SetType.Unpublish, (isSuccess) =>
            {
                if (!isSuccess)
                {
                    TipPanel.ShowToast("下架失败，请重试");
                    return;
                }
                HidePanel();
            });
        }

        private void OnBtnCreateExtPackClick()
        {
            if (CheckDataIllegal()) return;

            if (_isCreatingExtPack)
                return;

            _isCreatingExtPack = true;
            Btn_CreateExtPack.SetClickAble(false);

            var defConfigList = DataTables.GetCabinDefCharacterConfigList();
            var defConfig = defConfigList != null && defConfigList.Count > 0 ? defConfigList[0] : null;

            var packInfo = new CabinCharacterPackInfo
            {
                characterId = characterInfo.id,
                name = characterInfo.name,
            };
            packInfo.skinPack = new List<SkinPackInfo>()
            {
                new SkinPackInfo()
                {
                    packId = "",
                    avatarJson = CharacterData.SerializeObject(AccountDataManager.Inst.UserInfo.avatarInfo as CharacterData),
                    isDefault = 1,
                    cover = "",
                }
            };

            var detail = new CabinCoverDetail()
            {
                colorId = defConfig != null ? defConfig.CardColorID : 1,
                sizeVec3 = defConfig != null ? defConfig.CardSize : Vector3.one,
                posVec3 = defConfig != null ? defConfig.CardPos : new Vector3(0f, -1f, 0f),
            };
            if (defConfig != null && !string.IsNullOrEmpty(defConfig.PoseAnimID))
            {
                detail.poseId = defConfig.PoseAnimID;
                detail.poseResourceType = (int)ResourceType.Pose;
            }
            packInfo.coverInfo = new CabinCoverInfo()
            {
                desc = "",
                detail = JsonConvert.SerializeObject(detail),
            };

            void ResetCreateBtn()
            {
                _isCreatingExtPack = false;
                Btn_CreateExtPack.SetClickAble(true);
                TimerManager.Inst.Stop(_createExtPackTimer);
                _createExtPackTimer = null;
            }

            void OnPanelOpened()
            {
                ResetCreateBtn();
            }

            _createExtPackTimer = TimerManager.Inst.RunOnce("CreateExtPackTimeout", 10f, () =>
            {
                ResetCreateBtn();
                TipPanel.ShowToast("操作失败，请稍后重试");
            });

            if (_draftBox != null)
            {
                _draftBox.ApplyDefaultDataToExtPackAndOpenPanel(packInfo, characterInfo.toneId, OnPanelOpened);
            }
            else
            {
                UIManager.Inst.OpenPanel(PanelId.IncubationCabinPanel, packInfo, CabinEntryType.Create, characterInfo.toneId);
                OnPanelOpened();
            }
        }

        // ──────────────── 扩展包列表 ────────────────

        private IncubationExtensionPackItem GetFromPool()
        {
            if (_extPackItemPool.Count > 0)
            {
                var poolItem = _extPackItemPool[_extPackItemPool.Count - 1];
                _extPackItemPool.RemoveAt(_extPackItemPool.Count - 1);
                poolItem.gameObject.SetActive(true);
                poolItem.transform.SetAsLastSibling();
                return poolItem;
            }
            var go = Instantiate(ExtPackItemPrefab, ExtPackContainer);
            go.gameObject.SetActive(true);
            go.transform.SetAsLastSibling();
            return go.GetComponent<IncubationExtensionPackItem>();
        }

        private void ReturnToPool(IncubationExtensionPackItem item)
        {
            item.gameObject.SetActive(false);
            _extPackItemPool.Add(item);
        }

        private void ReturnAllToPool()
        {
            foreach (var item in _extPackItems)
            {
                if (item != null)
                {
                    ReturnToPool(item);
                }
            }
            _extPackItems.Clear();
        }

        private void RefreshExtPackList()
        {
            if (characterInfo == null) return;

            var packIds = characterInfo.extensionPackList;
            // Txt_ExtPackCount.text = (packIds?.Count ?? 0).ToString();

            if (packIds == null || packIds.Count == 0)
            {
                ReturnAllToPool();
                return;
            }

            CabinNetManager.Inst.GetExtensionPackBatchInfo(packIds, (isSuccess, list) =>
            {
                ReturnAllToPool();

                if (!isSuccess || list == null) return;

                foreach (var pack in list)
                    AddExtPackItem(pack);
                //Txt_ExtPackCount.text = _extPackItems.Count.ToString();
            });
        }

        // 从对象池取出 Item 并绑定数据，供 RefreshExtPackList 和消息处理复用
        private void AddExtPackItem(CabinCharacterPackInfo pack)
        {
            var item = GetFromPool();
            BindExtPackItem(item, pack);
            _extPackItems.Add(item);
        }

        private void RemoveExtPackItem(CabinCharacterPackInfo pack)
        {
            IncubationExtensionPackItem removeItem = null;
            foreach (var item in _extPackItems)
            {
                if (item._data.id == pack.id)
                {
                    removeItem = item;
                    _extPackItems.Remove(item);
                    break;
                }
            }
            if (removeItem == null)
            {
                return;
            }
            ReturnToPool(removeItem);
        }

        // 绑定数据和回调到已有 Item
        private void BindExtPackItem(IncubationExtensionPackItem item, CabinCharacterPackInfo pack)
        {
            if (characterInfo == null)
            {
                return;
            }

            var capturedPack = pack;
            var capturedItem = item;
            item.SetData(pack, characterInfo,
                onDelect: () => ConfirmDelectExtPack(capturedItem, capturedPack),
                onUnpublish: () => ConfirmUnpublishExtPack(capturedItem, capturedPack),
                onEdit: () => UIManager.Inst.OpenPanel(PanelId.IncubationCabinPanel, capturedPack, CabinEntryType.Edit, characterInfo.toneId),
                onPublish: () =>
                {
                    if (characterInfo?.ugcclass != 2)
                    {
                        TipPanel.ShowToast("需要先发布AI伙伴");
                        return;
                    }
                    UIManager.Inst.OpenPanel(PanelId.CabinExtPackPublishPanel, capturedPack, characterInfo);
                },
                onCopy: (_packData) =>
                {
                    var confirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
                    confirmPanel.SetLocalText("提示", $"是否要另存皮肤包【{_packData.name}】？", "确定", "取消");
                    confirmPanel.SetOnClickAction(() =>
                    {
                        string jsonData = JsonConvert.SerializeObject(_packData);
                        var tempPackData = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(jsonData);
                        tempPackData.id = null;
                        CabinNetManager.Inst.SetCabinCharacterPackInfo(tempPackData, SetType.Create, (isSuccess, _) =>
                        {
                            if (!isSuccess)
                            {
                                TipPanel.ShowToast("操作失败，请稍后重试");
                            }
                        });
                    }, () => { });
                    confirmPanel.HideCloseBtn();
                }
                );
        }

        private void ConfirmDelectExtPack(IncubationExtensionPackItem item, CabinCharacterPackInfo pack)
        {
            var confirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            confirmPanel.SetLocalText("提示", $"确认删除【{pack.name}】吗？", "确认", "取消");
            confirmPanel.SetOnClickAction(() =>
            {
                CabinNetManager.Inst.SetCabinCharacterPackInfo(pack, SetType.Delete, (isSuccess, _) =>
                {
                    if (!isSuccess)
                    {
                        TipPanel.ShowToast("删除失败，请重试");
                        return;
                    }
                    _extPackItems.Remove(item);
                    ReturnToPool(item);
                    //Txt_ExtPackCount.text = _extPackItems.Count.ToString();
                });
            }, () => { });
            confirmPanel.HideCloseBtn();
        }

        private void ConfirmUnpublishExtPack(IncubationExtensionPackItem item, CabinCharacterPackInfo pack)
        {
            var confirmPanel = UIManager.Inst.OpenPanel<CommonBoxConfirmWithTitlePanel>(PanelId.CommonBoxConfirmWithTitlePanel);
            confirmPanel.SetLocalText("提示", $"确认下架【{pack.name}】吗？", "确认", "取消");
            confirmPanel.SetOnClickAction(() =>
            {
                CabinNetManager.Inst.SetCabinCharacterPackInfo(pack, SetType.Unpublish, (isSuccess, _) =>
                {
                    if (!isSuccess)
                    {
                        TipPanel.ShowToast("下架失败，请重试");
                        return;
                    }
                    //_extPackItems.Remove(item);
                    //ReturnToPool(item);
                    //Txt_ExtPackCount.text = _extPackItems.Count.ToString();
                });
            }, () => { });
            confirmPanel.HideCloseBtn();
        }

        // 扩展包创建成功：仅处理属于当前角色的包
        private void OnExtPackCreated(CabinCharacterPackInfo packInfo)
        {
            if (packInfo == null || characterInfo == null)
            {
                return;
            }

            if (packInfo?.characterId != characterInfo.id)
            {
                return;
            }

            if (!characterInfo.extensionPackList.Contains(packInfo?.id))
            {
                characterInfo.extensionPackList.Add(packInfo?.id);
            }
            AddExtPackItem(packInfo);
            //Txt_ExtPackCount.text = _extPackItems.Count.ToString();
        }

        private void OnExtPackDelect(CabinCharacterPackInfo packInfo)
        {
            if (packInfo == null || characterInfo == null)
            {
                return;
            }

            if (packInfo?.characterId != characterInfo.id)
            {
                return;
            }
            if (characterInfo.extensionPackList.Contains(packInfo?.id))
            {
                characterInfo.extensionPackList.Remove(packInfo.id);
            }
            RemoveExtPackItem(packInfo);
        }

        // 扩展包编辑保存成功：找到对应 Item 刷新数据，找不到则新增
        private void OnExtPackUpdated(CabinCharacterPackInfo packInfo)
        {
            if (packInfo == null) return;
            List<IncubationExtensionPackItem> poolList = new List<IncubationExtensionPackItem>();
            foreach (var item in _extPackItems)
            {
                if (!characterInfo.extensionPackList.Contains(item.PackId))
                {
                    poolList.Add(item);
                }
            }

            foreach (var item in poolList)
            {
                _extPackItems.Remove(item);
                ReturnToPool(item);
            }
            var existing = _extPackItems.Find(item => item.PackId == packInfo.id);
            if (existing != null)
            {
                BindExtPackItem(existing, packInfo);
            }
            else
            {
                AddExtPackItem(packInfo);
                // Txt_ExtPackCount.text = _extPackItems.Count.ToString();
            }
        }

        protected override void HidePanel()
        {
            base.HidePanel();
            Content.anchoredPosition = Vector2.zero;
        }
    }
}
