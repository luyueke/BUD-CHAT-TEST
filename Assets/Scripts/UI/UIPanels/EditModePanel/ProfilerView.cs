using Game.Config;
using Game.Props.PropsManagers;
using Message;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class ProfilerView : MonoBehaviour
{
    private Text memoryText;
    private Text verticesText;
    private Text materialsText;
    private Text dText;
    private GameObject infoGroup;
    private Button infoBtn;
    private Button closeInfoBtn;

    private int memoryTipCount;
    private LimitType _curLimitType;

    private const string Tip_Memory = "哦莫！你的手机内存不足啦，不妨控制一下模具的使用数量并记得及时保存草稿喔！继续添加模具编辑器可能要崩溃啦呜呜o(\u2565﹏\u2565)o";
    private const string Tip_Vertices = "啊噢！老师你的作品使用了过多的模具已达到作品顶点数限制啦，非常抱歉，为降低因模型加载造成的游玩卡顿体验影响，请尝试减少模具使作品顶点数低于限制，我们会持续优化性能努力提高限制上限，让创作者老师们体验更好嘟!";
    private const string Tip_Material = "啊噢！老师你的作品使用了过多的材质已达到作品材质数限制啦，非常抱歉，为降低因模型加载造成的游玩卡顿体验影响，请尝试减少材质使用使作品材质数低于限制，我们会持续优化性能努力提高限制上限，让创作者老师们体验更好嘟!";
    private const string Tip_DText = "哦豁！老师你的作品使用了太多的文字啦！快减少使用的文字数量，否则可能会导致卡顿或崩溃呜呜o(\u2565﹏\u2565)o\n";

    private void Awake()
    {
        memoryText = GameObjectEx.FindComponentByName<Text>(transform, "InfoGroup/MemoryText");
        verticesText = GameObjectEx.FindComponentByName<Text>(transform, "InfoGroup/VerticesText");
        materialsText = GameObjectEx.FindComponentByName<Text>(transform, "InfoGroup/MaterialsText");
        dText = GameObjectEx.FindComponentByName<Text>(transform, "InfoGroup/DText");
        infoGroup = GameObjectEx.FindChildByName(transform, "InfoGroup").gameObject;
        infoBtn = GameObjectEx.FindComponentByName<Button>(transform, "InfoBtn");
        closeInfoBtn = GameObjectEx.FindComponentByName<Button>(transform, "InfoGroup/CloseBtn");
        infoBtn.onClick.AddListener(() =>
        {
            infoBtn.gameObject.SetActive(false);
            infoGroup.SetActive(true);
        });
        closeInfoBtn.onClick.AddListener(() =>
        {
            infoGroup.SetActive(false);
            infoBtn.gameObject.SetActive(true);
        });
        MessageHelper.AddListener<bool>(MessageName.UpgradeProfilerInfo, UpdateStaticsInfo);

    }

    public void InitData(LimitType limitType)
    {
        this._curLimitType = limitType;
        UpdateStaticsInfo(false);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<bool>(MessageName.UpgradeProfilerInfo, UpdateStaticsInfo);
    }

    private void UpdateStaticsInfo(bool isAdd)
    {
        var curMapStatisticInfo = GameProfilerManager.Inst.mapStatisticInfo;
        if (curMapStatisticInfo == null)
        {
            LoggerUtils.Log("UpdateStaticsInfo == null");
            return;
        }

        curMapStatisticInfo.memory = Profiler.GetTotalAllocatedMemoryLong();
        string warningText = "";
        long limitMemory = (long)SystemInfo.systemMemorySize * 1024 * 1024;
        if (isAdd)
        {
            // 内存⚠️提示
            var percentage = (float)curMapStatisticInfo.memory / limitMemory;
            if (percentage >= 0.95f && memoryTipCount < 3)
            {
                warningText = Tip_Memory;
                memoryTipCount = 3;

            }
            else if (percentage >= 0.9f && memoryTipCount < 2)
            {
                warningText = Tip_Memory;
                memoryTipCount = 2;
            }
            else if (percentage > 0.8f && memoryTipCount < 1)
            {
                warningText = Tip_Memory;
                memoryTipCount = 1;
            }
        }
        
        memoryText.SetLocalText("内存: {0}\n剩余: {1}",ConvertBytesToSize(curMapStatisticInfo.memory),ConvertBytesToSize(limitMemory));

        var limitVerticesCount = GameProfilerManager.GetLimitVerticesCount(_curLimitType);
        if (curMapStatisticInfo.vertices > limitVerticesCount)
        {
            if (isAdd)
            {
                warningText = Tip_Vertices;
            }
            verticesText.SetLocalText("顶点数: <color=red>{0}</color>/{1}",ConvertNumberToSize(curMapStatisticInfo.vertices),ConvertNumberToSize(limitVerticesCount));
        }
        else
        {
            verticesText.SetLocalText("顶点数: {0}/{1}",ConvertNumberToSize(curMapStatisticInfo.vertices),ConvertNumberToSize(limitVerticesCount));
        }
        
        var limitMaterialsCount = GameProfilerManager.GetLimitMaterialsCount(_curLimitType);
        if (curMapStatisticInfo.materials > limitMaterialsCount)
        {
            if (isAdd)
            {
                warningText = Tip_Material;
            }
            materialsText.SetLocalText("材质: <color=red>{0}</color>/{1}",ConvertNumberToSize(curMapStatisticInfo.materials),ConvertNumberToSize(limitMaterialsCount));
        }
        else
        {
            materialsText.SetLocalText("材质: {0}/{1}",ConvertNumberToSize(curMapStatisticInfo.materials),ConvertNumberToSize(limitMaterialsCount));
        }

        var limitTextCount = GameProfilerManager.GetLimitTextCount(_curLimitType);
        if (curMapStatisticInfo.dTexts > limitTextCount)
        {
            if (isAdd)
            {
                warningText = Tip_DText;
            }
            dText.SetLocalText("3D文字: <color=red>{0}</color>/{1}",curMapStatisticInfo.dTexts,limitTextCount);
        }
        else
        {
            dText.SetLocalText("3D文字: {0}/{1}",curMapStatisticInfo.dTexts,limitTextCount);
        }
        if (!string.IsNullOrEmpty(warningText))
        {
            if (!UIManager.Inst.TryFindPanel(PanelId.WarningPanel, out WarningPanel wPanel))
            {
                wPanel = UIManager.Inst.OpenPanel<WarningPanel>(PanelId.WarningPanel, warningText);
            }
        }
    }

    private string ConvertBytesToSize(long bytes)
    {
        double size;
        string unit;
        if (bytes < 102.4f * 1024)
        {
            size = bytes / 1024.0;
            unit = "KB";
        }
        else if (bytes < 102.4f * 1024 * 1024)
        {
            size = bytes / (1024.0 * 1024);
            unit = "MB";
        }
        else
        {
            size = bytes / (1024.0 * 1024 * 1024);
            unit = "GB";
        }

        if (size == 0)
        {
            return size + unit;
        }

        return $"{size:F2}" + unit;
    }

    private string ConvertNumberToSize(int value)
    {
        if (value <= 0)
            value = 0;
        
        if (value > 1000)
        {
            return (value / 1000).ToString("#.##") + "k";
        }
        else
        {
            return value.ToString();
        }
    }
}
