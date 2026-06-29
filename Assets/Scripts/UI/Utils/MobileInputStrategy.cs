using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Message;
using UnityEngine.UI;
using DG.Tweening;

public enum InputStrategyType{
    Honking, //喇叭
    Skill1, // 技能1
    Skill2, // 技能2
    Control, // 释放/控制
    Jump, // 跳跃
    Skill3, // 技能3
    Skill4, // 技能4

    ShowBanner, // 展示或隐藏
    ShowBannerName, // 改名
    Skill5, // 技能5（娃娃机抓取）
    Skill6, // 技能6
    //后面根据需求自己扩容枚举，要去预制体上配置按钮引
}

public class MobileInputStrategy : MonoBehaviour
{
    // 1. 定义一个配置结构体，代替写死的变量
    [Serializable]
    public struct InputBinding
    {
        [Header("类型")]
        public InputStrategyType type;
        public ClickEventListener button;
        [Header("是否开启Toggle")]
        public bool isToggle;
        public GameObject toggleNormal;
        public GameObject togglePressed;
        [Header("是否开启冷却")]
        public bool isCoolDown;
        [Header("冷却图片")]
        public Image coolDownImage;
        [Header("剩余次数")]
        public Text remainingTimesText;
    }

    protected struct RunTimeToggle{
        public bool isToggled;
        public GameObject toggleNormal;
        public GameObject togglePressed;
    }

    // 在面板上直接配置这个列表，想加多少按钮都可以
    [SerializeField] private List<InputBinding> inputBindings = new List<InputBinding>();

    // 优化：使用 int 作为 Key 避免任何潜在的 Enum 装箱
    private Dictionary<int, InputStrategyType> goIdToTypeMap = new Dictionary<int, InputStrategyType>();
    
    // 事件存储
    private Dictionary<int, Action> pointerDownEventMap = new Dictionary<int, Action>();
    private Dictionary<int, Action> pointerUpEventMap = new Dictionary<int, Action>();
    private Dictionary<int, RunTimeToggle> togglePressedMap = new Dictionary<int, RunTimeToggle>();

    private Dictionary<InputStrategyType, Image> coolDownImageMap = new Dictionary<InputStrategyType, Image>();
    private Dictionary<InputStrategyType, Text> remainingTimesTextMap = new Dictionary<InputStrategyType, Text>();
    public void Init()
    {
        // 2. 统一初始化逻辑，拒绝复制粘贴
        foreach (var binding in inputBindings)
        {
            if (binding.button == null) continue;

            // FIX: ClickEventListener 回调传递的是 GameObject，所以这里必须用 gameObject.GetInstanceID()
            // Component.GetInstanceID() != GameObject.GetInstanceID()
            int goID = binding.button.gameObject.GetInstanceID();
            int typeID = (int)binding.type;

            // 建立 GameObject ID -> Enum 类型的映射
            if (!goIdToTypeMap.ContainsKey(goID))
            {
                goIdToTypeMap.Add(goID, binding.type);
                
                // 注册底层点击回调
                binding.button.AddPointerDownHandler(OnPointerDownCallback);
                binding.button.AddPointerUpHandler(OnPointerUpCallback);

                if(binding.isToggle){
                    binding.button.AddClickEventHandler(OnPointerClickedCallback);
                    togglePressedMap.Add(goID, new RunTimeToggle{
                        isToggled = false,
                        toggleNormal = binding.toggleNormal,
                        togglePressed = binding.togglePressed
                    });
                }

                if(binding.isCoolDown && binding.coolDownImage != null){
                    coolDownImageMap.Add(binding.type, binding.coolDownImage);
                    binding.coolDownImage.fillAmount = 0;
                    binding.coolDownImage.gameObject.SetActive(false);
                }

                if(binding.remainingTimesText != null){
                    remainingTimesTextMap.Add(binding.type, binding.remainingTimesText);
                    binding.remainingTimesText.text = "";
                    binding.remainingTimesText.gameObject.SetActive(false);
                }
            }
        }

        //业务需求
        MessageHelper.AddListener<float>(MessageName.OnJumpInputCoolDown, OnJumpInputCoolDownCallback);
        MessageHelper.AddListener<float>(MessageName.OnSkill1CoolDown, OnSkill1CoolDownCallback);
        MessageHelper.AddListener<float>(MessageName.OnSkill2CoolDown, OnSkill2CoolDownCallback);
        MessageHelper.AddListener<float>(MessageName.OnHonkingCoolDown, OnHonkingCoolDownCallback);
        MessageHelper.AddListener<float>(MessageName.OnSkill5CoolDown, OnSkill5CoolDownCallback);
        MessageHelper.AddListener<int>(MessageName.OnJumpRemainingTimes, UpdateJumpRemainingTimes);
        MessageHelper.AddListener<int>(MessageName.OnSkill1RemainingTimes, UpdateSkill1RemainingTimes);
        MessageHelper.AddListener<int>(MessageName.OnSkill2RemainingTimes, UpdateSkill2RemainingTimes);
    }

