using System;
using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsManagers;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using xasset;

namespace UI.UIPanels.AINPC.AIBuddyInMap
{
    /// <summary>
    /// 地图 AI 伙伴 UI 侧编排器（跨程序集桥）。
    /// 背景：Cabin 数据（CabinCharacterUgcInfo / CabinNetManager）在 UI 程序集，Game 行为层取不到。
    /// 行为层（AIBuddyInMapBehaviour，Game）运行/加载时广播 OnAIBuddyInMapNeedSetup（带自身引用），
    /// 本编排器在 UI 侧拉取 Cabin 数据、解析所选皮肤 avatarJson，再回调行为层用基础类型装配，
    /// 并启动待机、渲染盒子。本体口令缓存在此供世界交互口令面板读取。
    /// </summary>
    public class AIBuddyInMapUIManager : GlobalInstance<AIBuddyInMapUIManager>
    {
        // 每个地图伙伴行为对应的本体数据（口令面板读 voiceCommands；只取本体、不含皮肤卡/创作者编辑配置）
        private readonly Dictionary<AIBuddyInMapBehaviour, CabinCharacterUgcInfo> _buddyInfos = new();
        // 每个 behaviour 当前已请求/装配的 AiBuddyID，用于去重多来源的重复装配（OnEdit/OnPlay 广播、OnInitByCreate 广播、兜底扫描）
        private readonly Dictionary<AIBuddyInMapBehaviour, string> _setupAiId = new();
        // 每个地图伙伴行为对应的盒子模型实例（清理用）
        private readonly Dictionary<AIBuddyInMapBehaviour, GameObject> _boxModels = new();
        // 每个盒子动态创建的像素贴图（清理时统一销毁，防内存泄漏，对标 BudBoxModel._boxTextures）
        private readonly Dictionary<AIBuddyInMapBehaviour, List<Texture2D>> _boxTextures = new();
        // 含图片/文字叠加层的盒子面经合成相机渲染出的 RenderTexture（清理时统一 Release，对标 BudBoxModel._compositorRTs）
        private readonly Dictionary<AIBuddyInMapBehaviour, List<RenderTexture>> _boxRTs = new();
        // 合成相机实例（DrawCamera.prefab），复用单个实例依次为各面合成，Release 时销毁
        private DynamicDrawCanvas _compositor;
        private Font _ugcTextFont;     // UGC 文字字体（best-effort 加载，缺失则用默认字体）
        private bool _fontLoadTried;
        private const float DrawBoardHeight = 925f;    // 与编辑器 DrawBoard 高度一致，用于 mDrawPanel 缩放
        private const string CompositorPrefabPath = "Assets/Loadable/UI/UIPanel/UGCResourceEditPanel/DrawCamera.prefab";
        private const string UgcFontPath = "Assets/Arts/Font/SourceHanSansCN-Regular.otf";

        public override void Initialize()
        {
            base.Initialize();
            MessageHelper.AddListener<AIBuddyInMapBehaviour>(MessageName.OnAIBuddyInMapNeedSetup, OnNeedSetup);
            MessageHelper.AddListener<AIBuddyInMapBehaviour>(MessageName.OnAIBuddyInMapRemoved, OnRemoved);

            // 兜底：编排器初始化时机若晚于伙伴创建（错过广播），主动扫描已注册的伙伴装配
            var manager = GlobalNodeManager.Inst?.Get<AIBuddyInMapManager>();
            if (manager != null)
            {
                foreach (var b in manager.AllBehaviours)
                {
                    if (b != null && b.aIBuddyInMapComponent != null && !string.IsNullOrEmpty(b.aIBuddyInMapComponent.AiBuddyID))
                    {
                        OnNeedSetup(b);
                    }
                }
            }
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<AIBuddyInMapBehaviour>(MessageName.OnAIBuddyInMapNeedSetup, OnNeedSetup);
            MessageHelper.RemoveListener<AIBuddyInMapBehaviour>(MessageName.OnAIBuddyInMapRemoved, OnRemoved);
            foreach (var kv in _boxTextures)
                if (kv.Value != null)
                    foreach (var t in kv.Value)
                        if (t != null) GameObject.Destroy(t);
            foreach (var kv in _boxRTs)
                if (kv.Value != null)
                    foreach (var rt in kv.Value)
                        if (rt != null) rt.Release();
            if (_compositor != null)
            {
                GameObject.Destroy(_compositor.gameObject);
                _compositor = null;
            }
            _buddyInfos.Clear();
            _boxModels.Clear();
            _boxTextures.Clear();
            _boxRTs.Clear();
            _setupAiId.Clear();
        }

