using System.Collections.Generic;
using System.IO;
using Com.TheFallenGames.OSA.Util.IO;
using Game.COSXML;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.UIPanels.FittingRoom;
//using UnityEditor.Localization.Editor;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour
{
    private Transform custom_parent_1;
    private Transform normal_parent_1;
    private Transform expression_parent_1;

    private GameObject expression_item;

    private Toggle tog_materials;
    private Toggle tog_image;
    private Button custom_add;

    private ScrollRect m_ScrollView;

    private GameObject _pendingAddItem;
    private OCTAvatarExpression _pendingExpr;
    private OCTAvatarExpression AddExpressionData = new OCTAvatarExpression{
        expressionName = "自定义",
        expressionURL = "",
        mType = (int)OCTAvatarExpressionType.materials,
        expressionType = (int)ExpressionType.custom
        };//添加自定的预制数据

    //string[] expressionStrings = {"疲倦", "疑惑", "紧张", "惊讶", "开心", "害羞", "生气","难过", "平静", "宠溺"};

    private void Awake()
    {
        m_ScrollView = GameObjectEx.FindComponentByName<ScrollRect>(transform, "m_ScrollView");
        custom_parent_1 = GameObjectEx.FindComponentByName<Transform>(transform, "custom_parent_1");
        normal_parent_1 = GameObjectEx.FindComponentByName<Transform>(transform, "normal_parent_1");
        expression_parent_1 = GameObjectEx.FindComponentByName<Transform>(transform, "expression_parent_1");
        expression_item = GameObjectEx.FindChildByName(transform, "expression_item").gameObject;
        tog_materials = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_materials");
        tog_image = GameObjectEx.FindComponentByName<Toggle>(transform, "tog_image");
        custom_add = GameObjectEx.FindComponentByName<Button>(transform, "custom_add");
        custom_add.onClick.AddListener(() =>
        {
            // 清理上次未完成的临时 item
            if (_pendingAddItem != null)
            {
                OCTheatreActorEditorDataManager.Inst.RemoveOCTAvatarExpression(_pendingExpr);
                Destroy(_pendingAddItem);
                _pendingAddItem = null;
                _pendingExpr = null;
            }

            var currentMType = tog_materials.isOn ? OCTAvatarExpressionType.materials : OCTAvatarExpressionType.image;
            var existingCustoms = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(
                currentMType, ExpressionType.custom);
            if (existingCustoms.Count >= 6)
            {
                TipPanel.ShowToast("最多只能添加6个自定义表情");
                return;
            }
            string tempName = "自定义" + (existingCustoms.Count + 1);
            var newExpression = new OCTAvatarExpression
            {
                expressionName = tempName,
                expressionURL = "",
                mType = (int)currentMType,
                expressionType = (int)ExpressionType.custom
            };
            // 先加入 data manager，SetExpressionName/SetExpressionURL 内部依赖 Contains 检查
            OCTheatreActorEditorDataManager.Inst.AddOCTAvatarExpression(newExpression);

            // item 隐藏，等 btn_ok 保存成功后（onIconChanged）再显示
            var go = Instantiate(expression_item, custom_parent_1);
            go.SetActive(false);
            go.transform.localPosition = Vector3.zero;
            _pendingAddItem = go;
            _pendingExpr = newExpression;
            var item = go.GetComponent<CharacterViewExpressionItem>();
            item.InitData(newExpression);
            item.onIconChanged = () =>
            {
                go.SetActive(true);
                _pendingAddItem = null;
                _pendingExpr = null;
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollView.content);
            };

            item.ItemOnClick();
        });
        tog_materials.onValueChanged.AddListener((isOn =>
        {
            if (isOn)
            {
                AddExpressionData.mType = (int)OCTAvatarExpressionType.materials;

                var nomalExpressions = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(OCTAvatarExpressionType.materials, ExpressionType.normal);
                InitExpressionList(nomalExpressions,normal_parent_1);

                var expressions = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(OCTAvatarExpressionType.materials, ExpressionType.expression);
                InitExpressionList(expressions,expression_parent_1);

                RefreshCustomItems(OCTAvatarExpressionType.materials);
            }
        }));

        tog_image.onValueChanged.AddListener((isOn =>
        {
            if (isOn)
            {
                #if !UNITY_EDITOR
                if(!VipDataManager.Inst.isVip)
                {
                    //TipPanel.ShowToast("未开通 VIP ");
                    var joinVipType = new List<JoinVipType>(){JoinVipType.Image};
                    var joinVipTitle = "您正在使用的VIP功能：" + "演员编辑器上传图片";
                    UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
                    tog_materials.isOn = true;
                    return;
                }
                #endif
                AddExpressionData.mType = (int)OCTAvatarExpressionType.image;

                var nomalExpressions = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(OCTAvatarExpressionType.image, ExpressionType.normal);
                InitExpressionList(nomalExpressions,normal_parent_1);

                var expressions = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(OCTAvatarExpressionType.image, ExpressionType.expression);
                InitExpressionList(expressions,expression_parent_1);

                RefreshCustomItems(OCTAvatarExpressionType.image);
            }
        }));
            
        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshData);
        MessageHelper.AddListener<OCTAvatarExpression>(MessageName.ActorExpressionItemDeleted, OnCustomItemDeleted);
    }

    private void OnEnable()
    {
        // 激活时主动刷新，防止 Awake 未执行时错过广播
        // toggle Invoke 内部已包含 RefreshCustomItems，不需要再额外调用
        if (tog_materials == null) return;
        if (tog_materials.isOn)
            tog_materials.onValueChanged.Invoke(true);
        else
            tog_image.onValueChanged.Invoke(true);
    }

    private void RefreshData(OCTheatreAvatarInfo acotr)
    {
        var mType = tog_materials.isOn ? OCTAvatarExpressionType.materials : OCTAvatarExpressionType.image;
        UpdateParentItems(normal_parent_1, mType, ExpressionType.normal);
        UpdateParentItems(expression_parent_1, mType, ExpressionType.expression);
        RefreshCustomItems(mType);
    }
   
    private void RefreshCustomItems(OCTAvatarExpressionType mType)
    {
        // 切换到不同 mType 时，清理属于另一个 tab 的 pending item
        // 否则 onIconChanged 触发时会在错误的 tab 里 SetActive(true)
        if (_pendingAddItem != null && _pendingExpr != null && _pendingExpr.mType != (int)mType)
        {
            OCTheatreActorEditorDataManager.Inst?.RemoveOCTAvatarExpression(_pendingExpr);
            Destroy(_pendingAddItem);
            _pendingAddItem = null;
            _pendingExpr = null;
        }

        m_ScrollView.content.gameObject.SetActive(false);

        for (int i = custom_parent_1.childCount - 1; i >= 1; i--)
        {
            var child = custom_parent_1.GetChild(i);
            if (child.gameObject != _pendingAddItem)
            {
                child.gameObject.SetActive(false); // 立即从布局中移除
                Destroy(child.gameObject);         // 帧末销毁，避免 MissingReferenceException
            }
        }

        var expressions = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(mType, ExpressionType.custom);
        foreach (var expr in expressions)
        {
            if (expr == _pendingExpr) continue;
            var go = Instantiate(expression_item, custom_parent_1);
            go.SetActive(true);
            go.transform.localPosition = Vector3.zero;
            var newItem = go.GetComponent<CharacterViewExpressionItem>();
            newItem.InitData(expr);
        }

        m_ScrollView.content.gameObject.SetActive(true);
    }

    private void UpdateParentItems(Transform parent, OCTAvatarExpressionType mType, ExpressionType expType)
    {
        var expressions = OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(mType, expType);
        var items = parent.GetComponentsInChildren<CharacterViewExpressionItem>(true);
        foreach (var item in items)
        {
            if (item.expressionData == null) continue;
            // 直接用引用判断，避免名字匹配导致的错位
            if (expressions.Contains(item.expressionData))
                item.UpdateDisplay(item.expressionData);
        }
    }

    private void OnDisable()
    {
        if (_pendingAddItem != null)
        {
            OCTheatreActorEditorDataManager.Inst?.RemoveOCTAvatarExpression(_pendingExpr);
            Destroy(_pendingAddItem);
            _pendingAddItem = null;
            _pendingExpr = null;
        }
    }

    private void OnCustomItemDeleted(OCTAvatarExpression expr)
    {
        for (int i = 1; i < custom_parent_1.childCount; i++)
        {
            var child = custom_parent_1.GetChild(i);
            var item = child.GetComponent<CharacterViewExpressionItem>();
            if (item != null && item.expressionData == expr)
            {
                child.gameObject.SetActive(false);
                break;
            }
        }
        // 删除 case 没有异步 Destroy 时序问题，直接强制立即重算
        m_ScrollView.content.gameObject.SetActive(false);
        m_ScrollView.content.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)custom_parent_1);
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollView.content);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshData);
        MessageHelper.RemoveListener<OCTAvatarExpression>(MessageName.ActorExpressionItemDeleted, OnCustomItemDeleted);
    }

    private void InitExpressionList( List<OCTAvatarExpression> expressions,Transform itemParent)
        {
            // foreach (Transform child in itemParent)
            // {
            //     Destroy(child.gameObject);
            // }
            itemParent.gameObject.SetActive(false);
            for (int i = 0; i < expressions.Count; i++)
            {
               AddExpressionItem(expressions[i],itemParent);
            }
            itemParent.gameObject.SetActive(true);

        }
    private CharacterViewExpressionItem AddExpressionItem(OCTAvatarExpression expression,Transform itemParent)
    {
        var go = GameObjectEx.FindChildByName(itemParent, expression.expressionName)?.gameObject;
        if(go == null)
            go = Instantiate(expression_item,itemParent);
        go.transform.localPosition = Vector3.zero;
        go.transform.name = expression.expressionName;
        CharacterViewExpressionItem m_item = go.gameObject.GetComponent<CharacterViewExpressionItem>();
        m_item.InitData(expression);
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollView.content);
        return m_item;
    }
    
}
        
    
    