    private void OnDestroy() {
        MessageHelper.RemoveListener<float>(MessageName.OnJumpInputCoolDown, OnJumpInputCoolDownCallback);
        MessageHelper.RemoveListener<float>(MessageName.OnSkill1CoolDown, OnSkill1CoolDownCallback);
        MessageHelper.RemoveListener<float>(MessageName.OnSkill2CoolDown, OnSkill2CoolDownCallback);
        MessageHelper.RemoveListener<float>(MessageName.OnHonkingCoolDown, OnHonkingCoolDownCallback);
        MessageHelper.RemoveListener<float>(MessageName.OnSkill5CoolDown, OnSkill5CoolDownCallback);
        MessageHelper.RemoveListener<int>(MessageName.OnJumpRemainingTimes, UpdateJumpRemainingTimes);
        MessageHelper.RemoveListener<int>(MessageName.OnSkill1RemainingTimes, UpdateSkill1RemainingTimes);
        MessageHelper.RemoveListener<int>(MessageName.OnSkill2RemainingTimes, UpdateSkill2RemainingTimes);
    }

    public void SetVisible(InputStrategyType type, bool isVisible)
    {
        foreach(var binding in inputBindings){
            if(binding.type == type){
                binding.button.gameObject.SetActive(isVisible);
                if(binding.isToggle){
                    binding.toggleNormal.SetActive(isVisible);
                    binding.togglePressed.SetActive(isVisible);
                }
                if(binding.isCoolDown){
                    binding.coolDownImage.gameObject.SetActive(isVisible);
                }
            }
        }
    }