        /// <summary>口令面板读取：该地图伙伴的本体数据（含 voiceCommands）。</summary>
        public CabinCharacterUgcInfo GetBuddyInfo(AIBuddyInMapBehaviour behaviour)
        {
            if (behaviour == null) return null;
            return _buddyInfos.TryGetValue(behaviour, out var info) ? info : null;
        }

        // ───────────── 运行/加载态：按 id 拉取 Cabin 数据装配 ─────────────

        private void OnNeedSetup(AIBuddyInMapBehaviour behaviour) => SetupFromId(behaviour);

        /// <summary>按行为层组件里的 ai_id 拉取 Cabin 数据并装配（运行态广播 / 编辑态工具箱放置共用）。</summary>
        public void SetupFromId(AIBuddyInMapBehaviour behaviour)
        {
            if (behaviour == null || behaviour.aIBuddyInMapComponent == null) return;
            var comp = behaviour.aIBuddyInMapComponent;
            if (string.IsNullOrEmpty(comp.AiBuddyID)) return;

            // 去重：同一 behaviour 的同一 AiBuddyID 已在装配/已装配则跳过（多来源重复请求时防重复拉取与重建）
            if (_setupAiId.TryGetValue(behaviour, out var loadedId) && loadedId == comp.AiBuddyID)
                return;
            _setupAiId[behaviour] = comp.AiBuddyID;

            string aiId = comp.AiBuddyID;
            string skinPackId = comp.SkinPackId;
            string boxId = comp.BoxId;
            string boxMetaUrl = comp.BoxMetaUrl;

            CabinNetManager.Inst.GetCabinCharacterInfo(aiId, (isSuccess, detail) =>
            {
                if (behaviour == null) return; // 行为层可能已销毁
                if (!isSuccess || detail?.characterInfo == null)
                {
                    LoggerUtils.LogError($"[AIBuddyInMap] 拉取 Cabin 伙伴失败 id={aiId}");
                    // 拉取失败：清除标记，允许后续重试
                    if (_setupAiId.TryGetValue(behaviour, out var cur) && cur == aiId)
                        _setupAiId.Remove(behaviour);
                    return;
                }
                ApplySetup(behaviour, detail.characterInfo, skinPackId, boxId, boxMetaUrl);
            });
        }

        // ───────────── 编辑态：已有完整 info，直接装配（无需再拉） ─────────────

        /// <summary>编辑态选择/换皮肤后由 AIBuddyEditSubView 调用，直接用已持有的 info 装配。</summary>
        public void SetupFromInfo(AIBuddyInMapBehaviour behaviour, CabinCharacterUgcInfo info, string skinPackId, string boxId, string boxMetaUrl)
        {
            if (behaviour == null || info == null) return;
            _setupAiId[behaviour] = info.id; // 记录已装配 id，避免随后 OnEdit/广播重复装配（换肤同 id 由本方法直接覆盖）
            ApplySetup(behaviour, info, skinPackId, boxId, boxMetaUrl);
        }

        // ───────────── 核心装配 ─────────────

        private void ApplySetup(AIBuddyInMapBehaviour behaviour, CabinCharacterUgcInfo info, string skinPackId, string boxId, string boxMetaUrl)
        {
            // 缓存本体（口令面板用本体 voiceCommands）
            _buddyInfos[behaviour] = info;

            // 解析所选皮肤的 avatarJson 与对应待机：主体皮肤优先，找不到则查扩展包，再不行用默认皮肤
            ResolveSkin(info, skinPackId, (avatarJson, standby) =>
            {
                if (behaviour == null) return;
                if (string.IsNullOrEmpty(avatarJson))
                {
                    LoggerUtils.LogError($"[AIBuddyInMap] 皮肤 avatarJson 解析失败 id={info.id} packId={skinPackId}");
                    return;
                }

                behaviour.SetupBuddyByBasics(info.id, info.GetName(), avatarJson, () =>
                {
                    if (behaviour == null) return;
                    // 待机：复用 self-buddy 的待机逻辑（动画由 AIBuddyStandbyBehaviour 驱动）
                    var stateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(behaviour.LocalBuddyId);
                    if (stateCtrl != null && standby != null)
                    {
                        AIBoxBuddyCallPanel.StartBuddyStandbyOn(stateCtrl, standby, 0f);
                    }
                });

                // 盒子（无碰撞，挂行为层 BoxRoot 下，不随双人交互移动）
                RefreshBox(behaviour, boxId, boxMetaUrl);
            });
        }

