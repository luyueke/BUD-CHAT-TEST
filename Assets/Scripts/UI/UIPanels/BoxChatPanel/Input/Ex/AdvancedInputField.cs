using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 控制剪切 复制 粘贴
/// </summary> <summary>
/// 
/// </summary>

[RequireComponent(typeof(MyInputField))]
[RequireComponent(typeof(MultiLineInputFieldSelection))]
public class AdvancedInputField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [HideInInspector] public MyInputField inputField;
    private bool isLongPress = false;
    private bool isPointerDown = false;
    private float longPressThreshold = 0.2f;
    private float pointerDownTime;
    private Vector2 pointerDownPosition;

    // 文本选择相关
    private int selectionStart = 0;
    private int selectionEnd = 0;
    private bool hasSelection = false;

    // 上下文菜单
    private TextSelectionContextMenu contextMenu;


    int recordSelectionAnchorPosition = 0;
    int recordSelectionFocusPosition = 0;
    int recordCaretPosition = 0;

    void Awake()
    {
        inputField = GetComponent<MyInputField>();
        inputField.shouldHideMobileInput = true;//需要隐藏移动端输入法
        inputField.caretBlinkRate = 0.85f;
        inputField.caretWidth = 5;
        contextMenu = FindObjectOfType<TextSelectionContextMenu>();

        longPressThreshold = MyInputFieldConfig.longPressThreshold;
        // 如果没有上下文菜单，创建一个
        if (contextMenu == null)
        {
            CreateContextMenu();
        }
        inputField.selectIncludeObjects.Add(contextMenu.cutButton.gameObject);
        inputField.selectIncludeObjects.Add(contextMenu.copyButton.gameObject);
        inputField.selectIncludeObjects.Add(contextMenu.pasteButton.gameObject);
        contextMenu.currentInputField = this;
        contextMenu.multiLineInputFieldSelection = GetComponent<MultiLineInputFieldSelection>();

        inputField.onEndEdit.AddListener(OnEndEdit);
    }

    void OnEndEdit(string text)
    {
        Invoke("HideContextMenu", 0.1f);
    }

    void Record()
    {
        recordSelectionAnchorPosition = Math.Min(selectionStart, inputField.selectionFocusPosition);
        recordSelectionFocusPosition = Math.Max(selectionStart, inputField.selectionFocusPosition);
        recordCaretPosition = inputField.caretPosition;
    }

    void Update()
    {
        HandleLongPress();
        // HandleCursorMovement(); //注释
    }

    // 长按检测
    private void HandleLongPress()
    {
        if (isPointerDown && !isLongPress)
        {
            if (Time.time - pointerDownTime > longPressThreshold)
            {
                isLongPress = true;
                OnLongPressDetected();
            }
        }
    }

    private void OnLongPressDetected()
    {
        // 启动文本选择模式
        StartTextSelection();

        // 显示上下文菜单
        // ShowContextMenu();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        pointerDownTime = Time.time;
        pointerDownPosition = eventData.position;

        // 记录点击位置对应的光标位置
        selectionStart = GetCharacterIndexFromPosition(eventData.position);
        selectionEnd = selectionStart;


    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;

        if (!isLongPress)
        {
            // 短按：移动光标
            // Debug.LogError("OnPointerUp: 短按");
            // MoveCursorToPosition(eventData.position);
            // ShowContextMenu();
            Record();

            if (recordSelectionAnchorPosition != recordSelectionFocusPosition)
            {
                if (recordSelectionAnchorPosition > recordSelectionFocusPosition)
                {
                    int temp = recordSelectionAnchorPosition;
                    recordSelectionAnchorPosition = recordSelectionFocusPosition;
                    recordSelectionFocusPosition = temp;
                }
                gameObject.GetComponent<MultiLineInputFieldSelection>().canHighlight = true;
                gameObject.GetComponent<MultiLineInputFieldSelection>().SetSelection(recordSelectionAnchorPosition, recordSelectionFocusPosition);
                ShowContextMenu();
            }
            else
            {
                HideContextMenu();
            }
        }
        else
        {
            // 长按结束：更新选择结束位置
            // Debug.LogError("OnPointerUp: 长按");
            selectionEnd = GetCharacterIndexFromPosition(eventData.position);
            // UpdateTextSelection();

            var canHighlight = gameObject.GetComponent<MultiLineInputFieldSelection>().canHighlight;
            if (canHighlight)
            {
                ShowContextMenu();
            }
            else
            {
                HideContextMenu();
            }
            Record();

            inputField.selectionAnchorPosition = Math.Min(selectionStart, inputField.selectionFocusPosition);
            inputField.selectionFocusPosition = Math.Max(selectionStart, inputField.selectionFocusPosition);
            inputField.caretPosition = inputField.selectionFocusPosition;
        }



        isLongPress = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2)
        {
            // 双击：选择单词
            SelectWordAtCursor();
        }
    }

    // 获取点击位置对应的字符索引
    private int GetCharacterIndexFromPosition(Vector2 position)
    {

        return gameObject.GetComponent<MultiLineInputFieldSelection>().GetCharacterIndexFromPosition(position);
        // 将屏幕坐标转换为InputField本地坐标
        // RectTransformUtility.ScreenPointToLocalPointInRectangle(
        //     inputField.textComponent.rectTransform,
        //     position,
        //     null,
        //     out Vector2 localPos);

        // TextGenerator generator = inputField.textComponent.cachedTextGenerator;

        // for (int i = 0; i < generator.characterCount; i++)
        // {
        //     UICharInfo charInfo = generator.characters[i];
        //     if (localPos.x >= charInfo.cursorPos.x &&
        //         localPos.x <= charInfo.cursorPos.x + charInfo.charWidth)
        //     {
        //         return i;
        //     }
        // }

        // return inputField.text.Length;
    }



    // 开始文本选择
    private void StartTextSelection()
    {
        hasSelection = true;
        // inputField.selectionAnchorPosition = selectionStart;
        // inputField.selectionFocusPosition = selectionStart;
    }

    // 更新文本选择
    private void UpdateTextSelection()
    {
        // if (hasSelection)
        // {
        //     inputField.selectionAnchorPosition = Mathf.Min(selectionStart, selectionEnd);
        //     inputField.selectionFocusPosition = Mathf.Max(selectionStart, selectionEnd);
        //     Debug.LogError("UpdateTextSelection: " + inputField.selectionAnchorPosition + " " + inputField.selectionFocusPosition);
        // }
    }

    // 选择光标处的单词
    private void SelectWordAtCursor()
    {
        string text = inputField.text;
        int cursorPos = inputField.caretPosition;

        if (string.IsNullOrEmpty(text) || cursorPos >= text.Length)
            return;

        // 查找单词边界
        int start = cursorPos;
        int end = cursorPos;

        // 向左查找单词开始
        while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
        {
            start--;
        }

        // 向右查找单词结束
        while (end < text.Length && !char.IsWhiteSpace(text[end]))
        {
            end++;
        }

        // 设置选择范围
        // inputField.selectionAnchorPosition = start;
        // inputField.selectionFocusPosition = end;
        hasSelection = true;

        ShowContextMenu();
    }

    // 复制选中的文本
    public void Copy()
    {
        Debug.LogError("recordSelectionAnchorPosition: " + recordSelectionAnchorPosition);
        Debug.LogError("recordSelectionFocusPosition: " + recordSelectionFocusPosition);
        Debug.LogError("recordCaretPosition: " + recordCaretPosition);
        Debug.LogError("Copy: " + hasSelection + " " + inputField.selectionAnchorPosition + " " + inputField.selectionFocusPosition);

        if (!hasSelection || recordSelectionAnchorPosition == recordSelectionFocusPosition)
            return;

        int start = Mathf.Min(recordSelectionAnchorPosition, recordSelectionFocusPosition);
        int end = Mathf.Max(recordSelectionAnchorPosition, recordSelectionFocusPosition);
        int length = end - start;

        string selectedText = inputField.text.Substring(start, length);
        GUIUtility.systemCopyBuffer = selectedText;

        HideContextMenu();
    }

    // 剪切选中的文本
    public void Cut()
    {
        Debug.LogError("recordSelectionAnchorPosition: " + recordSelectionAnchorPosition);
        Debug.LogError("recordSelectionFocusPosition: " + recordSelectionFocusPosition);
        Debug.LogError("recordCaretPosition: " + recordCaretPosition);
        Debug.LogError("Cut: " + hasSelection + " " + inputField.selectionAnchorPosition + " " + inputField.selectionFocusPosition);

        if (!hasSelection || recordSelectionAnchorPosition == recordSelectionFocusPosition)
            return;
        Copy(); // 先复制

        int start = Mathf.Min(recordSelectionAnchorPosition, recordSelectionFocusPosition);
        int end = Mathf.Max(recordSelectionAnchorPosition, recordSelectionFocusPosition);
        int length = end - start;

        // 删除选中的文本
        string newText = inputField.text.Remove(start, length);
        inputField.text = newText;

        // 移动光标到删除位置
        inputField.caretPosition = start;
        inputField.selectionAnchorPosition = start;
        inputField.selectionFocusPosition = start;

        hasSelection = false;
        HideContextMenu();
    }

    // 粘贴文本
    public void Paste()
    {
        string pasteText = GUIUtility.systemCopyBuffer;
        Debug.LogError("pasteText: " + pasteText);
        if (string.IsNullOrEmpty(pasteText))
            return;

        recordSelectionAnchorPosition = inputField.selectionAnchorPosition;
        recordSelectionFocusPosition = inputField.selectionFocusPosition;
        recordCaretPosition = inputField.caretPosition;

        Debug.LogError("recordSelectionAnchorPosition: " + recordSelectionAnchorPosition);
        Debug.LogError("recordSelectionFocusPosition: " + recordSelectionFocusPosition);
        Debug.LogError("recordCaretPosition: " + recordCaretPosition);
        Debug.LogError("Paste: " + hasSelection + " " + recordSelectionAnchorPosition + " " + recordSelectionFocusPosition);

        if (hasSelection)
        {
            // 替换选中的文本
            int start = Mathf.Min(recordSelectionAnchorPosition, recordSelectionFocusPosition);
            int end = Mathf.Max(recordSelectionAnchorPosition, recordSelectionFocusPosition);

            string newText = inputField.text.Remove(start, end - start);
            newText = newText.Insert(start, pasteText);
            inputField.text = newText;

            // 移动光标到粘贴结束位置
            inputField.caretPosition = start + pasteText.Length;
            recordCaretPosition = start + pasteText.Length;
        }
        else
        {
            // 在光标处插入文本
            int caretPos = recordCaretPosition;
            string newText = inputField.text.Insert(caretPos, pasteText);
            inputField.text = newText;
            inputField.caretPosition = caretPos + pasteText.Length;
            recordCaretPosition = caretPos + pasteText.Length;
        }

        inputField.selectionAnchorPosition = recordCaretPosition;
        inputField.selectionFocusPosition = recordCaretPosition;
        hasSelection = false;

        HideContextMenu();

        // 立即标记文本组件布局为脏（关键：在设置文本后立即标记）
        // MarkTextLayoutDirty();

        // // 延迟刷新布局（确保文本渲染完成后再刷新）
        // StartCoroutine(DelayedRefreshLayout());
    }

    // 标记文本组件布局为脏
    private void MarkTextLayoutDirty()
    {
        if (inputField?.textComponent == null)
            return;

        RectTransform textRect = inputField.textComponent.rectTransform;
        if (textRect == null)
            return;

        // 关键：标记文本组件本身为脏，这样布局系统会重新计算其大小
        LayoutRebuilder.MarkLayoutForRebuild(textRect);

        // 如果文本组件有 ContentSizeFitter，需要强制更新
        ContentSizeFitter csf = textRect.GetComponent<ContentSizeFitter>();
        if (csf != null)
        {
            // 强制 ContentSizeFitter 重新计算
            LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        }

        // 标记文本组件为图形脏（确保文本重新渲染）
        inputField.textComponent.SetAllDirty();
    }

    // 刷新布局
    private void RefreshLayout()
    {
        if (inputField?.textComponent == null)
            return;

        RectTransform rectTransform = inputField.textComponent.rectTransform;
        if (rectTransform == null)
            return;

        // 方法1: 使用 LayoutRebuilder 强制重建布局（推荐）
        // 从文本组件开始，向上遍历所有父级，标记布局为脏
        Transform current = rectTransform;
        while (current != null)
        {
            RectTransform rt = current as RectTransform;
            if (rt != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rt);
            }
            current = current.parent;
        }

        // 方法2: 强制立即重建（如果需要立即生效）
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // 方法3: 如果知道具体的 VerticalLayoutGroup，也可以直接标记
        // 注意：这需要确保 VerticalLayoutGroup 在父级层级中
        VerticalLayoutGroup vlg = rectTransform.GetComponentInParent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            RectTransform vlgRect = vlg.GetComponent<RectTransform>();
            if (vlgRect != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(vlgRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(vlgRect);
            }
        }

        // 方法4: 如果文本组件有 ContentSizeFitter，也需要刷新
        ContentSizeFitter csf = rectTransform.GetComponent<ContentSizeFitter>();
        if (csf != null)
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    // 延迟刷新布局（等待文本渲染完成）
    private IEnumerator DelayedRefreshLayout()
    {
        // 等待一帧，确保文本已经渲染
        yield return null;

        // 再次刷新布局
        RefreshLayout();

        // 再等待一帧，确保布局计算完成
        yield return null;

        // 最后再刷新一次，确保所有布局都更新完成
        RefreshLayout();
    }

    // 选择所有文本
    public void SelectAll()
    {
        // inputField.selectionAnchorPosition = 0;
        // inputField.selectionFocusPosition = inputField.text.Length;
        // hasSelection = true;

        // ShowContextMenu();
    }

    // 创建上下文菜单
    private void CreateContextMenu()
    {
        GameObject menuPrefab = Resources.Load<GameObject>("TextSelectionContextMenu");
        if (menuPrefab != null)
        {
            GameObject menuInstance = Instantiate(menuPrefab, FindObjectOfType<Canvas>().transform);
            contextMenu = menuInstance.GetComponent<TextSelectionContextMenu>();
        }
    }

    // 显示上下文菜单
    private void ShowContextMenu()
    {
        // Debug.LogError("ShowContextMenu");
        if (contextMenu != null)
        {
            Vector2 menuPosition = GetCurrentCursorPosition();
            contextMenu.ShowAtPosition(menuPosition);
        }
    }

    private void HideContextMenu()
    {
        // Debug.LogError("HideContextMenu");
        contextMenu?.Hide();
    }

    // 获取当前光标位置（用于显示菜单）
    private Vector2 GetCurrentCursorPosition()
    {
        TextGenerator generator = inputField.textComponent.cachedTextGenerator;
        int cursorPos = inputField.caretPosition;

        if (cursorPos < generator.characterCount)
        {
            UICharInfo charInfo = generator.characters[cursorPos];
            Vector3 worldPos = inputField.textComponent.rectTransform.TransformPoint(charInfo.cursorPos);
            return RectTransformUtility.WorldToScreenPoint(null, worldPos);
        }

        return Input.mousePosition;
    }

    public bool HasSelection()
    {
        int anchorPosition = Math.Min(selectionStart, inputField.selectionFocusPosition);
        int focusPosition = Math.Max(selectionStart, inputField.selectionFocusPosition);
        if (anchorPosition == focusPosition)
        {
            hasSelection = false;
        }
        else
        {
            hasSelection = true;
        }
        Debug.LogError("HasSelection: " + hasSelection + " " + anchorPosition + " " + focusPosition + " " + inputField.selectionAnchorPosition + " " + inputField.selectionFocusPosition + " " + inputField.caretPosition);
        return hasSelection && anchorPosition != focusPosition;
    }

    private void HandleCursorMovement()
    {
        // 只在输入框激活且没有长按时处理光标移动
        if (!inputField.isFocused || isLongPress || !inputField.interactable)
            return;

        // 处理键盘光标移动
        // HandleKeyboardCursorMovement();

        // 处理移动端手势光标移动
        HandleMobileCursorMovement();

        // 更新选择视觉反馈
        // UpdateSelectionVisual();
    }




    private void UpdateSelectionVisual()
    {
        if (hasSelection && inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
        {
            // 这里可以添加自定义的选择高亮效果
            // 比如改变选中文本的颜色或背景
            HighlightSelectedText();
        }
    }

    private void HighlightSelectedText()
    {
        // 简单的选择视觉反馈实现
        // 在实际项目中，你可能需要创建自定义的文本渲染器来实现更好的高亮效果
        if (inputField.textComponent != null)
        {
            // 这里可以修改文本的材质或添加高亮UI元素
            // 由于Unity的InputField限制，完整的文本高亮需要更复杂的实现
        }
    }

#if UNITY_IOS || UNITY_ANDROID
    private void HandleMobileGestures()
    {
        if (!inputField.isFocused || !inputField.interactable)
            return;

#if UNITY_IOS || UNITY_ANDROID
        switch (Input.touchCount)
        {
            case 1:
                HandleSingleTouchGesture();
                break;
            case 2:
                HandleTwoFingerGesture();
                break;
            case 3:
                HandleThreeFingerGesture();
                break;
        }
#endif
    }

    private void HandleSingleTouchGesture()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Stationary:
                // 长按检测已经在Update中处理
                break;

            case TouchPhase.Moved:
                if (isLongPress && hasSelection)
                {
                    // 长按后拖动：调整选择范围
                    UpdateSelectionWithDrag(touch.position);
                }
                else if (!isLongPress && isPointerDown)
                {
                    // 普通拖动：移动光标
                    MoveCursorWithDrag(touch.position);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isLongPress)
                {
                    // 长按选择结束，显示上下文菜单
                    ShowContextMenu();
                }
                break;
        }
    }

    private void HandleTwoFingerGesture()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        // 双指捏合手势：调整文本选择范围
        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            HandlePinchSelection(touch1, touch2);
        }

        // 双指双击：全选
        if (touch1.tapCount == 2 && touch2.tapCount == 2)
        {
            SelectAll();
        }

        // 双指滑动：快速滚动（对于多行文本）
        if (Mathf.Abs(touch1.deltaPosition.y) > 10f || Mathf.Abs(touch2.deltaPosition.y) > 10f)
        {
            ScrollTextContent((touch1.deltaPosition.y + touch2.deltaPosition.y) * 0.5f);
        }
    }

    private void HandleThreeFingerGesture()
    {
        // 三指点击：显示高级编辑菜单
        if (Input.GetTouch(0).phase == TouchPhase.Began &&
            Input.GetTouch(1).phase == TouchPhase.Began &&
            Input.GetTouch(2).phase == TouchPhase.Began)
        {
            ShowAdvancedEditMenu();
        }

        // 三指左滑：撤销
        if (Input.GetTouch(0).phase == TouchPhase.Moved &&
            Input.GetTouch(1).phase == TouchPhase.Moved &&
            Input.GetTouch(2).phase == TouchPhase.Moved)
        {
            Vector2 avgDelta = (Input.GetTouch(0).deltaPosition +
                               Input.GetTouch(1).deltaPosition +
                               Input.GetTouch(2).deltaPosition) / 3f;

            if (avgDelta.x < -20f) // 左滑阈值
            {
                Undo();
            }
            else if (avgDelta.x > 20f) // 右滑阈值
            {
                Redo();
            }
        }
    }

    private void UpdateSelectionWithDrag(Vector2 touchPosition)
    {
        int newSelectionEnd = GetCharacterIndexFromPosition(touchPosition);

        if (newSelectionEnd != selectionEnd)
        {
            selectionEnd = newSelectionEnd;
            UpdateTextSelection();
        }
    }

    private void MoveCursorWithDrag(Vector2 touchPosition)
    {
        int newCursorPos = GetCharacterIndexFromPosition(touchPosition);

        if (newCursorPos != inputField.caretPosition)
        {
            inputField.caretPosition = newCursorPos;
            inputField.selectionAnchorPosition = newCursorPos;
            Debug.LogError("111inputField.selectionAnchorPosition: " + newCursorPos);
            inputField.selectionFocusPosition = newCursorPos;

            // 光标移动的视觉反馈
            ShowCursorMovementFeedback();
        }
    }

    private void HandlePinchSelection(Touch touch1, Touch touch2)
    {
        // 计算两个触摸点的中心位置和距离变化
        Vector2 currentCenter = (touch1.position + touch2.position) * 0.5f;
        float currentDistance = Vector2.Distance(touch1.position, touch2.position);

        // 获取上一次的距离（需要存储历史数据）
        if (!pinchHistory.ContainsKey(0))
        {
            pinchHistory[0] = new PinchData { center = currentCenter, distance = currentDistance };
            return;
        }

        PinchData previousPinch = pinchHistory[0];
        float distanceDelta = currentDistance - previousPinch.distance;

        // 根据捏合手势调整选择范围
        if (Mathf.Abs(distanceDelta) > 5f) // 防止微小抖动
        {
            if (distanceDelta > 0)
            {
                // 双指张开：扩大选择范围
                ExpandSelection();
            }
            else
            {
                // 双指捏合：缩小选择范围
                ShrinkSelection();
            }

            // 更新历史数据
            pinchHistory[0] = new PinchData { center = currentCenter, distance = currentDistance };
        }
    }

    // 支持捏合手势的数据结构
    private Dictionary<int, PinchData> pinchHistory = new Dictionary<int, PinchData>();

    private struct PinchData
    {
        public Vector2 center;
        public float distance;
    }

    private void ExpandSelection()
    {
        if (!hasSelection) return;

        int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
        int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);

        // 向左扩展
        if (start > 0)
        {
            start = Mathf.Max(0, start - 1);
        }

        // 向右扩展
        if (end < inputField.text.Length)
        {
            end = Mathf.Min(inputField.text.Length, end + 1);
        }

        inputField.selectionAnchorPosition = start;
        Debug.LogError("222inputField.selectionAnchorPosition: " + start);
        inputField.selectionFocusPosition = end;
    }

    private void ShrinkSelection()
    {
        if (!hasSelection) return;

        int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
        int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);

        // 选择区域足够大时才缩小
        if (end - start > 2)
        {
            // 从右侧缩小
            end--;

            inputField.selectionAnchorPosition = start;
            Debug.LogError("333inputField.selectionAnchorPosition: " + start);
            inputField.selectionFocusPosition = end;
        }
    }

    private void ScrollTextContent(float deltaY)
    {
        // 对于多行InputField，实现文本滚动
        // 这需要自定义的滚动视图实现
        if (inputField.multiLine && inputField.textComponent != null)
        {
            RectTransform textRect = inputField.textComponent.rectTransform;
            Vector2 currentPos = textRect.anchoredPosition;
            currentPos.y += deltaY * 0.5f; // 滚动速度系数

            // 限制滚动范围
            float maxScroll = Mathf.Max(0, textRect.sizeDelta.y - textRect.rect.height);
            currentPos.y = Mathf.Clamp(currentPos.y, 0, maxScroll);

            textRect.anchoredPosition = currentPos;
        }
    }

    private void ShowCursorMovementFeedback()
    {

    }

    // 撤销/重做功能
    private void Undo()
    {
        // 实现撤销逻辑
        Debug.Log("Undo triggered by three-finger swipe");
    }

    private void Redo()
    {
        // 实现重做逻辑
        Debug.Log("Redo triggered by three-finger swipe");
    }

    private void ShowAdvancedEditMenu()
    {
        // 显示包含撤销、重做、全选等选项的高级菜单
        Debug.Log("Advanced edit menu shown");
    }
