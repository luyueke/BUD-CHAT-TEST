using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using Message;
using Pb.Base;
using UnityEngine;
using System;
using System.Collections;


namespace Game.Props.PropsBehaviours
{
    /// <summary>
    /// 地图中放置的 AI 伙伴（养成舱 Cabin 伙伴）。
    /// 跨程序集约束：Cabin 数据（CabinCharacterUgcInfo/CabinNetManager）在 UI 程序集，Game 取不到，
    /// 因此本行为层只持基础类型，由 UI 侧编排器（AIBuddyInMapUIManager）拉取 Cabin 数据后回调装配。
    /// 运行态/编辑态加载已存地图时广播 OnAIBuddyInMapNeedSetup（带本行为引用）请求装配；
    /// 编辑态新选择伙伴时由 AIBuddyEditSubView 直接驱动编排器装配。
    /// </summary>
    public class AIBuddyInMapBehaviour : ActorNodeBehaviour
    {
        public AIBuddyInMapComponent aIBuddyInMapComponent;
        private AIBuddyInMapManager manager;

        // AIBuddyAvatarController 中的本地伙伴 id（装配成功后赋值；供 UI 取状态机做待机/口令）
        private string _localBuddyId;
        public string LocalBuddyId => _localBuddyId;

        // 是否已装配出真实 Cabin 伙伴（区别于编辑态白模占位）
        public bool HasBuddy => !string.IsNullOrEmpty(_localBuddyId);

        private string avatarPath = "Assets/Loadable/Avatar/CharacterBody/WhiteCharacter.prefab";
        private GameObject avatarInstanceForEditMode;

        // 盒子挂点：UI 侧渲染盒子模型时挂到这里（盒子无碰撞、不随伙伴双人交互移动）
        private Transform _boxRoot;
        public Transform BoxRoot
        {
            get
            {
                if (_boxRoot == null)
                {
                    var go = new GameObject("BoxRoot");
                    go.transform.SetParent(transform);
                    go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    go.transform.localScale = Vector3.one;
                    _boxRoot = go.transform;
                }
                return _boxRoot;
            }
        }

        public uint EntityUid => entity.GetComp<GameObjectComponent>().Uid;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            aIBuddyInMapComponent = entity.GetOrAddComp<AIBuddyInMapComponent>();

            StartCoroutine(waitOneFrameToLoad());

            manager = GlobalNodeManager.Inst.Get<AIBuddyInMapManager>();
            if (manager != null)
            {
                manager.RegisterBehaviour(this);
                // 已配置伙伴：请求 UI 侧编排器拉取 Cabin 数据装配（编辑态重开地图 / 运行态访客均走此路）
                if (!string.IsNullOrEmpty(aIBuddyInMapComponent.AiBuddyID))
                {
                    manager.QueueAIBuddyRequest(this);
                }
            }
        }

        IEnumerator waitOneFrameToLoad()
        {
            yield return null; // 等待一帧
            // 编辑态且未配置伙伴：显示白模占位，方便选中/摆放
            if (GameController.GetCurrentGameMode() == GameMode.Edit && string.IsNullOrEmpty(aIBuddyInMapComponent.AiBuddyID))
            {
                CreateAvatarInEditMode();
            }
        }

        /// <summary>请求 UI 侧编排器装配（拉 Cabin 数据 → 回调 SetupBuddyByBasics）。</summary>
        public void RequestSetup()
        {
            MessageHelper.Broadcast(MessageName.OnAIBuddyInMapNeedSetup, this);
        }

        private void ClearAvatarChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (_boxRoot != null && child == _boxRoot) continue; // 保留盒子挂点
                Destroy(child.gameObject);
            }
            avatarInstanceForEditMode = null;
        }

        /// 供UGC地图编辑器使用：未配置伙伴时的白模占位
        private void CreateAvatarInEditMode()
        {
            ClearAvatarChildren();
            var avatar = Loader.Load<GameObject>(avatarPath, gameObject);
            avatarInstanceForEditMode = Instantiate(avatar);
            avatarInstanceForEditMode.transform.SetParent(transform);
            avatarInstanceForEditMode.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            avatarInstanceForEditMode.transform.localScale = Vector3.one * 1.7f;
            CapsuleCollider collider = avatarInstanceForEditMode.GetOrAddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.5f, 0);
            collider.radius = 0.25f;
            avatarInstanceForEditMode.layer = LayerMask.NameToLayer("Model");
            PlayerInfo info = new PlayerInfo();
            info.Name = "AI 伙伴";
            var assetWrapper = Loader.Load<GameObject>("Assets/Loadable/UI/UIWidgets/UserInfoView/UserInfoHeadView.prefab");
            var headView = assetWrapper.Instantiate(avatarInstanceForEditMode.transform).GetComponent<UserInfoHeadView>();
            headView.SetPlayerInfo(info);
            headView.ResetPos(SkinType.Avatar, info);
            foreach (Transform child in avatarInstanceForEditMode.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("ShotExclude");
            }
        }

        /// <summary>
        /// UI 编排器回调：用基础类型装配出 Cabin 伙伴形象（原地，保留盒子挂点）。
        /// </summary>
        /// <param name="id">Cabin 伙伴 id（仅用于本地 id 命名）</param>
        /// <param name="name">伙伴名（头顶展示）</param>
        /// <param name="avatarJson">所选皮肤的 avatarJson</param>
        public void SetupBuddyByBasics(string id, string name, string avatarJson, Action onComplete = null)
        {
            var data = CharacterData.DeserializeObject(avatarJson);
            if (data == null)
            {
                onComplete?.Invoke();
                return;
            }
            ClearAvatarChildren();
            // 传 EntityUid → 确定性 key（AINpcInMap_{entityUid}），跨客户端一致，双人交互房间同步用。
            // 预设同款 key：实体机头像资源已缓存→装配回调会在 CreateLocalGameAIBuddy 返回前“同步”触发，
            // 若此时 _localBuddyId 未赋值，回调里 GetPlayerStateCtrl 会取到 null（PC 异步无此问题）。返回值与之相同。
            _localBuddyId = $"{AIBuddyAvatarController.MapBuddyKeyPrefix}{EntityUid}";
            _localBuddyId = AIBuddyAvatarController.Inst.CreateLocalGameAIBuddy(id, name, data, transform, onComplete, EntityUid);
        }

        /// <summary>UI 编排器回调：原地更换皮肤形象（编辑态换皮肤用）。</summary>
        public void RefreshBuddyAvatarByJson(string avatarJson)
        {
            if (string.IsNullOrEmpty(_localBuddyId)) return;
            var data = CharacterData.DeserializeObject(avatarJson);
            if (data == null) return;
            AIBuddyAvatarController.Inst.ChangeLocalGameAIBuddyAvatar(_localBuddyId, data);
        }

        public override string GetTouchName()
        {
            return aIBuddyInMapComponent.ShowText;
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
        }

        public void OnPlay()
        {
            if (avatarInstanceForEditMode)
            {
                avatarInstanceForEditMode.SetActive(false);
            }
        }

        public void OnEdit()
        {
            if (avatarInstanceForEditMode) avatarInstanceForEditMode.SetActive(true);
        }

        private void OnDestroy()
        {
            MessageHelper.Broadcast(MessageName.OnAIBuddyInMapRemoved, this);
            if (manager != null && entity != null && entity.GetComp<GameObjectComponent>() != null)
            {
                manager.UnregisterBehaviour(EntityUid);
            }
        }
    }
}