        /// <summary>解析所选皮肤的 avatarJson 与待机数据。回调 (avatarJson, standbyEmote)。</summary>
        private void ResolveSkin(CabinCharacterUgcInfo info, string skinPackId, Action<string, PendingEmoteData> onResolved)
        {
            // 1) 主体皮肤包
            if (info.skinPack != null && !string.IsNullOrEmpty(skinPackId))
            {
                var sp = info.skinPack.Find(s => s != null && s.packId == skinPackId);
                if (sp != null)
                {
                    onResolved?.Invoke(sp.avatarJson, info.usingEmote);
                    return;
                }
            }

            // 2) 扩展包皮肤（异步）
            if (!string.IsNullOrEmpty(skinPackId) && info.extensionPackList != null && info.extensionPackList.Count > 0)
            {
                CabinNetManager.Inst.GetExtensionPackBatchInfo(info.extensionPackList, (isS, packList) =>
                {
                    if (isS && packList != null)
                    {
                        foreach (var pack in packList)
                        {
                            if (pack?.skinPack == null) continue;
                            var sp = pack.skinPack.Find(s => s != null && s.packId == skinPackId);
                            if (sp != null)
                            {
                                onResolved?.Invoke(sp.avatarJson, pack.pendingEmote);
                                return;
                            }
                        }
                    }
                    // 扩展包没找到 → 回退默认皮肤
                    ResolveDefaultSkin(info, onResolved);
                });
                return;
            }

            // 3) 默认皮肤
            ResolveDefaultSkin(info, onResolved);
        }

        private void ResolveDefaultSkin(CabinCharacterUgcInfo info, Action<string, PendingEmoteData> onResolved)
        {
            var def = CabinTools.GetDefaultSkin(info.skinPack);
            onResolved?.Invoke(def?.avatarJson, info.usingEmote);
        }

        // ───────────── 盒子渲染 ─────────────

        /// <summary>编辑态盒子 Tab 选择/切换盒子后调用，重渲染盒子。</summary>
        public void RefreshBoxForEdit(AIBuddyInMapBehaviour behaviour, string boxId, string boxMetaUrl)
        {
            RefreshBox(behaviour, boxId, boxMetaUrl);
        }

