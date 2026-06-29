using System.Collections.Generic;
using GameData.BaseInfo;
using Message;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoView : MonoBehaviour
{
    private Transform normal_parent_1;
    private Transform expression_parent_1;
    private Transform expression_parent_1_title;
    private Transform custom_parent_1;
    private Transform custom_parent_1_title;
    private GameObject expression_item;
    public CButton btn_switch;
    public Text txt_switch;

    private readonly Dictionary<OCTAvatarExpression, CharacterViewExpressionItem> _displayItems =
        new Dictionary<OCTAvatarExpression, CharacterViewExpressionItem>();

    private void Awake()
    {
        normal_parent_1 = GameObjectEx.FindComponentByName<Transform>(transform, "normal_parent_1");
        expression_parent_1 = GameObjectEx.FindComponentByName<Transform>(transform, "expression_parent_1");
        expression_parent_1_title = GameObjectEx.FindComponentByName<Transform>(transform, "expression_parent_1_title");

        custom_parent_1 = GameObjectEx.FindComponentByName<Transform>(transform, "custom_parent_1");
        custom_parent_1_title = GameObjectEx.FindComponentByName<Transform>(transform, "custom_parent_1_title");

        expression_item = GameObjectEx.FindChildByName(transform, "expression_item").gameObject;
        btn_switch = GameObjectEx.FindComponentByName<CButton>(transform, "btn_switch");
        expression_item.SetActive(false);

        RefreshItems(null);

        MessageHelper.AddListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshItems);
        MessageHelper.AddListener<OCTAvatarExpression>(MessageName.ActorExpressionItemDeleted, OnExpressionItemDeleted);
       
    }
    OCTheatreAvatarInfo _actor;
    OCTAvatarExpressionType curType = OCTAvatarExpressionType.materials;
    private void RefreshItems(OCTheatreAvatarInfo actor)
    {
        _actor = actor;
        btn_switch.onClick.RemoveAllListeners();
         btn_switch.onClick.AddListener(() =>
        {
            // if(!VipDataManager.Inst.isVip)
            // {
            //     //TipPanel.ShowToast("未开通 VIP ");
            //     var joinVipType = new List<JoinVipType>(){JoinVipType.Image};
            //     var joinVipTitle = "您正在使用的VIP功能：" + "演员编辑器上传图片";
            //     if (joinVipType.Count > 0)
            //     {
            //         UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
            //     }   
            //     return;
            // }
            RefreshItems(_actor);
            if(curType == OCTAvatarExpressionType.materials) 
                curType = OCTAvatarExpressionType.image;
            else 
                curType = OCTAvatarExpressionType.materials;
            });
        
        txt_switch.text = curType == OCTAvatarExpressionType.materials ? "资料" : "图片";
        if(_displayItems != null)
        {
            foreach (var item in _displayItems.Values)
            {
                if(item && item.gameObject)
                    DestroyImmediate(item.gameObject);
            }
            _displayItems.Clear();
        }
        if(actor != null)
        {
            List<OCTAvatarExpression> list = new List<OCTAvatarExpression>();

            list = actor.expressions.FindAll((info) => info.mType == (int)curType && info.expressionType == (int)ExpressionType.normal);
            InitItems(list, normal_parent_1);
                
            list = actor.expressions.FindAll((info) => info.mType == (int)curType && info.expressionType == (int)ExpressionType.expression);
            bool isshow = list.Find((info)=> info.expressionURL != null) != null;
            InitItems(list, expression_parent_1);
            expression_parent_1.gameObject.SetActive(isshow);
            expression_parent_1_title.gameObject.SetActive(isshow);

            list = actor.expressions.FindAll((info) => info.mType == (int)curType && info.expressionType == (int)ExpressionType.custom);
            InitItems(list, custom_parent_1);
            custom_parent_1.gameObject.SetActive(list.Count > 0);
            custom_parent_1_title.gameObject.SetActive(list.Count > 0);
        }
        else
        {
            InitItems(OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(curType, ExpressionType.normal), normal_parent_1);
            InitItems(OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(curType, ExpressionType.expression), expression_parent_1);
            InitItems(OCTheatreActorEditorDataManager.Inst.GetOCTAvatarExpression(curType, ExpressionType.custom), custom_parent_1);
        }
    }

    private void InitItems(List<OCTAvatarExpression> expressions, Transform parent)
    {
        foreach (var expr in expressions)
        {
            var go = Instantiate(expression_item, parent);
            go.SetActive(true);
            go.transform.localPosition = Vector3.zero;
            var item = go.GetComponent<CharacterViewExpressionItem>();
            item.InitDisplayOnly(expr);
            _displayItems[expr] = item;
            if(expr.expressionType == (int)ExpressionType.expression)
            {
                if(string.IsNullOrEmpty(expr.expressionURL))
                {
                     go.SetActive(false);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
        }
    }



    private void OnExpressionItemDeleted(OCTAvatarExpression expr)
    {
        if (expr == null || !_displayItems.TryGetValue(expr, out var item)) return;
        _displayItems.Remove(expr);
        Destroy(item.gameObject);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<OCTheatreAvatarInfo>(MessageName.ActorCardInfoUpdate, RefreshItems);
        MessageHelper.RemoveListener<OCTAvatarExpression>(MessageName.ActorExpressionItemDeleted, OnExpressionItemDeleted);
    }
}
