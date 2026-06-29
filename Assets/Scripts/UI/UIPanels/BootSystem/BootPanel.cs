using DG.Tweening;
using GameUI;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;
namespace Newbie
{

    public class BootPanel : BasePanel<BootPanel>
    {
        public GameObject tipsItem; // Text组件
        public GameObject closeTxt; //提示关闭文案
        public GameObject mask; //阻挡点击的透明膜
        public Transform parant; //蒙版挂在位置
        public Transform textTransform; //变换位置
        public Transform effTransform; //特效位置
        public Transform cubeTransform; //方形位置
        public Transform cycleEffTransform; //圆圈特效位置
        public Transform handEffTransform; //小手掌特效位置

        Vector2 centerScreenPos;
        Vector2 screenSize;
        Vector2 screenPos, screenPos2;


        static public int currId = 1;
        public static bool isPlaying = false;
        public override void OnShow(params object[] args)
        {
            base.OnShow(args);
            if (args.Length > 0)
            {
                int id = (int)args[0];
                SetData(id);
            }
        }
        private void SetData(int id)
        {
            
            isPlaying = true;
            currId = id;
            NewbieTipsInfo tipsData = BootDataManager.Inst.GetNewbieTipsInfo(id);
            BootMaskInfo maskData = BootDataManager.Inst.GetBootMaskInfo(id);
            closeTxt.SetActive(maskData.isClickClose == 0);
            effTransform.gameObject.SetActive(false); //特效位置
            cubeTransform.gameObject.SetActive(false); //方形位置
            cycleEffTransform.gameObject.SetActive(false); //圆圈特效位置
            textTransform.gameObject.SetActive(false); //文字位置
            if (maskData != null)
            {
                StartCoroutine(SetLaterFind(id, maskData, tipsData));
            }

            SetTextItem(tipsData);

            //Debug.Log("BootPanel setdata id=" + currId);
            switch (currId)
            {
                //case 2:
                //    EventTracking.LoadEvent.ReportPopupStatus("6", "guide_done");
                //    break;
                //case 3:
                //    EventTracking.LoadEvent.ReportPopupStatus("9", "guide_done");
                //    break;


                case 103: //首次进入【自定义】页面
                    EventTracking.LoadEvent.ReportPopupStatus("1", "guide_dress_done");
                    break;

                case 111: //1.首次进入创作工作室
                    EventTracking.LoadEvent.ReportPopupStatus("1", "guide_create_done");
                    break;
                case 112: // 3.进入模版编辑界面
                    EventTracking.LoadEvent.ReportPopupStatus("3", "guide_create_done");
                    break;

            }


        }

        void SetTextItem(NewbieTipsInfo tipsData)
        {
            if (tipsData.tipsItems != null && tipsData.tipsItems.Count > 0)
            {
                foreach (Transform child in textTransform)
                {
                    GameObject.Destroy(child.gameObject);
                }
                foreach (var itemData in tipsData.tipsItems)
                {
                    GameObject clonedTextObject = GameObject.Instantiate(tipsItem);
                    clonedTextObject.transform.SetParent(textTransform, false);
                    clonedTextObject.GetComponentInChildren<BootTipsItem>().SetData(itemData);
                }
                // 强制刷新布局
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(textTransform as RectTransform);

                // 如果还不行，延迟一帧再刷新
                StartCoroutine(RefreshLayoutNextFrame(textTransform as RectTransform));
            }
            else
            {
                textTransform.gameObject.SetActive(false);
            }
        }
        IEnumerator RefreshLayoutNextFrame(RectTransform textTransform)
        {
            yield return null;
            // 强制刷新布局
            textTransform.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(textTransform as RectTransform);
        }
        IEnumerator SetLaterFind(int id , BootMaskInfo maskData,  NewbieTipsInfo tipsData)
        {
            yield return null;
            Color maskColor;
            FindMaskMono(id);
            if (TryParseColor(maskData.maskColor, out maskColor))
            {
                GuideMaskUtils.Inst.CreateFullscreenCutoutMaskNormalized(centerScreenPos, screenSize, maskColor, parant, maskData.isClickClose == 1, maskData.isRayTarget == 1);
            }
            else
            {
                GuideMaskUtils.Inst.CreateFullscreenCutoutMaskNormalized(centerScreenPos, screenSize, null, parant, maskData.isClickClose == 1 , maskData.isRayTarget == 1);
            }
            GuideMaskUtils.Inst.removeMaskCallBack += CloseBoot;
            CheckSelectEff(tipsData, id);
        }

