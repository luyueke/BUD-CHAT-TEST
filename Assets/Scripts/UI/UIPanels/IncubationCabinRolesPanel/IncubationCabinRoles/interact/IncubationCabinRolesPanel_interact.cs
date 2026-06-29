using System.Collections.Generic;
using UnityEngine;

namespace UI.UIPanels.IncubationCabin
{
    partial class IncubationCabinRolesPanel
    {
        RoleInteractNode1RoleItem _currentSelectedNode1Item;
        List<bool> _isActivationOpenList = new List<bool>() { };
        List<bool> _isVoiceCommandsOpenList = new List<bool>() { };
        int currentCharacterInteractIndex = 0; //0:待机,1:激活,2:口令互动

        public void RefreshInteract()
        {
            // 先移除再添加，防止多次调用导致事件重复注册
            characterInteractToggleParent.onSelect -= OnSelectCharacterInteract;
            characterInteractToggleParent.onSelect += OnSelectCharacterInteract;
            characterInteractToggleParent.Init(0);
        }

        public void OnSelectCharacterInteract(int index)
        {
            LoggerUtils.Log($"[IncubationCabinRolesPanel] OnSelectCharacterInteract index={index}");
            // 切换页签时中断当前预览
            EndPreviewActivation();
            currentCharacterInteractIndex = index;
            interactContent1Go.SetActive(index == 0);
            interactContent2Go.SetActive(index == 1);
            interactContent3Go.SetActive(index == 2);
            RfreshInteractContent();
        }

