using Game.Audio;
using GameData.BaseInfo;
using Message;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;

/// <summary>
/// 孵化舱音色选择弹窗，提供 PGC、已创建、已购买三个 Tab 切换选择音色。
/// 确认时若音色发生变化，会调用批量 TTS 接口为所有唤醒动作和口令互动重新生成音频，
/// 期间确认按钮保持不可交互状态，接口完成后再关闭面板。
/// </summary>
public class IncubationCabinVoicePopPanel : BasePanel<IncubationCabinVoicePopPanel>
{
    public Button Btn_Close;
    public CButton Btn_Confirm;
    public Toggle Tog_Pgc;
    public Toggle Tog_Created;
    public Toggle Tog_Owned;

    public CabinUgcAnimPgcToneInfoPanel PgcTonePanel;
    public CabinUgcAnimUgcToneInfoPanel CreatedPanel;
    public CabinUgcAnimUgcToneInfoPanel OwnedPanel;

    public List<Text> toggleTextList;

    private CabinToneInfo _curChooseToneInfo;

    /// <summary>当前编辑的角色数据，只改内存，不触发网络请求</summary>
    private CabinCharacterUgcInfo _localInfo;

    /// <summary>打开面板时记录的原始音色 ID，确认时与选中的新 ID 对比，判断是否需要重新生成音频</summary>
    private string _originalToneId;

    /// <summary>
    /// 音频重生成的映射条目，记录每条待生成文本与回写目标之间的对应关系。
    /// </summary>
    private class RegenEntry
    {
        /// <summary>true = 唤醒动作（activation），false = 口令互动（voiceCommands）</summary>
        public bool IsActivation;

        /// <summary>该条目在 texts 列表中的位置，用于从 API 返回的 data.list 中取对应结果</summary>
        public int TextIndex;

        /// <summary>该条目在原始 activation 或 voiceCommands 列表中的索引，用于回写 audioUrl</summary>
        public int SourceIndex;
    }

    /// <summary>在 OnBtnConfirmClick 中构建，在 API 回调中按 TextIndex 取结果、按 SourceIndex 写回</summary>
    private List<RegenEntry> _regenMapping;

    public enum ToneStudioType
    {
        PGC = 0,
        Created = 1,
        Owned = 2,
    }

    public override void OnCreate()
    {
        base.OnCreate();
        AddListener();
        Tog_Pgc.isOn = true;
        OnSelectView(ToneStudioType.PGC);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        AkSoundManager.Inst.StopBGSound();

        _localInfo = args != null && args.Length > 0 && args[0] is CabinCharacterUgcInfo info ? info : null;

        // 记录打开面板时的原始音色 ID，用于确认时判断音色是否真的发生了变化
        _originalToneId = _localInfo?.toneId;

        CreatedPanel.SetOnToneItemSelectAct(_localInfo);
        OwnedPanel.SetOnToneItemSelectAct(_localInfo);
    }