        void FindMaskMono(int id)
        {   
            RectTransform v = null;
            var maskMonos = UIManager.Inst.UIRoot.GetComponentsInChildren<BootMaskMono>(true);
            Debug.Log("FindMaskMono id=" + id + ",now=" + Time.realtimeSinceStartup);
            if (maskMonos != null && maskMonos.Length > 0)
            {
                bool find = false;
                foreach (var maskMono in maskMonos)
                {
                    foreach (var _id in maskMono.id)
                    {
                        if (_id == id)
                        {
                            v = maskMono.GetComponent<RectTransform>();
                            find = true;
                            break;
                        }
                    }
                    if (find) break;
                }
                if (!find)
                {
                    v = maskMonos[0].GetComponent<RectTransform>();
                }
            }
            else
            {
                Debug.LogError("找不到：" + id);
                CloseSelf();
                return;
            }


            Vector3[] corners = new Vector3[4];
            v.GetWorldCorners(corners);



            Camera canvasCamera = UIManager.Inst.Canvas.worldCamera;
            screenPos = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
            screenPos2 = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]);




            var scaleFactor = UIManager.Inst.Canvas.scaleFactor;
            screenPos /= scaleFactor;
            screenPos2 /= scaleFactor;
            // 计算屏幕坐标的中心点和大小
            centerScreenPos = (screenPos + screenPos2) * 0.5f;
            screenSize = screenPos2 - screenPos;

