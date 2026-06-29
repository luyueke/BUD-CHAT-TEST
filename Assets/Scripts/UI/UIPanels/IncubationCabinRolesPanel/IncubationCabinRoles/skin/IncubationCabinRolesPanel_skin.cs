using Game.Avatar;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    partial class IncubationCabinRolesPanel
    {
        /// <summary>
        /// 构建皮肤数据列表并创建 CabinSkinCardItem 卡片。
        /// 每个 SkinPackInfo 包装为一个 CabinCharacterBaseInfo，保持一张卡对应一个皮肤变体的粒度。
        /// </summary>
        public void CreatRoleSkinDataList(string skinId = null)
        {
            LoggerUtils.Log("RefreshSkinRoleList");

            if (skinRoleItemPrefab == null || scrollRect == null)
            {
                LoggerUtils.LogError("RefreshSkinRoleList: 必要组件为空");
                return;
            }

            skinRoleItemPrefab.SetActive(false);
            var netCabinCharacterUgcInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

            if (netCabinCharacterUgcInfo == null)
                return;

            // 销毁动态生成的子节点，跳过 prefab 本身避免被误删
            var trans = scrollRect.content;
            for (int i = trans.childCount - 1; i >= 0; i--)
            {
                var child = trans.GetChild(i).gameObject;
                if (child == skinRoleItemPrefab) continue;
                Destroy(child);
            }

            if (netCabinCharacterUgcInfo.skinPack == null)
                return;

            List<CabinCharacterBaseInfo> skinItems = new List<CabinCharacterBaseInfo>();

            // ── 主角色皮肤（皮肤类型）──
            // 将主角色信息深拷贝为 CabinCharacterBaseInfo，每个 SkinPackInfo 对应一张卡片
            foreach (var skinInfo in netCabinCharacterUgcInfo.skinPack)
            {
                string json = JsonConvert.SerializeObject(netCabinCharacterUgcInfo);
                var wrapper = JsonConvert.DeserializeObject<CabinCharacterUgcInfo>(json);
                skinItems.Add(wrapper);
            }

            if (netCabinCharacterUgcInfo.extensionPackList.Count > 0)
            {
                CabinNetManager.Inst.GetExtensionPackBatchInfo(netCabinCharacterUgcInfo.extensionPackList, (isS, dataList) =>
                {
                    if (isS)
                    {
                        foreach (var packInfo in dataList)
                        {
                            if (packInfo.ugcclass != (int)UGCClass.Published)
                                continue;

                            // ── 拓展包皮肤（角色类型）──
                            // 将 CabinCharacterPackInfo 深拷贝，每个 SkinPackInfo 对应一张卡片
                            foreach (var skinInfo in packInfo.skinPack)
                            {
                                var json = JsonConvert.SerializeObject(packInfo);
                                var wrapper = JsonConvert.DeserializeObject<CabinCharacterPackInfo>(json);
                                skinItems.Add(wrapper);
                            }
                        }
                    }
                    CreatSkinRoleList(skinItems, skinId);
                });
            }
            else
            {
                CreatSkinRoleList(skinItems, skinId);
            }
        }

        /// <summary>
        /// 根据皮肤数据列表实例化 CabinSkinCardItem 卡片并绑定选中回调。
        /// </summary>
        /// <param name="datas">每项包含单个 SkinPackInfo 的 CabinCharacterBaseInfo 包装列表</param>
        /// <param name="skinId">指定需要预选的皮肤包 ID；为空则预选默认皮肤</param>
        public void CreatSkinRoleList(List<CabinCharacterBaseInfo> datas, string skinId = null)
        {
            CabinSkinCardItem firstItem = null;
            CabinSkinCardItem targetItem = null;

            datas.ForEach(item =>
            {
                if (item == null) return;

                var skinRoleItem = Instantiate(skinRoleItemPrefab, scrollRect.content);
                skinRoleItem.SetActive(true);

                var cabinSkinCardItem = skinRoleItem.GetComponent<CabinSkinCardItem>();

                if (cabinSkinCardItem == null)
                    return;

                // 用 lambda 捕获 item 引用，以便在回调中设置选中态
                cabinSkinCardItem.SetData(item, _ => SelectItem(cabinSkinCardItem));
                cabinSkinCardItem.RefreshLock();
                // 皮肤包场景下 Badge（草稿/发布/下架）无意义，统一隐藏
                cabinSkinCardItem.SetBadgesVisible(false);

                if (firstItem == null)
                    firstItem = cabinSkinCardItem;

                var skinPackInfo = item.skinPack?[0];

                if (!string.IsNullOrEmpty(skinId))
                {
                    if (targetItem == null && skinPackInfo?.packId == skinId)
                        targetItem = cabinSkinCardItem;
                }
                else
                {
                    if (targetItem == null && skinPackInfo?.isDefault == 1)
                        targetItem = cabinSkinCardItem;
                }
            });

            // 优先选中指定皮肤，找不到则 fallback 到第一个
            var selected = targetItem ?? firstItem;
            if (selected != null)
            {
                SelectItem(selected);
            }
        }

        /// <summary>
        /// 刷新所有皮肤卡片的锁定状态（购买或获得皮肤后调用）。
        /// </summary>
        public void RefreshSkinItemsLock()
        {
            foreach (Transform child in scrollRect.content)
            {
                var item = child.GetComponent<CabinSkinCardItem>();
                if (item != null) item.RefreshLock();
            }
        }

        /// <summary>
        /// 处理皮肤卡片选中：切换高亮态，并将对应皮肤应用到角色形象。
        /// </summary>
        /// <param name="item">被点击的 CabinSkinCardItem</param>
        private void SelectItem(CabinSkinCardItem item)
        {
            // 切换皮肤时停止当前预览语音和定时器
            StopCurrentPreviewAudioAndTimers();
            // 清除所有卡片的选中高亮
            foreach (Transform child in scrollRect.content)
            {
                var sibling = child.GetComponent<CabinSkinCardItem>();
                if (sibling != null)
                    sibling.SetSelected(false);
            }

            // 设置当前卡片为选中态
            item.SetSelected(true);

            var data = item._data;
            // 包装时 skinPack 只含 1 个元素，取下标 0 即为该皮肤的具体信息
            var skinPackInfo = data?.skinPack?[0];

            if (skinPackInfo == null)
            {
                LoggerUtils.LogError("皮肤 SkinPackInfo 为空，无法切换形象");
                TipPanel.ShowToast("皮肤数据异常，请稍后重试");
                return;
            }

            curSkinPackId = skinPackInfo.packId;
            var _ugcInfo = data as CabinCharacterUgcInfo;
            curSkinUgcId = (_ugcInfo != null && !string.IsNullOrEmpty(_ugcInfo.targetUgcId))
                ? _ugcInfo.targetUgcId
                : data.id;
            curSkinCreator = data.creator;
            // 记录当前选中皮肤所属角色的完整数据，供激活/口令 Tab 使用
            _currentSkinData = data;
            RefreshInteractItemsLock();

            // 若当前停留在激活 / 口令 Tab，立即刷新内容以反映新皮肤的激活和口令
            // 商城模式下，待机 Tab 也依赖 _currentSkinData，皮肤切换时同样需要刷新
            if (currentCharacterInteractIndex == 1 || currentCharacterInteractIndex == 2
                || (currentCharacterInteractIndex == 0 && _isFromShop))
            {
                RfreshInteractContent();
            }

            if (string.IsNullOrEmpty(skinPackInfo.avatarJson))
            {
                LoggerUtils.LogError("皮肤 avatarJson 为空，无法切换形象");
                TipPanel.ShowToast("皮肤数据异常，请稍后重试");
                return;
            }

            try
            {
                InitCharacterWrapper(CharacterData.DeserializeObject(skinPackInfo.avatarJson));
            }
            catch (System.Exception e)
            {
                LoggerUtils.LogError("切换皮肤失败: " + e.Message);
                TipPanel.ShowToast("切换皮肤失败，请稍后重试");
            }

            nameText.text = data.name;
        }
    }
}