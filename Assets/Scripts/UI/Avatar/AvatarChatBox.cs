using System;
using System.Collections.Generic;
using DG.Tweening;
using EventTracking;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;

namespace UI.Avatar
{
    public class AvatarChatBox : MonoBehaviour
    {
        [SerializeField] private BUD_Text contentTxt;
        [SerializeField] private SpriteRenderer bgSpRender;
        [SerializeField] private Color originColor;
        private Sprite cur_BgSprite;

        float animatorDuration = 0.2f; // 气泡显示、移动、消失的动画时长
        float showTime = 10f; // 单条聊天消息显示时长，超过就走消失逻辑
        Action<AvatarChatBox> onDisappearAction;
        BudTimer autoDisappearTimer;
        private const float originFade = 1f;
        Dictionary<string, GameObject> bubEffDict;
        //气泡框参数
        private float paddingX = 0.2f;
        private float paddingY = 0.2f;
        private Vector2 minSize; // 动态计算的最小尺寸
        
        public int thisBundleId;

        private void Start()
        {
            LoadEvent.ReportTask(150, 0);
        }

        /// <summary>
        /// 根据当前背景Sprite的九宫格边界计算最小尺寸
        /// </summary>
        private void CalculateMinSize()
        {
            if (cur_BgSprite == null)
            {
                Debug.LogError("背景Sprite未设置！");
                minSize = Vector2.zero;
                return;
            }

            // 获取边界信息（以像素为单位）
            Vector4 border = cur_BgSprite.border; // left, bottom, right, top

            // 获取Sprite的Pixels Per Unit
            float ppu = cur_BgSprite.pixelsPerUnit;

            // 将边界从像素转换为世界单位
            float left = border.x / ppu;
            float right = border.z / ppu;
            float bottom = border.y / ppu;
            float top = border.w / ppu;
            
            // 计算最小宽度和最小高度
            float minWidth = left + right;
            float minHeight = bottom + top;

            minSize = new Vector2(minWidth, minHeight);
            Debug.LogError(JsonConvert.SerializeObject(minSize));
        }

        public void SetBubbleId(int bubbleId)
        {
            thisBundleId = bubbleId;
            var data = UserUIWidgetManager.Inst.GetGameChatBubbleData(bubbleId, this.gameObject);
            cur_BgSprite = data.Sp;
            if (data.CornerEffectPrefabPaths != null)
            {
                if (bubEffDict == null)
                {
                    bubEffDict = new Dictionary<string, GameObject>();
                }
                else
                {
                    foreach(var eff in bubEffDict)
                    {
                        Destroy(eff.Value);
                    }
                    bubEffDict.Clear();
                }
                foreach (var assetPath in data.CornerEffectPrefabPaths)
                {
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        LoggerUtils.LogError("预制体路径未指定!");
                        continue;
                    }
                    if(bubEffDict!=null && bubEffDict.ContainsKey(assetPath))//不重复创建
                    {
                        continue;
                    }
                    var path = UserUIWidgetManager.Inst.GetGameChatBubbleEffPath(bubbleId) + assetPath + "_Game.prefab";
                    AssetWrapper<GameObject> wrapper = Loader.Load<GameObject>(path);
                    // 检查加载是否成功，并从 wrapper.request.asset 获取预制体
                    if (wrapper != null && wrapper.request != null && wrapper.request.isDone && wrapper.request.result == xasset.Request.Result.Success)
                    {
                        GameObject loadedPrefab = wrapper.request.asset as GameObject;

                        if (loadedPrefab != null)
                        {
                            var Obj = Instantiate(loadedPrefab, bgSpRender.gameObject.transform, false);
                            bubEffDict.Add(assetPath, Obj);
                        }
                        else
                        {
                            LoggerUtils.LogError($"通过 Loader 加载成功，但 wrapper.request.asset 为空: {assetPath}");
                            continue;
                        }
                    }
                }
            }
            else
            {
                if (bubEffDict != null)
                {
                    foreach (var eff in bubEffDict)
                    {
                        Destroy(eff.Value);
                    }
                    bubEffDict.Clear();
                }
            }
            bgSpRender.sprite = cur_BgSprite;
            originColor = data.DefColor;
            paddingX = data.Boarder.x;
            paddingY = data.Boarder.y;
            minSize = data.MiniSize;
            contentTxt.color = data.TxtColor;
        }