            // 确保大小为正值
            screenSize.x = Mathf.Abs(screenSize.x);
            screenSize.y = Mathf.Abs(screenSize.y);
        }

        void CheckSelectEff(NewbieTipsInfo tipsData, int id)
        {   
            int curr = id;
            if (id == 1)
            {
                curr = 1099;
            }
            
            if (id == 109)
            {
                curr = 1010;
            }

            FindMaskMono(curr);
            
            // 假设centerScreenPos是已经被除以scaleFactor的
            float scaleFactor = UIManager.Inst.Canvas.scaleFactor;
            Vector2 realScreenPos = centerScreenPos * scaleFactor;

            Vector2 localPos;
            RectTransform parentRect = parant as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                realScreenPos,
                UIManager.Inst.Canvas.worldCamera,
                out localPos
            );

            // 统一位置：特效和文本都使用相同的localPos
            Vector3 targetPosition = new Vector3(localPos.x, localPos.y, 0);

            if (tipsData.hasEff)
            {
                effTransform.localPosition = targetPosition;
                effTransform.gameObject.SetActive(true);
            }
            else
            {
                effTransform.gameObject.SetActive(false);
            }

            if (tipsData.hasCycleEff)
            {
                if (curr == 105 || curr == 107)
                {
                    cycleEffTransform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
                cycleEffTransform.localPosition = targetPosition;
                cycleEffTransform.gameObject.SetActive(true);

            }
            else
            {
                cycleEffTransform.gameObject.SetActive(false);
            }
            Debug.Log($"BootPanel cycleEffect curId={currId},positon={targetPosition},visible={cycleEffTransform.gameObject.activeSelf},now={Time.realtimeSinceStartup}");
            if (tipsData.hasCudeEff)
            {
                cubeTransform.localPosition = targetPosition;
                RectTransform rectTransform = cubeTransform.gameObject.GetComponent<RectTransform>();

                // 设置宽高
                if (curr == 103)
                {
                    rectTransform.sizeDelta = new Vector2(900, 535);
                }
                else if (curr == 129)
                {
                    rectTransform.sizeDelta = new Vector2(screenSize.x + 100, screenSize.y + 100);
                }
                else
                {
                    rectTransform.sizeDelta = new Vector2(screenSize.x + 100, screenSize.y + 200);
                }
                cubeTransform.gameObject.SetActive(true);
            }
            else
            {
                cubeTransform.gameObject.SetActive(false);
            }

            if (tipsData.hasHandEff)
            {
                handEffTransform.localPosition = targetPosition;
                handEffTransform.gameObject.SetActive(true);
            }
            else
            {
                handEffTransform.gameObject.SetActive(false);
            }
            // textTransform与特效在同一位置
            textTransform.localPosition = targetPosition + new Vector3(tipsData.pos.x, tipsData.pos.y, 0);
            PlayTextPunchAnimation();
        }


        void CloseBoot()
        {
            if (mask != null)
            { 
                mask?.SetActive(false);
            }
            var panel = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
            var gameHallpanel = UIManager.Inst.FindPanel<GameHallPanel>(WindowId.GameHallWindow , PanelId.GameHallPanel);
            GuideMaskUtils.Inst.removeMaskCallBack -= CloseBoot;
            switch (currId)
            {
                case 2:
                    EventTracking.LoadEvent.ReportPopupStatus("8", "guide_ID");
                    SetData(3);
                    break;
                case 3:
                    panel.PlayNewBieAnimation();
                    EventTracking.LoadEvent.ReportPopupStatus("9", "guide_ID");
                    UIManager.Inst.OpenPanelTakeAni(PanelId.NewBieSevenDayV2TaskPanel, 1);
                    EventTracking.LoadEvent.ReportPopupStatus("10", "guide_ID");
                    panel.canShowActivity = true;
                    panel.OpenActivityBtn(true);
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 4:
                    EventTracking.LoadEvent.ReportPopupStatus("12", "guide_ID");
                    SetData(5);
                    break;
                case 5:
                    EventTracking.LoadEvent.ReportPopupStatus("13", "guide_ID");
                    panel.OnBackClick();
                    gameHallpanel.OnWindowShow();
                    isPlaying = false;
                    CloseSelf();
                    UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.GameHallWindow,  6);
                    gameHallpanel.ChangBtAlpha(0, "BtnNewbieV3");
                    break;
                case 6:
                    EventTracking.LoadEvent.ReportPopupStatus("14", "guide_ID");
                    UIManager.Inst.OpenPanel(PanelId.NewBieSevenDayV3TaskPanel);
                    isPlaying = false;
                    CloseSelf();
                    UIManager.Inst.OpenPanel(PanelId.BootPanel, WindowId.EventCenterWindow, 7);
                    break;
                case 7:
                    EventTracking.LoadEvent.ReportPopupStatus("15", "guide_ID");
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 101: //官方-选择衣服
                    EventTracking.LoadEvent.ReportPopupStatus("5", "guide_official_done");
                    SetData(122);
                    break;
                case 102:
                    isPlaying = false;
                    CloseSelf();
                    //完成保存设子
                    EventTracking.LoadEvent.ReportPopupStatus("8", "guide_dress_done");
                    break;
                case 103:
                    //完成初始设子介绍
                    EventTracking.LoadEvent.ReportPopupStatus("2", "guide_dress_done");
                    SetData(104);
                    break;
                case 104:
                    //完成列表设子介绍
                    EventTracking.LoadEvent.ReportPopupStatus("3", "guide_dress_done");
                    SetData(105);
                    break;
                case 105:
                    //完成选中头发页签
                    EventTracking.LoadEvent.ReportPopupStatus("4", "guide_dress_done");
                    panel?.JumpTo(10008);
                    SetData(106);
                    break;
                case 106:
                    //完成选中头发
                    EventTracking.LoadEvent.ReportPopupStatus("5", "guide_dress_done");
                    SetData(107);
                    break;
                case 107:
                    //完成选中衣服页签
                    EventTracking.LoadEvent.ReportPopupStatus("6", "guide_dress_done");
                    panel?.JumpTo(10004);
                    SetData(108);
                    break;
                case 108:
                    //完成选中衣服
                    EventTracking.LoadEvent.ReportPopupStatus("7", "guide_dress_done");
                    SetData(102);
                    break;
                case 109:
                    EventTracking.LoadEvent.ReportPopupStatus("2", "guide_official_done");
                    panel?.JumpTo(MainTabs.Tab.BUD);
                    SetData(110);
                    break;
                case 110:
                    EventTracking.LoadEvent.ReportPopupStatus("4", "guide_official_done");
                    panel?.JumpTo(MainTabs.Tab.Bag, 10004);
                    SetData(101);
                    break;
                case 111:
                    //2.完成选择模板
                    EventTracking.LoadEvent.ReportPopupStatus("2", "guide_create_done");
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 112:
                    //4.完成帮助弹窗示意
                    EventTracking.LoadEvent.ReportPopupStatus("4", "guide_create_done");
                    SetData(113);
                    break;
                case 113:
                    //5.完成引导使用画笔
                    EventTracking.LoadEvent.ReportPopupStatus("5", "guide_create_done");
                    SetData(114);
                    break;
                case 114:
                    //6.完成引导画笔点击衣服
                    EventTracking.LoadEvent.ReportPopupStatus("6", "guide_create_done");
                    SetData(115);
                    break;
                case 115:
                    //7.完成展示画笔效果
                    EventTracking.LoadEvent.ReportPopupStatus("7", "guide_create_done");
                    SetData(116);
                    break;
                case 116:
                    //8.完成引导使用油漆桶
                    EventTracking.LoadEvent.ReportPopupStatus("8", "guide_create_done");
                    var GuiEdit = UIManager.Inst.FindPanel<UGCResourceEditPanel>(PanelId.UGCResourceEditPanel);
                    GuiEdit.ForceSwitchToBucketTool();
                    SetData(117);
                    break;
                case 117:
                    //9.完成引导油漆桶铺色
                    EventTracking.LoadEvent.ReportPopupStatus("9", "guide_create_done");
                    SetData(118);
                    break;
                case 118:
                    //10.完成引导切换背面
                    var GuiEditPanel = UIManager.Inst.FindPanel<UGCResourceEditPanel>(PanelId.UGCResourceEditPanel);
                    GuiEditPanel.OnSwitchPartBtnClick();
                    EventTracking.LoadEvent.ReportPopupStatus("10", "guide_create_done");
                    SetData(119);
                    break;
                case 119:
                    //11.完成引导再次铺色
                    EventTracking.LoadEvent.ReportPopupStatus("11", "guide_create_done");
                    SetData(120);
                    break;
                case 120:
                    SetData(121);
                    //12.完成示意保存
                    EventTracking.LoadEvent.ReportPopupStatus("12", "guide_create_done");
                    break;
                case 121:
                    //13.完成示意发布
                    EventTracking.LoadEvent.ReportPopupStatus("13", "guide_create_done");
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 122:
                    EventTracking.LoadEvent.ReportPopupStatus("6", "guide_official_done");
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 124:
                    EventTracking.LoadEvent.ReportPopupStatus("2", "guide_official_done");
                    SetData(110);
                    break;
                case 125:
                    var GameEntryParkPanel = UIManager.Inst.FindPanel<GameEntryParkPanel>(PanelId.GameEntryParkPanel);
                    GameEntryParkPanel.PlayBtn.onClick.Invoke();
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 126:
                    GameEntrySystem.Inst.OpenParkSelectPanel();
                    SetData(141);
                    break;
                case 127:
                    var GEParkSelectPanel = UIManager.Inst.FindPanel<GEParkSelectPanel>(PanelId.GEParkSelectPanel);
                    GEParkSelectPanel.UgcTog.isOn = true;
                    SetData(128);
                    break;
                case 128:
                    var GEParkSelectPanel2 = UIManager.Inst.FindPanel<GEParkSelectPanel>(PanelId.GEParkSelectPanel);
                    GEParkSelectPanel2.MoreBtn.onClick.Invoke();
                    SetData(129);
                    break;
                case 129:
                    BootDataManager.Inst.SetParkEntry(1);
                    isPlaying = false;
                    CloseSelf();
                    break;
                case 130:
                    AIParkGuideMgr.Inst.ClickGuide(130);
                    CloseSelf();
                    break;
                case 131:
                    AIParkGuideMgr.Inst.ClickGuide(131);
                    CloseSelf();
                    break;
                case 132:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(132);
                    break;
                case 133:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(133);
                    break;
                case 134:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(134);
                    break;
                case 135:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(135);
                    break;
                case 136:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(136);
                    break;
                case 137:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(137);
                    break;
                case 138:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(138);
                    break;
                case 139:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(139);
                    break;
                case 140:
                    CloseSelf();
                    AIParkGuideMgr.Inst.ClickGuide(140);
                    break;
                case 141:
                    SetData(127);
                    break;
                default:
                    isPlaying = false;
                    CloseSelf();
                    break;
            }
        }
        public override void CloseSelf()
        {
            base.CloseSelf();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        bool TryParseColor(string hexString, out Color color)
        {
            // ColorUtility.TryParseHtmlString 是 Unity 官方提供的方法，
            // 能够安全地解析 HTML 颜色代码（例如 #FF0000FF）。
            // 它能自动处理6位（RGB）和8位（RGBA）的格式。
            if (ColorUtility.TryParseHtmlString(hexString, out color))
            {
                return true;
            }

            // 如果解析失败，返回一个默认颜色（白色）。
            color = Color.white;
            return false;
        }

        /// <summary>
        /// 让textTransform从小到大弹动出现
        /// </summary>
        /// <param name="duration">动画持续时间，默认0.5秒</param>
        /// <param name="elasticity">弹性强度，默认0.3f</param>
        /// <param name="vibrato">震动次数，默认10</param>
        public void PlayTextBounceAnimation(float duration = 0.5f, float elasticity = 0.1f, int vibrato = 1)
        {
            if (textTransform == null)
            {
                Debug.LogWarning("textTransform is null, cannot play bounce animation");
                return;
            }

            // 确保textTransform是激活的
            textTransform.gameObject.SetActive(true);

            // 设置初始缩放为0
            textTransform.localScale = Vector3.zero;

            // 创建弹动动画序列
            Sequence bounceSequence = DOTween.Sequence();

            // 第一步：从小到正常大小，使用弹性缓动
            bounceSequence.Append(textTransform.DOScale(Vector3.one, duration)
                .SetEase(Ease.OutElastic, amplitude: elasticity, period: 0.1f));

            // 第二步：添加一个小的弹动效果
            bounceSequence.Append(textTransform.DOScale(Vector3.one * 1.1f, duration * 0.2f)
                .SetEase(Ease.OutQuad));

            // 第三步：回到正常大小
            bounceSequence.Append(textTransform.DOScale(Vector3.one, duration * 0.2f)
                .SetEase(Ease.InOutQuad));

            // 播放序列
            bounceSequence.Play();
        }


        /// <summary>
        /// 让textTransform从小到大弹动出现（带震动效果）
        /// </summary>
        /// <param name="duration">动画持续时间，默认0.5秒</param>
        /// <param name="punchStrength">震动强度，默认0.2f</param>
        /// <param name="vibrato">震动次数，默认10</param>
        /// <param name="elasticity">弹性，默认0.5f</param>
        public void PlayTextPunchAnimation(float duration = 0.6f, float punchStrength = 0f, int vibrato = 2, float elasticity = 0.5f)
        {
            if (textTransform == null)
            {
                Debug.LogWarning("textTransform is null, cannot play punch animation");
                return;
            }

            // 确保textTransform是激活的
            textTransform.gameObject.SetActive(true);

            // 设置初始缩放为0
            textTransform.localScale = Vector3.zero;

            // 创建动画序列
            Sequence punchSequence = DOTween.Sequence();

            // 第一步：从小到正常大小
            punchSequence.Append(textTransform.DOScale(Vector3.one, duration * 0.6f)
                .SetEase(Ease.OutBack));

            // 第二步：添加震动效果
            punchSequence.Append(textTransform.DOPunchScale(Vector3.one * punchStrength, duration * 0.4f, vibrato, elasticity));

            // 播放序列
            punchSequence.Play();
        }
    }


}
