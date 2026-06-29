using System;
using System.Collections;
using System.Collections.Generic;
using Basic;
using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.Props;
using Game.Props.PropsBehaviours;
using Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIParkHUDPanel : MonoBehaviour
{
    public AIParkNpcHudItem HudItemPrefab;
    public Transform HudItemParent;

    private Camera mainCamera;
    private Camera uiCamera;
    private RectTransform canvasRect;

    // 缓存已创建的HUD项
    private List<AIParkNpcHudItem> hudItems = new List<AIParkNpcHudItem>();
    // 当前活跃的HUD项数量
    private int activeHudItemCount = 0;

    [SerializeField] private float offsetY = 4f;

    /// <summary>
    /// 角色高度，计算是否在相机内时用的脚底点，加上高度才准确点
    /// </summary>
    [SerializeField] private float characterHeight = 1.7f;

    bool _bCheckRaycast = true;
    public bool bCheckRaycast
    {
        set
        {
            if (!value)
            {
                foreach (var item in hudItems)
                {
                    item.SetActive(false);
                }
            }
            _bCheckRaycast = value;
        }
        get{
            return _bCheckRaycast;
        }
    }

    public void Init()
    {
        // 获取射线检测组件并添加监听
        AvatarRaycast avtRaycast = AvatarController.Inst.SelfAvatarRaycast;
        avtRaycast.OnRaycast.AddListener(OnAvatarRaycast);

        // 获取相机引用
        mainCamera = GlobalCameraManager.Inst.GlobalMainCamera;
        uiCamera = GlobalCameraManager.Inst.UICamera;

        // 获取Canvas的RectTransform
        canvasRect = transform.parent.GetComponent<RectTransform>();
        // 初始化HUD项池
        InitHudItemPool();
        bCheckRaycast = true;
    }

    private void InitHudItemPool()
    {
        // 预创建一些HUD项以提高性能
        int preInitCount = 5;
        for (int i = 0; i < preInitCount; i++)
        {
            CreateHudItem();
        }
    }

    private AIParkNpcHudItem CreateHudItem()
    {
        // 创建HUD项实例
        AIParkNpcHudItem item = Instantiate(HudItemPrefab, HudItemParent);
        item.SetActive(false);
        hudItems.Add(item);
        return item;
    }

    private void OnAvatarRaycast(Collider[] colliders)
    {
        if(!bCheckRaycast){
            return;
        }
        // 获取视野内的医院NPC
        List<AIPark_CharacterBehaviour> visibleCharacters = GetAllVisibleParkCharacters(colliders);

        // 更新并显示HUD项，返回已使用的HUD项引用
        List<AIParkNpcHudItem> usedItems = UpdateHudItems(visibleCharacters);

        // 只隐藏未使用的HUD项，而不是全部隐藏再显示
        foreach (var item in hudItems)
        {
            if (!usedItems.Contains(item))
            {
                item.SetActive(false);
            }
        }

        // 重置活跃计数为已使用的数量
        activeHudItemCount = usedItems.Count;
    }

    private void ResetHudItems()
    {
        // 此方法不再使用，保留以防其他地方调用
        activeHudItemCount = 0;
    }

    private List<AIParkNpcHudItem> UpdateHudItems(List<AIPark_CharacterBehaviour> characters)
    {
        List<AIParkNpcHudItem> usedItems = new List<AIParkNpcHudItem>();
        Dictionary<AIPark_CharacterBehaviour, AIParkNpcHudItem> existingPairs = new Dictionary<AIPark_CharacterBehaviour, AIParkNpcHudItem>();

        // 第一步：检查已激活的HUD项，看是否有对应的NPC仍在可见列表中
        foreach (var item in hudItems)
        {
            if (item.gameObject.activeSelf)
            {
                AIPark_CharacterBehaviour npc = item.GetCurrentNpc();
                if (npc != null && characters.Contains(npc))
                {
                    existingPairs[npc] = item;
                    usedItems.Add(item);

                    item.UpdateDecisionRate();
                    // 只更新位置，不重新设置数据，避免动画中断
                    UpdateHudItemPosition(item, npc.GetHeadBone().position);
                }
            }
        }

        // 第二步：为新的可见NPC分配或创建HUD项
        foreach (var character in characters)
        {
            // 如果这个NPC已经有对应的HUD项，跳过
            if (existingPairs.ContainsKey(character))
                continue;

            // 获取或创建新的HUD项
            AIParkNpcHudItem hudItem = GetAvailableHudItem();

            // 设置HUD项数据
            hudItem.SetData(character);

            // 更新HUD项位置
            //UpdateHudItemPosition(hudItem, character.transform.position + offset);

            // 显示HUD项
            if (hudItem.GetCurrentNpc()?.CanShowInteractBtn == true)
            {
                hudItem.SetActive(true);
                // 添加到已使用列表
                usedItems.Add(hudItem);
            }

        }

        return usedItems;
    }

    private AIParkNpcHudItem GetAvailableHudItem()
    {
        // 尝试查找一个未激活的HUD项
        foreach (var item in hudItems)
        {
            if (!item.gameObject.activeSelf)
            {
                return item;
            }
        }

        // 如果没有未激活的，创建一个新的
        return CreateHudItem();
    }

    private void UpdateHudItemPosition(AIParkNpcHudItem hudItem, Vector3 worldPosition)
    {
        if (mainCamera == null) return;

        if (hudItem.GetCurrentNpc()?.CanShowInteractBtn == false)
        {
            hudItem.SetActive(false);
        }

        // 世界坐标转屏幕坐标
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition + Vector3.up * offsetY); // 偏移以显示在NPC头顶上方

        // 屏幕坐标转UI本地坐标
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out localPos);

        // 设置HUD项位置
        hudItem.transform.localPosition = localPos;
    }

    /// <summary>
    /// 获取视野范围内所有的AIPark_CharacterBehaviour
    /// </summary>
    private List<AIPark_CharacterBehaviour> GetAllVisibleParkCharacters(Collider[] colliders)
    {
        List<AIPark_CharacterBehaviour> visibleCharacters = new List<AIPark_CharacterBehaviour>();

        if (colliders == null || colliders.Length == 0)
        {
            return visibleCharacters;
        }

        foreach (var collider in colliders)
        {
            // 直接查找AIPark_CharacterBehaviour组件
            AIPark_CharacterBehaviour[] characters = collider.GetComponentsInParent<AIPark_CharacterBehaviour>();

            foreach (var character in characters)
            {
                // 检查是否在视野内
                if (IsInView(character.transform.position + Vector3.up * characterHeight))
                {
                    visibleCharacters.Add(character);
                }
            }
        }

        return visibleCharacters;
    }

    /// <summary>
    /// 判断一个世界坐标点是否在相机视野内
    /// </summary>
    /// <param name="worldPosition">要检测的世界坐标</param>
    /// <returns>是否在视野内</returns>
    private bool IsInView(Vector3 worldPosition)
    {
        if (mainCamera == null) return false;

        // 转换为视口坐标
        Vector2 viewPos = mainCamera.WorldToViewportPoint(worldPosition);

        // 计算点积来判断是否在相机前方
        Vector3 dir = (worldPosition - mainCamera.transform.position).normalized;
        float dot = Vector3.Dot(mainCamera.transform.forward, dir);

        // 在视口范围内且在相机前方
        return dot > 0 && viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1;
    }


    private void OnDestroy()
    {
        // 清理事件监听
        if (AvatarController.Inst != null && AvatarController.Inst.SelfAvatarRaycast != null)
        {
            AvatarController.Inst.SelfAvatarRaycast.OnRaycast.RemoveListener(OnAvatarRaycast);
        }
    }
}