        public void RfreshInteractContent()
        {
            if (currentCharacterInteractIndex == 0)
            {
                //待机
                _currentSelectedNode1Item = null;
                interact_node1_itemPrefab.SetActive(false);
                var content1 = interact_node1_scrollRect.content.Find("Content1");
                var content2 = interact_node1_scrollRect.content.Find("Content2");
                if (content1 == null || content2 == null)
                {
                    LoggerUtils.LogError("RfreshInteractContent: Content1/Content2 节点未找到");
                    return;
                }
                for (int i = content1.childCount - 1; i >= 0; i--)
                    DestroyImmediate(content1.GetChild(i).gameObject);
                for (int i = content2.childCount - 1; i >= 0; i--)
                    DestroyImmediate(content2.GetChild(i).gameObject);

                var cabinInfo = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                if (cabinInfo == null)
                    return;

                List<pEmoteData> loopList;
                List<pEmoteData> nonLoopList;

                if (_isFromShop)
                {
                    // 商城模式：只显示当前选中皮肤对应的 CabinCharacterBaseInfo 的 pendingEmote
                    // 与唤醒 Tab、口令互动 Tab 保持一致，均使用 _currentSkinData ?? 本体数据
                    var currentSkinInfo = _currentSkinData ?? (CabinCharacterBaseInfo)cabinInfo;
                    loopList    = new List<pEmoteData>(currentSkinInfo?.pendingEmote?.loopEmoteList ?? new List<pEmoteData>());
                    nonLoopList = new List<pEmoteData>(currentSkinInfo?.pendingEmote?.emoteList     ?? new List<pEmoteData>());
                }
                else
                {
                    // 普通入口（草稿箱、角色列表等）：使用当前生效的 usingEmote
                    loopList    = cabinInfo.usingEmote?.loopEmoteList ?? new List<pEmoteData>();
                    nonLoopList = cabinInfo.usingEmote?.emoteList     ?? new List<pEmoteData>();
                }

                void SpawnItem(pEmoteData data, Transform parent, bool isLoop)
                {
                    var item2 = Instantiate(interact_node1_itemPrefab, parent);
                    item2.SetActive(true);
                    var roleItem = item2.GetComponent<RoleInteractNode1RoleItem>();
                    roleItem.Init(data);
                    roleItem.RefreshLock(curSkinUgcId, curSkinCreator);
                    roleItem.onAddAction = (pEmoteData) =>
                    {
                        LoggerUtils.LogError("onAddAction: " + pEmoteData);
                        _currentSelectedNode1Item?.SetSelected(false);
                        _currentSelectedNode1Item = roleItem;
                        _currentSelectedNode1Item.SetSelected(true);
                        PlayEmote(pEmoteData, isLoop);
                        GlobalFuncExtensions.RefreshLayout(interact_node1_scrollRect.content);
                    };
                }

                foreach (var d in loopList) SpawnItem(d, content1, true);
                foreach (var d in nonLoopList) SpawnItem(d, content2, false);
                interact_node1_emptyTipGo?.SetActive(loopList.Count == 0 && nonLoopList.Count == 0);
                GlobalFuncExtensions.RefreshLayout(interact_node1_scrollRect.content);
            }
            else if (currentCharacterInteractIndex == 1)
            {
                //激活
                int childCount = interact_node2_scrollRect.content.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Destroy(interact_node2_scrollRect.content.transform.GetChild(i).gameObject);
                }
                interact_node2_emptyTipGo.SetActive(false);
                interact_node2_btnPrefab.gameObject.SetActive(false);
                interact_node2_itemPrefab.gameObject.SetActive(false);

                CabinCharacterBaseInfo charInfo2;
                List<characterInteraction> activationList;

                if (_isFromShop)
                {
                    // 商城模式：使用当前选中皮肤的 activation（待上架草稿数据）
                    charInfo2 = _currentSkinData ?? (CabinCharacterBaseInfo)CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                    if (charInfo2 == null)
                        return;

                    activationList = charInfo2.activation ?? new List<characterInteraction>();
                }
                else
                {
                    // 普通入口（草稿箱、角色列表等）：使用当前生效的 usingActivation
                    var cabinUgcInfo2 = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                    if (cabinUgcInfo2 == null)
                        return;

                    charInfo2 = cabinUgcInfo2;
                    activationList = cabinUgcInfo2.usingActivation ?? new List<characterInteraction>();
                }

                string toneId2 = (charInfo2 as CabinCharacterUgcInfo)?.toneId ?? "";
                for (int i = 0; i < activationList.Count; i++)
                {
                    int idx = i;
                    if (idx >= _isActivationOpenList.Count)
                    {
                        _isActivationOpenList.Add(false);
                    }
                    var item = Instantiate(interact_node2_itemPrefab, interact_node2_scrollRect.content);
                    item.SetActive(true);
                    var node2Item = item.GetComponent<RoleInteractNode2RoleItem>();
                    node2Item.Init(charInfo2, activationList[i], toneId2, i, _isActivationOpenList[i]);
                    node2Item.RefreshLock(curSkinUgcId, curSkinCreator);
                    node2Item.onCommonSelectBtnClick = () =>
                    {
                        _isActivationOpenList[idx] = !_isActivationOpenList[idx];
                        GlobalFuncExtensions.RefreshLayout(interact_node2_scrollRect.content);
                    };
                }

                // var btnItem = Instantiate(interact_node2_btnPrefab, interact_node2_scrollRect.content);
                // btnItem.SetActive(true);
                // btnItem.GetComponent<Button>().onClick.AddListener(() =>
                // {
                //     var activationList2 = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo().activation;
                //     if (activationList2 != null && activationList2.Count >= 5)
                //     {
                //         TipPanel.ShowToast("唤醒动作最多5个");
                //         return;
                //     }
                //     CabinRolesNetManager.Inst.AddEmptyActivation((isSuccess) =>
                //     {
                //         if (isSuccess)
                //         {
                //             LoggerUtils.Log("添加空唤醒动作成功");
                //             CabinRolesNetManager.Inst.RefreshInteractContent();
                //         }
                //     });
                // });

                interact_node2_emptyTipGo.SetActive(activationList.Count == 0);
                GlobalFuncExtensions.RefreshLayout(interact_node2_scrollRect.content);
            }
            else if (currentCharacterInteractIndex == 2)
            {
                //口令互动
                int childCount = interact_node3_scrollRect.content.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Destroy(interact_node3_scrollRect.content.transform.GetChild(i).gameObject);
                }
                interact_node3_emptyTipGo.SetActive(false);
                interact_node3_btnPrefab.gameObject.SetActive(false);
                interact_node3_itemPrefab.gameObject.SetActive(false);

                CabinCharacterBaseInfo charInfo3;
                List<voiceCommands> voiceCommandsList;

