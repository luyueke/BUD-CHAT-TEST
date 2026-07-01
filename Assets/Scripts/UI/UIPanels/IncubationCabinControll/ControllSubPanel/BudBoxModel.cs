using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.BudBox;
using GameData;
using GameData.BaseInfo;
using GameData.UGCData;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using xasset;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// 盒子场景预览视图组件：封装角色模型创建、盒子 3D 模型加载与纹理应用逻辑。
    /// 外部通过 <see cref="Setup"/> 传入角色数据和场景数据，组件负责驱动模型生成和销毁。
    /// 可挂载在 3D 布局节点（BaseLayout3D）上，与 UI 面板脚本解耦。
    /// </summary>
    public class BudBoxModel : MonoBehaviour
    {
        /// <summary>角色模型挂载节点</summary>
        [SerializeField] private Transform CharacterRoot;

        /// <summary>盒子 3D 模型挂载节点</summary>
        [SerializeField] private Transform BoxModelRoot;

        /// <summary>渲染到 RT 的专用相机（预制件中已预配置 targetTexture）</summary>
        [SerializeField] private Camera PhotoCamera;

        /// <summary>
        /// 文字+图片合成相机预制件（DrawCamera.prefab），内含 DynamicDrawCanvas。
        /// 需在 Inspector 中赋值。
        /// </summary>
        [SerializeField] private DynamicDrawCanvas _compositorPrefab;

        /// <summary>
        /// UGC 文字元素使用的字体（SourceHanSansCN-Regular.otf）。
        /// 需在 Inspector 中赋值，与编辑器 UGCText 保持一致。
        /// </summary>
        [SerializeField] private Font _ugcTextFont;

        private CharacterWrap _characterWrap;
        private PlayerAnimationCtrl _animationCtrl;

        /// <summary>角色播放控制器，负责待机动画驱动</summary>
        private readonly CabinPgcUgcPlayController _cabinPlayCtrl = new CabinPgcUgcPlayController();

        /// <summary>盒子 3D 模型实例</summary>
        private GameObject _boxModel;

        /// <summary>动态创建的盒子贴图列表，Clear 时统一销毁以防内存泄漏</summary>
        private readonly List<Texture2D> _boxTextures = new List<Texture2D>();

        /// <summary>
        /// 合成器实例（DrawCamera.prefab 的运行时实例）。
        /// 复用单个实例依次为各面生成合成贴图，在 DestroyBoxModel 时销毁。
        /// </summary>
        private DynamicDrawCanvas _compositor;

        /// <summary>
        /// 合成管线产生的 RenderTexture 列表，Clear 时统一 Release 防止内存泄漏。
        /// </summary>
        private readonly List<RenderTexture> _compositorRTs = new List<RenderTexture>();

        /// <summary>
        /// DrawBoard 高度常量，与编辑器 UGCBoxSceneEditorPanel 的 DrawBoard.rect.height 一致，
        /// 用于计算 mDrawPanel 缩放比：scaleRatio = FinalTextureSize / DrawBoardHeight = 256 / 925。
        /// </summary>
        private const float DrawBoardHeight = 925f;

        #region 对外接口

        /// <summary>当前是否已创建角色模型</summary>
        public bool HasCharacter => _characterWrap != null;

        /// <summary>
        /// 设置3D预览的屏幕状态。isOff=true 时显示熄屏效果，false 时恢复正常。
        /// 当前为接口桩，视觉效果待后续实现。
        /// </summary>
        /// <param name="isOff">true=熄屏，false=亮屏</param>
        public void SetScreenOff(bool isOff)
        {
            // TODO: 实现熄屏视觉效果（遮罩/着色器切换等）
        }

        /// <summary>角色动画播放控制器，供外部驱动预览动画（如唤醒、待机、口令）</summary>
        public CabinPgcUgcPlayController PlayController => _cabinPlayCtrl;

        /// <summary>
        /// 根据传入的角色数据和场景数据，创建角色模型并加载盒子 3D 模型。
        /// 角色创建完成后自动播放默认待机动画。调用前会先清理旧的模型实例。
        /// </summary>
        /// <param name="characterInfo">设备绑定的角色 UGC 数据，为 null 时跳过角色创建</param>
        /// <param name="sceneInfo">选中的盒子场景数据，含 metaDataUrl 用于加载纹理</param>
        public void Setup(CabinCharacterUgcInfo characterInfo, BoxSceneInfo sceneInfo)
        {
            CreateCharacter(characterInfo);
            LoadBoxModel(sceneInfo?.metaDataUrl);
        }

        /// <summary>
        /// 单独加载（或刷新）盒子 3D 模型，不影响已创建的角色模型。
        /// 适用于角色和场景分步异步加载的场景（如 CabinControllBoxPanel）。
        /// </summary>
        /// <param name="metaDataUrl">场景元数据 URL；为 null 或空时仅展示空白模型</param>
        public void LoadBoxScene(string metaDataUrl)
        {
            LoadBoxModel(metaDataUrl);
        }

        /// <summary>
        /// 根据皮肤包信息直接创建 UI 角色，不自动播放待机动画。
        /// 适用于需要外部驱动后续动画逻辑的场景（如口令预览、唤醒预览）。
        /// 调用前会先清理旧的角色实例。
        /// </summary>
        /// <param name="skinPack">皮肤包数据，为 null 或 avatarJson 为空时跳过创建</param>
        public void SetupCharacterFromSkinPack(SkinPackInfo skinPack)
        {
            DestroyCharacter();

            if (skinPack == null || string.IsNullOrEmpty(skinPack.avatarJson))
                return;

            var characterData = CharacterData.DeserializeObject(skinPack.avatarJson);

            if (characterData == null)
                return;

            _characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, CharacterRoot);
            _animationCtrl = _characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();

            // 仅初始化控制器，不自动播放待机，由外部按需触发预览动画
            _cabinPlayCtrl.Init(_animationCtrl, _characterWrap, null, null);
        }

        /// <summary>
        /// 销毁角色模型、盒子模型及动态贴图，释放所有运行时资源。
        /// </summary>
        public void Clear()
        {
            DestroyCharacter();
            DestroyBoxModel();
        }

        #endregion

        #region Unity 生命周期

        /// <summary>组件销毁时确保资源全部释放，防止内存泄漏。</summary>
        private void OnDestroy()
        {
            DestroyCharacter();
            DestroyBoxModel();
        }

        #endregion

        #region 角色模型

        /// <summary>
        /// 根据角色 UGC 数据创建带 IK 控制器的 UI 预览角色，并播放默认待机动画。
        /// </summary>
        /// <param name="characterInfo">角色 UGC 数据，包含皮肤包列表</param>
        private void CreateCharacter(CabinCharacterUgcInfo characterInfo)
        {
            DestroyCharacter();

            if (characterInfo == null)
                return;

            // 优先取默认皮肤包，否则取第一个
            var pack = characterInfo.skinPack?.Find(p => p.isDefault == 1) ?? characterInfo.skinPack?[0];

            if (pack == null || string.IsNullOrEmpty(pack.avatarJson))
                return;

            var characterData = CharacterData.DeserializeObject(pack.avatarJson);
            _characterWrap = AvatarController.Inst.CreateUIAvatarWithIKController(characterData, CharacterRoot);
            _animationCtrl = _characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();

            // 不传 AvatarCameraController，禁止用户旋转视角；直接播放默认站立待机动画
            _cabinPlayCtrl.Init(_animationCtrl, _characterWrap, null, null);
            _cabinPlayCtrl.PlayLeisureIdle();
        }

        /// <summary>销毁角色模型并释放相关引用，同时取消正在播放的动画。</summary>
        private void DestroyCharacter()
        {
            if (_characterWrap != null)
            {
                _cabinPlayCtrl.CancelAnim();
                Destroy(_characterWrap.CustomAvatar);
                _characterWrap = null;
                _animationCtrl = null;
            }
        }

        #endregion

        #region 盒子 3D 模型

        /// <summary>
        /// 根据 metaDataUrl 是否有效，选择加载默认盒子模型或可自定义纹理的盒子模型。
        /// metaDataUrl 为 null 或空时加载 <see cref="DefaultBoxModelPath"/> 指定的默认外观；
        /// 有值时加载 <see cref="BoxModelPath"/> 并异步拉取纹理数据应用到材质。
        /// </summary>
        /// <param name="metaDataUrl">场景元数据 URL，来源可以是 BoxSceneInfo 或 CharacterBoxInfo</param>
        private void LoadBoxModel(string metaDataUrl)
        {
            DestroyBoxModel();

            // metaDataUrl 为空时加载默认盒子外观
            if (string.IsNullOrEmpty(metaDataUrl))
            {
                if (string.IsNullOrEmpty(CabinBoxSceneNetManager.defaultModelPath))
                    return;

                var defaultPrefab = Loader.Load<GameObject>(CabinBoxSceneNetManager.defaultModelPath);

                if (defaultPrefab == null)
                {
                    LoggerUtils.LogError("[BudBoxModel] 默认盒子 Prefab 加载失败，路径=" + CabinBoxSceneNetManager.defaultModelPath);
                    return;
                }

                _boxModel = defaultPrefab.Instantiate(BoxModelRoot);
                _boxModel.transform.localPosition = Vector3.zero;
                return;
            }

            // 有 metaDataUrl 时加载可编辑盒子并应用自定义纹理
            var prefab = Loader.Load<GameObject>(CabinBoxSceneNetManager.modelPath);

            if (prefab == null)
            {
                LoggerUtils.LogError("[BudBoxModel] 盒子 Prefab 加载失败，路径=" + CabinBoxSceneNetManager.modelPath);
                return;
            }

            _boxModel = prefab.Instantiate(BoxModelRoot);
            _boxModel.transform.localPosition = Vector3.zero;

            var cachedModel = _boxModel;
            var request = Asset.LoadRemoteAssetAsync(metaDataUrl);

            if (request == null)
                return;

            request.completed += _ =>
            {
                if (cachedModel == null || request.result != Request.Result.Success)
                {
                    LoggerUtils.LogError($"[BudBoxModel] 元数据加载失败，result={request.result}，url={metaDataUrl}");
                    return;
                }

                var text = System.Text.Encoding.UTF8.GetString(request.asset);

                if (string.IsNullOrEmpty(text))
                {
                    LoggerUtils.LogError($"[BudBoxModel] 元数据内容为空，url={metaDataUrl}");
                    return;
                }

                var boxData = JsonConvert.DeserializeObject<UGCBoxSceneData>(text);

                if (boxData != null)
                {
                    LoggerUtils.Log($"[BudBoxModel] 元数据解析成功，parts 数量={boxData.parts?.Count ?? 0}");
                    ApplyBoxTextures(cachedModel, boxData);
                }
                else
                {
                    LoggerUtils.LogError($"[BudBoxModel] 元数据 JSON 反序列化失败，url={metaDataUrl}");
                }
            };
        }

        /// <summary>
        /// 将 UGCBoxSceneData 中各 part 的像素数据解析为 Texture2D，并绑定到对应 Renderer 的材质。
        /// 无覆盖层的 part 直接上像素贴图；含文字或图片的 part 先以像素贴图占位，
        /// 再由协程 <see cref="CompositePartsAsync"/> 异步加载图片后完成合成渲染。
        /// 若 part 无像素数据但有叠加层（如 type=5 仅有 photos），则创建透明底图走合成管线。
        /// </summary>
        /// <param name="model">盒子 3D 模型实例</param>
        /// <param name="boxData">从 metaDataUrl 解析得到的盒子场景数据</param>
        private void ApplyBoxTextures(GameObject model, UGCBoxSceneData boxData)
        {
            if (boxData?.parts == null || boxData.parts.Count == 0)
                return;

            var renderers = model.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
                return;

            // 需要异步合成的 part：(材质, 像素贴图, partData)
            var overlayParts = new List<(Material mat, Texture2D pixelTex, UGCPartData part)>();

            LoggerUtils.Log($"[BudBoxModel] ApplyBoxTextures 开始，renderers={renderers.Length}，parts={boxData.parts.Count}");

            foreach (var part in boxData.parts)
            {
                if (part?.pixels == null || part.pixels.Count == 0)
                {
                    // 无像素数据时，检查是否仍有叠加层（文字/图片）需要合成（例如 type=5 仅含 photos）
                    bool hasOnlyOverlays = (part?.texts != null && part.texts.Count > 0) ||
                                           (part?.photos != null && part.photos.Count > 0);

                    if (hasOnlyOverlays && _compositorPrefab != null)
                    {
                        int rendererIdx = (part?.type ?? 0) - 1;

                        if (rendererIdx < 0 || rendererIdx >= renderers.Length)
                        {
                            LoggerUtils.LogError($"[BudBoxModel] part type={part?.type} 超出 renderers 范围（renderers={renderers.Length}），跳过");
                            continue;
                        }

                        // 创建全透明 32×32 底图作为 Canvas 背景，让合成器只渲染叠加层内容
                        var transparentTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                        transparentTex.SetPixels32(new Color32[32 * 32]); // 默认全零 = RGBA(0,0,0,0)
                        transparentTex.Apply();

                        var matNoPixel = renderers[rendererIdx].material;
                        _boxTextures.Add(transparentTex);
                        ApplyTextureToMaterial(matNoPixel, transparentTex);
                        overlayParts.Add((matNoPixel, transparentTex, part));
                        LoggerUtils.Log($"[BudBoxModel] part type={part?.type} 无像素背景但有叠加层，用透明底图加入合成队列（texts={part?.texts?.Count ?? 0}，photos={part?.photos?.Count ?? 0}）");
                    }
                    else
                    {
                        LoggerUtils.Log($"[BudBoxModel] 跳过 part type={part?.type}（无像素数据且无叠加层，texts={part?.texts?.Count ?? 0}，photos={part?.photos?.Count ?? 0}）");
                    }

                    continue;
                }

                int rendererIndex = part.type - 1;

                if (rendererIndex < 0 || rendererIndex >= renderers.Length)
                {
                    LoggerUtils.LogError($"[BudBoxModel] part type={part.type} 超出 renderers 范围（renderers={renderers.Length}），跳过");
                    continue;
                }

                // 根据像素数量判断画布尺寸：≤1024 为 32×32，否则为 64×64
                int canvasSize = part.pixels.Count <= 1024 ? 32 : 64;
                var colors = new Color32[canvasSize * canvasSize];

                foreach (var pixel in part.pixels)
                {
                    var pos = DataUtil.DeSerializeVector2Int(pixel.p);
                    Color col = DataUtil.DeSerializeColor(pixel.col);
                    int idx = pos.y * canvasSize + pos.x;

                    if (idx >= 0 && idx < colors.Length)
                    {
                        colors[idx] = col;
                    }
                }

                var pixelTex = new Texture2D(canvasSize, canvasSize, TextureFormat.RGBA32, false);
                pixelTex.SetPixels32(colors);
                pixelTex.Apply();

                var mat = renderers[rendererIndex].material;

                // 有文字或图片时，先以像素贴图占位，再排入异步合成队列
                bool hasOverlays = (part.texts != null && part.texts.Count > 0) ||
                                   (part.photos != null && part.photos.Count > 0);

                if (hasOverlays && _compositorPrefab != null)
                {
                    // 像素贴图作为占位（合成完成后会被替换为 RenderTexture）
                    _boxTextures.Add(pixelTex);
                    ApplyTextureToMaterial(mat, pixelTex);
                    overlayParts.Add((mat, pixelTex, part));
                    LoggerUtils.Log($"[BudBoxModel] part type={part.type} 加入合成队列（pixels={part.pixels.Count}，texts={part.texts?.Count ?? 0}，photos={part.photos?.Count ?? 0}，shader={mat.shader.name}）");
                }
                else
                {
                    // 无覆盖层：直接使用像素 Texture2D，纳入 _boxTextures 统一管理
                    _boxTextures.Add(pixelTex);
                    ApplyTextureToMaterial(mat, pixelTex);
                    LoggerUtils.Log($"[BudBoxModel] part type={part.type} 直接应用像素贴图（{canvasSize}×{canvasSize}，shader={mat.shader.name}）");
                }
            }

            // 有需要合成的面时启动异步协程
            if (overlayParts.Count > 0)
            {
                LoggerUtils.Log($"[BudBoxModel] 启动异步合成协程，待合成面数={overlayParts.Count}");
                StartCoroutine(CompositePartsAsync(overlayParts));
            }
        }

        /// <summary>
        /// 异步合成协程：依次为每个含文字/图片的 part 加载图片资源，全部就绪后执行合成渲染。
        /// 图片通过 <see cref="Loader.LoadRemoteImageAsync"/> 异步加载，不阻塞主线程。
        /// 合成完成后将材质从像素贴图替换为 256×256 RenderTexture，并销毁不再需要的像素贴图。
        /// </summary>
        /// <param name="parts">需合成的 part 列表：(材质引用, 像素贴图, partData)</param>
        private IEnumerator CompositePartsAsync(List<(Material mat, Texture2D pixelTex, UGCPartData part)> parts)
        {
            foreach (var (mat, pixelTex, part) in parts)
            {
                // 模型已被销毁时终止协程
                if (this == null || _compositorPrefab == null)
                {
                    LoggerUtils.Log("[BudBoxModel] CompositePartsAsync：组件已销毁，终止协程");
                    yield break;
                }

                LoggerUtils.Log($"[BudBoxModel] 开始合成 part type={part.type}（texts={part.texts?.Count ?? 0}，photos={part.photos?.Count ?? 0}）");

                // 异步加载当前 part 的所有图片
                var photoTextures = new Dictionary<string, Texture>();
                int pendingCount = 0;

                if (part.photos != null)
                {
                    foreach (var pd in part.photos)
                    {
                        if (string.IsNullOrEmpty(pd.photoUrl))
                            continue;

                        pendingCount++;
                        var url = pd.photoUrl; // 闭包捕获
                        LoggerUtils.Log($"[BudBoxModel] 异步加载图片：{url}");
                        var wrapper = Loader.LoadRemoteImageAsync(url);

                        if (wrapper == null)
                        {
                            LoggerUtils.LogError($"[BudBoxModel] LoadRemoteImageAsync 返回 null，url={url}");
                            pendingCount--;
                            continue;
                        }

                        // completed 在 Unity 主线程回调，与 yield return null 的帧推进兼容
                        wrapper.completed += success =>
                        {
                            if (success)
                            {
                                var tex = wrapper.RetainAsset(this.gameObject);

                                if (tex != null)
                                {
                                    photoTextures[url] = tex;
                                    LoggerUtils.Log($"[BudBoxModel] 图片加载成功：{url}，尺寸={tex.width}×{tex.height}");
                                }
                                else
                                {
                                    LoggerUtils.LogError($"[BudBoxModel] RetainAsset 返回 null，url={url}");
                                }
                            }
                            else
                            {
                                LoggerUtils.LogError($"[BudBoxModel] 图片加载失败（success=false），url={url}");
                            }

                            pendingCount--;
                        };
                    }
                }

                // 等待所有图片加载完毕（每帧检查一次，不阻塞主线程）
                if (pendingCount > 0)
                {
                    LoggerUtils.Log($"[BudBoxModel] 等待 {pendingCount} 张图片加载完毕...");
                }

                while (pendingCount > 0)
                {
                    yield return null;
                }

                // 加载完成后执行同步合成（Camera.Render 是同步调用）
                if (this == null)
                {
                    LoggerUtils.Log("[BudBoxModel] CompositePartsAsync：等待图片后组件已销毁，终止协程");
                    yield break;
                }

                LoggerUtils.Log($"[BudBoxModel] 开始渲染合成 RT，part type={part.type}，已加载图片={photoTextures.Count}");
                var finalRT = CreateCompositeTexture(pixelTex, part, photoTextures);
                _compositorRTs.Add(finalRT);
                ApplyTextureToMaterial(mat, finalRT);
                LoggerUtils.Log($"[BudBoxModel] part type={part.type} 合成完成，RT={finalRT.width}×{finalRT.height}");

                // 像素贴图已被 RT 替代，从管理列表移除并销毁
                _boxTextures.Remove(pixelTex);
                Destroy(pixelTex);
            }

            LoggerUtils.Log($"[BudBoxModel] CompositePartsAsync 全部完成，共处理 {parts.Count} 个面");
        }

        /// <summary>
        /// 根据 shader 名称将纹理绑定到材质的正确属性名。
        /// </summary>
        /// <param name="mat">目标材质</param>
        /// <param name="tex">要绑定的纹理（Texture2D 或 RenderTexture）</param>
        private void ApplyTextureToMaterial(Material mat, Texture tex)
        {
            if (mat.shader.name == "Universal Render Pipeline/Lit")
            {
                mat.SetTexture("_BaseMap", tex);
            }
            else if (mat.shader.name == "bud/patterns_ugc_ARI")
            {
                mat.SetTexture("_patterns_tex", tex);
            }
            else
            {
                mat.SetTexture("_MainTex", tex);
            }
        }

        /// <summary>
        /// 确保合成器实例已创建。复用单个 DynamicDrawCanvas 实例避免重复实例化开销。
        /// 初次调用时从 _compositorPrefab 实例化，并将 mDrawPanel 的缩放设置为与编辑器一致的比例
        /// （scaleRatio = 256 / DrawBoardHeight）。
        /// </summary>
        private void EnsureCompositor()
        {
            if (_compositor != null)
                return;

            _compositor = Instantiate(_compositorPrefab);

            // 禁用自动渲染：Camera.enabled=false 后每帧不再自动渲染，但仍可通过 Camera.Render() 手动触发
            // 防止相机每帧持续写入 RT，避免闪烁及不必要的 GPU 开销
            _compositor.mDrawCamera.enabled = false;

            // 同步编辑器中 InitCanvas 对 mDrawPanel 的缩放设置：scaleRatio = FinalTextureSize / DrawBoardHeight
            // 使存储的 localPosition（在 mDrawPanel 本地空间）能正确映射到 256×256 画布坐标
            // Z 轴保持 1，与编辑器 new Vector3(scaleRatio, scaleRatio, 1) 保持一致
            float scaleRatio = 256f / DrawBoardHeight;
            _compositor.mDrawPanel.localScale = new Vector3(scaleRatio, scaleRatio, 1f);
        }

        /// <summary>
        /// 将像素贴图、文字、图片合成到 256×256 RenderTexture（同步执行，调用前需确保图片已异步加载完毕）。
        /// 流程与编辑器 InititalMapCanvas 对齐：
        ///   1. 以像素贴图为背景（mRawImage）
        ///   2. 在 mDrawPanel 下按 hierarchy 顺序创建 Text / RawImage 元素
        ///   3. mDrawCamera 渲染到 finalRT 并返回
        /// </summary>
        /// <param name="pixelTex">像素贴图（将作为 Canvas 背景）</param>
        /// <param name="part">当前面的 UGCPartData，包含 texts 和 photos 列表</param>
        /// <param name="preloadedPhotos">已异步预加载完成的图片字典，key=photoUrl，由 <see cref="CompositePartsAsync"/> 提供</param>
        /// <returns>256×256 合成 RenderTexture</returns>
        private RenderTexture CreateCompositeTexture(Texture2D pixelTex, UGCPartData part, Dictionary<string, Texture> preloadedPhotos)
        {
            EnsureCompositor();

            // 重置 mRawImage 状态：Prefab 中 alpha 可能为 0（用于隐藏预览），需恢复为完全不透明白色
            // 同时清除可能残留的自定义 material（如上次用过 TransparentMat），防止贴图不可见
            _compositor.mRawImage.color = Color.white;
            _compositor.mRawImage.material = null;

            // 1. 设置背景贴图
            _compositor.mRawImage.texture = pixelTex;

            // 2. 清除上一个面遗留的 Text/RawImage 子节点（DestroyImmediate 确保渲染前立即移除）
            var drawPanel = _compositor.mDrawPanel;

            for (int i = drawPanel.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(drawPanel.GetChild(i).gameObject);
            }

            // 3. 构建按 hierarchy 排序的元素列表，保持与编辑器 HierarchicalSort 一致的层叠顺序
            var elements = new List<(int hierarchy, bool isText, object data)>();

            if (part.texts != null)
            {
                foreach (var t in part.texts)
                {
                    elements.Add((t.hierarchy, true, t));
                }
            }

            if (part.photos != null)
            {
                foreach (var p in part.photos)
                {
                    elements.Add((p.hierarchy, false, p));
                }
            }

            elements.Sort((a, b) => a.hierarchy.CompareTo(b.hierarchy));

            // 4. 依次创建 UI 元素，位置/旋转/大小来自保存的字符串数据
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

                    // 与编辑器 UGCTextBehaviour 保持一致：SourceHanSansCN-Regular、64px、粗体、居中
                    var textComp = go.GetComponent<Text>();
                    textComp.font = _ugcTextFont;
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

                    // 从预加载字典中取图片（图片已在 CompositePartsAsync 中异步加载完毕）
                    if (!string.IsNullOrEmpty(pd.photoUrl) &&
                        preloadedPhotos.TryGetValue(pd.photoUrl, out var loadedTex) &&
                        loadedTex != null)
                    {
                        go.GetComponent<RawImage>().texture = loadedTex;
                    }
                    else if (!string.IsNullOrEmpty(pd.photoUrl))
                    {
                        LoggerUtils.Log($"[BudBoxModel] 图片加载失败，跳过显示：{pd.photoUrl}");
                    }
                }
            }

            // 5. 创建 256×256 finalRT，触发相机渲染将背景+文字+图片合成到 RT
            var finalRT = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            finalRT.Create();
            _compositor.SetCameraTargetTexture(finalRT);

            return finalRT;
        }

        /// <summary>销毁盒子模型实例并释放动态创建的贴图，防止内存泄漏。</summary>
        private void DestroyBoxModel()
        {
            // 停止正在进行的异步合成协程，避免协程持有已销毁对象的引用
            StopAllCoroutines();

            foreach (var t in _boxTextures)
            {
                if (t != null)
                {
                    Destroy(t);
                }
            }

            _boxTextures.Clear();

            // 释放合成管线产生的 RenderTexture
            foreach (var rt in _compositorRTs)
            {
                if (rt != null)
                {
                    rt.Release();
                }
            }

            _compositorRTs.Clear();

            // 销毁合成器实例（下次加载场景时会按需重建）
            if (_compositor != null)
            {
                Destroy(_compositor.gameObject);
                _compositor = null;
            }

            if (_boxModel != null)
            {
                Destroy(_boxModel);
                _boxModel = null;
            }
        }

        #endregion
    }
}