#endif


    private void HandleMobileCursorMovement()
    {
#if UNITY_IOS || UNITY_ANDROID
        // 处理移动端特有的光标移动手势
        if (Input.touchCount == 1)
        {
            HandleSingleTouchCursorMovement();
        }
        else if (Input.touchCount == 2)
        {
            HandleDoubleTouchCursorMovement();
        }
#elif UNITY_EDITOR
#endif
    }

    private void HandleSingleTouchCursorMovement()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                OnTouchBeganForCursor(touch);
                break;

            case TouchPhase.Moved:
                OnTouchMovedForCursor(touch);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnTouchEndedForCursor(touch);
                break;

            case TouchPhase.Stationary:
                OnTouchStationaryForCursor(touch);
                break;
        }
    }

    private void OnTouchBeganForCursor(Touch touch)
    {
        // 记录触摸开始信息
        touchStartTime = Time.time;
        touchStartPosition = touch.position;
        isTouchForCursor = true;

        // 检测是否是双击
        if (Time.time - lastTapTime < doubleTapThreshold)
        {
            isDoubleTap = true;
            OnDoubleTap(touch.position);
        }
        else
        {
            isDoubleTap = false;
        }

        lastTapTime = Time.time;
    }

    private void OnTouchMovedForCursor(Touch touch)
    {
        if (!isTouchForCursor) return;

        // 计算移动距离
        float moveDistance = Vector2.Distance(touch.position, touchStartPosition);

        if (moveDistance > cursorMoveThreshold)
        {
            // 移动距离超过阈值，开始光标移动或文本选择
            if (Time.time - touchStartTime > selectionActivationTime)
            {
                // 长按后移动：文本选择模式
                HandleTextSelectionWithTouch(touch.position);
            }
            else
            {
                // 短按移动：光标移动模式
                HandleCursorMovementWithTouch(touch.position);
            }
        }
    }

    private void OnTouchEndedForCursor(Touch touch)
    {
        if (isDoubleTap)
        {
            // 双击处理已经在开始时完成
            return;
        }

        float moveDistance = Vector2.Distance(touch.position, touchStartPosition);

        if (moveDistance < tapThreshold && Time.time - touchStartTime < maxTapTime)
        {
            // 轻触：定位光标
            PositionCursorAtTouch(touch.position);
        }

        isTouchForCursor = false;
        isSelectingWithTouch = false;
    }

    private void OnTouchStationaryForCursor(Touch touch)
    {
        // 处理长按激活文本选择
        if (Time.time - touchStartTime > longPressThreshold && !isSelectingWithTouch)
        {
            ActivateTextSelectionMode(touch.position);
        }
    }

    private void HandleCursorMovementWithTouch(Vector2 touchPosition)
    {
        int newCursorPosition = GetCharacterIndexFromPosition(touchPosition);

        if (newCursorPosition != currentCursorPosition)
        {
            currentCursorPosition = newCursorPosition;
            inputField.caretPosition = newCursorPosition;
            inputField.selectionAnchorPosition = newCursorPosition;
            Debug.LogError("444inputField.selectionAnchorPosition: " + newCursorPosition);
            inputField.selectionFocusPosition = newCursorPosition;

            // 更新光标视觉位置
            UpdateCursorVisualPosition();

            // 触觉反馈
            ProvideCursorHapticFeedback();
        }
    }

    private void HandleTextSelectionWithTouch(Vector2 touchPosition)
    {
        if (!isSelectingWithTouch)
        {
            // 第一次进入选择模式
            selectionStartPosition = GetCharacterIndexFromPosition(touchStartPosition);
            selectionEndPosition = selectionStartPosition;
            isSelectingWithTouch = true;
        }

        int newSelectionEnd = GetCharacterIndexFromPosition(touchPosition);

        if (newSelectionEnd != selectionEndPosition)
        {
            selectionEndPosition = newSelectionEnd;

            // 更新文本选择
            int start = Mathf.Min(selectionStartPosition, selectionEndPosition);
            int end = Mathf.Max(selectionStartPosition, selectionEndPosition);

            inputField.selectionAnchorPosition = start;
            Debug.LogError("555inputField.selectionAnchorPosition: " + start);
            inputField.selectionFocusPosition = end;
            hasSelection = true;

            // 更新选择视觉
            UpdateSelectionVisual();

            // 选择调整的触觉反馈
            ProvideSelectionHapticFeedback();
        }
    }

    private void HandleDoubleTouchCursorMovement()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        // 双指滑动：快速光标移动
        if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
        {
            Vector2 avgDelta = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;

            if (Mathf.Abs(avgDelta.x) > doubleTouchMoveThreshold)
            {
                // 双指水平滑动：单词级光标移动
                HandleWordLevelCursorMovement(avgDelta.x);
            }

            if (Mathf.Abs(avgDelta.y) > doubleTouchMoveThreshold)
            {
                // 双指垂直滑动：行级光标移动
                HandleLineLevelCursorMovement(avgDelta.y);
            }
        }

        // 双指捏合：调整光标宽度或选择模式
        if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) &&
            Vector2.Distance(touch1.position, touch2.position) < startPinchDistance - pinchThreshold)
        {
            SwitchToWordSelectionMode();
        }
    }

    private void HandleWordLevelCursorMovement(float deltaX)
    {
        int direction = deltaX > 0 ? 1 : -1;
        int newPosition = GetNextWordBoundary(inputField.caretPosition, direction);

        inputField.caretPosition = newPosition;
        inputField.selectionAnchorPosition = newPosition;
        Debug.LogError("666inputField.selectionAnchorPosition: " + newPosition);
        inputField.selectionFocusPosition = newPosition;

        UpdateCursorVisualPosition();
        ProvideWordNavigationHapticFeedback();
    }

    private void HandleLineLevelCursorMovement(float deltaY)
    {
        int direction = deltaY > 0 ? -1 : 1; // 注意：y轴方向与直觉相反
        int newPosition = GetNextLinePosition(inputField.caretPosition, direction);

        inputField.caretPosition = newPosition;
        inputField.selectionAnchorPosition = newPosition;
        Debug.LogError("777inputField.selectionAnchorPosition: " + newPosition);
        inputField.selectionFocusPosition = newPosition;

        UpdateCursorVisualPosition();
    }

    private int GetNextWordBoundary(int currentPosition, int direction)
    {
        string text = inputField.text;

        if (string.IsNullOrEmpty(text)) return currentPosition;

        if (direction > 0)
        {
            // 向右移动到下一个单词边界
            for (int i = currentPosition + 1; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]) || i == text.Length - 1)
                {
                    return i;
                }
            }
        }
        else
        {
            // 向左移动到上一个单词边界
            for (int i = currentPosition - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(text[i]) || i == 0)
                {
                    return i;
                }
            }
        }

        return currentPosition;
    }

    private int GetNextLinePosition(int currentPosition, int direction)
    {
        string text = inputField.text;

        if (string.IsNullOrEmpty(text) || !inputField.multiLine)
            return currentPosition;

        // 查找当前行
        int currentLineStart = 0;
        int currentLineEnd = text.Length;
        int currentLine = 0;

        for (int i = 0; i <= currentPosition && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                currentLine++;
                currentLineStart = i + 1;
            }
        }

        // 查找目标行
        int targetLine = currentLine + direction;
        if (targetLine < 0) targetLine = 0;

        // 计算目标行的位置（简化实现）
        int lineCount = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lineCount++;
                if (lineCount == targetLine)
                {
                    return Mathf.Min(i + 1, text.Length);
                }
            }
        }

        return direction > 0 ? text.Length : 0;
    }

    private void PositionCursorAtTouch(Vector2 touchPosition)
    {
        int characterIndex = GetCharacterIndexFromPosition(touchPosition);

        inputField.caretPosition = characterIndex;
        inputField.selectionAnchorPosition = characterIndex;
        Debug.LogError("888inputField.selectionAnchorPosition: " + characterIndex);
        inputField.selectionFocusPosition = characterIndex;
        hasSelection = false;

        UpdateCursorVisualPosition();
        ProvideTapHapticFeedback();
    }

    private void OnDoubleTap(Vector2 position)
    {
        // 双击选择单词
        // SelectWordAtPosition(position);
        ProvideDoubleTapHapticFeedback();
    }

    private void ActivateTextSelectionMode(Vector2 position)
    {
        selectionStartPosition = GetCharacterIndexFromPosition(position);
        selectionEndPosition = selectionStartPosition;
        isSelectingWithTouch = true;
        hasSelection = true;

        inputField.selectionAnchorPosition = selectionStartPosition;
        Debug.LogError("999inputField.selectionAnchorPosition: " + selectionStartPosition);
        inputField.selectionFocusPosition = selectionEndPosition;

        // 显示选择手柄
        ShowSelectionHandles();
        ProvideLongPressHapticFeedback();
    }

    private void SwitchToWordSelectionMode()
    {
        // 切换到按单词选择模式
        if (hasSelection)
        {
            ExpandSelectionToWordBoundaries();
        }
        // ProvideModeSwitchHapticFeedback();
    }

    private void ExpandSelectionToWordBoundaries()
    {
        int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
        int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);

        // 向左扩展到单词边界
        while (start > 0 && !char.IsWhiteSpace(inputField.text[start - 1]))
        {
            start--;
        }

        // 向右扩展到单词边界
        while (end < inputField.text.Length && !char.IsWhiteSpace(inputField.text[end]))
        {
            end++;
        }

        inputField.selectionAnchorPosition = start;
        Debug.LogError("101010inputField.selectionAnchorPosition: " + start);
        inputField.selectionFocusPosition = end;
    }

    // 触觉反馈方法
    private void ProvideCursorHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    Vibrate(10);