                if (_isFromShop)
                {
                    // 商城模式：使用当前选中皮肤的 voiceCommands（待上架草稿数据）
                    charInfo3 = _currentSkinData ?? (CabinCharacterBaseInfo)CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                    if (charInfo3 == null)
                        return;

                    voiceCommandsList = charInfo3.voiceCommands ?? new List<voiceCommands>();
                }
                else
                {
                    // 普通入口（草稿箱、角色列表等）：使用当前生效的 usingVoiceCommands
                    var cabinUgcInfo3 = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo();

                    if (cabinUgcInfo3 == null)
                        return;

                    charInfo3 = cabinUgcInfo3;
                    voiceCommandsList = cabinUgcInfo3.usingVoiceCommands ?? new List<voiceCommands>();
                }

                string toneId3 = (charInfo3 as CabinCharacterUgcInfo)?.toneId ?? "";
                for (int i = 0; i < voiceCommandsList.Count; i++)
                {
                    int idx = i;
                    if (idx >= _isVoiceCommandsOpenList.Count)
                    {
                        _isVoiceCommandsOpenList.Add(false);
                    }
                    var item = Instantiate(interact_node3_itemPrefab, interact_node3_scrollRect.content);
                    item.SetActive(true);
                    var node3Item = item.GetComponent<RoleInteractNode3RoleItem>();
                    node3Item.Init(charInfo3, voiceCommandsList[i], toneId3, i, _isVoiceCommandsOpenList[i]);
                    node3Item.RefreshLock(curSkinUgcId, curSkinCreator);
                    node3Item.onCommonSelectBtnClick = () =>
                    {
                        _isVoiceCommandsOpenList[idx] = !_isVoiceCommandsOpenList[idx];
                        GlobalFuncExtensions.RefreshLayout(interact_node3_scrollRect.content);
                    };
                }
                // var btnItem = Instantiate(interact_node3_btnPrefab, interact_node3_scrollRect.content);
                // btnItem.SetActive(true);
                // btnItem.GetComponent<Button>().onClick.AddListener(() =>
                // {
                //     var voiceCommandsList2 = CabinRolesNetManager.Inst.GetNetCabinCharacterUgcInfo().voiceCommands;
                //     if (voiceCommandsList2 != null && voiceCommandsList2.Count >= 5)
                //     {
                //         TipPanel.ShowToast("口令互动最多5个");
                //         return;
                //     }
                //     CabinRolesNetManager.Inst.AddEmptyVoiceCommands((isSuccess) =>
                //     {
                //         if (isSuccess)
                //         {
                //             LoggerUtils.Log("添加空口令互动成功");
                //             CabinRolesNetManager.Inst.RefreshInteractContent();
                //         }
                //     });
                // });
                interact_node3_emptyTipGo.SetActive(voiceCommandsList.Count == 0);
                GlobalFuncExtensions.RefreshLayout(interact_node3_scrollRect.content);
            }
            else if (currentCharacterInteractIndex == 3)
            {
                // 语音对话（暂不实现）
            }
        }

        public void RefreshInteractItemsLock()
        {
            if (currentCharacterInteractIndex == 0)
            {
                var content1 = interact_node1_scrollRect.content.Find("Content1");
                var content2 = interact_node1_scrollRect.content.Find("Content2");
                if (content1 != null)
                    foreach (Transform c in content1)
                        c.GetComponent<RoleInteractNode1RoleItem>()?.RefreshLock(curSkinUgcId, curSkinCreator);
                if (content2 != null)
                    foreach (Transform c in content2)
                        c.GetComponent<RoleInteractNode1RoleItem>()?.RefreshLock(curSkinUgcId, curSkinCreator);
            }
            else if (currentCharacterInteractIndex == 1)
            {
                foreach (Transform c in interact_node2_scrollRect.content)
                    c.GetComponent<RoleInteractNode2RoleItem>()?.RefreshLock(curSkinUgcId, curSkinCreator);
            }
            else if (currentCharacterInteractIndex == 2)
            {
                foreach (Transform c in interact_node3_scrollRect.content)
                    c.GetComponent<RoleInteractNode3RoleItem>()?.RefreshLock(curSkinUgcId, curSkinCreator);
            }
        }

        void PlayEmote(pEmoteData pEmoteData, bool isLoop)
        {
            if (pEmoteData == null)
                return;

            if (isLoop)
                _standbyAnimCtrl.PlayUserLoopAnim(pEmoteData);
            else
                _standbyAnimCtrl.PlayUserPerformAnim(pEmoteData);
        }

        void PauseAnim()
        {
            cabinPgcUgcPlayController.CancelAnim();
        }
    }
}