        /// <summary>
        /// 设置聊天文本，并调整气泡尺寸
        /// </summary>
        /// <param name="str">聊天内容</param>
        public void SetText(string str)
        {
            contentTxt.text = str;
            contentTxt.Rebuild();

            // 获取文本的渲染边界
            Vector2 textSize = contentTxt.rawBottomRightTextBounds;

            float newW = Mathf.Abs(textSize.x) + paddingX;
            float newH = Mathf.Abs(textSize.y) + paddingY;

            Vector2 desiredSize = new Vector2(newW, newH);

            // 确保气泡尺寸不小于最小尺寸
            Vector2 finalSize = new Vector2(
                Mathf.Max(desiredSize.x, minSize.x),
                Mathf.Max(desiredSize.y, minSize.y)
            );

            bgSpRender.size = finalSize;
            if (bubEffDict != null)
            {
                if(thisBundleId == 31 || thisBundleId == 29)  return;
                foreach (var k in bubEffDict)
                {
                    switch (k.Key)
                    {
                        case "TopLeft":
                        case "LeftUp":
                            k.Value.transform.localPosition = new Vector3(-(bgSpRender.size.x / 2 - 0.35f), (bgSpRender.size.y / 2 - 0.3f), 0);
                            break;
                        case "TopRight":
                        case "RightUp":
                            k.Value.transform.localPosition = new Vector3((bgSpRender.size.x / 2  - 0.25f), (bgSpRender.size.y / 2 - 0.25f), 0);
                            break;
                        case "BottomLeft":
                        case "LeftDown":
                            k.Value.transform.localPosition = new Vector3(-(bgSpRender.size.x / 2 - 0.35f), -(bgSpRender.size.y / 2 - 0.25f), 0);
                            if (thisBundleId == 14)
                            {
                                k.Value.transform.localPosition = new Vector3(-(bgSpRender.size.x / 2 - 0.35f) + 0.26f, 0, 0);
                            }
                            break;
                        case "BottomRight":
                        case "RightDown":
                            k.Value.transform.localPosition = new Vector3((bgSpRender.size.x / 2  - 0.25f), -(bgSpRender.size.y / 2 - 0.3f), 0);
                            if (thisBundleId == 14)
                            {
                                k.Value.transform.localPosition = new Vector3((bgSpRender.size.x / 2 - 0.65f), -(bgSpRender.size.y / 2 - 0.25f)+0.25f, 0);
                            }
                            break;
                        default:
                            LoggerUtils.Log("气泡特效命名错误！");
                            break;
                    }
                    if (thisBundleId == 18)
                    {
                        k.Value.transform.localPosition += new Vector3(0, -0.15f, -0.2f);
                    }
                }
            }
        }

        public void AddDisappearListener(Action<AvatarChatBox> action)
        {
            onDisappearAction = action;
        }

        public float GetHeight()
        {
            return bgSpRender.size.y;
        }

        public void Appear()
        {
            Debug.Log("AvatarChatBox日志出现 " + transform.name);
            bgSpRender?.DOKill();
            contentTxt?.DOKill();
            t2?.Kill();
            t2 = null;
            gameObject.SetActive(true);
            contentTxt.fade = originFade;
            bgSpRender.color = originColor;

            // 启动定时器
            if (autoDisappearTimer != null)
            {
                TimerManager.Inst.Stop(autoDisappearTimer);
                autoDisappearTimer = null;
            }
            autoDisappearTimer = TimerManager.Inst.RunOnce("AvatarChatBox.AutoDisappear", showTime, OnAutoDisappear);

            var h = GetHeight();
            this.transform.DOLocalMoveY(h / 2, animatorDuration).SetRelative(true);
        }

        public void Move(float offsetY)
        {
            this.transform.DOLocalMoveY(offsetY, animatorDuration).SetRelative(true);
        }

        public void Disappear(float offsetY)
        {
            Move(offsetY);
            Disappear();
        }

        Tweener t2;
        public void Disappear()
        {
            if (autoDisappearTimer != null)
            {
                TimerManager.Inst.Stop(autoDisappearTimer);
                autoDisappearTimer = null;
            }

            //bgSpRender?.DOKill();
            //contentTxt?.DOKill();

            if (t2 == null)
            {
                Debug.Log("AvatarChatBox日志消失 " + transform.name);

             //   bgSpRender?.DOColor(Color.clear, animatorDuration);

                t2 = DOTween.To(() => contentTxt.fade, x => contentTxt.fade = x, 0, animatorDuration);
                //t2.SetTarget(contentTxt);

                t2.OnComplete(() =>
                {
                    t2?.Kill();
                    t2 = null;
            //        bgSpRender?.DOKill();
                    gameObject.SetActive(false);
                    onDisappearAction?.Invoke(this);
                });
            }
            else
            {
                Debug.LogError("AvatarChatBox 正在执行消失");
            }
        }

        void OnAutoDisappear()
        {
            Disappear(GetHeight() / 2);
        }
        private void OnDisable()
        {
            this.transform?.DOKill();
            bgSpRender?.DOKill();
            contentTxt?.DOKill();
            t2?.Kill();
            t2 = null;
            if (autoDisappearTimer != null)
            {
                TimerManager.Inst.Stop(autoDisappearTimer);
                autoDisappearTimer = null;
            }
        }
        private void OnDestroy()
        {
            this.transform?.DOKill();
            bgSpRender?.DOKill();
            contentTxt?.DOKill();
            t2?.Kill();
            t2 = null;
            onDisappearAction = null;
            if (autoDisappearTimer != null)
            {
                TimerManager.Inst.Stop(autoDisappearTimer);
                autoDisappearTimer = null;
            }
        }
    }
}
