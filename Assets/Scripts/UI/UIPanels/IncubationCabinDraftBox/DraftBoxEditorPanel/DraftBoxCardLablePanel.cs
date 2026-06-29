using Message;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc: 台词显示面板，提供输入框供用户编辑显示在封面上的台词文本。
///       输入结束（失焦）时直接修改 characterInfo 并广播 CardDesc 事件。
/// Date:26-04-02
/// </summary>
public class DraftBoxCardLablePanel : MonoBehaviour
{
    [SerializeField] private InputField DescInputField; // 台词输入框

    private CabinCharacterBaseInfo _info;

    public void InitUI()
    {
        DescInputField.onEndEdit.AddListener(OnEndEdit);
    }

    /// <summary>设置当前编辑的角色数据引用，并将已有台词填入输入框</summary>
    public void SetData(CabinCharacterBaseInfo info)
    {
        _info = info;
        DescInputField.text = info?.coverInfo?.desc ?? "";
    }

    /// <summary>输入结束时将输入内容写入 coverInfo.desc 并广播 CardDesc 事件</summary>
    private void OnEndEdit(string value)
    {
        if (_info == null) return;
        if (_info.coverInfo == null) _info.coverInfo = new CabinCoverInfo();
        _info.coverInfo.desc = value;
        MessageHelper.Broadcast(MessageName.OnCabinCharacterUpdated, _info.id, CabinCharacterUpdateType.CardDesc);
    }
}
