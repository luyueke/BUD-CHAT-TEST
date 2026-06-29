using Es;
using Game.Avatar;
using GameData;
using System;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UI.Catalog;
using UI.Catalog.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Catalog
{
    /// <summary>
    /// 图鉴面板基类
    /// </summary>
    public abstract class BaseCatalogPanel : BasePanel
    {   
        //Close按钮
        public CButton CloseButton;

        private PlayerAnimationCtrl animationCtrl;
        private CharacterWrap characterWrap;
        private GameObject characterWrapObj;

        private PlayerAnimationCtrl otherAnimationCtrl;
        private CharacterWrap otherCharacterWrap;

        public GameObject modelRoot; // 用于放置角色模型的根节点

        //名字
        public Text nameText;

        //解锁进度
        public Text LockNumber;

        //云端数据列表
        public List<Gallery> galleryList;

        //组件ID列表
        public List<CatalogComponentType> SupportedComponentTypes;

        
        [SerializeField] internal AvatarCameraController avatarCameraController;

        //右侧组件对象创建的父类
        public Transform componentCenter;

        //左侧tag对象创建的父类
        public Transform tagCenter;

        //tag对象的实例
        private GameObject characterTagBtPrefab;
        private string characterTagBtPrefabPath = "Assets/Loadable/UI/UIPanel/CataPanel/Cata/CharacterTagItem.prefab";

        //Tag列表
        protected List<CataCharacterTag> characterTagBts;

        //组件列表
        protected List<CatalogComponentBase> catalogComponents;

        // 角色数据列表
        public List<CatalogCharacterData> characterDataList;

        public static BaseCatalogPanel Instant;

        //]红点数量
        public Action<bool> redpointEvent;//]红点数量
        public Action<bool> ugcRedpointEvent; //ugc红点
        int tagRedPointcnt;
        int ugcTagRedPointcnt;
        Dictionary<String, int> actionPointcnt;

        // 当前选中的角色ID
        protected string currentCharacterId;
        
        // 角色对应的动作映射
        protected Dictionary<string, List<string>> characterActionMap = new Dictionary<string, List<string>>();

        public GameObject subViw;

        // 跟踪已注册的角色标签
        protected readonly List<CataCharacterTag> _registeredTags = new List<CataCharacterTag>();
        
        // 跟踪事件注册状态
        private bool _hasRegisteredEvents = false;
        
        // 销毁标志
        private bool _isDestroyed = false;

        bool isugcCata;
        public override void OnShow(params object[] args)
        {   
        }

        public void SetCataData(List<Gallery> _galleryList , bool _isugcCata)
        {
            galleryList = _galleryList;
            isugcCata = _isugcCata;
            UpdateCanvas();
            UpdateRedPoint();
        }



        public override void OnCreate()
        {
            SetData();
            Instant = this;
        }

        public void SetData()
        {
            actionPointcnt = new Dictionary<string, int>();
            characterTagBts = new List<CataCharacterTag>();
            catalogComponents = new List<CatalogComponentBase>();
            characterDataList = new List<CatalogCharacterData>();
            characterTagBtPrefab = XAssetLoaderMgr.Inst.LoadResource<GameObject>(characterTagBtPrefabPath, this.gameObject);
            //componentCenter = gameObject.transform.Find("BaseLayout2D/Right/ScrollView/Viewport/Content");
            //tagCenter = gameObject.transform.Find("BaseLayout2D/Left/ScrollView/Viewport/Content");
            //CloseButton = gameObject.transform.Find("BaseLayout2D/CloseButton").GetComponent<CButton>();
            CloseButton.onClick.AddListener(OnCloseClick);
        }


        /// <summary>
        /// 组件启用时
        /// </summary>
        protected virtual void OnEnable()
        {
        }
        
        /// <summary>
        /// 组件销毁时
        /// </summary>
        protected virtual void OnDestroy()
        {
            _isDestroyed = true;
            _registeredTags.Clear();
        }
        public void UpdateRedPoint()
        {
            tagRedPointcnt = 0;
            ugcTagRedPointcnt = 0;
            // 清空计数字典
            actionPointcnt.Clear();

            // 遍历所有角色和动作
            if(galleryList != null)
            foreach (var gl in galleryList)
            {
                int unclickedCount = 0;
                    if (gl.isLock == 1)
                    {
                        continue;
                    }
                //先判断这个角色点过没有
                var key = AccountDataManager.Inst.Uid + gl.galleryId;
                 if (!PlayerPrefs.HasKey(key))
                 {
                        actionPointcnt[gl.galleryId] = 1;
                        ugcTagRedPointcnt++; // 增加有未点击动作的角色计数
                 }
                
                //再判断这个角色下面的动作判断领了没有
                if (gl.roleGallery!=null&& gl.roleGallery.roleEmote != null)
                foreach (var emote in gl.roleGallery.roleEmote)
                {
                    if (emote.islock == 1) continue;
                    key = AccountDataManager.Inst.Uid + gl.galleryId + emote.emoteId;
                    if (!PlayerPrefs.HasKey(key))
                    {
                        unclickedCount++;
                    }
                }

                // 只有当有未点击的动作时才添加到字典
                if (unclickedCount > 0)
                {
                    actionPointcnt[gl.galleryId] = unclickedCount;
                    tagRedPointcnt++; // 增加有未点击动作的角色计数
                }
            }

            reShowRedPoint();
        }

        public void reShowRedPoint()
        {
            foreach (var c in characterTagBts)
            {
                // 检查是否有未点击的动作
                bool hasUnclickedActions = actionPointcnt.TryGetValue(c.gallery.galleryId, out int count) && count > 0;
                c.ShowRedPoint(hasUnclickedActions);
            }

            // 更新主面板红点
            redpointEvent?.Invoke(tagRedPointcnt > 0);
            ugcRedpointEvent?.Invoke(ugcTagRedPointcnt > 0);

        }
        /// <summary>
        /// 取消所有标签的事件注册
        /// </summary>
        protected void UnregisterAllTags()
        {
            foreach (var tag in _registeredTags)
            {
                if (tag != null)
                {
                    
                }
            }
            _registeredTags.Clear();
        }
        
        /// <summary>
        /// 注册角色标签
        /// </summary>
        public void RegisterTag(CataCharacterTag tag)
        {
            if (tag != null && !_registeredTags.Contains(tag))
            {
                // 避免重复订阅
                
                
                if (!_registeredTags.Contains(tag))
                {
                    _registeredTags.Add(tag);
                }
            }
        }
        

        
     
        
        
        
        /// <summary>
        /// 获取角色的所有动作ID
        /// </summary>
        protected List<string> GetCharacterActionIds(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                return new List<string>();
            }
            
            if (characterActionMap.TryGetValue(characterId, out List<string> actionIds))
            {
                return actionIds;
            }
            
            return new List<string>();
        }
        
        
        public void UpdateCanvas()
        {
            // 加载右侧组件预制体
            LoadComponentPrefabs();
            // 加载左侧角色信息
            LoadCharacterTag();
        }



        


        private void OnCloseClick()
        {
            CloseSelf();
        }

        public override void OnWindowPop()
        {
        }
        private void LoadCharacterTag()
        {
            int lockcnt = 0;
            int unlockcnt = 0;
            //清空现有标签
            if (characterTagBts != null && characterTagBts.Count > 0)
            {
                foreach (var tag in characterTagBts)
                {
                    if (tag != null && tag.gameObject != null)
                    {
                        Destroy(tag.gameObject);
                    }
                }
                characterTagBts.Clear();
            }

            //加载角色
            Gallery firstUnlockedGallery = null;
            foreach (var gallery in galleryList)
            {
                var tag = Instantiate(characterTagBtPrefab, tagCenter);
                var characterTag = tag.GetComponent<CataCharacterTag>();
                if (gallery.isLock != 1)
                {
                    unlockcnt += 1;
                    if (firstUnlockedGallery == null)
                    {
                        firstUnlockedGallery = gallery;
                    }
                }
                lockcnt += 1;
                characterTag.Init(gallery , isugcCata);
                characterTag.clickAction += UpdateComponents;

                // ... 其他代码 ...

                characterTagBts.Add(characterTag);
            }

            // 如果有未锁定的标签，选中第一个未锁定的
            if (firstUnlockedGallery != null)
            {
                UpdateComponents(firstUnlockedGallery);
                InitCharacter(firstUnlockedGallery.roleGallery.roleAvatar);
                foreach (var tag in characterTagBts)
                {
                    if (tag.gallery.galleryId == firstUnlockedGallery.galleryId)
                    {
                        tag.SetSelected(true);
                        break;
                    }
                }
            }

            LockNumber.text = unlockcnt.ToString() + "/" + lockcnt.ToString();
        }

        
        /// <summary>
        /// 加载组件
        void LoadComponentPrefabs() {
            if(SupportedComponentTypes!=null)
            foreach (var componentType in SupportedComponentTypes)
            {
                CreateComponent(componentType);
            }
        }

        public void UpdateComponents(Gallery gallery)
        {   
            nameText.text = gallery.name;
            var key = AccountDataManager.Inst.Uid + gallery.galleryId;
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            // 记录当前选中的角色ID
            currentCharacterId = gallery.galleryId.ToString();
            
            foreach (var component in catalogComponents)
            {
                component.updateUI(gallery);
            }
            
            // 更新标签选中状态 - 找到对应的标签并设置为选中
            if (characterTagBts != null)
            {
                foreach (var tag in characterTagBts)
                {
                    if (tag.gallery.galleryId == gallery.galleryId)
                    {
                        tag.SetSelected(true);
                    }
                    else
                    {
                        tag.SetSelected(false);
                    }
                }
            }
            UpdateRedPoint();
        }


        /// <summary>
        /// 创建组件
        /// </summary>
        protected virtual CatalogComponentBase CreateComponent(CatalogComponentType componentType)
        {
            // 获取预制体路径
            string prefabPath = GetComponentPrefabPath(componentType);
            if (string.IsNullOrEmpty(prefabPath))
            {
                LoggerUtils.LogError($"无法找到组件类型 {componentType} 的预制体配置");
                return null;
            }

            // 加载预制体资源
            GameObject prefab = LoadPrefabAsset(prefabPath);
            if (prefab == null)
            {
                LoggerUtils.LogError($"无法加载组件预制体: {prefabPath}");
                return null;
            }

            // 实例化预制体
            var componentObj = Instantiate(prefab,componentCenter);
            var component = componentObj.GetComponent<CatalogComponentBase>();

            catalogComponents.Add(component);

            return component;
        }

        /// <summary>
        /// 获取组件预制体路径
        /// </summary>
        protected virtual string GetComponentPrefabPath(CatalogComponentType componentType)
        {
            // 这里需要从配置表中获取预制体路径，或者使用预先定义的路径
            // 实际项目中应该使用UI配置表中的路径

            // 示例实现
            switch (componentType)
            {
                case CatalogComponentType.Profession:
                    return "Assets/Loadable/UI/UIPanel/CataPanel/View/ProfessionView.prefab";
                case CatalogComponentType.InterativeAction:
                    return "Assets/Loadable/UI/UIPanel/CataPanel/View/InteractiveActionView.prefab";
                case CatalogComponentType.Plot:
                    return "Assets/Loadable/UI/UIPanel/CataPanel/View/PlotView.prefab";
                default:
                    return null;
            }
        }

        /// <summary>
        /// 加载预制体资源
        /// </summary>
        protected virtual GameObject LoadPrefabAsset(string prefabPath)
        {
            // 使用项目的资源加载系统
            return XAssetLoaderMgr.Inst.LoadResource<GameObject>(prefabPath, this.gameObject);


        }
        private void DestroyCharacter()
        {
            // 1. 先保存引用
            var tempAnimCtrl = animationCtrl;
            var tempWrap = characterWrap;

            // 2. 先清空引用，防止后续访问
            animationCtrl = null;
            characterWrap = null;

            // 3. 停止动画
            if (tempAnimCtrl != null)
            {
                tempAnimCtrl.StopEmoteCo();  // 只停止协程，不调用可能使用characterWrap的方法
            }

            // 4. 销毁角色实例
            if (tempWrap != null && tempWrap.Avatar != null)
            {
                GameObject.Destroy(tempWrap.Avatar.gameObject);
            }
        }



        private void InitOtherCharacter()
        {
            // 如果已经存在,先销毁
            if (otherAnimationCtrl != null)
            {
                otherAnimationCtrl.ResetEmoteForUICharacter();
                otherAnimationCtrl = null;
            }

            if (otherCharacterWrap != null)
            {
                if (otherCharacterWrap.Avatar != null)
                {
                    GameObject.Destroy(otherCharacterWrap.Avatar.gameObject);
                }
                otherCharacterWrap = null;
            }

            if (modelRoot == null)
            {
                LoggerUtils.LogError("modelRoot未设置");
                return;
            }

            // 获取玩家角色数据
            CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
            if (avatarInfo == null)
            {
                LoggerUtils.LogError("无法获取玩家角色数据");
                return;
            }

            // 创建玩家角色
            otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
            otherCharacterWrap.SetParent(characterWrap.Avatar.gameObject.transform, true);
            otherCharacterWrap.Avatar.gameObject.name = "PlayerAvatarPreview";

            // 获取动画控制器
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            if (otherAnimationCtrl == null)
            {
                LoggerUtils.LogError("无法获取PlayerAnimationCtrl组件");
                return;
            }

            // 设置位置
            var sourcePos = characterWrap.Avatar.transform.localPosition;
            otherCharacterWrap.Avatar.transform.localPosition = new Vector3(sourcePos.x + 20, sourcePos.y, sourcePos.z);
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }



        public void InitCharacter(string avatar)
        {
            DestroyCharacter();

            if (modelRoot == null)
            {
                LoggerUtils.LogError("modelRoot未设置");
                return;
            }

            // 创建角色
            CharacterData avatarInfo = CharacterData.DeserializeObject(avatar);
            characterWrap = AvatarController.Inst.CreateUIAvatar(avatarInfo);
            characterWrap.SetParent(modelRoot.transform, true);
            characterWrap.Avatar.gameObject.name = "PlayerAvatarPreview";
            avatarCameraController.RotateTarget = characterWrap.Avatar.gameObject.transform;
            // 获取动画控制器
            animationCtrl = characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
            if (animationCtrl == null)
            {
                LoggerUtils.LogError("无法获取PlayerAnimationCtrl组件");
                return;
            }
        }



        public void ShowAnime(string remoteid)
        {
            if (characterWrap == null || characterWrap.Avatar == null)
            {
                LoggerUtils.LogError("角色未正确初始化");
                return;
            }

            if (animationCtrl == null)
            {
                animationCtrl = characterWrap.Avatar.GetComponent<PlayerAnimationCtrl>();
                if (animationCtrl == null)
                {
                    LoggerUtils.LogError("无法获取PlayerAnimationCtrl组件");
                    return;
                }
            }

            // 获取动画配置
            var emoteConfig = DataTables.GetEmoUIConfig(remoteid);
            if (emoteConfig == null)
            {
                LoggerUtils.LogError($"无法获取动画配置: {remoteid}");
                return;
            }

            // 根据动画类型播放不同的动画
            UIEmoteType emoteType = (UIEmoteType)emoteConfig.emoType;
            switch (emoteType)
            {
                case UIEmoteType.SinglePlayer:
                    // 单人动画
                    if (otherAnimationCtrl != null)
                    {
                        otherAnimationCtrl.gameObject.SetActive(false);
                    }
                    animationCtrl.PlaySingleEmoteForUICharacter(remoteid);
                    break;

                case UIEmoteType.PetSingle:
                    // 双人动画
                    if (otherAnimationCtrl == null)
                    {
                        InitOtherCharacter();
                    }
                    otherAnimationCtrl.gameObject.SetActive(true);
                    // 播放动画
                    animationCtrl.PlayDoubleEmoteForUICharacter(remoteid, otherAnimationCtrl);
                    break;

                default:
                    LoggerUtils.LogError($"不支持的动画类型: {emoteType}");
                    break;
            }
        }

    }
}
 