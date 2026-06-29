using System;
using System.Collections.Generic;
using Game.Avatar;
using GameData;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class CabinExtendSkinModule : MonoBehaviour
    {
        [SerializeField] TextInputView packNameInput;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] GameObject skinRoleItemPrefab;
        [SerializeField] ExtPackSkinAddItem skinAddItemPrefab;

        Action<CharacterData> _onAvatarChanged;
        CabinCharacterPackInfo _characterInfo;
        SkinPackInfo _currentSkinPackInfo;

        readonly List<SkinRoleItem> _skinRoleItemList = new();
        readonly List<SkinRoleItem> _pool = new();

        public void Init(Action<CharacterData> onAvatarChanged, CabinCharacterPackInfo characterInfo)
        {
            _onAvatarChanged = onAvatarChanged;
            _characterInfo = characterInfo;
            packNameInput.SetOnInput(input => _characterInfo.name = input);
        }

        // 切换选中皮肤：更新 isDefault 标记、刷新列表项高亮并通知 Panel 刷新角色预览
        void OnSkinSelected(SkinPackInfo skinPackInfo)
        {
            if (_currentSkinPackInfo != null)
            {
                _currentSkinPackInfo.isDefault = 0;
            }
            if (skinPackInfo != null)
            {
                skinPackInfo.isDefault = 1;
            }
            _currentSkinPackInfo = skinPackInfo;
            foreach (var item in _skinRoleItemList)
            {
                item.RefreshDefaultState();
            }
            _onAvatarChanged?.Invoke(CharacterData.DeserializeObject(skinPackInfo.avatarJson));
        }

        // 新建皮肤写入包数据，选中后拍照更新封面
        void OnCreatSkinData(CharacterData characterData)
        {
            var newSkin = new SkinPackInfo { avatarJson = CharacterData.SerializeObject(characterData) };
            _characterInfo.skinPack.Add(newSkin);
            OnSkinSelected(newSkin);
            Refresh();
            CabinNetManager.Inst.ReTakePhoto((isSuccess, url) =>
            {
                if (isSuccess)
                {
                    newSkin.cover = url;
                    Refresh();
                }
            });
        }

        public void Refresh()
        {
            RefreshSkinList();
        }

        void RefreshSkinList()
        {
            skinRoleItemPrefab.SetActive(false);
            if (_characterInfo == null)
            {
                return;
            }
            packNameInput.SetInputWithoutNotify(_characterInfo.name ?? "");

            // 把当前列表全部归还对象池
            foreach (var item in _skinRoleItemList)
            {
                item.gameObject.SetActive(false);
                _pool.Add(item);
            }
            _skinRoleItemList.Clear();
            _currentSkinPackInfo = null;

            foreach (var skinPack in _characterInfo.skinPack)
            {
                SkinRoleItem roleItem;
                // 优先从对象池复用，池空则实例化新条目
                if (_pool.Count > 0)
                {
                    roleItem = _pool[_pool.Count - 1];
                    _pool.RemoveAt(_pool.Count - 1);
                }
                else
                {
                    var go = Instantiate(skinRoleItemPrefab, scrollRect.content);
                    roleItem = go.GetComponent<SkinRoleItem>();
                }
                roleItem.gameObject.SetActive(true);
                roleItem.Init(skinPack);
                if (skinPack.isDefault == 1)
                {
                    _currentSkinPackInfo = skinPack;
                }
                roleItem.onSelectAction = OnSkinSelected;
                var capturedRoleItem = roleItem; // 显式捕获当前 item，防止异步回调中引用被循环覆盖
                roleItem.onEditClose = (skinData, charData) =>
                {
                    bool isDataChange = CharacterData.SerializeObject(charData) != skinData.avatarJson;
                    if (!isDataChange)
                    {
                        return;
                    }
                    skinData.avatarJson = CharacterData.SerializeObject(charData);
                    CabinNetManager.Inst.UpdateAvatar(charData);
                    CabinNetManager.Inst.ReTakePhoto((isSuccess, url) =>
                    {
                        capturedRoleItem.RefreshCover(url);
                    });
                };
                roleItem.onDeleteConfirm = (skinData) =>
                {
                    _characterInfo.skinPack.Remove(skinData);
                    Refresh();
                };
                _skinRoleItemList.Add(roleItem);
            }

            skinAddItemPrefab.Init(_characterInfo, OnCreatSkinData);
            skinAddItemPrefab.transform.SetAsLastSibling();
        }
    }
}
