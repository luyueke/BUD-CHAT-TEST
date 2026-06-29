using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Message;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{

    /// <summary>
    /// 养成舱皮肤卡片列表项，展示单个皮肤包的封面与名称。
    /// 继承自 IncubationDraftBaseItem，复用封面加载、选中高亮、Badge 等通用逻辑。
    /// 额外支持皮肤锁定状态（LockObj 需在 Inspector 中绑定锁定遮罩 GameObject）。
    /// </summary>
    public class CabinSkinCardItem : IncubationDraftBaseItem
    {
        /// <summary>锁定遮罩对象；未拥有该皮肤时显示，已拥有或自己创建时隐藏</summary>
        [SerializeField] private GameObject LockObj;

        // "当前装备"标记节点（prefab 下名为 Current 的子物体，默认隐藏），按名懒查找
        private GameObject _currentObj;
        private bool _currentObjResolved;

        /// <summary>
        /// 根据当前绑定的数据刷新锁定状态。
        /// 自己创建的皮肤不显示锁定；未购买的皮肤显示锁定遮罩。
        /// </summary>
        public void RefreshLock()
        {
            // 优先使用 targetUgcId（商品原始UGCid）作为背包查询 key，与 RefreshBaseMsg 中的逻辑保持一致。
            // GetCabinCharacterInfo 返回的 id 是发布记录ID，targetUgcId 才是 BagDatabase 中的存储 key。
            var ugcId = _data?.id;
            if (_data is CabinCharacterUgcInfo ugcInfo && !string.IsNullOrEmpty(ugcInfo.targetUgcId))
                ugcId = ugcInfo.targetUgcId;
            CabinSkinLockHelper.ApplyLock(LockObj, ugcId, _data?.creator);
        }

        /// <summary>直接控制锁定遮罩显隐（用于"主体随召唤拥有，强制解锁"等场景）。</summary>
        public void SetLockVisible(bool locked)
        {
            if (LockObj != null) LockObj.SetActive(locked);
        }

        /// <summary>显隐"当前装备"标记（prefab 下 Root/Current 子节点，默认隐藏）。</summary>
        public void SetCurrent(bool isCurrent)
        {
            if (!_currentObjResolved)
            {
                // 优先按已知路径 Root/Current，找不到再在所有子级里按名递归查找
                var t = transform.Find("Root/Current") ?? FindDeep(transform, "Current");
                _currentObj = t != null ? t.gameObject : null;
                _currentObjResolved = true;
            }
            if (_currentObj != null) _currentObj.SetActive(isCurrent);
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>加载皮肤专属卡面底图（SkinSprite 目录）</summary>
        protected override void SetCardColor(DraftBoxCardColorConfig config)
        {
            if (config == null)
                return;
            var path = $"Assets/Loadable/UI/UIPanel/IncubationCabinDraftBox/SkinSprite/{config.Color}.png";
            var sprite = XAssetLoaderMgr.Inst.LoadResource<Sprite>(path, gameObject);
            if (sprite != null)
                CardImg.sprite = sprite;
        }
    }
}