        private void RefreshBox(AIBuddyInMapBehaviour behaviour, string boxId, string boxMetaUrl)
        {
            ClearBox(behaviour);
            if (behaviour == null || string.IsNullOrEmpty(boxId)) return; // 无盒子

            // 有自定义像素数据用可编辑盒子模型，否则用默认外观（与養成舱 BudBoxModel.LoadBoxModel 一致）。
            // Acrylic 外壳无分部 renderer，无法承载 part.type 索引的贴图，故必须用養成舱盒子模型。
            bool hasMeta = !string.IsNullOrEmpty(boxMetaUrl);
            string prefabPath = hasMeta ? CabinBoxSceneNetManager.modelPath : CabinBoxSceneNetManager.defaultModelPath;
            if (string.IsNullOrEmpty(prefabPath)) return;

            var prefabWrapper = Loader.Load<GameObject>(prefabPath);
            if (prefabWrapper == null || prefabWrapper.request?.asset == null)
            {
                LoggerUtils.LogError("[AIBuddyInMap] 盒子 prefab 加载失败：" + prefabPath);
                return;
            }
            var box = prefabWrapper.Instantiate(behaviour.BoxRoot);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = Vector3.one * 1.1627f; // 地图放置盒子统一缩放

            // 盒子无碰撞体
            foreach (var col in box.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            _boxModels[behaviour] = box;

            if (!hasMeta) return;

            // 异步拉取盒子像素数据，应用到模型材质（复刻 BudBoxModel.LoadBoxModel 的远程加载段）
            var cachedModel = box;
            var request = Asset.LoadRemoteAssetAsync(boxMetaUrl);
            if (request == null) return;
            request.completed += _ =>
            {
                if (behaviour == null || cachedModel == null || request.result != Request.Result.Success)
                {
                    LoggerUtils.LogError($"[AIBuddyInMap-Box] 元数据加载失败 result={request.result} url={boxMetaUrl}");
                    return;
                }
                var text = System.Text.Encoding.UTF8.GetString(request.asset);
                if (string.IsNullOrEmpty(text))
                {
                    LoggerUtils.LogError("[AIBuddyInMap-Box] 元数据内容为空 url=" + boxMetaUrl);
                    return;
                }
                var boxData = JsonConvert.DeserializeObject<UGCBoxSceneData>(text);
                if (boxData != null)
                    ApplyBoxTextures(behaviour, cachedModel, boxData);
            };
        }

        /// <summary>
        /// 将 UGCBoxSceneData 各 part 的像素数据解析为 Texture2D 并绑定到对应 Renderer 材质。
        /// 逐字复刻 BudBoxModel.ApplyBoxTextures，区别仅在于贴图缓存按 behaviour 分桶以便清理。
        /// </summary>
        private void ApplyBoxTextures(AIBuddyInMapBehaviour behaviour, GameObject model, UGCBoxSceneData boxData)
        {
            if (boxData?.parts == null || boxData.parts.Count == 0) return;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            if (!_boxTextures.TryGetValue(behaviour, out var texList))
            {
                texList = new List<Texture2D>();
                _boxTextures[behaviour] = texList;
            }

            // 含文字/图片叠加层的面：先以像素(或透明)底图占位，收集后由协程异步加载图片并合成到 RT
            var overlayParts = new List<(Material mat, Texture2D pixelTex, UGCPartData part)>();

            foreach (var part in boxData.parts)
            {
                bool hasOverlays = (part?.texts != null && part.texts.Count > 0) ||
                                   (part?.photos != null && part.photos.Count > 0);

                if (part?.pixels == null || part.pixels.Count == 0)
                {
                    // 无像素但有叠加层（如 type=5 仅含 photos）：用透明底图走合成管线
                    if (hasOverlays)
                    {
                        int idxNoPixel = (part?.type ?? 0) - 1;
                        if (idxNoPixel < 0 || idxNoPixel >= renderers.Length) continue;
                        var transparentTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                        transparentTex.SetPixels32(new Color32[32 * 32]); // 全透明 RGBA(0,0,0,0)
                        transparentTex.Apply();
                        texList.Add(transparentTex);
                        var matNoPixel = renderers[idxNoPixel].material;
                        ApplyTextureToMaterial(matNoPixel, transparentTex);
                        overlayParts.Add((matNoPixel, transparentTex, part));
                    }
                    continue;
                }

                int rendererIndex = part.type - 1;
                if (rendererIndex < 0 || rendererIndex >= renderers.Length) continue;

                // 根据像素数量判断画布尺寸：≤1024 为 32×32，否则为 64×64
                int canvasSize = part.pixels.Count <= 1024 ? 32 : 64;
                var colors = new Color32[canvasSize * canvasSize];

                foreach (var pixel in part.pixels)
                {
                    var pos = DataUtil.DeSerializeVector2Int(pixel.p);
                    Color col = DataUtil.DeSerializeColor(pixel.col);
                    int idx = pos.y * canvasSize + pos.x;
                    if (idx >= 0 && idx < colors.Length)
                        colors[idx] = col;
                }

                var tex = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
                tex.SetPixels32(colors);
                tex.Apply();
                texList.Add(tex);

                var mat = renderers[rendererIndex].material;
                ApplyTextureToMaterial(mat, tex);

                // 有叠加层：像素底图先占位，排入异步合成队列（合成完成后替换为 RT）
                if (hasOverlays)
                    overlayParts.Add((mat, tex, part));
            }

            if (overlayParts.Count > 0)
                CoroutineManager.Inst.StartCoroutine(CompositePartsAsync(behaviour, model, overlayParts));
        }

        /// <summary>按 shader 名把贴图绑定到正确属性（对标 BudBoxModel.ApplyTextureToMaterial）。</summary>
        private void ApplyTextureToMaterial(Material mat, Texture tex)
        {
            if (mat.shader.name == "Universal Render Pipeline/Lit")
                mat.SetTexture("_BaseMap", tex);
            else if (mat.shader.name == "bud/patterns_ugc_ARI")
                mat.SetTexture("_patterns_tex", tex);
            else
                mat.SetTexture("_MainTex", tex);
        }

        /// <summary>
        /// 异步合成：为每个含图片/文字的面加载图片 → 用合成相机渲染到 256×256 RT → 替换材质贴图。
        /// 逐字复刻 BudBoxModel.CompositePartsAsync，RT 按 behaviour 分桶以便清理。
        /// </summary>
        private IEnumerator CompositePartsAsync(AIBuddyInMapBehaviour behaviour, GameObject model,
            List<(Material mat, Texture2D pixelTex, UGCPartData part)> parts)
        {
            foreach (var (mat, pixelTex, part) in parts)
            {
                if (model == null) yield break; // 盒子已销毁，终止

                // 异步加载该面所有图片
                var photoTextures = new Dictionary<string, Texture>();
                int pendingCount = 0;
                if (part.photos != null)
                {
                    foreach (var pd in part.photos)
                    {
                        if (string.IsNullOrEmpty(pd.photoUrl)) continue;
                        pendingCount++;
                        var url = pd.photoUrl; // 闭包捕获
                        var wrapper = Loader.LoadRemoteImageAsync(url);
                        if (wrapper == null) { pendingCount--; continue; }
                        wrapper.completed += success =>
                        {
                            if (success && model != null)
                            {
                                var loaded = wrapper.RetainAsset(model);
                                if (loaded != null) photoTextures[url] = loaded;
                            }
                            pendingCount--;
                        };
                    }
                }

                while (pendingCount > 0) yield return null;
                if (model == null) yield break;

                var finalRT = CreateCompositeTexture(pixelTex, part, photoTextures);
                if (finalRT == null) continue; // 合成器不可用

                if (!_boxRTs.TryGetValue(behaviour, out var rtList))
                {
                    rtList = new List<RenderTexture>();
                    _boxRTs[behaviour] = rtList;
                }
                rtList.Add(finalRT);
                ApplyTextureToMaterial(mat, finalRT);

                // 像素占位贴图已被 RT 替代，从管理列表移除并销毁
                if (_boxTextures.TryGetValue(behaviour, out var texList))
                    texList.Remove(pixelTex);
                GameObject.Destroy(pixelTex);
            }
        }

        /// <summary>把像素底图 + 文字 + 图片合成到 256×256 RenderTexture（对标 BudBoxModel.CreateCompositeTexture）。</summary>
        private RenderTexture CreateCompositeTexture(Texture2D pixelTex, UGCPartData part, Dictionary<string, Texture> preloadedPhotos)
        {
            EnsureCompositor();
            if (_compositor == null) return null;

            // 重置背景：恢复不透明白、清除残留 material
            _compositor.mRawImage.color = Color.white;
            _compositor.mRawImage.material = null;
            _compositor.mRawImage.texture = pixelTex;

            var drawPanel = _compositor.mDrawPanel;
            for (int i = drawPanel.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(drawPanel.GetChild(i).gameObject);

            // 按 hierarchy 排序保持层叠顺序，与编辑器一致
            var elements = new List<(int hierarchy, bool isText, object data)>();
            if (part.texts != null)
                foreach (var t in part.texts) elements.Add((t.hierarchy, true, t));
            if (part.photos != null)
                foreach (var p in part.photos) elements.Add((p.hierarchy, false, p));
            elements.Sort((a, b) => a.hierarchy.CompareTo(b.hierarchy));

            foreach (var elem in elements)
            {
                if (elem.isText)
                {
                    var td = (TextData)elem.data;
                    var go = new GameObject("UGCText", typeof(Text));
                    go.transform.SetParent(drawPanel, false);
                    var rectTrans = go.GetComponent<RectTransform>();
                    rectTrans.localPosition = FormatUtils.StringToVector3(td.pos);
                    rectTrans.localEulerAngles = FormatUtils.StringToVector3(td.rot);
                    rectTrans.sizeDelta = FormatUtils.StringToVector2(td.sizeDelta);
                    var textComp = go.GetComponent<Text>();
                    textComp.font = EnsureFont();
                    textComp.fontSize = 64;
                    textComp.fontStyle = FontStyle.Bold;
                    textComp.alignment = TextAnchor.MiddleCenter;
                    textComp.text = td.content;
                    textComp.color = FormatUtils.StringToColor(td.color);
                }
                else
                {
                    var pd = (PhotoData)elem.data;
                    var go = new GameObject("UGCPhoto", typeof(RawImage));
                    go.transform.SetParent(drawPanel, false);
                    var rectTrans = go.GetComponent<RectTransform>();
                    rectTrans.localPosition = FormatUtils.StringToVector3(pd.pos);
                    rectTrans.localEulerAngles = FormatUtils.StringToVector3(pd.rot);
                    rectTrans.sizeDelta = FormatUtils.StringToVector2(pd.sizeDelta);
                    if (!string.IsNullOrEmpty(pd.photoUrl) &&
                        preloadedPhotos.TryGetValue(pd.photoUrl, out var loadedTex) && loadedTex != null)
                        go.GetComponent<RawImage>().texture = loadedTex;
                }
            }

            var finalRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            finalRT.Create();
            _compositor.SetCameraTargetTexture(finalRT);
            return finalRT;
        }

        /// <summary>按需创建合成相机实例（复用单个，对标 BudBoxModel.EnsureCompositor），prefab 按路径加载。</summary>
        private void EnsureCompositor()
        {
            if (_compositor != null) return;
            var wrapper = Loader.Load<GameObject>(CompositorPrefabPath);
            if (wrapper == null || wrapper.request?.asset == null)
            {
                LoggerUtils.LogError("[AIBuddyInMap] 合成相机 prefab 加载失败：" + CompositorPrefabPath);
                return;
            }
            var go = wrapper.Instantiate(null);
            _compositor = go.GetComponent<DynamicDrawCanvas>();
            if (_compositor == null)
            {
                LoggerUtils.LogError("[AIBuddyInMap] DrawCamera.prefab 缺少 DynamicDrawCanvas 组件");
                GameObject.Destroy(go);
                return;
            }
            _compositor.mDrawCamera.enabled = false; // 禁用自动渲染，仅手动 Camera.Render
            float scaleRatio = 256f / DrawBoardHeight;
            _compositor.mDrawPanel.localScale = new Vector3(scaleRatio, scaleRatio, 1f);
        }

        /// <summary>best-effort 加载 UGC 文字字体；缺失则返回 null（Text 退默认字体，仅影响带文字的盒子）。</summary>
        private Font EnsureFont()
        {
            if (_ugcTextFont != null) return _ugcTextFont;
            if (_fontLoadTried) return null;
            _fontLoadTried = true;
            var wrapper = Loader.Load<Font>(UgcFontPath);
            if (wrapper != null && wrapper.request?.asset != null)
                _ugcTextFont = wrapper.request.asset as Font;
            return _ugcTextFont;
        }

        private void ClearBox(AIBuddyInMapBehaviour behaviour)
        {
            if (behaviour == null) return;
            // 先销毁动态贴图再销毁模型，防内存泄漏
            if (_boxTextures.TryGetValue(behaviour, out var texList))
            {
                if (texList != null)
                    foreach (var t in texList)
                        if (t != null) GameObject.Destroy(t);
                _boxTextures.Remove(behaviour);
            }
            // 释放合成管线产生的 RenderTexture
            if (_boxRTs.TryGetValue(behaviour, out var rtList))
            {
                if (rtList != null)
                    foreach (var rt in rtList)
                        if (rt != null) rt.Release();
                _boxRTs.Remove(behaviour);
            }
            if (_boxModels.TryGetValue(behaviour, out var box))
            {
                if (box != null) GameObject.Destroy(box);
                _boxModels.Remove(behaviour);
            }
        }

        private void OnRemoved(AIBuddyInMapBehaviour behaviour)
        {
            if (behaviour == null) return;
            ClearBox(behaviour);
            _buddyInfos.Remove(behaviour);
            _setupAiId.Remove(behaviour);
        }
    }
}