    private void AddListener()
    {
        PgcTonePanel.SetOnToneItemSelectAct(OnPgcToneItemClick);
        CreatedPanel.SetOnToneItemSelectAct(OnUgcToneItemClick);
        OwnedPanel.SetOnToneItemSelectAct(OnUgcToneItemClick);

        Btn_Close.onClick.AddListener(DoClose);
        Btn_Confirm.onClick.AddListener(OnBtnConfirmClick);
        Tog_Pgc.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnSelectView(ToneStudioType.PGC);
            }
        });
        Tog_Created.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnSelectView(ToneStudioType.Created);
            }
        });
        Tog_Owned.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnSelectView(ToneStudioType.Owned);
            }
        });
    }

    private void OnPgcToneItemClick(CabinToneInfo info)
    {
        _curChooseToneInfo = info;
        RefreshConfirmBtnState();
    }

    private void OnUgcToneItemClick(CabinToneInfo info)
    {
        _curChooseToneInfo = info;
        RefreshConfirmBtnState();
    }

    private void RefreshConfirmBtnState()
    {
        Btn_Confirm.gameObject.SetActive(true);
        Btn_Confirm.interactable = _curChooseToneInfo != null;
    }

    public override void OnHidden()
    {
        UgcAnimToneManager.Inst.StopPreviewTone();
        AkSoundManager.Inst.PlayBGSound();
    }

    public void DoClose()
    {
        UgcAnimToneManager.Inst.StopPreviewTone();
        CloseSelf();
        AkSoundManager.Inst.PlayBGSound();
    }

    /// <summary>
    /// 确认按钮点击处理。
    /// 若音色发生变化且存在需要重新生成的音频，则禁用确认按钮（loading 状态），
    /// 等待批量 TTS 接口完成后再关闭面板并刷新显示；
    /// 若音色未变化或没有需要更新的音频，则直接关闭。
    /// </summary>
    private void OnBtnConfirmClick()
    {
        if (_curChooseToneInfo == null || _localInfo == null)
        {
            DoClose();
            return;
        }

        var newToneId = _curChooseToneInfo.id;

        // 将新音色 ID 写入角色数据（内存操作）
        _localInfo.toneId = newToneId;

        // 构建待生成文本列表和映射表
        var texts = new List<string>();
        _regenMapping = new List<RegenEntry>();

        // 收集唤醒动作（activation）中需要重新生成的文本
        if (_localInfo.activation != null)
        {
            for (int i = 0; i < _localInfo.activation.Count; i++)
            {
                var item = _localInfo.activation[i];

                if (!string.IsNullOrEmpty(item.text))
                {
                    _regenMapping.Add(new RegenEntry
                    {
                        IsActivation = true,
                        TextIndex = texts.Count,
                        SourceIndex = i,
                    });
                    texts.Add(item.text);
                }
            }
        }

        // 收集口令互动（voiceCommands）中需要重新生成的响应语音文本
        if (_localInfo.voiceCommands != null)
        {
            for (int i = 0; i < _localInfo.voiceCommands.Count; i++)
            {
                var item = _localInfo.voiceCommands[i];

                if (!string.IsNullOrEmpty(item.text))
                {
                    _regenMapping.Add(new RegenEntry
                    {
                        IsActivation = false,
                        TextIndex = texts.Count,
                        SourceIndex = i,
                    });
                    texts.Add(item.text);
                }
            }
        }

        // 音色未变化，或没有任何需要重新生成的音频时，直接刷新并关闭
        if (newToneId == _originalToneId || texts.Count == 0)
        {
            MessageHelper.Broadcast(MessageName.OnCabinRefreshBaseMsg);
            DoClose();
            return;
        }

        // 音色已变化且有音频需要重新生成：禁用确认按钮（loading 状态），等 API 完成后再关闭
        Btn_Confirm.interactable = false;

        CabinNetManager.Inst.GetCabinCharacterToneBatchPreview(
            newToneId,
            texts,
            (success, data) =>
            {
                if (success && data?.list != null)
                {
                    // 遍历映射表，按文本内容在 data.list 中查找对应结果，匹配后立即移除，
                    // 避免 data.list 中存在相同文本时重复匹配到同一条记录
                    foreach (var entry in _regenMapping)
                    {
                        var text = texts[entry.TextIndex];

                        // 在 data.list 中找第一条 text 字段匹配的记录
                        var matchIndex = data.list.FindIndex(item => item != null && item.text == text);

                        if (matchIndex < 0)
                            continue;

                        var previewItem = data.list[matchIndex];

                        // 命中后立即移除，防止后续相同文本的条目匹配到同一结果
                        data.list.RemoveAt(matchIndex);

                        if (entry.IsActivation)
                        {
                            // 更新唤醒动作的音频 URL
                            _localInfo.activation[entry.SourceIndex].audioUrl = previewItem.url;
                        }
                        else
                        {
                            // 更新口令互动的音频 URL
                            _localInfo.voiceCommands[entry.SourceIndex].audioUrl = previewItem.url;
                        }
                    }

                    // audioUrl 写回完成后，通知 Node2/Node3 Manager 刷新各自的 Item 显示
                    MessageHelper.Broadcast(MessageName.OnCabinRefreshInteractNode2And3);
                }
                else
                {
                    // 生成失败时提示用户，旧的 audioUrl 保持不变
                    LoggerUtils.LogError("OnBtnConfirmClick: 批量音频重生成失败");
                    TipPanel.ShowToast("音频生成失败，请重试");
                }

                // 无论成功与否，都刷新音色显示并关闭面板
                MessageHelper.Broadcast(MessageName.OnCabinRefreshBaseMsg);
                DoClose();
            });
    }

    private void OnSelectView(ToneStudioType studioType)
    {
        _curChooseToneInfo = null;
        RefreshConfirmBtnState();

        toggleTextList[0].gameObject.SetActive(studioType == ToneStudioType.PGC);
        toggleTextList[1].gameObject.SetActive(studioType == ToneStudioType.Created);
        toggleTextList[2].gameObject.SetActive(studioType == ToneStudioType.Owned);

        switch (studioType)
        {
            case ToneStudioType.PGC:
                PgcTonePanel.gameObject.SetActive(true);
                CreatedPanel.gameObject.SetActive(false);
                OwnedPanel.gameObject.SetActive(false);
                PgcTonePanel.OnSelectPanel();
                PgcTonePanel.SelectFirstItem();
                break;

            case ToneStudioType.Created:
                PgcTonePanel.gameObject.SetActive(false);
                CreatedPanel.gameObject.SetActive(true);
                OwnedPanel.gameObject.SetActive(false);
                CreatedPanel.GetPublishedData();
                break;

            case ToneStudioType.Owned:
                PgcTonePanel.gameObject.SetActive(false);
                CreatedPanel.gameObject.SetActive(false);
                OwnedPanel.gameObject.SetActive(true);
                OwnedPanel.GetPublishedData();
                break;
        }
    }
}
