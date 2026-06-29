using System.Collections;
using System.Collections.Generic;
using Game.Base;
using UnityEngine;
using UnityEngine.UI;

public class UICommonUtils
{
    //TODO:临时方法，后续大厅3D模型更换流程，则无需使用该节点
    public static void DestoryModel(GameObject target)
    {
        if (GamePropNodeManager.HasInstance)
        {
            GamePropNodeManager.Inst.DestroyNode(target, true);
        }
        else
        {
            GameObject.Destroy(target);
        }


    }

    public static void RefreshLayout(Transform targetTransform)
    {
        if (targetTransform == null) return;

        RectTransform rectTransform = targetTransform.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 方法1: 使用 LayoutRebuilder 强制重建布局（推荐）
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        // 方法2: 如果有关联的 ContentSizeFitter，也需要刷新
        ContentSizeFitter contentSizeFitter = targetTransform.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null && contentSizeFitter.enabled)
        {
            contentSizeFitter.SetLayoutHorizontal();
            contentSizeFitter.SetLayoutVertical();
            // 再次强制重建
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        // 方法3: 如果有关联的 VerticalLayoutGroup，也需要刷新
        VerticalLayoutGroup verticalLayoutGroup = targetTransform.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null && verticalLayoutGroup.enabled)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    /// <summary>
    /// 将任意字符串（如玩家名字）转换为一个固定且唯一的颜色。
    /// 同一字符串每次返回相同颜色，适合用于头像背景色等场景。
    /// </summary>
    public static Color StringToColor(string str)
    {
        // 将字符串转成整数哈希值
        // 用 31 作乘数是经典散列算法，让不同字符串尽量产生不同的数值
        int hash = 0;
        foreach (char c in str)
            hash = hash * 31 + c;

        // 用哈希值作为随机种子，保证同一字符串每次得到相同的随机序列
        System.Random rng = new System.Random(hash);

        // 依次生成 H（色相）、S（饱和度）、V（明度）三个分量
        float h = (float)rng.NextDouble(); // 色相 0~1，决定是红/绿/蓝等哪种颜色
        float s = (float)rng.NextDouble(); // 饱和度 0~1
        float v = (float)rng.NextDouble(); // 明度 0~1

        // HSV 转 RGB，Unity 内置方法
        return Color.HSVToRGB(h, s, v);
    }
}