    /// <summary>置灰/恢复某按钮（用 CanvasGroup 变暗 + 禁止点击）。不改变显隐，仅控制可用性。
    /// 用于：没可抓目标时把 Skill5 置灰，有目标立即恢复。</summary>
    public void SetButtonEnabled(InputStrategyType type, bool enabled){
        foreach(var binding in inputBindings){
            if(binding.type != type || binding.button == null) continue;
            var go = binding.button.gameObject;
            var cg = go.GetComponent<CanvasGroup>();
            if(cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = enabled ? 1f : 0.6f; // 0.6：比 0.4 更明显，但仍明显变暗=不可按
            cg.interactable = enabled;
            cg.blocksRaycasts = enabled;
        }
    }

    public void HideAllButtons(IList<InputStrategyType> exceptTypes){
        foreach(var binding in inputBindings){
            if(!exceptTypes.Contains(binding.type)){
                binding.button.gameObject.SetActive(false);
                if(binding.isToggle){
                    binding.toggleNormal.SetActive(false);
                    binding.togglePressed.SetActive(false);
                }
            }
        }
    }

    public void ShowAllButtons(IList<InputStrategyType> exceptTypes){
        foreach(var binding in inputBindings){
            if(!exceptTypes.Contains(binding.type)){
                binding.button.gameObject.SetActive(true);
            }
        }
    }

    private void OnJumpInputCoolDownCallback(float coolDown)
    {
        HandleCoolDown(InputStrategyType.Jump, coolDown);
    }

    private void OnSkill1CoolDownCallback(float coolDown)
    {
        HandleCoolDown(InputStrategyType.Skill1, coolDown);
    }

    private void OnSkill2CoolDownCallback(float coolDown)
    {
        HandleCoolDown(InputStrategyType.Skill2, coolDown);
    }

    private void OnHonkingCoolDownCallback(float coolDown)
    {
        HandleCoolDown(InputStrategyType.Honking, coolDown);
    }

    private void OnSkill5CoolDownCallback(float coolDown)
    {
        HandleCoolDown(InputStrategyType.Skill5, coolDown);
    }

    private void UpdateJumpRemainingTimes(int remainingTimes)
    {
        if(remainingTimesTextMap.TryGetValue(InputStrategyType.Jump, out Text text)){
            if(remainingTimes > 1){ text.gameObject.SetActive(true); }
            text.text = remainingTimes.ToString();
            text.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        }
    }

    private void UpdateSkill1RemainingTimes(int remainingTimes)
    {
        if(remainingTimesTextMap.TryGetValue(InputStrategyType.Skill1, out Text text)){
            text.text = remainingTimes.ToString();
        }
    }

    private void UpdateSkill2RemainingTimes(int remainingTimes)
    {
        if(remainingTimesTextMap.TryGetValue(InputStrategyType.Skill2, out Text text)){
            text.text = remainingTimes.ToString();
        }
    }

    private void HandleCoolDown(InputStrategyType type, float coolDown){
        if(coolDownImageMap.TryGetValue(type, out Image image)){
            image.fillAmount = 1;
            image.gameObject.SetActive(true);
            //使用dotween在coolDown区间内从1到0
            DOTween.To(() => image.fillAmount, x => image.fillAmount = x, 0, coolDown).SetEase(Ease.Linear).OnComplete(() => {
                image.gameObject.SetActive(false);
            });
        }
    }
    // ---------------------- 外部调用接口 ----------------------
    public bool IsButtonExist(InputStrategyType type)
    {
        // 遍历性能稍差，但由于通常按钮数量极少(<20)，比维护一个额外的 List 更省内存和逻辑
        foreach (var binding in inputBindings)
        {
            if (binding.type == type && binding.button != null) return true;
        }
        return false;
    }

    public void AddButtonDownEvent(InputStrategyType type, Action action)
    {
        AddEventSafe(pointerDownEventMap, (int)type, action);
    }

    public void RemoveButtonDownEvent(InputStrategyType type, Action action)
    {
        RemoveEventSafe(pointerDownEventMap, (int)type, action);
    }
    
    public void AddButtonUpEvent(InputStrategyType type, Action action)
    {
        AddEventSafe(pointerUpEventMap, (int)type, action);
    }

    public void RemoveButtonUpEvent(InputStrategyType type, Action action)
    {
        RemoveEventSafe(pointerUpEventMap, (int)type, action);
    }

    // ---------------------- 内部逻辑与安全封装 ----------------------

    // 通用添加方法：安全处理 Dictionary Key 不存在的情况
    private void AddEventSafe(Dictionary<int, Action> map, int key, Action action)
    {
        if (map.ContainsKey(key))
        {
            map[key] += action;
        }
        else
        {
            map.Add(key, action);
        }
    }

    // 通用移除方法
    private void RemoveEventSafe(Dictionary<int, Action> map, int key, Action action)
    {
        if (map.ContainsKey(key))
        {
            map[key] -= action;
            // 可选：如果委托为空了，移除 Key 以节省内存
            if (map[key] == null) map.Remove(key);
        }
    }

    // ---------------------- 回调处理 ----------------------

    private void OnPointerDownCallback(GameObject go, PointerEventData eventData)
    {
        HandleCallback(go, pointerDownEventMap);
    }

    private void OnPointerUpCallback(GameObject go, PointerEventData eventData)
    {
        HandleCallback(go, pointerUpEventMap);
    }

    private void OnPointerClickedCallback(GameObject go, PointerEventData eventData){
        int goID = go.GetInstanceID();
        if(togglePressedMap.TryGetValue(goID, out RunTimeToggle toggle)){
            toggle.isToggled = !toggle.isToggled;
            toggle.togglePressed.SetActive(toggle.isToggled);
            toggle.toggleNormal.SetActive(!toggle.isToggled);
        }
    }

    private void HandleCallback(GameObject go, Dictionary<int, Action> eventMap)
    {
        int goID = go.GetInstanceID();

        // 1. 确认这是一个注册过的按钮
        if (goIdToTypeMap.TryGetValue(goID, out InputStrategyType type))
        {
            // 2. 确认该类型是否有注册监听事件（安全查找）
            if (eventMap.TryGetValue((int)type, out Action callback))
            {
                callback?.Invoke();
            }
        }
    }
}
