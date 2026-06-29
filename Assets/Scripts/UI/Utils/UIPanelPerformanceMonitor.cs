#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UI.Performance
{
    public class UIPanelPerformanceMonitor
    {
        private class PanelPerformanceData
        {
            public float createTime;
            public float showTime;
            public int drawCallsBefore;
            public int drawCallsAfter;
            public long memoryBefore;
            public long memoryAfter;
            public int imageCount;
            public int textCount;
            public HashSet<Material> materials = new HashSet<Material>();
            public HashSet<Texture> textures = new HashSet<Texture>();
            public System.Text.StringBuilder log = new System.Text.StringBuilder();
        }

        private readonly System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();
        private readonly Dictionary<string, PanelPerformanceData> _performanceCache = new Dictionary<string, PanelPerformanceData>();

        public void StartMonitor(string panelName)
        {
            if (!_performanceCache.TryGetValue(panelName, out var data))
            {
                data = new PanelPerformanceData();
                _performanceCache[panelName] = data;
            }

            _stopwatch.Restart();
            data.drawCallsBefore = UnityEditor.UnityStats.drawCalls;
            data.memoryBefore = System.GC.GetTotalMemory(false);
            data.log.Clear();
            data.log.AppendLine($"[性能监控] 开始监控面板: {panelName}");
        }

        public void RecordStep(string panelName, string stepName)
        {
            if (!_performanceCache.TryGetValue(panelName, out var data)) return;
            
            var stepTime = _stopwatch.ElapsedMilliseconds;
            data.log.AppendLine($"{stepName}耗时: {stepTime}ms");
        }

        public void CollectPanelInfo(string panelName, GameObject panelObj)
        {
            if (!_performanceCache.TryGetValue(panelName, out var data)) return;

            var images = panelObj.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            var texts = panelObj.GetComponentsInChildren<UnityEngine.UI.Text>(true);

            data.materials.Clear();
            data.textures.Clear();
            data.imageCount = images.Length;
            data.textCount = texts.Length;

            foreach (var img in images)
            {
                if (img.material != null)
                    data.materials.Add(img.material);
                if (img.mainTexture != null)
                    data.textures.Add(img.mainTexture);
            }

            foreach (var txt in texts)
            {
                if (txt==null||txt.font==null||txt.material==null)
                {
                    continue;
                }
                if (txt.material != null)
                    data.materials.Add(txt.material);
                if (txt.font?.material != null)
                    data.materials.Add(txt.font.material);
            }

            data.log.AppendLine($"UI组件统计:");
            data.log.AppendLine($"- Image数量: {data.imageCount}");
            data.log.AppendLine($"- Text数量: {data.textCount}");
            data.log.AppendLine($"- 材质数量: {data.materials.Count}");
            data.log.AppendLine($"- 贴图数量: {data.textures.Count}");
        }

        public void CompleteMonitor(string panelName)
        {
            if (!_performanceCache.TryGetValue(panelName, out var data)) return;

            _stopwatch.Stop();
            data.showTime = _stopwatch.ElapsedMilliseconds;
            data.drawCallsAfter = UnityEditor.UnityStats.drawCalls;
            data.memoryAfter = System.GC.GetTotalMemory(false);

            var drawCallDiff = data.drawCallsAfter - data.drawCallsBefore;
            var memoryDiff = (data.memoryAfter - data.memoryBefore) / 1024f;

            data.log.AppendLine($"性能统计:");
            data.log.AppendLine($"- 总耗时: {data.showTime}ms");
            data.log.AppendLine($"- DrawCall增量: {drawCallDiff}");
            data.log.AppendLine($"- 内存增量: {memoryDiff:F2}KB");

            // 两级警告
            if (data.showTime > 150)
                data.log.AppendLine($"<color=red>[严重] 面板打开耗时: {data.showTime}ms</color>");
            else if (data.showTime > 100)
                data.log.AppendLine($"<color=yellow>[警告] 面板打开耗时: {data.showTime}ms</color>");

            if (drawCallDiff > 50)
                data.log.AppendLine($"<color=red>[严重] DrawCall增量: {drawCallDiff}</color>");
            else if (drawCallDiff > 30)
                data.log.AppendLine($"<color=yellow>[警告] DrawCall增量: {drawCallDiff}</color>");

            if (data.imageCount > 50)
                data.log.AppendLine($"<color=red>[严重] Image组件数量: {data.imageCount}</color>");
            else if (data.imageCount > 30)
                data.log.AppendLine($"<color=yellow>[警告] Image组件数量: {data.imageCount}</color>");

            if (data.materials.Count > 10)
                data.log.AppendLine($"<color=red>[严重] 材质数量: {data.materials.Count}</color>");
            else if (data.materials.Count > 5)
                data.log.AppendLine($"<color=yellow>[警告] 材质数量: {data.materials.Count}</color>");

            Debug.Log(data.log.ToString());
        }

        public string GetPanelPerformanceReport(string panelName)
        {
            if (!_performanceCache.TryGetValue(panelName, out var data))
                return $"No performance data for panel: {panelName}";

            return data.log.ToString();
        }
    }
}
#endif