#elif UNITY_IOS && !UNITY_EDITOR
    // iOS触觉反馈
#endif
    }

    private void ProvideSelectionHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    Vibrate(15);
#endif
    }

    private void ProvideWordNavigationHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    Vibrate(20);
#endif
    }

    private void ProvideTapHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    Vibrate(5);
#endif
    }

    private void ProvideDoubleTapHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    Vibrate(25);
#endif
    }

    private void ProvideLongPressHapticFeedback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    Vibrate(30);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
private void Vibrate(long milliseconds)
{
    try
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
        {
            if (vibrator != null)
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
    }
    catch (System.Exception e)
    {
        Debug.LogWarning("Vibration failed: " + e.Message);
    }
}
#endif

    // 需要的成员变量
    private float touchStartTime;
    private Vector2 touchStartPosition;
    private bool isTouchForCursor = false;
    private bool isSelectingWithTouch = false;
    private bool isDoubleTap = false;
    private float lastTapTime;
    private int currentCursorPosition = 0;
    private int selectionStartPosition = 0;
    private int selectionEndPosition = 0;

    // 配置参数
    private const float doubleTapThreshold = 0.3f;
    private const float cursorMoveThreshold = 10f;
    private const float selectionActivationTime = 0.5f;
    private const float tapThreshold = 5f;
    private const float maxTapTime = 0.2f;
    private const float doubleTouchMoveThreshold = 5f;
    private const float startPinchDistance = 100f;
    private const float pinchThreshold = 20f;

    // 选择手柄相关
    private void ShowSelectionHandles()
    {
        // 实现选择手柄的显示
        // 这需要创建两个可拖动的UI元素来表示选择范围的开始和结束
    }

    private void UpdateCursorVisualPosition()
    {
        // 更新自定义光标的视觉位置
        // 可以在这里添加光标移动动画等效果
    }

    public int testpos = 1;



    [Button("Test")]
    public void Test()
    {
        inputField.selectionAnchorPosition = 0;
        inputField.selectionFocusPosition = 122;
        inputField.caretPosition = -1;
        inputField.ForceLabelUpdate();

        // 确保InputField获得焦点
        inputField.ActivateInputField();
        inputField.Select();

        // GameObject.Find("realTxt").GetComponent<ContentSizeFitter>().SetLayoutVertical();
        // GameObject.Find("realTxt").GetComponent<VerticalLayoutGroup>().SetLayoutVertical();
        // RefreshLayout();
    }

}